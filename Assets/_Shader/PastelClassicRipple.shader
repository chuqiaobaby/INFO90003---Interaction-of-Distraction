Shader "INFO90003/Display 2 Cinematic Particle Field HLSL"
{
    // NOTE (Display 2 only): The visual look of this shader was replaced with a
    // fluid "lava lamp" metaball effect (ported from FluidLavaLamp.shader).
    // The shader NAME and all existing properties are kept unchanged so that
    // PastelClassicRippleController.cs / ActivateProjector.cs and the MediaPipe
    // hand tracking keep working exactly as before. The tracked hand positions
    // (_Hand0.._Hand3) now drive the blob attraction instead of a mouse cursor.
    Properties
    {
        // --- Existing Display 2 properties (kept for controller compatibility) ---
        _BackgroundColor ("Transparent Background", Color) = (0, 0, 0, 0)
        _StrokeOpacity ("Particle Opacity", Range(0, 3)) = 1.75
        _Duration ("Burst Duration", Float) = 2.8
        _MaxRadius ("Burst Radius", Float) = 1.25
        _TriggerTime ("Legacy Trigger Time", Float) = -1000
        _RippleCenter ("Legacy Center", Vector) = (0.5, 0.5, 0, 0)
        _Seed ("Seed", Float) = 90003
        _GrainStrength ("Dust Amount", Range(0, 1)) = 0.18
        _FlowSpeed ("Liquid Flow Speed", Range(0, 4)) = 0.72
        _TrailLength ("Glow Trail Length", Range(0.2, 4)) = 2.35
        _BloomBoost ("Bloom Boost", Range(0, 12)) = 6.5
        _HandInfluence ("Hand Flow Influence", Range(0, 4)) = 1.65
        _ParticleDensity ("Particle Density", Range(0.4, 3)) = 1.55
        _InteractionRadius ("Interaction Radius", Range(0.25, 1.5)) = 0.62
        _ContinuousTrailStrength ("Continuous Trail Strength", Range(0, 4)) = 2.6

        // --- New lava-lamp look properties (defaults match FluidLavaLamp.shader) ---
        _Speed ("Lava Animation Speed", Float) = 0.4
        _BlobCount ("Lava Blob Count", Int) = 5
        _GlowIntensity ("Lava Glow Intensity", Float) = 1.5
        _MouseInfluence ("Hand Influence Strength (lava)", Float) = 0.42
        _Color1 ("Color 1 (Pink)", Color) = (1.0, 0.2, 0.7, 1)
        _Color2 ("Color 2 (Blue)", Color) = (0.1, 0.5, 1.0, 1)
        _Color3 ("Color 3 (White Core)", Color) = (1.0, 0.9, 1.0, 1)

        // --- Range / touch tuning (smaller = more contained) ---
        _FieldScale ("Lava Field Size", Range(0.2, 1.5)) = 0.55
        _BlobRadius ("Lava Blob Radius", Range(0.03, 0.2)) = 0.07
        _HaloTightness ("Glow Halo Tightness", Range(6, 24)) = 16
        _TouchBoost ("Touch (water) Reaction Strength", Range(0, 4)) = 2.2
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
            // Additive blend keeps the projection transparent: black areas of the
            // lava lamp add no light, so the projector only shows the glow.
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

            // --- Existing Display 2 uniforms (declared so controller SetX calls bind) ---
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

            // Hand tracking inputs (fed every frame by PastelClassicRippleController).
            // .xy = normalized position, .z = active flag (1 = hand present).
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

            // --- Lava lamp look uniforms ---
            float _Speed;
            int _BlobCount;
            float _GlowIntensity;
            float _MouseInfluence;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float _FieldScale;
            float _BlobRadius;
            float _HaloTightness;
            float _TouchBoost;

            // Hash functions for pseudo-random values
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            // Smooth noise
            float snoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(dot(hash2(i + float2(0,0)), f - float2(0,0)),
                                 dot(hash2(i + float2(1,0)), f - float2(1,0)), u.x),
                            lerp(dot(hash2(i + float2(0,1)), f - float2(0,1)),
                                 dot(hash2(i + float2(1,1)), f - float2(1,1)), u.x), u.y);
            }

            // FBM (Fractal Brownian Motion) for organic distortion
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < 5; i++)
                {
                    value += amplitude * snoise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.1;
                }
                return value;
            }

            // Smooth minimum for blob merging
            float smin(float a, float b, float k)
            {
                float h = max(k - abs(a - b), 0.0) / k;
                return min(a, b) - h * h * k * 0.25;
            }

            // Single metaball SDF
            float metaball(float2 uv, float2 center, float radius)
            {
                float d = length(uv - center);
                return d - radius;
            }

            // Fetch hand i (.xy position, .z active flag)
            float4 getHand(int index)
            {
                if (index == 0) return _Hand0;
                if (index == 1) return _Hand1;
                if (index == 2) return _Hand2;
                return _Hand3;
            }

            // Fetch burst i (.xy center, .z start time, .w active) — set on touch.
            float4 getBurst(int index)
            {
                if (index == 0) return _Burst0;
                if (index == 1) return _Burst1;
                if (index == 2) return _Burst2;
                return _Burst3;
            }

            // Fetch hand velocity i (.xy = normalized velocity) — used for comet tails.
            float4 getVelocity(int index)
            {
                if (index == 0) return _Velocity0;
                if (index == 1) return _Velocity1;
                if (index == 2) return _Velocity2;
                return _Velocity3;
            }

            // Primary "cursor" = first active hand, else screen centre.
            float2 effectiveHandPos()
            {
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 h = getHand(i);
                    if (h.z > 0.5) return saturate(h.xy);
                }
                return float2(0.5, 0.5);
            }

            // 0..1 amount of any active hand on screen.
            float anyHandActive()
            {
                float a = 0.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    a = max(a, saturate(getHand(i).z));
                }
                return a;
            }

            // 0..1 amount of any active touch burst. This keeps Space/manual
            // triggering visible even when no hand is currently tracked.
            float anyBurstActive()
            {
                float t = _Time.y;
                float a = 0.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 b = getBurst(i);
                    float age = t - b.z;
                    a = max(a, saturate(b.w) * step(0.0, age) * step(age, _Duration));
                }
                return a;
            }

            // How close a blob is to any active hand (used to inflate blobs).
            float handProximity(float2 center)
            {
                float m = 0.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 h = getHand(i);
                    float active = saturate(h.z);
                    m = max(m, active * exp(-length(h.xy - center) * 4.0));
                }
                return m;
            }

            // Generate blob center position for index i, attracted by every active hand.
            float2 getBlobCenter(int i, float t, float influence)
            {
                float fi = float(i);
                float baseX = 0.3 + 0.4 * hash(fi * 13.7);
                float baseY = 0.2 + 0.6 * hash(fi * 7.3);

                // Lava lamp style: blobs rise and fall
                float risePhase = t * (0.5 + hash(fi * 3.1) * 0.4) + fi * 1.2;
                float xWobble = sin(risePhase * 1.3 + fi) * 0.11;
                float yRise = sin(risePhase + fi * 2.0) * 0.38;

                // Secondary organic drift
                float2 noise2 = float2(
                    snoise(float2(t * 0.3 + fi * 5.1, fi)),
                    snoise(float2(fi * 4.2, t * 0.25 + fi * 3.7))
                ) * 0.16;

                // Slow autonomous circular drift so the lava keeps moving even when
                // nobody is touching the water (idle state still looks alive).
                float2 idleDrift = float2(sin(t * 0.45 + fi * 2.1), cos(t * 0.37 + fi * 1.6)) * 0.07;

                float2 pos = float2(baseX + xWobble, baseY + yRise) + noise2 + idleDrift;

                // Compress the whole blob field toward the center so the effect
                // occupies a smaller, more contained region (tunable: _FieldScale).
                pos = float2(0.5, 0.5) + (pos - float2(0.5, 0.5)) * _FieldScale;

                // Hand attraction/repulsion (sum over all active tracked hands)
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    float4 h = getHand(k);
                    float active = saturate(h.z);
                    float2 toHand = h.xy - pos;
                    float handDist = length(toHand);
                    float force = influence / (handDist * handDist + 0.12);
                    force = clamp(force, 0.0, 0.5) * active;
                    pos += normalize(toHand + float2(0.001, 0.001)) * force;
                }

                return clamp(pos, float2(0.05, 0.05), float2(0.95, 0.95));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // ===== Original "Cinematic Particle Field" ripple (ported back) =====
            // The water-ripple touch feedback from the very first version.

            float3 palette(float id, float heat)
            {
                float h = frac(id * 0.381966 + heat * 0.25);
                float3 darkNavy = float3(0.006, 0.025, 0.085);
                float3 deepOcean = float3(0.00, 0.16, 0.36);
                float3 icyCyan = float3(0.18, 0.92, 1.0);
                float3 marbleWhite = float3(0.88, 0.98, 1.0);

                float3 c = lerp(darkNavy, deepOcean, smoothstep(0.02, 0.38, h));
                c = lerp(c, icyCyan, smoothstep(0.32, 0.76, h));
                c = lerp(c, marbleWhite, pow(saturate(heat), 1.9));
                return c;
            }

            float2 centerToField(float2 center, float aspect)
            {
                return float2((saturate(center.x) * 2.0 - 1.0) * aspect, saturate(center.y) * 2.0 - 1.0);
            }

            // Expanding concentric water ripple, stamped on each touch (_Burst data).
            float burstMask(float2 p, float aspect, out float3 burstColor)
            {
                float t = _Time.y;
                float burst = 0.0;
                float heat = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 burstData = getBurst(i);
                    float start = burstData.z;
                    float active = saturate(burstData.w);
                    float age = t - start;
                    float life = saturate(age / max(_Duration, 0.001));
                    float2 c = centerToField(burstData.xy, aspect);
                    float d = length(p - c);
                    float radius = lerp(0.025, _MaxRadius, 1.0 - pow(1.0 - life, 2.6));
                    float ring = exp(-pow((d - radius) / 0.055, 2.0));
                    float core = exp(-d * d * lerp(28.0, 2.3, life));
                    float fade = active * step(0.0, age) * step(age, _Duration) * pow(1.0 - life, 1.15);
                    burst += (ring * 1.2 + core * 0.85) * fade;
                    heat += (ring + core) * fade;
                }

                burstColor = palette(8.0, saturate(heat * 1.4));
                return burst;
            }

            // Glowing aura that follows each hand, with a velocity-stretched comet
            // tail. Makes the lava-lamp interaction obvious and pretty.
            float3 handGlow(float2 uv, float t, out float glowAmt)
            {
                float3 col = float3(0, 0, 0);
                glowAmt = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 h = getHand(i);
                    float active = saturate(h.z);

                    float2 vel = getVelocity(i).xy;
                    float speed = saturate(length(vel) * 1.6);
                    float2 dir = normalize(vel + float2(0.0001, 0.0));

                    float2 d2 = uv - h.xy;
                    float along = dot(d2, dir);
                    float across = dot(d2, float2(-dir.y, dir.x));

                    // Stretch the tail behind the motion direction (comet shape).
                    float tail = lerp(1.0, 3.6, speed);
                    float a = (along < 0.0) ? along / tail : along;
                    float dist = length(float2(a, across));

                    float core = exp(-dist * dist * 170.0);          // hot center
                    float halo = exp(-dist * 8.5);                   // soft glow
                    float pulse = 0.65 + 0.35 * sin(t * 3.0 + i * 1.7);

                    // Iridescent rim that drifts between the two fluid colors.
                    float3 rimCol = lerp(_Color2.rgb, _Color1.rgb, 0.5 + 0.5 * sin(t * 1.4 + across * 7.0 + i));
                    col += active * (rimCol * halo * pulse * 0.95 + _Color3.rgb * core * 1.9);
                    glowAmt += active * (halo * 0.5 + core);
                }

                return col;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Field-space coords (aspect-correct) for the original ripple layer.
                float aspect = max(_ScreenParams.x / max(_ScreenParams.y, 1.0), 1.0);
                float2 pField = float2((uv.x * 2.0 - 1.0) * aspect, uv.y * 2.0 - 1.0);

                // Speed driven by lava _Speed, scaled by the existing _FlowSpeed slider.
                float t = _Time.y * _Speed * (_FlowSpeed / 0.72);

                // Strength of hand influence on the fluid.
                float influence = _MouseInfluence;

                // Primary hand acts like the "mouse" in the original lava lamp.
                float2 handPos = effectiveHandPos();
                float handAmount = anyHandActive();
                float burstAmount = anyBurstActive();

                // No hand and no touch burst means no Display 2 projection.
                // A manual Space burst must still be visible without tracked hands.
                if (handAmount <= 0.001 && burstAmount <= 0.001)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                float2 toHand = uv - handPos;
                float handDist = length(toHand);

                // Organic UV distortion based on FBM
                float2 distortedUV = uv;
                float distortStrength = 0.06 + influence * 0.04;
                distortedUV += float2(
                    fbm(uv * 2.5 + float2(t * 0.4, 0.0)),
                    fbm(uv * 2.5 + float2(0.0, t * 0.35) + float2(5.2, 1.3))
                ) * distortStrength;

                // Hand-driven swirl effect (only kicks in where a hand is present)
                float swirlAngle = influence * handAmount * exp(-handDist * 2.2) * sin(t * 1.5) * 2.6;
                float cs = cos(swirlAngle);
                float sn = sin(swirlAngle);
                float2 centered = distortedUV - handPos;
                distortedUV = float2(
                    cs * centered.x - sn * centered.y,
                    sn * centered.x + cs * centered.y
                ) + handPos;

                // Build metaball field (smaller base radius => more contained look)
                float field = 1000.0;
                float blobRadius = _BlobRadius + 0.03 * sin(t * 0.7);

                [unroll(6)]
                for (int b = 0; b < 6; b++)
                {
                    if (b >= _BlobCount) break;
                    float2 center = getBlobCenter(b, t, influence);
                    float r = blobRadius * (0.8 + 0.3 * hash(float(b) * 9.1));
                    // Hand proximity inflates blobs (stronger, more obvious)
                    float hProx = handProximity(center);
                    r += hProx * 0.11 * influence * (1.0 + handAmount * 1.8);
                    field = smin(field, metaball(distortedUV, center, r), 0.08);
                }

                // --- Color & shading ---
                float glowI = _GlowIntensity * (_StrokeOpacity / 2.2);

                // Breathing pulse while the hand is active.
                float breathe = 0.85 + 0.35 * sin(_Time.y * 0.9) + 0.12 * sin(_Time.y * 0.37 + 1.7);

                // Interior density (0 = surface, positive = inside)
                float density = -field;
                float surface = 1.0 - smoothstep(-0.01, 0.025, field);
                float interior = smoothstep(0.0, 0.08, density);

                // Layered color mixing based on FBM and distance from the hand
                float colorNoise = fbm(distortedUV * 3.0 + float2(t * 0.2, 0.0));
                float colorNoise2 = fbm(distortedUV * 5.0 - float2(0.0, t * 0.15));

                float handColorShift = exp(-handDist * 2.5) * 0.5 * handAmount;
                float colorBlend = saturate(colorNoise * 0.5 + 0.5 + handColorShift);
                float coreBlend = saturate(colorNoise2 * 0.5 + 0.5);

                // Base fluid color (pink <-> blue gradient)
                float3 fluidColor = lerp(_Color1.rgb, _Color2.rgb, colorBlend);

                // Hot white core in centers
                float3 coreColor = _Color3.rgb;
                fluidColor = lerp(fluidColor, coreColor, coreBlend * coreBlend * interior * 0.7);

                // Cyan/blue outline glow (the bright edges)
                float edgeMask = smoothstep(0.04, 0.0, abs(field + 0.005));
                float3 edgeGlow = float3(0.3, 0.8, 1.0) * edgeMask * glowI * 2.5;

                // Outer ambient glow (soft halo). Higher _HaloTightness => smaller halo.
                float outerGlow = exp(field * _HaloTightness) * smoothstep(0.32, 0.0, abs(field)) * glowI;
                float3 glowColor = lerp(_Color2.rgb, _Color1.rgb, sin(t + distortedUV.y * 3.0) * 0.5 + 0.5);
                float3 ambientGlow = glowColor * outerGlow * 0.6;

                // Bright speculars
                float specNoise = fbm(distortedUV * 8.0 + float2(t, 0));
                float spec = pow(saturate(specNoise * 0.5 + 0.5), 6.0) * interior * 0.8;

                // While touching (water touch / space), brighten the whole fluid so
                // the moment of contact is clearly visible.
                float touchLift = 1.0 + handAmount * _TouchBoost;

                // Compose everything over black background.
                float3 finalColor = float3(0, 0, 0);
                finalColor += ambientGlow * touchLift * breathe;
                finalColor += fluidColor * surface * glowI * touchLift * (0.85 + breathe * 0.35);
                finalColor += edgeGlow * (1.0 + handAmount * _TouchBoost * 0.6) * (0.7 + breathe * 0.3);
                finalColor += float3(1.0, 1.0, 1.0) * spec * surface;

                // Glowing comet aura that follows the hand (the interactive highlight).
                float handGlowAmt;
                float3 hg = handGlow(uv, t, handGlowAmt);
                finalColor += hg * glowI * (1.2 + _HandInfluence * 0.6);

                // Tone map and gamma (lava base only)
                finalColor = finalColor / (finalColor + 0.8);
                finalColor = pow(saturate(finalColor), 0.4545);

                // Add optional explicit background tint (defaults to transparent / none).
                finalColor += _BackgroundColor.rgb * _BackgroundColor.a;

                // === Original cinematic water ripple, overlaid on touch ===
                // Added after tone-map so it stays bright/punchy like the first version.
                float3 rippleColor;
                float ripple = burstMask(pField, aspect, rippleColor);
                float rippleBloom = (1.0 + _BloomBoost * 0.32) * _StrokeOpacity;
                float3 rippleOut = rippleColor * ripple * rippleBloom;
                finalColor += rippleOut;

                // Alpha used as a luminance mask so the projection stays clean on black.
                float alphaLike = saturate(max(finalColor.r, max(finalColor.g, finalColor.b)) + ripple * 0.6);
                return float4(finalColor, alphaLike);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
