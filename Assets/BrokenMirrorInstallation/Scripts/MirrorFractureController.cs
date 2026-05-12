using UnityEngine;

namespace BrokenMirrorInstallation
{
    [DisallowMultipleComponent]
    public sealed class MirrorFractureController : MonoBehaviour
    {
        [System.Serializable]
        public struct MirrorVisualState
        {
            [Range(0f, 3f)] public float mirrorState;
            [Range(0f, 2f)] public float crackStrength;
            [Range(0f, 0.18f)] public float distortionStrength;
            [Range(0f, 0.08f)] public float rippleStrength;
            [Range(0f, 1f)] public float blurStrength;
            [Range(0f, 0.05f)] public float chromaticStrength;
            [Range(0f, 1f)] public float instability;
            [Range(0f, 1f)] public float darken;
            [Range(0.5f, 2f)] public float contrast;
        }

        [SerializeField] private Renderer mirrorRenderer;
        [SerializeField] private float transitionSpeed = 2.8f;
        [SerializeField] private MirrorVisualState cleanState = new MirrorVisualState
        {
            mirrorState = 0f,
            crackStrength = 0f,
            distortionStrength = 0f,
            rippleStrength = 0f,
            blurStrength = 0f,
            chromaticStrength = 0f,
            instability = 0f,
            darken = 0f,
            contrast = 1f
        };
        [SerializeField] private MirrorVisualState state1 = new MirrorVisualState
        {
            mirrorState = 1f,
            crackStrength = 0.42f,
            distortionStrength = 0.006f,
            rippleStrength = 0.001f,
            blurStrength = 0.025f,
            chromaticStrength = 0f,
            instability = 0f,
            darken = 0.14f,
            contrast = 1.02f
        };
        [SerializeField] private MirrorVisualState state2 = new MirrorVisualState
        {
            mirrorState = 2f,
            crackStrength = 0.72f,
            distortionStrength = 0.018f,
            rippleStrength = 0.002f,
            blurStrength = 0.11f,
            chromaticStrength = 0.0025f,
            instability = 0.06f,
            darken = 0.36f,
            contrast = 1.04f
        };
        [SerializeField] private MirrorVisualState state3 = new MirrorVisualState
        {
            mirrorState = 3f,
            crackStrength = 0.78f,
            distortionStrength = 0.038f,
            rippleStrength = 0.004f,
            blurStrength = 0.22f,
            chromaticStrength = 0.009f,
            instability = 0.22f,
            darken = 0.68f,
            contrast = 1.12f
        };

        private static readonly int MirrorStateId = Shader.PropertyToID("_MirrorState");
        private static readonly int CrackStrengthId = Shader.PropertyToID("_CrackStrength");
        private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
        private static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
        private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");
        private static readonly int ChromaticStrengthId = Shader.PropertyToID("_ChromaticStrength");
        private static readonly int InstabilityId = Shader.PropertyToID("_Instability");
        private static readonly int DarkenId = Shader.PropertyToID("_Darken");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");

        private Material runtimeMaterial;
        private MirrorVisualState currentState;
        private MirrorVisualState targetState;

        public int ActiveDebugState { get; private set; }

        private void Awake()
        {
            if (mirrorRenderer == null)
            {
                mirrorRenderer = GetComponent<Renderer>();
            }

            runtimeMaterial = mirrorRenderer != null ? mirrorRenderer.material : null;
            currentState = cleanState;
            targetState = cleanState;
            ApplyState(currentState);
        }

        private void Update()
        {
            float step = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
            currentState = LerpState(currentState, targetState, step);
            ApplyState(currentState);
        }

        public void SetFractureState(int stateIndex)
        {
            ActiveDebugState = Mathf.Clamp(stateIndex, 0, 3);
            targetState = ActiveDebugState switch
            {
                1 => state1,
                2 => state2,
                3 => state3,
                _ => cleanState
            };
        }

        public void SetWaterIntensity(float intensity01)
        {
            float intensity = Mathf.Clamp01(intensity01);
            if (intensity < 0.18f)
            {
                SetFractureState(0);
            }
            else if (intensity < 0.48f)
            {
                SetFractureState(1);
            }
            else if (intensity < 0.76f)
            {
                SetFractureState(2);
            }
            else
            {
                SetFractureState(3);
            }
        }

        public void ResetMirror()
        {
            ActiveDebugState = 0;
            targetState = cleanState;
            currentState = cleanState;
            ApplyState(currentState);
        }

        private static MirrorVisualState LerpState(MirrorVisualState from, MirrorVisualState to, float t)
        {
            return new MirrorVisualState
            {
                mirrorState = Mathf.Lerp(from.mirrorState, to.mirrorState, t),
                crackStrength = Mathf.Lerp(from.crackStrength, to.crackStrength, t),
                distortionStrength = Mathf.Lerp(from.distortionStrength, to.distortionStrength, t),
                rippleStrength = Mathf.Lerp(from.rippleStrength, to.rippleStrength, t),
                blurStrength = Mathf.Lerp(from.blurStrength, to.blurStrength, t),
                chromaticStrength = Mathf.Lerp(from.chromaticStrength, to.chromaticStrength, t),
                instability = Mathf.Lerp(from.instability, to.instability, t),
                darken = Mathf.Lerp(from.darken, to.darken, t),
                contrast = Mathf.Lerp(from.contrast, to.contrast, t)
            };
        }

        private void ApplyState(MirrorVisualState state)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            runtimeMaterial.SetFloat(MirrorStateId, state.mirrorState);
            runtimeMaterial.SetFloat(CrackStrengthId, state.crackStrength);
            runtimeMaterial.SetFloat(DistortionStrengthId, state.distortionStrength);
            runtimeMaterial.SetFloat(RippleStrengthId, state.rippleStrength);
            runtimeMaterial.SetFloat(BlurStrengthId, state.blurStrength);
            runtimeMaterial.SetFloat(ChromaticStrengthId, state.chromaticStrength);
            runtimeMaterial.SetFloat(InstabilityId, state.instability);
            runtimeMaterial.SetFloat(DarkenId, state.darken);
            runtimeMaterial.SetFloat(ContrastId, state.contrast);
        }
    }
}
