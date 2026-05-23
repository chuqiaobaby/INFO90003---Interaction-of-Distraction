using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public sealed class HandTrackingUdpReceiver : MonoBehaviour
{
    public static HandTrackingUdpReceiver Instance { get; private set; }

    [Header("UDP")]
    [SerializeField] private int port = 5052;

    [Header("Coordinate Calibration")]
    [SerializeField] private bool flipX;
    [SerializeField] private bool flipY = true;
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.18f;
    [SerializeField] private float lostTimeout = 0.25f;

    public Vector2 NormalizedPosition { get; private set; } = new Vector2(0.5f, 0.5f);
    public bool HandVisible { get; private set; }
    public float LastReceivedTime { get; private set; } = -1000f;

    private UdpClient client;
    private Thread receiveThread;
    private volatile bool running;
    private readonly object packetLock = new object();
    private Vector2 stagedPosition = new Vector2(0.5f, 0.5f);
    private bool stagedVisible;
    private bool hasPacket;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        StartReceiver();
    }

    private void Update()
    {
        bool receivedPacket = false;
        Vector2 nextPosition = NormalizedPosition;
        bool nextVisible = false;

        lock (packetLock)
        {
            if (hasPacket)
            {
                nextPosition = stagedPosition;
                nextVisible = stagedVisible;
                hasPacket = false;
                receivedPacket = true;
            }
        }

        if (receivedPacket)
        {
            LastReceivedTime = Time.time;
            HandVisible = nextVisible;
            float lerpAmount = smoothing <= 0f ? 1f : 1f - Mathf.Pow(smoothing, Time.deltaTime * 60f);
            NormalizedPosition = Vector2.Lerp(NormalizedPosition, nextPosition, lerpAmount);
        }
        else if (Time.time - LastReceivedTime > lostTimeout)
        {
            HandVisible = false;
        }
    }

    private void OnDisable()
    {
        StopReceiver();
    }

    private void OnDestroy()
    {
        StopReceiver();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void StartReceiver()
    {
        if (running)
        {
            return;
        }

        try
        {
            client = new UdpClient(port);
            running = true;
            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true
            };
            receiveThread.Start();
            Debug.Log($"[HandTrackingUdpReceiver] Listening on UDP port {port}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandTrackingUdpReceiver] Could not open UDP port {port}: {e.Message}");
            StopReceiver();
        }
    }

    private void StopReceiver()
    {
        running = false;
        client?.Close();
        client = null;

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(250);
        }

        receiveThread = null;
    }

    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = client.Receive(ref remote);
                string message = System.Text.Encoding.UTF8.GetString(data);

                if (TryParsePacket(message, out Vector2 position, out bool visible))
                {
                    lock (packetLock)
                    {
                        stagedPosition = position;
                        stagedVisible = visible;
                        hasPacket = true;
                    }
                }
            }
            catch (SocketException)
            {
                if (running)
                {
                    Debug.LogWarning("[HandTrackingUdpReceiver] UDP socket stopped unexpectedly.");
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }

    private bool TryParsePacket(string message, out Vector2 position, out bool visible)
    {
        position = NormalizedPosition;
        visible = false;

        string[] parts = message.Trim().Split(',');
        if (parts.Length < 2)
        {
            return false;
        }

        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            return false;
        }

        visible = true;
        if (parts.Length >= 3)
        {
            string flag = parts[2].Trim();
            visible = flag == "1" || flag.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        x = flipX ? 1f - x : x;
        y = flipY ? 1f - y : y;
        position = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
        return true;
    }
}
