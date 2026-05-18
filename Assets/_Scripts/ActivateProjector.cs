using UnityEngine;

public class ActivateProjector : MonoBehaviour
{
    [Header("Display 2")]
    [SerializeField] private int projectorDisplayIndex = 1;
    [SerializeField] private Camera projectorCamera;

    [Header("Pastel Projection")]
    [SerializeField] private bool createPastelProjection = true;
    [SerializeField] private KeyCode manualTriggerKey = KeyCode.Space;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField, Range(0f, 2f)] private float strokeOpacity = 0.96f;
    [SerializeField, Range(0.2f, 4f)] private float rippleDuration = 2.2f;
    [SerializeField, Range(0.1f, 1.5f)] private float maxRippleRadius = 0.86f;
    [SerializeField, Range(0f, 1f)] private float grainStrength = 0.045f;
    [SerializeField] private float cameraHeight = 5f;
    [SerializeField] private float projectionHeight = 0f;

    [Header("Display 2 Debug Background")]
    [SerializeField] private bool showExternalCameraAsBackground = true;
    [SerializeField] private bool debugBackgroundFlipX;
    [SerializeField] private bool debugBackgroundFlipY;

    [Header("Hand Tracking")]
    [SerializeField] private bool useHandTrackingPosition = true;
    [SerializeField] private bool useMediaPipeHandTracking = true;
    [SerializeField] private bool mediaPipeHandVisibleTriggersRipple = true;
    [SerializeField] private bool useMousePositionWhenHandMissing = true;
    [SerializeField] private float repeatRippleWhileTouching = 0.45f;

    private PastelClassicRippleController pastelProjection;
    private Display2CameraDebugBackground debugBackground;
    private Display2MediaPipeHandTracker mediaPipeHandTracking;
    private HandTrackingUdpReceiver handTracking;
    private bool wasTouching;
    private float lastRippleTime = -1000f;

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

        if (mediaPipeHandVisibleTriggersRipple && canUseMediaPipePosition)
        {
            isTouching = true;
        }

        if (Application.isEditor && Input.GetKey(manualTriggerKey))
        {
            isTouching = true;
        }

        bool canUseMousePosition = useMousePositionWhenHandMissing &&
            !canUseHandPosition &&
            Application.isEditor;

        if (isTouching && (canUseHandPosition || canUseMousePosition) && (!wasTouching || Time.time - lastRippleTime >= repeatRippleWhileTouching))
        {
            Vector2 ripplePosition = canUseMediaPipePosition
                ? mediaPipeHandTracking.NormalizedPosition
                : canUseUdpPosition
                    ? handTracking.NormalizedPosition
                    : GetMouseNormalizedPosition();
            pastelProjection.TriggerRipple(ripplePosition);
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
