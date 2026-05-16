Shader "Custom/StarGlow"
{
    // Renders a single glowing star (4-pointed) or soft dot.
    // _StarType 0 = dot, 1 = 4-pointed star.
    // Uses additive blending so stars naturally stack and glow over any background.

    Properties
    {
        _Color      ("Glow Color",              Color)            = (1, 0.95, 0.85, 1)
        _Alpha      ("Alpha (fade control)",    Range(0, 1))      = 1.0
        _StarType   ("Shape  0=dot  1=4-star",  Range(0, 1))      = 1.0
        _Sharpness  ("Tip Sharpness",           Range(1, 12))     = 5.0
        _Softness   ("Edge Softness",           Range(0.01, 0.4)) = 0.12
        _HaloSize   ("Outer Glow Radius",       Range(0, 1))      = 0.40
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+50"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend  SrcAlpha One     // additive — stars stack and bloom together
        ZWrite Off
        ZTest  Always
        Cull   Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Alpha;
                float  _StarType;
                float  _Sharpness;
                float  _Softness;
                float  _HaloSize;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.posCS = TransformObjectToHClip(i.posOS.xyz);
                o.uv    = i.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;   // -1..1 centred
                float  r = length(p);
                float  a = atan2(p.y, p.x);

                // ── 4-pointed star (polar SDF) ────────────────────────────────
                // pow(|cos(2θ)|, sharpness): at sharpness=2 gives original cos²
                // shape; higher values concentrate mass at the 4 tips → sharper
                float cos2a  = cos(2.0 * a);
                float tipW   = pow(abs(cos2a), _Sharpness);
                float starR  = lerp(0.18, 0.92, tipW);
                float starSDF = r - starR;

                // ── Soft dot ─────────────────────────────────────────────────
                float dotSDF = r - 0.72;

                // ── Blend by _StarType ────────────────────────────────────────
                float d = lerp(dotSDF, starSDF, _StarType);

                // Sharp filled region
                float fill = 1.0 - smoothstep(-_Softness, _Softness, d);

                // Soft additive halo beyond the edge
                float halo = (1.0 - smoothstep(0.0, _HaloSize, max(d, 0.0))) * 0.55;

                float brightness = fill + halo;
                return half4(_Color.rgb * brightness, saturate(brightness) * _Alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
