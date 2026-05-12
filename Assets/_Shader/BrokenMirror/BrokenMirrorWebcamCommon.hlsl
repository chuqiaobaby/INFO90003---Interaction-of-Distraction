#ifndef BROKEN_MIRROR_WEBCAM_COMMON_INCLUDED
#define BROKEN_MIRROR_WEBCAM_COMMON_INCLUDED

float Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = Hash12(i + float2(0.0, 0.0));
    float b = Hash12(i + float2(1.0, 0.0));
    float c = Hash12(i + float2(0.0, 1.0));
    float d = Hash12(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float Fbm3(float2 p)
{
    float value = 0.0;
    float amp = 0.5;
    value += ValueNoise(p) * amp;
    p = p * 2.07 + 17.13;
    amp *= 0.5;
    value += ValueNoise(p) * amp;
    p = p * 2.11 + 9.31;
    amp *= 0.5;
    value += ValueNoise(p) * amp;
    return value;
}

float2 SafeNormalize2(float2 v)
{
    return v * rsqrt(max(dot(v, v), 1e-5));
}

float SampleCrackMask(float2 uv, float state, TEXTURE2D_PARAM(mask1, samplerMask1), TEXTURE2D_PARAM(mask2, samplerMask2), TEXTURE2D_PARAM(mask3, samplerMask3))
{
    float w1 = saturate(1.0 - abs(state - 1.0));
    float w2 = saturate(1.0 - abs(state - 2.0));
    float w3 = saturate(1.0 - abs(state - 3.0));

    float c1 = SAMPLE_TEXTURE2D(mask1, samplerMask1, uv).r;
    float c2 = SAMPLE_TEXTURE2D(mask2, samplerMask2, uv).r;
    float c3 = SAMPLE_TEXTURE2D(mask3, samplerMask3, uv).r;
    return saturate(max(max(c1 * w1, c2 * w2), c3 * w3));
}

float2 CrackGradient(float2 uv, float state, float texel, TEXTURE2D_PARAM(mask1, samplerMask1), TEXTURE2D_PARAM(mask2, samplerMask2), TEXTURE2D_PARAM(mask3, samplerMask3))
{
    float2 dx = float2(texel, 0.0);
    float2 dy = float2(0.0, texel);
    float gx = SampleCrackMask(uv + dx, state, TEXTURE2D_ARGS(mask1, samplerMask1), TEXTURE2D_ARGS(mask2, samplerMask2), TEXTURE2D_ARGS(mask3, samplerMask3))
             - SampleCrackMask(uv - dx, state, TEXTURE2D_ARGS(mask1, samplerMask1), TEXTURE2D_ARGS(mask2, samplerMask2), TEXTURE2D_ARGS(mask3, samplerMask3));
    float gy = SampleCrackMask(uv + dy, state, TEXTURE2D_ARGS(mask1, samplerMask1), TEXTURE2D_ARGS(mask2, samplerMask2), TEXTURE2D_ARGS(mask3, samplerMask3))
             - SampleCrackMask(uv - dy, state, TEXTURE2D_ARGS(mask1, samplerMask1), TEXTURE2D_ARGS(mask2, samplerMask2), TEXTURE2D_ARGS(mask3, samplerMask3));
    return float2(gx, gy);
}

float3 SampleWebcamBlur(float2 uv, float radius, TEXTURE2D_PARAM(tex, samplerTex))
{
    float2 r = float2(radius, radius);
    float3 color = SAMPLE_TEXTURE2D(tex, samplerTex, saturate(uv)).rgb * 0.40;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, saturate(uv + float2( r.x, 0.0))).rgb * 0.15;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, saturate(uv + float2(-r.x, 0.0))).rgb * 0.15;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, saturate(uv + float2(0.0,  r.y))).rgb * 0.15;
    color += SAMPLE_TEXTURE2D(tex, samplerTex, saturate(uv + float2(0.0, -r.y))).rgb * 0.15;
    return color;
}

#endif
