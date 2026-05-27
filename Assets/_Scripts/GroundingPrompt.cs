using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monitors two conditions and shows a "you can now ground yourself" prompt
/// on BOTH Display 1 (main screen) and Display 2 (projector).
///
/// ─ Condition A │ Touch Count : simultaneous touch effects on screen ≥ touchCountThreshold
/// ─ Condition B │ Session Timer: seconds since first touch ≥ timerThreshold (default 2 min)
///
/// Either condition alone fires the prompt. Both thresholds, images, and text
/// are fully configurable in the Inspector at runtime.
///
/// Setup
/// ─────
///  1. Add this script to any GameObject in the scene.
///  2. Assign your Sprite assets to the Display 1 / Display 2 Image slots in the Inspector.
///  3. (Optional) Tweak thresholds, layout, and text in the Inspector.
///  No other scene setup is required — all UI is built at runtime.
///
/// Dependencies
/// ────────────
///  • TextMeshPro  (Window → Package Manager → TextMeshPro → Import TMP Essentials)
///  • InteractionVFXController  (provides ActiveTouchCount)
///  • DistractionManager        (provides GlobalDistractionTimer / HasExperienceStarted)
/// </summary>
public class GroundingPrompt : MonoBehaviour
{
    public static GroundingPrompt Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════════════════════
    //  TRIGGER CONDITIONS
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Trigger Conditions ───────────────────────────────────────────")]

    [Tooltip("Condition A — number of simultaneous touch-glass effects on screen\n" +
             "required to fire the prompt. Set to 0 to disable this condition.")]
    public int touchCountThreshold = 50;

    [Tooltip("Condition B — seconds after the first touch before the prompt\n" +
             "auto-fires (120 = 2 minutes). Set to 0 to disable this condition.")]
    public float timerThreshold = 120f;

    // ══════════════════════════════════════════════════════════════════════════════
    //  DISPLAY 1 — IMAGE
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Display 1 — Hand Image ──────────────────────────────────────────")]

    [Tooltip("Unity display index for the main screen  (0 = first display in Project Settings).")]
    public int display1Index = 0;

    [Tooltip("Sprite for the HAND on Display 1. This image will animate toward the basin.")]
    public Sprite display1Image;

    [Tooltip("Tint / alpha of the hand image on Display 1. White = no tint; lower alpha = more transparent.")]
    public Color display1ImageTint = Color.white;

    [Tooltip("Horizontal padding on each side as a fraction of screen width  (0 = edge-to-edge).")]
    [Range(0f, 0.45f)]
    public float display1ImageSidePadding = 0.05f;

    [Tooltip("Height of the hand image area as a fraction of screen height.")]
    [Range(0.05f, 1f)]
    public float display1ImageHeightFraction = 0.30f;

    [Tooltip("(Legacy — ignored when animation is active.) Static vertical centre of the hand image.")]
    [Range(0f, 1f)]
    public float display1ImageCenterY = 0.62f;

    // ══════════════════════════════════════════════════════════════════════════
    //  DISPLAY 1 — BASIN IMAGE (second image, fixed)
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── Display 1 — Basin Image (fixed) ──────────────────────────────")]

    [Tooltip("Sprite for the WATER BASIN on Display 1. Stays fixed; the hand animates toward it.")]
    public Sprite display1Image2;

    [Tooltip("Tint / alpha of the basin image.")]
    public Color display1Image2Tint = Color.white;

    [Tooltip("Horizontal padding for the basin image on each side (fraction of screen width).")]
    [Range(0f, 0.45f)]
    public float display1Image2SidePadding = 0.05f;

    [Tooltip("Height of the basin image area as a fraction of screen height.")]
    [Range(0.05f, 1f)]
    public float display1Image2HeightFraction = 0.30f;

    [Tooltip("Vertical centre of the basin image (0 = bottom · 0.5 = centre · 1 = top).")]
    [Range(0f, 1f)]
    public float display1Image2CenterY = 0.35f;

    // ══════════════════════════════════════════════════════════════════════════
    //  DISPLAY 1 — HAND ANIMATION
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── Display 1 — Hand Animation ─────────────────────────────────────")]

    [Tooltip("Vertical centre where the hand starts each cycle (above the basin).")]
    [Range(0f, 1f)]
    public float handAnimStartCenterY = 0.78f;

    [Tooltip("Vertical centre where the hand ends each cycle (dipped into the basin).")]
    [Range(0f, 1f)]
    public float handAnimEndCenterY   = 0.43f;

    [Tooltip("Seconds for the hand to travel from start to end position.")]
    [Range(0.3f, 6f)]
    public float handAnimCycleDuration = 2.0f;

    [Tooltip("Seconds the hand lingers at the end position before snapping back to the start.")]
    [Range(0f, 3f)]
    public float handAnimPauseDuration = 0.8f;

    // ══════════════════════════════════════════════════════════════════════════════
    //  DISPLAY 1 — TEXT
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Display 1 — Text ──────────────────────────────────────────────")]

    [Tooltip("Show a text prompt beneath the image on Display 1.")]
    public bool showTextOnDisplay1 = true;

    [TextArea(2, 8)]
    public string display1Text =
        "You may now ground yourself.\n" +
        "Place your hands firmly on the surface to end the session.";

    [Tooltip("Drag a TMP Font Asset (.asset) here, or leave empty for the TMP default font.")]
    public TMP_FontAsset display1Font;

    [Range(12f, 96f)]
    public float display1FontSize = 36f;

    public Color      display1TextColor = Color.white;
    public FontStyles display1TextStyle = FontStyles.Normal;

    [Tooltip("Vertical centre of the text block  (0 = bottom · 1 = top).")]
    [Range(0f, 1f)]
    public float display1TextCenterY = 0.24f;

    [Tooltip("Height of the text area as a fraction of screen height.")]
    [Range(0.04f, 0.5f)]
    public float display1TextHeight = 0.18f;

    [Tooltip("Horizontal padding for the text on each side  (fraction of screen width).")]
    [Range(0f, 0.45f)]
    public float display1TextSidePadding = 0.10f;

    // ══════════════════════════════════════════════════════════════════════════════
    //  DISPLAY 2 — IMAGE
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Display 2 — Image ─────────────────────────────────────────────")]

    [Tooltip("Unity display index for the projector  (1 = second display in Project Settings).")]
    public int display2Index = 1;

    [Tooltip("Sprite to show on Display 2. Can be the same or different from Display 1.")]
    public Sprite display2Image;

    [Tooltip("Tint / alpha of the image on Display 2.")]
    public Color display2ImageTint = Color.white;

    [Tooltip("Horizontal padding on each side as a fraction of screen width.")]
    [Range(0f, 0.45f)]
    public float display2ImageSidePadding = 0f;

    [Tooltip("Height of the image area as a fraction of screen height on Display 2.")]
    [Range(0.05f, 1f)]
    public float display2ImageHeightFraction = 0.65f;

    [Tooltip("Vertical centre of the image on Display 2.")]
    [Range(0f, 1f)]
    public float display2ImageCenterY = 0.5f;

    // ══════════════════════════════════════════════════════════════════════════════
    //  SHARED BACKGROUND
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Background (both displays) ──────────────────────────────────────")]

    [Tooltip("Semi-transparent overlay drawn behind the image and text on both displays.\n" +
             "Set alpha to 0 for no background.")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.55f);

    // ══════════════════════════════════════════════════════════════════════════════
    //  ANIMATION
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Animation ─────────────────────────────────────────────────────")]

    [Range(0.1f, 5f)]
    public float fadeInDuration  = 1.2f;

    [Range(0.1f, 2f)]
    public float fadeOutDuration = 0.6f;

    // ══════════════════════════════════════════════════════════════════════════════
    //  DEBUG
    // ══════════════════════════════════════════════════════════════════════════════

    [Header("─── Debug ─────────────────────────────────────────────────────────")]

    [Tooltip("Show a real-time status overlay in the Game view during Play mode.")]
    public bool showDebugOverlay = false;

    // ── Internal state ────────────────────────────────────────────────────────────

    private DistractionManager       dm;
    private InteractionVFXController vfx;

    // Display 1 UI
    private CanvasGroup      d1Group;
    private Image            d1Bg;
    private Image            d1Sprite;      // hand image
    private RectTransform    d1SpriteRT;
    private Image            d1Sprite2;     // basin image (fixed)
    private RectTransform    d1SpriteRT2;
    private TextMeshProUGUI  d1TextLabel;
    private RectTransform    d1TextRT;

    // Hand animation state
    private float handAnimTimer      = 0f;
    private bool  handAnimPausing    = false;
    private float handAnimPauseTimer = 0f;

    // Display 2 UI
    private CanvasGroup      d2Group;
    private Image            d2Bg;
    private Image            d2Sprite;
    private RectTransform    d2SpriteRT;

    private bool promptTriggered      = false;
    private bool isFullyShown         = false;   // fade-in 完全结束后才为 true
    private bool wasExpStarted        = false;

    // Which condition fired (for debug display)
    private string triggeredBy = "—";

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        dm  = DistractionManager.Instance;
        vfx = FindObjectOfType<InteractionVFXController>();

        BuildUIDisplay1();
        BuildUIDisplay2();
        ApplyAll();
    }

    private void Update()
    {
        // Lazy-find dependencies (survive scene reload)
        if (dm  == null) dm  = DistractionManager.Instance;
        if (vfx == null) vfx = FindObjectOfType<InteractionVFXController>();

        // ── Detect game reset / cooldown start ───────────────────────────────────
        // ClearAllEffects() (called by FullReset and EnterCooldown) sets
        // hasExperienceStarted = false, which we watch here to auto-hide the prompt.
        bool expNow = dm != null && dm.HasExperienceStarted;
        if (wasExpStarted && !expNow)
        {
            promptTriggered = false;
            isFullyShown    = false;
            triggeredBy     = "—";
            HidePrompt();
        }
        wasExpStarted = expNow;

        // ── 用户开始 grounding 或已完成 grounding → 立刻 fade out ─────────────────
        // 不等 blow 结束，一旦检测到 grounding 动作就隐藏提示。
        if (IsVisible && dm != null && (dm.IsGrounding || dm.IsShielded))
        {
            HidePrompt();
        }

        // ── Check trigger conditions ──────────────────────────────────────────────
        if (!promptTriggered)
        {
            string reason = GetTriggerReason();
            if (reason != null)
            {
                promptTriggered = true;
                triggeredBy     = reason;
                ShowPrompt();
            }
        }

        UpdateHandAnimation();
    }

    /// <summary>
    /// Returns a short description of why the prompt fired, or null if not triggered.
    /// </summary>
    private string GetTriggerReason()
    {
        // Condition A — touch count threshold
        // Guard with HasExperienceStarted so stale frozen touch glasses from a
        // just-completed session cannot re-fire the prompt the moment it resets.
        if (touchCountThreshold > 0 && vfx != null &&
            dm != null && dm.HasExperienceStarted &&
            vfx.ActiveTouchCount >= touchCountThreshold)
            return $"Touch count ({vfx.ActiveTouchCount} ≥ {touchCountThreshold})";

        // Condition B — session timer threshold
        if (timerThreshold > 0f && dm != null &&
            dm.HasExperienceStarted &&
            dm.GlobalDistractionTimer >= timerThreshold)
            return $"Timer ({dm.GlobalDistractionTimer:F0}s ≥ {timerThreshold:F0}s)";

        return null;
    }

    // Reflects Inspector edits immediately in Play mode.
    private void OnValidate()
    {
        if (!Application.isPlaying || d1Group == null) return;
        ApplyAll();
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>Whether the prompt is currently faded in.</summary>
    public bool IsVisible    => d1Group != null && d1Group.alpha > 0.01f;

    /// <summary>提示已经被触发过（哪怕之后淡出了也保持 true，reset 后变回 false）。</summary>
    public bool HasTriggered => promptTriggered;

    /// <summary>
    /// 提示的 fade-in 动画已完全结束（alpha = 1 到达后才为 true）。
    /// DistractionManager 和 InteractionVFXController 用这个判断 grounding 是否解锁，
    /// 确保提示完整展示后才允许触发 grounding。
    /// reset 后变回 false。
    /// </summary>
    public bool IsFullyShown => isFullyShown;

    /// <summary>Fade the grounding prompt in on both displays.</summary>
    public void ShowPrompt()
    {
        StopAllCoroutines();
        isFullyShown = false;
        // d1Group fade-in 完成时才将 isFullyShown 置 true，确保完整展示后才解锁 grounding。
        if (d1Group != null) StartCoroutine(FadeTo(d1Group, 1f, fadeInDuration,
                                                    () => isFullyShown = true));
        if (d2Group != null) StartCoroutine(FadeTo(d2Group, 1f, fadeInDuration));
    }

    /// <summary>Fade the grounding prompt out on both displays.</summary>
    public void HidePrompt()
    {
        StopAllCoroutines();
        if (d1Group != null) StartCoroutine(FadeTo(d1Group, 0f, fadeOutDuration));
        if (d2Group != null) StartCoroutine(FadeTo(d2Group, 0f, fadeOutDuration));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────────

    private IEnumerator FadeTo(CanvasGroup group, float target, float duration,
                               System.Action onComplete = null)
    {
        if (group == null) yield break;
        float start = group.alpha, elapsed = 0f;
        group.blocksRaycasts = (target > 0f);
        while (elapsed < duration)
        {
            elapsed     += Time.deltaTime;
            group.alpha  = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = target;
        onComplete?.Invoke();
    }

    // ── Hand animation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Drives the hand image start → end → pause → snap-back loop.
    /// Uses a quadratic ease-in so the hand accelerates into the water.
    /// Only active while the prompt is visible (alpha > 0).
    /// </summary>
    private void UpdateHandAnimation()
    {
        if (d1SpriteRT == null) return;

        // When prompt is hidden: reset state and park hand at animation start
        if (!IsVisible)
        {
            handAnimTimer      = 0f;
            handAnimPausing    = false;
            handAnimPauseTimer = 0f;
            SetAnchors(d1SpriteRT,
                       handAnimStartCenterY,
                       display1ImageHeightFraction,
                       display1ImageSidePadding);
            return;
        }

        if (handAnimPausing)
        {
            // Hand is resting at end position (inside the water)
            handAnimPauseTimer += Time.deltaTime;
            if (handAnimPauseTimer >= handAnimPauseDuration)
            {
                // Snap back to start and begin next cycle
                handAnimPausing    = false;
                handAnimPauseTimer = 0f;
                handAnimTimer      = 0f;
            }
            // Hold at end position during pause — no anchor update needed
            return;
        }

        // Advance timer and compute eased position
        handAnimTimer += Time.deltaTime;
        float safeDuration = Mathf.Max(0.1f, handAnimCycleDuration);
        float t = Mathf.Clamp01(handAnimTimer / safeDuration);

        // Quadratic ease-in: starts slow (hovering above), accelerates into water
        float easedT       = t * t;
        float currentCenterY = Mathf.Lerp(handAnimStartCenterY, handAnimEndCenterY, easedT);

        SetAnchors(d1SpriteRT,
                   currentCenterY,
                   display1ImageHeightFraction,
                   display1ImageSidePadding);

        // When travel completes, enter pause phase (hand rests in water)
        if (handAnimTimer >= safeDuration)
        {
            handAnimPausing    = true;
            handAnimPauseTimer = 0f;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION — DISPLAY 1
    // ══════════════════════════════════════════════════════════════════════════════

    private void BuildUIDisplay1()
    {
        // ── Canvas ────────────────────────────────────────────────────────────────
        var canvasGO            = new GameObject("GroundingPromptCanvas_D1");
        canvasGO.transform.SetParent(transform, false);
        var canvas              = canvasGO.AddComponent<Canvas>();
        canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay    = display1Index;
        canvas.sortingOrder     = 160;   // above VFX canvases (which typically use 0 or lower)
        var scaler                     = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode             = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution     = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight      = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Root CanvasGroup (unified fade) ───────────────────────────────────────
        var rootGO              = new GameObject("GroundingRoot_D1");
        rootGO.transform.SetParent(canvasGO.transform, false);
        d1Group                 = rootGO.AddComponent<CanvasGroup>();
        d1Group.alpha           = 0f;
        d1Group.blocksRaycasts  = false;
        Stretch(rootGO.AddComponent<RectTransform>());

        // ── Background overlay ────────────────────────────────────────────────────
        var bgGO                = new GameObject("Background");
        bgGO.transform.SetParent(rootGO.transform, false);
        d1Bg                    = bgGO.AddComponent<Image>();
        d1Bg.raycastTarget      = false;
        Stretch(d1Bg.rectTransform);

        // ── Basin image (fixed, drawn first so hand renders on top) ──────────────
        var basinGO               = new GameObject("BasinImage");
        basinGO.transform.SetParent(rootGO.transform, false);
        d1Sprite2                 = basinGO.AddComponent<Image>();
        d1Sprite2.raycastTarget   = false;
        d1Sprite2.preserveAspect  = true;
        d1SpriteRT2               = d1Sprite2.rectTransform;

        // ── Hand image (animated, drawn on top of basin) ──────────────────────────
        var imgGO               = new GameObject("HandImage");
        imgGO.transform.SetParent(rootGO.transform, false);
        d1Sprite                = imgGO.AddComponent<Image>();
        d1Sprite.raycastTarget  = false;
        d1Sprite.preserveAspect = true;
        d1SpriteRT              = d1Sprite.rectTransform;

        // ── Text label ────────────────────────────────────────────────────────────
        var textGO                    = new GameObject("PromptText");
        textGO.transform.SetParent(rootGO.transform, false);
        d1TextLabel                   = textGO.AddComponent<TextMeshProUGUI>();
        d1TextLabel.raycastTarget     = false;
        d1TextLabel.alignment         = TextAlignmentOptions.Center;
        d1TextLabel.enableWordWrapping = true;
        d1TextRT                      = d1TextLabel.rectTransform;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION — DISPLAY 2
    // ══════════════════════════════════════════════════════════════════════════════

    private void BuildUIDisplay2()
    {
        // ── Canvas ────────────────────────────────────────────────────────────────
        var canvasGO            = new GameObject("GroundingPromptCanvas_D2");
        canvasGO.transform.SetParent(transform, false);
        var canvas              = canvasGO.AddComponent<Canvas>();
        canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay    = display2Index;
        canvas.sortingOrder     = 160;
        var scaler                     = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode             = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution     = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight      = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Root CanvasGroup ──────────────────────────────────────────────────────
        var rootGO              = new GameObject("GroundingRoot_D2");
        rootGO.transform.SetParent(canvasGO.transform, false);
        d2Group                 = rootGO.AddComponent<CanvasGroup>();
        d2Group.alpha           = 0f;
        d2Group.blocksRaycasts  = false;
        Stretch(rootGO.AddComponent<RectTransform>());

        // ── Background overlay ────────────────────────────────────────────────────
        var bgGO                = new GameObject("Background");
        bgGO.transform.SetParent(rootGO.transform, false);
        d2Bg                    = bgGO.AddComponent<Image>();
        d2Bg.raycastTarget      = false;
        Stretch(d2Bg.rectTransform);

        // ── Prompt image ──────────────────────────────────────────────────────────
        var imgGO               = new GameObject("PromptImage");
        imgGO.transform.SetParent(rootGO.transform, false);
        d2Sprite                = imgGO.AddComponent<Image>();
        d2Sprite.raycastTarget  = false;
        d2Sprite.preserveAspect = true;
        d2SpriteRT              = d2Sprite.rectTransform;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  APPLY INSPECTOR VALUES TO LIVE UI
    // ══════════════════════════════════════════════════════════════════════════════

    private void ApplyAll()
    {
        ApplyDisplay1();
        ApplyDisplay2();
    }

    private void ApplyDisplay1()
    {
        // Background
        if (d1Bg != null)
            d1Bg.color = overlayColor;

        // Basin image (static)
        if (d1Sprite2 != null)
        {
            d1Sprite2.sprite  = display1Image2;
            d1Sprite2.color   = display1Image2Tint;
            d1Sprite2.enabled = (display1Image2 != null);
        }
        if (d1SpriteRT2 != null)
            SetAnchors(d1SpriteRT2,
                       display1Image2CenterY,
                       display1Image2HeightFraction,
                       display1Image2SidePadding);

        // Hand image — initial position is the animation start (UpdateHandAnimation drives it each frame)
        if (d1Sprite != null)
        {
            d1Sprite.sprite  = display1Image;
            d1Sprite.color   = display1ImageTint;
            d1Sprite.enabled = (display1Image != null);
        }
        if (d1SpriteRT != null)
            SetAnchors(d1SpriteRT,
                       handAnimStartCenterY,
                       display1ImageHeightFraction,
                       display1ImageSidePadding);

        // Text
        if (d1TextLabel != null)
        {
            d1TextLabel.gameObject.SetActive(showTextOnDisplay1);
            if (showTextOnDisplay1)
            {
                d1TextLabel.text      = display1Text;
                d1TextLabel.fontSize  = display1FontSize;
                d1TextLabel.color     = display1TextColor;
                d1TextLabel.fontStyle = display1TextStyle;
                if (display1Font != null) d1TextLabel.font = display1Font;
            }
        }
        if (d1TextRT != null && showTextOnDisplay1)
            SetAnchors(d1TextRT,
                       display1TextCenterY,
                       display1TextHeight,
                       display1TextSidePadding);
    }

    private void ApplyDisplay2()
    {
        // Background
        if (d2Bg != null)
            d2Bg.color = overlayColor;

        // Image
        if (d2Sprite != null)
        {
            d2Sprite.sprite  = display2Image;
            d2Sprite.color   = display2ImageTint;
            d2Sprite.enabled = (display2Image != null);
        }
        if (d2SpriteRT != null)
            SetAnchors(d2SpriteRT,
                       display2ImageCenterY,
                       display2ImageHeightFraction,
                       display2ImageSidePadding);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  LAYOUT HELPERS
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Positions a RectTransform using anchor fractions.
    /// centerY and height are both in [0,1] relative to the canvas height.
    /// sideInset is in [0,0.45] relative to the canvas width.
    /// </summary>
    private static void SetAnchors(RectTransform rt,
                                   float centerY, float height, float sideInset)
    {
        float minY = Mathf.Clamp01(centerY - height * 0.5f);
        float maxY = Mathf.Clamp01(centerY + height * 0.5f);
        rt.anchorMin = new Vector2(sideInset,      minY);
        rt.anchorMax = new Vector2(1f - sideInset, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  DEBUG OVERLAY
    // ══════════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        if (!showDebugOverlay) return;

        // Semi-transparent background panel
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(8f, 8f, 320f, 200f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float y = 14f;
        const float lineH = 18f;

        void Line(string text, Color col = default)
        {
            if (col != default) GUI.color = col;
            GUI.Label(new Rect(14f, y, 306f, lineH), text);
            GUI.color = Color.white;
            y += lineH;
        }

        Line("[GroundingPrompt]");

        // ── Condition A ────────────────────────────────────────────────────────────
        int touchCount = vfx != null ? vfx.ActiveTouchCount : 0;
        bool condA = touchCountThreshold > 0 && touchCount >= touchCountThreshold;
        Color colA = condA ? Color.green : Color.white;
        Line($"Condition A — Touch count : {touchCount} / {touchCountThreshold}", colA);

        // ── Condition B ────────────────────────────────────────────────────────────
        float timer = dm != null ? dm.GlobalDistractionTimer : 0f;
        bool expStarted = dm != null && dm.HasExperienceStarted;
        bool condB = timerThreshold > 0f && expStarted && timer >= timerThreshold;
        Color colB = condB ? Color.green : Color.white;
        Line($"Condition B — Session timer : {timer:F1}s / {timerThreshold:F0}s", colB);

        Line($"Experience started : {(expStarted ? "YES" : "no")}",
             expStarted ? Color.white : new Color(1f, 0.6f, 0.2f));

        Line($"Prompted triggered : {(promptTriggered ? "YES" : "no")}",
             promptTriggered ? Color.cyan : Color.white);

        if (promptTriggered)
            Line($"Triggered by       : {triggeredBy}", Color.cyan);

        float alpha = d1Group != null ? d1Group.alpha : 0f;
        Line($"Overlay alpha (D1) : {alpha:F2}");
    }
}
