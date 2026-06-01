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
///   Duration filtering is done on the Arduino (BLOW_DURATION_MS). Unity detects the
///   rising edge of isBlowing and enforces a cooldown (blowCooldownSeconds) to prevent
///   repeated triggers.
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
    [Tooltip("Seconds before another blow can be confirmed — prevents repeated triggers. " +
             "Duration filtering is handled by the Arduino (BLOW_DURATION_MS).")]
    [SerializeField] private float blowCooldownSeconds = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugOverlay = false;
    [SerializeField] private bool logSerialLines = false;

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
    private volatile int _validSerialLineCount;
    private volatile int _invalidSerialLineCount;
    private volatile int _drainedSerialLineCount;
    private long _lastSerialLineUtcTicks;
    private string _lastSerialLine = "";

    // Track whether hardware mode was active so we can react to runtime toggling
    private bool _wasHardwareActive;

    // Blow pulse: keeps isBlowing=1 for 2 updates so all scripts catch it
    // regardless of script execution order.
    private int _blowFramesLeft;

    // Hardware blow debounce state
    private int   _prevStageBlowing;
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
        int prevBlowing = isBlowing;
        isBlowing = _blowFramesLeft > 0 ? 1 : 0;
        if (_blowFramesLeft > 0) _blowFramesLeft--;
        if (isBlowing == 1 && prevBlowing == 0)
            Debug.Log("[DeviceInputManager] Blow triggered!");
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

        // Arduino already filters blow duration (BLOW_DURATION_MS).
        // Unity just detects the rising edge and enforces a cooldown.
        _blowCooldownTimer = Mathf.Max(0f, _blowCooldownTimer - Time.deltaTime);

        if (_stageBlowing == 1 && _prevStageBlowing == 0 && _blowCooldownTimer <= 0f)
        {
            _blowFramesLeft    = 2;
            _blowCooldownTimer = blowCooldownSeconds;
        }
        _prevStageBlowing = _stageBlowing;
    }

    // ── Serial port management ────────────────────────────────────────
    private void StartSerial()
    {
        if (_running) return;
        try
        {
            ResetSerialState();

            _serial = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 50,
                NewLine = "\n"
            };
            _serial.Open();
            _serial.DiscardInBuffer();
            _serial.DiscardOutBuffer();

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

        if (_serial != null)
        {
            try
            {
                if (_serial.IsOpen)
                {
                    _serial.DiscardInBuffer();
                    _serial.DiscardOutBuffer();
                    _serial.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeviceInputManager] Error while closing serial port: {e.Message}");
            }
            finally
            {
                _serial.Dispose();
            }
        }

        _serial     = null;
        _readThread = null;
    }

    private void ResetSerialState()
    {
        _stageLevel = 0;
        _stageTouching = 0;
        _stageGrounding = 0;
        _stageBlowing = 0;
        _prevStageBlowing = 0;
        _blowFramesLeft = 0;
        _blowCooldownTimer = 0f;
        _validSerialLineCount = 0;
        _invalidSerialLineCount = 0;
        _drainedSerialLineCount = 0;
        Interlocked.Exchange(ref _lastSerialLineUtcTicks, 0L);
        _lastSerialLine = "";
    }

    // ── Background read loop ──────────────────────────────────────────
    private void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string latest = _serial.ReadLine();

                // If Unity ever falls behind, skip stale buffered packets and keep
                // only the newest complete sensor state. This prevents multi-second
                // touch latency from old serial lines being replayed one by one.
                int drainedThisTick = 0;
                while (_serial.BytesToRead > 0 && drainedThisTick < 32)
                {
                    try
                    {
                        latest = _serial.ReadLine();
                        drainedThisTick++;
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                if (drainedThisTick > 0)
                    _drainedSerialLineCount += drainedThisTick;

                ParseLine(latest.Trim());
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
        if (string.IsNullOrWhiteSpace(line)) return;

        string[] parts = line.Split(',');
        if (parts.Length != 4)
        {
            _invalidSerialLineCount++;
            return;
        }

        int lv = 0;
        int tc = 0;
        int gr = 0;
        int bl = 0;

        bool ok =
            int.TryParse(parts[0].Trim(), out lv) &
            int.TryParse(parts[1].Trim(), out tc) &
            int.TryParse(parts[2].Trim(), out gr) &
            int.TryParse(parts[3].Trim(), out bl);

        if (!ok)
        {
            _invalidSerialLineCount++;
            return;
        }

        _stageLevel = lv < 0 ? 0 : (lv > 3 ? 3 : lv);
        _stageTouching = tc != 0 ? 1 : 0;
        _stageGrounding = gr != 0 ? 1 : 0;
        _stageBlowing = bl != 0 ? 1 : 0;

        _validSerialLineCount++;
        Interlocked.Exchange(ref _lastSerialLineUtcTicks, DateTime.UtcNow.Ticks);
        _lastSerialLine = line;

        if (logSerialLines)
            Debug.Log($"[DeviceInputManager] Serial latest: {line}");
    }

    // ── Debug overlay ─────────────────────────────────────────────────
    private void OnGUI()
    {
        if (!showDebugOverlay) return;

        string source = useHardwareInput
            ? (_running ? $"Hardware  {portName}" : $"Hardware  {portName}  (ERROR — check Console)")
            : "Keyboard";

        GUI.Label(new Rect(10f, 10f, 520f, 20f), $"[DeviceInputManager]  {source}");
        GUI.Label(new Rect(10f, 30f, 520f, 20f), $"Level:       {Level}");
        GUI.Label(new Rect(10f, 50f, 520f, 20f), $"isTouching:  {isTouching}");
        GUI.Label(new Rect(10f, 70f, 520f, 20f), $"isGrounding: {isGrounding}");
        GUI.Label(new Rect(10f, 90f, 520f, 20f), $"isBlowing:   {isBlowing}");
        if (useHardwareInput && _blowCooldownTimer > 0f)
            GUI.Label(new Rect(10f, 110f, 520f, 20f), $"Blow cooldown: {_blowCooldownTimer:F1}s");
        if (useHardwareInput)
        {
            long lastTicks = Interlocked.Read(ref _lastSerialLineUtcTicks);
            double age = lastTicks > 0L
                ? (DateTime.UtcNow.Ticks - lastTicks) / (double)TimeSpan.TicksPerSecond
                : -1f;
            GUI.Label(new Rect(10f, 130f, 520f, 20f), $"Serial valid/invalid/drained: {_validSerialLineCount}/{_invalidSerialLineCount}/{_drainedSerialLineCount}");
            GUI.Label(new Rect(10f, 150f, 520f, 20f), $"Last line age: {(age >= 0f ? age.ToString("F2") : "—")}s");
            GUI.Label(new Rect(10f, 170f, 520f, 20f), $"Last line: {_lastSerialLine}");
        }
    }

    private void OnDestroy() => StopSerial();
}
