// GLITCHED water / liquids: animated procedural wave normals (no texture), strong fresnel reflections of the sky
// and city through the reflection probe, a deep body colour, optional flow direction. Opaque (no sorting issues).
Shader "Glitched/Water"
{
    Properties
    {
        _DeepColor ("Deep Colour", Color) = (0.03,0.07,0.08,1)
        _ShallowColor ("Shallow Colour", Color) = (0.08,0.16,0.16,1)
        _WaveScale ("Wave Size (m)", Float) = 3
        _WaveStrength ("Wave Strength", Range(0,1)) = 0.35
        _Flow ("Flow (xz m/s)", Vector) = (0.4,0,0.1,0)
        _Smoothness ("Smoothness", Range(0,1)) = 0.93
        [HDR] _EmissionColor ("Glow (chemicals)", Color) = (0,0,0,1)
        _Glitchiness ("Corruption Response", Range(0,2)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "GlitchPBR.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _DeepColor, _ShallowColor, _EmissionColor;
            float _WaveScale; half _WaveStrength; float4 _Flow; half _Smoothness; half _Glitchiness;
        CBUFFER_END
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half fogFactor : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // sum of a few drifting waves -> height gradient -> normal
            float2 WaveGrad(float2 p, float t)
            {
                float2 g = 0;
                float2 dirs[4] = { float2(1, 0.3), float2(-0.4, 1), float2(0.7, -0.8), float2(-1, -0.2) };
                float freq[4] = { 1.0, 1.7, 2.9, 4.3 };
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float2 d = normalize(dirs[k]);
                    float f = freq[k] / _WaveScale;
                    float ph = dot(d, p) * f * 6.2831 + t * (1.2 + k * 0.4);
                    g += d * cos(ph) * f / (k + 1.0);
                }
                return g;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 p = i.positionWS;
                float2 q = p.xz - _Flow.xz * _Time.y;
                float2 g = WaveGrad(q, _Time.y) * _WaveStrength;
                half3 N = normalize(half3(-g.x, 1, -g.y));
                float3 V = GetWorldSpaceNormalizeViewDir(p);
                float fres = pow(1.0 - saturate(dot(N, V)), 4.0);
                InputData inputData = (InputData)0;
                inputData.positionWS = p;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(p);
                inputData.fogCoord = i.fogFactor;
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
                SurfaceData s = (SurfaceData)0;
                s.albedo = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.5 + 0.5 * sin(dot(q, float2(0.05, 0.07))));
                s.alpha = 1; s.metallic = 0; s.occlusion = 1;
                s.smoothness = _Smoothness;
                s.normalTS = half3(0, 0, 1);
                s.emission = _EmissionColor.rgb * (0.6 + 0.4 * (1 - fres));
                half4 col = UniversalFragmentPBR(inputData, s);
                float c = EffectiveCorruption(p.x) * _Glitchiness;
                col.rgb = SubtleCorruption(col.rgb, p, c, 0.37);
                col.rgb += RestorationGlow(p);
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return half4(col.rgb, 1);
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
