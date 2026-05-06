using UnityEngine;

public class WebCamManager : MonoBehaviour
{
    [Header("把刚刚创建的 WebCamBackground 拖到这里")]
    public MeshRenderer backgroundQuad;

    private WebCamTexture webcamTexture;

    void Start()
    {
        // 获取摄像头设备
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            // 默认连接第一个摄像头
            webcamTexture = new WebCamTexture(devices[0].name);
            
            // 核心修改：把摄像头的画面赋值给 3D Quad 材质的主贴图
            // 【核心修复】URP 材质必须通过 "_BaseMap" 接收贴图
            backgroundQuad.material.SetTexture("_BaseMap", webcamTexture);

            // 顺手补一句传统的，防止以后材质切回老管线
            backgroundQuad.material.mainTexture = webcamTexture;
            
            webcamTexture.Play();
            Debug.Log("Camera Connect Successfuly：" + devices[0].name);
        }
        else
        {
            Debug.LogError("Didn't find any camera!");
        }
    }

    void OnDestroy()
    {
        // 退出时关闭摄像头
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
