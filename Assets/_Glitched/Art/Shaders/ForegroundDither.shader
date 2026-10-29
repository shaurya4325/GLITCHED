// L0 foreground occluders (pipes, railings, cables, leaves). They dither out where they overlap the player,
// so the player is never hidden. No per-object script: the player's screen position is a global.
Shader "Glitched/ForegroundDither"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (0.1,0.1,0.15,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Darken ("Silhouette Darkening", Range(0,1)) = 0.55
        _Bands ("Glitch Bands", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest+20" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "GlitchCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _EmissionColor;
                half _Darken, _Bands;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 screenPos : TEXCOORD2; float fog : TEXCOORD3; float3 positionWS : TEXCOORD4; };
            V vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            static const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            half4 frag(V i) : SV_Target
            {
                float2 sp = i.screenPos.xy / i.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 d = (sp - _PlayerScreenPos.xy) * float2(aspect, 1);
                float fade = saturate((length(d) - _PlayerScreenPos.z) / max(_PlayerScreenPos.z * 0.6, 0.01));
                fade = lerp(1.0, fade, _PlayerScreenPos.w);
                uint2 px = uint2(sp * _ScreenParams.xy) % 4;
                float threshold = (bayer[px.y * 4 + px.x] + 0.5) / 16.0;
                clip(fade - threshold * 0.999);
                half4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                Light l = GetMainLight();
                float ndl = saturate(dot(normalize(i.normalWS), l.direction) * 0.5 + 0.5);
                half3 col = t.rgb * (l.color * ndl * 0.6 + SampleSH(i.normalWS)) * (1.0 - _Darken) + _EmissionColor.rgb;
                float c = EffectiveCorruption(i.positionWS.x);
                col += GlitchBands(i.positionWS, c * 0.6 * _Bands, 0.3);
                return half4(MixFog(col, i.fog), 1);
            }
            ENDHLSL
        }
    }
}
