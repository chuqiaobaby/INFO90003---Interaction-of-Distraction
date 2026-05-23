using UnityEngine;

// Attach to the background Quad. Scales it to exactly fill the camera view at runtime,
// so it works for any screen size or orientation (landscape / portrait / external display).
// The Quad must be a direct child of the camera, at localPosition (0, 0, depth).
[ExecuteAlways]
public class ScreenFillQuad : MonoBehaviour
{
    [Tooltip("The camera rendering this quad. Leave empty to use Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Distance in front of the camera in world units (must exceed near clip plane).")]
    [Min(0.1f)]
    public float depth = 10f;

    private int lastW, lastH;
    private float lastFov, lastOrthoSize;

    void OnEnable() => Fit();

    void Update()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        bool dirty = Screen.width != lastW || Screen.height != lastH;
        if (targetCamera.orthographic)
            dirty |= targetCamera.orthographicSize != lastOrthoSize;
        else
            dirty |= targetCamera.fieldOfView != lastFov;

        if (dirty) Fit();
    }

    void Fit()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        lastW          = Screen.width;
        lastH          = Screen.height;
        lastFov        = targetCamera.fieldOfView;
        lastOrthoSize  = targetCamera.orthographicSize;

        if (Screen.height == 0) return;
        float aspect = (float)Screen.width / Screen.height;
        float halfH  = targetCamera.orthographic
            ? targetCamera.orthographicSize
            : depth * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfW = halfH * aspect;

        transform.localScale = new Vector3(halfW * 2f, halfH * 2f, 1f);

        // Keep quad centred in front of the camera
        transform.localPosition = new Vector3(0f, 0f, depth);
        transform.localRotation = Quaternion.identity;
    }
}
