// GLITCHED character toon shader: 2-3 band cel shading, rim light, inverted-hull outline (~0.02),
// hit flash, and a per-material glitch (RGB split + slice tearing + dissolve) for enemies and bosses glitching apart.
// Applied to every character so cartoony and realistic models look like they belong in the same game.
Shader "Glitched/Toon"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade Color", Color) = (0.55,0.5,0.75,1)
        _Bands ("Bands", Range(2,3)) = 3
        _BandSoftness ("Band Softness", Range(0.001,0.2)) = 0.03
        _RimColor ("Rim Color", Color) = (0.6,0.9,1,1)
        _RimPower ("Rim Power", Range(1,8)) = 3.5
        _RimStrength ("Rim Strength", Range(0,2)) = 0.6
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _OutlineColor ("Outline Color", Color) = (0.08,0.05,0.12,1)
        _OutlineWidth ("Outline Width", Range(0,0.08)) = 0.02
        _HitFlash ("Hit Flash", Range(0,1)) = 0
        _GlitchAmount ("Glitch Amount", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "GlitchCommon.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _Bands;
            half _BandSoftness;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half4 _EmissionColor;
            half4 _OutlineColor;
            float _OutlineWidth;
            half _HitFlash;
            half _GlitchAmount;
            half _Dissolve;
            half4 _Tint;
        CBUFFER_END

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        float3 ToonGlitchVertex(float3 posWS)
        {
            float g = _GlitchAmount;
            posWS.x += SliceOffset(posWS, g * 1.2, 0.37);
            return posWS;
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; float fog : TEXCOORD3; float4 screenPos : TEXCOORD4; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = ToonGlitchVertex(TransformObjectToWorld(v.positionOS.xyz));
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float vox = GHash31(floor(i.positionWS * 6.0));
                clip(vox - _Dissolve);

                float2 uv = i.uv;
                float g = _GlitchAmount;
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 albedo = tex.rgb * _BaseColor.rgb * _Tint.rgb;
                if (g > 0.001)
                {
                    // RGB channel split: sample shifted copies for red and blue
                    float s = 0.02 * g;
                    albedo.r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(s, 0)).r * _BaseColor.r;
                    albedo.b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv - float2(s, 0)).b * _BaseColor.b;
                }

                float3 N = normalize(i.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = dot(N, light.direction) * 0.5 + 0.5;
                ndl *= lerp(0.5, 1.0, light.shadowAttenuation);
                float b1 = smoothstep(0.42 - _BandSoftness, 0.42 + _BandSoftness, ndl);
                float b2 = smoothstep(0.75 - _BandSoftness, 0.75 + _BandSoftness, ndl);
                float lit = _Bands > 2.5 ? (b1 * 0.6 + b2 * 0.4) : b1;
                half3 shade = albedo * _ShadeColor.rgb;
                half3 col = lerp(shade, albedo, lit) * (light.color * 0.85 + 0.15) + albedo * SampleSH(N) * 0.45;

                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;
                col += _RimColor.rgb * rim * (0.5 + 0.5 * lit);
                col += _EmissionColor.rgb;

                if (g > 0.001)
                {
                    col += GlitchBands(i.positionWS, g, 0.71) * 1.5;
                    float flick = step(0.7, GHash11(floor(_Time.y * 20.0)));
                    col = lerp(col, col * float3(1.4, 0.4, 1.4), flick * g * 0.5);
                }
                float edge = saturate(1.0 - abs(vox - _Dissolve) * 10.0) * step(0.001, _Dissolve);
                col += _RegionAccent.rgb * edge * 3.0;
                col = lerp(col, half3(1, 1, 1), _HitFlash);
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float fog : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            V vertOutline(A v)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = ToonGlitchVertex(TransformObjectToWorld(v.positionOS.xyz));
                float3 nWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                float dist = length(GetCameraPositionWS() - posWS);
                posWS += nWS * _OutlineWidth * clamp(dist / 18.0, 0.6, 2.0);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 fragOutline(V i) : SV_Target
            {
                float vox = GHash31(floor(i.positionWS * 6.0));
                clip(vox - _Dissolve);
                half3 col = lerp(_OutlineColor.rgb, half3(1, 1, 1), _HitFlash * 0.6);
                col = lerp(col, _RegionAccent.rgb, _GlitchAmount * 0.6);
                return half4(MixFog(col, i.fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
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
                float3 posWS = ToonGlitchVertex(TransformObjectToWorld(v.positionOS.xyz));
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
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformWorldToHClip(ToonGlitchVertex(TransformObjectToWorld(v.positionOS.xyz))); return o; }
            half fragDepth(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
