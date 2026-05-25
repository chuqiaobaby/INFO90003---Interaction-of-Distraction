using UnityEngine;

public class ActivateProjector : MonoBehaviour
{
    [Header("Display 2")]
    [SerializeField] private int projectorDisplayIndex = 1;
    [SerializeField] private Camera projectorCamera;

    [Header("Display 2 Soft Ribbon Projection")]
    [SerializeField] private bool createRibbonProjection = true;
    [SerializeField, Range(0.2f, 2.5f)] private float ribbonScale = 1f;
    [SerializeField, Range(0.5f, 4f)] private float ribbonGlowIntensity = 1.6f;
    [SerializeField, Range(0.08f, 0.6f)] private float ribbonFollowLagSeconds = 0.18f;
    [SerializeField, Range(0.4f, 3.5f)] private float ribbonFadeSeconds = 1.65f;
    [SerializeField] private bool enableMistParticles = true;
    [SerializeField, Range(0f, 120f)] private float mistAmount = 34f;
    [SerializeField, Range(0.05f, 1.4f)] private float mistSize = 0.48f;
    [SerializeField, Range(0.2f, 4f)] private float mistLifetime = 1.75f;
    [SerializeField, Range(0f, 2f)] private float mistSpeed = 0.24f;
    [SerializeField, Range(0.02f, 1.2f)] private float mistSpread = 0.34f;
    [SerializeField, Range(0f, 2f)] private float mistDrift = 0.55f;
    [SerializeField, Range(0f, 2f)] private float mistNoiseStrength = 0.72f;
    [SerializeField, Range(0f, 1f)] private float mistOpacity = 0.34f;
    [SerializeField] private Color mistTint = new Color(0.10f, 1.00f, 0.60f, 1f);
    [SerializeField] private bool enableShakeWaveBurst = true;
    [SerializeField] private string shakeWaveResourcePath = "ShakeWaveI/Prefebs/shake_wave_1";
    [SerializeField, Range(0.2f, 4f)] private float shakeWaveScale = 1.35f;
    [SerializeField, Range(0.2f, 6f)] private float shakeWaveLifetime = 2.8f;
    [SerializeField, Range(0f, 3f)] private float shakeWaveBrightness = 1.35f;
    [SerializeField] private Color shakeWaveColor = new Color(0.16f, 1.00f, 0.42f, 1f);

    [Header("Display 2 Cinematic Particle Projection")]
    [SerializeField] private bool createPastelProjection = false;
    [SerializeField] private KeyCode manualTriggerKey = KeyCode.Space;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField, Range(0f, 3f)] private float strokeOpacity = 2.2f;
    [SerializeField, Range(0.2f, 5f)] private float rippleDuration = 2.8f;
    [SerializeField, Range(0.1f, 2.2f)] private float maxRippleRadius = 1.25f;
    [SerializeField, Range(0f, 1f)] private float grainStrength = 0.18f;
    [SerializeField, Range(0f, 4f)] private float flowSpeed = 0.72f;
    [SerializeField, Range(0.2f, 4f)] private float trailLength = 3.0f;
    [SerializeField, Range(0f, 12f)] private float bloomBoost = 6.5f;
    [SerializeField, Range(0f, 4f)] private float handInfluence = 1.6f;
    [SerializeField, Range(0.4f, 3f)] private float particleDensity = 1.95f;
    [SerializeField, Range(0.25f, 1.5f)] private float interactionRadius = 0.62f;
    [SerializeField, Range(0f, 4f)] private float continuousTrailStrength = 0.8f;
    [Header("Display 2 Ribbon Cursor Feel")]
    [SerializeField] private bool rainbowMode;
    [SerializeField, Range(0.02f, 0.35f)] private float cursorLagSeconds = 0.16f;
    [SerializeField, Range(0.02f, 0.35f)] private float cursorVelocityLagSeconds = 0.12f;
    [SerializeField, Range(0f, 0.08f)] private float trailSubsampleDistance = 0.016f;
    [Header("Display 2 Touch Ink")]
    [SerializeField] private Color[] inkPalette =
    {
        new Color(0.08f, 1.00f, 0.45f, 1f),
        new Color(0.36f, 1.00f, 0.08f, 1f),
        new Color(0.95f, 0.95f, 0.05f, 1f),
        new Color(0.00f, 0.92f, 1.00f, 1f),
        new Color(1.00f, 0.42f, 0.18f, 1f)
    };
    [SerializeField, Min(0.1f)] private float inkColorCycleSeconds = 2f;
    [SerializeField, Range(0.5f, 4f)] private float inkFadeSeconds = 2f;
    [SerializeField, Range(0f, 4f)] private float inkSplatForce = 1.65f;
    [SerializeField, Range(-4f, 4f)] private float inkAngularVelocityMin = -1.15f;
    [SerializeField, Range(-4f, 4f)] private float inkAngularVelocityMax = 1.45f;
    [SerializeField, Range(0f, 1.5f)] private float inkRadiusDrift = 0.42f;
    [SerializeField, Range(0f, 2f)] private float inkSpeedDrift = 0.35f;
    [SerializeField, Range(0f, 3f)] private float inkChaos = 0.8f;
    [SerializeField, Range(0f, 3f)] private float inkDiffusion = 0.45f;
    [SerializeField, Range(0.2f, 4f)] private float inkBrightness = 1.55f;
    [SerializeField, Range(0.2f, 4f)] private float inkSoftness = 1.7f;
    [SerializeField, Range(0f, 3f)] private float inkNoiseStrength = 0.7f;
    [SerializeField, Range(0.5f, 12f)] private float inkNoiseScale = 4.8f;
    [SerializeField, Range(0.2f, 5f)] private float inkTrailStretch = 2.7f;
    [SerializeField, Range(0f, 2f)] private float inkWhiteCore = 0.42f;
    [SerializeField, Range(0.3f, 3f)] private float inkBurstSize = 1.15f;
    [SerializeField] private float cameraHeight = 5f;
    [SerializeField] private float projectionHeight = 0f;

    [Header("Display 2 Debug Background")]
    [SerializeField] private bool showExternalCameraAsBackground = false;
    [SerializeField] private bool debugBackgroundFlipX;
    [SerializeField] private bool debugBackgroundFlipY;

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

        bool manualTrigger = Application.isEditor && Input.GetKey(manualTriggerKey);
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
        ribbonProjection.enableMistParticles = enableMistParticles;
        ribbonProjection.mistAmount = mistAmount;
        ribbonProjection.mistSize = mistSize;
        ribbonProjection.mistLifetime = mistLifetime;
        ribbonProjection.mistSpeed = mistSpeed;
        ribbonProjection.mistSpread = mistSpread;
        ribbonProjection.mistDrift = mistDrift;
        ribbonProjection.mistNoiseStrength = mistNoiseStrength;
        ribbonProjection.mistOpacity = mistOpacity;
        ribbonProjection.mistTint = mistTint;
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
