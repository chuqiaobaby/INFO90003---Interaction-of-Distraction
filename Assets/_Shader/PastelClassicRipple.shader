Shader "INFO90003/Display 2 Cinematic Particle Field HLSL"
{
    Properties
    {
        _BackgroundColor ("Transparent Background", Color) = (0, 0, 0, 0)
        _StrokeOpacity ("Ink Opacity", Range(0, 3)) = 2.2
        _Duration ("Touch Burst Duration", Float) = 2.8
        _MaxRadius ("Touch Burst Radius", Float) = 1.25
        _TriggerTime ("Legacy Trigger Time", Float) = -1000
        _RippleCenter ("Legacy Center", Vector) = (0.5, 0.5, 0, 0)
        _Seed ("Seed", Float) = 90003
        _GrainStrength ("Film Grain", Range(0, 1)) = 0.18
        _FlowSpeed ("Ink Flow Speed", Range(0, 4)) = 0.72
        _TrailLength ("Legacy Trail Length", Range(0.2, 4)) = 3.0
        _BloomBoost ("Bloom Boost", Range(0, 12)) = 8.5
        _HandInfluence ("Hand Flow Influence", Range(0, 4)) = 2.25
        _ParticleDensity ("Ink Density", Range(0.4, 3)) = 1.95
        _InteractionRadius ("Interaction Radius", Range(0.25, 1.5)) = 0.62
        _ContinuousTrailStrength ("Continuous Trail Strength", Range(0, 4)) = 2.6
        _RainbowMode ("Rainbow Mode", Float) = 1

        _InkPaletteCount ("Ink Palette Count", Float) = 4
        _InkColor0 ("Ink Color 0", Color) = (0.08, 1.00, 0.45, 1)
        _InkColor1 ("Ink Color 1", Color) = (0.36, 1.00, 0.08, 1)
        _InkColor2 ("Ink Color 2", Color) = (0.95, 0.95, 0.05, 1)
        _InkColor3 ("Ink Color 3", Color) = (0.00, 0.92, 1.00, 1)
        _InkColor4 ("Ink Color 4", Color) = (1.00, 0.42, 0.18, 1)
        _InkColor5 ("Ink Color 5", Color) = (0.08, 1.00, 0.45, 1)
        _InkColor6 ("Ink Color 6", Color) = (0.36, 1.00, 0.08, 1)
        _InkColor7 ("Ink Color 7", Color) = (0.95, 0.95, 0.05, 1)
        _InkColorCycleSeconds ("Ink Color Cycle Seconds", Float) = 2
        _InkFadeSeconds ("Ink Fade Seconds", Float) = 2
        _InkSplatForce ("Ink Splat Force", Range(0, 4)) = 1.65
        _InkAngularVelocityRange ("Ink Angular Velocity Range", Vector) = (-1.15, 1.45, 0, 0)
        _InkRadiusDrift ("Ink Radius Drift", Range(0, 1.5)) = 0.42
        _InkSpeedDrift ("Ink Speed Drift", Range(0, 2)) = 0.75
        _InkChaos ("Ink Chaos", Range(0, 3)) = 1.35
        _InkDiffusion ("Ink Diffusion", Range(0, 3)) = 1.25
        _InkBrightness ("Ink Brightness", Range(0.2, 4)) = 2.25
        _InkSoftness ("Ink Softness", Range(0.2, 4)) = 1.7
        _InkNoiseStrength ("Ink Noise Strength", Range(0, 3)) = 1.15
        _InkNoiseScale ("Ink Noise Scale", Range(0.5, 12)) = 4.8
        _InkTrailStretch ("Ink Trail Stretch", Range(0.2, 5)) = 2.1
        _InkWhiteCore ("Ink White Core", Range(0, 2)) = 0.42
        _InkBurstSize ("Ink Burst Size", Range(0.3, 3)) = 1.15

        // Legacy properties kept so old scene/material data keeps loading.
        _Speed ("Legacy Speed", Float) = 0.4
        _BlobCount ("Legacy Blob Count", Int) = 5
        _GlowIntensity ("Legacy Glow Intensity", Float) = 1.5
        _MouseInfluence ("Legacy Mouse Influence", Float) = 0.42
        _Color1 ("Legacy Color 1", Color) = (1.0, 0.2, 0.7, 1)
        _Color2 ("Legacy Color 2", Color) = (0.1, 0.5, 1.0, 1)
        _Color3 ("Legacy Color 3", Color) = (1.0, 0.9, 1.0, 1)
        _FieldScale ("Legacy Field Size", Range(0.2, 1.5)) = 0.55
        _BlobRadius ("Legacy Blob Radius", Range(0.03, 0.2)) = 0.07
        _HaloTightness ("Legacy Halo Tightness", Range(6, 24)) = 16
        _TouchBoost ("Legacy Touch Boost", Range(0, 4)) = 2.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+25"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend One One
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
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
            float _FlowSpeed;
            float _TrailLength;
            float _BloomBoost;
            float _HandInfluence;
            float _ParticleDensity;
            float _InteractionRadius;
            float _ContinuousTrailStrength;
            float _RainbowMode;

            float _InkPaletteCount;
            float4 _InkColor0;
            float4 _InkColor1;
            float4 _InkColor2;
            float4 _InkColor3;
            float4 _InkColor4;
            float4 _InkColor5;
            float4 _InkColor6;
            float4 _InkColor7;
            float _InkColorCycleSeconds;
            float _InkFadeSeconds;
            float _InkSplatForce;
            float4 _InkAngularVelocityRange;
            float _InkRadiusDrift;
            float _InkSpeedDrift;
            float _InkChaos;
            float _InkDiffusion;
            float _InkBrightness;
            float _InkSoftness;
            float _InkNoiseStrength;
            float _InkNoiseScale;
            float _InkTrailStretch;
            float _InkWhiteCore;
            float _InkBurstSize;

            float4 _Hand0;
            float4 _Hand1;
            float4 _Hand2;
            float4 _Hand3;
            float4 _Velocity0;
            float4 _Velocity1;
            float4 _Velocity2;
            float4 _Velocity3;
            float4 _Burst0;
            float4 _Burst1;
            float4 _Burst2;
            float4 _Burst3;
            float4 _Trail0;
            float4 _Trail1;
            float4 _Trail2;
            float4 _Trail3;
            float4 _Trail4;
            float4 _Trail5;
            float4 _Trail6;
            float4 _Trail7;
            float4 _Trail8;
            float4 _Trail9;
            float4 _Trail10;
            float4 _Trail11;
            float4 _Trail12;
            float4 _Trail13;
            float4 _Trail14;
            float4 _Trail15;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash11(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            float noise21(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash11(dot(i + float2(0.0, 0.0), float2(1.0, 57.0)));
                float b = hash11(dot(i + float2(1.0, 0.0), float2(1.0, 57.0)));
                float c = hash11(dot(i + float2(0.0, 1.0), float2(1.0, 57.0)));
                float d = hash11(dot(i + float2(1.0, 1.0), float2(1.0, 57.0)));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    v += a * noise21(p);
                    p = p * 2.02 + 19.31;
                    a *= 0.5;
                }
                return v;
            }

            float4 getHand(int index)
            {
                if (index == 0) return _Hand0;
                if (index == 1) return _Hand1;
                if (index == 2) return _Hand2;
                return _Hand3;
            }

            float4 getVelocity(int index)
            {
                if (index == 0) return _Velocity0;
                if (index == 1) return _Velocity1;
                if (index == 2) return _Velocity2;
                return _Velocity3;
            }

            float4 getBurst(int index)
            {
                if (index == 0) return _Burst0;
                if (index == 1) return _Burst1;
                if (index == 2) return _Burst2;
                return _Burst3;
            }

            float4 getTrail(int index)
            {
                if (index == 0) return _Trail0;
                if (index == 1) return _Trail1;
                if (index == 2) return _Trail2;
                if (index == 3) return _Trail3;
                if (index == 4) return _Trail4;
                if (index == 5) return _Trail5;
                if (index == 6) return _Trail6;
                if (index == 7) return _Trail7;
                if (index == 8) return _Trail8;
                if (index == 9) return _Trail9;
                if (index == 10) return _Trail10;
                if (index == 11) return _Trail11;
                if (index == 12) return _Trail12;
                if (index == 13) return _Trail13;
                if (index == 14) return _Trail14;
                return _Trail15;
            }

            float3 getInkColor(int index)
            {
                if (index == 0) return _InkColor0.rgb;
                if (index == 1) return _InkColor1.rgb;
                if (index == 2) return _InkColor2.rgb;
                if (index == 3) return _InkColor3.rgb;
                if (index == 4) return _InkColor4.rgb;
                if (index == 5) return _InkColor5.rgb;
                if (index == 6) return _InkColor6.rgb;
                return _InkColor7.rgb;
            }

            float3 cycleInkColor(float t)
            {
                float count = clamp(_InkPaletteCount, 1.0, 8.0);
                if (count < 1.5)
                {
                    return getInkColor(0);
                }

                float cycle = t / max(_InkColorCycleSeconds, 0.1);
                float wrapped = cycle - floor(cycle / count) * count;
                int a = (int)floor(wrapped);
                int b = a + 1;
                if (b >= (int)count)
                {
                    b = 0;
                }

                float blend = frac(wrapped);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(getInkColor(a), getInkColor(b), blend);
            }

            float3 steppedInkColor(float t)
            {
                float count = clamp(_InkPaletteCount, 1.0, 8.0);
                if (count < 1.5)
                {
                    return getInkColor(0);
                }

                float cycle = floor(t / max(_InkColorCycleSeconds, 0.1));
                int index = (int)(cycle - floor(cycle / count) * count);
                return getInkColor(index);
            }

            float3 ribbonInkColor(float t, float sampleOffset)
            {
                float3 rainbowColor = cycleInkColor(t + sampleOffset * max(_InkColorCycleSeconds, 0.1));
                float3 singleJumpColor = steppedInkColor(t);
                return lerp(singleJumpColor, rainbowColor, step(0.5, _RainbowMode));
            }

            float2 toField(float2 uv, float aspect)
            {
                return float2((uv.x * 2.0 - 1.0) * aspect, uv.y * 2.0 - 1.0);
            }

            float2 centerToField(float2 center, float aspect)
            {
                return float2((saturate(center.x) * 2.0 - 1.0) * aspect, saturate(center.y) * 2.0 - 1.0);
            }

            float2 rotate2(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            float activeAmount()
            {
                float t = _Time.y;
                float lifeSeconds = max(_InkFadeSeconds, 0.1);
                float a = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 h = getHand(i);
                    a = max(a, saturate(h.z));

                    float4 b = getBurst(i);
                    float burstAge = t - b.z;
                    a = max(a, saturate(b.w) * step(0.0, burstAge) * step(burstAge, lifeSeconds));
                }

                [unroll]
                for (int j = 0; j < 16; j++)
                {
                    float4 tr = getTrail(j);
                    float age = t - tr.z;
                    a = max(a, saturate(tr.w) * step(0.0, age) * step(age, lifeSeconds));
                }

                return a;
            }

            float interactionMask(float2 p, float aspect, float t)
            {
                float mask = 0.0;
                float lifeSeconds = max(_InkFadeSeconds, 0.1);
                float baseRadius = 0.04 + _InteractionRadius * 0.12;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 h = getHand(i);
                    float2 hp = centerToField(h.xy, aspect);
                    float2 vh = getVelocity(i).xy;
                    float speed = saturate(length(vh) * 1.85);
                    float2 dir = normalize(float2(vh.x * aspect, vh.y) + float2(0.001, 0.0));
                    float2 q = p - hp;
                    float2 localQ = float2(dot(q, dir), dot(q, float2(-dir.y, dir.x)));
                    localQ.x /= 1.0 + speed * _InkTrailStretch * 0.9;
                    float handRadius = baseRadius * (1.0 + speed * 0.4);
                    mask = max(mask, saturate(h.z) * exp(-dot(localQ, localQ) / max(handRadius * handRadius, 0.0005)));

                    float4 b = getBurst(i);
                    float burstAge = t - b.z;
                    float burstActive = saturate(b.w) * step(0.0, burstAge) * step(burstAge, lifeSeconds);
                    float burstLife = saturate(1.0 - burstAge / lifeSeconds);
                    float2 bp = centerToField(b.xy, aspect);
                    float burstRadius = (0.055 + (1.0 - burstLife) * (0.12 + _InkDiffusion * 0.1)) * _InkBurstSize;
                    mask = max(mask, burstActive * burstLife * exp(-dot(p - bp, p - bp) / max(burstRadius * burstRadius, 0.0005)));
                }

                return saturate(mask * 1.35);
            }

            float softInkBlob(float2 q, float radius, float chaos, float randomSeed)
            {
                float d = length(q);
                float n1 = fbm(q * (_InkNoiseScale + chaos * 1.6) + randomSeed);
                float n2 = fbm(q * (_InkNoiseScale * 1.9 + chaos * 3.0) - randomSeed * 1.7);
                float warpedRadius = radius * _InkSoftness * (0.72 + 0.42 * n1 + 0.12 * chaos * _InkNoiseStrength * (n2 - 0.5));
                float core = exp(-(d * d) / max(warpedRadius * warpedRadius, 0.0005));
                float veil = exp(-d / max(warpedRadius * (1.65 + _InkSoftness * 0.55 + chaos * 0.18), 0.0005));
                float lace = pow(saturate(n2 * (1.2 + _InkNoiseStrength * 0.45) - 0.18), lerp(3.2, 1.2, saturate(chaos / 3.0)));
                return core * 0.78 + veil * lace * 0.44;
            }

            float inkTrail(float2 p, float aspect, float t, out float heat, out float3 colorEnergy)
            {
                heat = 0.0;
                colorEnergy = float3(0.0, 0.0, 0.0);
                return 0.0;
            }

            float inkHands(float2 p, float aspect, float t, out float heat, out float3 colorEnergy)
            {
                float density = 0.0;
                heat = 0.0;
                colorEnergy = float3(0.0, 0.0, 0.0);
                float baseRadius = 0.05 + _InteractionRadius * 0.12;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 h = getHand(i);
                    float active = saturate(h.z);
                    float2 hp = centerToField(h.xy, aspect);
                    float2 v = getVelocity(i).xy;
                    float speed = saturate(length(v) * 1.85);
                    float2 dir = normalize(float2(v.x * aspect, v.y) + float2(0.001, 0.0));
                    float2 q = p - hp;
                    float2 localQ = float2(dot(q, dir), dot(q, float2(-dir.y, dir.x)));
                    localQ.x /= 1.0 + speed * _InkTrailStretch * 0.55;
                    localQ += (fbm(q * (_InkNoiseScale * 0.65) + t * (0.6 + _FlowSpeed) + i * 9.0) - 0.5) * speed * _InkChaos * _InkNoiseStrength * 0.045;

                    float core = exp(-dot(localQ, localQ) / max(baseRadius * baseRadius * (0.72 + speed * 0.38), 0.0005));
                    float halo = exp(-dot(localQ, localQ) / max(baseRadius * baseRadius * (4.8 + speed * 3.0), 0.0005));
                    float wake = exp(-abs(localQ.y) / max(baseRadius * (1.15 + speed * 0.5), 0.001)) *
                        exp(min(localQ.x, 0.0) * (2.1 - speed * 0.45)) *
                        step(localQ.x, baseRadius * 0.75);
                    float local = active * (core * 1.15 + halo * 0.42 + wake * speed * 0.34) * (0.72 + _HandInfluence * 0.18);
                    density += local;
                    heat += local * (0.5 + speed);
                    colorEnergy += local * ribbonInkColor(t, i * 0.47 + speed * 0.34);
                }

                return density;
            }

            float inkBursts(float2 p, float aspect, float t, out float heat, out float3 colorEnergy)
            {
                float density = 0.0;
                heat = 0.0;
                colorEnergy = float3(0.0, 0.0, 0.0);
                float lifeSeconds = max(_InkFadeSeconds, 0.1);

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 b = getBurst(i);
                    float age = t - b.z;
                    float active = saturate(b.w) * step(0.0, age) * step(age, lifeSeconds);
                    float life = saturate(1.0 - age / lifeSeconds);
                    float born = 1.0 - life;
                    float rnd = hash11(i * 31.7 + _Seed);
                    float omega = lerp(_InkAngularVelocityRange.x, _InkAngularVelocityRange.y, rnd);
                    float2 center = centerToField(b.xy, aspect);
                    float2 q = rotate2(p - center, omega * age);
                    float radius = (0.055 + born * (0.11 + _MaxRadius * 0.065 + _InkDiffusion * 0.11)) * _InkBurstSize;
                    float local = softInkBlob(q, radius, _InkChaos + _InkSplatForce * 0.5, rnd * 15.0);
                    float fade = active * pow(life, 1.45);
                    float contribution = local * fade * (0.75 + _InkSplatForce * 0.45);
                    density += contribution;
                    heat += local * fade;
                    colorEnergy += contribution * ribbonInkColor(t, i * 0.53 + born * 0.9 + rnd * 0.5);
                }

                return density;
            }

            float4 frag(v2f i) : SV_Target
            {
                float active = activeAmount();
                if (active <= 0.001)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                float2 uv = i.uv;
                float aspect = max(_ScreenParams.x / max(_ScreenParams.y, 1.0), 1.0);
                float2 p = toField(uv, aspect);
                float t = _Time.y;

                float trailHeat;
                float handHeat;
                float burstHeat;
                float3 trailColorEnergy;
                float3 handColorEnergy;
                float3 burstColorEnergy;
                float spatialMask = interactionMask(p, aspect, t);
                float trail = inkTrail(p, aspect, t, trailHeat, trailColorEnergy);
                float hand = inkHands(p, aspect, t, handHeat, handColorEnergy);
                float burst = inkBursts(p, aspect, t, burstHeat, burstColorEnergy);
                float density = (trail + hand + burst) * spatialMask;

                float grain = fbm(p * 42.0 + t * 0.45 + _Seed);
                float softMask = saturate(density * (0.58 + _ParticleDensity * 0.28));
                if (softMask <= 0.003)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }
                float edgeMist = pow(saturate(density), 1.75) * 0.18;
                float brightness = _InkBrightness * (0.72 + _BloomBoost * 0.08);
                float3 ink = (trailColorEnergy + handColorEnergy + burstColorEnergy) / max(trail + hand + burst, 0.001);
                ink = pow(saturate(ink), 0.72);

                float whiteCore = saturate(((handHeat + burstHeat) * 0.32 + pow(softMask, 2.2) * 0.28) * _InkWhiteCore);
                float3 color = lerp(ink, float3(0.88, 0.96, 1.0), whiteCore);
                color *= (softMask + edgeMist) * brightness * _StrokeOpacity;
                color *= lerp(1.0 - _GrainStrength * 0.25, 1.0 + _GrainStrength * 0.35, grain);
                color += _BackgroundColor.rgb * _BackgroundColor.a;

                float alpha = saturate(max(color.r, max(color.g, color.b)) * 0.7 + softMask * 0.2);
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
