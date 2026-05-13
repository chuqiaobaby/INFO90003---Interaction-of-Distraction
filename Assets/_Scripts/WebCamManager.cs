using UnityEngine;

public class WebCamManager : MonoBehaviour
{
    [Header("把 BrokenMirror Quad 拖到这里（替换原来的 WebCamBackground）")]
    public MeshRenderer backgroundQuad;

    private WebCamTexture webcamTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            webcamTexture = new WebCamTexture(devices[0].name);
            // BrokenMirror shader 使用 _WebcamTex，不再用 _BaseMap
            backgroundQuad.material.SetTexture("_WebcamTex", webcamTexture);
            webcamTexture.Play();
            Debug.Log("[WebCamManager] Camera connected: " + devices[0].name);
        }
        else
        {
            Debug.LogError("[WebCamManager] No camera found!");
        }
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
}
