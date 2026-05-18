using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class Display2CameraDebugBackground : MonoBehaviour
{
    public Camera targetCamera;
    public Display2MediaPipeHandTracker handTracker;
    public Color fallbackColor = Color.black;
    public float cameraHeight = 5.05f;
    public float projectionHeight = -0.01f;
    public bool matchHandTrackerOrientation = true;
    public bool flipX;
    public bool flipY;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material materialInstance;
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void Update()
    {
        EnsureSetup();
        ConfigureCameraAndPlane();
        UpdateTexture();
    }

    private void EnsureSetup()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = CreateQuadMesh();
        }

        if (materialInstance == null)
        {
            materialInstance = new Material(Shader.Find("Unlit/Texture"))
            {
                name = "Display 2 Camera Debug Background"
            };
        }

        meshRenderer.sharedMaterial = materialInstance;
    }

    private void ConfigureCameraAndPlane()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = true;
        targetCamera.transform.SetPositionAndRotation(
            new Vector3(0f, cameraHeight, 0f),
            Quaternion.Euler(90f, 0f, 0f));

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        transform.SetPositionAndRotation(
            new Vector3(0f, projectionHeight, 0f),
            Quaternion.identity);
        transform.localScale = new Vector3(width, 1f, height);
    }

    private void UpdateTexture()
    {
        if (materialInstance == null)
        {
            return;
        }

        materialInstance.SetColor(ColorId, fallbackColor);

        Texture cameraTexture = handTracker != null ? handTracker.CurrentCameraTexture : null;
        materialInstance.SetTexture(MainTexId, cameraTexture != null ? cameraTexture : Texture2D.blackTexture);

        bool effectiveFlipX = flipX;
        bool effectiveFlipY = flipY;

        if (matchHandTrackerOrientation && handTracker != null)
        {
            effectiveFlipX ^= handTracker.FlipX;
            effectiveFlipY ^= handTracker.FlipY;

            if (handTracker.RotatePosition180)
            {
                effectiveFlipX = !effectiveFlipX;
                effectiveFlipY = !effectiveFlipY;
            }
        }

        Vector2 scale = new Vector2(effectiveFlipX ? -1f : 1f, effectiveFlipY ? -1f : 1f);
        Vector2 offset = new Vector2(effectiveFlipX ? 1f : 0f, effectiveFlipY ? 1f : 0f);
        materialInstance.mainTextureScale = scale;
        materialInstance.mainTextureOffset = offset;
    }

    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh
        {
            name = "Display 2 Camera Debug Background Quad"
        };

        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        if (materialInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(materialInstance);
        }
        else
        {
            DestroyImmediate(materialInstance);
        }
    }
}
