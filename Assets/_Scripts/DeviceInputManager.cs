using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Unified input manager: toggle between keyboard simulation and real Arduino hardware
/// in the Inspector. Replaces HardwareSimulator as the data source for DistractionManager.
///
/// Arduino output format (combined_water_sensors.ino):
///   "waterLevel,isTouching,isGrounding,isBlowing\n"
///   e.g. "2,1,0,0" — baud 115200
///
/// Keyboard fallback controls:
///   0-3       → water level
///   Space     → hold to touch
///   Enter     → hold to ground
///   B         → blow pulse
///
/// Blow debounce (hardware mode only):
///   The raw sensor signal must read 1 for BlowConfirmFrames consecutive Unity frames
///   before a blow is confirmed. After confirmation, BlowCooldownSeconds must elapse
///   before the next blow can be registered. This prevents accidental triggers from
///   talking, movement, or sensor noise.
/// </summary>
public class DeviceInputManager : MonoBehaviour
{
    public static DeviceInputManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────
    [Header("Input Mode")]
    [Tooltip("OFF = keyboard simulation  |  ON = read from Arduino via Serial port")]
    public bool useHardwareInput = false;

    [Header("Serial Port Settings  (Hardware Mode Only)")]
    [Tooltip("Windows: COM3, COM4 …   macOS/Linux: /dev/ttyUSB0, /dev/cu.usbmodem… ")]
    public string portName = "COM3";
    public int baudRate = 115200;

    [Header("Blow Debounce  (Hardware Mode Only)")]
    [Tooltip("Consecutive Unity frames the sensor must read 1 before a blow is confirmed")]
    [SerializeField] private int blowConfirmFrames = 3;
    [Tooltip("Seconds before another blow can be confirmed — prevents repeated triggers")]
    [SerializeField] private float blowCooldownSeconds = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugOverlay = false;

    [Header("Current Values  (Read Only)")]
    public int Level      = 0;
    public int isTouching = 0;
    public int isGrounding = 0;
    public int isBlowing  = 0;

    // ── Serial internals ─────────────────────────────────────────────
    private SerialPort _serial;
    private Thread     _readThread;
    private volatile bool _running;

    // Background thread writes here; main thread reads in PollSerial()
    private volatile int _stageLevel;
    private volatile int _stageTouching;
    private volatile int _stageGrounding;
    private volatile int _stageBlowing;

    // Track whether hardware mode was active so we can react to runtime toggling
    private bool _wasHardwareActive;

    // Blow pulse: keeps isBlowing=1 for 2 updates so all scripts catch it
    // regardless of script execution order.
    private int _blowFramesLeft;

    // Hardware blow debounce state
    private int   _blowConsecutiveCount;
    private float _blowCooldownTimer;

    // ── Unity lifecycle ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (useHardwareInput) StartSerial();
        _wasHardwareActive = useHardwareInput;
    }

    private void OnDisable() => StopSerial();

    private void Update()
    {
        // Support toggling useHardwareInput at runtime via Inspector
        if (useHardwareInput != _wasHardwareActive)
        {
            if (useHardwareInput) StartSerial();
            else                  StopSerial();
            _wasHardwareActive = useHardwareInput;
        }

        if (useHardwareInput)
            PollSerial();
        else
            PollKeyboard();

        // Unified blow output for both modes: emit a 2-frame pulse then go back to 0
        isBlowing = _blowFramesLeft > 0 ? 1 : 0;
        if (_blowFramesLeft > 0) _blowFramesLeft--;
    }

    // ── Keyboard fallback ────────────────────────────────────────────
    private void PollKeyboard()
    {
        if      (Input.GetKeyDown(KeyCode.Alpha0)) Level = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha1)) Level = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) Level = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) Level = 3;

        isTouching  = Input.GetKey(KeyCode.Space)  ? 1 : 0;
        isGrounding = Input.GetKey(KeyCode.Return)  ? 1 : 0;

        if (Input.GetKeyDown(KeyCode.B))
            _blowFramesLeft = 2;
    }

    // ── Serial polling (main thread) ─────────────────────────────────
    private void PollSerial()
    {
        Level       = _stageLevel;
        isTouching  = _stageTouching;
        isGrounding = _stageGrounding;

        // Debounce: require blowConfirmFrames consecutive 1s before confirming.
        // After confirmation, ignore the sensor for blowCooldownSeconds.
        _blowCooldownTimer = Mathf.Max(0f, _blowCooldownTimer - Time.deltaTime);

        if (_stageBlowing == 1 && _blowCooldownTimer <= 0f)
        {
            _blowConsecutiveCount++;
            if (_blowConsecutiveCount >= blowConfirmFrames)
            {
                _blowFramesLeft       = 2;   // emit pulse; Update() drives isBlowing
                _blowConsecutiveCount = 0;
                _blowCooldownTimer    = blowCooldownSeconds;
                Debug.Log("[DeviceInputManager] Blow confirmed.");
            }
        }
        else
        {
            // Reset streak if sensor drops back to 0, or cooldown is still active
            if (_stageBlowing == 0)
                _blowConsecutiveCount = 0;
        }
    }

    // ── Serial port management ────────────────────────────────────────
    private void StartSerial()
    {
        if (_running) return;
        try
        {
            _serial = new SerialPort(portName, baudRate) { ReadTimeout = 200 };
            _serial.Open();
            _running    = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
            Debug.Log($"[DeviceInputManager] Serial opened: {portName} @ {baudRate} baud");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DeviceInputManager] Cannot open {portName}: {e.Message}\n" +
                           "Falling back to keyboard input.");
            useHardwareInput = false;
            _wasHardwareActive = false;
        }
    }

    private void StopSerial()
    {
        _running = false;
        if (_readThread != null && _readThread.IsAlive)
            _readThread.Join(500);
        if (_serial != null && _serial.IsOpen)
            _serial.Close();
        _serial     = null;
        _readThread = null;
    }

    // ── Background read loop ──────────────────────────────────────────
    private void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string raw = _serial.ReadLine();
                ParseLine(raw.Trim());
            }
            catch (TimeoutException)
            {
                // expected when no data arrives within ReadTimeout — just loop
            }
            catch (Exception)
            {
                // port disconnected or error — stop the thread gracefully
                if (_running) _running = false;
            }
        }
    }

    // Parses "waterLevel,isTouching,isGrounding,isBlowing"
    // Matches combined_water_sensors.ino Serial.println output
    private void ParseLine(string line)
    {
        string[] parts = line.Split(',');
        if (parts.Length != 4) return;

        if (int.TryParse(parts[0].Trim(), out int lv))
            _stageLevel = lv < 0 ? 0 : (lv > 3 ? 3 : lv);

        if (int.TryParse(parts[1].Trim(), out int tc))
            _stageTouching = tc;

        if (int.TryParse(parts[2].Trim(), out int gr))
            _stageGrounding = gr;

        if (int.TryParse(parts[3].Trim(), out int bl))
            _stageBlowing = bl;
    }

    // ── Debug overlay ─────────────────────────────────────────────────
    private void OnGUI()
    {
        if (!showDebugOverlay) return;

        string source = useHardwareInput
            ? (_running ? $"Hardware  {portName}" : $"Hardware  {portName}  (ERROR — check Console)")
            : "Keyboard";

        GUI.Label(new Rect(10f, 10f, 320f, 20f), $"[DeviceInputManager]  {source}");
        GUI.Label(new Rect(10f, 30f, 320f, 20f), $"Level:       {Level}");
        GUI.Label(new Rect(10f, 50f, 320f, 20f), $"isTouching:  {isTouching}");
        GUI.Label(new Rect(10f, 70f, 320f, 20f), $"isGrounding: {isGrounding}");
        GUI.Label(new Rect(10f, 90f, 320f, 20f), $"isBlowing:   {isBlowing}");
        if (useHardwareInput && _blowCooldownTimer > 0f)
            GUI.Label(new Rect(10f, 110f, 320f, 20f), $"Blow cooldown: {_blowCooldownTimer:F1}s  (streak: {_blowConsecutiveCount}/{blowConfirmFrames})");
    }

    private void OnDestroy() => StopSerial();
}
