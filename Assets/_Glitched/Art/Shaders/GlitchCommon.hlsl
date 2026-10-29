// Shared corruption maths for every GLITCHED shader.
// Globals are set from C# by CorruptionManager / RestorationDirector / GlitchFX.
#ifndef GLITCHED_COMMON_INCLUDED
#define GLITCHED_COMMON_INCLUDED

float _Corruption;            // 0 healthy .. 1 fully corrupted
float _RestoreWaveX;          // world X of the restoration wave front
float _RestoreWaveWidth;      // softness of the wave front
float _RestoreWaveActive;     // 1 while the wave is moving
float4 _RegionAccent;         // region accent colour (magenta, orange, acid green...)
float4 _RegionAccent2;        // secondary accent (cyan)
float _ScreenGlitch;          // short spikes on impacts (0..1)
float _HeightFogOn;
float4 _HeightFogColor;
float4 _HeightFogParams;      // x = base height, y = falloff, z = max amount
float4 _PlayerScreenPos;      // xy = player position in viewport (0..1), z = radius, w = enabled

float GHash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float GHash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float GHash31(float3 p3)
{
    p3 = frac(p3 * 0.1031);
    p3 += dot(p3, p3.zyx + 31.32);
    return frac((p3.x + p3.y) * p3.z);
}

// Corruption at a world X position, taking the restoration wave into account.
float EffectiveCorruption(float worldX)
{
    float heal = saturate((_RestoreWaveX - worldX) / max(_RestoreWaveWidth, 0.01) + 0.5) * _RestoreWaveActive;
    return saturate(_Corruption * (1.0 - heal));
}

// Glow of the restoration wave front.
float RestoreGlow(float worldX)
{
    return exp(-abs(worldX - _RestoreWaveX) * 0.22) * _RestoreWaveActive;
}

// Horizontal slice displacement (the "tearing" look). Returns an X offset in world units.
float SliceOffset(float3 positionWS, float c, float seed)
{
    float t = floor(_Time.y * 11.0);
    float slice = floor(positionWS.y * 1.6 + seed * 7.0);
    float r = GHash21(float2(slice, t + seed));
    float on = step(1.0 - 0.16 * c, r);
    return (GHash21(float2(slice + 3.1, t)) - 0.5) * 0.9 * c * on;
}

float3 GDesaturate(float3 col, float amount)
{
    float g = dot(col, float3(0.299, 0.587, 0.114));
    return lerp(col, g.xxx, amount);
}

// Animated magenta/cyan scan bands for corrupted surfaces.
float3 GlitchBands(float3 positionWS, float c, float seed)
{
    float band = frac(positionWS.y * 0.35 - _Time.y * 0.55 + seed);
    float b1 = smoothstep(0.93, 1.0, band);
    float band2 = frac(positionWS.y * 0.35 - _Time.y * 0.55 + seed + 0.04);
    float b2 = smoothstep(0.93, 1.0, band2);
    float flick = step(0.9, GHash11(floor(_Time.y * 5.0) + seed * 13.0));
    float3 mag = _RegionAccent.rgb * b1;
    float3 cy = _RegionAccent2.rgb * b2;
    return (mag + cy) * c * (0.6 + flick * 1.4);
}

float3 ApplyHeightFog(float3 col, float3 positionWS, float viewDistance)
{
    if (_HeightFogOn < 0.5) return col;
    float h = saturate(exp(-(positionWS.y - _HeightFogParams.x) * _HeightFogParams.y)) * _HeightFogParams.z;
    float dist = saturate(viewDistance / 140.0);
    return lerp(col, _HeightFogColor.rgb, h * dist);
}

#endif
