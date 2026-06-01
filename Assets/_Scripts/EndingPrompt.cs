using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a configurable Display 1 ending prompt after the user completes blow/clear.
/// </summary>
public class EndingPrompt : MonoBehaviour
{
    [Header("─── Trigger ───────────────────────────────────────────────────")]
    [Tooltip("Show the prompt when DistractionManager enters Cooldown after blow/clear.")]
    public bool triggerOnCooldown = true;
    [Tooltip("Seconds to wait after blow/clear before showing the ending prompt.")]
    [Range(0f, 10f)] public float triggerDelay = 1.0f;

    [Header("─── Display ───────────────────────────────────────────────────")]
    [Tooltip("0 = Display 1, 1 = Display 2, ...")]
    public int displayIndex = 0;
    public int sortingOrder = 260;

    [Header("─── Text ──────────────────────────────────────────────────────")]
    [TextArea(2, 8)]
    public string promptText = "Take a moment.\nNotice what has cleared.";
    public TMP_FontAsset font;
    [Range(12f, 120f)] public float fontSize = 44f;
    public Color textColor = Color.white;
    public FontStyles fontStyle = FontStyles.Normal;
    [Range(-10f, 30f)] public float lineSpacing = 4f;

    [Header("─── Text Layout ───────────────────────────────────────────────")]
    [Tooltip("Vertical centre of the text block. 0 = bottom, 1 = top.")]
    [Range(0f, 1f)] public float textCenterY = 0.50f;
    [Tooltip("Height of the text area as a fraction of screen height.")]
    [Range(0.04f, 0.75f)] public float textHeight = 0.24f;
    [Tooltip("Horizontal padding on each side as a fraction of screen width.")]
    [Range(0f, 0.45f)] public float textSidePadding = 0.12f;

    [Header("─── Background ────────────────────────────────────────────────")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("─── Timing ────────────────────────────────────────────────────")]
    [Range(0.05f, 8f)] public float fadeInDuration = 1.0f;
    [Range(0f, 20f)] public float holdDuration = 3.0f;
    [Range(0.05f, 8f)] public float fadeOutDuration = 1.0f;

    private DistractionManager dm;
    private CanvasGroup group;
    private Image background;
    private TextMeshProUGUI label;
    private RectTransform labelRT;
    private Coroutine promptRoutine;
    private bool wasInCooldown;

    private void Start()
    {
        dm = DistractionManager.Instance;
        BuildUI();
        ApplyInspectorValues();
    }

    private void Update()
    {
        if (dm == null) dm = DistractionManager.Instance;
        if (dm == null || !triggerOnCooldown) return;

        bool inCooldown = dm.IsInCooldown;
        if (inCooldown && !wasInCooldown)
            TriggerPrompt();

        wasInCooldown = inCooldown;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || label == null) return;
        ApplyInspectorValues();
    }

    public void ShowPrompt()
    {
        if (group == null || label == null) return;

        if (promptRoutine != null)
            StopCoroutine(promptRoutine);

        ApplyInspectorValues();
        promptRoutine = StartCoroutine(PlayPromptRoutine());
    }

    private void TriggerPrompt()
    {
        if (promptRoutine != null)
            StopCoroutine(promptRoutine);

        promptRoutine = StartCoroutine(DelayedPromptRoutine());
    }

    public void HidePrompt()
    {
        if (promptRoutine != null)
        {
            StopCoroutine(promptRoutine);
            promptRoutine = null;
        }

        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }

    private IEnumerator PlayPromptRoutine()
    {
        yield return FadeTo(1f, fadeInDuration);
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);
        yield return FadeTo(0f, fadeOutDuration);
        promptRoutine = null;
    }

    private IEnumerator DelayedPromptRoutine()
    {
        if (triggerDelay > 0f)
            yield return new WaitForSeconds(triggerDelay);

        yield return PlayPromptRoutine();
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = group.alpha;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        group.blocksRaycasts = target > 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        group.alpha = target;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("EndingPromptCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = displayIndex;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject rootGO = new GameObject("EndingPromptRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        group = rootGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        Stretch(rootGO.AddComponent<RectTransform>());

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(rootGO.transform, false);
        background = bgGO.AddComponent<Image>();
        background.raycastTarget = false;
        Stretch(background.rectTransform);

        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(rootGO.transform, false);
        label = textGO.AddComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        labelRT = label.rectTransform;
    }

    private void ApplyInspectorValues()
    {
        if (background != null)
            background.color = overlayColor;

        if (label != null)
        {
            label.text = promptText;
            label.fontSize = fontSize;
            label.color = textColor;
            label.fontStyle = fontStyle;
            label.lineSpacing = lineSpacing;
            if (font != null) label.font = font;
        }

        if (labelRT != null)
            SetAnchors(labelRT, textCenterY, textHeight, textSidePadding);
    }

    private static void SetAnchors(RectTransform rt, float centerY, float height, float sideInset)
    {
        float minY = Mathf.Clamp01(centerY - height * 0.5f);
        float maxY = Mathf.Clamp01(centerY + height * 0.5f);
        rt.anchorMin = new Vector2(sideInset, minY);
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
