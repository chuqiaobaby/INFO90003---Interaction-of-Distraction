using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a "blow to clear" prompt on Display 1 whenever the system enters
/// the Shielded state (grounding complete). Fades out automatically when
/// the state leaves Shielded (cooldown starts or reset).
///
/// Setup
/// ─────
///  1. Add this script to any GameObject in the scene.
///  2. Tweak text, font, layout, and colours in the Inspector.
///  No other scene setup required — UI is built at runtime.
/// </summary>
public class BlowPrompt : MonoBehaviour
{
    public static BlowPrompt Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════════════════
    //  DISPLAY 1
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── Display ──────────────────────────────────────────────────────")]
    [Tooltip("Unity display index for the main screen (0 = first display).")]
    public int displayIndex = 0;

    // ══════════════════════════════════════════════════════════════════════════
    //  TEXT
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── Text ────────────────────────────────────────────────────────")]

    [TextArea(2, 6)]
    public string promptText = "Take a deep breath\nand blow gently to release.";

    [Tooltip("Drag a TMP Font Asset here, or leave empty for the TMP default font.")]
    public TMP_FontAsset font;

    [Range(12f, 96f)]
    public float fontSize = 42f;

    public Color textColor = Color.white;

    public FontStyles textStyle = FontStyles.Normal;

    [Tooltip("Vertical centre of the text block (0 = bottom · 0.5 = centre · 1 = top).")]
    [Range(0f, 1f)]
    public float textCenterY = 0.25f;

    [Tooltip("Height of the text area as a fraction of screen height.")]
    [Range(0.04f, 0.5f)]
    public float textHeight = 0.20f;

    [Tooltip("Horizontal padding on each side (fraction of screen width).")]
    [Range(0f, 0.45f)]
    public float textSidePadding = 0.10f;

    // ══════════════════════════════════════════════════════════════════════════
    //  BACKGROUND OVERLAY
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── Background ──────────────────────────────────────────────────")]
    [Tooltip("Semi-transparent overlay behind the text. Set alpha to 0 for no background.")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.45f);

    // ══════════════════════════════════════════════════════════════════════════
    //  ANIMATION
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── Animation ───────────────────────────────────────────────────")]

    [Range(0.1f, 5f)]
    public float fadeInDuration  = 1.0f;

    [Range(0.1f, 3f)]
    public float fadeOutDuration = 0.5f;

    // ── Internal state ────────────────────────────────────────────────────────

    private CanvasGroup     group;
    private Image           bg;
    private TextMeshProUGUI label;
    private RectTransform   labelRT;

    private bool wasShielded = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildUI();
        ApplyInspectorValues();
    }

    private void Update()
    {
        DistractionManager dm = DistractionManager.Instance;
        bool shieldedNow = dm != null && dm.IsShielded;

        if (!wasShielded && shieldedNow)
            ShowPrompt();
        else if (wasShielded && !shieldedNow)
            HidePrompt();

        wasShielded = shieldedNow;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || group == null) return;
        ApplyInspectorValues();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsVisible => group != null && group.alpha > 0.01f;

    public void ShowPrompt()
    {
        StopAllCoroutines();
        if (SFXController.Instance != null)
            SFXController.Instance.PlayBlowPrompt();
        if (group != null) StartCoroutine(FadeTo(group, 1f, fadeInDuration));
    }

    public void HidePrompt()
    {
        StopAllCoroutines();
        if (group != null) StartCoroutine(FadeTo(group, 0f, fadeOutDuration));
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        var canvasGO         = new GameObject("BlowPromptCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas           = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = displayIndex;
        canvas.sortingOrder  = 170;   // above GroundingPrompt (160)
        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Root CanvasGroup for unified fade
        var rootGO             = new GameObject("BlowPromptRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        group                  = rootGO.AddComponent<CanvasGroup>();
        group.alpha            = 0f;
        group.blocksRaycasts   = false;
        Stretch(rootGO.AddComponent<RectTransform>());

        // Background overlay
        var bgGO          = new GameObject("Background");
        bgGO.transform.SetParent(rootGO.transform, false);
        bg                = bgGO.AddComponent<Image>();
        bg.raycastTarget  = false;
        Stretch(bg.rectTransform);

        // Text label
        var textGO                     = new GameObject("BlowPromptText");
        textGO.transform.SetParent(rootGO.transform, false);
        label                          = textGO.AddComponent<TextMeshProUGUI>();
        label.raycastTarget            = false;
        label.alignment                = TextAlignmentOptions.Center;
        label.enableWordWrapping       = true;
        labelRT                        = label.rectTransform;
    }

    private void ApplyInspectorValues()
    {
        if (bg != null)
            bg.color = overlayColor;

        if (label != null)
        {
            label.text      = promptText;
            label.fontSize  = fontSize;
            label.color     = textColor;
            label.fontStyle = textStyle;
            if (font != null) label.font = font;
        }

        if (labelRT != null)
            SetAnchors(labelRT, textCenterY, textHeight, textSidePadding);
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    private IEnumerator FadeTo(CanvasGroup cg, float target, float duration)
    {
        if (cg == null) yield break;
        float start = cg.alpha, elapsed = 0f;
        cg.blocksRaycasts = target > 0f;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            cg.alpha   = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cg.alpha = target;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private static void SetAnchors(RectTransform rt, float centerY, float height, float sideInset)
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
}
