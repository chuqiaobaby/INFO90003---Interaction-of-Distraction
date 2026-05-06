using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VFX controller.
/// Touch effect = world-space Quad with LiquidGlassAnomaly shader.
/// Other effects (color overlay, grounding border, camera shake, blow) are unchanged.
/// </summary>
public class PlaceholderVFXController : MonoBehaviour
{
    // ── UI References ─────────────────────────────────────────────────────────

    [Header("UI References  [leave empty → auto-created]")]
    public Image      colorOverlay;
    public Image      groundingBorder;
    public CanvasGroup vfxGroup;
    public Camera     mainCamera;

    // ── Water Level Colors ────────────────────────────────────────────────────

    [Header("Water Level Overlay Colors")]
    public Color level1Color = new Color(0.00f, 0.50f, 1.00f, 0.15f);
    public Color level2Color = new Color(1.00f, 0.20f, 0.20f, 0.30f);
    public Color level3Color = new Color(1.00f, 0.05f, 0.05f, 0.55f);

    // ── Grounding Border Colors ───────────────────────────────────────────────

    [Header("Grounding Border Colors")]
    public Color groundingColor = new Color(0.00f, 1.00f, 0.816f, 1.00f);
    public Color shieldedColor  = new Color(1.00f, 0.90f, 0.30f,  1.00f);

    // ── Liquid Glass Touch Effect ─────────────────────────────────────────────

    [Header("Touch Effect  —  Liquid Glass Anomaly")]
    [Tooltip("Material using Custom/LiquidGlassAnomaly shader. " +
             "Drag LiquidGlassAnomaly_Mat from the Project window here.")]
    public Material liquidGlassMaterial;

    [Tooltip("Size of the effect in screen pixels (width × height).")]
    public Vector2 touchEffectPixelSize = new Vector2(380f, 380f);

    [Tooltip("Distance in front of the camera in metres (must be > Near Clip Plane).")]
    public float touchEffectDepth = 2.0f;

    [Tooltip("Screen edge margin for random spawn (0 = edge, 0.2 = 20% inset each side).")]
    [Range(0f, 0.45f)]
    public float touchEffectPadding = 0.18f;

    [Tooltip("Peak opacity applied to the shader when a touch is first detected.")]
    [Range(0f, 1f)]
    public float touchGlassMaxOpacity = 0.95f;

    // ── Camera Shake ──────────────────────────────────────────────────────────

    [Header("Camera Shake  (Level 3 only)")]
    public float shakeAmount = 4f;
    public float shakeSpeed  = 15f;

    // ── Blow Fade ─────────────────────────────────────────────────────────────

    [Header("Blow Fade-Out")]
    public float blowFadeDuration = 0.5f;

    // ── Debug ─────────────────────────────────────────────────────────────────

    [Header("Debug")]
    [Tooltip("Shows a real-time status panel in the Game view during Play mode.")]
    public bool showDebugOverlay = true;

    // ── Internal ──────────────────────────────────────────────────────────────

    private enum VFXState { Normal, Grounding, Shielded }
    private VFXState vfxState = VFXState.Normal;

    private DeviceInputManager hw;
    private DistractionManager dm;

    private float groundingTimer    = 0f;
    private float groundingDuration = 5f;
    private float backtrackSpeed    = 1f;

    private Vector3 camOrigin;
    private bool    blowInProgress = false;
    private int     levelAtBlow    = -1;

    // Touch glass
    private GameObject   touchGlassGO;
    private MeshRenderer touchGlassMR;
    private Material     touchGlassMat;
    private float        touchGlassAlpha = 0f;
    private int          prevIsTouching  = 0;

    // Keyboard-fallback level (persists between frames when hw is null)
    private int          _kbLevel        = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        hw = DeviceInputManager.Instance;
        dm = DistractionManager.Instance;

        if (dm != null)
        {
            groundingDuration = dm.groundingDuration;
            backtrackSpeed    = dm.backtrackSpeed;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) camOrigin  = mainCamera.transform.localPosition;

        SetupUI();
        SetupTouchGlass();
        ResetUI();

        if (groundingBorder != null)
        {
            groundingBorder.type       = Image.Type.Filled;
            groundingBorder.fillMethod = Image.FillMethod.Horizontal;
            groundingBorder.fillOrigin = (int)Image.OriginHorizontal.Left;
            groundingBorder.fillAmount = 0f;
            groundingBorder.enabled    = false;
        }
    }

    void OnDestroy()
    {
        if (touchGlassMat != null) Destroy(touchGlassMat);
        if (touchGlassGO  != null) Destroy(touchGlassGO);
    }

    // ── Canvas UI Setup ───────────────────────────────────────────────────────

    void SetupUI()
    {
        Canvas canvas = FindOrCreateCanvas();

        if (vfxGroup == null)
            vfxGroup = BuildVFXGroup(canvas.gameObject);
        ForceStretch(vfxGroup.GetComponent<RectTransform>());

        GameObject root = vfxGroup.gameObject;

        if (colorOverlay == null)
            colorOverlay = BuildFullScreenImage(root, "ColorOverlay", Color.clear);
        else
            ForceStretch(colorOverlay.rectTransform);

        if (groundingBorder == null)
            groundingBorder = BuildGroundingBorder(root, "GroundingBorder");
        else
        {
            SetProgressBarRect(groundingBorder.rectTransform);
            if (groundingBorder.sprite == null)
                groundingBorder.sprite = Sprite.Create(
                    Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            groundingBorder.type       = Image.Type.Filled;
            groundingBorder.fillMethod = Image.FillMethod.Horizontal;
            groundingBorder.fillOrigin = (int)Image.OriginHorizontal.Left;
            groundingBorder.fillAmount = 0f;
        }
    }

    // ── Liquid Glass Touch Effect Setup ───────────────────────────────────────

    void SetupTouchGlass()
    {
        if (liquidGlassMaterial == null)
        {
            Debug.LogWarning("[VFX] Liquid Glass Material is NOT assigned! " +
                             "Drag 'LiquidGlassAnomaly_Mat' from _Shader/ onto this component.");
            return;
        }

        touchGlassGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        touchGlassGO.name = "TouchGlassEffect";
        Destroy(touchGlassGO.GetComponent<MeshCollider>());

        // Parent to camera so the Quad lives in the same render pass.
        // This fixes invisibility caused by URP camera stacking (overlay cameras
        // compositing on top of the base camera's transparent pass).
        Camera setupCam = mainCamera != null ? mainCamera : Camera.main;
        if (setupCam != null)
            touchGlassGO.transform.SetParent(setupCam.transform, false);
        else
            Debug.LogWarning("[VFX] No camera found during setup — Quad is unparented.");

        touchGlassMR = touchGlassGO.GetComponent<MeshRenderer>();
        touchGlassMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        touchGlassMR.receiveShadows    = false;

        touchGlassMat         = new Material(liquidGlassMaterial);
        touchGlassMR.material = touchGlassMat;

        touchGlassMat.SetFloat("_Opacity", 0f);
        touchGlassGO.SetActive(false);

        Debug.Log("[VFX] TouchGlassEffect Quad created successfully.");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        // ── Retry finding managers every frame until found ──────────────────
        // Fixes the case where DeviceInputManager.Awake() runs after Start().
        if (hw == null) hw = DeviceInputManager.Instance;
        if (dm == null) dm = DistractionManager.Instance;

        // Sync grounding params if dm just became available
        if (dm != null && groundingDuration != dm.groundingDuration)
        {
            groundingDuration = dm.groundingDuration;
            backtrackSpeed    = dm.backtrackSpeed;
        }

        if (blowInProgress) return;

        // ── Read inputs ─────────────────────────────────────────────────────
        // Primary: DeviceInputManager. Fallback: direct keyboard (keyboard mode
        // works even if DeviceInputManager is missing from the scene, so you can
        // always test by pressing Space).
        int isTouching  = GetInputTouching();
        int isGrounding = GetInputGrounding();
        int isBlowing   = GetInputBlowing();
        int level       = GetInputLevel();

        TickStateMachine(isGrounding, isBlowing, level);

        bool frozen = (vfxState == VFXState.Shielded);

        if (!frozen) UpdateColorOverlay(level);
        if (!frozen) UpdateTouchGlass(isTouching);
        if (!frozen) UpdateCameraShake(level);

        UpdateBorder();
    }

    // ── Input helpers (DeviceInputManager with keyboard fallback) ─────────────

    int GetInputTouching()
    {
        if (hw != null) return hw.isTouching;
        return Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    int GetInputGrounding()
    {
        if (hw != null) return hw.isGrounding;
        return Input.GetKey(KeyCode.Return) ? 1 : 0;
    }

    int GetInputBlowing()
    {
        if (hw != null) return hw.isBlowing;
        return Input.GetKeyDown(KeyCode.B) ? 1 : 0;
    }

    int GetInputLevel()
    {
        if (hw != null) return hw.Level;
        // Keyboard fallback: level persists until a new key is pressed
        if      (Input.GetKeyDown(KeyCode.Alpha0)) _kbLevel = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha1)) _kbLevel = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) _kbLevel = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) _kbLevel = 3;
        return _kbLevel;
    }

    // ── State Machine ─────────────────────────────────────────────────────────

    void TickStateMachine(int isGrounding, int isBlowing, int level)
    {
        switch (vfxState)
        {
            case VFXState.Normal:
                if (isGrounding == 1) vfxState = VFXState.Grounding;
                break;

            case VFXState.Grounding:
                float delta = isGrounding == 1
                    ? Time.deltaTime
                    : -backtrackSpeed * Time.deltaTime;

                groundingTimer = Mathf.Clamp(groundingTimer + delta, 0f, groundingDuration);

                if (groundingTimer >= groundingDuration)
                    EnterShielded();
                else if (groundingTimer <= 0f && isGrounding == 0)
                    vfxState = VFXState.Normal;
                break;

            case VFXState.Shielded:
                if (isBlowing == 1 && !blowInProgress)
                {
                    blowInProgress = true;
                    levelAtBlow    = level;
                    StartCoroutine(BlowRoutine());
                }
                break;
        }
    }

    // ── Effect 1: Water Level Color Overlay ───────────────────────────────────

    void UpdateColorOverlay(int level)
    {
        if (colorOverlay == null) return;

        if (levelAtBlow >= 0)
        {
            if (level == levelAtBlow) { colorOverlay.color = Color.clear; return; }
            else                        levelAtBlow = -1;
        }

        Color target;
        switch (level)
        {
            case 1:  target = level1Color; break;
            case 2:  target = level2Color; break;
            case 3:  target = level3Color; break;
            default: target = Color.clear; break;
        }

        colorOverlay.color = Color.Lerp(colorOverlay.color, target, Time.deltaTime * 5f);
    }

    // ── Effect 2: Liquid Glass Touch Effect ───────────────────────────────────
    //
    //  Timer logic (identical to the original white-circle flash):
    //   • Rising edge of isTouching  → random screen position, opacity snaps to max
    //   • While touching             → opacity held at max
    //   • After release              → linear fade at dm.CurrentFadeTime rate
    //     (starts fast at 1.5 s, grows to 180 s as globalDistractionTimer accumulates)

    void UpdateTouchGlass(int isTouchingValue)
    {
        if (touchGlassGO == null || touchGlassMat == null) return;

        bool touching     = (isTouchingValue == 1);
        bool isRisingEdge = touching && (prevIsTouching == 0);
        prevIsTouching    = isTouchingValue;

        if (touching)
        {
            if (isRisingEdge) RandomizeTouchPosition();
            touchGlassAlpha = touchGlassMaxOpacity;
        }
        else
        {
            float fadeTime   = (dm != null) ? dm.CurrentFadeTime : 1.5f;
            float decaySpeed = touchGlassMaxOpacity / Mathf.Max(0.05f, fadeTime);
            touchGlassAlpha  = Mathf.MoveTowards(touchGlassAlpha, 0f, decaySpeed * Time.deltaTime);
        }

        // Apply alpha to shader and toggle GameObject active state
        touchGlassMat.SetFloat("_Opacity", touchGlassAlpha);
        bool shouldBeActive = touchGlassAlpha > 0.004f;
        if (touchGlassGO.activeSelf != shouldBeActive)
            touchGlassGO.SetActive(shouldBeActive);
    }

    // Positions and scales the Quad at a random viewport location.
    void RandomizeTouchPosition()
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) { Debug.LogWarning("[VFX] No camera found for touch position."); return; }

        float pad   = Mathf.Clamp(touchEffectPadding, 0f, 0.45f);
        float vx    = Random.Range(pad, 1f - pad);
        float vy    = Random.Range(pad, 1f - pad);
        float depth = Mathf.Max(cam.nearClipPlane + 0.05f, touchEffectDepth);

        // Convert desired pixel size → world-space size at 'depth'
        float halfFovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float vpWorldH   = 2f * Mathf.Tan(halfFovRad) * depth;
        float vpWorldW   = vpWorldH * cam.aspect;
        float scaleX     = (touchEffectPixelSize.x / Screen.width)  * vpWorldW;
        float scaleY     = (touchEffectPixelSize.y / Screen.height) * vpWorldH;

        // Local-space placement (Quad is a child of the camera).
        // (0, 0, depth) = straight ahead.  Offset by viewport fraction from centre.
        float localX = (vx - 0.5f) * vpWorldW;
        float localY = (vy - 0.5f) * vpWorldH;
        touchGlassGO.transform.localPosition = new Vector3(localX, localY, depth);
        touchGlassGO.transform.localRotation = Quaternion.identity;
        touchGlassGO.transform.localScale    = new Vector3(scaleX, scaleY, 1f);

        Vector3 worldPos = touchGlassGO.transform.position;
        Debug.Log($"[VFX] Touch glass spawned at viewport ({vx:F2}, {vy:F2})  " +
                  $"world {worldPos}  local ({localX:F3}, {localY:F3}, {depth:F3})  scale ({scaleX:F3}, {scaleY:F3})");
    }

    // ── Effect 3 / 4: Grounding Border ───────────────────────────────────────

    void UpdateBorder()
    {
        if (groundingBorder == null) return;

        if (vfxState == VFXState.Shielded)
        {
            groundingBorder.enabled    = true;
            groundingBorder.fillAmount = 1f;
            groundingBorder.color      = shieldedColor;
            return;
        }

        float fill  = groundingDuration > 0f ? Mathf.Clamp01(groundingTimer / groundingDuration) : 0f;
        bool  active = (vfxState == VFXState.Grounding) || (fill > 0.005f);
        groundingBorder.enabled    = active;
        groundingBorder.fillAmount = fill;
        groundingBorder.color      = groundingColor;
    }

    // ── Effect 5: Camera Shake (Level 3) ─────────────────────────────────────

    void UpdateCameraShake(int level)
    {
        if (mainCamera == null) return;

        if (level == 3)
        {
            float t = Time.time * shakeSpeed;
            mainCamera.transform.localPosition = camOrigin +
                new Vector3(Mathf.Sin(t), Mathf.Cos(t * 1.3f), 0f) * (shakeAmount * 0.01f);
        }
        else
        {
            mainCamera.transform.localPosition = Vector3.Lerp(
                mainCamera.transform.localPosition, camOrigin, Time.deltaTime * 10f);
        }
    }

    // ── Shielded state ────────────────────────────────────────────────────────

    void EnterShielded()
    {
        vfxState       = VFXState.Shielded;
        groundingTimer = groundingDuration;
        if (mainCamera != null) mainCamera.transform.localPosition = camOrigin;
    }

    // ── Blow: fade everything out ─────────────────────────────────────────────

    IEnumerator BlowRoutine()
    {
        float elapsed         = 0f;
        float startTouchAlpha = touchGlassAlpha;

        if (vfxGroup != null)
        {
            while (elapsed < blowFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Clamp01(elapsed / blowFadeDuration);
                vfxGroup.alpha = t;

                touchGlassAlpha = startTouchAlpha * t;
                if (touchGlassMat != null) touchGlassMat.SetFloat("_Opacity", touchGlassAlpha);

                yield return null;
            }
            vfxGroup.alpha = 0f;
        }
        else
        {
            Color startOverlay = colorOverlay    != null ? colorOverlay.color    : Color.clear;
            Color startBorder  = groundingBorder != null ? groundingBorder.color : Color.clear;

            while (elapsed < blowFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Clamp01(elapsed / blowFadeDuration);

                if (colorOverlay != null)
                    colorOverlay.color = new Color(
                        startOverlay.r, startOverlay.g, startOverlay.b, startOverlay.a * t);

                if (groundingBorder != null)
                    groundingBorder.color = new Color(
                        startBorder.r, startBorder.g, startBorder.b, startBorder.a * t);

                touchGlassAlpha = startTouchAlpha * t;
                if (touchGlassMat != null) touchGlassMat.SetFloat("_Opacity", touchGlassAlpha);

                yield return null;
            }
        }

        HardReset();
    }

    // ── Debug Overlay ─────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (!showDebugOverlay) return;

        // Semi-transparent background
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(8f, 8f, 280f, 210f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float y = 14f;
        string hwStatus  = hw  != null ? "OK" : "NULL  ← check scene";
        string dmStatus  = dm  != null ? "OK" : "NULL";
        string matStatus = liquidGlassMaterial != null ? "assigned" : "MISSING  ← drag mat here!";
        string parentName = (touchGlassGO != null && touchGlassGO.transform.parent != null)
                           ? touchGlassGO.transform.parent.name : "NONE";
        string goStatus  = touchGlassGO != null
                           ? (touchGlassGO.activeSelf ? $"ACTIVE (parent={parentName})" : $"inactive (parent={parentName})")
                           : "NULL  ← setup failed";

        GUI.Label(new Rect(14f, y, 270f, 20f), "[VFXController]"); y += 20f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"DeviceInputManager : {hwStatus}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"DistractionManager : {dmStatus}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"LiquidGlassMaterial: {matStatus}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"TouchGlassGO       : {goStatus}"); y += 18f;

        int touch = GetInputTouching();
        GUI.Label(new Rect(14f, y, 270f, 20f), $"isTouching (input) : {touch}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"touchGlassAlpha    : {touchGlassAlpha:F3}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"VFX state          : {vfxState}"); y += 18f;

        float camDepth = mainCamera != null ? mainCamera.farClipPlane : -1f;
        string opaqTex = "enable Opaque Texture in URP Asset!";
        GUI.color = Color.yellow;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"depth={touchEffectDepth:F1}  farClip={camDepth:F0}  {opaqTex}");
        GUI.color = Color.white;
        y += 18f;

        if (hw == null)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(14f, y, 270f, 20f), "Keyboard fallback active (no DeviceMgr)");
            GUI.color = Color.white;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Canvas FindOrCreateCanvas()
    {
        foreach (Canvas c in FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;

        GameObject go = new GameObject("VFXCanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    CanvasGroup BuildVFXGroup(GameObject canvasGO)
    {
        GameObject go = new GameObject("VFXGroup");
        go.transform.SetParent(canvasGO.transform, false);
        ForceStretch(go.AddComponent<RectTransform>());
        return go.AddComponent<CanvasGroup>();
    }

    Image BuildFullScreenImage(GameObject parent, string objName, Color startColor)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent.transform, false);
        Image img       = go.AddComponent<Image>();
        img.color       = startColor;
        img.raycastTarget = false;
        ForceStretch(img.rectTransform);
        return img;
    }

    Image BuildGroundingBorder(GameObject parent, string objName)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent.transform, false);

        Image img  = go.AddComponent<Image>();
        img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        img.color  = groundingColor;
        img.raycastTarget = false;
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 0f;
        img.enabled    = false;
        SetProgressBarRect(img.rectTransform);
        return img;
    }

    void ForceStretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void SetProgressBarRect(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, 12f);
    }

    void ResetUI()
    {
        if (colorOverlay    != null) colorOverlay.color = Color.clear;
        if (groundingBorder != null)
        {
            groundingBorder.fillAmount = 0f;
            groundingBorder.color      = groundingColor;
            groundingBorder.enabled    = false;
        }
        if (vfxGroup != null) vfxGroup.alpha = 1f;

        touchGlassAlpha = 0f;
        if (touchGlassMat != null) touchGlassMat.SetFloat("_Opacity", 0f);
        if (touchGlassGO  != null) touchGlassGO.SetActive(false);
    }

    void HardReset()
    {
        vfxState       = VFXState.Normal;
        groundingTimer = 0f;
        prevIsTouching = 0;

        ResetUI();

        if (mainCamera != null) mainCamera.transform.localPosition = camOrigin;
        if (vfxGroup   != null) vfxGroup.alpha = 1f;

        blowInProgress = false;
    }
}
