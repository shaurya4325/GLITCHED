// Far skyline impostor cards (L4): a camera-facing (Y axis) card with a procedural tower silhouette,
// lit windows and fog. Extremely cheap, so even Potato keeps a skyline.
Shader "Glitched/Impostor"
{
    Properties
    {
        _BaseColor ("Body Color", Color) = (0.12,0.14,0.25,1)
        _TopColor ("Top Color", Color) = (0.2,0.22,0.4,1)
        [HDR] _WindowColor ("Window Color", Color) = (1.2,1.0,0.6,1)
        _WindowGrid ("Window Grid (cols, rows)", Vector) = (6, 18, 0, 0)
        _WindowDensity ("Window Density", Range(0,1)) = 0.45
        _Taper ("Top Taper", Range(0,0.5)) = 0.08
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
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
                half4 _BaseColor, _TopColor, _WindowColor;
                float4 _WindowGrid;
                half _WindowDensity, _Taper;
                float _Seed;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; float3 positionWS : TEXCOORD2; float seed : TEXCOORD3; };
            V vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                float3 centre = TransformObjectToWorld(float3(0, 0, 0));
                float3 scale = float3(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21), 1);
                float3 toCam = GetCameraPositionWS() - centre; toCam.y = 0; toCam = normalize(toCam);
                float3 right = normalize(cross(float3(0, 1, 0), toCam));
                float3 posWS = centre + right * v.positionOS.x * scale.x + float3(0, 1, 0) * v.positionOS.y * scale.y;
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                o.seed = GHash31(floor(centre)) + _Seed;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float2 uv = i.uv;
                float taper = _Taper * smoothstep(0.7, 1.0, uv.y);
                clip(min(uv.x - taper, 1.0 - taper - uv.x));
                float c = EffectiveCorruption(i.positionWS.x);
                float2 g = _WindowGrid.xy;
                float2 cell = floor(uv * g);
                float2 f = frac(uv * g);
                float win = step(0.25, f.x) * step(f.x, 0.75) * step(0.3, f.y) * step(f.y, 0.75);
                float h = GHash21(cell + i.seed * 31.0);
                float on = step(1.0 - _WindowDensity, frac(h + _Time.y * 0.02 * (1.0 + c * 8.0) * h));
                half3 col = lerp(_BaseColor.rgb, _TopColor.rgb, uv.y);
                col = GDesaturate(col, c * 0.5);
                half3 wc = lerp(_WindowColor.rgb, _RegionAccent.rgb * 1.5, c * step(0.5, h));
                col += wc * win * on * (1.0 - c * 0.3 * step(0.5, GHash11(floor(_Time.y * 8.0) + h)));
                col += float3(0.5, 1, 1) * RestoreGlow(i.positionWS.x) * 0.5;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
