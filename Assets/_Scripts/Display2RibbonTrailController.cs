using UnityEngine;

public sealed class Display2RibbonTrailController : MonoBehaviour
{
    private const int MaxHands = 2;
    private const int RibbonLayersPerHand = 4;

    [HideInInspector] public Camera targetCamera;
    [HideInInspector] public bool rainbowMode;
    [HideInInspector] public float cameraHeight = 5f;
    [HideInInspector] public float projectionHeight = 0.02f;

    [Header("Look")]
    [HideInInspector, Range(0.2f, 2.5f)] public float ribbonScale = 1f;
    [HideInInspector, Range(0.5f, 4f)] public float glowIntensity = 1.6f;
    [HideInInspector, Range(0.08f, 0.6f)] public float followLagSeconds = 0.18f;
    [HideInInspector, Range(0.4f, 3.5f)] public float fadeSeconds = 1.65f;
    [HideInInspector] public bool enableScreenEdgeFade = true;
    [HideInInspector, Range(0.01f, 0.7f)] public float screenEdgeFadeWidth = 0.28f;
    [HideInInspector, Range(0f, 1f)] public float screenEdgeFadeStrength = 0.85f;
    [HideInInspector, Range(0.25f, 4f)] public float screenEdgeFadeSoftness = 0.85f;
    [HideInInspector, Range(0f, 1f)] public float screenCornerFadeBoost = 0.45f;

    [Header("Particle Ribbon Main")]
    [HideInInspector] public bool enableParticleRibbon = true;
    [HideInInspector] public bool enableTrailRendererRibbon;
    [HideInInspector, Range(256, 6000)] public int particleRibbonMaxParticles = 2200;
    [HideInInspector, Range(0f, 2600f)] public float particleRibbonAmount = 1050f;
    [HideInInspector, Range(0.035f, 0.75f)] public float particleRibbonSize = 0.18f;
    [HideInInspector, Range(0.35f, 4f)] public float particleRibbonLifetime = 1.85f;
    [HideInInspector, Range(0f, 4f)] public float particleRibbonForce = 1.15f;
    [HideInInspector, Range(0f, 3f)] public float particleRibbonBackflow = 0.82f;
    [HideInInspector, Range(0.02f, 2.6f)] public float particleRibbonSpread = 0.34f;
    [HideInInspector, Range(0f, 3f)] public float particleRibbonNoise = 0.95f;
    [HideInInspector, Range(0.4f, 5f)] public float particleRibbonHeadBrightness = 2.1f;
    [HideInInspector, Range(0f, 0.45f)] public float particleRibbonTailOpacity = 0.035f;
    [HideInInspector] public bool enableRibbonColorCycle = true;
    [HideInInspector, Min(0.1f)] public float ribbonColorCycleSeconds = 3f;
    [HideInInspector] public Color[] ribbonColorCyclePalette =
    {
        new Color(0.98f, 0.39f, 0.92f, 1f),
        new Color(0.66f, 0.36f, 1.00f, 1f),
        new Color(0.36f, 0.62f, 1.00f, 1f),
        new Color(1.00f, 0.72f, 0.96f, 1f),
        new Color(0.52f, 0.22f, 0.92f, 1f)
    };

    [Header("Ribbon Head Shape")]
    [HideInInspector, Range(0.6f, 3.5f)] public float ribbonHeadSize = 1.55f;
    [HideInInspector, Range(0f, 2.5f)] public float ribbonHeadBulge = 0.85f;
    [HideInInspector, Range(0f, 1f)] public float ribbonHeadSoftness = 0.55f;
    [HideInInspector, Range(0f, 2.5f)] public float ribbonSpeedStretch = 0.65f;
    [HideInInspector, Range(0f, 3f)] public float ribbonForce = 0.55f;

    [Header("Ribbon Color Falloff")]
    [HideInInspector, Range(0.5f, 3f)] public float ribbonHeadBrightness = 1.35f;
    [HideInInspector, Range(0.05f, 1f)] public float ribbonTailBrightness = 0.32f;
    [HideInInspector, Range(0f, 1f)] public float ribbonTailAlpha = 0.04f;
    [HideInInspector, Range(0f, 1f)] public float ribbonColorVariation = 0.45f;

    [Header("Mist Particles")]
    [HideInInspector] public bool enableMistParticles = true;
    [HideInInspector, Range(128, 5000)] public int mistMaxParticles = 2000;
    [HideInInspector, Range(0f, 2500f)] public float mistAmount = 720f;
    [HideInInspector, Range(0.015f, 0.6f)] public float mistSize = 0.12f;
    [HideInInspector, Range(0.2f, 6f)] public float mistLifetime = 2.75f;
    [HideInInspector, Range(0f, 2f)] public float mistSpeed = 0.14f;
    [HideInInspector, Range(0.02f, 2.4f)] public float mistSpread = 0.62f;
    [HideInInspector, Range(0f, 2f)] public float mistDrift = 0.32f;
    [HideInInspector, Range(0f, 3f)] public float mistNoiseStrength = 0.95f;
    [HideInInspector, Range(0f, 1f)] public float mistOpacity = 0.075f;
    [HideInInspector] public Color mistTint = new Color(0.10f, 1.00f, 0.60f, 1f);

    [Header("SDF/FBM Mist Material")]
    [HideInInspector, Range(0.02f, 0.8f)] public float mistCoreRadius = 0.18f;
    [HideInInspector, Range(0.1f, 2f)] public float mistHaloRadius = 0.95f;
    [HideInInspector, Range(0.01f, 1f)] public float mistSdfSoftness = 0.34f;
    [HideInInspector, Range(0f, 8f)] public float mistCorePower = 2.7f;
    [HideInInspector, Range(0f, 8f)] public float mistHaloPower = 1.45f;
    [HideInInspector, Range(0f, 10f)] public float mistEmissionPower = 2.8f;
    [HideInInspector, Range(0.2f, 16f)] public float mistFbmScale = 4.2f;
    [HideInInspector, Range(0f, 3f)] public float mistFbmFlowSpeed = 0.18f;
    [HideInInspector, Range(0.2f, 4f)] public float mistAlphaPower = 1.15f;
    [HideInInspector] public Color mistFlowColorA = new Color(0.08f, 1.00f, 0.55f, 1f);
    [HideInInspector] public Color mistFlowColorB = new Color(0.22f, 0.84f, 1.00f, 1f);

    [Header("Shake Wave Burst")]
    [HideInInspector] public bool enableShakeWaveBurst = true;
    [HideInInspector] public string shakeWaveResourcePath = "ShakeWaveI/Prefebs/shake_wave_1";
    [HideInInspector, Range(0.2f, 4f)] public float shakeWaveScale = 1.35f;
    [HideInInspector, Range(0.2f, 6f)] public float shakeWaveLifetime = 2.8f;
    [HideInInspector, Range(0f, 3f)] public float shakeWaveBrightness = 1.35f;
    [HideInInspector] public Color shakeWaveColor = new Color(0.16f, 1.00f, 0.42f, 1f);

    private static readonly Color[] RainbowPalette =
    {
        new Color(0.08f, 1.00f, 0.45f, 1f),
        new Color(0.00f, 0.88f, 1.00f, 1f),
        new Color(0.80f, 0.25f, 1.00f, 1f),
        new Color(0.95f, 0.95f, 0.08f, 1f)
    };

    private static readonly Color[] GreenPalette =
    {
        new Color(0.08f, 1.00f, 0.45f, 1f),
        new Color(0.28f, 1.00f, 0.14f, 1f),
        new Color(0.00f, 0.86f, 0.58f, 1f),
        new Color(0.70f, 1.00f, 0.16f, 1f)
    };

    private readonly HandRibbon[] hands = new HandRibbon[MaxHands];
    private readonly Vector3[] targetPositions = new Vector3[MaxHands];
    private readonly bool[] handActive = new bool[MaxHands];
    private readonly bool[] handWasActive = new bool[MaxHands];
    private Material[] layerMaterials;
    private Material ribbonParticleMaterial;
    private Material mistMaterial;
    private GameObject shakeWavePrefab;
    private int activeHandCount;
    private int projectionLayer = -1;

    private void OnEnable()
    {
        EnsureSetup();
    }

    private void Update()
    {
        EnsureSetup();
        ConfigureCamera();
        UpdateRibbonColors();
        UpdateParticleMaterials();
        UpdateHands();
    }

    public void SetInteractionHands(Vector2[] normalizedPositions, int count)
    {
        EnsureSetup();

        activeHandCount = Mathf.Clamp(count, 0, MaxHands);
        for (int i = 0; i < MaxHands; i++)
        {
            bool active = i < activeHandCount && normalizedPositions != null;
            handActive[i] = active;

            if (!active)
            {
                hands[i].SetTrailEmitting(false);
                hands[i].SetParticleRibbonEmitting(false);
                hands[i].SetMistEmitting(false);
                handWasActive[i] = false;
                continue;
            }

            targetPositions[i] = NormalizedToWorld(normalizedPositions[i], i);
            hands[i].SetTrailEmitting(enableTrailRendererRibbon);
            hands[i].SetParticleRibbonEmitting(enableParticleRibbon);
            hands[i].SetMistEmitting(enableMistParticles);

            if (enableShakeWaveBurst && !handWasActive[i])
            {
                TriggerShakeWave(targetPositions[i]);
            }

            handWasActive[i] = true;
        }
    }

    private void EnsureSetup()
    {
        projectionLayer = LayerMask.NameToLayer("ProjectionContent");
        if (projectionLayer >= 0)
        {
            gameObject.layer = projectionLayer;
        }

        EnsureMaterials();

        for (int i = 0; i < MaxHands; i++)
        {
            if (hands[i] != null)
            {
                continue;
            }

            GameObject handObject = new GameObject($"Ribbon Hand {i + 1}");
            handObject.transform.SetParent(transform, false);
            if (projectionLayer >= 0)
            {
                handObject.layer = projectionLayer;
            }

            hands[i] = new HandRibbon(handObject.transform, layerMaterials, ribbonParticleMaterial, mistMaterial);
            targetPositions[i] = NormalizedToWorld(new Vector2(0.5f, 0.5f), i);
            hands[i].Reset(targetPositions[i]);
            hands[i].SetTrailEmitting(false);
            hands[i].SetParticleRibbonEmitting(false);
            hands[i].SetMistEmitting(false);
        }
    }

    private void EnsureMaterials()
    {
        if (layerMaterials != null &&
            layerMaterials.Length == RibbonLayersPerHand &&
            ribbonParticleMaterial != null &&
            mistMaterial != null)
        {
            return;
        }

        Shader ribbonShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (ribbonShader == null)
        {
            ribbonShader = Shader.Find("Particles/Standard Unlit");
        }

        if (ribbonShader == null)
        {
            ribbonShader = Shader.Find("Sprites/Default");
        }

        Shader particleShader = Shader.Find("INFO90003/Display2 SDF FBM Particle");
        if (particleShader == null)
        {
            particleShader = ribbonShader;
        }

        layerMaterials = new Material[RibbonLayersPerHand];
        for (int i = 0; i < layerMaterials.Length; i++)
        {
            layerMaterials[i] = new Material(ribbonShader)
            {
                name = $"Display 2 Soft Ribbon Layer {i + 1}"
            };
            layerMaterials[i].SetColor("_BaseColor", Color.white);
            layerMaterials[i].SetColor("_Color", Color.white);
        }

        ribbonParticleMaterial = new Material(particleShader)
        {
            name = "Display 2 Particle Ribbon Material"
        };

        mistMaterial = new Material(particleShader)
        {
            name = "Display 2 Mist Particle Material"
        };
        UpdateParticleMaterials();
    }

    private void ConfigureCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = true;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;
        targetCamera.transform.SetPositionAndRotation(
            new Vector3(0f, cameraHeight, 0f),
            Quaternion.Euler(90f, 0f, 0f));

        if (projectionLayer >= 0)
        {
            targetCamera.cullingMask = 1 << projectionLayer;
        }
    }

    private void UpdateRibbonColors()
    {
        Color[] palette = rainbowMode ? RainbowPalette : GreenPalette;
        float t = Time.time * 0.38f;

        for (int i = 0; i < layerMaterials.Length; i++)
        {
            float offset = i / (float)layerMaterials.Length;
            Color color = SamplePalette(palette, t + offset);
            color *= glowIntensity;
            color.a = i == 0 ? 0.82f : Mathf.Lerp(0.44f, 0.18f, i / 3f);
            layerMaterials[i].SetColor("_BaseColor", color);
            layerMaterials[i].SetColor("_Color", color);
        }
    }

    private void UpdateParticleMaterials()
    {
        Color baseColor = rainbowMode ? SamplePalette(RainbowPalette, Time.time * 0.16f) : mistFlowColorA;
        Color edgeColor = rainbowMode ? SamplePalette(RainbowPalette, Time.time * 0.16f + 0.32f) : mistFlowColorB;

        if (ribbonParticleMaterial != null)
        {
            Color ribbonColor = SampleRibbonColor(0);
            Color ribbonEdgeColor = enableRibbonColorCycle && HasRibbonCyclePalette()
                ? SampleRibbonCycleColor(Time.time + ribbonColorCycleSeconds * 0.5f)
                : edgeColor;
            Color ribbonBase = ribbonColor * glowIntensity * particleRibbonHeadBrightness;
            Color ribbonEdge = ribbonEdgeColor * glowIntensity * Mathf.Lerp(0.75f, 1.25f, particleRibbonTailOpacity * 2f);
            ribbonBase.a = 1f;
            ribbonEdge.a = 1f;

            ApplySdfFbmMaterial(
                ribbonParticleMaterial,
                ribbonBase,
                ribbonEdge,
                Mathf.Lerp(0.10f, 0.30f, Mathf.InverseLerp(0.035f, 0.75f, particleRibbonSize)),
                Mathf.Clamp(mistHaloRadius * 0.86f, 0.08f, 2f),
                mistSdfSoftness,
                Mathf.Max(mistCorePower, 3.1f),
                Mathf.Max(mistHaloPower, 1.7f),
                Mathf.Max(mistEmissionPower, 3.2f),
                mistFbmScale,
                mistFbmFlowSpeed,
                mistAlphaPower,
                enableScreenEdgeFade,
                screenEdgeFadeWidth,
                screenEdgeFadeStrength,
                screenEdgeFadeSoftness,
                screenCornerFadeBoost);
        }

        if (mistMaterial != null)
        {
            Color mistBase = baseColor * glowIntensity;
            Color mistEdge = edgeColor * glowIntensity;
            mistBase.a = mistOpacity;
            mistEdge.a = mistOpacity;

            ApplySdfFbmMaterial(
                mistMaterial,
                mistBase,
                mistEdge,
                mistCoreRadius,
                mistHaloRadius,
                mistSdfSoftness,
                mistCorePower,
                mistHaloPower,
                mistEmissionPower,
                mistFbmScale,
                mistFbmFlowSpeed,
                mistAlphaPower,
                enableScreenEdgeFade,
                screenEdgeFadeWidth,
                screenEdgeFadeStrength,
                screenEdgeFadeSoftness,
                screenCornerFadeBoost);
        }
    }

    private static void ApplySdfFbmMaterial(
        Material material,
        Color baseColor,
        Color edgeColor,
        float coreRadius,
        float haloRadius,
        float sdfSoftness,
        float corePower,
        float haloPower,
        float emissionPower,
        float fbmScale,
        float fbmFlowSpeed,
        float alphaPower,
        bool enableScreenEdgeFade,
        float screenEdgeFadeWidth,
        float screenEdgeFadeStrength,
        float screenEdgeFadeSoftness,
        float screenCornerFadeBoost)
    {
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetColor("_ColorA", baseColor);
        material.SetColor("_ColorB", edgeColor);
        material.SetFloat("_CoreRadius", coreRadius);
        material.SetFloat("_HaloRadius", haloRadius);
        material.SetFloat("_SdfSoftness", sdfSoftness);
        material.SetFloat("_CorePower", corePower);
        material.SetFloat("_HaloPower", haloPower);
        material.SetFloat("_EmissionPower", emissionPower);
        material.SetFloat("_FbmScale", fbmScale);
        material.SetFloat("_FbmFlowSpeed", fbmFlowSpeed);
        material.SetFloat("_AlphaPower", alphaPower);
        material.SetFloat("_ScreenEdgeFadeWidth", Mathf.Clamp(screenEdgeFadeWidth, 0.01f, 0.7f));
        material.SetFloat("_ScreenEdgeFadeStrength", enableScreenEdgeFade ? Mathf.Clamp01(screenEdgeFadeStrength) : 0f);
        material.SetFloat("_ScreenEdgeFadeSoftness", Mathf.Clamp(screenEdgeFadeSoftness, 0.25f, 4f));
        material.SetFloat("_ScreenCornerFadeBoost", Mathf.Clamp01(screenCornerFadeBoost));
    }

    private void UpdateHands()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.001f);
        for (int i = 0; i < MaxHands; i++)
        {
            if (!handActive[i])
            {
                hands[i].SetTrailEmitting(false);
                hands[i].SetParticleRibbonEmitting(false);
                hands[i].SetMistEmitting(false);
                hands[i].UpdateInactive(dt);
                continue;
            }

            float handDelay = followLagSeconds + i * 0.035f;
            hands[i].MoveToward(
                targetPositions[i],
                dt,
                handDelay,
                ribbonScale,
                fadeSeconds,
                ribbonHeadSize,
                ribbonHeadBulge,
                ribbonHeadSoftness,
                ribbonSpeedStretch,
                ribbonForce,
                ribbonHeadBrightness,
                ribbonTailBrightness,
                ribbonTailAlpha,
                ribbonColorVariation);

            float speedEffect = hands[i].SpeedEffect;
            float forceBoost = 1f + speedEffect * ribbonForce;
            hands[i].ConfigureParticleRibbon(
                enableParticleRibbon,
                particleRibbonMaxParticles,
                particleRibbonAmount,
                particleRibbonSize,
                particleRibbonLifetime,
                particleRibbonForce,
                particleRibbonBackflow,
                particleRibbonSpread,
                particleRibbonNoise,
                particleRibbonHeadBrightness,
                particleRibbonTailOpacity,
                SampleRibbonColor(i),
                dt);
            hands[i].ConfigureMist(
                enableMistParticles,
                mistMaxParticles,
                mistAmount,
                mistSize * (1f + speedEffect * ribbonForce * 0.18f),
                mistLifetime,
                mistSpeed * forceBoost,
                mistSpread * forceBoost,
                mistDrift * forceBoost,
                mistNoiseStrength * (1f + speedEffect * ribbonForce * 0.75f),
                mistOpacity,
                SampleMistColor(i));
        }
    }

    private Color SampleRibbonColor(int handIndex)
    {
        Color color;
        if (enableRibbonColorCycle && HasRibbonCyclePalette())
        {
            color = SampleRibbonCycleColor(Time.time);
        }
        else
        {
            color = rainbowMode
                ? SamplePalette(RainbowPalette, Time.time * 0.26f + handIndex * 0.19f)
                : mistFlowColorA;
        }

        color.a = 1f;
        return color;
    }

    private bool HasRibbonCyclePalette()
    {
        return ribbonColorCyclePalette != null && ribbonColorCyclePalette.Length > 0;
    }

    private Color SampleRibbonCycleColor(float time)
    {
        if (!HasRibbonCyclePalette())
        {
            return mistFlowColorA;
        }

        if (ribbonColorCyclePalette.Length == 1)
        {
            Color onlyColor = ribbonColorCyclePalette[0];
            onlyColor.a = 1f;
            return onlyColor;
        }

        float cycleSeconds = Mathf.Max(0.1f, ribbonColorCycleSeconds);
        float wrapped = Mathf.Repeat(time / cycleSeconds, ribbonColorCyclePalette.Length);
        int currentIndex = Mathf.FloorToInt(wrapped) % ribbonColorCyclePalette.Length;
        int nextIndex = (currentIndex + 1) % ribbonColorCyclePalette.Length;
        float blend = Mathf.SmoothStep(0f, 1f, wrapped - Mathf.Floor(wrapped));
        Color color = Color.Lerp(ribbonColorCyclePalette[currentIndex], ribbonColorCyclePalette[nextIndex], blend);
        color.a = 1f;
        return color;
    }

    private Color SampleMistColor(int handIndex)
    {
        Color color = rainbowMode
            ? SamplePalette(RainbowPalette, Time.time * 0.22f + handIndex * 0.17f)
            : mistTint;
        color.a = mistOpacity;
        return color;
    }

    private void TriggerShakeWave(Vector3 position)
    {
        if (shakeWavePrefab == null)
        {
            shakeWavePrefab = Resources.Load<GameObject>(shakeWaveResourcePath);
        }

        if (shakeWavePrefab == null)
        {
            Debug.LogWarning($"[Display2RibbonTrail] Could not load Shake Wave prefab from Resources/{shakeWaveResourcePath}.");
            return;
        }

        GameObject burst = Instantiate(shakeWavePrefab, position, Quaternion.identity, transform);
        burst.name = "Display 2 Shake Wave Burst";
        burst.transform.localScale = Vector3.one * shakeWaveScale;
        ApplyLayerRecursive(burst, projectionLayer);
        TintParticleSystems(burst, shakeWaveColor * shakeWaveBrightness);
        Destroy(burst, shakeWaveLifetime);
    }

    private static void ApplyLayerRecursive(GameObject rootObject, int layer)
    {
        if (layer < 0 || rootObject == null)
        {
            return;
        }

        rootObject.layer = layer;
        foreach (Transform child in rootObject.transform)
        {
            ApplyLayerRecursive(child.gameObject, layer);
        }
    }

    private static void TintParticleSystems(GameObject rootObject, Color color)
    {
        ParticleSystem[] particleSystems = rootObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            color.a = Mathf.Max(color.a, 0.65f);
            main.startColor = color;
        }
    }

    private Vector3 NormalizedToWorld(Vector2 normalized, int handIndex)
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
        {
            return new Vector3(normalized.x - 0.5f, projectionHeight, normalized.y - 0.5f);
        }

        float height = cameraToUse.orthographicSize * 2f;
        float width = height * cameraToUse.aspect;
        float x = (Mathf.Clamp01(normalized.x) - 0.5f) * width;
        float z = (Mathf.Clamp01(normalized.y) - 0.5f) * height;
        return new Vector3(x, projectionHeight + handIndex * 0.015f, z);
    }

    private static Color SamplePalette(Color[] palette, float t)
    {
        if (palette == null || palette.Length == 0)
        {
            return Color.white;
        }

        float wrapped = Mathf.Repeat(t, 1f) * palette.Length;
        int a = Mathf.FloorToInt(wrapped) % palette.Length;
        int b = (a + 1) % palette.Length;
        float blend = Mathf.SmoothStep(0f, 1f, wrapped - Mathf.Floor(wrapped));
        return Color.Lerp(palette[a], palette[b], blend);
    }

    private void OnDestroy()
    {
        if (layerMaterials == null)
        {
            return;
        }

        for (int i = 0; i < layerMaterials.Length; i++)
        {
            if (layerMaterials[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(layerMaterials[i]);
            }
            else
            {
                DestroyImmediate(layerMaterials[i]);
            }
        }

        if (ribbonParticleMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(ribbonParticleMaterial);
            }
            else
            {
                DestroyImmediate(ribbonParticleMaterial);
            }
        }

        if (mistMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(mistMaterial);
            }
            else
            {
                DestroyImmediate(mistMaterial);
            }
        }

    }

    private sealed class HandRibbon
    {
        private readonly Transform root;
        private readonly TrailRenderer[] trails = new TrailRenderer[RibbonLayersPerHand];
        private readonly ParticleSystem ribbonParticles;
        private readonly ParticleSystem mistParticles;
        private readonly Vector3[] offsets =
        {
            Vector3.zero,
            new Vector3(0.055f, 0.006f, -0.035f),
            new Vector3(-0.05f, 0.012f, 0.042f),
            new Vector3(0.02f, 0.018f, 0.065f)
        };
        private Vector3 position;
        private Vector3 velocity;
        private float speedEffect;
        private float particleEmitAccumulator;

        public float SpeedEffect => speedEffect;

        public HandRibbon(Transform root, Material[] materials, Material ribbonParticleMaterial, Material mistMaterial)
        {
            this.root = root;

            GameObject particleRibbonObject = new GameObject("Particle Ribbon Main");
            particleRibbonObject.transform.SetParent(root, false);
            particleRibbonObject.layer = root.gameObject.layer;
            ribbonParticles = particleRibbonObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer ribbonRenderer = particleRibbonObject.GetComponent<ParticleSystemRenderer>();
            ribbonRenderer.material = ribbonParticleMaterial;
            ribbonRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            ribbonRenderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            ribbonRenderer.velocityScale = 0.08f;
            ribbonRenderer.lengthScale = 1.8f;
            ribbonRenderer.cameraVelocityScale = 0f;
            ribbonRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ribbonRenderer.receiveShadows = false;
            ConfigureParticleRibbon(false, 2200, 0f, 0.18f, 1.85f, 1.15f, 0.82f, 0.34f, 0.95f, 2.1f, 0.035f, Color.white, 0.016f);
            ribbonParticles.Play();

            for (int i = 0; i < trails.Length; i++)
            {
                GameObject trailObject = new GameObject($"Soft Ribbon Layer {i + 1}");
                trailObject.transform.SetParent(root, false);
                trailObject.layer = root.gameObject.layer;

                TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
                trail.material = materials[i];
                trail.time = 1.65f + i * 0.18f;
                trail.minVertexDistance = 0.035f + i * 0.012f;
                trail.numCornerVertices = 10;
                trail.numCapVertices = 10;
                trail.alignment = LineAlignment.View;
                trail.textureMode = LineTextureMode.Stretch;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.widthCurve = CreateWidthCurve(i, 1.55f, 0.85f, 0.55f, 0f, 0f);
                trail.colorGradient = CreateRibbonGradient(i, 1.35f, 0.32f, 0.04f, 0.45f);
                trail.autodestruct = false;
                trail.emitting = false;
                trails[i] = trail;
            }

            GameObject mistObject = new GameObject("Soft Mist Particles");
            mistObject.transform.SetParent(root, false);
            mistObject.layer = root.gameObject.layer;
            mistParticles = mistObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = mistObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = mistMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ConfigureMist(false, 2000, 0f, 0.12f, 2.75f, 0.14f, 0.62f, 0.32f, 0.95f, 0.075f, Color.white);
            mistParticles.Play();
        }

        public void Reset(Vector3 startPosition)
        {
            position = startPosition;
            velocity = Vector3.zero;
            particleEmitAccumulator = 0f;
            root.position = startPosition;

            ribbonParticles.transform.position = startPosition;
            ribbonParticles.Clear();

            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].transform.position = startPosition + offsets[i];
                trails[i].Clear();
            }

            mistParticles.transform.position = startPosition;
            mistParticles.Clear();
        }

        public void SetTrailEmitting(bool emitting)
        {
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].emitting = emitting;
            }
        }

        public void SetParticleRibbonEmitting(bool emitting)
        {
            ParticleSystem.EmissionModule emission = ribbonParticles.emission;
            emission.enabled = false;

            if (emitting && !ribbonParticles.isPlaying)
            {
                ribbonParticles.Play();
            }

            if (!emitting)
            {
                particleEmitAccumulator = 0f;
            }
        }

        public void SetMistEmitting(bool emitting)
        {
            ParticleSystem.EmissionModule emission = mistParticles.emission;
            emission.enabled = emitting;
        }

        public void MoveToward(
            Vector3 target,
            float deltaTime,
            float lagSeconds,
            float scale,
            float fadeSeconds,
            float headSize,
            float headBulge,
            float headSoftness,
            float speedStretch,
            float force,
            float headBrightness,
            float tailBrightness,
            float tailAlpha,
            float colorVariation)
        {
            position = Vector3.SmoothDamp(position, target, ref velocity, Mathf.Max(0.02f, lagSeconds), Mathf.Infinity, deltaTime);
            root.position = position;
            speedEffect = Mathf.Lerp(speedEffect, Mathf.Clamp01(velocity.magnitude * 0.18f), 1f - Mathf.Exp(-deltaTime * 12f));

            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];
                float layerScale = scale * Mathf.Lerp(1f, 1.75f, i / 3f);
                float forceSpread = 1f + speedEffect * force * Mathf.Lerp(0.08f, 0.28f, i / 3f);
                trail.time = fadeSeconds + i * 0.22f;
                trail.widthMultiplier = layerScale * forceSpread;
                trail.widthCurve = CreateWidthCurve(i, headSize, headBulge, headSoftness, speedEffect, speedStretch);
                trail.colorGradient = CreateRibbonGradient(i, headBrightness, tailBrightness, tailAlpha, colorVariation);
                trail.transform.position = position + offsets[i] * scale * forceSpread;
            }

            ribbonParticles.transform.position = position;
            mistParticles.transform.position = position;
        }

        public void ConfigureParticleRibbon(
            bool enabled,
            int maxParticles,
            float amount,
            float size,
            float lifetime,
            float force,
            float backflow,
            float spread,
            float noiseStrength,
            float headBrightness,
            float tailOpacity,
            Color color,
            float deltaTime)
        {
            ParticleSystem.MainModule main = ribbonParticles.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.72f, lifetime * 1.18f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.72f, size * 1.44f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            color.a = Mathf.Clamp01(0.72f + speedEffect * 0.24f);
            main.startColor = color * headBrightness;
            main.maxParticles = Mathf.Max(64, maxParticles);

            ParticleSystem.EmissionModule emission = ribbonParticles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ribbonParticles.shape;
            shape.enabled = false;

            ParticleSystem.NoiseModule noise = ribbonParticles.noise;
            noise.enabled = noiseStrength > 0f;
            noise.strength = noiseStrength * (1f + speedEffect * force);
            noise.frequency = 0.34f;
            noise.scrollSpeed = 0.22f + speedEffect * 0.16f;
            noise.damping = true;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ribbonParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white * headBrightness, 0f),
                    new GradientColorKey(Color.white, 0.42f),
                    new GradientColorKey(new Color(0.62f, 0.78f, 0.72f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.58f, 0.22f),
                    new GradientAlphaKey(0.18f, 0.68f),
                    new GradientAlphaKey(tailOpacity, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ribbonParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            float forceLift = 1f + speedEffect * force * 0.34f;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(forceLift, new AnimationCurve(
                new Keyframe(0f, 0.58f),
                new Keyframe(0.18f, 1.1f),
                new Keyframe(0.62f, 1.35f),
                new Keyframe(1f, 1.72f)));

            if (!enabled || amount <= 0f || deltaTime <= 0f)
            {
                particleEmitAccumulator = 0f;
                return;
            }

            float velocityMagnitude = Mathf.Min(velocity.magnitude, 12f);
            float forceBoost = 1f + speedEffect * force;
            particleEmitAccumulator += amount * deltaTime * forceBoost;
            int emitCount = Mathf.Min(96, Mathf.FloorToInt(particleEmitAccumulator));
            particleEmitAccumulator -= emitCount;

            Vector3 motionDirection = velocityMagnitude > 0.02f
                ? velocity.normalized
                : new Vector3(Mathf.Sin(Time.time * 1.7f), 0f, Mathf.Cos(Time.time * 1.7f)).normalized;
            Vector3 sideDirection = new Vector3(-motionDirection.z, 0f, motionDirection.x);
            float sourceRadius = size * (0.32f + speedEffect * force * 0.2f);

            for (int i = 0; i < emitCount; i++)
            {
                Vector2 disk = Random.insideUnitCircle;
                Vector3 sourceOffset = (sideDirection * disk.x + motionDirection * disk.y * 0.35f) * sourceRadius;
                Vector3 outward = (sideDirection * Random.Range(-1f, 1f) + motionDirection * Random.Range(-0.24f, 0.16f));
                Vector3 backVelocity = -motionDirection * velocityMagnitude * backflow * (0.14f + force * 0.08f);
                Vector3 spreadVelocity = outward * spread * forceBoost * Random.Range(0.35f, 1.35f);

                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                {
                    position = position + sourceOffset,
                    velocity = backVelocity + spreadVelocity,
                    startSize = size * Random.Range(0.78f, 1.58f) * (1f + speedEffect * force * 0.2f),
                    startLifetime = lifetime * Random.Range(0.72f, 1.18f),
                    startColor = color * Random.Range(headBrightness * 0.72f, headBrightness * 1.18f)
                };
                ribbonParticles.Emit(emitParams, 1);
            }
        }

        public void ConfigureMist(
            bool enabled,
            int maxParticles,
            float amount,
            float size,
            float lifetime,
            float speed,
            float spread,
            float drift,
            float noiseStrength,
            float opacity,
            Color color)
        {
            ParticleSystem.MainModule main = mistParticles.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = Mathf.Max(0.05f, lifetime);
            main.startSpeed = Mathf.Max(0f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size * 1.25f);
            color.a = opacity;
            main.startColor = color;
            main.maxParticles = Mathf.Max(32, maxParticles);

            ParticleSystem.EmissionModule emission = mistParticles.emission;
            emission.enabled = enabled;
            emission.rateOverTime = amount;

            ParticleSystem.ShapeModule shape = mistParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = spread;

            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = mistParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = drift > 0f;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-drift, drift);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, drift * 0.18f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-drift, drift);

            ParticleSystem.NoiseModule noise = mistParticles.noise;
            noise.enabled = noiseStrength > 0f;
            noise.strength = noiseStrength;
            noise.frequency = 0.42f;
            noise.scrollSpeed = 0.18f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = mistParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(opacity, 0.18f),
                    new GradientAlphaKey(opacity * 0.45f, 0.66f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = mistParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.32f),
                new Keyframe(0.42f, 1f),
                new Keyframe(1f, 1.45f)));
        }

        public void UpdateInactive(float deltaTime)
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, 1f - Mathf.Exp(-deltaTime * 8f));
        }

        private static AnimationCurve CreateWidthCurve(
            int layer,
            float headSize,
            float headBulge,
            float headSoftness,
            float speedEffect,
            float speedStretch)
        {
            float baseWidth = layer == 0 ? 0.17f : Mathf.Lerp(0.28f, 0.52f, layer / 3f);
            float layerLift = Mathf.Lerp(1f, 1.26f, layer / 3f);
            float stretch = speedEffect * speedStretch;
            float soft = Mathf.Clamp01(headSoftness);
            float shoulder = Mathf.Lerp(0.48f, 0.70f, soft) + stretch * 0.12f;
            float crown = Mathf.Lerp(0.22f, 0.12f, soft);
            float headWidth = baseWidth * Mathf.Max(0.2f, headSize) * layerLift;
            float bulgeWidth = headWidth * (1f + headBulge * 0.38f + stretch * 0.22f);
            float compressedTip = headWidth * Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(stretch * 0.65f));

            return new AnimationCurve(
                new Keyframe(0f, compressedTip),
                new Keyframe(Mathf.Clamp01(crown), bulgeWidth),
                new Keyframe(Mathf.Clamp01(shoulder), baseWidth * Mathf.Lerp(0.68f, 1.06f, soft)),
                new Keyframe(0.84f, baseWidth * Mathf.Lerp(0.22f, 0.48f, soft)),
                new Keyframe(1f, baseWidth * 0.05f));
        }

        private static Gradient CreateRibbonGradient(
            int layer,
            float headBrightness,
            float tailBrightness,
            float tailAlpha,
            float colorVariation)
        {
            float alpha = layer == 0 ? 0.9f : Mathf.Lerp(0.32f, 0.12f, layer / 3f);
            float layerFade = Mathf.Lerp(1f, 0.62f, layer / 3f);
            Color tailColor = Color.Lerp(Color.white, new Color(0.55f, 0.72f, 0.68f, 1f), colorVariation) * tailBrightness;
            Color midColor = Color.Lerp(Color.white, new Color(0.82f, 1f, 0.88f, 1f), colorVariation * 0.45f);
            Color headColor = Color.white * headBrightness;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(headColor, 0f),
                    new GradientColorKey(midColor, 0.42f),
                    new GradientColorKey(tailColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(Mathf.Clamp01(alpha * headBrightness), 0f),
                    new GradientAlphaKey(alpha * 0.62f * layerFade, 0.28f),
                    new GradientAlphaKey(alpha * 0.24f * layerFade, 0.72f),
                    new GradientAlphaKey(alpha * tailAlpha * layerFade, 1f)
                });
            return gradient;
        }
    }
}
