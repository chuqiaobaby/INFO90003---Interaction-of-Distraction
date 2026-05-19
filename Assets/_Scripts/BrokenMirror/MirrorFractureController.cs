using UnityEngine;

[DisallowMultipleComponent]
public sealed class MirrorFractureController : MonoBehaviour
{
    [System.Serializable]
    public struct MirrorVisualState
    {
        [Range(0f, 3f)]   public float mirrorState;
        [Range(0f, 2f)]   public float crackStrength;
        [Range(0f, 0.18f)] public float distortionStrength;
        [Range(0f, 0.08f)] public float rippleStrength;
        [Range(0f, 1f)]   public float blurStrength;
        [Range(0f, 0.05f)] public float chromaticStrength;
        [Range(0f, 1f)]   public float instability;
        [Range(0f, 1f)]   public float darken;
        [Range(0.5f, 2f)] public float contrast;
    }

    [SerializeField] private Renderer mirrorRenderer;
    [SerializeField] private Renderer overlayRenderer;   // CrackOverlay Quad (optional)
    [SerializeField] private float transitionSpeed = 2.8f;
    [SerializeField, Range(0f, 1f)] private float fractureShockIntensity = 0.20f;
    [SerializeField, Range(0.05f, 1.5f)] private float fractureShockDuration = 0.42f;

    [SerializeField] private MirrorVisualState cleanState = new MirrorVisualState
    {
        mirrorState = 0f, crackStrength = 0f, distortionStrength = 0f,
        rippleStrength = 0f, blurStrength = 0f, chromaticStrength = 0f,
        instability = 0f, darken = 0f, contrast = 1f
    };
    [SerializeField] private MirrorVisualState state1 = new MirrorVisualState
    {
        mirrorState = 1.3f, crackStrength = 0.09f, distortionStrength = 0.096f,
        rippleStrength = 0.014f, blurStrength = 0.31f, chromaticStrength = 0.022f,
        instability = 0.27f, darken = 0.31f, contrast = 1.03f
    };
    [SerializeField] private MirrorVisualState state2 = new MirrorVisualState
    {
        mirrorState = 2f, crackStrength = 0f, distortionStrength = 0.08f,
        rippleStrength = 0f, blurStrength = 0.58f, chromaticStrength = 0.0025f,
        instability = 0.06f, darken = 0.58f, contrast = 1.03f
    };
    [SerializeField] private MirrorVisualState state3 = new MirrorVisualState
    {
        mirrorState = 3f, crackStrength = 0.18f, distortionStrength = 0.056f,
        rippleStrength = 0.011f, blurStrength = 0.27f, chromaticStrength = 0.018f,
        instability = 0.49f, darken = 0.76f, contrast = 1.1f
    };

    private static readonly int MirrorStateId        = Shader.PropertyToID("_MirrorState");
    private static readonly int CrackStrengthId      = Shader.PropertyToID("_CrackStrength");
    private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
    private static readonly int RippleStrengthId     = Shader.PropertyToID("_RippleStrength");
    private static readonly int BlurStrengthId       = Shader.PropertyToID("_BlurStrength");
    private static readonly int ChromaticStrengthId  = Shader.PropertyToID("_ChromaticStrength");
    private static readonly int InstabilityId        = Shader.PropertyToID("_Instability");
    private static readonly int DarkenId             = Shader.PropertyToID("_Darken");
    private static readonly int ContrastId           = Shader.PropertyToID("_Contrast");
    private static readonly int CrackTexId           = Shader.PropertyToID("_CrackTex");
    private static readonly int CrackTexLayer1Id     = Shader.PropertyToID("_CrackTexLayer1");
    private static readonly int CrackTexLayer2Id     = Shader.PropertyToID("_CrackTexLayer2");
    private static readonly int FractureShockId      = Shader.PropertyToID("_FractureShock");

    // Global properties consumed by CrackScreenDistortionFeature
    private static readonly int GlobalMirrorStateId   = Shader.PropertyToID("_GlobalMirrorState");
    private static readonly int GlobalCrackStrengthId = Shader.PropertyToID("_GlobalCrackStrength");
    private static readonly int GlobalDistortionId    = Shader.PropertyToID("_GlobalDistortionStrength");
    private static readonly int GlobalCrackTexId      = Shader.PropertyToID("_GlobalCrackTex");
    private static readonly int GlobalCrackTexLayer1Id = Shader.PropertyToID("_GlobalCrackTexLayer1");
    private static readonly int GlobalCrackTexLayer2Id = Shader.PropertyToID("_GlobalCrackTexLayer2");
    private static readonly int GlobalFractureShockId = Shader.PropertyToID("_GlobalFractureShock");

    private Material runtimeMaterial;
    private Material overlayMaterial;
    private MirrorVisualState currentState;
    private MirrorVisualState targetState;
    private float fractureShock;
    private float fractureShockTarget;

    public int ActiveState { get; private set; }

    private void Awake()
    {
        if (mirrorRenderer == null)
            mirrorRenderer = GetComponent<Renderer>();

        runtimeMaterial = mirrorRenderer != null ? mirrorRenderer.material : null;
        overlayMaterial = overlayRenderer != null ? overlayRenderer.material : null;

        // Publish crack texture globally so CrackScreenDistortionFeature can sample it
        Texture crackTex = runtimeMaterial != null ? runtimeMaterial.GetTexture(CrackTexId) : null;
        if (crackTex != null) Shader.SetGlobalTexture(GlobalCrackTexId, crackTex);
        Texture layer1Tex = runtimeMaterial != null ? runtimeMaterial.GetTexture(CrackTexLayer1Id) : null;
        if (layer1Tex != null) Shader.SetGlobalTexture(GlobalCrackTexLayer1Id, layer1Tex);
        Texture layer2Tex = runtimeMaterial != null ? runtimeMaterial.GetTexture(CrackTexLayer2Id) : null;
        if (layer2Tex != null) Shader.SetGlobalTexture(GlobalCrackTexLayer2Id, layer2Tex);
        currentState    = cleanState;
        targetState     = cleanState;
        ApplyState(currentState);
    }

    private void Update()
    {
        float step   = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
        currentState = LerpState(currentState, targetState, step);
        fractureShockTarget = Mathf.MoveTowards(fractureShockTarget, 0f, Time.deltaTime / Mathf.Max(fractureShockDuration, 0.01f));
        fractureShock = Mathf.Lerp(fractureShock, fractureShockTarget, 1f - Mathf.Exp(-2f * Time.deltaTime));
        ApplyState(currentState);
    }

    private void OnValidate()
    {
        // Re-push the current state struct to targetState so Inspector tweaks
        // take effect immediately during Play mode without re-pressing the key.
        if (!Application.isPlaying) return;
        targetState = ActiveState switch
        {
            1 => state1,
            2 => state2,
            3 => state3,
            _ => cleanState
        };
    }

    public void SetFractureState(int stateIndex)
    {
        int previousState = ActiveState;
        ActiveState = Mathf.Clamp(stateIndex, 0, 3);
        targetState = ActiveState switch
        {
            1 => state1,
            2 => state2,
            3 => state3,
            _ => cleanState
        };

        if (ActiveState > previousState)
        {
            float stateBoost = Mathf.InverseLerp(1f, 3f, ActiveState);
            fractureShockTarget = Mathf.Max(fractureShockTarget, fractureShockIntensity * Mathf.Lerp(0.45f, 1f, stateBoost));
        }
    }

    public void ResetMirror()
    {
        ActiveState         = 0;
        targetState         = cleanState;
        fractureShock       = 0f;
        fractureShockTarget = 0f;
    }

    private static MirrorVisualState LerpState(MirrorVisualState from, MirrorVisualState to, float t)
    {
        return new MirrorVisualState
        {
            mirrorState        = Mathf.Lerp(from.mirrorState,        to.mirrorState,        t),
            crackStrength      = Mathf.Lerp(from.crackStrength,      to.crackStrength,      t),
            distortionStrength = Mathf.Lerp(from.distortionStrength, to.distortionStrength, t),
            rippleStrength     = Mathf.Lerp(from.rippleStrength,     to.rippleStrength,     t),
            blurStrength       = Mathf.Lerp(from.blurStrength,       to.blurStrength,       t),
            chromaticStrength  = Mathf.Lerp(from.chromaticStrength,  to.chromaticStrength,  t),
            instability        = Mathf.Lerp(from.instability,        to.instability,        t),
            darken             = Mathf.Lerp(from.darken,             to.darken,             t),
            contrast           = Mathf.Lerp(from.contrast,           to.contrast,           t)
        };
    }

    private void ApplyState(MirrorVisualState state)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(MirrorStateId,        state.mirrorState);
            runtimeMaterial.SetFloat(CrackStrengthId,      state.crackStrength);
            runtimeMaterial.SetFloat(DistortionStrengthId, state.distortionStrength);
            runtimeMaterial.SetFloat(RippleStrengthId,     state.rippleStrength);
            runtimeMaterial.SetFloat(BlurStrengthId,       state.blurStrength);
            runtimeMaterial.SetFloat(ChromaticStrengthId,  state.chromaticStrength);
            runtimeMaterial.SetFloat(InstabilityId,        state.instability);
            runtimeMaterial.SetFloat(DarkenId,             state.darken);
            runtimeMaterial.SetFloat(ContrastId,           state.contrast);
            runtimeMaterial.SetFloat(FractureShockId,      fractureShock);
        }

        if (overlayMaterial != null)
        {
            overlayMaterial.SetFloat(MirrorStateId,   state.mirrorState);
            overlayMaterial.SetFloat(CrackStrengthId, state.crackStrength);
            overlayMaterial.SetFloat(InstabilityId,   state.instability);
            overlayMaterial.SetFloat(DarkenId,        state.darken);
            overlayMaterial.SetFloat(FractureShockId, fractureShock);
        }

        // Push to global scope for the CrackScreenDistortionFeature blit pass
        Shader.SetGlobalFloat(GlobalMirrorStateId,   state.mirrorState);
        Shader.SetGlobalFloat(GlobalCrackStrengthId, state.crackStrength);
        Shader.SetGlobalFloat(GlobalDistortionId,    state.distortionStrength);
        Shader.SetGlobalFloat(GlobalFractureShockId, fractureShock);
    }
}
