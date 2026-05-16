using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all body-interaction VFX: touch (Liquid Glass), grounding border,
/// shielded state, blow fade-out, and level-3 camera shake.
/// Water-level broken mirror effect is handled separately by BrokenMirrorLevelBridge.
/// </summary>
public class InteractionVFXController : MonoBehaviour
{
    // ── UI References ─────────────────────────────────────────────────────────

    [Header("UI References  [leave empty → auto-created]")]
    public Image      colorOverlay;
    public Image      groundingBorder;
    public CanvasGroup vfxGroup;
    public Camera     mainCamera;

    // ── Water Level Colors ────────────────────────────────────────────────────

    [Header("Water Level Overlay Colors")]
    [Tooltip("Disable this when BrokenMirrorLevelBridge is used — the mirror effect replaces the overlay.")]
    public bool enableLevelColorOverlay = false;
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

    [Tooltip("Fixed time (seconds) for the spawn-in animation regardless of effect lifetime.")]
    [Range(0.05f, 1.0f)]
    public float touchSpawnDuration = 0.30f;
    [Tooltip("Fixed time (seconds) for the despawn animation regardless of effect lifetime.")]
    [Range(0.05f, 1.0f)]
    public float touchDespawnDuration = 0.30f;

    // ── Camera Shake ──────────────────────────────────────────────────────────

    [Header("Camera Shake  (Level 3 only)")]
    public float shakeAmount = 4f;
    public float shakeSpeed  = 15f;

    // ── Blow Fade ─────────────────────────────────────────────────────────────

    [Header("Blow — Timing")]
    [Tooltip("How long the UI border/overlay fades out (seconds).")]
    public float blowFadeDuration = 1.5f;

    [Header("Blow — Star Material")]
    [Tooltip("Material using Custom/StarGlow shader — create from StarGlow.shader in Project window.")]
    public Material starGlowMaterial;

    [Header("Blow — Star Count & Size")]
    [Tooltip("Number of star particles spawned per active touch effect.")]
    [Range(4, 120)] public int starsPerEffect = 12;

    [Tooltip("Smallest star diameter in screen pixels.")]
    [Range(2f, 80f)] public float starSizePixelMin = 10f;

    [Tooltip("Largest star diameter in screen pixels.")]
    [Range(5f, 160f)] public float starSizePixelMax = 45f;

    [Header("Blow — Star Shape")]
    [Tooltip("Probability (0–1) that a particle is a 4-pointed star rather than a soft dot.")]
    [Range(0f, 1f)] public float starChance = 0.55f;

    [Tooltip("Tip sharpness range — higher = more needle-like, lower = rounder.")]
    public Vector2 starSharpnessRange = new Vector2(4f, 8f);

    [Tooltip("Softness of the star edge — lower = crisper, higher = softer.")]
    public Vector2 starSoftnessRange = new Vector2(0.04f, 0.14f);

    [Tooltip("Outer glow radius — lower = tight sparkle, higher = wide halo.")]
    public Vector2 starHaloRange = new Vector2(0.20f, 0.50f);

    [Header("Blow — Star Timing")]
    [Tooltip("Duration (seconds) for the glass to shrink away AFTER the stars have appeared.")]
    [Range(0.1f, 1.0f)] public float blowTransitionDuration = 0.40f;

    [Tooltip("Max random micro-delay (seconds) before each star pops in. Keep small so stars appear while the glass is still visible.")]
    [Range(0f, 0.3f)] public float starMaxDelay = 0.05f;

    [Tooltip("Min and max fade-out duration for each individual star (seconds).")]
    public Vector2 starLifetimeRange = new Vector2(0.5f, 1.1f);

    [Header("Blow — Star Brightness")]
    [Tooltip("HDR brightness multiplier range — raise max for more bloom.")]
    public Vector2 starBrightnessRange = new Vector2(1.2f, 2.5f);

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

    // Per-instance data for each active touch glass effect
    private class TouchGlassInstance
    {
        public GameObject go;
        public Material   mat;
        public float      alpha;
        public float      fadeTime;
        public float      elapsed;
    }

    private class StarParticle
    {
        public GameObject go;
        public Material   mat;
        public float      delay;      // seconds before fade begins
        public float      lifetime;   // fade duration after delay
        public float      age;        // running age in seconds
    }
    private readonly List<TouchGlassInstance> activeInstances = new List<TouchGlassInstance>();
    private readonly List<StarParticle>       activeStars     = new List<StarParticle>();

    private static readonly int s_SpawnProgressId   = Shader.PropertyToID("_SpawnProgress");
    private static readonly int s_BlowFadeId        = Shader.PropertyToID("_BlowFade");
    private static readonly int s_RotationOffsetId  = Shader.PropertyToID("_RotationOffset");
    private static readonly int s_TimeOffsetId      = Shader.PropertyToID("_TimeOffset");
    private static readonly int s_StarAlphaId       = Shader.PropertyToID("_Alpha");

    private int prevIsTouching = 0;

    // Keyboard-fallback level (persists between frames when hw is null)
    private int _kbLevel = 0;

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
        ResetUI();

        if (groundingBorder != null)
        {
            groundingBorder.type       = Image.Type.Filled;
            groundingBorder.fillMethod = Image.FillMethod.Horizontal;
            groundingBorder.fillOrigin = (int)Image.OriginHorizontal.Left;
            groundingBorder.fillAmount = 0f;
            groundingBorder.enabled    = false;
        }

        if (liquidGlassMaterial == null)
            Debug.LogWarning("[VFX] Liquid Glass Material is NOT assigned! " +
                             "Drag 'LiquidGlassAnomaly_Mat' from _Shader/ onto this component.");

        // Force shader compilation before the first blow so there's no hitch at runtime
        if (starGlowMaterial != null) StartCoroutine(WarmupStarShader());
    }

    IEnumerator WarmupStarShader()
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) yield break;

        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dummy.name = "StarShaderWarmup";
        Destroy(dummy.GetComponent<MeshCollider>());
        dummy.transform.SetParent(cam.transform, false);
        dummy.transform.localPosition = new Vector3(0f, 0f, touchEffectDepth);
        dummy.transform.localScale    = Vector3.one * 0.0001f;   // too small to see
        Material dummyMat = new Material(starGlowMaterial);
        dummyMat.SetFloat(s_StarAlphaId, 0f);
        dummy.GetComponent<MeshRenderer>().material = dummyMat;

        yield return null;   // one frame is enough to compile the shader

        Destroy(dummyMat);
        Destroy(dummy);
    }

    void OnDestroy()
    {
        DestroyAllInstances();
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

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (hw == null) hw = DeviceInputManager.Instance;
        if (dm == null) dm = DistractionManager.Instance;

        if (dm != null && groundingDuration != dm.groundingDuration)
        {
            groundingDuration = dm.groundingDuration;
            backtrackSpeed    = dm.backtrackSpeed;
        }

        UpdateStars();

        if (blowInProgress) return;

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
        if (!enableLevelColorOverlay) { colorOverlay.color = Color.clear; return; }

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
    //  Each rising edge of isTouching spawns a brand-new Quad instance at a
    //  random screen position.  Every instance captures dm.CurrentFadeTime at
    //  the moment it is created and fades independently over that duration.
    //  Old instances continue fading while new ones appear — no limit on count.

    void UpdateTouchGlass(int isTouchingValue)
    {
        bool touching     = (isTouchingValue == 1);
        bool isRisingEdge = touching && (prevIsTouching == 0);
        prevIsTouching    = isTouchingValue;

        if (isRisingEdge)
            SpawnTouchGlassInstance();

        // Tick every active instance independently
        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            TouchGlassInstance inst = activeInstances[i];

            inst.elapsed += Time.deltaTime;

            // Spawn-in and despawn always take fixed seconds; only the hold
            // period in the middle stretches with fadeTime. This prevents the
            // entrance/exit animation from slowing down as fadeTime grows.
            float spawnDur   = Mathf.Max(0.05f, touchSpawnDuration);
            float despawnDur = Mathf.Max(0.05f, touchDespawnDuration);
            float holdDur    = Mathf.Max(0f, inst.fadeTime - spawnDur - despawnDur);
            float progress;
            if (inst.elapsed < spawnDur)
                progress = (inst.elapsed / spawnDur) * 0.25f;
            else if (inst.elapsed < spawnDur + holdDur)
                progress = 0.25f + ((inst.elapsed - spawnDur) / Mathf.Max(0.001f, holdDur)) * 0.50f;
            else
                progress = 0.75f + Mathf.Clamp01((inst.elapsed - spawnDur - holdDur) / despawnDur) * 0.25f;

            inst.mat.SetFloat(s_SpawnProgressId, progress);

            if (inst.elapsed >= inst.fadeTime)
            {
                inst.go.SetActive(false);
                Destroy(inst.mat);
                Destroy(inst.go);
                activeInstances.RemoveAt(i);
            }
        }
    }

    const float StarFadeInDur = 0.10f;  // quick pop-in before the fade-out begins

    void UpdateStars()
    {
        for (int i = activeStars.Count - 1; i >= 0; i--)
        {
            StarParticle star = activeStars[i];
            star.age += Time.deltaTime;

            float total = star.delay + StarFadeInDur + star.lifetime;
            if (star.age >= total)
            {
                if (star.mat != null) Destroy(star.mat);
                if (star.go  != null) Destroy(star.go);
                activeStars.RemoveAt(i);
                continue;
            }

            float alpha;
            if (star.age < star.delay)
            {
                alpha = 0f;                                        // waiting — invisible
            }
            else
            {
                float local = star.age - star.delay;
                if (local < StarFadeInDur)
                    alpha = local / StarFadeInDur;                 // linear pop-in
                else
                {
                    float t = Mathf.Clamp01((local - StarFadeInDur) / star.lifetime);
                    alpha = 1f - (t * t * t);                      // cubic ease-out
                }
            }

            if (star.mat != null) star.mat.SetFloat(s_StarAlphaId, alpha);
        }
    }

    // Spawns exactly one star quad for the given instance. Called from BlowRoutine
    // a few at a time per frame to avoid an instantiation spike.
    void SpawnOneStar(Camera cam, TouchGlassInstance inst, float worldPerPixel)
    {
        if (inst.go == null) return;

        Vector3 center = inst.go.transform.localPosition;
        float halfW    = inst.go.transform.localScale.x * 0.5f;
        float halfH    = inst.go.transform.localScale.y * 0.5f;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "StarParticle";
        Destroy(go.GetComponent<MeshCollider>());
        go.transform.SetParent(cam.transform, false);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        Material mat = new Material(starGlowMaterial);
        mr.material = mat;

        // Spawn within the visible blob (shader blob radius ≈ 0.45 of quad half-size).
        // Disk distribution (sqrt keeps density uniform, not centre-heavy).
        float blobRadius = Mathf.Min(halfW, halfH) * 0.50f;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Mathf.Sqrt(Random.value) * blobRadius;
        float lx = center.x + Mathf.Cos(angle) * dist;
        float ly = center.y + Mathf.Sin(angle) * dist;
        go.transform.localPosition = new Vector3(lx, ly, center.z);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));

        float pMin = Mathf.Min(starSizePixelMin, starSizePixelMax);
        float pMax = Mathf.Max(starSizePixelMin, starSizePixelMax);
        float size = Random.Range(pMin, pMax) * worldPerPixel;
        go.transform.localScale = new Vector3(size, size, 1f);

        mat.SetFloat("_StarType",  Random.value < starChance ? 1.0f : 0.0f);
        mat.SetFloat("_Sharpness", Random.Range(starSharpnessRange.x, starSharpnessRange.y));
        mat.SetFloat("_Softness",  Random.Range(starSoftnessRange.x,  starSoftnessRange.y));
        mat.SetFloat("_HaloSize",  Random.Range(starHaloRange.x,      starHaloRange.y));

        float warm   = Random.value;
        float bright = Random.Range(starBrightnessRange.x, starBrightnessRange.y);
        // warm=0 → cool ice-white (R low, B high), warm=1 → golden-white (R high, B low)
        // G stays near 0.96 so there's always enough green to keep the star white, not pink
        float r = Mathf.Lerp(0.88f, 1.00f, warm);
        float g = 0.96f;
        float b = Mathf.Lerp(1.00f, 0.80f, warm);
        mat.SetColor("_Color", new Color(r * bright, g * bright, b * bright, 1f));
        mat.SetFloat(s_StarAlphaId, 0f);  // starts invisible; UpdateStars fades it in

        float lMin = Mathf.Min(starLifetimeRange.x, starLifetimeRange.y);
        float lMax = Mathf.Max(starLifetimeRange.x, starLifetimeRange.y);
        activeStars.Add(new StarParticle
        {
            go       = go,
            mat      = mat,
            delay    = Random.Range(0f, Mathf.Max(0f, starMaxDelay)),
            lifetime = Random.Range(lMin, lMax),
            age      = 0f
        });
    }

    void SpawnTouchGlassInstance()
    {
        if (liquidGlassMaterial == null) return;

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) { Debug.LogWarning("[VFX] No camera found — cannot spawn touch glass."); return; }

        // Build Quad
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "TouchGlassEffect";
        Destroy(go.GetComponent<MeshCollider>());
        go.transform.SetParent(cam.transform, false);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        Material mat = new Material(liquidGlassMaterial);
        mr.material = mat;

        // Random position within padded viewport
        float pad   = Mathf.Clamp(touchEffectPadding, 0f, 0.45f);
        float vx    = Random.Range(pad, 1f - pad);
        float vy    = Random.Range(pad, 1f - pad);
        float depth = Mathf.Max(cam.nearClipPlane + 0.05f, touchEffectDepth);

        // ViewportToWorldPoint handles any camera mode / aspect ratio correctly
        Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(vx, vy, depth));
        Vector3 localPos = cam.transform.InverseTransformPoint(worldPos);

        // Derive world-space size of the full viewport at this depth
        float worldW = Vector3.Distance(
            cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)),
            cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)));
        float worldH = Vector3.Distance(
            cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)),
            cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth)));

        float scaleX = (touchEffectPixelSize.x / Screen.width)  * worldW;
        float scaleY = (touchEffectPixelSize.y / Screen.height) * worldH;

        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = new Vector3(scaleX, scaleY, 1f);

        // Capture fade duration at spawn time so each instance is independent
        float fadeTime = (dm != null) ? dm.CurrentFadeTime : 1.5f;

        // ── Per-instance visual randomisation ─────────────────────────────────
        // Hue: shift each instance to a different segment of the iridescence cycle
        mat.SetFloat("_IriOffset", Random.value);

        // Shape: unique noise seed → unique organic blob silhouette each spawn
        mat.SetFloat("_ShapeIrregularity", Random.Range(0.18f, 0.35f));
        mat.SetFloat("_ShapeOffsetX",      Random.Range(0f, 100f));
        mat.SetFloat("_ShapeOffsetY",      Random.Range(0f, 100f));
        mat.SetFloat(s_RotationOffsetId,   Random.Range(0f, Mathf.PI * 2f));
        mat.SetFloat(s_TimeOffsetId,       Random.Range(0f, 100f));

        // Core colour: cycle through vivid HDR hues so simultaneous instances
        // are visually distinct even without looking at the blob shape
        Color[] coreHues =
        {
            new Color(3.0f, 0.4f, 0.1f, 1f),   // hot orange
            new Color(0.1f, 2.5f, 0.5f, 1f),   // electric green
            new Color(0.4f, 0.1f, 3.0f, 1f),   // deep violet
            new Color(2.8f, 0.1f, 1.2f, 1f),   // magenta-pink
            new Color(0.1f, 2.0f, 3.0f, 1f),   // electric cyan
            new Color(3.0f, 2.2f, 0.1f, 1f),   // gold-yellow
            new Color(0.1f, 0.8f, 2.8f, 1f),   // sky blue
            new Color(2.5f, 0.1f, 0.5f, 1f),   // crimson
        };
        mat.SetColor("_CoreColor", coreHues[Random.Range(0, coreHues.Length)]);

        // Extra variety: randomise glow energy so instances feel different in weight
        mat.SetFloat("_IriIntensity", Random.Range(1.8f, 4.5f));
        mat.SetFloat("_TendrilGlow",  Random.Range(1.2f, 4.5f));
        mat.SetFloat("_CoreEmission", Random.Range(8f,  22f));
        // ─────────────────────────────────────────────────────────────────────

        mat.SetFloat(s_SpawnProgressId, 0.0f);
        go.SetActive(true);

        activeInstances.Add(new TouchGlassInstance
        {
            go       = go,
            mat      = mat,
            alpha    = touchGlassMaxOpacity,   // retained for BlowRoutine
            fadeTime = fadeTime,
            elapsed  = 0f
        });

        Debug.Log($"[VFX] TouchGlass spawned at viewport ({vx:F2}, {vy:F2})  " +
                  $"fadeTime={fadeTime:F1}s  active={activeInstances.Count}");
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
        Camera cam = mainCamera != null ? mainCamera : Camera.main;

        // ── Setup ─────────────────────────────────────────────────────────────────
        List<TouchGlassInstance> instances = new List<TouchGlassInstance>(activeInstances);

        float depth = instances.Count > 0 && instances[0].go != null
            ? instances[0].go.transform.localPosition.z
            : touchEffectDepth;
        float worldPerPixel = cam != null
            ? Vector3.Distance(
                cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)),
                cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth))) / Screen.height
            : 0.001f;

        // Interleave: inst0, inst1, inst0, inst1, … so all effects get
        // their first star in the same frame and appear in sync.
        var spawnQ = new Queue<TouchGlassInstance>(instances.Count * starsPerEffect);
        for (int j = 0; j < starsPerEffect; j++)
            foreach (TouchGlassInstance inst in instances)
                spawnQ.Enqueue(inst);

        // ── Phase 1+2 combined: glass fades out WHILE stars simultaneously appear ──
        // Use _BlowFade (1→0) instead of _SpawnProgress so effects that are still
        // spawning-in don't flash to full visibility before fading out.
        float trans = 0f;
        while (trans < blowTransitionDuration || spawnQ.Count > 0)
        {
            trans += Time.deltaTime;
            float t = Mathf.Clamp01(trans / blowTransitionDuration);

            // Fade via independent multiplier — works at any SpawnProgress value
            for (int i = 0; i < instances.Count; i++)
                if (instances[i].mat != null)
                    instances[i].mat.SetFloat(s_BlowFadeId, 1.0f - t);

            // Stars trickle in alongside the glass fade
            if (cam != null)
                for (int n = 0; n < 8 && spawnQ.Count > 0; n++)
                    SpawnOneStar(cam, spawnQ.Dequeue(), worldPerPixel);

            yield return null;
        }

        foreach (TouchGlassInstance inst in instances)
            if (inst.go != null) inst.go.SetActive(false);

        // ── Phase 3: fade UI (border / overlay) out ──────────────────────────────
        Color startOverlay = colorOverlay    != null ? colorOverlay.color    : Color.clear;
        Color startBorder  = groundingBorder != null ? groundingBorder.color : Color.clear;

        float elapsed = 0f;
        while (elapsed < blowFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / blowFadeDuration);
            if (colorOverlay    != null)
                colorOverlay.color    = new Color(startOverlay.r, startOverlay.g, startOverlay.b, startOverlay.a * t);
            if (groundingBorder != null)
                groundingBorder.color = new Color(startBorder.r,  startBorder.g,  startBorder.b,  startBorder.a  * t);
            yield return null;
        }

        // ── Phase 3: wait for all stars to finish ────────────────────────────────
        float starWait = 0f;
        while (activeStars.Count > 0 && starWait < 3f)
        {
            starWait += Time.deltaTime;
            yield return null;
        }

        HardReset();
    }

    // ── Debug Overlay ─────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (!showDebugOverlay) return;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(8f, 8f, 280f, 230f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float y = 14f;
        string hwStatus  = hw  != null ? "OK" : "NULL  ← check scene";
        string dmStatus  = dm  != null ? "OK" : "NULL";
        string matStatus = liquidGlassMaterial != null ? "assigned" : "MISSING  ← drag mat here!";

        GUI.Label(new Rect(14f, y, 270f, 20f), "[VFXController]"); y += 20f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"DeviceInputManager : {hwStatus}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"DistractionManager : {dmStatus}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"LiquidGlassMaterial: {matStatus}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"Active instances   : {activeInstances.Count}"); y += 18f;

        int touch = GetInputTouching();
        GUI.Label(new Rect(14f, y, 270f, 20f), $"isTouching (input) : {touch}"); y += 18f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"VFX state          : {vfxState}"); y += 18f;

        float fadeTime = dm != null ? dm.CurrentFadeTime : 1.5f;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"Current fadeTime   : {fadeTime:F1}s"); y += 18f;

        float camDepth = mainCamera != null ? mainCamera.farClipPlane : -1f;
        GUI.color = Color.yellow;
        GUI.Label(new Rect(14f, y, 270f, 20f), $"depth={touchEffectDepth:F1}  farClip={camDepth:F0}  enable Opaque Texture!");
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

    void DestroyAllInstances()
    {
        foreach (TouchGlassInstance inst in activeInstances)
        {
            if (inst.mat != null) Destroy(inst.mat);
            if (inst.go  != null) Destroy(inst.go);
        }
        activeInstances.Clear();
    }

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
        Image img         = go.AddComponent<Image>();
        img.color         = startColor;
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

        DestroyAllInstances();
    }

    void HardReset()
    {
        foreach (StarParticle star in activeStars)
        {
            if (star.mat != null) Destroy(star.mat);
            if (star.go  != null) Destroy(star.go);
        }
        activeStars.Clear();

        vfxState       = VFXState.Normal;
        groundingTimer = 0f;
        prevIsTouching = 0;

        ResetUI();

        if (mainCamera != null) mainCamera.transform.localPosition = camOrigin;
        if (vfxGroup   != null) vfxGroup.alpha = 1f;

        blowInProgress = false;
    }
}
