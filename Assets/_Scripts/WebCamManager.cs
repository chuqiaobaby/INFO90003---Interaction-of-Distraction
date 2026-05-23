using System.Collections;
using UnityEngine;

public class WebCamManager : MonoBehaviour
{
    [Header("把 BrokenMirror Quad 拖到这里（替换原来的 WebCamBackground）")]
    public MeshRenderer backgroundQuad;

    [Header("Display 1 Camera")]
    [Tooltip("Optional manual override. Leave empty to automatically choose an external camera.")]
    [SerializeField] private string preferredCameraNameContains = "Logitech MeetUp";
    [Tooltip("-1 = automatically choose by external camera order. Use a fixed index only for manual override.")]
    [SerializeField] private int fallbackCameraDeviceIndex = -1;
    [Tooltip("0 = first external camera, 1 = second external camera. Used only if Preferred Camera Name does not match.")]
    [SerializeField] private int externalCameraOrdinal = 0;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFps = 30;
    [Tooltip("Flip Display 1 vertically. Use this when the physical camera view is upside down.")]
    [SerializeField] private bool forceFlipY = true;

    private WebCamTexture webcamTexture;
    private int lastScreenW = -1;
    private int lastScreenH = -1;

    IEnumerator Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                string cameraType = GetCameraTypeLabel(devices[i].name);
                Debug.Log($"[WebCamManager] Camera device {i}: {devices[i].name} ({cameraType})");
            }

            int selectedIndex = SelectCameraIndex(devices);
            webcamTexture = new WebCamTexture(devices[selectedIndex].name, requestedWidth, requestedHeight, requestedFps);
            backgroundQuad.material.SetTexture("_WebcamTex", webcamTexture);
            backgroundQuad.material.SetFloat("_FlipX", 0f);
            webcamTexture.Play();
            Debug.Log($"[WebCamManager] Camera connected: {devices[selectedIndex].name} (index {selectedIndex})");

            yield return null; // wait one frame for videoVerticallyMirrored and real resolution
            float flipY = webcamTexture.videoVerticallyMirrored ? 1f : 0f;
            if (forceFlipY)
                flipY = 1f - flipY;
            backgroundQuad.material.SetFloat("_FlipY", flipY);
            Debug.Log($"[WebCamManager] videoVerticallyMirrored={webcamTexture.videoVerticallyMirrored}, forceFlipY={forceFlipY} → _FlipY={flipY}");

            ApplyAspectCorrection();
        }
        else
        {
            Debug.LogError("[WebCamManager] No camera found!");
        }
    }

    private int SelectCameraIndex(WebCamDevice[] devices)
    {
        if (!string.IsNullOrWhiteSpace(preferredCameraNameContains))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name.IndexOf(preferredCameraNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
        }

        if (fallbackCameraDeviceIndex >= 0 && fallbackCameraDeviceIndex < devices.Length)
            return fallbackCameraDeviceIndex;

        int externalSeen = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            if (LooksLikeBuiltInCamera(devices[i].name) || LooksLikeContinuityCamera(devices[i].name))
                continue;

            if (externalSeen == Mathf.Max(0, externalCameraOrdinal))
                return i;

            externalSeen++;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (!LooksLikeBuiltInCamera(devices[i].name) && !LooksLikeContinuityCamera(devices[i].name))
                return i;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (LooksLikeContinuityCamera(devices[i].name))
                return i;
        }

        return devices.Length > 1 ? 1 : 0;
    }

    private static bool LooksLikeBuiltInCamera(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        return deviceName.IndexOf("MacBook", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("FaceTime", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("Built-in", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeContinuityCamera(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        return deviceName.IndexOf("Continuity", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            deviceName.IndexOf("iPhone", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetCameraTypeLabel(string deviceName)
    {
        if (LooksLikeBuiltInCamera(deviceName))
            return "built-in";

        if (LooksLikeContinuityCamera(deviceName))
            return "continuity/iPhone";

        return "external";
    }

    void Update()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying) return;
        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
            ApplyAspectCorrection();
    }

    void ApplyAspectCorrection()
    {
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;

        // webcam.width can be tiny before the first real frame arrives
        if (webcamTexture == null || webcamTexture.width < 64) return;

        float screenAspect = (float)Screen.width / Screen.height;
        float webcamAspect = (float)webcamTexture.width / webcamTexture.height;

        // Center-crop (aspect fill): no black bars, crops the narrower dimension
        float corrX = 1f, corrY = 1f;
        if (screenAspect >= webcamAspect)
            corrY = webcamAspect / screenAspect; // screen wider → crop webcam height
        else
            corrX = screenAspect / webcamAspect; // screen taller → crop webcam width

        backgroundQuad.material.SetVector("_AspectRatioCorrection", new Vector4(corrX, corrY, 0f, 0f));

        Debug.Log($"[WebCamManager] Screen {Screen.width}×{Screen.height} ({screenAspect:F2}) | " +
                  $"Webcam {webcamTexture.width}×{webcamTexture.height} ({webcamAspect:F2}) | " +
                  $"UV correction ({corrX:F3}, {corrY:F3})");
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
}
