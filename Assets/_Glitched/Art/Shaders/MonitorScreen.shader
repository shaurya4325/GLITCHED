// GLITCHED monitor screen: shows a live game feed (render texture) with a controllable in-screen glitch
// (row tearing, RGB split, blocky corruption, scanlines), a glass crack overlay, and an error image fade.
// Driven by MonitorScreenFx during the opening cinematic.
Shader "Glitched/MonitorScreen"
{
    Properties
    {
        _MainTex ("Feed", 2D) = "black" {}
        _ErrorTex ("Error Image", 2D) = "black" {}
        _CrackTex ("Crack (alpha)", 2D) = "black" {}
        _Brightness ("Brightness", Float) = 1.6
        _Glitch ("Glitch", Range(0,1)) = 0
        _Error ("Error Image Blend", Range(0,1)) = 0
        _Crack ("Crack", Range(0,1)) = 0
        _Accent ("Accent", Color) = (1,0.25,0.8,1)
        _Accent2 ("Accent 2", Color) = (0.3,0.9,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GlitchCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Brightness, _Glitch, _Error, _Crack;
                half4 _Accent, _Accent2;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_ErrorTex);
            TEXTURE2D(_CrackTex);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o; }

            half4 frag(V i) : SV_Target
            {
                float2 uv = i.uv;
                float g = _Glitch;
                float t = floor(_Time.y * 18.0);
                // row tearing: bands of rows shift sideways
                float row = floor(uv.y * 48.0);
                float band = step(1.0 - 0.45 * g, GHash21(float2(floor(uv.y * 12.0), t)));
                uv.x += (GHash21(float2(row, t)) - 0.5) * 0.18 * g * band;
                // RGB split
                float s = 0.006 + 0.03 * g;
                half3 col;
                col.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(s * g, 0)).r;
                col.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                col.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(s * g, 0)).b;
                half3 err = SAMPLE_TEXTURE2D(_ErrorTex, sampler_MainTex, uv).rgb;
                col = lerp(col, err, _Error);
                // blocky corruption
                float2 cell = floor(uv * float2(32, 14));
                float block = step(1.0 - 0.25 * g * g, GHash21(cell + t));
                col = lerp(col, lerp(_Accent.rgb, _Accent2.rgb, GHash21(cell * 1.7 + t)), block * 0.85);
                // scanlines and slight vignette
                col *= 0.9 + 0.1 * sin(i.uv.y * 900.0);
                float2 d = i.uv - 0.5;
                col *= 1.0 - dot(d, d) * 0.6;
                col *= _Brightness;
                // cracked glass: dark fractures with bright edges, a dead pixel cluster at the impact
                half crack = SAMPLE_TEXTURE2D(_CrackTex, sampler_MainTex, i.uv).a * _Crack;
                col = lerp(col, half3(0, 0, 0), crack * 0.85);
                col += crack * (1.0 - crack) * 2.5 * half3(0.9, 0.95, 1.0);
                return half4(col, 1);
            }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half fragDepth(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }
}
