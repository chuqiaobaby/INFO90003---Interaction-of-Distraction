// ══════════════════════════════════════════════════════════════════════════════
//  DigitalGlitchGrid.shader  ─  URP  (no Opaque Texture needed)
//
//  Visual concept: a floating grid of emissive light-dots that independently
//  flicker, drift, and colour-shift — with digital glitch band-shifts and
//  anamorphic horizontal lens-streak flares.
//
//  Lifecycle properties match LiquidGlassAnomaly so the same C# code drives
//  spawn / hold / despawn / blow-fade on both effects:
//    _SpawnProgress  0→0.25 mosaic cells pop IN  one-by-one
//                    0.25→0.75 hold (all cells visible, dots animate freely)
//                    0.75→1.00 mosaic cells pop OUT one-by-one
//    _BlowFade       1→0 during blow routine (overall fade-out override)
//    _Opacity        peak alpha set at spawn
//    _TimeOffset     per-instance offset keeps colours out-of-sync
//    _IriOffset      per-instance hue base offset
//    _CoreColor      [HDR] primary colour (supports syncTouchColors)
//
//  Blend mode: additive (SrcAlpha One) — dots glow on top of the scene.
// ══════════════════════════════════════════════════════════════════════════════

Shader "Custom/DigitalGlitchGrid"
{
    Properties
    {
        // ── Lifecycle (shared IDs with LiquidGlassAnomaly) ────────────────────
        _SpawnProgress  ("Spawn Progress",          Range(0,1))      = 0
        _BlowFade       ("Blow Fade",               Range(0,1))      = 1
        _Opacity        ("Opacity",                 Range(0,1))      = 0.95
        _TimeOffset     ("Time Offset",             Float)           = 0
        _IriOffset      ("Iridescence Offset",      Range(0,1))      = 0
        [HDR] _CoreColor("Core Color",              Color)           = (0.1,2.0,3.5,1)

        // ── Grid structure ────────────────────────────────────────────────────
        [Header(Grid)]
        _GridSize       ("Grid Density (dots/side)", Range(6,80))    = 20
        _DotRadius      ("Dot Radius",               Range(0.05,0.48))= 0.34
        _DotDrift       ("Dot Drift Amount",         Range(0,0.4))   = 0.10
        _DotDriftSpeed  ("Dot Drift Speed",          Range(0,5))     = 0.9

        // ── Glitch ────────────────────────────────────────────────────────────
        [Header(Glitch)]
        _FlickerSpeed   ("Flicker Speed",            Range(0,30))    = 9
        _GlitchAmt      ("Glitch Shift Amount",      Range(0,0.15))  = 0.03
        _GlitchDensity  ("Glitch Band Density",      Range(1,20))    = 6
        _ScanStrength   ("Scanline Strength",        Range(0,1))     = 0.18

        // ── Color ─────────────────────────────────────────────────────────────
        [Header(Color)]
        [HDR] _GlowColor("Glow / Flare Color",      Color)          = (0.0,1.0,2.5,1)
        _IriIntensity   ("Iridescence Intensity",    Range(0,5))     = 2.5
        _IriSpeed       ("Iridescence Speed",        Range(0,3))     = 0.28
        _CoreEmission   ("Core Emission Power",      Range(1,20))    = 7

        // ── Anamorphic flare ──────────────────────────────────────────────────
        [Header(Anamorphic Flare)]
        _FlareIntensity ("Flare Intensity",          Range(0,12))    = 3.5
        _FlareVWidth    ("Flare Vertical Width",     Range(0.001,0.3))= 0.022
        _FlareHLength   ("Flare Horizontal Length",  Range(0.05,3))  = 1.1
        _FlarePower     ("Flare Vertical Falloff",   Range(1,16))    = 5.0

        // ── Shape ─────────────────────────────────────────────────────────────
        [Header(Shape)]
        _EdgeSoftness   ("Edge Softness",            Range(0.01,0.3))= 0.10
        _ShapeRoundness ("Roundness  0=box  1=oval", Range(0,1))     = 1.0
        _ShapeW         ("Half Width",               Range(0.05,0.5)) = 0.44
        _ShapeH         ("Half Height",              Range(0.05,0.5)) = 0.44
        _ShapeSkewX     ("Skew X  parallelogram",    Range(-0.7,0.7)) = 0.0
        _ShapeTaper     ("Taper   trapezoid",        Range(-1,1))     = 0.0


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
            Name "DigitalGlitchGrid"
            Blend  SrcAlpha One      // additive — dots glow on top of whatever is behind
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

                float  _GridSize;
                float  _DotRadius;
                float  _DotDrift;
                float  _DotDriftSpeed;

                float  _FlickerSpeed;
                float  _GlitchAmt;
                float  _GlitchDensity;
                float  _ScanStrength;

                float4 _GlowColor;
                float  _IriIntensity;
                float  _IriSpeed;
                float  _CoreEmission;

                float  _FlareIntensity;
                float  _FlareVWidth;
                float  _FlareHLength;
                float  _FlarePower;

                float  _EdgeSoftness;
                float  _ShapeRoundness;
                float  _ShapeW;
                float  _ShapeH;
                float  _ShapeSkewX;
                float  _ShapeTaper;
            CBUFFER_END

            // ── Structs ───────────────────────────────────────────────────────
            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // ── Hash / noise utils ────────────────────────────────────────────
            float  h11(float  n) { return frac(sin(n)                          * 43758.5453); }
            float  h21(float2 p) { return frac(sin(dot(p, float2(127.1,311.7)))* 43758.5453); }
            float2 h22(float2 p) {
                return frac(sin(float2(dot(p,float2(127.1,311.7)),
                                       dot(p,float2(269.5,183.3)))) * 43758.5453);
            }

            // HSV → RGB  (standard Iñigo Quilez formula)
            float3 hsv2rgb(float h, float s, float v) {
                float4 K = float4(1, 2.0/3.0, 1.0/3.0, 3);
                float3 p = abs(frac(h + K.xyz) * 6 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            // ── Vertex ────────────────────────────────────────────────────────
            Varyings vert(Attributes v) {
                Varyings o;
                o.posHCS = TransformObjectToHClip(v.posOS.xyz);
                o.uv     = v.uv;
                return o;
            }

            // ── Fragment ──────────────────────────────────────────────────────
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv  = i.uv;
                float  t   = _Time.y + _TimeOffset;
                float2 fc  = uv - 0.5;          // offset from quad centre
                float  r   = length(fc);        // radial distance

                // ════════════════════════════════════════════════════════════
                //  1. SHAPE BOUNDARY  (SDF — circle / rect / parallelogram / trapezoid)
                //
                //  fc = uv - 0.5  is the fragment offset from quad centre.
                //
                //  _ShapeSkewX  ≠ 0  →  shear x by y  →  parallelogram
                //  _ShapeTaper  ≠ 0  →  width varies with y  →  trapezoid
                //  _ShapeRoundness   →  0 = sharp box,  1 = oval / circle
                //
                //  The SDF is negative inside the shape, positive outside,
                //  zero on the boundary.  smoothstep turns it into a [0,1] mask.
                // ════════════════════════════════════════════════════════════

                // Step 1 — parallelogram shear
                float2 sfc  = float2(fc.x - fc.y * _ShapeSkewX, fc.y);

                // Step 2 — trapezoid: half-width widens / narrows along y
                //   _ShapeTaper > 0 → wider at bottom  (normal trapezoid)
                //   _ShapeTaper < 0 → wider at top     (inverted trapezoid)
                float  hw   = max(_ShapeW * (1.0 - sfc.y * _ShapeTaper), 0.02);

                // Step 3 — box SDF in sheared space
                float2 dBox  = abs(sfc) - float2(hw, _ShapeH);
                float  boxSDF = length(max(dBox, 0.0)) + min(max(dBox.x, dBox.y), 0.0);

                // Step 4 — circle SDF (original r, un-sheared)
                float  cirSDF = r - 0.46;

                // Step 5 — blend and apply soft edge
                float  shapeSDF = lerp(boxSDF, cirSDF, _ShapeRoundness);
                float  radial   = 1.0 - smoothstep(-_EdgeSoftness, _EdgeSoftness, shapeSDF);

                // ════════════════════════════════════════════════════════════
                //  2. SPAWN / DESPAWN  —  mosaic cell-by-cell reveal
                //
                //  Each grid cell has its own random birth threshold (0–1) and
                //  a separate random death threshold, derived from cell position.
                //  During the spawn phase, cells pop IN  as progress sweeps 0→1:
                //    cells with a low birth-hash appear first (random order).
                //  During the despawn phase, cells pop OUT as progress sweeps 0→1:
                //    cells with a low death-hash vanish first (independent order).
                //
                //  The result is a digital mosaic / scramble look — no radial sweep.
                //  A small soft-width (softW) prevents a completely hard on/off snap
                //  so each cell has a brief pixel-flash rather than instant cut.
                // ════════════════════════════════════════════════════════════
                float sp = _SpawnProgress;

                // Per-cell random thresholds  (one hash for birth, independent one for death)
                float2 revealCell = floor(uv * _GridSize);
                float  cellBirth  = h21(revealCell);                // 0–1, birth order
                float  cellDeath  = h21(revealCell * 2.17 + 5.31);  // 0–1, death order
                float  softW      = 0.055;   // width of the per-cell fade edge

                float reveal;
                if (sp < 0.25)
                {
                    // Cells appear in random order: low-birth cells come first.
                    // pp goes 0→1 across the spawn phase.
                    float pp = sp / 0.25;
                    reveal = smoothstep(cellBirth - softW, cellBirth + softW, pp);
                }
                else if (sp < 0.75)
                {
                    reveal = 1.0;
                }
                else
                {
                    // Cells vanish in random order: low-death cells go first.
                    // dp goes 0→1 across the despawn phase.
                    float dp = (sp - 0.75) / 0.25;
                    reveal = 1.0 - smoothstep(cellDeath - softW, cellDeath + softW, dp);
                }

                float shapeMask = saturate(reveal * radial);
                if (shapeMask < 0.002) discard;     // early-out for empty pixels

                // ════════════════════════════════════════════════════════════
                //  3. TOUCH / CENTRE INFLUENCE
                //     The quad is positioned at the touch point, so the centre
                //     of UV space (0.5,0.5) IS the touch position.
                // ════════════════════════════════════════════════════════════
                float touchMask = exp(-r * r * 7.0);   // strong centre, soft edge

                // ════════════════════════════════════════════════════════════
                //  4. GLITCH  — random horizontal band-shifts
                //     Bands appear sporadically, stronger near centre
                // ════════════════════════════════════════════════════════════
                float bandRow  = floor(uv.y * _GlitchDensity * 2.8);
                float bandTime = floor(t * _FlickerSpeed * 0.30);
                float bandRng  = h11(bandRow * 19.3 + bandTime);
                float shiftX   = step(0.79, bandRng)
                               * (h11(bandRow + bandTime * 6.1) - 0.5)
                               * _GlitchAmt * (0.4 + touchMask * 0.6);

                float2 sUV = float2(uv.x + shiftX, uv.y);   // glitch-shifted UV

                // ════════════════════════════════════════════════════════════
                //  5. GRID CELL  — per-cell random seeds
                // ════════════════════════════════════════════════════════════
                float2 gUV    = sUV * _GridSize;
                float2 cellID = floor(gUV);
                float2 cUV    = frac(gUV);          // 0–1 inside cell

                float cr0 = h21(cellID);             // cell random A
                float cr1 = h21(cellID + 5.77);      // cell random B
                float cr2 = h21(cellID * 3.1 + 13.0);// cell random C

                // ════════════════════════════════════════════════════════════
                //  6. PER-DOT FLICKER  ← 闪
                //     Two-layer: slow envelope × sharp fast pulse
                //     Each dot has its own independent frequency & phase.
                // ════════════════════════════════════════════════════════════
                float slowEnv  = 0.5 + 0.5 * sin(t * _FlickerSpeed * (0.18 + cr0 * 0.55)
                                                  + cr0 * 6.2832);
                float fastPulse= 0.5 + 0.5 * sin(t * _FlickerSpeed * (1.1  + cr1 * 1.6)
                                                  + cr1 * 6.2832);
                fastPulse      = pow(fastPulse, 2.2);   // sharpen: off most of time, bright burst
                float flicker  = slowEnv * fastPulse;

                // Hard blink: dot disappears entirely on a slow independent cycle
                float blinkCyc = 0.5 + 0.5 * sin(t * (0.12 + cr2 * 0.28) + cr2 * 6.2832);
                float blinkOn  = step(0.22, blinkCyc);   // ~78 % of time on

                // ════════════════════════════════════════════════════════════
                //  7. DOT DRIFT  ← 动
                //     Each dot drifts smoothly within its cell using Lissajous-
                //     like sine curves with per-cell random frequencies.
                // ════════════════════════════════════════════════════════════
                float2 drift = float2(
                    sin(t * _DotDriftSpeed * (0.45 + cr0 * 0.75) + cr0 * 6.28) * _DotDrift,
                    cos(t * _DotDriftSpeed * (0.38 + cr1 * 0.65) + cr1 * 6.28) * _DotDrift
                );
                float2 dotCentre = float2(0.5, 0.5) + drift;

                // Dot radius breathes gently with flicker
                float dotR = _DotRadius * (0.52 + 0.48 * flicker);
                dotR       = clamp(dotR, 0.04, 0.47);

                float dotDst = length(cUV - dotCentre);
                float dotVal = smoothstep(dotR, dotR * 0.45, dotDst) * blinkOn;

                // ════════════════════════════════════════════════════════════
                //  8. IRIDESCENT COLOUR  ← 变化
                //     Hue = per-cell base + spatial gradient + time drift.
                //     Saturation & value also animate with flicker.
                // ════════════════════════════════════════════════════════════
                float hue = frac(
                    cr0 * 0.85                    // per-cell base hue
                  + r   * 0.55                    // radial gradient (centre→edge)
                  + uv.x * 0.22 - uv.y * 0.10    // diagonal colour sweep
                  + t   * _IriSpeed               // continuous time drift
                  + _IriOffset                    // per-instance phase
                  + flicker * 0.06               // subtle flicker hue-wobble
                );
                float sat    = 0.72 + 0.28 * flicker;
                float val    = 0.85 + 0.35 * flicker;
                float3 iriRGB = hsv2rgb(hue, sat, val);

                // Blend core colour with iridescence
                float iriFrac = saturate(_IriIntensity * 0.25);
                float3 dotCol = lerp(_CoreColor.rgb,
                                     iriRGB * _CoreColor.rgb * 0.35 + iriRGB * 0.65,
                                     iriFrac);

                // Brighter near touch centre
                dotCol *= (1.0 + touchMask * 2.0) * (0.55 + 0.45 * flicker);

                // ════════════════════════════════════════════════════════════
                //  9. SCANLINES  — subtle horizontal modulation
                // ════════════════════════════════════════════════════════════
                float scan    = 0.5 + 0.5 * sin(uv.y * _GridSize * PI * 1.4);
                float scanMod = lerp(1.0, pow(max(scan, 0.001), 0.55), _ScanStrength);

                // ════════════════════════════════════════════════════════════
                //  10. ANAMORPHIC HORIZONTAL FLARE
                //      Each grid ROW is treated as a potential flare source.
                //      Per-row energy is computed analytically (no loop needed):
                //      the average dot brightness of a row is approximated from
                //      row-hash + time so each row independently flickers.
                //      A thin vertical profile turns each bright row into a
                //      horizontal streak that extends edge-to-edge.
                // ════════════════════════════════════════════════════════════

                // Row identity and independent energy
                float rowY      = floor(sUV.y * _GridSize);
                float rowRng0   = h11(rowY * 41.7);
                float rowRng1   = h11(rowY * 17.3 + 99.1);
                float rowEnergy = 0.5 + 0.5 * sin(t * _FlickerSpeed * (0.15 + rowRng0 * 0.4)
                                                   + rowRng0 * 6.2832);
                rowEnergy       = pow(rowEnergy, 2.5);          // sparse — most rows dark
                rowEnergy      *= (0.4 + rowRng1 * 0.6);        // row-specific brightness
                rowEnergy      *= touchMask;

                // Distance from nearest row centre-line
                float nearRowCentreY = (rowY + 0.5) / _GridSize;
                float flareVDst      = abs(uv.y - nearRowCentreY);
                float flareYMask     = exp(-pow(flareVDst / max(_FlareVWidth, 0.0001),
                                               _FlarePower));

                // Horizontal taper: stronger towards quad centre, fades at edges
                float flareHMask = exp(-abs(uv.x - 0.5) / max(_FlareHLength * 0.5, 0.001));

                float  flare    = rowEnergy * flareYMask * flareHMask * _FlareIntensity;
                float3 flareCol = _GlowColor.rgb * flare;

                // ════════════════════════════════════════════════════════════
                //  11. COMPOSITE
                // ════════════════════════════════════════════════════════════
                float  dotEmit  = dotVal * scanMod;

                // Emission: raise dot brightness to _CoreEmission power for HDR glow
                float3 col = dotCol * pow(max(dotEmit, 0.001), 1.0 / max(_CoreEmission, 0.1))
                                     * _CoreEmission
                           + flareCol;

                // Alpha: dots + subtle flare contribution
                float alpha = saturate(
                      dotVal   * (0.45 + 0.55 * flicker) * scanMod
                    + flare    * 0.06
                );
                alpha  = pow(alpha, 0.75);                  // slight gamma lift
                alpha *= shapeMask * _Opacity * _BlowFade;

                // Additive blending: output premultiplied so SrcAlpha·One works correctly
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
