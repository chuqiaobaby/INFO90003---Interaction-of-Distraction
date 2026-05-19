Shader "Hidden/CrackScreenDistortion"
{
    // Full-screen blit shader for URP 17 / Unity 6.
    // Source texture is bound as _BlitTexture by Blitter.BlitTexture().
    // Crack parameters are pushed as global properties by MirrorFractureController.

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CrackDistort"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // _BlitTexture + sampler_LinearClamp are declared inside Blit.hlsl

            TEXTURE2D(_GlobalCrackTex);
            SAMPLER(sampler_GlobalCrackTex);
            TEXTURE2D(_GlobalCrackTexLayer1);
            SAMPLER(sampler_GlobalCrackTexLayer1);
            TEXTURE2D(_GlobalCrackTexLayer2);
            SAMPLER(sampler_GlobalCrackTexLayer2);
            float _GlobalMirrorState;
            float _GlobalCrackStrength;
            float _GlobalDistortionStrength;
            float _GlobalFractureShock;

            float CrackMask(float2 uv)
            {
                float l1 = SAMPLE_TEXTURE2D(_GlobalCrackTexLayer1, sampler_GlobalCrackTexLayer1, uv).r;
                float l2 = SAMPLE_TEXTURE2D(_GlobalCrackTexLayer2, sampler_GlobalCrackTexLayer2, uv).r;
                float p = SAMPLE_TEXTURE2D(_GlobalCrackTex, sampler_GlobalCrackTex, uv).r;
                float s = SAMPLE_TEXTURE2D(_GlobalCrackTex, sampler_GlobalCrackTex,
                              uv * 1.73 + float2(0.17, 0.31)).r;
                float t = SAMPLE_TEXTURE2D(_GlobalCrackTex, sampler_GlobalCrackTex,
                              uv * 2.61 + float2(0.61, 0.09)).r;
                float cinematic = saturate(p + s * 0.55 + t * 0.45);
                float stage12 = saturate(l1 * saturate(_GlobalMirrorState) + l2 * saturate(_GlobalMirrorState - 1.0));
                return lerp(stage12, cinematic, saturate(_GlobalMirrorState - 2.0));
            }

            float2 CrackGradient(float2 uv)
            {
                const float e = 0.003;
                return float2(
                    CrackMask(uv + float2(e, 0)) - CrackMask(uv - float2(e, 0)),
                    CrackMask(uv + float2(0, e)) - CrackMask(uv - float2(0, e)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float state2  = saturate(_GlobalMirrorState - 1.0);
                float state3  = saturate(_GlobalMirrorState - 2.0);
                float distAmt = (state2 * 0.55 + state3 * 1.3)
                              * _GlobalDistortionStrength
                              * _GlobalCrackStrength;
                distAmt += _GlobalFractureShock * _GlobalCrackStrength * saturate(_GlobalMirrorState / 3.0) * 0.012;

                if (distAmt < 0.0005)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 distortedUV = saturate(uv + CrackGradient(uv) * distAmt);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUV);
            }
            ENDHLSL
        }
    }
}
