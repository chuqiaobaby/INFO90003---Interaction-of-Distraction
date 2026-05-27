using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Triggers a water-ripple overlay on Display 1 when the user completes blow.
/// Drives _LocalTime (0 = drop impact) and _Intensity (global fade) each frame.
///
/// Setup
/// ─────
///  1. Add this script to any GameObject.
///  2. Create a Material using shader "Custom/BlowWaterRipple".
///     Drag it into the 'Ripple Material' slot.
/// </summary>
public class BlowWaterRippleController : MonoBehaviour
{
    [Header("─── Material ───────────────────────────────────────────────────")]
    public Material rippleMaterial;

    [Header("─── Display ────────────────────────────────────────────────────")]
    public int displayIndex = 0;

    [Header("─── Timing ─────────────────────────────────────────────────────")]
    [Tooltip("Seconds to fade the overlay in after blow.")]
    [Range(0.05f, 1f)]  public float fadeInDuration  = 0.15f;
    [Tooltip("Total seconds _LocalTime runs before fading out. " +
             "Should be long enough for the outermost rings to reach the edge.")]
    [Range(1f, 10f)]    public float effectDuration  = 4.0f;
    [Tooltip("Seconds to fade the overlay out at the end.")]
    [Range(0.2f, 4f)]   public float fadeOutDuration = 1.8f;

    // ── Internals ─────────────────────────────────────────────────────────────

    private Material  runtimeMat;
    private Coroutine playRoutine;
    private bool      wasInCooldown;

    private static readonly int PropIntensity  = Shader.PropertyToID("_Intensity");
    private static readonly int PropLocalTime  = Shader.PropertyToID("_LocalTime");
    private static readonly int PropAspect     = Shader.PropertyToID("_AspectRatio");

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildUI();
    }

    private void Update()
    {
        DistractionManager dm = DistractionManager.Instance;
        bool inCooldown = dm != null && dm.IsInCooldown;

        if (inCooldown && !wasInCooldown)
            TriggerEffect();

        if (runtimeMat != null && Screen.width > 0 && Screen.height > 0)
            runtimeMat.SetFloat(PropAspect, (float)Screen.width / Screen.height);

        wasInCooldown = inCooldown;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    public void TriggerEffect()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayRoutine());
    }

    // ── Canvas setup ──────────────────────────────────────────────────────────

    private void BuildUI()
    {
        runtimeMat = rippleMaterial != null
            ? new Material(rippleMaterial)
            : new Material(Shader.Find("Custom/BlowWaterRipple"));

        if (runtimeMat == null)
        {
            Debug.LogError("[BlowWaterRippleController] Shader 'Custom/BlowWaterRipple' not found.");
            return;
        }

        runtimeMat.SetFloat(PropIntensity, 0f);
        runtimeMat.SetFloat(PropLocalTime, 0f);

        // Canvas (Display 1, on top of everything)
        var canvasGO         = new GameObject("BlowRippleCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas           = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = displayIndex;
        canvas.sortingOrder  = 300;
        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var rootGO                 = new GameObject("BlowRippleRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        var cg                     = rootGO.AddComponent<CanvasGroup>();
        cg.alpha                   = 1f;
        cg.blocksRaycasts          = false;
        cg.interactable            = false;
        Stretch(rootGO.AddComponent<RectTransform>());

        var imgGO              = new GameObject("RippleImage");
        imgGO.transform.SetParent(rootGO.transform, false);
        var raw                = imgGO.AddComponent<RawImage>();
        raw.material           = runtimeMat;
        raw.color              = Color.white;
        raw.raycastTarget      = false;
        Stretch(raw.rectTransform);
    }

    // ── Effect coroutine ──────────────────────────────────────────────────────

    private IEnumerator PlayRoutine()
    {
        if (runtimeMat == null) yield break;

        // Reset local time to 0 — this is "drop impact"
        runtimeMat.SetFloat(PropLocalTime, 0f);

        float localTime = 0f;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed   += Time.deltaTime;
            localTime += Time.deltaTime;
            runtimeMat.SetFloat(PropIntensity, Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration));
            runtimeMat.SetFloat(PropLocalTime, localTime);
            yield return null;
        }
        runtimeMat.SetFloat(PropIntensity, 1f);

        // Run — keep advancing _LocalTime until effectDuration
        while (localTime < effectDuration)
        {
            localTime += Time.deltaTime;
            runtimeMat.SetFloat(PropLocalTime, localTime);
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed   += Time.deltaTime;
            localTime += Time.deltaTime;
            runtimeMat.SetFloat(PropIntensity, Mathf.SmoothStep(1f, 0f, elapsed / fadeOutDuration));
            runtimeMat.SetFloat(PropLocalTime, localTime);
            yield return null;
        }

        runtimeMat.SetFloat(PropIntensity, 0f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (runtimeMat != null) Destroy(runtimeMat);
    }
}
