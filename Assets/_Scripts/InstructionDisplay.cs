using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a full-screen instruction overlay on Display 1 at game start.
/// Hides when the user first touches the water; re-appears after R-reset or cooldown.
///
/// Setup: attach to any GameObject in the scene. All UI is built at runtime.
/// Requires TextMeshPro  (Window → Package Manager → TextMeshPro → Import TMP Essentials).
/// To change fonts: Window → TextMeshPro → Font Asset Creator → generate a .asset from a .ttf,
/// then drag the .asset into the Title Font / Body Font slot below.
/// </summary>
public class InstructionDisplay : MonoBehaviour
{
    public static InstructionDisplay Instance { get; private set; }

    // ── Display ───────────────────────────────────────────────────────────────

    [Header("Display")]
    [Tooltip("0 = Display 1 (main), 1 = Display 2, …")]
    public int targetDisplay = 0;

    // ── Background ────────────────────────────────────────────────────────────

    [Header("Background")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.82f);

    // ── Title ─────────────────────────────────────────────────────────────────

    [Header("Title — Content")]
    [TextArea(1, 4)]
    public string titleText = "Welcome to Ephemeral Focus.";
    [Tooltip("Drag a TMP Font Asset (.asset) here. Empty = TMP default.")]
    public TMP_FontAsset titleFont;
    [Range(20f, 120f)] public float  titleFontSize = 58f;
    public Color                     titleColor    = Color.white;
    public FontStyles                titleStyle    = FontStyles.Bold;

    [Header("Title — Position")]
    [Tooltip("Vertical centre of the title block.\n0 = bottom of screen  ·  1 = top of screen.\nRaise to move the title up, lower to move it down.")]
    [Range(0f, 1f)] public float titleCenterY = 0.74f;
    [Tooltip("Height of the title's text area as a fraction of screen height.")]
    [Range(0.04f, 0.45f)] public float titleHeight = 0.16f;

    // ── Body ──────────────────────────────────────────────────────────────────

    [Header("Body — Content")]
    [TextArea(8, 30)]
    public string bodyText =
        "\U0001F4A7  The Water: Imagine this basin as your calm focus.\n\n" +
        "\U0001F300  The Ripples: Play with the water. This is the search for dopamine,\n" +
        "where we slowly drift into distraction.\n\n" +
        "\U0001FAA7  The Bubbles: These represent the constant stimuli around us.\n" +
        "Gently place them into the water and feel the change.\n\n" +
        "Immerse your hands to begin the journey of losing focus—and healing yourself.";
    [Tooltip("Drag a TMP Font Asset (.asset) here. Can be the same as Title Font.")]
    public TMP_FontAsset bodyFont;
    [Range(12f, 72f)] public float   bodyFontSize   = 30f;
    public Color                     bodyColor      = new Color(0.92f, 0.92f, 0.92f, 1f);
    public FontStyles                bodyStyle      = FontStyles.Normal;
    [Tooltip("Extra line-spacing on top of the font's natural spacing (TMP em units).")]
    [Range(-10f, 30f)] public float  bodyLineSpacing = 6f;

    [Header("Body — Position")]
    [Tooltip("Vertical centre of the body block.\n0 = bottom  ·  1 = top.")]
    [Range(0f, 1f)] public float bodyCenterY = 0.38f;
    [Tooltip("Height of the body's text area as a fraction of screen height.")]
    [Range(0.10f, 0.75f)] public float bodyHeight = 0.50f;

    // ── Shared layout ─────────────────────────────────────────────────────────

    [Header("Shared Layout")]
    [Tooltip("Horizontal inset on each side (fraction of screen width).")]
    [Range(0f, 0.40f)] public float sidePadding = 0.12f;

    // ── Animation ─────────────────────────────────────────────────────────────

    [Header("Animation")]
    [Range(0.1f, 3f)] public float fadeInDuration  = 0.8f;
    [Range(0.1f, 2f)] public float fadeOutDuration = 0.5f;

    // ── Internals ─────────────────────────────────────────────────────────────

    private CanvasGroup     canvasGroup;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private RectTransform   titleRT;
    private RectTransform   bodyRT;
    private Image           bgImage;
    private DistractionManager dm;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        dm = DistractionManager.Instance;
        BuildUI();
        ShowInstruction();
    }

    private void Update()
    {
        if (dm == null) dm = DistractionManager.Instance;
        if (dm != null && dm.HasExperienceStarted && IsVisible)
            HideInstruction();
    }

    // Reflects Inspector changes immediately while in Play mode.
    private void OnValidate()
    {
        if (!Application.isPlaying || titleLabel == null) return;
        ApplyAll();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

    public void ShowInstruction()
    {
        if (canvasGroup == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, fadeInDuration));
    }

    public void HideInstruction()
    {
        if (canvasGroup == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = canvasGroup.alpha, elapsed = 0f;
        canvasGroup.blocksRaycasts = target > 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = target;
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        GameObject canvasGO  = new GameObject("InstructionCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = targetDisplay;
        canvas.sortingOrder  = 200;
        CanvasScaler scaler         = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight   = 0.5f;

        // Root fading group
        GameObject groupGO = new GameObject("InstructionRoot");
        groupGO.transform.SetParent(canvasGO.transform, false);
        canvasGroup = groupGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        Stretch(groupGO.AddComponent<RectTransform>());

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(groupGO.transform, false);
        bgImage = bgGO.AddComponent<Image>();
        bgImage.color         = overlayColor;
        bgImage.raycastTarget = false;
        Stretch(bgImage.rectTransform);

        // Title
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(groupGO.transform, false);
        titleLabel             = titleGO.AddComponent<TextMeshProUGUI>();
        titleLabel.raycastTarget = false;
        titleLabel.alignment   = TextAlignmentOptions.Center;
        titleLabel.enableWordWrapping = true;
        titleRT = titleLabel.rectTransform;

        // Body
        GameObject bodyGO = new GameObject("BodyText");
        bodyGO.transform.SetParent(groupGO.transform, false);
        bodyLabel              = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyLabel.raycastTarget = false;
        bodyLabel.alignment    = TextAlignmentOptions.Center;
        bodyLabel.enableWordWrapping = true;
        bodyRT = bodyLabel.rectTransform;

        ApplyAll();
    }

    // Pushes all Inspector values to the live UI components.
    private void ApplyAll()
    {
        if (bgImage   != null) bgImage.color = overlayColor;
        if (titleRT   != null) ApplyAnchors(titleRT, titleCenterY, titleHeight, sidePadding);
        if (bodyRT    != null) ApplyAnchors(bodyRT,  bodyCenterY,  bodyHeight,  sidePadding);

        if (titleLabel != null)
        {
            titleLabel.text      = titleText;
            titleLabel.fontSize  = titleFontSize;
            titleLabel.color     = titleColor;
            titleLabel.fontStyle = titleStyle;
            if (titleFont != null) titleLabel.font = titleFont;
        }
        if (bodyLabel != null)
        {
            bodyLabel.text         = bodyText;
            bodyLabel.fontSize     = bodyFontSize;
            bodyLabel.color        = bodyColor;
            bodyLabel.fontStyle    = bodyStyle;
            bodyLabel.lineSpacing  = bodyLineSpacing;
            if (bodyFont != null) bodyLabel.font = bodyFont;
        }
    }

    // Positions a RectTransform using a screen-fraction centre + height.
    private static void ApplyAnchors(RectTransform rt,
                                     float centerY, float height, float sideInset)
    {
        float minY = Mathf.Clamp01(centerY - height * 0.5f);
        float maxY = Mathf.Clamp01(centerY + height * 0.5f);
        rt.anchorMin = new Vector2(sideInset,       minY);
        rt.anchorMax = new Vector2(1f - sideInset,  maxY);
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
}
