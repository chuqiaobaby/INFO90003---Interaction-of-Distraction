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

    private PastelClassicRippleController pastelProjection;
    private bool wasTouching;

    private void Start()
    {
        ActivateDisplay(projectorDisplayIndex);

        if (createPastelProjection)
        {
            SetupPastelProjection();
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

        if (isTouching && !wasTouching)
        {
            pastelProjection.TriggerRipple();
        }

        wasTouching = isTouching;
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
        pastelProjection.triggerKey = manualTriggerKey;
        pastelProjection.backgroundColor = backgroundColor;
        pastelProjection.strokeOpacity = strokeOpacity;
        pastelProjection.duration = rippleDuration;
        pastelProjection.maxRadius = maxRippleRadius;
        pastelProjection.grainStrength = grainStrength;
        pastelProjection.cameraHeight = cameraHeight;
        pastelProjection.projectionHeight = projectionHeight;
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
