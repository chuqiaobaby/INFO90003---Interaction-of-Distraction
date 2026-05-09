using UnityEngine;

public class WebCamManager : MonoBehaviour
{
    [Header("把刚刚创建的 WebCamBackground 拖到这里")]
    public MeshRenderer backgroundQuad;

    private WebCamTexture webcamTexture;
    private bool aspectFixed = false;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            webcamTexture = new WebCamTexture(devices[0].name);
            backgroundQuad.material.SetTexture("_BaseMap", webcamTexture);
            webcamTexture.Play();
            Debug.Log("Camera connected: " + devices[0].name);
        }
        else
        {
            Debug.LogError("No camera found!");
        }
    }

    void Update()
    {
        // webcamTexture 刚启动时会报告 16x16，等第一帧真实画面到来再修正
        if (!aspectFixed && webcamTexture != null && webcamTexture.width > 16)
        {
            FixAspectAndMirror();
            aspectFixed = true;
        }
    }

    void FixAspectAndMirror()
    {
        float camAspect    = (float)webcamTexture.width / webcamTexture.height;
        float screenAspect = (float)Screen.width / Screen.height;

        float scaleX, scaleY, offsetX, offsetY;

        if (camAspect > screenAspect)
        {
            // 摄像头比屏幕宽：高度撑满，左右裁切
            scaleX  = screenAspect / camAspect;
            scaleY  = 1f;
            offsetY = 0f;
        }
        else
        {
            // 摄像头比屏幕高：宽度撑满，上下裁切
            scaleX  = 1f;
            scaleY  = camAspect / screenAspect;
            offsetY = (1f - scaleY) / 2f;
        }

        // 镜像：把 X 的 UV scale 取负，offset 相应补偿到右边缘
        float cropOffsetX = (1f - scaleX) / 2f;
        offsetX = cropOffsetX + scaleX; // 负向 scale 时 offset 指向裁切区右端

        backgroundQuad.material.SetTextureScale("_BaseMap",  new Vector2(-scaleX, scaleY));
        backgroundQuad.material.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
}
