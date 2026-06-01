using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display 1 debug overlay for the current number of active touch effects.
/// </summary>
public class TouchEffectCountDebugOverlay : MonoBehaviour
{
    [Header("─── Display ───────────────────────────────────────────────────")]
    public bool showOverlay = true;
    [Tooltip("0 = Display 1, 1 = Display 2, ...")]
    public int displayIndex = 0;
    public int sortingOrder = 500;

    [Header("─── Layout ────────────────────────────────────────────────────")]
    [Range(0f, 1f)] public float anchorX = 0.02f;
    [Range(0f, 1f)] public float anchorY = 0.93f;
    [Range(80f, 600f)] public float width = 360f;
    [Range(30f, 180f)] public float height = 54f;

    [Header("─── Style ─────────────────────────────────────────────────────")]
    public TMP_FontAsset font;
    [Range(10f, 72f)] public float fontSize = 28f;
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);

    private InteractionVFXController vfx;
    private DistractionManager dm;
    private CanvasGroup group;
    private Image background;
    private TextMeshProUGUI label;
    private RectTransform rootRT;
    private bool timerRunning;
    private float timerSeconds;

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

        if (group != null)
            group.alpha = showOverlay ? 1f : 0f;

        int activeCount = vfx != null ? vfx.ActiveTouchCount : 0;
        UpdateTimer(activeCount);

        if (label != null)
            label.text = $"Active touch effects: {activeCount}\nTimer: {timerSeconds:F1}s";
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || label == null) return;
        ApplyInspectorValues();
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("TouchEffectCountDebugCanvas");
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

        GameObject rootGO = new GameObject("TouchEffectCountDebugRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        group = rootGO.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        rootRT = rootGO.AddComponent<RectTransform>();

        background = rootGO.AddComponent<Image>();
        background.raycastTarget = false;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rootGO.transform, false);
        label = labelGO.AddComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);
    }

    private void ApplyInspectorValues()
    {
        if (rootRT != null)
        {
            rootRT.anchorMin = new Vector2(anchorX, anchorY);
            rootRT.anchorMax = new Vector2(anchorX, anchorY);
            rootRT.pivot = new Vector2(0f, 1f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(width, height);
        }

        if (background != null)
            background.color = backgroundColor;

        if (label != null)
        {
            label.fontSize = fontSize;
            label.color = textColor;
            if (font != null) label.font = font;
        }
    }

    private void UpdateTimer(int activeCount)
    {
        bool loopEnded = dm != null && !dm.HasExperienceStarted;
        if (loopEnded)
        {
            timerRunning = false;
            timerSeconds = 0f;
            return;
        }

        if (!timerRunning && activeCount > 0)
            timerRunning = true;

        if (timerRunning)
            timerSeconds += Time.deltaTime;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
