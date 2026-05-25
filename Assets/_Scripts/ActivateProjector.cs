using UnityEngine;

public class ActivateProjector : MonoBehaviour
{
    [Header("Display 2")]
    [SerializeField] private int projectorDisplayIndex = 1;
    [SerializeField] private Camera projectorCamera;
    [SerializeField] private bool rainbowMode;
    [SerializeField] private KeyCode manualTriggerKey = KeyCode.Space;
    [SerializeField] private float cameraHeight = 5f;
    [SerializeField] private float projectionHeight = 0f;

    [Header("Display 2 Particle Ribbon")]
    [SerializeField] private bool createRibbonProjection = true;
    [SerializeField] private bool enableParticleRibbon = true;
    [SerializeField] private bool enableTrailRendererRibbon;
    [SerializeField, Range(0.2f, 2.5f)] private float ribbonScale = 1f;
    [SerializeField, Range(0.5f, 4f)] private float ribbonGlowIntensity = 1.6f;
    [SerializeField, Range(0.08f, 0.6f)] private float ribbonFollowLagSeconds = 0.18f;
    [SerializeField, Range(256, 6000)] private int particleRibbonMaxParticles = 2200;
    [SerializeField, Range(0f, 2600f)] private float particleRibbonAmount = 1050f;
    [SerializeField, Range(0.035f, 0.75f)] private float particleRibbonSize = 0.18f;
    [SerializeField, Range(0.35f, 4f)] private float particleRibbonLifetime = 1.85f;
    [SerializeField, Range(0f, 4f)] private float particleRibbonForce = 1.15f;
    [SerializeField, Range(0f, 3f)] private float particleRibbonBackflow = 0.82f;
    [SerializeField, Range(0.02f, 2.6f)] private float particleRibbonSpread = 0.34f;
    [SerializeField, Range(0f, 3f)] private float particleRibbonNoise = 0.95f;
    [SerializeField, Range(0.4f, 5f)] private float particleRibbonHeadBrightness = 2.1f;
    [SerializeField, Range(0f, 0.45f)] private float particleRibbonTailOpacity = 0.035f;
    [SerializeField] private bool enableRibbonColorCycle = true;
    [SerializeField, Min(0.1f)] private float ribbonColorCycleSeconds = 3f;
    [SerializeField] private Color[] ribbonColorCyclePalette =
    {
        new Color(0.98f, 0.39f, 0.92f, 1f),
        new Color(0.66f, 0.36f, 1.00f, 1f),
        new Color(0.36f, 0.62f, 1.00f, 1f),
        new Color(1.00f, 0.72f, 0.96f, 1f),
        new Color(0.52f, 0.22f, 0.92f, 1f)
    };
    [SerializeField, HideInInspector] private float ribbonFadeSeconds = 1.65f;
    [SerializeField, HideInInspector] private float ribbonHeadSize = 1.55f;
    [SerializeField, HideInInspector] private float ribbonHeadBulge = 0.85f;
    [SerializeField, HideInInspector] private float ribbonHeadSoftness = 0.55f;
    [SerializeField, HideInInspector] private float ribbonSpeedStretch = 0.65f;
    [SerializeField, HideInInspector] private float ribbonForce = 0.55f;
    [SerializeField, HideInInspector] private float ribbonHeadBrightness = 1.35f;
    [SerializeField, HideInInspector] private float ribbonTailBrightness = 0.32f;
    [SerializeField, HideInInspector] private float ribbonTailAlpha = 0.04f;
    [SerializeField, HideInInspector] private float ribbonColorVariation = 0.45f;
    [Header("Display 2 Mist / Light Curtain")]
    [SerializeField] private bool enableMistParticles = true;
    [SerializeField, Range(128, 5000)] private int mistMaxParticles = 2000;
    [SerializeField, Range(0f, 2500f)] private float mistAmount = 720f;
    [SerializeField, Range(0.015f, 0.6f)] private float mistSize = 0.12f;
    [SerializeField, Range(0.2f, 6f)] private float mistLifetime = 2.75f;
    [SerializeField, Range(0f, 2f)] private float mistSpeed = 0.14f;
    [SerializeField, Range(0.02f, 2.4f)] private float mistSpread = 0.62f;
    [SerializeField, Range(0f, 2f)] private float mistDrift = 0.32f;
    [SerializeField, Range(0f, 3f)] private float mistNoiseStrength = 0.95f;
    [SerializeField, Range(0f, 1f)] private float mistOpacity = 0.075f;
    [SerializeField] private Color mistTint = new Color(0.10f, 1.00f, 0.60f, 1f);
    [Header("Display 2 SDF/FBM Mist Material")]
    [SerializeField, Range(0.02f, 0.8f)] private float mistCoreRadius = 0.18f;
    [SerializeField, Range(0.1f, 2f)] private float mistHaloRadius = 0.95f;
    [SerializeField, Range(0.01f, 1f)] private float mistSdfSoftness = 0.34f;
    [SerializeField, Range(0f, 8f)] private float mistCorePower = 2.7f;
    [SerializeField, Range(0f, 8f)] private float mistHaloPower = 1.45f;
    [SerializeField, Range(0f, 10f)] private float mistEmissionPower = 2.8f;
    [SerializeField, Range(0.2f, 16f)] private float mistFbmScale = 4.2f;
    [SerializeField, Range(0f, 3f)] private float mistFbmFlowSpeed = 0.18f;
    [SerializeField, Range(0.2f, 4f)] private float mistAlphaPower = 1.15f;
    [SerializeField] private Color mistFlowColorA = new Color(0.08f, 1.00f, 0.55f, 1f);
    [SerializeField] private Color mistFlowColorB = new Color(0.22f, 0.84f, 1.00f, 1f);
    [Header("Display 2 Shake Wave Burst")]
    [SerializeField] private bool enableShakeWaveBurst = true;
    [SerializeField, HideInInspector] private string shakeWaveResourcePath = "ShakeWaveI/Prefebs/shake_wave_1";
    [SerializeField, Range(0.2f, 4f)] private float shakeWaveScale = 1.35f;
    [SerializeField, Range(0.2f, 6f)] private float shakeWaveLifetime = 2.8f;
    [SerializeField, Range(0f, 3f)] private float shakeWaveBrightness = 1.35f;
    [SerializeField] private Color shakeWaveColor = new Color(0.16f, 1.00f, 0.42f, 1f);

    [SerializeField, HideInInspector] private bool createPastelProjection = false;
    [SerializeField, HideInInspector] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField, HideInInspector] private float strokeOpacity = 2.2f;
    [SerializeField, HideInInspector] private float rippleDuration = 2.8f;
    [SerializeField, HideInInspector] private float maxRippleRadius = 1.25f;
    [SerializeField, HideInInspector] private float grainStrength = 0.18f;
    [SerializeField, HideInInspector] private float flowSpeed = 0.72f;
    [SerializeField, HideInInspector] private float trailLength = 3.0f;
    [SerializeField, HideInInspector] private float bloomBoost = 6.5f;
    [SerializeField, HideInInspector] private float handInfluence = 1.6f;
    [SerializeField, HideInInspector] private float particleDensity = 1.95f;
    [SerializeField, HideInInspector] private float interactionRadius = 0.62f;
    [SerializeField, HideInInspector] private float continuousTrailStrength = 0.8f;
    [SerializeField, HideInInspector] private float cursorLagSeconds = 0.16f;
    [SerializeField, HideInInspector] private float cursorVelocityLagSeconds = 0.12f;
    [SerializeField, HideInInspector] private float trailSubsampleDistance = 0.016f;
    [SerializeField, HideInInspector] private Color[] inkPalette =
    {
        new Color(0.08f, 1.00f, 0.45f, 1f),
        new Color(0.36f, 1.00f, 0.08f, 1f),
        new Color(0.95f, 0.95f, 0.05f, 1f),
        new Color(0.00f, 0.92f, 1.00f, 1f),
        new Color(1.00f, 0.42f, 0.18f, 1f)
    };
    [SerializeField, HideInInspector] private float inkColorCycleSeconds = 2f;
    [SerializeField, HideInInspector] private float inkFadeSeconds = 2f;
    [SerializeField, HideInInspector] private float inkSplatForce = 1.65f;
    [SerializeField, HideInInspector] private float inkAngularVelocityMin = -1.15f;
    [SerializeField, HideInInspector] private float inkAngularVelocityMax = 1.45f;
    [SerializeField, HideInInspector] private float inkRadiusDrift = 0.42f;
    [SerializeField, HideInInspector] private float inkSpeedDrift = 0.35f;
    [SerializeField, HideInInspector] private float inkChaos = 0.8f;
    [SerializeField, HideInInspector] private float inkDiffusion = 0.45f;
    [SerializeField, HideInInspector] private float inkBrightness = 1.55f;
    [SerializeField, HideInInspector] private float inkSoftness = 1.7f;
    [SerializeField, HideInInspector] private float inkNoiseStrength = 0.7f;
    [SerializeField, HideInInspector] private float inkNoiseScale = 4.8f;
    [SerializeField, HideInInspector] private float inkTrailStretch = 2.7f;
    [SerializeField, HideInInspector] private float inkWhiteCore = 0.42f;
    [SerializeField, HideInInspector] private float inkBurstSize = 1.15f;

    [SerializeField, HideInInspector] private bool showExternalCameraAsBackground = false;
    [SerializeField, HideInInspector] private bool debugBackgroundFlipX;
    [SerializeField, HideInInspector] private bool debugBackgroundFlipY;

    [Header("Display 2 Camera")]
    [Tooltip("Camera name substring for Display 2 MediaPipe tracking. Leave empty to use automatic external camera selection.")]
    [SerializeField] private string mediaPipePreferredCameraNameContains = "C922 Pro Stream Webcam";
    [Tooltip("Fallback rule for garbled camera names: Display 2 will avoid this Display 1 camera and use another external camera.")]
    [SerializeField] private string mediaPipeAvoidCameraNameContains = "Logitech MeetUp";
    [Tooltip("-1 = choose by camera name/external order. Use only for temporary manual override.")]
    [SerializeField] private int mediaPipeCameraDeviceIndex = -1;
    [SerializeField] private int mediaPipeExternalCameraOrdinal = 0;

    [Header("Hand Tracking")]
    [SerializeField] private bool useHandTrackingPosition = true;
    [SerializeField] private bool useMediaPipeHandTracking = true;
    [SerializeField] private bool mediaPipeHandVisibleTriggersRipple = true;
    [SerializeField] private bool useMousePositionWhenHandMissing = true;
    [SerializeField] private float repeatRippleWhileTouching = 0f;
    [SerializeField] private bool handTrackingRotatePosition180;
    [SerializeField] private bool handTrackingFlipX;
    [SerializeField] private bool handTrackingFlipY;
    [SerializeField, Range(0f, 1f)] private float handTrackingSmoothing = 0.04f;

    private PastelClassicRippleController pastelProjection;
    private Display2RibbonTrailController ribbonProjection;
    private Display2CameraDebugBackground debugBackground;
    private Display2MediaPipeHandTracker mediaPipeHandTracking;
    private HandTrackingUdpReceiver handTracking;
    private bool wasTouching;
    private float lastRippleTime = -1000f;
    private readonly Vector2[] handPositionBuffer = new Vector2[4];

    private void OnValidate()
    {
        if (Application.isPlaying && ribbonProjection != null)
        {
            ApplyRibbonProjectionProperties();
        }
    }

    private void Start()
    {
        ActivateDisplay(projectorDisplayIndex);

        if (createPastelProjection)
        {
            SetupPastelProjection();
        }

        if (createRibbonProjection)
        {
            SetupRibbonProjection();
        }

        if (useMediaPipeHandTracking)
        {
            mediaPipeHandTracking = Display2MediaPipeHandTracker.Instance;
            if (mediaPipeHandTracking == null)
            {
                mediaPipeHandTracking = gameObject.AddComponent<Display2MediaPipeHandTracker>();
            }

            mediaPipeHandTracking.ConfigureCameraSelection(
                mediaPipePreferredCameraNameContains,
                mediaPipeAvoidCameraNameContains,
                mediaPipeCameraDeviceIndex,
                mediaPipeExternalCameraOrdinal);

            mediaPipeHandTracking.ConfigurePositionMapping(
                handTrackingRotatePosition180,
                handTrackingFlipX,
                handTrackingFlipY,
                handTrackingSmoothing);
        }

        if (showExternalCameraAsBackground)
        {
            SetupExternalCameraDebugBackground();
        }

        handTracking = HandTrackingUdpReceiver.Instance;
        if (handTracking == null && !useMediaPipeHandTracking)
        {
            handTracking = gameObject.AddComponent<HandTrackingUdpReceiver>();
        }
    }

    private void Update()
    {
        if (pastelProjection == null && ribbonProjection == null)
        {
            return;
        }

        DeviceInputManager input = DeviceInputManager.Instance;
        bool isTouching = input != null && input.isTouching == 1;

        bool canUseMediaPipePosition = useHandTrackingPosition &&
            useMediaPipeHandTracking &&
            mediaPipeHandTracking != null &&
            mediaPipeHandTracking.HandVisible;
        bool canUseUdpPosition = useHandTrackingPosition &&
            handTracking != null &&
            handTracking.HandVisible;
        bool canUseHandPosition = canUseMediaPipePosition || canUseUdpPosition;
        int handCount = 0;

        if (canUseMediaPipePosition)
        {
            handCount = mediaPipeHandTracking.CopyNormalizedPositions(handPositionBuffer);
        }
        else if (canUseUdpPosition)
        {
            handPositionBuffer[0] = handTracking.NormalizedPosition;
            handCount = 1;
        }

        if (mediaPipeHandVisibleTriggersRipple && canUseMediaPipePosition)
        {
            isTouching = true;
        }

        bool hardwareInputActive = input != null && input.useHardwareInput;
        bool manualTrigger = Application.isEditor && !hardwareInputActive && Input.GetKey(manualTriggerKey);
        if (manualTrigger)
        {
            isTouching = true;
        }

        bool canUseMousePosition = useMousePositionWhenHandMissing &&
            !canUseHandPosition &&
            Application.isEditor;

        if (isTouching && canUseMousePosition)
        {
            handPositionBuffer[0] = GetMouseNormalizedPosition();
            handCount = 1;
        }
        else if (manualTrigger && handCount == 0)
        {
            handPositionBuffer[0] = GetMouseNormalizedPosition();
            handCount = 1;
        }

        int interactionCount = isTouching ? Mathf.Min(handCount, 2) : 0;
        if (pastelProjection != null)
        {
            pastelProjection.SetInteractionHands(handPositionBuffer, interactionCount);
        }

        if (ribbonProjection != null)
        {
            ApplyRibbonProjectionProperties();
            ribbonProjection.SetInteractionHands(handPositionBuffer, interactionCount);
        }

        bool canRepeatBurst = repeatRippleWhileTouching > 0f && Time.time - lastRippleTime >= repeatRippleWhileTouching;

        if (isTouching && handCount > 0 && (!wasTouching || canRepeatBurst))
        {
            for (int i = 0; i < handCount; i++)
            {
                if (pastelProjection != null)
                {
                    pastelProjection.TriggerRipple(handPositionBuffer[i]);
                }
            }

            lastRippleTime = Time.time;
        }
        else if (isTouching && !wasTouching)
        {
            if (pastelProjection != null)
            {
                pastelProjection.TriggerRipple();
            }
            lastRippleTime = Time.time;
        }

        wasTouching = isTouching;
    }

    private Vector2 GetMouseNormalizedPosition()
    {
        float x = Screen.width > 0 ? Input.mousePosition.x / Screen.width : 0.5f;
        float y = Screen.height > 0 ? Input.mousePosition.y / Screen.height : 0.5f;
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    private void ActivateDisplay(int displayIndex)
    {
        if (displayIndex <= 0 || Display.displays.Length <= displayIndex)
        {
            Debug.Log($"[ActivateProjector] Display {displayIndex + 1} is not available. Build will use the primary display until a second display is connected.");
            return;
        }

        Display.displays[displayIndex].Activate();
        Debug.Log($"[ActivateProjector] Activated Display {displayIndex + 1}.");
    }

    private void SetupPastelProjection()
    {
        Camera targetCamera = projectorCamera != null ? projectorCamera : FindProjectorCamera();

        if (targetCamera == null)
        {
            Debug.LogWarning("[ActivateProjector] Could not find a camera assigned to Display 2, so the pastel projection was not created.");
            return;
        }

        targetCamera.targetDisplay = projectorDisplayIndex;
        targetCamera.orthographic = true;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        GameObject projectionObject = GameObject.Find("Display 2 Pastel Projection");
        if (projectionObject == null)
        {
            projectionObject = new GameObject("Display 2 Pastel Projection");
        }

        int projectionLayer = LayerMask.NameToLayer("ProjectionContent");
        if (projectionLayer >= 0)
        {
            projectionObject.layer = projectionLayer;
            targetCamera.cullingMask = 1 << projectionLayer;
        }

        pastelProjection = projectionObject.GetComponent<PastelClassicRippleController>();
        if (pastelProjection == null)
        {
            pastelProjection = projectionObject.AddComponent<PastelClassicRippleController>();
        }

        pastelProjection.targetCamera = targetCamera;
        pastelProjection.triggerKey = KeyCode.None;
        pastelProjection.backgroundColor = backgroundColor;
        pastelProjection.strokeOpacity = strokeOpacity;
        pastelProjection.duration = rippleDuration;
        pastelProjection.maxRadius = maxRippleRadius;
        pastelProjection.grainStrength = grainStrength;
        pastelProjection.flowSpeed = flowSpeed;
        pastelProjection.trailLength = trailLength;
        pastelProjection.bloomBoost = bloomBoost;
        pastelProjection.handInfluence = handInfluence;
        pastelProjection.particleDensity = particleDensity;
        pastelProjection.interactionRadius = interactionRadius;
        pastelProjection.continuousTrailStrength = continuousTrailStrength;
        pastelProjection.rainbowMode = rainbowMode;
        pastelProjection.cursorLagSeconds = cursorLagSeconds;
        pastelProjection.cursorVelocityLagSeconds = cursorVelocityLagSeconds;
        pastelProjection.trailSubsampleDistance = trailSubsampleDistance;
        pastelProjection.inkPalette = inkPalette;
        pastelProjection.inkColorCycleSeconds = inkColorCycleSeconds;
        pastelProjection.inkFadeSeconds = inkFadeSeconds;
        pastelProjection.inkSplatForce = inkSplatForce;
        pastelProjection.inkAngularVelocityRange = new Vector2(inkAngularVelocityMin, inkAngularVelocityMax);
        pastelProjection.inkRadiusDrift = inkRadiusDrift;
        pastelProjection.inkSpeedDrift = inkSpeedDrift;
        pastelProjection.inkChaos = inkChaos;
        pastelProjection.inkDiffusion = inkDiffusion;
        pastelProjection.inkBrightness = inkBrightness;
        pastelProjection.inkSoftness = inkSoftness;
        pastelProjection.inkNoiseStrength = inkNoiseStrength;
        pastelProjection.inkNoiseScale = inkNoiseScale;
        pastelProjection.inkTrailStretch = inkTrailStretch;
        pastelProjection.inkWhiteCore = inkWhiteCore;
        pastelProjection.inkBurstSize = inkBurstSize;
        pastelProjection.cameraHeight = cameraHeight;
        pastelProjection.projectionHeight = projectionHeight;
    }

    private void SetupRibbonProjection()
    {
        Camera targetCamera = projectorCamera != null ? projectorCamera : FindProjectorCamera();

        if (targetCamera == null)
        {
            Debug.LogWarning("[ActivateProjector] Could not find a camera assigned to Display 2, so the soft ribbon projection was not created.");
            return;
        }

        targetCamera.targetDisplay = projectorDisplayIndex;
        targetCamera.orthographic = true;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = Color.black;

        GameObject ribbonObject = GameObject.Find("Display 2 Soft Ribbon Projection");
        if (ribbonObject == null)
        {
            ribbonObject = new GameObject("Display 2 Soft Ribbon Projection");
        }

        int projectionLayer = LayerMask.NameToLayer("ProjectionContent");
        if (projectionLayer >= 0)
        {
            ribbonObject.layer = projectionLayer;
            targetCamera.cullingMask = 1 << projectionLayer;
        }

        ribbonProjection = ribbonObject.GetComponent<Display2RibbonTrailController>();
        if (ribbonProjection == null)
        {
            ribbonProjection = ribbonObject.AddComponent<Display2RibbonTrailController>();
        }

        ribbonProjection.targetCamera = targetCamera;
        ApplyRibbonProjectionProperties();
    }

    private void ApplyRibbonProjectionProperties()
    {
        if (ribbonProjection == null)
        {
            return;
        }

        ribbonProjection.targetCamera = projectorCamera != null ? projectorCamera : FindProjectorCamera();
        ribbonProjection.rainbowMode = rainbowMode;
        ribbonProjection.cameraHeight = cameraHeight;
        ribbonProjection.projectionHeight = projectionHeight + 0.02f;
        ribbonProjection.ribbonScale = ribbonScale;
        ribbonProjection.glowIntensity = ribbonGlowIntensity;
        ribbonProjection.followLagSeconds = ribbonFollowLagSeconds;
        ribbonProjection.fadeSeconds = ribbonFadeSeconds;
        ribbonProjection.enableParticleRibbon = enableParticleRibbon;
        ribbonProjection.enableTrailRendererRibbon = enableTrailRendererRibbon;
        ribbonProjection.particleRibbonMaxParticles = particleRibbonMaxParticles;
        ribbonProjection.particleRibbonAmount = particleRibbonAmount;
        ribbonProjection.particleRibbonSize = particleRibbonSize;
        ribbonProjection.particleRibbonLifetime = particleRibbonLifetime;
        ribbonProjection.particleRibbonForce = particleRibbonForce;
        ribbonProjection.particleRibbonBackflow = particleRibbonBackflow;
        ribbonProjection.particleRibbonSpread = particleRibbonSpread;
        ribbonProjection.particleRibbonNoise = particleRibbonNoise;
        ribbonProjection.particleRibbonHeadBrightness = particleRibbonHeadBrightness;
        ribbonProjection.particleRibbonTailOpacity = particleRibbonTailOpacity;
        ribbonProjection.enableRibbonColorCycle = enableRibbonColorCycle;
        ribbonProjection.ribbonColorCycleSeconds = ribbonColorCycleSeconds;
        ribbonProjection.ribbonColorCyclePalette = ribbonColorCyclePalette;
        ribbonProjection.ribbonHeadSize = ribbonHeadSize;
        ribbonProjection.ribbonHeadBulge = ribbonHeadBulge;
        ribbonProjection.ribbonHeadSoftness = ribbonHeadSoftness;
        ribbonProjection.ribbonSpeedStretch = ribbonSpeedStretch;
        ribbonProjection.ribbonForce = ribbonForce;
        ribbonProjection.ribbonHeadBrightness = ribbonHeadBrightness;
        ribbonProjection.ribbonTailBrightness = ribbonTailBrightness;
        ribbonProjection.ribbonTailAlpha = ribbonTailAlpha;
        ribbonProjection.ribbonColorVariation = ribbonColorVariation;
        ribbonProjection.enableMistParticles = enableMistParticles;
        ribbonProjection.mistMaxParticles = mistMaxParticles;
        ribbonProjection.mistAmount = mistAmount;
        ribbonProjection.mistSize = mistSize;
        ribbonProjection.mistLifetime = mistLifetime;
        ribbonProjection.mistSpeed = mistSpeed;
        ribbonProjection.mistSpread = mistSpread;
        ribbonProjection.mistDrift = mistDrift;
        ribbonProjection.mistNoiseStrength = mistNoiseStrength;
        ribbonProjection.mistOpacity = mistOpacity;
        ribbonProjection.mistTint = mistTint;
        ribbonProjection.mistCoreRadius = mistCoreRadius;
        ribbonProjection.mistHaloRadius = mistHaloRadius;
        ribbonProjection.mistSdfSoftness = mistSdfSoftness;
        ribbonProjection.mistCorePower = mistCorePower;
        ribbonProjection.mistHaloPower = mistHaloPower;
        ribbonProjection.mistEmissionPower = mistEmissionPower;
        ribbonProjection.mistFbmScale = mistFbmScale;
        ribbonProjection.mistFbmFlowSpeed = mistFbmFlowSpeed;
        ribbonProjection.mistAlphaPower = mistAlphaPower;
        ribbonProjection.mistFlowColorA = mistFlowColorA;
        ribbonProjection.mistFlowColorB = mistFlowColorB;
        ribbonProjection.enableShakeWaveBurst = enableShakeWaveBurst;
        ribbonProjection.shakeWaveResourcePath = shakeWaveResourcePath;
        ribbonProjection.shakeWaveScale = shakeWaveScale;
        ribbonProjection.shakeWaveLifetime = shakeWaveLifetime;
        ribbonProjection.shakeWaveBrightness = shakeWaveBrightness;
        ribbonProjection.shakeWaveColor = shakeWaveColor;

    }

    private void SetupExternalCameraDebugBackground()
    {
        Camera targetCamera = projectorCamera != null ? projectorCamera : FindProjectorCamera();
        if (targetCamera == null)
        {
            return;
        }

        GameObject backgroundObject = GameObject.Find("Display 2 Camera Debug Background");
        if (backgroundObject == null)
        {
            backgroundObject = new GameObject("Display 2 Camera Debug Background");
        }

        int projectionLayer = LayerMask.NameToLayer("ProjectionContent");
        if (projectionLayer >= 0)
        {
            backgroundObject.layer = projectionLayer;
        }

        debugBackground = backgroundObject.GetComponent<Display2CameraDebugBackground>();
        if (debugBackground == null)
        {
            debugBackground = backgroundObject.AddComponent<Display2CameraDebugBackground>();
        }

        debugBackground.targetCamera = targetCamera;
        debugBackground.handTracker = mediaPipeHandTracking;
        debugBackground.cameraHeight = cameraHeight + 0.05f;
        debugBackground.projectionHeight = projectionHeight - 0.01f;
        debugBackground.matchHandTrackerOrientation = true;
        debugBackground.flipX = debugBackgroundFlipX;
        debugBackground.flipY = debugBackgroundFlipY;
    }

    private Camera FindProjectorCamera()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera camera in cameras)
        {
            if (camera.targetDisplay == projectorDisplayIndex)
            {
                return camera;
            }
        }

        GameObject projectorCameraObject = GameObject.Find("ProjectorCamera");
        return projectorCameraObject != null ? projectorCameraObject.GetComponent<Camera>() : null;
    }
}
