using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class PastelClassicRippleController : MonoBehaviour
{
    public Camera targetCamera;
    public KeyCode triggerKey = KeyCode.Space;
    public Color backgroundColor = new Color(1f, 0.78f, 0.82f, 1f);
    [Range(0f, 2f)] public float strokeOpacity = 0.96f;
    [Range(0.2f, 4f)] public float duration = 2.2f;
    [Range(0.1f, 1.5f)] public float maxRadius = 0.86f;
    [Range(0f, 1f)] public float grainStrength = 0.045f;
    public float seed = 90003f;
    public float cameraHeight = 5f;
    public float projectionHeight = 0f;

    private static readonly int BackgroundColorId = Shader.PropertyToID("_BackgroundColor");
    private static readonly int StrokeOpacityId = Shader.PropertyToID("_StrokeOpacity");
    private static readonly int DurationId = Shader.PropertyToID("_Duration");
    private static readonly int MaxRadiusId = Shader.PropertyToID("_MaxRadius");
    private static readonly int TriggerTimeId = Shader.PropertyToID("_TriggerTime");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int GrainStrengthId = Shader.PropertyToID("_GrainStrength");

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material materialInstance;

    private void OnEnable()
    {
        EnsureSetup();
        ApplyProperties();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureSetup();
        ApplyProperties();
    }

    private void Update()
    {
        EnsureSetup();
        ConfigureCameraAndPlane();
        ApplyProperties();

        if (Application.isPlaying && Input.GetKeyDown(triggerKey))
        {
            TriggerRipple();
        }
    }

    [ContextMenu("Trigger Ripple")]
    public void TriggerRipple()
    {
        EnsureSetup();
        materialInstance.SetFloat(TriggerTimeId, Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
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
            Shader shader = Shader.Find("INFO90003/Pastel Classic Ripple HLSL");
            materialInstance = new Material(shader)
            {
                name = "Pastel Classic Ripple HLSL Material"
            };
            materialInstance.SetFloat(TriggerTimeId, -1000f);
        }

        meshRenderer.sharedMaterial = materialInstance;
        ConfigureCameraAndPlane();
    }

    private void ConfigureCameraAndPlane()
    {
        Camera cameraToFit = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToFit == null)
        {
            return;
        }

        cameraToFit.orthographic = true;
        cameraToFit.transform.SetPositionAndRotation(
            new Vector3(0f, cameraHeight, 0f),
            Quaternion.Euler(90f, 0f, 0f));

        float height = cameraToFit.orthographicSize * 2f;
        float width = height * cameraToFit.aspect;
        transform.SetPositionAndRotation(
            new Vector3(0f, projectionHeight, 0f),
            Quaternion.identity);
        transform.localScale = new Vector3(width, 1f, height);
    }

    private void ApplyProperties()
    {
        if (materialInstance == null)
        {
            return;
        }

        materialInstance.SetColor(BackgroundColorId, backgroundColor);
        materialInstance.SetFloat(StrokeOpacityId, strokeOpacity);
        materialInstance.SetFloat(DurationId, duration);
        materialInstance.SetFloat(MaxRadiusId, maxRadius);
        materialInstance.SetFloat(SeedId, seed);
        materialInstance.SetFloat(GrainStrengthId, grainStrength);
    }

    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh
        {
            name = "Top Down Ripple Quad"
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
