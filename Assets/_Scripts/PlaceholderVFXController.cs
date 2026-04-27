using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placeholder VFX controller.
/// All UI elements are auto-created at runtime — no Inspector drag-drop required.
/// Inspector fields act as optional overrides if you want to supply your own objects.
/// </summary>
public class PlaceholderVFXController : MonoBehaviour
{
    // ── Inspector References (all optional — auto-created if left empty) ───────

    [Header("UI References  [leave empty → auto-created at runtime]")]
    [Tooltip("Full-screen color tint for water level. Auto-created if empty.")]
    public Image colorOverlay;

    [Tooltip("Centered white circle for touch flash. Auto-created if empty.")]
    public Image flashOverlay;

    [Tooltip("Full-screen horizontal-fill image for grounding border. Auto-created if empty.")]
    public Image groundingBorder;

    [Tooltip("CanvasGroup parent used for blow fade-out. Auto-created if empty.")]
    public CanvasGroup vfxGroup;

    [Tooltip("Main Camera for Level 3 shake. Auto-detected (Camera.main) if empty.")]
    public Camera mainCamera;

    // ── Colors ─────────────────────────────────────────────────────────────────

    [Header("Water Level Overlay Colors")]
    public Color level1Color = new Color(0.00f, 0.50f, 1.00f, 0.15f); // Light blue
    public Color level2Color = new Color(1.00f, 0.20f, 0.20f, 0.30f); // Medium red
    public Color level3Color = new Color(1.00f, 0.05f, 0.05f, 0.55f); // Strong red

    [Header("Grounding Border Colors")]
    public Color groundingColor = new Color(0.00f, 1.00f, 0.816f, 1.00f); // #00FFD0
    public Color shieldedColor  = new Color(1.00f, 0.90f, 0.30f, 1.00f); // Gold

    // ── Touch Circle ───────────────────────────────────────────────────────────

    [Header("Touch Circle")]
    [Tooltip("Peak alpha of the circle when touching")]
    public float flashIntensity = 0.85f;

    [Tooltip("Diameter in pixels (auto-created circle only)")]
    public float touchCircleDiameter = 200f;

    [Tooltip("Fallback fade durations per level — used only when DistractionManager is absent")]
    public float[] flashFadeByLevel = { 0.3f, 1.5f, 4.0f, 10.0f };

    // ── Camera Shake ───────────────────────────────────────────────────────────

    [Header("Camera Shake  (Level 3 only)")]
    public float shakeAmount = 4f;
    public float shakeSpeed  = 15f;

    // ── Blow Fade ──────────────────────────────────────────────────────────────

    [Header("Blow Fade-Out")]
    public float blowFadeDuration = 0.5f;

    // ── Internal ───────────────────────────────────────────────────────────────

    private enum VFXState { Normal, Grounding, Shielded }
    private VFXState vfxState = VFXState.Normal;

    private HardwareSimulator hw;
    private DistractionManager dm;

    private float groundingTimer    = 0f;
    private float groundingDuration = 5f;
    private float backtrackSpeed    = 1f;

    private float   flashAlpha    = 0f;
    private Vector3 camOrigin;
    private bool    blowInProgress = false;
    private int     levelAtBlow    = -1;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        hw = HardwareSimulator.Instance;
        dm = DistractionManager.Instance;

        if (dm != null)
        {
            groundingDuration = dm.groundingDuration;
            backtrackSpeed    = dm.backtrackSpeed;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) camOrigin = mainCamera.transform.localPosition;

        SetupUI();
        ResetUI();

        if (groundingBorder != null)
        {
            groundingBorder.type       = Image.Type.Filled;
            groundingBorder.fillMethod = Image.FillMethod.Horizontal;
            groundingBorder.fillOrigin = (int)Image.OriginHorizontal.Left;
            groundingBorder.fillAmount = 0f;
            groundingBorder.enabled    = false;
            Debug.Log("[VFX] GroundingBorder type = " + groundingBorder.type
                      + ", fillAmount = " + groundingBorder.fillAmount);
        }
    }

    // ── UI Auto-Setup ─────────────────────────────────────────────────────────

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

        if (flashOverlay == null)
            flashOverlay = BuildTouchCircle(root, "TouchCircle");

        if (groundingBorder == null)
            groundingBorder = BuildGroundingBorder(root, "GroundingBorder");
        else
        {
            SetProgressBarRect(groundingBorder.rectTransform);
            
            // 【关键修复】：如果是 Inspector 拖进来的 Image，确保它有 Sprite，否则 Filled 模式会失效
            if (groundingBorder.sprite == null)
            {
                groundingBorder.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            }
            
            groundingBorder.type       = Image.Type.Filled;
            groundingBorder.fillMethod = Image.FillMethod.Horizontal;
            groundingBorder.fillOrigin = (int)Image.OriginHorizontal.Left;
            groundingBorder.fillAmount = 0f;
        }
    }

    // Finds a ScreenSpaceOverlay Canvas, or creates one.
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

    // Full-screen Image stretched to fill parent.
    Image BuildFullScreenImage(GameObject parent, string objName, Color startColor)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = startColor;
        img.raycastTarget = false;
        ForceStretch(img.rectTransform);
        return img;
    }

    // Centered circle using a procedurally generated 128×128 texture.
    Image BuildTouchCircle(GameObject parent, string objName)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent.transform, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        img.sprite = MakeCircleSprite(128);

        RectTransform rt = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(touchCircleDiameter, touchCircleDiameter);

        return img;
    }

    // Bottom-anchored progress bar (12 px tall, full screen width).
    Image BuildGroundingBorder(GameObject parent, string objName)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent.transform, false);

        Image img = go.AddComponent<Image>();
        
        // 【关键修复】：给代码生成的 Image 也塞一张纯白贴图
        img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        
        img.color = groundingColor;
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

    // Anchors the RectTransform to the bottom of the screen, 12 px tall.
    void SetProgressBarRect(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, 12f);
    }

    // Generates a 128×128 circular sprite in code — no external assets needed.
    Sprite MakeCircleSprite(int size)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     center = (size - 1) * 0.5f;
        float     radius = center;
        float     soft   = 2f; // pixel-wide anti-alias band at the edge

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = 1f - Mathf.Clamp01((dist - (radius - soft)) / soft);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (hw == null || blowInProgress) return;

        TickStateMachine();

        bool frozen = (vfxState == VFXState.Shielded);

        if (!frozen) UpdateColorOverlay();
        if (!frozen) UpdateFlash();
        if (!frozen) UpdateCameraShake();

        UpdateBorder();
    }

    // ── State Machine ─────────────────────────────────────────────────────────

    void TickStateMachine()
    {
        switch (vfxState)
        {
            case VFXState.Normal:
                if (hw.isGrounding == 1) vfxState = VFXState.Grounding;
                break;

            case VFXState.Grounding:
                float delta = hw.isGrounding == 1
                    ? Time.deltaTime
                    : -backtrackSpeed * Time.deltaTime;

                groundingTimer = Mathf.Clamp(groundingTimer + delta, 0f, groundingDuration);

                if (groundingTimer >= groundingDuration)
                    EnterShielded();
                else if (groundingTimer <= 0f && hw.isGrounding == 0)
                    vfxState = VFXState.Normal;
                break;

            case VFXState.Shielded:
                if (hw.isBlowing == 1 && !blowInProgress)
                {
                    blowInProgress = true;
                    levelAtBlow = hw.Level; // 【关键修复】：记录吹气清除时的 Level，传给特效层进行拦截
                    StartCoroutine(BlowRoutine());
                }
                break;
        }
    }


    // ── Effect 1: Water Level Color Overlay ───────────────────────────────────

    void UpdateColorOverlay()
    {
        if (colorOverlay == null) return;

        // 【关键修复】：如果当前 Level 还是吹气时的那个 Level，强制保持全透明
        if (levelAtBlow >= 0)
        {
            if (hw.Level == levelAtBlow)
            {
                colorOverlay.color = Color.clear; 
                return;
            }
            else
            {
                levelAtBlow = -1; // 用户改变了 Level，解除锁定，恢复正常逻辑
            }
        }

        Color target;
        switch (hw.Level)
        {
            case 1:  target = level1Color; break;
            case 2:  target = level2Color; break;
            case 3:  target = level3Color; break;
            default: target = Color.clear; break;
        }

        colorOverlay.color = Color.Lerp(colorOverlay.color, target, Time.deltaTime * 5f);
    }

    // ── Effect 2: Touch Circle Flash ──────────────────────────────────────────
    // Completely independent of hw.Level — only isTouching drives the circle.

    void UpdateFlash()
    {
        if (flashOverlay == null) return;

        if (hw.isTouching == 1)
        {
            flashAlpha = flashIntensity;
        }
        else
        {
            // Fade speed comes from dm.CurrentFadeTime, which encodes run-time progression
            // (fastFadeTimeAtStart=1.5s → slowFadeTimeAtEnd=180s via globalDistractionTimer).
            // hw.Level has no effect here.
            float fadeTime = (dm != null) ? dm.CurrentFadeTime : 1.5f;
            float decaySpeed = flashIntensity / Mathf.Max(0.05f, fadeTime);
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, decaySpeed * Time.deltaTime);
        }

        Color c = flashOverlay.color;
        c.a = flashAlpha;
        flashOverlay.color = c;
    }

    // ── Effect 3 / 4: Grounding Border + Shielded color ───────────────────────
    // fillAmount is assigned directly each frame so it tracks groundingTimer 1:1.
    // Using Lerp/smoothing here caused the bar to appear to jump to full instantly
    // because exponential smoothing reaches the target in just a few frames.

    void UpdateBorder()
    {
        if (groundingBorder == null) return;

        if (vfxState == VFXState.Shielded)
        {
            // Shielded: lock to full, gold color.
            groundingBorder.enabled    = true;
            groundingBorder.fillAmount = 1f;
            groundingBorder.color      = shieldedColor;
            return;
        }

        // Direct 1:1 mapping — bar exactly reflects timer progress every frame.
        float fill = groundingDuration > 0f
            ? Mathf.Clamp01(groundingTimer / groundingDuration)
            : 0f;

        bool active = (vfxState == VFXState.Grounding) || (fill > 0.005f);
        groundingBorder.enabled    = active;
        groundingBorder.fillAmount = fill;
        Debug.Log("[VFX] fill = " + fill + ", timer = " + groundingTimer
                  + " / " + groundingDuration);
        groundingBorder.color      = groundingColor;
    }

    // ── Effect 1b: Level 3 Camera Shake ──────────────────────────────────────

    void UpdateCameraShake()
    {
        if (mainCamera == null) return;

        if (hw.Level == 3)
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

    // ── Shielded: freeze dynamic effects ─────────────────────────────────────

    void EnterShielded()
    {
        vfxState       = VFXState.Shielded;
        groundingTimer = groundingDuration;
        if (mainCamera != null)
            mainCamera.transform.localPosition = camOrigin;
    }

    // ── Blow: fade all VFX out, then reset ───────────────────────────────────

    IEnumerator BlowRoutine()
    {
        float elapsed = 0f;

        if (vfxGroup != null)
        {
            // Preferred: fade entire group via CanvasGroup alpha
            while (elapsed < blowFadeDuration)
            {
                elapsed += Time.deltaTime;
                vfxGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / blowFadeDuration);
                yield return null;
            }
            vfxGroup.alpha = 0f;
        }
        else
        {
            // Fallback: fade each image individually
            Color startOverlay = colorOverlay   != null ? colorOverlay.color   : Color.clear;
            Color startBorder  = groundingBorder != null ? groundingBorder.color : Color.clear;
            float startFlash   = flashAlpha;

            while (elapsed < blowFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Clamp01(elapsed / blowFadeDuration);

                if (colorOverlay != null)
                    colorOverlay.color = new Color(startOverlay.r, startOverlay.g, startOverlay.b, startOverlay.a * t);

                if (groundingBorder != null)
                    groundingBorder.color = new Color(startBorder.r, startBorder.g, startBorder.b, startBorder.a * t);

                flashAlpha = startFlash * t;
                if (flashOverlay != null) { Color fc = flashOverlay.color; fc.a = flashAlpha; flashOverlay.color = fc; }

                yield return null;
            }
        }

        HardReset();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void ResetUI()
    {
        if (colorOverlay   != null) colorOverlay.color = Color.clear;
        if (flashOverlay   != null) flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        if (groundingBorder != null)
        {
            groundingBorder.fillAmount = 0f;
            groundingBorder.color      = groundingColor;
            groundingBorder.enabled    = false;
        }
        if (vfxGroup != null) vfxGroup.alpha = 1f;
    }

    void HardReset()
    {
        vfxState       = VFXState.Normal;
        groundingTimer = 0f;
        flashAlpha     = 0f;

        ResetUI();

        if (mainCamera != null)
            mainCamera.transform.localPosition = camOrigin;

        if (vfxGroup != null) vfxGroup.alpha = 1f;

        blowInProgress = false;
    }
}
