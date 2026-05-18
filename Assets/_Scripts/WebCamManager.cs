using System.Collections;
using UnityEngine;

public class WebCamManager : MonoBehaviour
{
    [Header("把 BrokenMirror Quad 拖到这里（替换原来的 WebCamBackground）")]
    public MeshRenderer backgroundQuad;

    [Header("Display 1 Camera")]
    [SerializeField] private string preferredCameraNameContains = "MacBook";
    [SerializeField] private int fallbackCameraDeviceIndex = 0;

    private WebCamTexture webcamTexture;
    private int lastScreenW = -1;
    private int lastScreenH = -1;

    IEnumerator Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            int selectedIndex = SelectCameraIndex(devices);
            webcamTexture = new WebCamTexture(devices[selectedIndex].name);
            backgroundQuad.material.SetTexture("_WebcamTex", webcamTexture);
            backgroundQuad.material.SetFloat("_FlipX", 0f);
            webcamTexture.Play();
            Debug.Log("[WebCamManager] Camera connected: " + devices[selectedIndex].name);

            yield return null; // wait one frame for videoVerticallyMirrored and real resolution
            float flipY = webcamTexture.videoVerticallyMirrored ? 1f : 0f;
            backgroundQuad.material.SetFloat("_FlipY", flipY);
            Debug.Log($"[WebCamManager] videoVerticallyMirrored={webcamTexture.videoVerticallyMirrored} → _FlipY={flipY}");

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

        return 0;
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
