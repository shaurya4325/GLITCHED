// GLITCHED realistic master material (URP PBR): albedo, normal, roughness, occlusion, metallic, emission,
// optional world-space (triplanar) mapping for generated geometry, and the subtle corruption layer from GlitchPBR.hlsl.
Shader "Glitched/Realistic"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        _Roughness ("Roughness", Range(0,1)) = 0.8
        _Metallic ("Metallic", Range(0,1)) = 0
        _MetalFromMap ("Metallic from Roughness Map G (glTF)", Range(0,1)) = 0
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        [Toggle(_WORLDUV)] _WorldUV ("World-space UV (triplanar)", Float) = 0
        _WorldUVScale ("World UV Scale (tiles per metre)", Float) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Cutout (leaves)", Float) = 0
        _Cutoff ("Cutoff", Range(0,1)) = 0.5
        _Translucency ("Leaf Translucency", Range(0,1)) = 0
        _HitFlash ("Hit Flash", Range(0,1)) = 0
        _GlitchAmount ("Glitch Amount (characters)", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _Glitchiness ("Corruption Response", Range(0,2)) = 1
        _Seed ("Seed", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "GlitchPBR.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Roughness;
            half _Metallic;
            half _MetalFromMap;
            half _OcclusionStrength;
            half4 _EmissionColor;
            float _WorldUVScale;
            half _Glitchiness;
            float _Seed;
            half _Cutoff;
            half _Translucency;
            half _HitFlash;
            half _GlitchAmount;
            half _Dissolve;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_RoughnessMap);
        TEXTURE2D(_OcclusionMap);
        TEXTURE2D(_EmissionMap);

        float3 RealisticVertex(float3 positionOS)
        {
            float3 p = TransformObjectToWorld(positionOS);
            float c = max(EffectiveCorruption(p.x) * _Glitchiness, _GlitchAmount);
            return SubtleTear(p, c, GObjectSeed(_Seed));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _WORLDUV
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ GLITCH_LOW
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                half3 vertexLight : TEXCOORD5;
                float2 lightmapUV : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 posWS = RealisticVertex(v.positionOS.xyz);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.normalWS = n.normalWS;
                o.tangentWS = half4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                o.vertexLight = VertexLighting(posWS, n.normalWS);
                o.lightmapUV = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                return o;
            }

            // world-space projection for generated geometry: picks the dominant axis (cheap, no blending seams on boxes)
            float2 WorldUV(float3 p, float3 n)
            {
                float3 a = abs(n);
                float2 uv = a.y > a.x && a.y > a.z ? p.xz : (a.x > a.z ? p.zy : p.xy);
                return uv * _WorldUVScale;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 uv = i.uv;
                #if defined(_WORLDUV)
                    uv = WorldUV(i.positionWS, i.normalWS) * _BaseMap_ST.xy + _BaseMap_ST.zw;
                #endif
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                half4 rm = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_BaseMap, uv);   // r = roughness (g = metallic for glTF-packed maps)
                half rough = rm.r * _Roughness;
                half occ = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, uv).r, _OcclusionStrength);

                float sgn = i.tangentWS.w;
                float3 bitangent = sgn * cross(i.normalWS.xyz, i.tangentWS.xyz);
                half3x3 tbn = half3x3(i.tangentWS.xyz, bitangent, i.normalWS.xyz);
                #if defined(_WORLDUV)
                    half3 normalWS = normalize(i.normalWS);   // tangent frame does not match projected UVs
                    normalWS = normalize(normalWS + (nTS.x * normalize(i.tangentWS.xyz) + nTS.y * bitangent) * 0.5);
                #else
                    half3 normalWS = normalize(TransformTangentToWorld(nTS, tbn));
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = i.fogFactor;
                inputData.vertexLighting = i.vertexLight;
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SampleLightmap(i.lightmapUV, normalWS);   // GPU-baked bounce light (static geometry)
                #else
                    inputData.bakedGI = SampleSH(normalWS);                       // light probes / ambient
                #endif
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
                inputData.tangentToWorld = tbn;

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo.rgb;
                s.alpha = 1;
                s.metallic = _Metallic * lerp(1.0, rm.g, _MetalFromMap);
                s.smoothness = 1.0 - rough;
                s.normalTS = nTS;
                s.occlusion = occ;
                s.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, uv).rgb * _EmissionColor.rgb;
                s.specular = half3(0, 0, 0);

                half4 col = UniversalFragmentPBR(inputData, s);
                if (_Translucency > 0)
                {
                    Light ml = GetMainLight(inputData.shadowCoord);
                    half back = saturate(dot(-inputData.viewDirectionWS, ml.direction));
                    col.rgb += albedo.rgb * ml.color * pow(back, 3.0) * _Translucency * ml.shadowAttenuation;
                }
                float c = max(EffectiveCorruption(i.positionWS.x) * _Glitchiness, _GlitchAmount);
                col.rgb = SubtleCorruption(col.rgb, i.positionWS, c, GObjectSeed(_Seed));
                col.rgb += RestorationGlow(i.positionWS);
                // characters: dissolve into small voxels when defeated, white flash on hits
                if (_Dissolve > 0.001)
                {
                    float vox = GHash31(floor(i.positionWS * 14.0));
                    clip(vox - _Dissolve);
                    col.rgb += _RegionAccent.rgb * step(vox, _Dissolve + 0.04) * 2.0;
                }
                col.rgb = lerp(col.rgb, half3(1, 1, 1), _HitFlash);
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }

        // Meta: albedo and emission for the GPU lightmapper, so baked bounce light picks up the surfaces' colours
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertMeta
            #pragma fragment fragMeta
            #pragma shader_feature_local _WORLDUV
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct MA { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; float2 uv2 : TEXCOORD2; };
            struct MV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; };

            MV vertMeta(MA v)
            {
                MV o;
                o.positionCS = UnityMetaVertexPosition(v.positionOS.xyz, v.uv1, v.uv2, unity_LightmapST, unity_DynamicLightmapST);
                o.uv = TRANSFORM_TEX(v.uv0, _BaseMap);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 fragMeta(MV i) : SV_Target
            {
                float2 uv = i.uv;
                #if defined(_WORLDUV)
                    float3 a = abs(i.normalWS);
                    uv = (a.y > a.x && a.y > a.z ? i.positionWS.xz : (a.x > a.z ? i.positionWS.zy : i.positionWS.xy)) * _WorldUVScale * _BaseMap_ST.xy + _BaseMap_ST.zw;
                #endif
                MetaInput m = (MetaInput)0;
                m.Albedo = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor).rgb;
                m.Emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, uv).rgb * _EmissionColor.rgb;
                return UnityMetaFragment(m);
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
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vertShadow(A v)
            {
                V o;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = RealisticVertex(v.positionOS.xyz);
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
            half4 fragShadow(V i) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
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
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vertDepth(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformWorldToHClip(RealisticVertex(v.positionOS.xyz)); o.uv = TRANSFORM_TEX(v.uv, _BaseMap); return o; }
            half fragDepth(V i) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return i.positionCS.z;
            }
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
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            V vertDN(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionCS = TransformWorldToHClip(RealisticVertex(v.positionOS.xyz));
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 fragDN(V i) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                #endif
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
    FallBack Off   // a fallback would pull every URP/Lit variant into builds (always-included shader)
}
