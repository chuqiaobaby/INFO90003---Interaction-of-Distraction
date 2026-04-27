using UnityEngine;
using UnityEngine.UI;

public class WebCamManager : MonoBehaviour
{
    [Header("Link to UserImage")]
    public RawImage displayImage;

    private WebCamTexture webcamTexture;

    void Start()
    {
        // Access Web Cam
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            // Link to the No.1 Camera by default
            webcamTexture = new WebCamTexture(devices[0].name);
            displayImage.texture = webcamTexture;
            webcamTexture.Play();
            Debug.Log("Camera Connect Successfuly��" + devices[0].name);
        }
        else
        {
            Debug.LogError("Didn't find any camera!");
        }
    }

    void OnDestroy()
    {
        // Turn off camera if exit
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
