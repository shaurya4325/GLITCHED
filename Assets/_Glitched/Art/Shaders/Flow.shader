// Scrolling surfaces: acid pools, blood rivers, upward waterfalls, data streams.
Shader "Glitched/Flow"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Flow ("Flow Speed (xy)", Vector) = (0.05, 0, 0, 0)
        _Wobble ("Wobble", Range(0,0.2)) = 0.03
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GlitchCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _EmissionColor;
                float4 _Flow;
                half _Wobble, _Alpha;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; float3 positionWS : TEXCOORD2; };
            V vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                p.y += sin(p.x * 1.3 + _Time.y * 2.0) * _Wobble;
                o.positionWS = p; o.positionCS = TransformWorldToHClip(p);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float c = EffectiveCorruption(i.positionWS.x);
                float2 uv = i.uv + _Flow.xy * _Time.y;
                uv += sin(uv.yx * 6.0 + _Time.y) * 0.01;
                half4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half4 t2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * 0.6 - _Flow.xy * _Time.y * 0.5);
                half3 col = (t.rgb * 0.6 + t2.rgb * 0.4) * _BaseColor.rgb + _EmissionColor.rgb;
                col = lerp(col, GDesaturate(col, 0.5) + _RegionAccent.rgb * 0.15, c * 0.5);
                col = MixFog(col, i.fog);
                return half4(col, _Alpha * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
