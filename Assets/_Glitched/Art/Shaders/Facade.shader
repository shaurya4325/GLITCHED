// GLITCHED building facade: turns a plain box (UVs in metres: u along the wall, v up from the base) into a realistic
// building. Window grid per floor/bay, metal frames, textured wall between, reflective glass, and interior mapping
// (each window looks into a 3D room with walls, ceiling light and blinds). Curtain-wall mode makes glass towers.
// Corruption is subtle: drained colour, a rare soft scan line, and now and then one room glowing in the region accent.
Shader "Glitched/Facade"
{
    Properties
    {
        _WallMap ("Wall Albedo", 2D) = "white" {}
        [Normal] _WallNormal ("Wall Normal", 2D) = "bump" {}
        _WallRoughnessMap ("Wall Roughness", 2D) = "white" {}
        _WallColor ("Wall Tint", Color) = (1,1,1,1)
        _WallTile ("Wall Texture Size (m)", Float) = 3
        _WallRoughness ("Wall Roughness Scale", Range(0,1)) = 0.9
        _FloorHeight ("Floor Height (m)", Float) = 3.4
        _BayWidth ("Bay Width (m)", Float) = 3
        _WindowW ("Window Width (fraction)", Range(0.1,1)) = 0.62
        _WindowH ("Window Height (fraction)", Range(0.1,1)) = 0.56
        _Sill ("Sill Height (fraction)", Range(0,0.6)) = 0.24
        _Frame ("Frame Width (m)", Float) = 0.07
        _FrameColor ("Frame Color", Color) = (0.12,0.13,0.14,1)
        _CurtainWall ("Curtain Wall (glass tower)", Range(0,1)) = 0
        _Mullion ("Curtain Mullion Every (m)", Float) = 1.5
        _GroundFloor ("Ground Floor Storefront", Range(0,1)) = 1
        _GlassTint ("Glass Tint", Color) = (0.16,0.2,0.24,1)
        _GlassSmoothness ("Glass Smoothness", Range(0,1)) = 0.95
        _InteriorDepth ("Room Depth (m)", Float) = 4
        _RoomLit ("Lit Room Fraction", Range(0,1)) = 0.45
        _LightIntensity ("Room Light Intensity", Float) = 1.6
        _RoomWarm ("Lit Room Colour", Color) = (1,0.86,0.66,1)
        _RoomCool ("Office Room Colour", Color) = (0.86,0.93,1,1)
        _RoomDark ("Unlit Room Colour", Color) = (0.05,0.055,0.06,1)
        _Glitchiness ("Corruption Response", Range(0,2)) = 1
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "GlitchPBR.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _WallColor;
            float _WallTile;
            half _WallRoughness;
            float _FloorHeight;
            float _BayWidth;
            half _WindowW;
            half _WindowH;
            half _Sill;
            float _Frame;
            half4 _FrameColor;
            half _CurtainWall;
            float _Mullion;
            half _GroundFloor;
            half4 _GlassTint;
            half _GlassSmoothness;
            float _InteriorDepth;
            half _RoomLit;
            half _LightIntensity;
            half4 _RoomWarm;
            half4 _RoomCool;
            half4 _RoomDark;
            half _Glitchiness;
            float _Seed;
        CBUFFER_END

        TEXTURE2D(_WallMap); SAMPLER(sampler_WallMap);
        TEXTURE2D(_WallNormal);
        TEXTURE2D(_WallRoughnessMap);

        float3 FacadeVertex(float3 positionOS)
        {
            float3 p = TransformObjectToWorld(positionOS);
            float c = EffectiveCorruption(p.x) * _Glitchiness;
            return SubtleTear(p, c, GObjectSeed(_Seed));
        }
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 posWS = FacadeVertex(v.positionOS.xyz);
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.normalWS = n.normalWS;
                o.tangentWS = half4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = v.uv;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Interior mapping: ray from the window plane into a room box (width w, height h, depth d), in metres.
            // Returns the lit room colour seen through the glass (pre-fresnel).
            half3 Interior(float2 roomPos, float w, float h, float3 dir, float roomId, out half lightOut)
            {
                float d = _InteriorDepth;
                float3 p = float3(roomPos, 0);
                float tx = dir.x > 0 ? (w - p.x) / dir.x : -p.x / min(dir.x, -1e-4);
                float ty = dir.y > 0 ? (h - p.y) / dir.y : -p.y / min(dir.y, -1e-4);
                float tz = d / max(dir.z, 1e-4);
                float t = min(tx, min(ty, tz));
                float3 hit = p + dir * t;

                float rA = GHash11(roomId * 1.7 + _Seed);
                float rB = GHash11(roomId * 3.1 + 11.0 + _Seed);
                // rooms slowly switch on/off (every ~30-60 s), so the city feels inhabited
                float phase = floor(_Time.y / (30.0 + rB * 30.0) + rA * 10.0);
                float lit = step(1.0 - _RoomLit, GHash11(roomId * 5.3 + phase));
                half3 roomCol = lerp(_RoomCool.rgb, _RoomWarm.rgb, step(0.5, rB));
                // a few rooms glow in colour (TVs, gaming setups, neon signs): the city reads alive, not beige
                float hue = GHash11(roomId * 7.9 + _Seed);
                half3 neon = hue < 0.33 ? half3(1.0, 0.32, 0.72) : hue < 0.66 ? half3(0.25, 0.85, 1.0) : half3(1.0, 0.62, 0.22);
                roomCol = lerp(roomCol, neon, step(0.82, GHash11(roomId * 2.3 + 5.0 + _Seed)) * 0.85);

                half3 col;
                float depthShade = 1.0 - saturate(hit.z / d) * 0.45;
                if (t == tz)            col = roomCol * 0.85;                                // back wall
                else if (t == ty)       col = dir.y > 0 ? roomCol * 1.15 : roomCol * 0.42; // ceiling / floor
                else                    col = roomCol * 0.68;                                // side walls
                // furniture silhouette against the back wall (desk / sofa)
                float furn = step(abs(hit.x - w * (0.3 + rA * 0.4)), w * 0.22) * step(hit.y, h * 0.32) * step(d * 0.7, hit.z);
                col = lerp(col, col * 0.35, furn);
                // ceiling light panel
                float panel = (t == ty && dir.y > 0) ? step(abs(hit.x - w * 0.5), w * 0.18) * step(abs(hit.z - d * 0.5), d * 0.2) : 0;
                col *= depthShade;
                lightOut = lit;
                half3 litCol = (col * _LightIntensity + panel * _RoomWarm.rgb * 4.0) * (1.0 + _Corruption * 0.6);   // dusk: lit windows glow
                return lerp(_RoomDark.rgb * (0.6 + 0.4 * depthShade), litCol, lit);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float u = i.uv.x, v = i.uv.y;
                // ---- glitch patches: tiny parts of a building fail for a moment, with long gaps in between
                float cP = EffectiveCorruption(i.positionWS.x) * _Glitchiness;
                float2 patch = floor(float2(u / (_BayWidth * 2.0), v / (_FloorHeight * 2.0)));
                float pid = GHash21(patch + GObjectSeed(_Seed) * 57.0);
                float period = 7.0 + pid * 9.0;
                float phaseT = frac((_Time.y + pid * 37.0) / period);
                float patchOn = step(1.0 - 0.12 * cP, GHash11(pid * 91.0)) * step(phaseT, 0.06 / period * 6.0);   // ~0.4-0.8 s per event
                #if defined(GLITCH_LOW)
                    patchOn = 0;
                #endif
                float stepT = floor(_Time.y * 14.0);
                u += patchOn * (GHash21(float2(pid, stepT)) - 0.5) * _BayWidth * 1.2;   // the window pattern jumps sideways
                float bayW = _BayWidth, floorH = _FloorHeight;
                float bay = floor(u / bayW), flr = floor(v / floorH);
                float fu = frac(u / bayW), fv = frac(v / floorH);

                // window rectangle in this cell (curtain wall: nearly full glass with spandrel bands)
                float ww = lerp(_WindowW, 0.985, _CurtainWall);
                float wh = lerp(_WindowH, 0.78, _CurtainWall);
                float sill = lerp(_Sill, 0.11, _CurtainWall);
                bool ground = flr < 0.5 && _GroundFloor > 0.5;
                if (ground) { ww = 0.9; wh = 0.78; sill = 0.04; }
                float x0 = (1 - ww) * 0.5;
                float inX = step(x0, fu) * step(fu, 1 - x0);
                float inY = step(sill, fv) * step(fv, sill + wh);
                float isWindow = inX * inY * step(0.0, v);

                // frame: thin band around the opening, plus curtain-wall mullions
                float fx = _Frame / bayW, fy = _Frame / floorH;
                float frame = isWindow * (1 - step(x0 + fx, fu) * step(fu, 1 - x0 - fx) * step(sill + fy, fv) * step(fv, sill + wh - fy));
                if (_CurtainWall > 0.5)
                {
                    float m = frac(u / _Mullion);
                    frame = max(frame, isWindow * (step(m, 0.02) + step(0.98, m)));
                }
                float glass = isWindow * (1 - frame);

                // shared lighting inputs
                float sgn = i.tangentWS.w;
                half3 N = normalize(i.normalWS);
                half3 T = normalize(i.tangentWS.xyz);
                half3 B = sgn * cross(N, T);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = i.fogFactor;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.alpha = 1; s.occlusion = 1; s.specular = 0;
                half3 emission = 0;

                if (glass > 0.5)
                {
                    // ray into the room, in tangent space (x along wall, y up, z into the building)
                    float3 viewIn = -V;
                    float3 dir = normalize(float3(dot(viewIn, T), dot(viewIn, B), -dot(viewIn, N)));
                    float winW = ww * bayW, winH = wh * floorH;
                    float roomId = bay * 17.0 + flr * 131.0 + GObjectSeed(_Seed) * 997.0;
                    half lit;
                    float2 rp = float2((fu - x0) * bayW, (fv - sill) * floorH);
                    // the room is wider than the window: centre the opening in a bay-wide room
                    rp.x += (bayW - winW) * 0.5;
                    half3 room = Interior(rp, bayW, floorH, dir, roomId, lit);
                    // blinds: some rooms have them partly down
                    float blind = GHash11(roomId * 7.7 + 3.0) * 0.7;
                    float wy = (fv - sill) / wh;
                    if (wy > 1.0 - blind) room = lerp(half3(0.55, 0.52, 0.47) * (0.25 + lit * 0.9), room, 0.15);
                    // subtle corruption: now and then one room glows in the region accent
                    float c0 = EffectiveCorruption(i.positionWS.x) * _Glitchiness;
                    float odd = step(0.992, GHash11(roomId + floor(_Time.y * 0.1))) * c0;
                    room = lerp(room, _RegionAccent.rgb * 1.2, odd);

                    float fres = pow(1.0 - saturate(dot(N, V)), 4.0);
                    s.albedo = _GlassTint.rgb * 0.25;
                    s.metallic = 0.0;
                    s.smoothness = _GlassSmoothness;
                    s.normalTS = half3(0, 0, 1);
                    inputData.normalWS = N;
                    emission = room * (1.0 - lerp(0.25, 0.9, fres));
                }
                else
                {
                    float2 wuv = float2(u, v) / max(_WallTile, 0.01);
                    half3 wall = SAMPLE_TEXTURE2D(_WallMap, sampler_WallMap, wuv).rgb * _WallColor.rgb;
                    half3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_WallNormal, sampler_WallMap, wuv));
                    half rough = SAMPLE_TEXTURE2D(_WallRoughnessMap, sampler_WallMap, wuv).r * _WallRoughness;
                    // soft shadow line under each window (reads as a recessed opening)
                    float under = (1 - inY) * inX * step(fv, sill) * smoothstep(sill - 0.08, sill, fv);
                    wall *= 1.0 - under * 0.35;
                    s.albedo = lerp(wall, _FrameColor.rgb, frame);
                    s.metallic = frame * 0.6;
                    s.smoothness = lerp(1.0 - rough, 0.55, frame);
                    s.normalTS = frame > 0.5 ? half3(0, 0, 1) : nTS;
                    inputData.normalWS = normalize(TransformTangentToWorld(s.normalTS, half3x3(T, B, N)));
                }
                inputData.bakedGI = SampleSH(inputData.normalWS);
                s.emission = emission;

                half4 col = UniversalFragmentPBR(inputData, s);
                float c = EffectiveCorruption(i.positionWS.x) * _Glitchiness;
                col.rgb = SubtleCorruption(col.rgb, i.positionWS, c, GObjectSeed(_Seed));
                if (patchOn > 0.5)
                {
                    // a few cells inside the patch light up in the region colour, the rest get a faint RGB split tint
                    float2 cell = floor(float2(i.uv.x / (_BayWidth * 0.5), i.uv.y / (_FloorHeight * 0.5)));
                    float blk = step(0.72, GHash21(cell + stepT));
                    col.rgb = lerp(col.rgb, _RegionAccent.rgb * 1.6, blk * 0.75);
                    col.rgb += half3(_RegionAccent.r, 0, _RegionAccent2.b) * 0.12;
                }
                col.rgb += RestorationGlow(i.positionWS);
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
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
                V o; UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = FacadeVertex(v.positionOS.xyz);
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
            V vertDepth(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformWorldToHClip(FacadeVertex(v.positionOS.xyz)); return o; }
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
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; };
            V vertDN(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformWorldToHClip(FacadeVertex(v.positionOS.xyz)); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
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
