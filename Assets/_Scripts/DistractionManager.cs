using UnityEngine;

/// <summary>
/// Central controller that reads hardware input and drives the face shader state.
/// </summary>
public class DistractionManager : MonoBehaviour
{
    public enum SystemState
    {
        Normal,
        Grounding,
        Shielded,
        Cooldown
    }

    public static DistractionManager Instance { get; private set; }

    public float CurrentFadeTime        => currentFadeTime;
    public bool  IsGrounding            => currentState == SystemState.Grounding;
    public bool  IsShielded             => currentState == SystemState.Shielded;
    public bool  IsInCooldown           => currentState == SystemState.Cooldown;
    public bool  HasExperienceStarted   => hasExperienceStarted;
    /// <summary>Seconds elapsed since the first touch. Read by GroundingPrompt.</summary>
    public float GlobalDistractionTimer => globalDistractionTimer;
    /// <summary>Current water level from hardware sensor (0–3). Read by SFXController.</summary>
    public int   HardwareLevel          => hardwareLevel;

    [Header("--- Shader Properties (MUST MATCH SHADER GRAPH) ---")]
    public Material faceMaterial;
    public string waterLevelEffectProperty = "_GlobalDistortion";
    public string touchEffectProperty = "_LocalGlitchIntensity";
    public string shieldProperty = "_ShieldActive";
    public string traceProgressProperty = "_TraceProgress";
    public string touchActiveProperty = "_TouchActive";
    public string blowActiveProperty = "_BlowTrigger";

    [Header("--- 1. WATER LEVEL EFFECT (Permanent Layer) ---")]
    [Tooltip("Minimum distortion when water level is 0")]
    public float minWaterLevelEffect = 0f;
    [Tooltip("Maximum distortion when water level is 3")]
    public float maxWaterLevelEffect = 1f;
    [Tooltip("How smoothly the visual transitions when water level changes")]
    public float effectSmoothSpeed = 5f;

    [Header("--- 2. TOUCH EFFECT FADE SETTINGS (Temporary Layer) ---")]
    [Tooltip("Maximum intensity applied the moment you touch the water")]
    public float touchEffectStrength = 1f;
    [Tooltip("Seconds from the VERY FIRST touch until the system reaches maximum Distraction")]
    public float timeToMaxDistraction = 120f; 
    [Space(10)]
    [Tooltip("How fast the touch effect disappears at the beginning of the experience")]
    public float fastFadeTimeAtStart = 1.5f;
    [Tooltip("How long it takes for the touch effect to disappear when Distraction is at max")]
    public float slowFadeTimeAtEnd = 180f;
    [Tooltip("Curve controlling the transition from fast fade to slow fade over time")]
    public AnimationCurve fadeTimeCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 1f)
    );

    [Header("--- 3. GROUNDING & PURIFY ---")]
    [Tooltip("Seconds hands must be pressed to the bottom to activate shield")]
    public float groundingDuration = 5f;
    [Tooltip("How fast the tracing reverses if hands are released early")]
    public float backtrackSpeed = 1f;
    [Tooltip("开启后：只有 GroundingPrompt 已经触发过提示，才允许进入 Grounding 状态。\n" +
             "关闭后：随时都可以 grounding（旧行为）。")]
    public bool requireGroundingPrompt = true;

    [Header("--- 4. COOLDOWN BETWEEN USERS ---")]
    [Tooltip("Seconds to block all interactions after a complete grounding + blow flow (before next user can start).")]
    public float cooldownDuration = 20f;

    [Header("--- DEBUG MONITOR (Read Only) ---")]
    [SerializeField] private SystemState currentState = SystemState.Normal;
    [SerializeField] private float groundingTimer = 0f;
    [SerializeField] private float traceProgress = 0f;
    [SerializeField] private float globalDistractionTimer = 0f;
    [SerializeField] private bool hasExperienceStarted = false;
    [SerializeField] private int hardwareLevel = 0;
    [SerializeField] private int isTouching = 0;
    [SerializeField] private int isGrounding = 0;
    [SerializeField] private int isBlowing = 0;
    
    [SerializeField] private float currentWaterLevelEffect = 0f;
    [SerializeField] private float lockedWaterLevelEffect = 0f;
    [SerializeField] private float currentTouchEffect = 0f;
    [SerializeField] private float currentFadeTime = 1.5f;
    [SerializeField] private float cooldownTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ApplyShieldState(false);
        ResetTemporaryEffects();
        currentWaterLevelEffect = minWaterLevelEffect;
        if (faceMaterial != null) faceMaterial.SetFloat(waterLevelEffectProperty, currentWaterLevelEffect);
        currentFadeTime = fastFadeTimeAtStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) { FullReset(); return; }

        if (currentState == SystemState.Cooldown)
        {
            UpdateCooldown();
            return;
        }

        ReadHardwareInput();
        UpdateGlobalDistractionTimer();
        UpdateWaterLevelEffect();
        UpdateStateMachine();
        UpdateTouchEffect();
        PushTraceProgressToShader();
    }

    private void UpdateCooldown()
    {
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < cooldownDuration) return;

        currentState  = SystemState.Normal;
        cooldownTimer = 0f;
        if (InstructionDisplay.Instance != null)
            InstructionDisplay.Instance.ShowInstruction();
    }

    private void EnterCooldown()
    {
        ClearAllEffects();
        currentState  = SystemState.Cooldown;
        cooldownTimer = 0f;

        BrokenMirrorLevelBridge bridge = FindObjectOfType<BrokenMirrorLevelBridge>();
        if (bridge != null) bridge.ResetBridge();

        Debug.Log($"[DistractionManager] Cooldown started ({cooldownDuration}s).");
    }

    public void FullReset()
    {
        currentState  = SystemState.Normal;
        cooldownTimer = 0f;
        ClearAllEffects();

        if (GroundingPrompt.Instance != null)
            GroundingPrompt.Instance.ForceResetPromptState();

        if (BlowPrompt.Instance != null)
            BlowPrompt.Instance.HidePrompt();

        TouchEffectCountPrompt touchCountPrompt = FindObjectOfType<TouchEffectCountPrompt>();
        if (touchCountPrompt != null) touchCountPrompt.ForceResetPromptState();

        EndingPrompt endingPrompt = FindObjectOfType<EndingPrompt>();
        if (endingPrompt != null) endingPrompt.HidePrompt();

        InteractionVFXController vfx = FindObjectOfType<InteractionVFXController>();
        if (vfx != null) vfx.HardReset();

        BrokenMirrorLevelBridge bridge = FindObjectOfType<BrokenMirrorLevelBridge>();
        if (bridge != null) bridge.ResetBridge();

        if (InstructionDisplay.Instance != null)
            InstructionDisplay.Instance.ShowInstruction();

        Debug.Log("[DistractionManager] Full reset triggered (R key).");
    }

    private void ReadHardwareInput()
    {
        DeviceInputManager input = DeviceInputManager.Instance;

        if (input == null)
        {
            // No DeviceInputManager — full keyboard fallback
            if      (Input.GetKeyDown(KeyCode.Alpha0)) hardwareLevel = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha1)) hardwareLevel = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) hardwareLevel = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) hardwareLevel = 3;
            isTouching  = Input.GetKey(KeyCode.Space)  ? 1 : 0;
            isGrounding = Input.GetKey(KeyCode.Return) ? 1 : 0;
            isBlowing   = Input.GetKeyDown(KeyCode.B)  ? 1 : 0;
            return;
        }

        hardwareLevel = Mathf.Clamp(input.Level, 0, 3);
        isTouching  = input.isTouching;
        isGrounding = input.isGrounding;
        isBlowing   = input.isBlowing;
    }

    private void UpdateWaterLevelEffect()
    {
        if (faceMaterial == null) return;

        // Water level effect is locked at minimum until the user first touches the water
        // AND the intro instruction overlay has fully faded out.
        bool instructionShowing = InstructionDisplay.Instance != null &&
                                  InstructionDisplay.Instance.IsVisible;
        float targetEffect = (hasExperienceStarted && !instructionShowing)
            ? Mathf.Lerp(minWaterLevelEffect, maxWaterLevelEffect, hardwareLevel / 3f)
            : minWaterLevelEffect;

        // Once the grounding prompt has triggered, freeze the water level visual at
        // whatever value it currently has — hardware level changes no longer affect it.
        bool groundingFrozen = GroundingPrompt.Instance != null &&
                               GroundingPrompt.Instance.HasTriggered;

        if (currentState == SystemState.Shielded)
        {
            currentWaterLevelEffect = Mathf.Lerp(currentWaterLevelEffect, lockedWaterLevelEffect, Time.deltaTime * effectSmoothSpeed);
        }
        else if (!groundingFrozen)
        {
            currentWaterLevelEffect = Mathf.Lerp(currentWaterLevelEffect, targetEffect, Time.deltaTime * effectSmoothSpeed);
        }
        // groundingFrozen && !Shielded → hold currentWaterLevelEffect as-is

        faceMaterial.SetFloat(waterLevelEffectProperty, currentWaterLevelEffect);
    }

    private void UpdateGlobalDistractionTimer()
    {
        if (!hasExperienceStarted && isTouching == 1)
        {
            hasExperienceStarted = true;
        }

        if (!hasExperienceStarted) return;

        float safeTime = Mathf.Max(0.01f, timeToMaxDistraction);
        globalDistractionTimer = Mathf.Min(globalDistractionTimer + Time.deltaTime, safeTime);
    }

    private void UpdateStateMachine()
    {
        switch (currentState)
        {
            case SystemState.Normal:
                if (isGrounding == 1 && IsGroundingAllowed())
                    currentState = SystemState.Grounding;
                break;

            case SystemState.Grounding:
                if (isGrounding == 1)
                {
                    groundingTimer += Time.deltaTime;
                }
                else
                {
                    groundingTimer = Mathf.MoveTowards(groundingTimer, 0f, backtrackSpeed * Time.deltaTime);
                }

                if (groundingTimer >= groundingDuration) EnterShieldedState();
                if (groundingTimer <= 0f && isGrounding == 0) ResetToNormal();
                break;

            case SystemState.Shielded:
                groundingTimer = groundingDuration;
                if (isBlowing == 1)
                {
                    EnterCooldown();
                }
                break;
        }

        groundingTimer = Mathf.Clamp(groundingTimer, 0f, groundingDuration);
        traceProgress = groundingDuration > 0f ? Mathf.Clamp01(groundingTimer / groundingDuration) : 0f;

        if (currentState == SystemState.Shielded) traceProgress = 1f;
    }

    private void UpdateTouchEffect()
    {
        if (faceMaterial != null)
        {
            faceMaterial.SetFloat(touchActiveProperty, isTouching == 1 ? 1f : 0f);
            // Blow effect only activates when fully shielded — not during grounding or any other phase.
            bool blowValid = isBlowing == 1 && currentState == SystemState.Shielded;
            faceMaterial.SetFloat(blowActiveProperty, blowValid ? 1f : 0f);
        }

        // When the grounding prompt has triggered, freeze the touch effect value.
        // All distortion stays locked at its current level until blow clears it.
        bool groundingFrozen = GroundingPrompt.Instance != null &&
                               GroundingPrompt.Instance.HasTriggered;

        if (isTouching == 1)
        {
            currentTouchEffect = touchEffectStrength;
        }
        else if (!groundingFrozen)
        {
            float safeTime = Mathf.Max(0.01f, timeToMaxDistraction);
            float progress = Mathf.Clamp01(globalDistractionTimer / safeTime);

            currentFadeTime = Mathf.Lerp(fastFadeTimeAtStart, slowFadeTimeAtEnd, fadeTimeCurve.Evaluate(progress));

            float decaySpeed = touchEffectStrength / Mathf.Max(0.1f, currentFadeTime);
            if (hardwareLevel == 3) decaySpeed = 0f;

            currentTouchEffect = Mathf.MoveTowards(currentTouchEffect, 0f, decaySpeed * Time.deltaTime);
        }
        // groundingFrozen && !touching → decay is suppressed; value holds as-is

        currentTouchEffect = Mathf.Clamp(currentTouchEffect, 0f, touchEffectStrength);

        if (faceMaterial != null) faceMaterial.SetFloat(touchEffectProperty, currentTouchEffect);
    }

    /// <summary>
    /// 当 requireGroundingPrompt = true 时，必须等 GroundingPrompt 触发过才返回 true。
    /// GroundingPrompt 不在场景中时视为已解锁（向下兼容）。
    /// </summary>
    private bool IsGroundingAllowed()
    {
        if (!requireGroundingPrompt) return true;
        // 必须等 fade-in 完全结束（IsFullyShown），而不只是触发了提示（HasTriggered）。
        return GroundingPrompt.Instance == null || GroundingPrompt.Instance.IsFullyShown;
    }

    private void EnterShieldedState()
    {
        currentState = SystemState.Shielded;
        groundingTimer = groundingDuration;
        lockedWaterLevelEffect = currentWaterLevelEffect;
        ApplyShieldState(true);
    }

    private void ResetToNormal()
    {
        currentState = SystemState.Normal;
        groundingTimer = 0f;
        traceProgress = 0f;
        lockedWaterLevelEffect = 0f;
        ApplyShieldState(false);
    }

    private void ApplyShieldState(bool isActive)
    {
        if (faceMaterial != null) faceMaterial.SetFloat(shieldProperty, isActive ? 1f : 0f);
    }

    private void ResetTemporaryEffects()
    {
        if (faceMaterial != null)
        {
            faceMaterial.SetFloat(touchActiveProperty, 0f);
            faceMaterial.SetFloat(blowActiveProperty, 0f);
            faceMaterial.SetFloat(touchEffectProperty, 0f);
        }
    }

    public void ClearAllEffects()
    {
        hardwareLevel = 0; isTouching = 0; isGrounding = 0; isBlowing = 0;
        lockedWaterLevelEffect = 0f; currentTouchEffect = 0f;
        globalDistractionTimer = 0f; hasExperienceStarted = false;
        groundingTimer = 0f; traceProgress = 0f;

        ResetTemporaryEffects();
        ApplyShieldState(false);
        currentWaterLevelEffect = minWaterLevelEffect;
        if (faceMaterial != null) faceMaterial.SetFloat(waterLevelEffectProperty, currentWaterLevelEffect);
        PushTraceProgressToShader();
    }

    private void PushTraceProgressToShader()
    {
        if (faceMaterial != null) faceMaterial.SetFloat(traceProgressProperty, traceProgress);
    }
}
