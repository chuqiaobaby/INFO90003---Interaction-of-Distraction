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
    [SerializeField] private string preferredCameraNameContains = "";
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;

    [Header("Hand Position")]
    [Tooltip("MediaPipe hand landmark index. 8 = index fingertip, 9 = palm/middle base area.")]
    [SerializeField, Range(0, 20)] private int landmarkIndex = 8;
    [SerializeField] private bool rotatePosition180 = true;
    [SerializeField] private bool flipX;
    [SerializeField] private bool flipY = true;
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.18f;
    [SerializeField] private float lostTimeout = 0.35f;

    [Header("Detection")]
    [SerializeField, Range(1, 2)] private int maxHands = 1;
    [SerializeField, Range(0f, 1f)] private float minHandDetectionConfidence = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minHandPresenceConfidence = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minTrackingConfidence = 0.5f;

    public Vector2 NormalizedPosition { get; private set; } = new Vector2(0.5f, 0.5f);
    public bool HandVisible { get; private set; }
    public string ActiveCameraName { get; private set; } = "";
    public Texture CurrentCameraTexture => webCamTexture;
    public bool RotatePosition180 => rotatePosition180;
    public bool FlipX => flipX;
    public bool FlipY => flipY;

    private WebCamTexture webCamTexture;
    private TextureFramePool textureFramePool;
    private HandLandmarker handLandmarker;
    private HandLandmarkerResult handResult;
    private Coroutine runCoroutine;
    private float lastSeenTime = -1000f;
    private readonly Stopwatch stopwatch = new Stopwatch();

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
            runCoroutine = StartCoroutine(Run());
        }
    }

    private void Update()
    {
        if (HandVisible && Time.time - lastSeenTime > lostTimeout)
        {
            HandVisible = false;
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

        int selectedIndex = SelectCameraIndex(devices);
        ActiveCameraName = devices[selectedIndex].name;
        webCamTexture = new WebCamTexture(ActiveCameraName, requestedWidth, requestedHeight, requestedFps);
        webCamTexture.Play();
        UnityEngine.Debug.Log($"[Display2MediaPipe] Camera connected: {ActiveCameraName}");

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

        for (int i = 0; i < devices.Length; i++)
        {
            if (!LooksLikeBuiltInCamera(devices[i].name))
            {
                return i;
            }
        }

        return devices.Length > 1 ? 1 : 0;
    }

    private static bool LooksLikeBuiltInCamera(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return false;
        }

        return deviceName.IndexOf("MacBook", StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("FaceTime", StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("Built-in", StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("Continuity", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void UpdatePositionFromResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
        {
            HandVisible = false;
            return;
        }

        NormalizedLandmarks hand = result.handLandmarks[0];
        if (hand.landmarks == null || hand.landmarks.Count <= landmarkIndex)
        {
            HandVisible = false;
            return;
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
        NormalizedPosition = Vector2.Lerp(NormalizedPosition, target, lerpAmount);
        HandVisible = true;
        lastSeenTime = Time.time;
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
    }
}
