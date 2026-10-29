// Full-screen post glitch (Medium tier and up), used by a URP Full Screen Pass Renderer Feature.
// Real colour-channel split + slice tearing of the rendered image on impacts, wrong answers and transitions.
Shader "Glitched/FullscreenGlitch"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "FullscreenGlitch"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "GlitchCommon.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float k = saturate(_ScreenGlitch);
                if (k < 0.002) return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float t = floor(_Time.y * 30.0);
                float slice = floor(uv.y * 22.0);
                float on = step(1.0 - 0.25 * k, GHash21(float2(slice, t)));
                uv.x += (GHash21(float2(slice + 7.0, t)) - 0.5) * 0.12 * k * on;
                float split = 0.012 * k + 0.02 * k * on;
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(split, 0)).r;
                half2 gb = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).ga;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(split, 0)).b;
                half3 col = half3(r, gb.x, b);
                float scan = 1.0 - 0.12 * k * (0.5 + 0.5 * sin(uv.y * _ScreenParams.y * 1.2));
                col *= scan;
                float block = step(1.0 - 0.02 * k * k * k, GHash21(floor(uv * float2(64, 36)) + t));
                col = lerp(col, _RegionAccent.rgb, block * 0.35);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
