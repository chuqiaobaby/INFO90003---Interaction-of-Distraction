using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class PastelClassicRippleController : MonoBehaviour
{
    private const int MaxInteractiveHands = 4;
    private const int MaxTrailSamples = 16;
    private const int MaxInkPaletteColors = 8;
    private const float TrailSampleInterval = 0.012f;
    private const float TrailSampleMinDistance = 0.002f;

    public Camera targetCamera;
    public KeyCode triggerKey = KeyCode.Space;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [Range(0f, 3f)] public float strokeOpacity = 2.2f;
    [Range(0.2f, 5f)] public float duration = 2.8f;
    [Range(0.1f, 2.2f)] public float maxRadius = 1.25f;
    [Range(0f, 1f)] public float grainStrength = 0.18f;
    [Header("Cinematic Particle Field")]
    [Range(0f, 4f)] public float flowSpeed = 0.72f;
    [Range(0.2f, 4f)] public float trailLength = 3.0f;
    [Range(0f, 12f)] public float bloomBoost = 6.5f;
    [Range(0f, 4f)] public float handInfluence = 1.6f;
    [Range(0.4f, 3f)] public float particleDensity = 1.95f;
    [Range(0.25f, 1.5f)] public float interactionRadius = 0.62f;
    [Range(0f, 4f)] public float continuousTrailStrength = 0.8f;
    [Header("Ribbon Cursor Feel")]
    public bool rainbowMode = true;
    [Range(0.02f, 0.35f)] public float cursorLagSeconds = 0.16f;
    [Range(0.02f, 0.35f)] public float cursorVelocityLagSeconds = 0.12f;
    [Range(0f, 0.08f)] public float trailSubsampleDistance = 0.016f;
    [Header("Touch Ink")]
    public Color[] inkPalette =
    {
        new Color(0.08f, 1.00f, 0.45f, 1f),
        new Color(0.36f, 1.00f, 0.08f, 1f),
        new Color(0.95f, 0.95f, 0.05f, 1f),
        new Color(0.00f, 0.92f, 1.00f, 1f),
        new Color(1.00f, 0.42f, 0.18f, 1f)
    };
    [Min(0.1f)] public float inkColorCycleSeconds = 2f;
    [Range(0.5f, 4f)] public float inkFadeSeconds = 2f;
    [Range(0f, 4f)] public float inkSplatForce = 1.65f;
    public Vector2 inkAngularVelocityRange = new Vector2(-1.15f, 1.45f);
    [Range(0f, 1.5f)] public float inkRadiusDrift = 0.42f;
    [Range(0f, 2f)] public float inkSpeedDrift = 0.35f;
    [Range(0f, 3f)] public float inkChaos = 0.8f;
    [Range(0f, 3f)] public float inkDiffusion = 0.45f;
    [Range(0.2f, 4f)] public float inkBrightness = 1.55f;
    [Range(0.2f, 4f)] public float inkSoftness = 1.7f;
    [Range(0f, 3f)] public float inkNoiseStrength = 0.7f;
    [Range(0.5f, 12f)] public float inkNoiseScale = 4.8f;
    [Range(0.2f, 5f)] public float inkTrailStretch = 2.7f;
    [Range(0f, 2f)] public float inkWhiteCore = 0.42f;
    [Range(0.3f, 3f)] public float inkBurstSize = 1.15f;
    public float seed = 90003f;
    public float cameraHeight = 5f;
    public float projectionHeight = 0f;

    private static readonly int BackgroundColorId = Shader.PropertyToID("_BackgroundColor");
    private static readonly int StrokeOpacityId = Shader.PropertyToID("_StrokeOpacity");
    private static readonly int DurationId = Shader.PropertyToID("_Duration");
    private static readonly int MaxRadiusId = Shader.PropertyToID("_MaxRadius");
    private static readonly int TriggerTimeId = Shader.PropertyToID("_TriggerTime");
    private static readonly int RippleCenterId = Shader.PropertyToID("_RippleCenter");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int GrainStrengthId = Shader.PropertyToID("_GrainStrength");
    private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
    private static readonly int TrailLengthId = Shader.PropertyToID("_TrailLength");
    private static readonly int BloomBoostId = Shader.PropertyToID("_BloomBoost");
    private static readonly int HandInfluenceId = Shader.PropertyToID("_HandInfluence");
    private static readonly int ParticleDensityId = Shader.PropertyToID("_ParticleDensity");
    private static readonly int InteractionRadiusId = Shader.PropertyToID("_InteractionRadius");
    private static readonly int ContinuousTrailStrengthId = Shader.PropertyToID("_ContinuousTrailStrength");
    private static readonly int RainbowModeId = Shader.PropertyToID("_RainbowMode");
    private static readonly int InkPaletteCountId = Shader.PropertyToID("_InkPaletteCount");
    private static readonly int InkColorCycleSecondsId = Shader.PropertyToID("_InkColorCycleSeconds");
    private static readonly int InkFadeSecondsId = Shader.PropertyToID("_InkFadeSeconds");
    private static readonly int InkSplatForceId = Shader.PropertyToID("_InkSplatForce");
    private static readonly int InkAngularVelocityRangeId = Shader.PropertyToID("_InkAngularVelocityRange");
    private static readonly int InkRadiusDriftId = Shader.PropertyToID("_InkRadiusDrift");
    private static readonly int InkSpeedDriftId = Shader.PropertyToID("_InkSpeedDrift");
    private static readonly int InkChaosId = Shader.PropertyToID("_InkChaos");
    private static readonly int InkDiffusionId = Shader.PropertyToID("_InkDiffusion");
    private static readonly int InkBrightnessId = Shader.PropertyToID("_InkBrightness");
    private static readonly int InkSoftnessId = Shader.PropertyToID("_InkSoftness");
    private static readonly int InkNoiseStrengthId = Shader.PropertyToID("_InkNoiseStrength");
    private static readonly int InkNoiseScaleId = Shader.PropertyToID("_InkNoiseScale");
    private static readonly int InkTrailStretchId = Shader.PropertyToID("_InkTrailStretch");
    private static readonly int InkWhiteCoreId = Shader.PropertyToID("_InkWhiteCore");
    private static readonly int InkBurstSizeId = Shader.PropertyToID("_InkBurstSize");

    private static readonly int[] InkColorIds =
    {
        Shader.PropertyToID("_InkColor0"),
        Shader.PropertyToID("_InkColor1"),
        Shader.PropertyToID("_InkColor2"),
        Shader.PropertyToID("_InkColor3"),
        Shader.PropertyToID("_InkColor4"),
        Shader.PropertyToID("_InkColor5"),
        Shader.PropertyToID("_InkColor6"),
        Shader.PropertyToID("_InkColor7")
    };

    private static readonly int[] HandIds =
    {
        Shader.PropertyToID("_Hand0"),
        Shader.PropertyToID("_Hand1"),
        Shader.PropertyToID("_Hand2"),
        Shader.PropertyToID("_Hand3")
    };

    private static readonly int[] VelocityIds =
    {
        Shader.PropertyToID("_Velocity0"),
        Shader.PropertyToID("_Velocity1"),
        Shader.PropertyToID("_Velocity2"),
        Shader.PropertyToID("_Velocity3")
    };

    private static readonly int[] BurstIds =
    {
        Shader.PropertyToID("_Burst0"),
        Shader.PropertyToID("_Burst1"),
        Shader.PropertyToID("_Burst2"),
        Shader.PropertyToID("_Burst3")
    };

    private static readonly int[] TrailIds =
    {
        Shader.PropertyToID("_Trail0"),
        Shader.PropertyToID("_Trail1"),
        Shader.PropertyToID("_Trail2"),
        Shader.PropertyToID("_Trail3"),
        Shader.PropertyToID("_Trail4"),
        Shader.PropertyToID("_Trail5"),
        Shader.PropertyToID("_Trail6"),
        Shader.PropertyToID("_Trail7"),
        Shader.PropertyToID("_Trail8"),
        Shader.PropertyToID("_Trail9"),
        Shader.PropertyToID("_Trail10"),
        Shader.PropertyToID("_Trail11"),
        Shader.PropertyToID("_Trail12"),
        Shader.PropertyToID("_Trail13"),
        Shader.PropertyToID("_Trail14"),
        Shader.PropertyToID("_Trail15")
    };

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material materialInstance;
    private readonly Vector2[] handPositions = new Vector2[MaxInteractiveHands];
    private readonly Vector2[] smoothedHandPositions = new Vector2[MaxInteractiveHands];
    private readonly Vector2[] previousHandPositions = new Vector2[MaxInteractiveHands];
    private readonly Vector2[] handVelocities = new Vector2[MaxInteractiveHands];
    private readonly bool[] handWasActive = new bool[MaxInteractiveHands];
    private readonly float[] burstTimes = new float[MaxInteractiveHands];
    private readonly bool[] burstIsActive = new bool[MaxInteractiveHands];
    private readonly Vector2[] trailPositions = new Vector2[MaxTrailSamples];
    private readonly float[] trailTimes = new float[MaxTrailSamples];
    private readonly float[] trailStrengths = new float[MaxTrailSamples];
    private readonly Vector2[] lastTrailSamplePositions = new Vector2[MaxInteractiveHands];
    private readonly float[] lastTrailSampleTimes = new float[MaxInteractiveHands];
    private int activeHandCount;
    private int nextBurstSlot;
    private int nextTrailSlot;

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
        TriggerRipple(new Vector2(0.5f, 0.5f));
    }

    public void TriggerRipple(Vector2 normalizedPosition)
    {
        EnsureSetup();
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp01(normalizedPosition.x),
            Mathf.Clamp01(normalizedPosition.y));
        materialInstance.SetVector(RippleCenterId, clampedPosition);
        materialInstance.SetFloat(TriggerTimeId, Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
        SetBurst(nextBurstSlot, clampedPosition, Application.isPlaying ? Time.time : Time.realtimeSinceStartup, true);
        nextBurstSlot = (nextBurstSlot + 1) % MaxInteractiveHands;
    }

    public void SetInteractionHands(Vector2[] normalizedPositions, int count)
    {
        EnsureSetup();

        int clampedCount = Mathf.Clamp(count, 0, MaxInteractiveHands);
        float deltaTime = Mathf.Max(Time.deltaTime, 0.001f);
        activeHandCount = clampedCount;

        for (int i = 0; i < MaxInteractiveHands; i++)
        {
            bool active = i < clampedCount && normalizedPositions != null;
            Vector2 position = active
                ? new Vector2(Mathf.Clamp01(normalizedPositions[i].x), Mathf.Clamp01(normalizedPositions[i].y))
                : handPositions[i];

            if (active)
            {
                if (!handWasActive[i])
                {
                    smoothedHandPositions[i] = position;
                    previousHandPositions[i] = position;
                    handVelocities[i] = Vector2.zero;
                    SetBurst(i, position, Application.isPlaying ? Time.time : Time.realtimeSinceStartup, true);
                    AddTrailSample(i, position, 1f);
                }
                else
                {
                    Vector2 previousSmoothedPosition = smoothedHandPositions[i];
                    float positionBlend = 1f - Mathf.Exp(-deltaTime / Mathf.Max(cursorLagSeconds, 0.001f));
                    smoothedHandPositions[i] = Vector2.Lerp(smoothedHandPositions[i], position, positionBlend);

                    Vector2 targetVelocity = (smoothedHandPositions[i] - previousSmoothedPosition) / deltaTime;
                    float velocityBlend = 1f - Mathf.Exp(-deltaTime / Mathf.Max(cursorVelocityLagSeconds, 0.001f));
                    handVelocities[i] = Vector2.Lerp(handVelocities[i], targetVelocity, velocityBlend);
                    float velocityStrength = Mathf.Clamp01(handVelocities[i].magnitude * 1.7f);
                    AddTrailSamplesBetween(i, previousSmoothedPosition, smoothedHandPositions[i], Mathf.Lerp(0.45f, 1f, velocityStrength));
                }

                handPositions[i] = smoothedHandPositions[i];
                previousHandPositions[i] = smoothedHandPositions[i];
            }
            else
            {
                handVelocities[i] = Vector2.Lerp(handVelocities[i], Vector2.zero, 0.12f);
            }

            handWasActive[i] = active;
        }

        ApplyInteractionProperties();
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
            Shader shader = Shader.Find("INFO90003/Display 2 Cinematic Particle Field HLSL");
            if (shader == null)
            {
                shader = Shader.Find("INFO90003/Pastel Classic Ripple HLSL");
            }

            materialInstance = new Material(shader)
            {
                name = "Display 2 Cinematic Particle Field Material"
            };
            materialInstance.SetFloat(TriggerTimeId, -1000f);
            materialInstance.SetVector(RippleCenterId, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < MaxInteractiveHands; i++)
            {
                handPositions[i] = new Vector2(0.5f, 0.5f);
                smoothedHandPositions[i] = handPositions[i];
                previousHandPositions[i] = handPositions[i];
                burstTimes[i] = -1000f;
                lastTrailSampleTimes[i] = -1000f;
            }

            for (int i = 0; i < MaxTrailSamples; i++)
            {
                trailPositions[i] = new Vector2(0.5f, 0.5f);
                trailTimes[i] = -1000f;
                trailStrengths[i] = 0f;
            }
        }

        meshRenderer.sharedMaterial = materialInstance;
        ConfigureCameraAndPlane();
        ApplyInteractionProperties();
    }

    private void ConfigureCameraAndPlane()
    {
        Camera cameraToFit = targetCamera;
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
        materialInstance.SetFloat(FlowSpeedId, flowSpeed);
        materialInstance.SetFloat(TrailLengthId, trailLength);
        materialInstance.SetFloat(BloomBoostId, bloomBoost);
        materialInstance.SetFloat(HandInfluenceId, handInfluence);
        materialInstance.SetFloat(ParticleDensityId, particleDensity);
        materialInstance.SetFloat(InteractionRadiusId, interactionRadius);
        materialInstance.SetFloat(ContinuousTrailStrengthId, continuousTrailStrength);
        materialInstance.SetFloat(RainbowModeId, rainbowMode ? 1f : 0f);
        ApplyInkProperties();
        ApplyInteractionProperties();
    }

    private void ApplyInkProperties()
    {
        int paletteCount = inkPalette == null ? 0 : Mathf.Min(inkPalette.Length, MaxInkPaletteColors);
        materialInstance.SetFloat(InkPaletteCountId, Mathf.Max(1, paletteCount));
        materialInstance.SetFloat(InkColorCycleSecondsId, Mathf.Max(0.1f, inkColorCycleSeconds));
        materialInstance.SetFloat(InkFadeSecondsId, Mathf.Max(0.1f, inkFadeSeconds));
        materialInstance.SetFloat(InkSplatForceId, inkSplatForce);
        materialInstance.SetVector(InkAngularVelocityRangeId, inkAngularVelocityRange);
        materialInstance.SetFloat(InkRadiusDriftId, inkRadiusDrift);
        materialInstance.SetFloat(InkSpeedDriftId, inkSpeedDrift);
        materialInstance.SetFloat(InkChaosId, inkChaos);
        materialInstance.SetFloat(InkDiffusionId, inkDiffusion);
        materialInstance.SetFloat(InkBrightnessId, inkBrightness);
        materialInstance.SetFloat(InkSoftnessId, inkSoftness);
        materialInstance.SetFloat(InkNoiseStrengthId, inkNoiseStrength);
        materialInstance.SetFloat(InkNoiseScaleId, inkNoiseScale);
        materialInstance.SetFloat(InkTrailStretchId, inkTrailStretch);
        materialInstance.SetFloat(InkWhiteCoreId, inkWhiteCore);
        materialInstance.SetFloat(InkBurstSizeId, inkBurstSize);

        Color fallback = new Color(0.08f, 0.90f, 1.00f, 1f);
        for (int i = 0; i < MaxInkPaletteColors; i++)
        {
            Color color = i < paletteCount ? inkPalette[i] : fallback;
            materialInstance.SetColor(InkColorIds[i], color);
        }
    }

    private void ApplyInteractionProperties()
    {
        if (materialInstance == null)
        {
            return;
        }

        for (int i = 0; i < MaxInteractiveHands; i++)
        {
            float active = i < activeHandCount && handWasActive[i] ? 1f : 0f;
            materialInstance.SetVector(HandIds[i], new Vector4(handPositions[i].x, handPositions[i].y, active, 0f));
            materialInstance.SetVector(VelocityIds[i], new Vector4(handVelocities[i].x, handVelocities[i].y, 0f, 0f));
            materialInstance.SetVector(BurstIds[i], new Vector4(handPositions[i].x, handPositions[i].y, burstTimes[i], burstIsActive[i] ? 1f : 0f));
        }

        for (int i = 0; i < MaxTrailSamples; i++)
        {
            materialInstance.SetVector(TrailIds[i], new Vector4(trailPositions[i].x, trailPositions[i].y, trailTimes[i], trailStrengths[i]));
        }
    }

    private void SetBurst(int slot, Vector2 position, float time, bool active)
    {
        int safeSlot = Mathf.Clamp(slot, 0, MaxInteractiveHands - 1);
        handPositions[safeSlot] = position;
        burstTimes[safeSlot] = time;
        burstIsActive[safeSlot] = active;
        materialInstance.SetVector(BurstIds[safeSlot], new Vector4(position.x, position.y, time, active ? 1f : 0f));
    }

    private void AddTrailSample(int handIndex, Vector2 position, float strength)
    {
        int safeHand = Mathf.Clamp(handIndex, 0, MaxInteractiveHands - 1);
        float now = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        float elapsed = now - lastTrailSampleTimes[safeHand];
        float distance = Vector2.Distance(position, lastTrailSamplePositions[safeHand]);

        if (elapsed < TrailSampleInterval && distance < TrailSampleMinDistance)
        {
            return;
        }

        lastTrailSampleTimes[safeHand] = now;
        lastTrailSamplePositions[safeHand] = position;

        trailPositions[nextTrailSlot] = position;
        trailTimes[nextTrailSlot] = now;
        trailStrengths[nextTrailSlot] = Mathf.Clamp01(strength);
        nextTrailSlot = (nextTrailSlot + 1) % MaxTrailSamples;
    }

    private void AddTrailSamplesBetween(int handIndex, Vector2 from, Vector2 to, float strength)
    {
        float distance = Vector2.Distance(from, to);
        int steps = trailSubsampleDistance > 0f
            ? Mathf.Clamp(Mathf.CeilToInt(distance / trailSubsampleDistance), 1, 5)
            : 1;

        for (int step = 1; step <= steps; step++)
        {
            float t = step / (float)steps;
            AddTrailSample(handIndex, Vector2.Lerp(from, to, t), strength);
        }
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
