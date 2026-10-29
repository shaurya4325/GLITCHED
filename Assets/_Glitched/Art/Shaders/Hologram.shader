// Hologram billboards: scrolling text/data, scanlines, flicker. Glitches harder when corrupted.
Shader "Glitched/Hologram"
{
    Properties
    {
        _BaseMap ("Content", 2D) = "white" {}
        [HDR] _BaseColor ("Color", Color) = (0.3,1.5,2,1)
        _Scroll ("Scroll Speed (xy)", Vector) = (0,0.15,0,0)
        _Scanlines ("Scanline Density", Float) = 120
        _Alpha ("Alpha", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
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
                half4 _BaseColor;
                float4 _Scroll;
                float _Scanlines;
                half _Alpha;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; float3 positionWS : TEXCOORD2; };
            V vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv; o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float c = EffectiveCorruption(i.positionWS.x);
                float2 uv = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _Scroll.xy * _Time.y;
                float row = floor(i.uv.y * 24.0);
                uv.x += (GHash21(float2(row, floor(_Time.y * 10.0))) - 0.5) * 0.25 * c * step(0.7, GHash21(float2(row, floor(_Time.y * 4.0))));
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float scan = 0.65 + 0.35 * sin(i.uv.y * _Scanlines + _Time.y * 6.0);
                float flick = lerp(1.0, step(0.3, GHash11(floor(_Time.y * 15.0) + i.positionWS.x)), 0.4 + 0.5 * c);
                half3 col = lerp(_BaseColor.rgb, _RegionAccent.rgb * 2.0, c * 0.6) * tex.rgb * scan * flick;
                float edge = smoothstep(0.0, 0.05, i.uv.x) * smoothstep(1.0, 0.95, i.uv.x) * smoothstep(0.0, 0.05, i.uv.y) * smoothstep(1.0, 0.95, i.uv.y);
                float a = saturate(tex.a * _Alpha * edge + 0.08 * edge);
                col = MixFog(col, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
