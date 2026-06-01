using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows short Display 1 text prompts when the number of simultaneous touch effects
/// reaches configurable thresholds.
/// </summary>
public class TouchEffectCountPrompt : MonoBehaviour
{
    [System.Serializable]
    public class PromptThreshold
    {
        [Min(1)] public int touchEffectCount = 10;
        [TextArea(1, 4)] public string text = "Notice how the distractions are accumulating.";
        public AudioClip audioClip;
        [Range(0f, 3f)] public float audioVolume = 1f;
        [HideInInspector] public bool triggered;
    }

    [Header("─── Trigger Thresholds ─────────────────────────────────────────")]
    public PromptThreshold[] thresholds =
    {
        new PromptThreshold
        {
            touchEffectCount = 10,
            text = "The distractions are building."
        },
        new PromptThreshold
        {
            touchEffectCount = 15,
            text = "It is getting harder to return to stillness."
        }
    };

    [Header("─── Display ───────────────────────────────────────────────────")]
    [Tooltip("0 = Display 1, 1 = Display 2, ...")]
    public int displayIndex = 0;

    [Header("─── Text ──────────────────────────────────────────────────────")]
    public TMP_FontAsset font;
    [Range(12f, 96f)] public float fontSize = 40f;
    public Color textColor = Color.white;
    public FontStyles fontStyle = FontStyles.Normal;

    [Tooltip("Vertical centre of the text block. 0 = bottom, 1 = top.")]
    [Range(0f, 1f)] public float textCenterY = 0.58f;
    [Tooltip("Height of the text area as a fraction of screen height.")]
    [Range(0.04f, 0.5f)] public float textHeight = 0.18f;
    [Tooltip("Horizontal padding on each side as a fraction of screen width.")]
    [Range(0f, 0.45f)] public float textSidePadding = 0.10f;

    [Header("─── Background ────────────────────────────────────────────────")]
    public Color overlayColor = new Color(0f, 0f, 0f, 0.35f);

    [Header("─── Timing ────────────────────────────────────────────────────")]
    [Range(0.05f, 5f)] public float fadeInDuration = 0.8f;
    [Range(0f, 10f)] public float holdDuration = 2.2f;
    [Range(0.05f, 5f)] public float fadeOutDuration = 0.8f;

    private InteractionVFXController vfx;
    private DistractionManager dm;
    private CanvasGroup group;
    private Image background;
    private TextMeshProUGUI label;
    private RectTransform labelRT;
    private AudioSource audioSource;
    private Coroutine promptRoutine;
    private bool wasExperienceStarted;

    private void Start()
    {
        vfx = FindObjectOfType<InteractionVFXController>();
        dm = DistractionManager.Instance;
        BuildUI();
        ApplyInspectorValues();
    }

    private void Update()
    {
        if (vfx == null) vfx = FindObjectOfType<InteractionVFXController>();
        if (dm == null) dm = DistractionManager.Instance;
        if (vfx == null) return;

        // A loop starts at the first touch and ends when DistractionManager resets
        // HasExperienceStarted, which happens after blow/clear or R reset.
        bool experienceStarted = dm == null || dm.HasExperienceStarted;
        if (wasExperienceStarted && !experienceStarted)
            ResetThresholds();
        wasExperienceStarted = experienceStarted;

        if (!CanShowPromptNow())
        {
            HideCurrentPrompt();
            return;
        }

        int activeCount = vfx.ActiveTouchCount;
        for (int i = 0; i < thresholds.Length; i++)
        {
            PromptThreshold threshold = thresholds[i];
            if (threshold == null) continue;

            if (!threshold.triggered && activeCount >= threshold.touchEffectCount)
            {
                threshold.triggered = true;
                ShowPrompt(threshold);
            }
        }
    }

    private bool CanShowPromptNow()
    {
        if (dm != null)
        {
            if (!dm.HasExperienceStarted) return false;
            if (dm.IsGrounding || dm.IsShielded || dm.IsInCooldown) return false;
        }

        return GroundingPrompt.Instance == null || !GroundingPrompt.Instance.HasTriggered;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || label == null) return;
        ApplyInspectorValues();
    }

    private void ShowPrompt(PromptThreshold threshold)
    {
        if (group == null || label == null) return;

        if (promptRoutine != null)
            StopCoroutine(promptRoutine);

        label.text = threshold.text;
        PlayThresholdAudio(threshold);
        promptRoutine = StartCoroutine(PlayPromptRoutine());
    }

    private void PlayThresholdAudio(PromptThreshold threshold)
    {
        if (threshold == null || threshold.audioClip == null) return;

        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        audioSource.PlayOneShot(threshold.audioClip, threshold.audioVolume);
    }

    private IEnumerator PlayPromptRoutine()
    {
        yield return FadeTo(1f, fadeInDuration);
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);
        yield return FadeTo(0f, fadeOutDuration);
        promptRoutine = null;
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

    private void ResetThresholds()
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (thresholds[i] != null)
                thresholds[i].triggered = false;
        }

        if (promptRoutine != null)
        {
            StopCoroutine(promptRoutine);
            promptRoutine = null;
        }

        if (group != null)
            group.alpha = 0f;
    }

    public void ForceResetPromptState()
    {
        wasExperienceStarted = false;
        ResetThresholds();
    }

    private void HideCurrentPrompt()
    {
        if (promptRoutine != null)
        {
            StopCoroutine(promptRoutine);
            promptRoutine = null;
        }

        if (group != null && group.alpha > 0f)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("TouchEffectCountPromptCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = displayIndex;
        canvas.sortingOrder = 155;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject rootGO = new GameObject("TouchEffectCountPromptRoot");
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
            label.fontSize = fontSize;
            label.color = textColor;
            label.fontStyle = fontStyle;
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
