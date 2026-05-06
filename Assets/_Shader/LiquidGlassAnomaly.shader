// ══════════════════════════════════════════════════════════════════════════════
//  LiquidGlassAnomaly.shader  —  URP  (Transparent / Opaque Texture required)
//
//  SETUP:
//    1. In your URP Renderer Asset → enable "Opaque Texture"
//    2. Assign this shader to a material on any mesh (sphere works great)
//    3. Place the mesh AFTER opaque geometry in the scene
//
//  FEATURES:
//    • 2-level domain-warped FBM distortion  (Inigo Quilez warp technique)
//    • Radial outward flow from object centre
//    • Screen-space refraction via _CameraOpaqueTexture
//    • Chromatic aberration (RGB split along distortion axis)
//    • Thin-film iridescence: silver → cyan → magenta → gold palette
//    • HDR core emission + tendril glow  (triggers Bloom in post-processing)
//    • Fresnel rim drives iridescence weighting
// ══════════════════════════════════════════════════════════════════════════════

Shader "Custom/LiquidGlassAnomaly"
{
    Properties
    {
        [Header(Distortion)]
        _DistortionStrength ("Distortion Strength",   Range(0.0, 0.15)) = 0.045
        _NoiseScale         ("Noise Scale",           Range(0.5,  8.0)) = 2.4
        _FlowSpeed          ("Flow Speed",            Range(0.0,  5.0)) = 0.75
        _RadialBias         ("Radial Outward Bias",   Range(0.0,  3.0)) = 1.2
        _WarpAmount         ("Domain Warp Amount",    Range(0.0, 12.0)) = 4.5

        [Space(6)][Header(Iridescence)]
        _IriScale           ("Iri Noise Scale",       Range(0.5,  8.0)) = 2.2
        _IriSpeed           ("Iri Animation Speed",   Range(0.0,  3.0)) = 0.35
        _IriIntensity       ("Iri Intensity",         Range(0.0,  5.0)) = 2.4
        _FresnelPower       ("Fresnel Power",         Range(0.1,  8.0)) = 2.5

        [Space(6)][Header(Emission)]
        [HDR] _CoreColor    ("Core Colour (HDR)",     Color) = (1.2, 0.9, 1.0, 1.0)
        _CoreEmission       ("Core HDR Emission",     Range(0.0, 40.0)) = 12.0
        _CoreRadius         ("Core Radius",           Range(0.005, 0.5)) = 0.11
        _TendrilGlow        ("Tendril Glow",          Range(0.0,  6.0)) = 1.8

        [Space(6)][Header(Chromatic Aberration)]
        _ChromaStrength     ("Chroma Strength",       Range(0.0, 0.04)) = 0.007

        [Space(6)][Header(Overall)]
        _Opacity            ("Opacity",               Range(0.0,  1.0)) = 0.93

        [Space(6)][Header(Diagnostic)]
        [Toggle] _TestMode  ("Test Mode (solid red — ignore all effects)", Float) = 0
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
                float  _DistortionStrength;
                float  _NoiseScale;
                float  _FlowSpeed;
                float  _RadialBias;
                float  _WarpAmount;
                float  _IriScale;
                float  _IriSpeed;
                float  _IriIntensity;
                float  _FresnelPower;
                float4 _CoreColor;
                float  _CoreEmission;
                float  _CoreRadius;
                float  _TendrilGlow;
                float  _ChromaStrength;
                float  _Opacity;
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

            // Pseudo-random gradient vectors, range [-1, 1]
            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1f,  311.7f)),
                           dot(p, float2(269.5f,  183.3f)));
                return frac(sin(p) * 43758.5453123f) * 2.0f - 1.0f;
            }

            // Gradient noise with quintic C² continuity (IQ-style), range ≈ [-1, 1]
            float GradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Quintic smoothstep
                float2 u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);

                float a = dot(Hash22(i + float2(0.0f, 0.0f)), f - float2(0.0f, 0.0f));
                float b = dot(Hash22(i + float2(1.0f, 0.0f)), f - float2(1.0f, 0.0f));
                float c = dot(Hash22(i + float2(0.0f, 1.0f)), f - float2(0.0f, 1.0f));
                float d = dot(Hash22(i + float2(1.0f, 1.0f)), f - float2(1.0f, 1.0f));

                return lerp(lerp(a, b, u.x),
                            lerp(c, d, u.x), u.y);
            }

            // 4-octave FBM with per-octave rotation to eliminate axis-aligned banding.
            // Rotation matrix: ~36.87° + 2× scale per octave.  Range ≈ [-0.94, 0.94]
            float FBM4(float2 p)
            {
                // Row-major: row0=(1.6, 1.2)  row1=(-1.2, 1.6)
                float2x2 M = float2x2(1.6f,  1.2f,
                                     -1.2f,  1.6f);
                float v = 0.0f;
                float a = 0.5f;
                v += a * GradNoise(p); p = mul(M, p); a *= 0.5f;
                v += a * GradNoise(p); p = mul(M, p); a *= 0.5f;
                v += a * GradNoise(p); p = mul(M, p); a *= 0.5f;
                v += a * GradNoise(p);
                return v;
            }

            // ── Two-level domain warp (Inigo Quilez) ─────────────────────────
            // Returns a 2D displacement vector in ≈ [-0.94, 0.94] per component.
            // The double-warp creates spectacular, self-similar fluid swirls.
            float2 WarpedFlow(float2 uv, float t)
            {
                float2 p = uv * _NoiseScale;

                // Level-1: independent X/Y noise fields, slowly animated
                float2 q = float2(
                    FBM4(p + float2( t * 0.11f,  t * 0.07f)),
                    FBM4(p + float2( 5.20f,  1.30f) + float2(t * 0.08f, -t * 0.09f))
                );

                // Level-2: p is displaced by _WarpAmount * q before sampling
                // Different constant offsets keep x/y decorrelated
                float2 r = float2(
                    FBM4(p + _WarpAmount * q + float2(1.70f + t * 0.150f,  9.20f + t * 0.100f)),
                    FBM4(p + _WarpAmount * q + float2(8.30f + t * 0.130f,  2.80f + t * 0.120f))
                );
                return r;
            }

            // ══════════════════════════════════════════════════════════════════
            //  IRIDESCENCE PALETTE
            //  4-stop smooth gradient:  silver → cyan → magenta → gold → (loop)
            //  Uses smoothstep for soft S-curve transitions at each stop.
            // ══════════════════════════════════════════════════════════════════
            float3 IriPalette(float t)
            {
                t = frac(t);

                float3 SILVER  = float3(0.80f, 0.83f, 0.88f);
                float3 CYAN    = float3(0.00f, 1.00f, 1.00f);
                float3 MAGENTA = float3(1.00f, 0.05f, 0.92f);
                float3 GOLD    = float3(1.00f, 0.84f, 0.08f);

                float s;
                if (t < 0.25f)
                {
                    s = saturate(t * 4.0f);
                    s = s * s * (3.0f - 2.0f * s);
                    return lerp(SILVER, CYAN, s);
                }
                if (t < 0.50f)
                {
                    s = saturate((t - 0.25f) * 4.0f);
                    s = s * s * (3.0f - 2.0f * s);
                    return lerp(CYAN, MAGENTA, s);
                }
                if (t < 0.75f)
                {
                    s = saturate((t - 0.50f) * 4.0f);
                    s = s * s * (3.0f - 2.0f * s);
                    return lerp(MAGENTA, GOLD, s);
                }
                s = saturate((t - 0.75f) * 4.0f);
                s = s * s * (3.0f - 2.0f * s);
                return lerp(GOLD, SILVER, s);
            }

            // ── Vertex ────────────────────────────────────────────────────────
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

            // ── Fragment ──────────────────────────────────────────────────────
            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Diagnostic: enable "Test Mode" on the material → solid red-pink confirms
                // the Quad geometry/layer/camera are all correct. Disable for production.
                if (_TestMode > 0.5f) return half4(1.0f, 0.0f, 0.2f, 1.0f);

                float2 uv = IN.uv;
                float  t  = _Time.y * _FlowSpeed;

                // ── Centre-relative coordinates ───────────────────────────────
                float2 centred     = uv - 0.5f;
                float  distCentre  = length(centred);
                float2 radialDir   = distCentre > 1e-5f
                                       ? centred / distCentre
                                       : float2(0.0f, 1.0f);

                // ── Distortion vector  ────────────────────────────────────────
                // Domain-warped noise gives the primary swirling fluid motion.
                float2 warp = WarpedFlow(uv, t);  // range ≈ [-0.94, 0.94]

                // Radial outward push: grows stronger towards the edge of the mesh
                float radialW  = smoothstep(0.0f, 0.48f, distCentre) * _RadialBias;
                float2 distort = (warp + radialDir * radialW) * _DistortionStrength;

                // Fade distortion near UV border to avoid screen-edge artefacts
                float edgeFade = smoothstep(0.0f, 0.07f, uv.x)
                               * smoothstep(0.0f, 0.07f, 1.0f - uv.x)
                               * smoothstep(0.0f, 0.07f, uv.y)
                               * smoothstep(0.0f, 0.07f, 1.0f - uv.y);
                distort *= edgeFade;

                // ── Screen UVs ────────────────────────────────────────────────
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // ── Chromatic aberration (RGB split along distortion axis) ─────
                // Magnitude of the current distortion drives how wide the split is.
                float  chromaMag = saturate(length(distort) / max(_DistortionStrength * 0.5f, 1e-5f));
                float2 chromaOff = radialDir * _ChromaStrength * chromaMag;

                float  scR = SampleSceneColor(saturate(screenUV + distort + chromaOff)).r;
                float  scG = SampleSceneColor(saturate(screenUV + distort            )).g;
                float  scB = SampleSceneColor(saturate(screenUV + distort - chromaOff)).b;
                float3 refracted = float3(scR, scG, scB);

                // ── Fresnel (N·V) ─────────────────────────────────────────────
                float3 N      = normalize(IN.normalWS);
                float3 V      = normalize(IN.viewDirWS);
                float  NdotV  = saturate(dot(N, V));
                float  fresnel = pow(1.0f - NdotV, _FresnelPower);

                // ── Iridescence noise ─────────────────────────────────────────
                // A second, independently animated FBM layer drives colour cycling.
                float  ti       = _Time.y * _IriSpeed;
                float  iriNoise = FBM4(uv * _IriScale + float2(ti * 0.09f, -ti * 0.07f));
                iriNoise = iriNoise * 0.5f + 0.5f;  // remap to [0, 1]

                // Thin-film iridescence parameter: Fresnel angle + noise + slow drift
                float  iriT     = frac(iriNoise * 0.65f + fresnel * 0.35f + ti * 0.04f);
                float3 iriColor = IriPalette(iriT) * _IriIntensity;

                // ── Tendril mask ──────────────────────────────────────────────
                // The magnitude of the warp field naturally traces the fluid tendrils:
                // high where swirling is strongest, quiet where the flow is calm.
                float tendrilM = saturate(length(warp) * 1.1f);
                tendrilM       = pow(tendrilM, 1.1f);
                // Suppress tendrils right at the mesh centre (core takes over there)
                tendrilM      *= smoothstep(0.0f, _CoreRadius * 1.8f, distCentre);

                // ── Colour composition ────────────────────────────────────────

                // Base: refracted background tinted by iridescence along tendrils
                float iriBlend = saturate(tendrilM * 0.85f + fresnel * 0.45f);
                float3 col     = lerp(refracted,
                                      refracted * iriColor * 0.5f + iriColor * 0.3f,
                                      iriBlend);

                // HDR tendril glow: iridescent light emitted by the fluid itself
                // Decays with distance so it concentrates near the centre
                float tendrilEmit  = tendrilM
                                   * saturate(1.0f - distCentre * 2.0f)
                                   * _TendrilGlow;
                col += iriColor * tendrilEmit;

                // HDR core: blazing white-pink centre — key Bloom target
                // Power curve gives a very tight, bright disc with a soft halo
                float coreMask = pow(saturate(1.0f - distCentre / _CoreRadius), 2.5f);
                col += _CoreColor.rgb * _CoreEmission * coreMask;

                // ── Alpha ─────────────────────────────────────────────────────
                // Smooth circular fade from centre towards edge of mesh UVs
                float radialAlpha = pow(saturate(1.0f - smoothstep(0.30f, 0.50f, distCentre)), 0.65f);
                float alpha       = _Opacity * radialAlpha * edgeFade;
                // Guarantee core emission is always visible regardless of opacity
                alpha = max(alpha, coreMask * _Opacity);
                alpha = saturate(alpha);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
