using UnityEngine;

public sealed class Display2RibbonTrailController : MonoBehaviour
{
    private const int MaxHands = 2;
    private const int RibbonLayersPerHand = 4;

    public Camera targetCamera;
    public bool rainbowMode;
    public float cameraHeight = 5f;
    public float projectionHeight = 0.02f;

    [Header("Look")]
    [Range(0.2f, 2.5f)] public float ribbonScale = 1f;
    [Range(0.5f, 4f)] public float glowIntensity = 1.6f;
    [Range(0.08f, 0.6f)] public float followLagSeconds = 0.18f;
    [Range(0.4f, 3.5f)] public float fadeSeconds = 1.65f;

    [Header("Mist Particles")]
    public bool enableMistParticles = true;
    [Range(0f, 120f)] public float mistAmount = 34f;
    [Range(0.05f, 1.4f)] public float mistSize = 0.48f;
    [Range(0.2f, 4f)] public float mistLifetime = 1.75f;
    [Range(0f, 2f)] public float mistSpeed = 0.24f;
    [Range(0.02f, 1.2f)] public float mistSpread = 0.34f;
    [Range(0f, 2f)] public float mistDrift = 0.55f;
    [Range(0f, 2f)] public float mistNoiseStrength = 0.72f;
    [Range(0f, 1f)] public float mistOpacity = 0.34f;
    public Color mistTint = new Color(0.10f, 1.00f, 0.60f, 1f);

    [Header("Shake Wave Burst")]
    public bool enableShakeWaveBurst = true;
    public string shakeWaveResourcePath = "ShakeWaveI/Prefebs/shake_wave_1";
    [Range(0.2f, 4f)] public float shakeWaveScale = 1.35f;
    [Range(0.2f, 6f)] public float shakeWaveLifetime = 2.8f;
    [Range(0f, 3f)] public float shakeWaveBrightness = 1.35f;
    public Color shakeWaveColor = new Color(0.16f, 1.00f, 0.42f, 1f);

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
                hands[i].SetEmitting(false);
                hands[i].SetMistEmitting(false);
                handWasActive[i] = false;
                continue;
            }

            targetPositions[i] = NormalizedToWorld(normalizedPositions[i], i);
            hands[i].SetEmitting(true);
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

            hands[i] = new HandRibbon(handObject.transform, layerMaterials, mistMaterial);
            targetPositions[i] = NormalizedToWorld(new Vector2(0.5f, 0.5f), i);
            hands[i].Reset(targetPositions[i]);
            hands[i].SetEmitting(false);
            hands[i].SetMistEmitting(false);
        }
    }

    private void EnsureMaterials()
    {
        if (layerMaterials != null && layerMaterials.Length == RibbonLayersPerHand && mistMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        layerMaterials = new Material[RibbonLayersPerHand];
        for (int i = 0; i < layerMaterials.Length; i++)
        {
            layerMaterials[i] = new Material(shader)
            {
                name = $"Display 2 Soft Ribbon Layer {i + 1}"
            };
            layerMaterials[i].SetColor("_BaseColor", Color.white);
            layerMaterials[i].SetColor("_Color", Color.white);
        }

        mistMaterial = new Material(shader)
        {
            name = "Display 2 Mist Particle Material"
        };
        mistMaterial.SetColor("_BaseColor", Color.white);
        mistMaterial.SetColor("_Color", Color.white);
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

    private void UpdateHands()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.001f);
        for (int i = 0; i < MaxHands; i++)
        {
            if (!handActive[i])
            {
                hands[i].SetMistEmitting(false);
                hands[i].UpdateInactive(dt);
                continue;
            }

            float handDelay = followLagSeconds + i * 0.035f;
            hands[i].MoveToward(targetPositions[i], dt, handDelay, ribbonScale, fadeSeconds);
            hands[i].ConfigureMist(
                enableMistParticles,
                mistAmount,
                mistSize,
                mistLifetime,
                mistSpeed,
                mistSpread,
                mistDrift,
                mistNoiseStrength,
                mistOpacity,
                SampleMistColor(i));
        }
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

        public HandRibbon(Transform root, Material[] materials, Material mistMaterial)
        {
            this.root = root;

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
                trail.widthCurve = CreateWidthCurve(i);
                trail.colorGradient = CreateAlphaGradient(i);
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
            ConfigureMist(false, 0f, 0.48f, 1.75f, 0.24f, 0.34f, 0.55f, 0.72f, 0.34f, Color.white);
            mistParticles.Play();
        }

        public void Reset(Vector3 startPosition)
        {
            position = startPosition;
            velocity = Vector3.zero;
            root.position = startPosition;

            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].transform.position = startPosition + offsets[i];
                trails[i].Clear();
            }

            mistParticles.transform.position = startPosition;
            mistParticles.Clear();
        }

        public void SetEmitting(bool emitting)
        {
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].emitting = emitting;
            }
        }

        public void SetMistEmitting(bool emitting)
        {
            ParticleSystem.EmissionModule emission = mistParticles.emission;
            emission.enabled = emitting;
        }

        public void MoveToward(Vector3 target, float deltaTime, float lagSeconds, float scale, float fadeSeconds)
        {
            position = Vector3.SmoothDamp(position, target, ref velocity, Mathf.Max(0.02f, lagSeconds), Mathf.Infinity, deltaTime);
            root.position = position;

            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];
                float layerScale = scale * Mathf.Lerp(1f, 1.75f, i / 3f);
                trail.time = fadeSeconds + i * 0.22f;
                trail.widthMultiplier = layerScale;
                trail.transform.position = position + offsets[i] * scale;
            }

            mistParticles.transform.position = position;
        }

        public void ConfigureMist(
            bool enabled,
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
            main.maxParticles = Mathf.CeilToInt(Mathf.Max(32f, amount * lifetime * 3f));

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

        private static AnimationCurve CreateWidthCurve(int layer)
        {
            float baseWidth = layer == 0 ? 0.17f : Mathf.Lerp(0.28f, 0.52f, layer / 3f);
            return new AnimationCurve(
                new Keyframe(0f, baseWidth * 0.12f),
                new Keyframe(0.18f, baseWidth),
                new Keyframe(0.72f, baseWidth * 0.72f),
                new Keyframe(1f, 0f));
        }

        private static Gradient CreateAlphaGradient(int layer)
        {
            float alpha = layer == 0 ? 0.9f : Mathf.Lerp(0.32f, 0.12f, layer / 3f);
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
                    new GradientAlphaKey(alpha, 0.12f),
                    new GradientAlphaKey(alpha * 0.55f, 0.68f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }
    }
}
