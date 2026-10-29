// GLITCHED sky (L5): blends the corrupted and restored HDRI skies by region health,
// with Glitch tears and a data aurora. Used as RenderSettings.skybox.
Shader "Glitched/Sky"
{
    Properties
    {
        _SkyCorrupted ("Sky Corrupted (equirect)", 2D) = "grey" {}
        _SkyRestored ("Sky Restored (equirect)", 2D) = "grey" {}
        _TintCorrupted ("Tint Corrupted", Color) = (0.6,0.5,0.9,1)
        _TintRestored ("Tint Restored", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0,4)) = 1
        _Rotation ("Rotation", Range(0,360)) = 0
        _HorizonColor ("Horizon Haze", Color) = (0.5,0.5,0.6,1)
        _HorizonHaze ("Horizon Haze Amount", Range(0,1)) = 0.25
        _Tears ("Glitch Tears", Range(0,1)) = 1
        _Aurora ("Data Aurora", Range(0,2)) = 0
        _GradientTop ("Fallback Top", Color) = (0.1,0.12,0.3,1)
        _GradientBottom ("Fallback Bottom", Color) = (0.5,0.4,0.6,1)
        _UseTextures ("Use Textures", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GlitchCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TintCorrupted, _TintRestored, _HorizonColor, _GradientTop, _GradientBottom;
                half _Exposure, _Rotation, _HorizonHaze, _Tears, _Aurora, _UseTextures;
            CBUFFER_END
            TEXTURE2D(_SkyCorrupted); SAMPLER(sampler_SkyCorrupted);
            TEXTURE2D(_SkyRestored); SAMPLER(sampler_SkyRestored);

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            V vert(A v)
            {
                V o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float2 Equirect(float3 d, float rot)
            {
                float a = atan2(d.z, d.x) + radians(rot);
                float u = a / (2 * PI) + 0.5;
                float vv = asin(clamp(d.y, -1, 1)) / PI + 0.5;
                return float2(u, vv);
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float c = _Corruption;
                // during the restoration wave, the sky heals with it (sky follows the global value)
                float2 uv = Equirect(d, _Rotation);
                half3 skyC, skyR;
                if (_UseTextures > 0.5)
                {
                    skyC = SAMPLE_TEXTURE2D_LOD(_SkyCorrupted, sampler_SkyCorrupted, uv, 0).rgb;
                    skyR = SAMPLE_TEXTURE2D_LOD(_SkyRestored, sampler_SkyRestored, uv, 0).rgb;
                }
                else
                {
                    half3 g = lerp(_GradientBottom.rgb, _GradientTop.rgb, saturate(d.y * 1.4 + 0.2));
                    skyC = g; skyR = g;
                }
                skyC = GDesaturate(skyC, 0.1) * _TintCorrupted.rgb;
                skyR *= _TintRestored.rgb;
                half3 col = lerp(skyR, skyC, c) * _Exposure;
                // vibrance: photographed skies are muted; push the blues and sunset colours
                half skyLum = dot(col, half3(0.2126, 0.7152, 0.0722));
                col = max(0, lerp(skyLum.xxx, col, 1.35));

                // horizon haze helps the depth layers read
                float horizon = pow(1.0 - saturate(abs(d.y) * 2.2), 3.0);
                col = lerp(col, _HorizonColor.rgb, horizon * _HorizonHaze);

                // glitch tears: jagged vertical rifts of magenta/cyan
                float tearSeed = floor(_Time.y * 0.35);
                float u = uv.x * 9.0;
                float lane = floor(u);
                float h = GHash21(float2(lane, tearSeed));
                float jag = (GHash21(float2(floor(uv.y * 40.0), lane + tearSeed)) - 0.5) * 0.15;
                float tear = smoothstep(0.06, 0.0, abs(frac(u) - 0.5 + jag)) * step(0.72, h) * smoothstep(0.25, 0.7, uv.y);
                float pulse = 0.6 + 0.4 * sin(_Time.y * 3.0 + lane);
                col += (_RegionAccent.rgb * tear + _RegionAccent2.rgb * tear * 0.4) * c * _Tears * pulse * 2.0;
                col *= lerp(1.0, 0.85 + 0.15 * step(0.5, GHash11(floor(_Time.y * 6.0))), c * 0.6);

                // data aurora (High tiers): soft moving bands near the top
                if (_Aurora > 0.001)
                {
                    float band = sin(uv.x * 18.0 + _Time.y * 0.4 + sin(uv.x * 7.0 + _Time.y * 0.3) * 2.0);
                    float mask = smoothstep(0.55, 0.85, uv.y) * smoothstep(1.0, 0.8, uv.y);
                    float a = saturate(band * 0.5 + 0.5) * mask;
                    col += lerp(_RegionAccent2.rgb, _RegionAccent.rgb, c) * a * _Aurora * 0.35;
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
