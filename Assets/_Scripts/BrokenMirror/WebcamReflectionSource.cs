using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WebcamReflectionSource : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string textureProperty      = "_WebcamTex";
    [SerializeField] private string preferredDeviceName  = "";
    [SerializeField] private int    requestedWidth       = 1280;
    [SerializeField] private int    requestedHeight      = 720;
    [SerializeField] private int    requestedFps         = 30;

    private WebCamTexture webcamTexture;
    private Texture2D     fallbackTexture;
    private Material      runtimeMaterial;
    private int           texturePropertyId;

    private static readonly int s_AspectCorrId = Shader.PropertyToID("_AspectRatioCorrection");
    private int _lastScreenW, _lastScreenH, _lastCamW, _lastCamH;

    public bool IsUsingWebcam => webcamTexture != null && webcamTexture.isPlaying;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        texturePropertyId = Shader.PropertyToID(textureProperty);
        runtimeMaterial   = targetRenderer != null ? targetRenderer.material : null;
        ApplyFallbackTexture();
    }

    private IEnumerator Start()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogWarning("[BrokenMirror] Webcam permission denied — using fallback texture.");
            yield break;
        }

        StartWebcam();
    }

    private void StartWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning("[BrokenMirror] No webcam found — using fallback texture.");
            return;
        }

        string selectedDevice = devices[0].name;
        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            foreach (WebCamDevice device in devices)
            {
                if (device.name.Contains(preferredDeviceName))
                {
                    selectedDevice = device.name;
                    break;
                }
            }
        }

        webcamTexture = new WebCamTexture(selectedDevice, requestedWidth, requestedHeight, requestedFps);
        webcamTexture.Play();

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetTexture(texturePropertyId, webcamTexture);
            runtimeMaterial.SetFloat("_FlipX", 0f); // no horizontal mirror on any platform
            StartCoroutine(ApplyPlatformFlipAfterStart());
        }
    }

    // WebCamTexture reports correct videoVerticallyMirrored only after the first frame.
    private System.Collections.IEnumerator ApplyPlatformFlipAfterStart()
    {
        yield return null; // wait one frame for the webcam to initialise
        if (webcamTexture == null) yield break;

        // macOS returns frames vertically mirrored; correct it via the shader _FlipY property.
        float flipY = webcamTexture.videoVerticallyMirrored ? 1f : 0f;
        runtimeMaterial.SetFloat("_FlipY", flipY);
        Debug.Log($"[BrokenMirror] videoVerticallyMirrored={webcamTexture.videoVerticallyMirrored} → _FlipY={flipY}");
    }

    private void ApplyFallbackTexture()
    {
        fallbackTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true)
        {
            name      = "BrokenMirror Fallback",
            wrapMode  = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < fallbackTexture.height; y++)
        {
            for (int x = 0; x < fallbackTexture.width; x++)
            {
                float u    = x / (float)(fallbackTexture.width  - 1);
                float v    = y / (float)(fallbackTexture.height - 1);
                float band = Mathf.Sin((u + v) * 18f) * 0.035f;
                fallbackTexture.SetPixel(x, y, new Color(0.07f + u * 0.06f + band, 0.09f + v * 0.08f, 0.12f + band, 1f));
            }
        }

        fallbackTexture.Apply(false, true);

        if (runtimeMaterial != null)
            runtimeMaterial.SetTexture(texturePropertyId, fallbackTexture);
    }

    private void Update()
    {
        if (runtimeMaterial == null) return;

        // WebCamTexture reports 16×16 before the first real frame arrives — ignore that.
        int cw = (webcamTexture != null && webcamTexture.isPlaying && webcamTexture.width > 16)
                 ? webcamTexture.width : 0;
        int ch = (webcamTexture != null && webcamTexture.isPlaying && webcamTexture.height > 16)
                 ? webcamTexture.height : 0;

        int sw = Screen.width;
        int sh = Screen.height;

        if (sw == _lastScreenW && sh == _lastScreenH && cw == _lastCamW && ch == _lastCamH) return;
        _lastScreenW = sw; _lastScreenH = sh; _lastCamW = cw; _lastCamH = ch;

        float corrX = 1f, corrY = 1f;
        if (sh > 0 && cw > 0 && ch > 0)
        {
            // Center-crop: scale webcam to fill screen, crop the longer dimension.
            float screenAspect = (float)sw / sh;
            float camAspect    = (float)cw / ch;
            corrX = Mathf.Min(1f, screenAspect / camAspect);
            corrY = Mathf.Min(1f, camAspect    / screenAspect);
        }

        runtimeMaterial.SetVector(s_AspectCorrId, new Vector4(corrX, corrY, 0f, 0f));
    }

    private void OnDestroy()
    {
        if (webcamTexture != null) { webcamTexture.Stop(); Destroy(webcamTexture); }
        if (fallbackTexture != null) Destroy(fallbackTexture);
    }
}
