using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Experimental;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class Display2MediaPipeHandTracker : MonoBehaviour
{
    public static Display2MediaPipeHandTracker Instance { get; private set; }

    [Header("Camera")]
    [Tooltip("-1 = automatically choose an external camera. Use a fixed index only for manual override.")]
    [SerializeField] private int cameraDeviceIndex = -1;
    [SerializeField] private string preferredCameraNameContains = "C922 Pro Stream Webcam";
    [Tooltip("Fallback rule: when the preferred name is not reported correctly by macOS/Unity, avoid this camera name and use another external camera.")]
    [SerializeField] private string avoidCameraNameContains = "Logitech MeetUp";
    [Tooltip("0 = first matching external camera, 1 = second matching external camera. Used only after name matching and avoid filtering.")]
    [SerializeField] private int externalCameraOrdinal = 0;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;

    [Header("Hand Position")]
    [Tooltip("MediaPipe hand landmark index. 8 = index fingertip, 9 = palm/middle base area.")]
    [SerializeField, Range(0, 20)] private int landmarkIndex = 8;
    [SerializeField] private bool rotatePosition180;
    [SerializeField] private bool flipX;
    [SerializeField] private bool flipY;
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.04f;
    [SerializeField] private float lostTimeout = 0.35f;

    [Header("Detection")]
    [SerializeField, Range(1, 4)] private int maxHands = 2;
    [SerializeField, Range(0f, 1f)] private float minHandDetectionConfidence = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minHandPresenceConfidence = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minTrackingConfidence = 0.5f;

    public Vector2 NormalizedPosition { get; private set; } = new Vector2(0.5f, 0.5f);
    public int HandCount { get; private set; }
    public bool HandVisible { get; private set; }
    public string ActiveCameraName { get; private set; } = "";
    public Texture CurrentCameraTexture => webCamTexture;
    public bool RotatePosition180 => rotatePosition180;
    public bool FlipX => flipX;
    public bool FlipY => flipY;

    public void ConfigureCameraSelection(string preferredNameContains, string avoidNameContains, int deviceIndex, int externalOrdinal)
    {
        preferredCameraNameContains = preferredNameContains ?? string.Empty;
        avoidCameraNameContains = avoidNameContains ?? string.Empty;
        cameraDeviceIndex = deviceIndex;
        externalCameraOrdinal = externalOrdinal;
    }

    public void ConfigurePositionMapping(bool rotate180, bool shouldFlipX, bool shouldFlipY, float smoothingAmount)
    {
        rotatePosition180 = rotate180;
        flipX = shouldFlipX;
        flipY = shouldFlipY;
        smoothing = Mathf.Clamp01(smoothingAmount);
    }

    private WebCamTexture webCamTexture;
    private TextureFramePool textureFramePool;
    private HandLandmarker handLandmarker;
    private HandLandmarkerResult handResult;
    private Coroutine runCoroutine;
    private float lastSeenTime = -1000f;
    private readonly Stopwatch stopwatch = new Stopwatch();
    private readonly Vector2[] normalizedPositions = new Vector2[4];
    private readonly bool[] handSlotInitialized = new bool[4];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (runCoroutine == null)
        {
            runCoroutine = StartCoroutine(RunAfterConfiguration());
        }
    }

    private IEnumerator RunAfterConfiguration()
    {
        // ActivateProjector adds this component at runtime, then immediately applies
        // the Inspector camera-name settings. Wait one frame so those settings win.
        yield return null;
        yield return Run();
    }

    private void Update()
    {
        if (HandVisible && Time.time - lastSeenTime > lostTimeout)
        {
            HandVisible = false;
            HandCount = 0;
            ClearHandSlots();
        }
    }

    private void OnDisable()
    {
        StopTracker();
    }

    private void OnDestroy()
    {
        StopTracker();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator Run()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            UnityEngine.Debug.LogWarning("[Display2MediaPipe] Webcam permission denied.");
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[Display2MediaPipe] No webcam devices found.");
            yield break;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            string cameraType = GetCameraTypeLabel(devices[i].name);
            UnityEngine.Debug.Log($"[Display2MediaPipe] Camera device {i}: {devices[i].name} ({cameraType})");
        }

        int selectedIndex = SelectCameraIndex(devices);
        ActiveCameraName = devices[selectedIndex].name;
        webCamTexture = new WebCamTexture(ActiveCameraName, requestedWidth, requestedHeight, requestedFps);
        webCamTexture.Play();
        UnityEngine.Debug.Log($"[Display2MediaPipe] Camera connected: {ActiveCameraName} (index {selectedIndex})");

        float waitStart = Time.realtimeSinceStartup;
        while (webCamTexture.width < 64 && Time.realtimeSinceStartup - waitStart < 3f)
        {
            yield return null;
        }

        if (webCamTexture.width < 64)
        {
            UnityEngine.Debug.LogWarning("[Display2MediaPipe] Camera did not provide frames in time.");
            yield break;
        }

        byte[] modelBytes = LoadHandLandmarkerModel();
        var options = new HandLandmarkerOptions(
            new BaseOptions(BaseOptions.Delegate.CPU, modelAssetBuffer: modelBytes),
            runningMode: RunningMode.VIDEO,
            numHands: maxHands,
            minHandDetectionConfidence: minHandDetectionConfidence,
            minHandPresenceConfidence: minHandPresenceConfidence,
            minTrackingConfidence: minTrackingConfidence);

        handLandmarker = HandLandmarker.CreateFromOptions(options);
        handResult = HandLandmarkerResult.Alloc(maxHands);
        textureFramePool = new TextureFramePool(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, 3);
        stopwatch.Restart();

        var waitForEndOfFrame = new WaitForEndOfFrame();
        var imageProcessingOptions = new ImageProcessingOptions(rotationDegrees: 0);

        while (enabled && gameObject.activeInHierarchy)
        {
            if (!textureFramePool.TryGetTextureFrame(out TextureFrame textureFrame))
            {
                yield return null;
                continue;
            }

            yield return waitForEndOfFrame;
            textureFrame.ReadTextureOnCPU(webCamTexture);

            using (Mediapipe.Image image = textureFrame.BuildCPUImage())
            {
                textureFrame.Release();
                bool found = handLandmarker.TryDetectForVideo(
                    image,
                    stopwatch.ElapsedMilliseconds,
                    imageProcessingOptions,
                    ref handResult);

                if (found)
                {
                    UpdatePositionFromResult(handResult);
                }
                else if (Time.time - lastSeenTime > lostTimeout)
                {
                    HandVisible = false;
                    HandCount = 0;
                }
            }
        }
    }

    private int SelectCameraIndex(WebCamDevice[] devices)
    {
        if (!string.IsNullOrWhiteSpace(preferredCameraNameContains))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name.IndexOf(preferredCameraNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i;
                }
            }
        }

        if (cameraDeviceIndex >= 0 && cameraDeviceIndex < devices.Length)
        {
            return cameraDeviceIndex;
        }

        // Prefer a true external camera for Display 2 hand tracking. If the C922 name
        // is garbled by macOS/Unity, use the external camera that is not reserved for Display 1.
        int externalSeen = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!IsSelectableFallbackExternal(devices[i].name))
            {
                continue;
            }

            if (externalSeen == Mathf.Max(0, externalCameraOrdinal))
            {
                return i;
            }

            externalSeen++;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (!LooksLikeBuiltInCamera(devices[i].name) && !LooksLikeContinuityCamera(devices[i].name))
            {
                return i;
            }
        }

        // If there is no USB/external camera, use iPhone Continuity Camera before MacBook built-in.
        for (int i = 0; i < devices.Length; i++)
        {
            if (LooksLikeContinuityCamera(devices[i].name))
            {
                return i;
            }
        }

        return devices.Length > 1 ? 1 : 0;
    }

    private bool IsSelectableFallbackExternal(string deviceName)
    {
        if (LooksLikeBuiltInCamera(deviceName) || LooksLikeContinuityCamera(deviceName))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(avoidCameraNameContains) &&
            deviceName.IndexOf(avoidCameraNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeBuiltInCamera(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return false;
        }

        return deviceName.IndexOf("MacBook", StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("FaceTime", StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("Built-in", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeContinuityCamera(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return false;
        }

        return deviceName.IndexOf("Continuity", StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetCameraTypeLabel(string deviceName)
    {
        if (LooksLikeBuiltInCamera(deviceName))
        {
            return "built-in";
        }

        if (LooksLikeContinuityCamera(deviceName))
        {
            return "continuity/iPhone";
        }

        return "external";
    }

    private void UpdatePositionFromResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
        {
            HandVisible = false;
            HandCount = 0;
            ClearHandSlots();
            return;
        }

        int count = Mathf.Min(result.handLandmarks.Count, normalizedPositions.Length);
        int validCount = 0;

        for (int i = 0; i < count; i++)
        {
            NormalizedLandmarks hand = result.handLandmarks[i];
            if (hand.landmarks == null || hand.landmarks.Count <= landmarkIndex)
            {
                continue;
            }

            NormalizedLandmark landmark = hand.landmarks[landmarkIndex];
            float x = flipX ? 1f - landmark.x : landmark.x;
            float y = flipY ? 1f - landmark.y : landmark.y;

            if (rotatePosition180)
            {
                x = 1f - x;
                y = 1f - y;
            }

            Vector2 target = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
            float lerpAmount = smoothing <= 0f ? 1f : 1f - Mathf.Pow(smoothing, Time.deltaTime * 60f);
            if (!handSlotInitialized[validCount])
            {
                normalizedPositions[validCount] = target;
                handSlotInitialized[validCount] = true;
            }
            else
            {
                normalizedPositions[validCount] = Vector2.Lerp(normalizedPositions[validCount], target, lerpAmount);
            }

            validCount++;
        }

        if (validCount == 0)
        {
            HandVisible = false;
            HandCount = 0;
            ClearHandSlots();
            return;
        }

        for (int i = validCount; i < handSlotInitialized.Length; i++)
        {
            handSlotInitialized[i] = false;
        }

        HandCount = validCount;
        NormalizedPosition = normalizedPositions[0];
        HandVisible = validCount > 0;
        lastSeenTime = Time.time;
    }

    public int CopyNormalizedPositions(Vector2[] destination)
    {
        if (destination == null || !HandVisible)
        {
            return 0;
        }

        int count = Mathf.Min(HandCount, destination.Length, normalizedPositions.Length);
        for (int i = 0; i < count; i++)
        {
            destination[i] = normalizedPositions[i];
        }

        return count;
    }

    private void ClearHandSlots()
    {
        for (int i = 0; i < handSlotInitialized.Length; i++)
        {
            handSlotInitialized[i] = false;
        }
    }

    private static byte[] LoadHandLandmarkerModel()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string packageModelPath = Path.Combine(
            projectRoot,
            "Packages",
            "com.github.homuler.mediapipe",
            "PackageResources",
            "MediaPipe",
            "hand_landmarker.bytes");

        if (File.Exists(packageModelPath))
        {
            return File.ReadAllBytes(packageModelPath);
        }

        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "hand_landmarker.bytes");
        if (File.Exists(streamingAssetsPath))
        {
            return File.ReadAllBytes(streamingAssetsPath);
        }

        throw new FileNotFoundException("Could not find hand_landmarker.bytes for Display 2 MediaPipe tracking.");
    }

    private void StopTracker()
    {
        if (runCoroutine != null)
        {
            StopCoroutine(runCoroutine);
            runCoroutine = null;
        }

        stopwatch.Stop();
        textureFramePool?.Dispose();
        textureFramePool = null;

        handLandmarker?.Close();
        handLandmarker = null;

        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
            Destroy(webCamTexture);
            webCamTexture = null;
        }

        HandVisible = false;
        HandCount = 0;
        ClearHandSlots();
    }
}
