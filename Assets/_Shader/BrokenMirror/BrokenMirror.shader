Shader "BrokenMirror/URP/Broken Mirror Webcam"
{
    Properties
    {
        _WebcamTex ("Webcam Texture", 2D) = "black" {}
        _CrackTex ("Crack Mask", 2D) = "black" {}
        _Tint ("Reflection Tint", Color) = (1, 1, 1, 1)
        _CrackColor ("Crack Color", Color) = (0.72, 0.9, 1.0, 1)
        _MirrorState ("Mirror State", Range(0, 3)) = 0
        _CrackStrength ("Crack Strength", Range(0, 2)) = 0
        _DistortionStrength ("UV Distortion", Range(0, 0.18)) = 0
        _RippleStrength ("Ripple Strength", Range(0, 0.08)) = 0
        _BlurStrength ("Crack Blur", Range(0, 1)) = 0
        _ChromaticStrength ("Chromatic Aberration", Range(0, 0.05)) = 0
        _Instability ("Reflection Instability", Range(0, 1)) = 0
        _Darken ("Mirror Darken", Range(0, 1)) = 0
        _Contrast ("Reflection Contrast", Range(0.5, 2)) = 1
        _FlipX ("Flip Webcam X", Float) = 0
        _FlipY ("Flip Webcam Y", Float) = 0
        _AspectRatioCorrection ("Aspect Ratio UV Correction (xy)", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "MirrorUnlit"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _WebcamTex;
            sampler2D _CrackTex;
            float4 _WebcamTex_ST;
            float4 _CrackTex_ST;
            half4 _Tint;
            half4 _CrackColor;
            half _MirrorState;
            half _CrackStrength;
            half _DistortionStrength;
            half _RippleStrength;
            half _BlurStrength;
            half _ChromaticStrength;
            half _Instability;
            half _Darken;
            half _Contrast;
            half _FlipX;
            half _FlipY;
            float4 _AspectRatioCorrection;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float CrackMask(float2 uv)
            {
                float primary = tex2D(_CrackTex, TRANSFORM_TEX(uv, _CrackTex)).r;
                float secondary = tex2D(_CrackTex, TRANSFORM_TEX(uv * 1.73 + float2(0.17, 0.31), _CrackTex)).r;
                float tertiary = tex2D(_CrackTex, TRANSFORM_TEX(uv * 2.61 + float2(0.61, 0.09), _CrackTex)).r;
                return saturate(primary + secondary * saturate(_MirrorState - 0.7) * 0.55 + tertiary * saturate(_MirrorState - 1.8) * 0.45);
            }

            float2 CrackGradient(float2 uv)
            {
                float e = 0.003;
                float left = CrackMask(uv - float2(e, 0));
                float right = CrackMask(uv + float2(e, 0));
                float down = CrackMask(uv - float2(0, e));
                float up = CrackMask(uv + float2(0, e));
                return float2(right - left, up - down);
            }

            half3 WebcamSample(float2 uv)
            {
                // Center-crop: keeps webcam at its native aspect, no stretching
                uv = (uv - 0.5) * _AspectRatioCorrection.xy + 0.5;
                uv = saturate(uv);
                if (_FlipX > 0.5) uv.x = 1.0 - uv.x;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                return tex2D(_WebcamTex, uv).rgb;
            }

            half3 BlurSample(float2 uv, float radius)
            {
                half3 c = WebcamSample(uv) * 0.26;
                c += WebcamSample(uv + float2(radius, 0)) * 0.12;
                c += WebcamSample(uv - float2(radius, 0)) * 0.12;
                c += WebcamSample(uv + float2(0, radius)) * 0.12;
                c += WebcamSample(uv - float2(0, radius)) * 0.12;
                c += WebcamSample(uv + float2(radius, radius)) * 0.065;
                c += WebcamSample(uv + float2(-radius, radius)) * 0.065;
                c += WebcamSample(uv + float2(radius, -radius)) * 0.065;
                c += WebcamSample(uv + float2(-radius, -radius)) * 0.065;
                return c;
            }

            float TriCell(float2 uv)
            {
                float2 center = float2(0.58, 0.48);
                float2 p = uv - center;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float sector = floor((angle + 3.14159265) / 6.2831853 * 28.0);
                float ring = floor(radius * 11.0);
                float n = frac(sin(dot(float2(sector, ring), float2(12.9898, 78.233))) * 43758.5453);
                return n * 2.0 - 1.0;
            }

            float2 ShardOffset(float2 uv, float state2, float state3)
            {
                float2 center = float2(0.58, 0.48);
                float2 p = uv - center;
                float radius = max(length(p), 0.001);
                float2 radial = p / radius;
                float2 tangent = float2(-radial.y, radial.x);
                float n = TriCell(uv);
                float nearImpact = smoothstep(0.62, 0.05, radius);
                float amount = (state2 * 0.006 + state3 * 0.026) * (0.35 + nearImpact * 0.9);
                return (radial * n + tangent * (frac(n * 17.13) - 0.5)) * amount;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = _Time.y;

                if (_MirrorState < 0.01 && _CrackStrength < 0.01)
                {
                    return half4(WebcamSample(uv), 1);
                }

                float state01 = saturate(_MirrorState / 3.0);
                float state1 = saturate(_MirrorState);
                float state2 = saturate(_MirrorState - 1.0);
                float state3 = saturate(_MirrorState - 2.0);

                float crack = CrackMask(uv);
                float hairline = smoothstep(0.42, 0.86, crack) * state1;
                float crackEdge = smoothstep(0.10, 0.74, crack) * saturate(0.28 + state2 * 0.58 + state3 * 0.7);
                float glassEdge = smoothstep(0.16, 0.58, crack) * smoothstep(1.0, 0.10, crack) * saturate(state2 + state3);

                float rippleA = sin((uv.y * 42.0) + t * 1.35);
                float rippleB = sin((uv.x * 58.0) - t * 0.92);
                float rippleC = sin((uv.x + uv.y) * 31.0 + t * 0.57);
                float2 ripple = float2(rippleA + rippleC, rippleB - rippleC) * _RippleStrength * (0.08 + state01 * 0.35);

                float2 crackNormal = CrackGradient(uv);
                float instabilityPulse = sin(t * 6.2 + uv.y * 17.0) * sin(t * 2.3 + uv.x * 11.0);
                float2 unstable = float2(instabilityPulse, sin(t * 4.1 + uv.x * 9.5)) * _Instability * 0.004;
                float2 shardSlip = ShardOffset(uv, state2, state3);
                shardSlip += crackNormal * (_DistortionStrength * (hairline * 0.15 + crackEdge * 0.9 + glassEdge * 0.55));

                float2 distortedUv = uv + ripple + shardSlip + unstable * state01;
                float blurRadius = lerp(0.0015, 0.010, _BlurStrength) * saturate(crackEdge + state01 * 0.25);

                half3 baseReflection = WebcamSample(distortedUv);
                half3 blurredReflection = BlurSample(distortedUv, blurRadius);
                half blurMix = saturate(_BlurStrength * (crackEdge * 0.85 + state01 * 0.18));
                half3 reflection = lerp(baseReflection, blurredReflection, blurMix);

                float2 chromaOffset = (crackNormal * 0.55 + shardSlip * 10.0 + unstable * 4.0) * _ChromaticStrength * (0.18 + crackEdge);
                if (_ChromaticStrength > 0.0001)
                {
                    reflection.r = WebcamSample(distortedUv + chromaOffset).r;
                    reflection.b = WebcamSample(distortedUv - chromaOffset).b;
                }

                reflection = (reflection - 0.5) * _Contrast + 0.5;
                reflection *= _Tint.rgb;

                float edgeDistance = distance(uv, 0.5);
                float vignette = smoothstep(0.68 - state01 * 0.16, 0.22, edgeDistance);
                float cinematicFalloff = lerp(1.0 - _Darken, 1.0, vignette);
                reflection *= cinematicFalloff;

                half3 edgeTint = _CrackColor.rgb * (hairline * 0.075 + glassEdge * 0.095 + crackEdge * 0.055) * _CrackStrength;
                half3 crackShadow = reflection * (1.0 - saturate((hairline * 0.115 + crackEdge * 0.145 + glassEdge * 0.07) * _CrackStrength));
                half3 color = crackShadow + edgeTint;
                color += pow(saturate(glassEdge), 2.0) * _CrackStrength * 0.032;

                float scan = sin((uv.y + t * 0.035) * 420.0) * 0.5 + 0.5;
                color += scan * _Instability * 0.012 * state3;
                color = saturate(color);

                return half4(color, 1);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Texture"
}
