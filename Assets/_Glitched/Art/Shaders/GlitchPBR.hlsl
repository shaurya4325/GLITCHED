// Realistic PBR lighting helpers for the GLITCHED world (URP), with a SUBTLE corruption layer:
//   - slow, gentle desaturation and a cool shift where the world is corrupted
//   - an occasional soft scan line drifting over a few objects at a time (never fast flicker)
//   - a rare, tiny horizontal tear in the geometry
//   - a warm glow at the restoration wave front
// Everything animates below ~1 Hz so it reads as "something is wrong" without hurting the eyes.
#ifndef GLITCHED_PBR_INCLUDED
#define GLITCHED_PBR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "GlitchCommon.hlsl"

float GObjectSeed(float seed)
{
    float3 o = TransformObjectToWorld(float3(0, 0, 0));
    return frac(GHash31(floor(o * 0.37)) + seed);
}

// Rare, tiny tear: about 3% of 0.5 m slices shift by a few centimetres for a fraction of a second.
float3 SubtleTear(float3 positionWS, float c, float seed)
{
    #if defined(GLITCH_LOW)
        return positionWS;
    #else
        float t = floor(_Time.y * 2.0);
        float slice = floor(positionWS.y * 2.0 + seed * 11.0);
        float on = step(1.0 - 0.03 * c, GHash21(float2(slice, t + seed * 3.0)));
        positionWS.x += (GHash21(float2(slice + 5.0, t)) - 0.5) * 0.12 * c * on;
        return positionWS;
    #endif
}

// Corruption colour treatment applied after lighting.
half3 SubtleCorruption(half3 col, float3 positionWS, float c, float seed)
{
    if (c <= 0.001) return col;
    // cool, slightly drained colour
    half3 drained = GDesaturate(col, 1.0) * half3(0.9, 0.95, 1.08);
    col = lerp(col, drained, 0.25 * c);
    #if !defined(GLITCH_LOW)
        // a soft scan line now and then: each object is "active" for ~4 s in about 1 of 6 windows
        float window = floor(_Time.y * 0.25 + seed * 7.0);
        float active = step(0.83, GHash11(window + seed * 31.0));
        float band = frac(positionWS.y * 0.06 - _Time.y * 0.12 + seed);
        float scan = smoothstep(0.975, 0.995, band) * (1.0 - smoothstep(0.995, 1.0, band));
        col += _RegionAccent.rgb * scan * active * 0.35 * c;
    #endif
    return col;
}

half3 RestorationGlow(float3 positionWS)
{
    return _RegionAccent2.rgb * RestoreGlow(positionWS.x) * 0.6;
}

#endif
