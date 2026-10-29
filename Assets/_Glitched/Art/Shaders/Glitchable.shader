// GLITCHED "Glitchable" master material for environment props (Section 3.3).
// Stylised (toon-ish, low contrast) lighting + corruption: vertex slice jitter, RGB-split scan bands,
// dissolve-to-voxels edges, saturation loss, window flicker, and the restoration wave.
// The GLITCH_LOW keyword (Potato/Low tiers) keeps only colour + UV jitter.
Shader "Glitched/Glitchable"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _AlbedoContrast ("Albedo Contrast", Range(0,1)) = 0.7
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission Mask", 2D) = "white" {}
        _Glitchiness ("Glitchiness", Range(0,2)) = 1
        _ToonSteps ("Toon Steps", Range(1,6)) = 3
        _ToonBlend ("Toon Blend", Range(0,1)) = 0.55
        _ShadeLift ("Shade Lift", Range(0,1)) = 0.35
        _RimStrength ("Rim Strength", Range(0,2)) = 0.35
        _Dissolve ("Dissolve At High Corruption", Range(0,1)) = 0.25
        _WindowFlicker ("Window Flicker", Range(0,1)) = 0
        _WindowCell ("Window Cell Size", Float) = 1.5
        _Seed ("Seed", Float) = 0
        [Toggle(_TRIPLANAR)] _Triplanar ("World-space UV (no UVs needed)", Float) = 0
        _TriplanarScale ("World UV Scale", Float) = 0.25
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "GlitchCommon.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _EmissionMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half _AlbedoContrast;
            half _Glitchiness;
            half _ToonSteps;
            half _ToonBlend;
            half _ShadeLift;
            half _RimStrength;
            half _Dissolve;
            half _WindowFlicker;
            float _WindowCell;
            float _Seed;
            float _TriplanarScale;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

        float ObjectSeed()
        {
            float3 o = TransformObjectToWorld(float3(0, 0, 0));
            return frac(GHash31(floor(o * 0.37)) + _Seed);
        }

        float3 GlitchVertex(float3 positionWS, out float c)
        {
            c = EffectiveCorruption(positionWS.x) * _Glitchiness;
            #if !defined(GLITCH_LOW)
                positionWS.x += SliceOffset(positionWS, c, ObjectSeed());
            #endif
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ GLITCH_LOW
            #pragma shader_feature_local _TRIPLANAR
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 data : TEXCOORD3;      // x = corruption, y = fog, z = seed
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float c;
                float3 posWS = GlitchVertex(TransformObjectToWorld(v.positionOS.xyz), c);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.data = float4(c, ComputeFogFactor(o.positionCS.z), ObjectSeed(), 0);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float c = i.data.x;
                float seed = i.data.z;
                float3 N = normalize(i.normalWS);
                float2 uv = i.uv;
                #if defined(_TRIPLANAR)
                    float3 an = abs(N);
                    uv = (an.x > an.y && an.x > an.z) ? i.positionWS.zy : (an.y > an.z ? i.positionWS.xz : i.positionWS.xy);
                    uv *= _TriplanarScale;
                #endif

                #if defined(GLITCH_LOW)
                    uv.x += (GHash11(floor(_Time.y * 10.0) + seed * 17.0) - 0.5) * 0.04 * c;
                #else
                    // dissolve-to-voxels at high corruption
                    // chunks go missing only at very high corruption, and only a few of them
                    float vox = GHash31(floor(i.positionWS * 1.25) + seed);
                    float dis = saturate((c - 0.8) * 5.0) * _Dissolve * 0.3;
                    clip(vox - dis);
                #endif

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 albedo = tex.rgb * _BaseColor.rgb * i.color.rgb;
                albedo = lerp(dot(albedo, half3(0.33, 0.33, 0.34)).xxx, albedo, 0.6 + 0.4 * _AlbedoContrast);   // reduced contrast, stylised
                albedo = lerp(albedo * 0.8 + 0.1 * _BaseColor.rgb, albedo, _AlbedoContrast);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light light = GetMainLight(shadowCoord);
                float ndl = dot(N, light.direction);
                float wrap = saturate(ndl * 0.5 + 0.5);
                float stepped = floor(wrap * _ToonSteps + 0.5) / max(_ToonSteps, 1);
                float lit = lerp(wrap, stepped, _ToonBlend);
                lit = lerp(_ShadeLift, 1.0, lit) * lerp(0.55, 1.0, light.shadowAttenuation);
                half3 ambient = SampleSH(N);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(dot(N, V)), 4.0) * _RimStrength;
                half3 col = albedo * (light.color * lit + ambient) + rim * ambient * 0.8;

                // corruption: desaturate, darken, glitch bands
                col = GDesaturate(col, c * 0.72) * lerp(1.0, 0.62, c);
                col = lerp(col, col * float3(0.6, 0.65, 1.0), c * 0.35);

                // emission (window flicker in patterns, driven by material property, not per-window objects)
                half3 emiMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
                half3 emission = _EmissionColor.rgb * emiMask;
                if (_WindowFlicker > 0.001)
                {
                    // a real window grid on the wall plane; each window switches on/off slowly (faster when corrupted)
                    float3 an2 = abs(N);
                    float2 wp = (an2.x > an2.z) ? i.positionWS.zy : i.positionWS.xy;
                    float2 g = wp / max(_WindowCell, 0.1);
                    float2 f = frac(g);
                    float win = step(0.22, f.x) * step(f.x, 0.78) * step(0.3, f.y) * step(f.y, 0.78) * step(an2.y, 0.5);
                    float h = GHash21(floor(g) + seed * 5.0);
                    float on = step(0.45, frac(h * 7.3 + _Time.y * (0.01 + h * 0.03 + c * 0.08)));
                    emission *= lerp(1.0, win * on, _WindowFlicker);
                }
                float flickerC = lerp(1.0, step(0.2, GHash11(floor(_Time.y * 6.0) + seed * 31.0)), c * 0.3);
                emission *= flickerC;

                #if !defined(GLITCH_LOW)
                    emission += GlitchBands(i.positionWS, c, seed) * 0.5;
                    float edge = step(dis, vox) * step(vox, dis + 0.02) * step(0.001, dis);
                    emission += _RegionAccent.rgb * edge * 1.5;
                #endif
                emission += float3(0.5, 1.0, 1.0) * RestoreGlow(i.positionWS.x) * 1.6;

                col += emission;
                float viewDist = length(GetCameraPositionWS() - i.positionWS);
                col = ApplyHeightFog(col, i.positionWS, viewDist);
                col = MixFog(col, i.data.y);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile _ GLITCH_LOW
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };

            V vertShadow(A v)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(v);
                float c;
                float3 posWS = GlitchVertex(TransformObjectToWorld(v.positionOS.xyz), c);
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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing
            #pragma multi_compile _ GLITCH_LOW
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A v)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(v);
                float c;
                o.positionCS = TransformWorldToHClip(GlitchVertex(TransformObjectToWorld(v.positionOS.xyz), c));
                return o;
            }
            half fragDepth(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma multi_compile_instancing
            #pragma multi_compile _ GLITCH_LOW
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            V vertDN(A v)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(v);
                float c;
                o.positionCS = TransformWorldToHClip(GlitchVertex(TransformObjectToWorld(v.positionOS.xyz), c));
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 fragDN(V i) : SV_Target { return half4(normalize(i.normalWS) * 0.5 + 0.5, 0); }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
