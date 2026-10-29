// GLITCHED realistic terrain: grass, rock and snow blended by slope and height (world-space UVs, no seams),
// PBR lit, with the subtle corruption layer. Used for the valley hills, cliffs and far mountains.
Shader "Glitched/Terrain"
{
    Properties
    {
        _GrassMap ("Grass Albedo", 2D) = "white" {}
        [Normal] _GrassNormal ("Grass Normal", 2D) = "bump" {}
        _RockMap ("Rock Albedo", 2D) = "white" {}
        [Normal] _RockNormal ("Rock Normal", 2D) = "bump" {}
        _SnowColor ("Snow Colour", Color) = (0.92,0.94,0.97,1)
        _GrassTint ("Grass Tint", Color) = (1,1,1,1)
        _RockTint ("Rock Tint", Color) = (1,1,1,1)
        _GrassTile ("Grass Tile (m)", Float) = 4
        _RockTile ("Rock Tile (m)", Float) = 8
        _SlopeStart ("Rock From Slope", Range(0,1)) = 0.55
        _SlopeEnd ("Rock Full Slope", Range(0,1)) = 0.75
        _SnowHeight ("Snow From Height", Float) = 160
        _SnowFade ("Snow Fade", Float) = 40
        _Glitchiness ("Corruption Response", Range(0,2)) = 1
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "GlitchPBR.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _SnowColor, _GrassTint, _RockTint;
            float _GrassTile, _RockTile;
            half _SlopeStart, _SlopeEnd;
            float _SnowHeight, _SnowFade;
            half _Glitchiness;
            float _Seed;
        CBUFFER_END
        TEXTURE2D(_GrassMap); SAMPLER(sampler_GrassMap);
        TEXTURE2D(_GrassNormal);
        TEXTURE2D(_RockMap);
        TEXTURE2D(_RockNormal);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ GLITCH_LOW
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half fogFactor : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float c = EffectiveCorruption(p.x) * _Glitchiness;
                p = SubtleTear(p, c, GObjectSeed(_Seed));
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 N = normalize(i.normalWS);
                float3 p = i.positionWS;
                // world-space mapping: grass from above, rock projected on the dominant side axis
                float2 uvG = p.xz / _GrassTile;
                float2 uvR = (abs(N.x) > abs(N.z) ? p.zy : p.xy) / _RockTile;
                float2 uvRTop = p.xz / _RockTile;
                half3 grass = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, uvG).rgb * _GrassTint.rgb;
                // break up tiling with a large-scale variation
                float macro = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, p.xz / (_GrassTile * 23.0)).g;
                grass *= lerp(0.82, 1.12, macro);
                half3 rockSide = SAMPLE_TEXTURE2D(_RockMap, sampler_GrassMap, uvR).rgb;
                half3 rockTop = SAMPLE_TEXTURE2D(_RockMap, sampler_GrassMap, uvRTop).rgb;
                float upness = saturate(N.y);
                half3 rock = lerp(rockSide, rockTop, upness * upness) * _RockTint.rgb;
                float slope = 1.0 - upness;
                float r = smoothstep(_SlopeStart, _SlopeEnd, slope + (macro - 0.5) * 0.12);
                float snow = saturate((p.y - _SnowHeight) / max(_SnowFade, 1.0)) * smoothstep(0.35, 0.85, upness);
                half3 albedo = lerp(lerp(grass, rock, r), _SnowColor.rgb, snow);
                half3 nG = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassMap, uvG));
                half3 nR = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal, sampler_GrassMap, uvR));
                half3 nT = normalize(lerp(nG, nR, r) * half3(1, 1, 1) + half3(0, 0, snow * 2.0));
                // approximate a tangent frame from the world normal (good enough for detail normals)
                half3 T = normalize(cross(N, abs(N.y) < 0.99 ? half3(0, 1, 0) : half3(1, 0, 0)));
                half3 B = cross(N, T);
                half3 Nd = normalize(T * nT.x + B * nT.y + N * nT.z);

                InputData inputData = (InputData)0;
                inputData.positionWS = p;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = Nd;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(p);
                inputData.shadowCoord = TransformWorldToShadowCoord(p);
                inputData.fogCoord = i.fogFactor;
                inputData.bakedGI = SampleSH(Nd);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo; s.alpha = 1; s.metallic = 0; s.occlusion = 1;
                s.smoothness = lerp(lerp(0.12, 0.2, r), 0.45, snow);
                s.normalTS = half3(0, 0, 1);
                half4 col = UniversalFragmentPBR(inputData, s);
                float c = EffectiveCorruption(p.x) * _Glitchiness;
                col.rgb = SubtleCorruption(col.rgb, p, c, GObjectSeed(_Seed));
                col.rgb += RestorationGlow(p);
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; };
            V vertShadow(A v)
            {
                V o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 fragShadow(V i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half fragDepth(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; };
            V vertDN(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 fragDN(V i) : SV_Target
            {
                half3 n = NormalizeNormalPerPixel(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Glitched/Realistic"
}
