using UnityEngine;

public class ActivateProjector : MonoBehaviour
{
    [Header("Display 2")]
    [SerializeField] private int projectorDisplayIndex = 1;
    [SerializeField] private Camera projectorCamera;

    [Header("Display 2 Cinematic Particle Projection")]
    [SerializeField] private bool createPastelProjection = true;
    [SerializeField] private KeyCode manualTriggerKey = KeyCode.Space;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField, Range(0f, 3f)] private float strokeOpacity = 2.2f;
    [SerializeField, Range(0.2f, 5f)] private float rippleDuration = 2.8f;
    [SerializeField, Range(0.1f, 2.2f)] private float maxRippleRadius = 1.25f;
    [SerializeField, Range(0f, 1f)] private float grainStrength = 0.18f;
    [SerializeField, Range(0f, 4f)] private float flowSpeed = 0.72f;
    [SerializeField, Range(0.2f, 4f)] private float trailLength = 3.0f;
    [SerializeField, Range(0f, 12f)] private float bloomBoost = 8.5f;
    [SerializeField, Range(0f, 4f)] private float handInfluence = 2.25f;
    [SerializeField, Range(0.4f, 3f)] private float particleDensity = 1.95f;
    [SerializeField, Range(0.25f, 1.5f)] private float interactionRadius = 0.62f;
    [SerializeField, Range(0f, 4f)] private float continuousTrailStrength = 2.6f;
    [SerializeField] private float cameraHeight = 5f;
    [SerializeField] private float projectionHeight = 0f;

    [Header("Display 2 Debug Background")]
    [SerializeField] private bool showExternalCameraAsBackground = false;
    [SerializeField] private bool debugBackgroundFlipX;
    [SerializeField] private bool debugBackgroundFlipY;

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

        if (useMediaPipeHandTracking)
        {
            mediaPipeHandTracking = Display2MediaPipeHandTracker.Instance;
            if (mediaPipeHandTracking == null)
            {
                mediaPipeHandTracking = gameObject.AddComponent<Display2MediaPipeHandTracker>();
            }

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
        if (pastelProjection == null)
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

        pastelProjection.SetInteractionHands(handPositionBuffer, isTouching ? handCount : 0);

        bool canRepeatBurst = repeatRippleWhileTouching > 0f && Time.time - lastRippleTime >= repeatRippleWhileTouching;

        if (isTouching && handCount > 0 && (!wasTouching || canRepeatBurst))
        {
            for (int i = 0; i < handCount; i++)
            {
                pastelProjection.TriggerRipple(handPositionBuffer[i]);
            }

            lastRippleTime = Time.time;
        }
        else if (isTouching && !wasTouching)
        {
            pastelProjection.TriggerRipple();
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
        pastelProjection.cameraHeight = cameraHeight;
        pastelProjection.projectionHeight = projectionHeight;
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
