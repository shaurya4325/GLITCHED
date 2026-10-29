// Full-screen UI overlay glitch: scanline tearing, colour fringes and noise blocks.
// On Potato/Low tiers this replaces post-processing; on all tiers it powers transitions and impacts.
Shader "Glitched/UIGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _Accent ("Accent", Color) = (1,0.24,0.78,1)
        _Accent2 ("Accent 2", Color) = (0.25,0.92,1,1)
        _Scanlines ("Always-on Scanlines", Range(0,1)) = 0.06
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest Always Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Intensity, _Scanlines, _ScreenGlitch;
            fixed4 _Accent, _Accent2;

            float H11(float p) { p = frac(p * 0.1031); p *= p + 33.33; p *= p + p; return frac(p); }
            float H21(float2 p) { float3 p3 = frac(float3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float k = saturate(_Intensity + _ScreenGlitch);
                float2 uv = i.uv;
                float t = floor(_Time.y * 24.0);
                float row = floor(uv.y * 64.0);
                float band = step(1.0 - 0.18 * k, H21(float2(floor(uv.y * 18.0), t)));
                float shift = (H21(float2(row, t)) - 0.5) * 0.2 * k * band;
                float block = step(1.0 - 0.025 * k * k * k, H21(floor((uv + float2(shift, 0)) * float2(64, 36)) + t));   // rare unless the hit is huge
                float scan = (0.5 + 0.5 * sin(uv.y * 900.0)) * _Scanlines;
                float stripe = step(0.5, frac(uv.x * 3.0 + shift * 10.0));
                float tear = band * k;
                float3 col = _Accent.rgb * tear * stripe + _Accent2.rgb * tear * (1 - stripe);
                col += lerp(_Accent.rgb, _Accent2.rgb, H11(t)) * block;
                float a = saturate(scan * (0.4 + k) + tear * 0.1 + block * 0.3 + k * 0.03);
                return fixed4(col, a * i.color.a);
            }
            ENDCG
        }
    }
}
