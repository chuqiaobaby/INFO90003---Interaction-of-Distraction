// ══════════════════════════════════════════════════════════════════════════════
//  EtherealFluidTrail.shader  —  URP  (SrcAlpha One additive, no Opaque Tex)
//
//  VISUAL:  Flowing vertical light drip-streaks with aurora-like nebula glow.
//           Wet-neon / plasma-trail / long-exposure light-painting aesthetic.
//           Organic liquid motion — streaks bend in domain-warped flow field.
//
//  LIFECYCLE  (shared IDs with LiquidGlassAnomaly / DigitalGlitchGrid):
//    _SpawnProgress  0→0.25  vertical strips drip IN  top-to-bottom
//                    0.25→0.75  hold (drips animate freely)
//                    0.75→1.00  strips drain OUT bottom-to-top
//    _BlowFade       1→0 during blow  (fade-out override)
//    _Opacity        peak alpha
//    _TimeOffset     per-instance desync
//    _IriOffset      per-instance palette shift
//    _CoreColor      [HDR] — tints entire effect; used by syncTouchColors
//
//  LAYERS:
//    1. Aurora Nebula  — anisotropic FBM background glow + colour-bleed
//    2. Fine Drips     — DripDensity columns, fast
//    3. Medium Drips   — half density, slower
//    4. Coarse Drips   — quarter density, widest glow
//    5. Core Diffusion — radial Gaussian centred on the quad
//
//  KEY PARAMETERS:
//    _StreakLength      exponential trail-decay — higher = longer trails
//    _DiffusionIntensity  how blurry / hazy the nebula is + sway amount
//    _FlickerSpeed      how fast the nebula aurora bands drift downward
// ══════════════════════════════════════════════════════════════════════════════

Shader "Custom/EtherealFluidTrail"
{
    Properties
    {
        // ── Lifecycle (shared with other touch effects) ───────────────────────
        _SpawnProgress  ("Spawn Progress",          Range(0,1))   = 0
        _BlowFade       ("Blow Fade",               Range(0,1))   = 1
        _Opacity        ("Opacity",                 Range(0,1))   = 1
        _TimeOffset     ("Time Offset",             Float)        = 0
        _IriOffset      ("Iridescence Offset",      Range(0,1))   = 0
        [HDR] _CoreColor("Core Color",              Color)        = (0.3, 1.5, 2.5, 1)

        // ── Trail effect ─────────────────────────────────────────────────────
        [Header(Trail Effect)]
        _StreakLength        ("Streak Length  (trail decay)",     Range(1,  20))   = 6.0
        _DiffusionIntensity ("Diffusion Intensity",               Range(0,   1))   = 0.55
        _FlickerSpeed       ("Flow Speed  (nebula drift)",        Range(0,   5))   = 0.90
        _DripSpeed          ("Drip Fall Speed",                   Range(0,   3))   = 0.70
        _DripDensity        ("Drip Column Density",               Range(4,  30))   = 12
        _GlowWidth          ("Streak Glow Width",                 Range(0.5, 8))   = 3.5
        _ColorBleed         ("Colour Bleed (chromatic spread)",   Range(0, 0.05))  = 0.018

        // ── Colours ──────────────────────────────────────────────────────────
        [Header(Colors)]
        [HDR] _Color1   ("Color 1",               Color) = (0.30, 0.10, 2.00, 1)
        [HDR] _Color2   ("Color 2",               Color) = (0.05, 1.20, 2.00, 1)
        [HDR] _Color3   ("Color 3",               Color) = (1.60, 0.10, 0.80, 1)
        [HDR] _Color4   ("Color 4",               Color) = (0.10, 1.80, 0.60, 1)
        _ColorSpeed     ("Colour Animation Speed", Range(0, 2))   = 0.25

        // ── Shape (matches DigitalGlitchGrid interface) ───────────────────────
        [Header(Shape)]
        _EdgeSoftness   ("Edge Softness",              Range(0.01,0.40)) = 0.15
        _ShapeRoundness ("Roundness  0=box  1=oval",   Range(0,1))       = 1.0
        _ShapeW         ("Half Width",                 Range(0.05,0.5))  = 0.44
        _ShapeH         ("Half Height",                Range(0.05,0.5))  = 0.44
        _ShapeSkewX     ("Skew X  (parallelogram)",    Range(-0.7,0.7))  = 0
        _ShapeTaper     ("Taper   (trapezoid)",        Range(-1,1))      = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent+1"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "EtherealFluidTrail"
            Blend  SrcAlpha One     // additive — streaks glow on top of the scene
            ZWrite Off
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Constant buffer ───────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _SpawnProgress;
                float  _BlowFade;
                float  _Opacity;
                float  _TimeOffset;
                float  _IriOffset;
                float4 _CoreColor;

                float  _StreakLength;
                float  _DiffusionIntensity;
                float  _FlickerSpeed;
                float  _DripSpeed;
                float  _DripDensity;
                float  _GlowWidth;
                float  _ColorBleed;

                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float  _ColorSpeed;

                float  _EdgeSoftness;
                float  _ShapeRoundness;
                float  _ShapeW;
                float  _ShapeH;
                float  _ShapeSkewX;
                float  _ShapeTaper;
            CBUFFER_END

            // ── IO structs ────────────────────────────────────────────────────
            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // ════════════════════════════════════════════════════════════════
            //  NOISE LIBRARY
            // ════════════════════════════════════════════════════════════════

            float  h11(float  n) { return frac(sin(n)                            * 43758.5453f); }
            float  h21(float2 p) { return frac(sin(dot(p, float2(127.1f,311.7f)))* 43758.5453f); }
            float2 h22(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1f,311.7f)),
                                       dot(p, float2(269.5f,183.3f)))) * 43758.5453f);
            }

            // Quintic C² smooth gradient noise  ≈ [-0.875, +0.875]
            float gradNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
                float2 g00 = h22(i)               * 2.0f - 1.0f;
                float2 g10 = h22(i + float2(1,0)) * 2.0f - 1.0f;
                float2 g01 = h22(i + float2(0,1)) * 2.0f - 1.0f;
                float2 g11 = h22(i + float2(1,1)) * 2.0f - 1.0f;
                return lerp(
                    lerp(dot(g00, f),               dot(g10, f - float2(1,0)), u.x),
                    lerp(dot(g01, f - float2(0,1)), dot(g11, f - float2(1,1)), u.x),
                    u.y);
            }

            // 3-octave FBM — good detail / cost balance
            float fbm(float2 p)
            {
                float v = 0.0f, a = 0.5f;
                v += a * gradNoise(p); p = p * 2.0f + float2(5.31f, 1.73f); a *= 0.5f;
                v += a * gradNoise(p); p = p * 2.0f + float2(2.13f, 8.41f); a *= 0.5f;
                v += a * gradNoise(p);
                return v;   // ≈ [-0.875, +0.875]
            }

            // ════════════════════════════════════════════════════════════════
            //  FOUR-STOP CYCLIC COLOUR PALETTE
            // ════════════════════════════════════════════════════════════════
            float3 fluidPalette(float t)
            {
                t = frac(t + _IriOffset);
                float s;
                if (t < 0.25f) { s = t * 4.0f;         s = s*s*(3.0f-2.0f*s); return lerp(_Color1.rgb, _Color2.rgb, s); }
                if (t < 0.50f) { s = (t-0.25f) * 4.0f; s = s*s*(3.0f-2.0f*s); return lerp(_Color2.rgb, _Color3.rgb, s); }
                if (t < 0.75f) { s = (t-0.50f) * 4.0f; s = s*s*(3.0f-2.0f*s); return lerp(_Color3.rgb, _Color4.rgb, s); }
                s = (t-0.75f) * 4.0f; s = s*s*(3.0f-2.0f*s); return lerp(_Color4.rgb, _Color1.rgb, s);
            }

            // ════════════════════════════════════════════════════════════════
            //  DRIP COLUMN BRIGHTNESS
            //
            //  Computes the luminance contribution of ONE drip column at the
            //  current fragment position.  O(1) — no loops.
            //
            //  cellId  — integer column index (hash seed for randomness)
            //  uv      — domain-warped fragment UV
            //  density — number of columns in [0,1] width
            //  tp      — animated time phase (t * _DripSpeed + per-layer offset)
            //
            //  Return value is in [0, ~1.7].  Caller multiplies by its HDR
            //  colour and emission scale.
            // ════════════════════════════════════════════════════════════════
            float DripBright(float cellId, float2 uv, float density, float tp)
            {
                // Per-drip randoms
                float rA = h11(cellId * 7.31f);
                float rB = h11(cellId * 3.17f + 4.5f);
                float rC = h11(cellId * 5.71f + 9.2f);

                // ── Horizontal Gaussian ───────────────────────────────────
                // Drip X in absolute UV space (positions vary per column).
                float dripUVX = (cellId + 0.20f + rA * 0.60f) / density;
                float dx      = uv.x - dripUVX;
                float xSig    = max(_GlowWidth * 0.022f, 0.006f);   // UV-space sigma
                float xGauss  = exp(-dx * dx / (2.0f * xSig * xSig));

                // ── Vertical: drip falls top→bottom (UV.y: 1=top, 0=bottom) ─
                // Phase staggers drips so columns don't all reach bottom at once.
                float phase = rA * 0.6f + rB * 0.3f + rC * 0.1f;
                float dripY = 1.0f - frac(tp + phase);   // 1 (top) → 0 (bottom)

                float dy     = uv.y - dripY;             // + = above head, - = below
                float decay  = max(_StreakLength, 1.0f);

                // Tight bright head at the drip tip
                float head      = exp(-dy * dy * 280.0f);
                // Soft exponential trail stretching upward (the "drip history")
                float trailUp   = dy > 0.0f ? exp(-dy * decay)          * 0.90f : 0.0f;
                // Tiny cap below the head (light spills a little beyond the tip)
                float trailDown = dy < 0.0f ? exp( dy * decay * 5.0f)   * 0.18f : 0.0f;

                return (head + trailUp + trailDown) * xGauss;
            }

            // ── Vertex ────────────────────────────────────────────────────────
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.posHCS = TransformObjectToHClip(v.posOS.xyz);
                o.uv     = v.uv;
                return o;
            }

            // ── Fragment ──────────────────────────────────────────────────────
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float  t  = _Time.y + _TimeOffset;
                float2 fc = uv - 0.5f;
                float  r  = length(fc);

                // ════════════════════════════════════════════════════════════
                //  1. SHAPE SDF  (identical logic to DigitalGlitchGrid)
                //
                //  Supports oval, box, parallelogram, and trapezoid silhouettes.
                //  The SDF is negative inside, positive outside; smoothstep gives
                //  a soft edge ramp of width _EdgeSoftness on each side.
                // ════════════════════════════════════════════════════════════
                float2 sfc    = float2(fc.x - fc.y * _ShapeSkewX, fc.y);
                float  hw     = max(_ShapeW * (1.0f - sfc.y * _ShapeTaper), 0.02f);
                float2 dBox   = abs(sfc) - float2(hw, _ShapeH);
                float  boxSDF = length(max(dBox, 0.0f)) + min(max(dBox.x, dBox.y), 0.0f);
                float  cirSDF = r - 0.46f;
                float  shSDF  = lerp(boxSDF, cirSDF, _ShapeRoundness);
                float  radial = 1.0f - smoothstep(-_EdgeSoftness, _EdgeSoftness, shSDF);

                // ════════════════════════════════════════════════════════════
                //  2. SPAWN / DESPAWN
                //
                //  Cells are vertical strips (taller than wide) that match the
                //  drip-streak visual language.  Each strip has its own random
                //  birth and death threshold — strips appear in a random order
                //  during spawn, and vanish in a different random order during
                //  despawn.  A small soft-width prevents hard on/off snapping.
                // ════════════════════════════════════════════════════════════
                float  dens   = max(_DripDensity, 4.0f);
                // Cell grid: dens columns × (dens*0.25) rows → tall portrait cells
                float2 sCell  = floor(uv * float2(dens, dens * 0.25f));
                float  cBirth = h21(sCell);
                float  cDeath = h21(sCell * 2.17f + 5.31f);
                float  softW  = 0.055f;
                float  sp     = _SpawnProgress;
                float  reveal;
                if      (sp < 0.25f) { float pp = sp/0.25f;       reveal = smoothstep(cBirth-softW, cBirth+softW, pp); }
                else if (sp < 0.75f) { reveal = 1.0f; }
                else                 { float dp = (sp-0.75f)/0.25f; reveal = 1.0f - smoothstep(cDeath-softW, cDeath+softW, dp); }

                float shapeMask = saturate(reveal * radial);
                if (shapeMask < 0.002f) discard;

                // ════════════════════════════════════════════════════════════
                //  3. DOMAIN WARP
                //
                //  A two-level FBM warp displaces the UV that all drip and
                //  nebula layers sample.  This makes streaks sway and bend
                //  rather than falling perfectly straight — organic motion.
                //  Strength scales with _DiffusionIntensity.
                // ════════════════════════════════════════════════════════════
                float  warpAmt = _DiffusionIntensity * 0.065f;
                float2 wQ      = float2(
                    fbm(uv * 1.25f + float2( t * 0.065f, 0.0f)),
                    fbm(uv * 1.25f + float2( 0.0f,  t * 0.050f + 3.73f)));
                float2 wUV     = uv + wQ * warpAmt;   // warped UV used by all layers

                // ════════════════════════════════════════════════════════════
                //  4. AURORA NEBULA  (anisotropic FBM background glow)
                //
                //  Sampling with high vertical scale (yScale >> 1) stretches
                //  the noise into wide horizontal aurora bands.  Time-based Y
                //  drift makes them flow downward like aurora curtains.
                //
                //  Colour bleed: R and B channels are sampled at slightly
                //  shifted UV positions, creating a watercolour-spread effect
                //  where one colour bleeds into the halo of the next.
                // ════════════════════════════════════════════════════════════
                float  yScale  = 2.5f + _StreakLength * 0.38f;
                float2 nebUV   = float2(wUV.x * 1.9f,
                                        wUV.y * yScale - t * _FlickerSpeed * 0.24f);

                // Threshold at 0.28 so the lower-value noise regions are transparent
                float nebN    = fbm(nebUV) * 0.5f + 0.5f;
                float nebula  = pow(saturate((nebN - 0.28f) / 0.72f), 1.6f);

                // Per-channel offsets for colour bleed
                float bl   = _ColorBleed * (1.4f + _DiffusionIntensity);
                float nebR = pow(saturate((fbm(nebUV + float2( bl*3.8f,-bl*2.1f))*0.5f+0.5f-0.28f)/0.72f), 1.6f);
                float nebG = nebula;
                float nebB = pow(saturate((fbm(nebUV + float2(-bl*3.8f, bl*1.6f))*0.5f+0.5f-0.28f)/0.72f), 1.6f);
                float3 nebBled = float3(nebR, nebG, nebB);

                float  palTNeb  = frac(wUV.x * 0.56f + wUV.y * 0.03f + t * _ColorSpeed * 0.034f);
                float3 nebColor = fluidPalette(palTNeb) * nebBled;

                // ════════════════════════════════════════════════════════════
                //  5. DRIP STREAKS  —  three density layers
                //
                //  Using three layers at different densities / speeds gives
                //  rich visual depth: fine fast drips in front, wide slow
                //  glowing ones behind.  Each layer uses a different column
                //  grid and UV offset so they never align.
                //
                //  DripBright() returns brightness for the single drip in the
                //  column that owns this pixel (O(1), no per-pixel loops).
                // ════════════════════════════════════════════════════════════

                // ─ Fine layer: many columns, fastest falling
                float  denF = dens;
                float  tpF  = t * _DripSpeed;
                float  cidF = floor(wUV.x * denF);
                float  bF   = DripBright(cidF, wUV, denF, tpF);
                float3 colF = fluidPalette(frac(cidF / denF + t * _ColorSpeed * 0.062f));

                // ─ Medium layer: half the columns, slower, slight X offset
                float  denM = max(denF * 0.50f, 2.0f);
                float  tpM  = t * _DripSpeed * 0.63f + 5.31f;
                float2 wUVM = wUV + float2(0.173f, 0.0f);
                float  cidM = floor(wUVM.x * denM);
                float  bM   = DripBright(cidM, wUVM, denM, tpM);
                float3 colM = fluidPalette(frac(cidM / denM + 0.385f + t * _ColorSpeed * 0.041f));

                // ─ Coarse layer: quarter columns, slowest, widest glow base
                float  denC = max(denF * 0.25f, 1.5f);
                float  tpC  = t * _DripSpeed * 0.37f + 11.73f;
                float2 wUVC = wUV - float2(0.091f, 0.0f);
                float  cidC = floor(wUVC.x * denC);
                float  bC   = DripBright(cidC, wUVC, denC, tpC);
                float3 colC = fluidPalette(frac(cidC / denC + 0.712f + t * _ColorSpeed * 0.028f));

                // Weighted composite: fine layer is the hero
                float3 dripCol = colF * bF * 2.6f
                               + colM * bM * 1.5f
                               + colC * bC * 0.9f;
                float  dripLum = saturate(bF * 0.62f + bM * 0.27f + bC * 0.11f);

                // ════════════════════════════════════════════════════════════
                //  6. SOFT CORE DIFFUSION GLOW
                //
                //  A broad Gaussian from the quad centre simulates light
                //  accumulating in a hazy cloud — the "long exposure" feel.
                //  Controlled by _DiffusionIntensity: 0 = tight streaks only,
                //  1 = surrounded by a thick luminous haze.
                // ════════════════════════════════════════════════════════════
                float  diffSig  = max(0.10f, _DiffusionIntensity * 0.30f);
                float  diffGlow = exp(-r * r / (diffSig * diffSig));
                float3 diffCol  = _CoreColor.rgb * diffGlow * _DiffusionIntensity * 0.40f;

                // ════════════════════════════════════════════════════════════
                //  7. COMPOSITE  +  ALPHA
                //
                //  Blend the three layers:
                //    • Drip streaks   — primary visual element
                //    • Aurora nebula  — background diffuse glow + colour source
                //    • Core diffusion — hazy cloud centre
                //
                //  Lock to CoreColor so syncTouchColors works correctly:
                //  lerp leaves the colour mostly intact but ensures the
                //  overall hue follows whatever the C# code sets on the mat.
                // ════════════════════════════════════════════════════════════
                float  nebW = 0.18f + _DiffusionIntensity * 0.38f;
                float3 col  = dripCol
                            + nebColor * nebW
                            + diffCol;

                // CoreColor tint — 40 % hard lock, 60 % free palette
                col *= (_CoreColor.rgb * 0.40f + 0.60f);

                // Edge vignette: fade near the silhouette boundary
                float vig   = 1.0f - smoothstep(0.26f, 0.47f, r);

                // Alpha: drips are the dominant opacity source
                float alpha = saturate(
                      dripLum                              * 0.76f
                    + nebula  * nebW                       * 0.21f
                    + diffGlow * _DiffusionIntensity       * 0.13f)
                    * vig * shapeMask * _Opacity * _BlowFade;

                // Gamma lift: raises midtones so the additive blend glows more
                alpha = pow(saturate(alpha), 0.60f);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
