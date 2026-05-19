Shader "BrokenMirror/URP/Crack Overlay"
{
    // Transparent overlay that draws ONLY the crack visual effects (shadow + edge highlights).
    // Place this on a full-screen quad in front of the BrokenMirror background quad.
    // Renders after LiquidGlass (Transparent+100) so cracks always appear on top.

    Properties
    {
        _CrackTex        ("Crack Mask",           2D)            = "black" {}
        _CrackTexLayer1  ("Layer 1 Crack Mask",   2D)            = "black" {}
        _CrackTexLayer2  ("Layer 2 Crack Mask",   2D)            = "black" {}
        _CrackColor      ("Crack Edge Color",      Color)         = (0.72, 0.9, 1.0, 1)
        _MirrorState     ("Mirror State",          Range(0, 3))   = 0
        _CrackStrength   ("Crack Strength",        Range(0, 2))   = 0
        _Instability     ("Instability",           Range(0, 1))   = 0
        _FractureShock   ("Fracture Shock",        Range(0, 1))   = 0
        _Darken          ("Darken",                Range(0, 1))   = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+100"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest  Always
        Cull Off

        Pass
        {
            Name "CrackOverlay"

            CGPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.0
            #include "UnityCG.cginc"

            sampler2D _CrackTex;
            sampler2D _CrackTexLayer1;
            sampler2D _CrackTexLayer2;
            float4    _CrackTex_ST;
            float4    _CrackTexLayer1_ST;
            float4    _CrackTexLayer2_ST;
            half4     _CrackColor;
            half      _MirrorState;
            half      _CrackStrength;
            half      _Instability;
            half      _FractureShock;
            half      _Darken;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv     = i.uv;
                return o;
            }

            float CrackMask(float2 uv)
            {
                float layer1   = tex2D(_CrackTexLayer1, TRANSFORM_TEX(uv, _CrackTexLayer1)).r;
                float layer2   = tex2D(_CrackTexLayer2, TRANSFORM_TEX(uv, _CrackTexLayer2)).r;
                float primary   = tex2D(_CrackTex, TRANSFORM_TEX(uv,                                          _CrackTex)).r;
                float secondary = tex2D(_CrackTex, TRANSFORM_TEX(uv * 1.73 + float2(0.17, 0.31),             _CrackTex)).r;
                float tertiary  = tex2D(_CrackTex, TRANSFORM_TEX(uv * 2.61 + float2(0.61, 0.09),             _CrackTex)).r;
                float cinematic = saturate(primary + secondary * 0.55 + tertiary * 0.45);
                float stage12   = saturate(layer1 * saturate(_MirrorState) + layer2 * saturate(_MirrorState - 1.0));
                return lerp(stage12, cinematic, saturate(_MirrorState - 2.0));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // Fully transparent when mirror is clean
                if (_MirrorState < 0.01 && _CrackStrength < 0.01)
                    return half4(0, 0, 0, 0);

                float2 uv = i.uv;
                float  t  = _Time.y;

                float state01 = saturate(_MirrorState / 3.0);
                float state1  = saturate(_MirrorState);
                float state2  = saturate(_MirrorState - 1.0);
                float state3  = saturate(_MirrorState - 2.0);

                float crack    = CrackMask(uv);
                float hairline  = smoothstep(0.42, 0.86, crack) * state1;
                float crackEdge = smoothstep(0.10, 0.74, crack) * saturate(0.28 + state2 * 0.58 + state3 * 0.7);
                float glassEdge = smoothstep(0.16, 0.58, crack) * smoothstep(1.0, 0.10, crack) * saturate(state2 + state3);
                float impactRing = 1.0 - smoothstep(0.02, 0.33, abs(distance(uv, float2(0.58, 0.48)) - (0.18 + _FractureShock * 0.42)));
                impactRing *= _FractureShock * saturate(state01 * 2.0);

                // Shadow darkening (dark overlay on top of LiquidGlass)
                float shadowAlpha = saturate((hairline * 0.42 + crackEdge * 0.52 + glassEdge * 0.28 + impactRing * 0.24) * _CrackStrength);

                // Edge highlight tint
                float edgeAlpha = saturate((hairline * 0.30 + glassEdge * 0.36 + crackEdge * 0.22 + impactRing * 0.42) * _CrackStrength);

                // Vignette darkening at mirror edges
                float edgeDist   = distance(uv, 0.5);
                float vignette   = smoothstep(0.68 - state01 * 0.16, 0.22, edgeDist);
                float darkenAlpha = (1.0 - lerp(1.0 - _Darken, 1.0, vignette)) * saturate(state01 * 2.0);

                // Scan-line flicker at max instability
                float scan      = sin((uv.y + t * 0.035) * 420.0) * 0.5 + 0.5;
                float scanAlpha = scan * _Instability * 0.012 * state3;

                float totalAlpha = saturate(shadowAlpha + darkenAlpha + scanAlpha);

                // Color: shadow (black) blended toward crack edge highlight
                float edgeWeight = edgeAlpha / max(totalAlpha, 0.001);
                half3 color = lerp(half3(0, 0, 0), _CrackColor.rgb, edgeWeight);
                color += _CrackColor.rgb * edgeAlpha * (0.18 + _FractureShock * 0.35); // edge glow boost for visibility over LiquidGlass
                color += half3(1.0, 0.92, 0.78) * hairline * _FractureShock * 0.18;

                return half4(saturate(color), totalAlpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
