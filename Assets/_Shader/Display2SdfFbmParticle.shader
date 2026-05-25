Shader "INFO90003/Display2 SDF FBM Particle"
{
    Properties
    {
        _ColorA ("Flow Color A", Color) = (0.08, 1.0, 0.58, 1)
        _ColorB ("Flow Color B", Color) = (0.30, 0.95, 1.0, 1)
        _CoreRadius ("Core Radius", Range(0.02, 0.8)) = 0.18
        _HaloRadius ("Halo Radius", Range(0.1, 2.0)) = 0.85
        _SdfSoftness ("SDF Softness", Range(0.01, 1.0)) = 0.32
        _CorePower ("Core Power", Range(0.0, 8.0)) = 2.6
        _HaloPower ("Halo Power", Range(0.0, 8.0)) = 1.35
        _EmissionPower ("Emission Power", Range(0.0, 10.0)) = 2.4
        _FbmScale ("FBM Scale", Range(0.2, 16.0)) = 4.2
        _FbmFlowSpeed ("FBM Flow Speed", Range(0.0, 3.0)) = 0.18
        _AlphaPower ("Alpha Power", Range(0.2, 4.0)) = 1.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+40"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend One One
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            float4 _ColorA;
            float4 _ColorB;
            float _CoreRadius;
            float _HaloRadius;
            float _SdfSoftness;
            float _CorePower;
            float _HaloPower;
            float _EmissionPower;
            float _FbmScale;
            float _FbmFlowSpeed;
            float _AlphaPower;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise21(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise21(p);
                    p = p * 2.03 + 17.13;
                    a *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv * 2.0 - 1.0;
                float distanceFromCenter = length(centered);
                float sdf = distanceFromCenter - _CoreRadius;

                float core = 1.0 - smoothstep(0.0, max(_SdfSoftness, 0.001), sdf);
                float halo = 1.0 - smoothstep(_CoreRadius, max(_HaloRadius, _CoreRadius + 0.001), distanceFromCenter);
                halo = max(halo - core * 0.35, 0.0);

                float flow = fbm(centered * _FbmScale + float2(_Time.y * _FbmFlowSpeed, -_Time.y * _FbmFlowSpeed * 0.63));
                float3 flowColor = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0.18, 0.92, flow));
                float3 particleColor = max(i.color.rgb, 0.0001);

                float mask = pow(saturate(core + halo * 0.72), _AlphaPower);
                float opacityBoost = lerp(0.35, 1.0, saturate(i.color.a * 4.0));
                float emission = core * _CorePower + halo * _HaloPower;
                emission *= _EmissionPower * opacityBoost;
                emission *= lerp(0.78, 1.25, flow);

                float alpha = mask * i.color.a;
                float3 color = flowColor * particleColor * emission * mask;
                return float4(color, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
