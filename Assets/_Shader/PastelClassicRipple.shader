Shader "INFO90003/Pastel Classic Ripple HLSL"
{
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
        _StrokeOpacity ("Stroke Opacity", Range(0, 2)) = 0.96
        _Duration ("Duration", Float) = 2.2
        _MaxRadius ("Max Radius", Float) = 0.86
        _TriggerTime ("Trigger Time", Float) = -1000
        _RippleCenter ("Ripple Center", Vector) = (0.5, 0.5, 0, 0)
        _Seed ("Seed", Float) = 90003
        _GrainStrength ("Grain Strength", Range(0, 1)) = 0.045
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BackgroundColor;
            float _StrokeOpacity;
            float _Duration;
            float _MaxRadius;
            float _TriggerTime;
            float4 _RippleCenter;
            float _Seed;
            float _GrainStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash11(float n)
            {
                return frac(sin(n * 127.1 + _Seed * 0.013) * 43758.5453123);
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7)) + _Seed * 0.017) * 43758.5453123);
            }

            float capsuleSdf(float2 p, float2 center, float lengthValue, float width, float angle)
            {
                float2 q = p - center;
                float s = sin(-angle);
                float c = cos(-angle);
                q = float2(q.x * c - q.y * s, q.x * s + q.y * c);
                float radius = width * 0.5;
                float halfLine = max(0.0, lengthValue * 0.5 - radius);
                float2 d = float2(abs(q.x) - halfLine, abs(q.y));
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
            }

            float ringMask(float2 p, float radius, float thickness, float feather)
            {
                float d = abs(length(p) - radius);
                return 1.0 - smoothstep(thickness, thickness + feather, d);
            }

            float3 palette(float id)
            {
                float h = frac(id * 0.6180339 + 0.08);
                if (h < 0.16) return float3(1.00, 0.34, 0.36);
                if (h < 0.30) return float3(1.00, 0.55, 0.60);
                if (h < 0.44) return float3(1.00, 0.90, 0.55);
                if (h < 0.58) return float3(0.90, 0.72, 1.00);
                if (h < 0.72) return float3(0.56, 0.91, 1.00);
                if (h < 0.86) return float3(0.73, 1.00, 0.94);
                return float3(1.00, 0.37, 0.40);
            }

            float3 blendOver(float3 baseColor, float3 color, float alpha)
            {
                alpha = saturate(alpha);
                float3 mixed = lerp(baseColor, color, alpha);
                mixed += color * alpha * 0.035;
                return saturate(mixed);
            }

            float dashLayer(float2 p, float progress, float phase, out float3 dashColor)
            {
                float total = 96.0;
                float best = 0.0;
                dashColor = 0.0;

                [loop]
                for (int i = 0; i < 96; i++)
                {
                    float fi = (float)i;
                    float t = fi / (total - 1.0);
                    float band = abs(t - progress);
                    float bandAlpha = 1.0 - smoothstep(0.0, 0.30, band);

                    if (bandAlpha <= 0.001)
                    {
                        continue;
                    }

                    float angle = phase + t * 34.5575 + progress * 2.6704;
                    float radius = lerp(0.035, _MaxRadius * progress, pow(t, 0.92));
                    float jitter = (hash11(fi + phase * 100.0) - 0.5) * 0.085;
                    float2 pos = float2(cos(angle + jitter), sin(angle + jitter)) * radius;

                    float lengthValue = lerp(0.030, 0.105, hash11(fi * 13.0 + 3.0)) * lerp(0.65, 1.2, progress);
                    float widthValue = lerp(0.013, 0.035, hash11(fi * 17.0 + 9.0));
                    float rot = angle + 1.5708 + lerp(-0.5, 0.5, hash11(fi * 31.0 + 1.0));
                    float d = capsuleSdf(p, pos, lengthValue, widthValue, rot);
                    float mask = 1.0 - smoothstep(0.0, lerp(0.007, 0.026, 0.72), d);
                        float alpha = lerp(0.25, 0.70, hash11(fi * 29.0 + 6.0)) * _StrokeOpacity * bandAlpha * sin(progress * 3.14159);

                    float visible = mask * alpha;
                    if (visible > best)
                    {
                        best = visible;
                        float3 col = palette(fi + phase * 10.0);
                        float warmBias = saturate((pos.x + 0.4) / 0.9);
                        dashColor = lerp(col, float3(1.0, 0.36, 0.39), warmBias * 0.30);
                    }

                    if (hash11(fi * 19.0 + 4.0) > 0.58)
                    {
                        float inner = 1.0 - smoothstep(0.0, lerp(0.007, 0.026, 0.72), capsuleSdf(p, pos, lengthValue * 0.60, widthValue * 0.45, rot));
                        best = max(best, inner * alpha * 0.20);
                    }
                }

                return best;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float aspect = max(_ScreenParams.x / max(_ScreenParams.y, 1.0), 1.0);
                float2 center = saturate(_RippleCenter.xy);
                float2 centerP = float2((center.x * 2.0 - 1.0) * aspect, center.y * 2.0 - 1.0);
                float2 p = float2((uv.x * 2.0 - 1.0) * aspect, uv.y * 2.0 - 1.0) - centerP;

                float grain = (hash21(floor(uv * _ScreenParams.xy * 0.8)) - 0.5) * _GrainStrength;
                float backgroundAlpha = saturate(_BackgroundColor.a);
                float3 color = saturate(_BackgroundColor.rgb + grain * backgroundAlpha);
                float outputAlpha = backgroundAlpha;

                float elapsed = _Time.y - _TriggerTime;
                float progress = saturate(elapsed / max(_Duration, 0.001));

                if (elapsed >= 0.0 && progress < 1.0)
                {
                    float wave = 1.0 - pow(1.0 - progress, 3.0);

                    [unroll]
                    for (int r = 0; r < 10; r++)
                    {
                        float delay = (float)r * 0.055;
                        float local = saturate((wave - delay) / max(1.0 - delay, 0.001));
                        float radius = lerp(0.035, _MaxRadius, 1.0 - pow(1.0 - local, 3.0));
                        float alpha = pow(1.0 - local, 1.28) * 0.28;
                        float thickness = lerp(0.010, 0.026, local);
                        float mask = ringMask(p, radius, thickness, lerp(0.012, 0.030, 0.72));
                        float3 ringColor = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.41, 0.44), (float)r / 9.0);
                        float visible = mask * alpha;
                        color = blendOver(color, ringColor, visible);
                        outputAlpha = max(outputAlpha, visible);
                    }

                    float3 dashColorA;
                    float dashA = dashLayer(p, wave, 0.0, dashColorA);
                    color = blendOver(color, dashColorA, dashA);
                    outputAlpha = max(outputAlpha, dashA);

                    float3 dashColorB;
                    float dashB = dashLayer(p, saturate(wave * 0.86), 1.7, dashColorB);
                    color = blendOver(color, dashColorB, dashB);
                    outputAlpha = max(outputAlpha, dashB);
                }

                return float4(color, saturate(outputAlpha));
            }
            ENDHLSL
        }
    }
}
