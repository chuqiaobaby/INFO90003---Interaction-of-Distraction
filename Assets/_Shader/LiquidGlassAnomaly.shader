// ══════════════════════════════════════════════════════════════════════════════
//  LiquidGlassAnomaly.shader  —  URP  (Opaque Texture required)
//
//  SETUP:
//    1. URP Renderer Asset → enable "Opaque Texture"
//    2. Assign to a mesh placed AFTER opaque geometry
//
//  FEATURES:
//    • Anisotropic brushstroke noise — 3 stretched gradient layers create
//      flowing paint / liquid-metal stroke patterns
//    • High-contrast harsh smoothstep alpha — fully opaque strokes + fully
//      transparent cracks (no uniform soft opacity)
//    • 4 inspector-exposed [HDR] colors blend across the fluid surface
//    • 3 concentric SDF rings distorted by domain warp (holographic / glitchy)
//    • Chromatic aberration exaggerated specifically on high-contrast stroke edges
//    • Metallic per-channel Schlick Fresnel + thin-film interference shimmer
//    • Domain-warped FBM refraction of background (screen-space, URP opaque texture)
//    • No core emission — blob fades cleanly at the centre
// ══════════════════════════════════════════════════════════════════════════════

Shader "Custom/LiquidGlassAnomaly"
{
    Properties
    {
        [Header(Distortion and Flow)]
        _DistortionStrength ("Distortion Strength",   Range(0.0, 0.40)) = 0.060
        _EdgePullStrength   ("Edge Pull Strength",    Range(0.0, 0.40)) = 0.12
        _NoiseScale         ("Noise Scale",           Range(0.5,  8.0)) = 2.8
        _FlowSpeed          ("Flow Speed",            Range(0.0,  5.0)) = 0.75
        _RadialBias         ("Radial Outward Bias",   Range(0.0,  3.0)) = 1.2
        _WarpAmount         ("Domain Warp Amount",    Range(0.0, 12.0)) = 5.5

        [Space(6)][Header(Global Size)]
        _TotalSize          ("Global Effect Size",                Range(0.1,  3.0)) = 1.0
        _EdgeSoftness       ("Edge Softness (0 = hard, 1 = full gradient)", Range(0.0, 1.0)) = 0.4

        [Space(6)][Header(Brushstroke Opacity)]
        _StrokeThreshold    ("Stroke Threshold",                  Range(0.0,  1.0)) = 0.52
        _StrokeEdge         ("Stroke Edge (smaller = sharper)",   Range(0.01, 0.3)) = 0.06
        _StrokeStretch      ("Stroke Anisotropy Stretch",         Range(1.0, 12.0)) = 6.5
        _GapOpacity         ("Gap Refraction Opacity",            Range(0.0,  1.0)) = 0.85
        _SpinSpeed          ("Swirl Speed (rad/s)",                Range(0.0,  4.0)) = 0.6
        _SwirlStrength      ("Swirl Strength",                   Range(0.0,  1.0)) = 0.25
        _SwirlTightness     ("Swirl Tightness (higher = tighter vortex)", Range(1.0, 20.0)) = 6.0

        [Space(6)][Header(Custom Fluid Colors)]
        [HDR] _Color1       ("Color 1",  Color) = (0.20, 0.00, 0.80, 1.0)
        [HDR] _Color2       ("Color 2",  Color) = (0.00, 0.90, 1.00, 1.0)
        [HDR] _Color3       ("Color 3",  Color) = (1.00, 0.00, 0.50, 1.0)
        [HDR] _Color4       ("Color 4",  Color) = (0.10, 0.30, 1.00, 1.0)
        _ColorSpeed         ("Color Animation Speed",      Range(0.0, 3.0)) = 0.40
        _IriOffset          ("Palette Offset (per instance)", Range(0.0, 1.0)) = 0.0

        [Space(6)][Header(SDF Holographic Rings)]
        _RingRadius         ("Ring Radius",          Range(0.05, 0.50)) = 0.18
        _RingThickness      ("Ring Thickness",       Range(0.001, 0.05)) = 0.008
        _RingEmission       ("Ring Emission (HDR)",  Range(0.0,  20.0)) = 7.0
        _RingWarpStrength   ("Ring Warp Distortion", Range(0.0,   0.5)) = 0.15
        _RingSpacing        ("Ring Spacing",         Range(1.1,   3.0)) = 1.55

        [Space(6)][Header(Chromatic Aberration)]
        _ChromaStrength     ("Chroma Strength",   Range(0.0,  0.05)) = 0.015
        _ChromaEdgePow      ("Chroma Edge Power", Range(0.5,   4.0)) = 1.5

        [Space(6)][Header(Metallic Fresnel)]
        _FresnelPower       ("Fresnel Power",          Range(0.5, 8.0)) = 3.0
        _MetalF0            ("Metal Base Reflectance", Range(0.0, 1.0)) = 0.85
        _ThinFilmFreq       ("Thin Film Frequency",    Range(0.0,20.0)) = 6.0
        _FresnelIntensity   ("Fresnel Intensity",      Range(0.0, 5.0)) = 2.2

        [Space(6)][Header(Per Instance Variation  set from C#)]
        _BlobScale          ("Blob Edge Scale (lower = larger blobs)", Range(0.2, 3.0)) = 0.75
        _ShapeIrregularity  ("Blob Deformation Amplitude",             Range(0.0, 0.65)) = 0.35
        _ShapeOffsetX       ("Shape Noise Seed X",                     Float) = 0.0
        _ShapeOffsetY       ("Shape Noise Seed Y",                     Float) = 0.0
        _RotationOffset     ("Instance Stroke Rotation (radians)",     Float) = 0.0
        _TimeOffset         ("Instance Time Offset",                   Float) = 0.0
        _SpawnProgress      ("Spawn / Despawn Progress",               Range(0.0, 1.0)) = 0.5
        _BlowFade           ("Blow Fade Multiplier",                   Range(0.0, 1.0)) = 1.0
        _DissolveProgress   ("Dissolve-to-Sparkles Progress",          Range(0.0, 1.0)) = 0.0
        _SparkleScale       ("Sparkle Cell Scale",                     Range(10,  120))  = 55
        _SparkleGlow        ("Sparkle Glow Intensity",                 Range(0.0,  8.0)) = 4.0

        [Space(6)][Header(Diagnostic)]
        [Toggle] _TestMode  ("Test Mode (solid red)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+1"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LiquidGlassAnomaly"

            Blend  SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest  Always
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // ── Constant buffer ───────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _TotalSize;
                float  _EdgeSoftness;
                float  _DistortionStrength;
                float  _EdgePullStrength;
                float  _NoiseScale;
                float  _FlowSpeed;
                float  _RadialBias;
                float  _WarpAmount;
                float  _StrokeThreshold;
                float  _StrokeEdge;
                float  _StrokeStretch;
                float  _GapOpacity;
                float  _SpinSpeed;
                float  _SwirlStrength;
                float  _SwirlTightness;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float  _ColorSpeed;
                float  _IriOffset;
                float  _RingRadius;
                float  _RingThickness;
                float  _RingEmission;
                float  _RingWarpStrength;
                float  _RingSpacing;
                float  _ChromaStrength;
                float  _ChromaEdgePow;
                float  _FresnelPower;
                float  _MetalF0;
                float  _ThinFilmFreq;
                float  _FresnelIntensity;
                float  _BlobScale;
                float  _ShapeIrregularity;
                float  _ShapeOffsetX;
                float  _ShapeOffsetY;
                float  _RotationOffset;
                float  _TimeOffset;
                float  _SpawnProgress;
                float  _BlowFade;
                float  _DissolveProgress;
                float  _SparkleScale;
                float  _SparkleGlow;
                float  _TestMode;
            CBUFFER_END

            #define TAU 6.28318530718f

            // ── IO structs ────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ══════════════════════════════════════════════════════════════════
            //  NOISE LIBRARY
            // ══════════════════════════════════════════════════════════════════

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1f,  311.7f)),
                           dot(p, float2(269.5f,  183.3f)));
                return frac(sin(p) * 43758.5453123f) * 2.0f - 1.0f;
            }

            // Quintic C² gradient noise, range ≈ [-1, 1]
            float GradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
                return lerp(
                    lerp(dot(Hash22(i + float2(0,0)), f - float2(0,0)),
                         dot(Hash22(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(Hash22(i + float2(0,1)), f - float2(0,1)),
                         dot(Hash22(i + float2(1,1)), f - float2(1,1)), u.x),
                    u.y);
            }

            // 4-octave FBM with per-octave rotation to break axis-aligned banding
            float FBM4(float2 p)
            {
                const float2x2 M = float2x2(1.6f, 1.2f, -1.2f, 1.6f);
                float v = 0.0f, a = 0.5f;
                v += a * GradNoise(p); p = mul(M, p); a *= 0.5f;
                v += a * GradNoise(p); p = mul(M, p); a *= 0.5f;
                v += a * GradNoise(p); p = mul(M, p); a *= 0.5f;
                v += a * GradNoise(p);
                return v;
            }

            // ══════════════════════════════════════════════════════════════════
            //  TWO-LEVEL DOMAIN WARP
            //  Drives both background refraction and ring distortion.
            //  Kept from original (Inigo Quilez technique).
            // ══════════════════════════════════════════════════════════════════
            float2 WarpedFlow(float2 uv, float t)
            {
                float2 p = uv * _NoiseScale;
                float2 q = float2(
                    FBM4(p + float2( t * 0.11f,  t * 0.07f)),
                    FBM4(p + float2( 5.20f, 1.30f) + float2(t * 0.08f, -t * 0.09f)));
                return float2(
                    FBM4(p + _WarpAmount * q + float2(1.70f + t * 0.15f,  9.20f + t * 0.10f)),
                    FBM4(p + _WarpAmount * q + float2(8.30f + t * 0.13f,  2.80f + t * 0.12f)));
            }

            // ══════════════════════════════════════════════════════════════════
            //  ANISOTROPIC BRUSHSTROKE NOISE  (Fix 1: noise-jittered directions)
            //
            //  Each of the 3 stretched gradient layers samples a separate
            //  low-frequency noise value (p * 0.3) to independently rotate its
            //  stretch axis each frame.  Because the jitter noise varies at roughly
            //  one cycle per blob diameter, adjacent surface regions get different
            //  dominant stroke orientations — breaking the regular parallel-stripe
            //  artifact without destroying the "flowing stroke" character.
            //
            //  Called at 5 UV positions per fragment (centre + 4 neighbours) for
            //  edge-gradient computation; the jitter noise is cheap (one GradNoise
            //  per layer) and effectively identical across the ±0.0025 offset, so
            //  the finite-difference gradient remains accurate.
            // ══════════════════════════════════════════════════════════════════
            float AnisoBrushstroke(float2 p, float t)
            {
                p *= _NoiseScale;
                float sx = _StrokeStretch;

                // ── Per-layer independent angle jitter ────────────────────────
                // p * 0.3 = very low frequency: one direction-zone per ~1 blob
                // width, giving coherent but non-repeating stroke orientations.
                // Different seeds ensure the three layers rotate independently.
                float j1 = GradNoise(p * 0.30f + float2( t * 0.030f,  0.000f       ));
                float j2 = GradNoise(p * 0.30f + float2( 3.71f, 1.93f) + float2( 0.000f,  t * 0.020f));
                float j3 = GradNoise(p * 0.30f + float2( 7.43f, 5.12f) + float2(-t * 0.025f, t * 0.010f));

                // Base angles: 0°, +55°, -40° — each perturbed ±45° by its jitter,
                // then shifted by _RotationOffset (random per instance, set from C#).
                const float JITTER_AMP = TAU / 8.0f;
                float a1 = j1 * JITTER_AMP + _RotationOffset;
                float a2 = j2 * JITTER_AMP + TAU * 0.1528f  + _RotationOffset;
                float a3 = j3 * JITTER_AMP - TAU * 0.1111f  + _RotationOffset;

                // ── Layer 1 ───────────────────────────────────────────────────
                float2 d1 = float2(cos(a1), sin(a1));
                float2 p1 = float2(dot(p, d1) * sx,
                                   dot(p, float2(-d1.y, d1.x)))
                          + float2(t * 0.28f, t * 0.05f);

                // ── Layer 2 ───────────────────────────────────────────────────
                float2 d2 = float2(cos(a2), sin(a2));
                float2 p2 = float2(dot(p, d2) * sx * 0.65f,
                                   dot(p, float2(-d2.y, d2.x)))
                          - float2(t * 0.20f, t * 0.03f);

                // ── Layer 3 ───────────────────────────────────────────────────
                float2 d3 = float2(cos(a3), sin(a3));
                float2 p3 = float2(dot(p, d3) * sx * 0.45f,
                                   dot(p, float2(-d3.y, d3.x)))
                          + float2(t * 0.12f, -t * 0.09f);

                return GradNoise(p1) * 0.55f
                     + GradNoise(p2) * 0.30f
                     + GradNoise(p3) * 0.15f;
            }

            // ══════════════════════════════════════════════════════════════════
            //  FOUR-STOP CYCLIC FLUID PALETTE  (Feature 2)
            //  Replaces hardcoded IriPalette. All 4 stops are [HDR] inspector
            //  properties — dial in purples, cyans, rainbow blacks, etc.
            // ══════════════════════════════════════════════════════════════════
            float3 FluidPalette(float t)
            {
                t = frac(t);
                float s;
                if (t < 0.25f)
                {
                    s = t * 4.0f; s = s * s * (3.0f - 2.0f * s);
                    return lerp(_Color1.rgb, _Color2.rgb, s);
                }
                if (t < 0.50f)
                {
                    s = (t - 0.25f) * 4.0f; s = s * s * (3.0f - 2.0f * s);
                    return lerp(_Color2.rgb, _Color3.rgb, s);
                }
                if (t < 0.75f)
                {
                    s = (t - 0.50f) * 4.0f; s = s * s * (3.0f - 2.0f * s);
                    return lerp(_Color3.rgb, _Color4.rgb, s);
                }
                s = (t - 0.75f) * 4.0f; s = s * s * (3.0f - 2.0f * s);
                return lerp(_Color4.rgb, _Color1.rgb, s);
            }

            // ══════════════════════════════════════════════════════════════════
            //  SDF RING GLOW  (Feature 3)
            //  Returns [0, 1] with peak 1 at exactly d == radius, falling off
            //  linearly over ±thickness. Called once per ring per fragment.
            // ══════════════════════════════════════════════════════════════════
            float SDFRingGlow(float d, float radius, float thickness)
            {
                return saturate(1.0f - abs(d - radius) / max(thickness, 1e-4f));
            }

            // ══════════════════════════════════════════════════════════════════
            //  VERTEX SHADER  (unchanged from original)
            // ══════════════════════════════════════════════════════════════════
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   norm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.uv         = IN.uv;
                OUT.screenPos  = ComputeScreenPos(pos.positionCS);
                OUT.normalWS   = norm.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                return OUT;
            }

            // ══════════════════════════════════════════════════════════════════
            //  FRAGMENT SHADER
            // ══════════════════════════════════════════════════════════════════
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                if (_TestMode > 0.5f) return half4(1.0f, 0.0f, 0.2f, 1.0f);

                float2 uv = IN.uv;
                float  localTime = _Time.y + _TimeOffset;
                float  t  = localTime * _FlowSpeed;
                float  tc = localTime * _ColorSpeed;

                // ── Centre-relative polar coords (Fix 3: global size) ────────
                // Dividing by _TotalSize shrinks the coordinate space so the
                // entire effect appears larger on-screen. All downstream values
                // (distCentre, blob boundary, SDF rings) scale consistently.
                float2 centred    = (uv - 0.5f) / max(_TotalSize, 0.001f);
                float  distCentre = length(centred);
                float2 radialDir  = distCentre > 1e-5f
                                      ? centred / distCentre
                                      : float2(0.0f, 1.0f);

                // ── Spawn/despawn scale & fade ────────────────────────────────
                // sizeScale  0→1→0 across lifetime; drives blob size + ring radii.
                // lifeFade   0→1→0 with tighter ramps; multiplied into final alpha.
                float sizeScale = 1.0f; // size is constant; fade in/out is handled by lifeFade only
                float lifeFade  = min(smoothstep(0.0f, 0.20f, _SpawnProgress),
                                      1.0f - smoothstep(0.80f, 1.0f, _SpawnProgress));
                if (lifeFade < 0.001f) discard;

                // ── Two-level domain-warp blob silhouette ─────────────────────
                // FBM is sampled in mesh-UV space so frequency is independent of
                // _TotalSize. The two-level warp displaces centred before the
                // distance test, creating ink-splatter / lava-splash boundaries
                // instead of a circle or wobbly ring.
                float2 shapeSeed = float2(_ShapeOffsetX, _ShapeOffsetY);
                float2 blobQ = float2(
                    FBM4(uv * _BlobScale + shapeSeed),
                    FBM4(uv * _BlobScale + shapeSeed + float2(5.2f, 1.3f)));
                float2 blobR = float2(
                    FBM4(uv * _BlobScale + blobQ * 2.5f + shapeSeed + float2(1.7f, 9.2f)),
                    FBM4(uv * _BlobScale + blobQ * 2.5f + shapeSeed + float2(8.3f, 2.8f)));
                float2 splatPos  = centred + blobR * (_ShapeIrregularity * 1.5f);
                float  splatDist = length(splatPos);
                // Dividing by sizeScale shrinks the apparent blob during spawn/despawn
                float  splatDistScaled = splatDist / max(sizeScale, 0.001f);
                float  radialAlpha = smoothstep(0.45f + 0.025f, 0.45f - 0.025f, splatDistScaled);
                if (radialAlpha < 0.001f) discard;

                // Radial edge gradient: opaque at centre, fades to transparent at boundary.
                // normalizedDist goes 0 (centre) → 1 (blob edge at splatDistScaled ≈ 0.45).
                // softEdge = 1 inside the opaque core, smoothly → 0 at the boundary.
                float normalizedDist = splatDistScaled / 0.45f;
                float softEdge = 1.0f - smoothstep(1.0f - _EdgeSoftness, 1.0f, normalizedDist);

                // ── Domain warp (used for refraction + ring distortion) ───────
                float2 warp = WarpedFlow(uv, t);

                // ── Mesh-boundary edge fade ───────────────────────────────────
                float edgeFade = smoothstep(0.0f, 0.05f, uv.x)
                               * smoothstep(0.0f, 0.05f, 1.0f - uv.x)
                               * smoothstep(0.0f, 0.05f, uv.y)
                               * smoothstep(0.0f, 0.05f, 1.0f - uv.y);

                // ════════════════════════════════════════════════════════════
                //  FEATURE 1: ANISOTROPIC BRUSHSTROKE MASK
                //
                //  The stroke noise is sampled at the current pixel and at four
                //  neighbours (±ED offset) so we can derive the spatial gradient.
                //  The gradient is used for both the edge-chromatic-aberration
                //  (Feature 4) and the metallic Fresnel weighting (Feature 5).
                // ════════════════════════════════════════════════════════════

                // Lightly domain-warp the UV so strokes follow the flow field
                float2 warpedUV = uv + warp * 0.12f;

                // Vortex swirl: per-instance random centre + distance-dependent angle.
                // _ShapeOffsetX/Y are random 0-100 floats set from C# per spawn;
                // frac(x * 0.01) maps them to [0,1), then scaled to [0.25, 0.75] UV
                // so the vortex eye is always inside the visible blob area.
                // Rotation decays exponentially outward → whirlpool, not rigid spin.
                // _SwirlStrength scales the overall amplitude independently of speed.
                float2 swirlCentre = float2(
                    frac(_ShapeOffsetX * 0.01f) * 0.5f + 0.25f,
                    frac(_ShapeOffsetY * 0.01f) * 0.5f + 0.25f);
                float2 spinCentre  = warpedUV - swirlCentre;
                float  spinDist    = length(spinCentre);
                float  spinAngle   = localTime * _SpinSpeed * _SwirlStrength
                                   * exp(-spinDist * _SwirlTightness);
                float  cosS = cos(spinAngle), sinS = sin(spinAngle);
                float2 spunUV = float2(
                    cosS * spinCentre.x - sinS * spinCentre.y,
                    sinS * spinCentre.x + cosS * spinCentre.y) + swirlCentre;

                float strokeRaw  = AnisoBrushstroke(spunUV, t);
                float strokeNorm = strokeRaw * 0.5f + 0.5f;   // remap [-1,1] → [0,1]

                // Harsh smoothstep: opaque strokes | transparent cracks
                float strokeMask = smoothstep(
                    _StrokeThreshold - _StrokeEdge,
                    _StrokeThreshold + _StrokeEdge,
                    strokeNorm);
                strokeMask *= radialAlpha;

                // ── Spatial gradient of stroke noise (finite difference) ──────
                const float ED = 0.0025f;
                float snR = AnisoBrushstroke(spunUV + float2( ED,  0), t) * 0.5f + 0.5f;
                float snL = AnisoBrushstroke(spunUV + float2(-ED,  0), t) * 0.5f + 0.5f;
                float snU = AnisoBrushstroke(spunUV + float2(  0, ED), t) * 0.5f + 0.5f;
                float snD = AnisoBrushstroke(spunUV + float2(  0,-ED), t) * 0.5f + 0.5f;
                float2 strokeGrad = float2(snR - snL, snU - snD) * (0.5f / ED);
                float  gradMag    = length(strokeGrad);
                float2 edgeDir    = gradMag > 1e-5f ? strokeGrad / gradMag : radialDir;

                // Edge weight = 4*x*(1-x): zero on solid/empty, peaks at the boundary
                float edgeWeight = 4.0f * strokeMask * (1.0f - strokeMask);

                // ════════════════════════════════════════════════════════════
                //  SCREEN-SPACE REFRACTION
                // ════════════════════════════════════════════════════════════

                float  radialW = smoothstep(0.0f, 0.48f, distCentre) * _RadialBias;
                float2 distort = (warp + radialDir * radialW) * _DistortionStrength;

                // Edge pull: radial outward displacement, quadratic weight so it
                // peaks at the blob boundary and fades to zero at the centre.
                // Sampling outward here means each edge fragment shows background
                // content from beyond the blob — visually the scene appears to be
                // dragged / curled into the effect at its rim.
                float  edgePullW = splatDistScaled * splatDistScaled;
                distort += radialDir * _EdgePullStrength * edgePullW;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // ════════════════════════════════════════════════════════════
                //  FEATURE 4: EDGE-CONCENTRATED CHROMATIC ABERRATION
                //
                //  chromaOff is scaled by edgeWeight — maximum at the exact
                //  transition line between opaque and transparent, zero inside
                //  flat solid or flat transparent regions.
                // ════════════════════════════════════════════════════════════
                float2 chromaOff = edgeDir * _ChromaStrength
                                 * pow(edgeWeight, _ChromaEdgePow);

                float  scR = SampleSceneColor(saturate(screenUV + distort + chromaOff)).r;
                float  scG = SampleSceneColor(saturate(screenUV + distort            )).g;
                float  scB = SampleSceneColor(saturate(screenUV + distort - chromaOff)).b;
                float3 refracted = float3(scR, scG, scB);

                // ════════════════════════════════════════════════════════════
                //  FEATURE 2: CUSTOM 4-COLOR FLUID PALETTE
                //
                //  palT blends stroke density, radial distance, time, and the
                //  per-instance offset for maximum inter-anomaly variation.
                // ════════════════════════════════════════════════════════════
                float  palT     = frac(strokeNorm * 0.70f
                                     + distCentre * 0.30f
                                     + tc         * 0.06f
                                     + _IriOffset);
                float3 fluidCol = FluidPalette(palT);

                // ════════════════════════════════════════════════════════════
                //  FEATURE 5: METALLIC FRESNEL + THIN-FILM INTERFERENCE
                //
                //  Per-channel F0 gives chromatic metal (IOR varies by λ).
                //  Thin-film: cos-based spectral bands shift with NdotV, the
                //  three channel frequencies (1.0 / 1.2 / 1.5 × TAU) produce
                //  RGB shimmer analogous to soap-film interference.
                //  Fresnel effect is masked to opaque stroke regions only.
                // ════════════════════════════════════════════════════════════
                float3 N     = normalize(IN.normalWS);
                float3 V     = normalize(IN.viewDirWS);
                float  NdotV = saturate(dot(N, V));

                // Schlick approximation with slightly wavelength-split F0
                float3 F0 = float3(_MetalF0 * 1.00f,
                                   _MetalF0 * 0.97f,
                                   _MetalF0 * 0.93f);
                float3 schlick = F0 + (1.0f - F0) * pow(1.0f - NdotV, _FresnelPower);

                // Thin-film spectral interference
                float  filmAngle = NdotV * _ThinFilmFreq;
                float3 thinFilm  = 0.5f + 0.5f * cos(filmAngle
                                 * float3(TAU, TAU * 1.2f, TAU * 1.5f));

                // Combined: Schlick tinted by thin-film shimmer
                float3 fresnelRim = schlick * (0.5f + 0.5f * thinFilm);

                // Boost opaque strokes to look highly reflective; gaps stay matte
                float3 metallicFluid = fluidCol
                                     * (1.0f + fresnelRim * _FresnelIntensity * strokeMask);

                // ════════════════════════════════════════════════════════════
                //  FEATURE 3: SDF HOLOGRAPHIC RINGS
                //
                //  The ring distance field is evaluated at a warp-displaced
                //  position so the rings look torn / glitchy, not perfectly
                //  circular. Three rings spaced by _RingSpacing. Each ring's
                //  glow colour cycles at a different palette offset to the fluid.
                // ════════════════════════════════════════════════════════════
                float2 ringPos  = centred + warp * _RingWarpStrength;
                float  ringDist = length(ringPos);

                float r1 = _RingRadius * sizeScale;
                float r2 = r1 * _RingSpacing;
                float r3 = r2 * _RingSpacing;

                float ring1 = SDFRingGlow(ringDist, r1, _RingThickness);
                float ring2 = SDFRingGlow(ringDist, r2, _RingThickness) * 0.85f;
                float ring3 = SDFRingGlow(ringDist, r3, _RingThickness) * 0.65f;
                float allRings = max(max(ring1, ring2), ring3) * radialAlpha;

                // Rings use a palette slice offset from the fluid for contrast
                float3 ringCol = FluidPalette(frac(palT + 0.37f))
                               * _RingEmission * allRings;

                // ── Final composition ─────────────────────────────────────────
                // Transparent gaps  → distorted refraction of background
                // Opaque strokes    → metallic fluid colour (Feature 2 + 5)
                // Rings             → additive HDR overlay (Feature 3)
                float3 col = lerp(refracted, metallicFluid, strokeMask);
                col += ringCol;

                // ── Alpha ─────────────────────────────────────────────────────
                // Gaps use _GapOpacity so the distorted refraction sample shows.
                // Strokes are fully opaque (alpha = 1).
                float alpha = lerp(_GapOpacity, 1.0f, strokeMask) * radialAlpha * edgeFade * lifeFade * softEdge * _BlowFade;
                alpha = saturate(alpha);

                // ── Dissolve to sparkles ──────────────────────────────────────
                // Cells below _DissolveProgress are discarded. Cells near the
                // boundary flash as circular glowing star dots (core + soft halo).
                if (_DissolveProgress > 0.001f)
                {
                    float2 cell      = floor(uv * _SparkleScale);
                    float  cellNoise = frac(sin(dot(cell, float2(127.1f, 311.7f))) * 43758.5453f);
                    if (cellNoise < _DissolveProgress) discard;

                    // Sub-cell coordinate → circular dot shape (not square pixel)
                    float2 cellUV  = frac(uv * _SparkleScale) - 0.5f;
                    float  dotDist = length(cellUV);
                    float  dotCore = smoothstep(0.30f, 0.04f, dotDist);  // sharp bright centre
                    float  dotHalo = smoothstep(0.50f, 0.00f, dotDist);  // soft outer glow

                    // Sparkle: brightest at the dissolve wavefront, fades ahead of it
                    float  sparkle = smoothstep(_DissolveProgress + 0.18f, _DissolveProgress, cellNoise);
                    // Per-cell random flicker speed so each star pulses differently
                    float  flicker = frac(sin(dot(cell, float2(311.7f, 127.1f)) + _Time.y * 7.0f) * 43758.5453f);
                    float  shine   = (dotCore * 1.6f + dotHalo * 0.5f) * (0.45f + 0.55f * flicker);

                    col += sparkle * _SparkleGlow * float3(1.0f, 0.95f, 0.82f) * shine;
                }

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
