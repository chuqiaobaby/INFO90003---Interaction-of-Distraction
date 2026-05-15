using System.Collections;
using UnityEngine;

public class WebCamManager : MonoBehaviour
{
    [Header("把 BrokenMirror Quad 拖到这里（替换原来的 WebCamBackground）")]
    public MeshRenderer backgroundQuad;

    private WebCamTexture webcamTexture;

    IEnumerator Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            webcamTexture = new WebCamTexture(devices[0].name);
            backgroundQuad.material.SetTexture("_WebcamTex", webcamTexture);
            backgroundQuad.material.SetFloat("_FlipX", 0f); // no horizontal mirror on any platform
            webcamTexture.Play();
            Debug.Log("[WebCamManager] Camera connected: " + devices[0].name);

            yield return null; // wait one frame for videoVerticallyMirrored to be valid
            float flipY = webcamTexture.videoVerticallyMirrored ? 1f : 0f;
            backgroundQuad.material.SetFloat("_FlipY", flipY);
            Debug.Log($"[WebCamManager] videoVerticallyMirrored={webcamTexture.videoVerticallyMirrored} → _FlipY={flipY}");
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
