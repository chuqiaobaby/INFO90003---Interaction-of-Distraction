Shader "Hidden/Display2FinalOutputSoftMask"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Display2FinalOutputSoftMask"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Display2FinalSoftMaskWidth;
            float _Display2FinalSoftMaskStrength;
            float _Display2FinalSoftMaskSoftness;
            float _Display2FinalSoftMaskCornerBoost;
            float _Display2FinalSoftMaskVignetteStrength;

            float EdgeKeep(float2 uv)
            {
                float2 edgeDistance = min(uv, 1.0 - uv);
                float nearestEdge = min(edgeDistance.x, edgeDistance.y);
                float sideKeep = smoothstep(0.0, max(_Display2FinalSoftMaskWidth, 0.0001), nearestEdge);

                float2 cornerDistance = edgeDistance / max(_Display2FinalSoftMaskWidth, 0.0001);
                float cornerKeep = smoothstep(0.0, 1.35, length(cornerDistance));
                float keep = min(sideKeep, lerp(1.0, cornerKeep, _Display2FinalSoftMaskCornerBoost));
                return pow(saturate(keep), max(_Display2FinalSoftMaskSoftness, 0.0001));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float keep = lerp(1.0, EdgeKeep(uv), saturate(_Display2FinalSoftMaskStrength));
                float darken = saturate((1.0 - keep) * _Display2FinalSoftMaskVignetteStrength);
                color.rgb = lerp(color.rgb, half3(0.0, 0.0, 0.0), darken);
                color.rgb *= keep;
                return color;
            }
            ENDHLSL
        }
    }
}
