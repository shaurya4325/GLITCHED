// Final Void floor: an emissive cyan grid on black that pulses with the region-swap glitch.
Shader "Glitched/Grid"
{
    Properties
    {
        _BaseColor ("Base", Color) = (0.01,0.01,0.03,1)
        [HDR] _LineColor ("Line", Color) = (0.2,1.6,2.0,1)
        _Spacing ("Spacing", Float) = 2
        _Width ("Line Width", Range(0.001,0.2)) = 0.04
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GlitchCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _LineColor;
                float _Spacing, _Width;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float fog : TEXCOORD1; };
            V vert(A v) { V o; o.positionWS = TransformObjectToWorld(v.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); o.fog = ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(V i) : SV_Target
            {
                float2 p = i.positionWS.xz / _Spacing;
                float2 g = abs(frac(p - 0.5) - 0.5) / fwidth(p);
                float l = 1.0 - saturate(min(g.x, g.y) * _Width * 10.0);
                float pulse = 0.6 + 0.4 * sin(_Time.y * 2.0 - length(i.positionWS.xz) * 0.15);
                half3 lc = lerp(_LineColor.rgb, _RegionAccent.rgb * 2.0, step(0.85, GHash11(floor(_Time.y * 3.0))) * _Corruption);
                half3 col = _BaseColor.rgb + lc * l * pulse;
                return half4(MixFog(col, i.fog), 1);
            }
            ENDHLSL
        }
    }
}
