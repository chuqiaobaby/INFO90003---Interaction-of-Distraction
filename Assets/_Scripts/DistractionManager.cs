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
        Shielded
    }

    public static DistractionManager Instance { get; private set; }

    // Read-only accessor used by PlaceholderVFXController to sync flash fade speed.
    public float CurrentFadeTime => currentFadeTime;

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
        ReadHardwareInput();
        UpdateGlobalDistractionTimer();
        UpdateWaterLevelEffect();
        UpdateStateMachine();
        UpdateTouchEffect();
        PushTraceProgressToShader();
    }

    private void ReadHardwareInput()
    {
        HardwareSimulator simulator = HardwareSimulator.Instance;

        if (simulator == null)
        {
            hardwareLevel = 0; isTouching = 0; isGrounding = 0; isBlowing = 0;
            return;
        }

        hardwareLevel = Mathf.Clamp(simulator.Level, 0, 3);
        isTouching = simulator.isTouching;
        isGrounding = simulator.isGrounding;
        isBlowing = simulator.isBlowing;
    }

    private void UpdateWaterLevelEffect()
    {
        if (faceMaterial == null) return;

        float targetEffect = Mathf.Lerp(minWaterLevelEffect, maxWaterLevelEffect, hardwareLevel / 3f);

        if (currentState == SystemState.Shielded)
        {
            currentWaterLevelEffect = Mathf.Lerp(currentWaterLevelEffect, lockedWaterLevelEffect, Time.deltaTime * effectSmoothSpeed);
        }
        else
        {
            currentWaterLevelEffect = Mathf.Lerp(currentWaterLevelEffect, targetEffect, Time.deltaTime * effectSmoothSpeed);
        }

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
                if (isGrounding == 1) currentState = SystemState.Grounding;
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
                    ClearAllEffects();
                    ResetToNormal();
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
            faceMaterial.SetFloat(blowActiveProperty, isBlowing == 1 ? 1f : 0f);
        }

        if (isTouching == 1)
        {
            currentTouchEffect = touchEffectStrength;
        }
        else
        {
            float safeTime = Mathf.Max(0.01f, timeToMaxDistraction);
            float progress = Mathf.Clamp01(globalDistractionTimer / safeTime);
            
            currentFadeTime = Mathf.Lerp(fastFadeTimeAtStart, slowFadeTimeAtEnd, fadeTimeCurve.Evaluate(progress));

            float decaySpeed = touchEffectStrength / Mathf.Max(0.1f, currentFadeTime);
            if (hardwareLevel == 3) decaySpeed = 0f;

            currentTouchEffect = Mathf.MoveTowards(currentTouchEffect, 0f, decaySpeed * Time.deltaTime);
        }
        
        currentTouchEffect = Mathf.Clamp(currentTouchEffect, 0f, touchEffectStrength);

        if (faceMaterial != null) faceMaterial.SetFloat(touchEffectProperty, currentTouchEffect);
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
