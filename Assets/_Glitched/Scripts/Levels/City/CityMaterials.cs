using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Realistic materials for the hand-made world: PBR surfaces from the ambientCG sets, facade styles for the
    /// procedural buildings, foliage, glass, lamps and paint. Cached per region so scenes share a small set.
    /// </summary>
    public static class CityMaterials
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static void ClearCache() => cache.Clear();

        /// <summary>Set by the level builder / editor builder: the runtime services are not booted while baking scenes.</summary>
        public static MaterialLibrarySO Library;
        static MaterialLibrarySO Lib => Library != null ? Library : (Services.Db != null ? Services.Db.materials : null);

        static Material Template(bool facade)
        {
            var lib = Lib;
            var m = lib != null ? (facade ? lib.facade : lib.realistic) : null;
            if (m != null) return m;
            var sh = Shader.Find(facade ? "Glitched/Facade" : "Glitched/Realistic") ?? Shader.Find("Universal Render Pipeline/Lit");
            return new Material(sh);
        }

        static Material Store(string key, Material m)
        {
            m.enableInstancing = true;
            if (MaterialCache.OnCreated != null) m = MaterialCache.OnCreated(m);
            cache[key] = m;
            return m;
        }

        /// <summary>PBR surface from an ambientCG set. <paramref name="tileMeters"/> = size of one texture repeat.</summary>
        public static Material PBR(string setId, Color tint, float tileMeters = 2f, float roughness = 1f, float metallic = 0f, float glitch = 1f, bool worldUV = true)
        {
            string key = $"pbr_{setId}_{ColorUtility.ToHtmlStringRGB(tint)}_{tileMeters:0.00}_{roughness:0.00}_{metallic:0.00}_{glitch:0.0}_{worldUV}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(false)) { name = $"PBR_{setId}_{ColorUtility.ToHtmlStringRGB(tint)}" };
            var set = Lib != null ? Lib.Set(setId) : null;
            if (set != null)
            {
                if (set.albedo) m.SetTexture("_BaseMap", set.albedo);
                if (set.normal) m.SetTexture("_BumpMap", set.normal);
                if (set.roughness) m.SetTexture("_RoughnessMap", set.roughness);
                if (set.occlusion) m.SetTexture("_OcclusionMap", set.occlusion);
            }
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Roughness", roughness);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glitchiness", glitch);
            float s = 1f / Mathf.Max(0.05f, tileMeters);
            if (worldUV) { m.SetFloat("_WorldUV", 1); m.EnableKeyword("_WORLDUV"); m.SetFloat("_WorldUVScale", s); }
            else m.SetTextureScale("_BaseMap", new Vector2(s, s));
            return Store(key, m);
        }

        /// <summary>Plain painted / coated surface (no texture): metal poles, plastic, paint.</summary>
        public static Material Solid(Color c, float roughness = 0.5f, float metallic = 0f, float glitch = 1f)
        {
            string key = $"solid_{ColorUtility.ToHtmlStringRGB(c)}_{roughness:0.00}_{metallic:0.00}_{glitch:0.0}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(false)) { name = "Solid_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Roughness", roughness);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glitchiness", glitch);
            return Store(key, m);
        }

        public static Material Emissive(Color baseColor, Color emission, float roughness = 0.3f)
        {
            string key = $"emis_{ColorUtility.ToHtmlStringRGB(baseColor)}_{emission.r:0.00}{emission.g:0.00}{emission.b:0.00}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(false)) { name = "Lamp_" + ColorUtility.ToHtmlStringRGB(baseColor) };
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_Roughness", roughness);
            m.SetFloat("_Glitchiness", 0.3f);
            return Store(key, m);
        }

        /// <summary>Terrain: grass on the flats, rock on the slopes, snow above <paramref name="snowHeight"/>.</summary>
        public static Material Terrain(string grassSet, string rockSet, Color grassTint, Color rockTint, float snowHeight = 9999f)
        {
            string key = $"terrain_{grassSet}_{rockSet}_{ColorUtility.ToHtmlStringRGB(grassTint)}_{snowHeight:0}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var lib = Lib;
            var tpl = lib != null && lib.terrain != null ? lib.terrain : new Material(Shader.Find("Glitched/Terrain"));
            m = new Material(tpl) { name = $"Terrain_{grassSet}_{rockSet}" };
            var g = lib?.Set(grassSet); var r = lib?.Set(rockSet);
            if (g != null) { m.SetTexture("_GrassMap", g.albedo); if (g.normal) m.SetTexture("_GrassNormal", g.normal); }
            if (r != null) { m.SetTexture("_RockMap", r.albedo); if (r.normal) m.SetTexture("_RockNormal", r.normal); }
            m.SetColor("_GrassTint", grassTint);
            m.SetColor("_RockTint", rockTint);
            m.SetFloat("_SnowHeight", snowHeight);
            return Store(key, m);
        }

        public static Material Water(Color deep, Color shallow, Vector2 flow, Color glow = default)
        {
            string key = $"water_{ColorUtility.ToHtmlStringRGB(deep)}_{flow.x:0.0}_{ColorUtility.ToHtmlStringRGB(glow)}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var lib = Lib;
            var tpl = lib != null && lib.water != null ? lib.water : new Material(Shader.Find("Glitched/Water"));
            m = new Material(tpl) { name = "Water_" + ColorUtility.ToHtmlStringRGB(deep) };
            m.SetColor("_DeepColor", deep);
            m.SetColor("_ShallowColor", shallow);
            m.SetVector("_Flow", new Vector4(flow.x, 0, flow.y, 0));
            m.SetColor("_EmissionColor", glow);
            return Store(key, m);
        }

        /// <summary>A screen showing a texture (digital signage, kiosk displays): the image glows, the glass reflects.</summary>
        public static Material Screen(Texture tex, Color tint)
        {
            string key = $"screen_{(tex != null ? tex.name : "none")}_{ColorUtility.ToHtmlStringRGB(tint)}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(false)) { name = "Screen_" + (tex != null ? tex.name : "plain") };
            m.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.025f));
            if (tex != null) m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", tint);
            m.SetFloat("_Roughness", 0.08f);
            m.SetFloat("_Glitchiness", 1f);
            return Store(key, m);
        }

        /// <summary>Dark, reflective glass for shelters, railings and car windows (opaque: no sorting issues).</summary>
        public static Material Glass(Color tint) => Solid(tint * 0.35f, 0.04f, 0.1f, 0.6f);

        public static Material Leaves(int variant = 0)
        {
            string key = "leaves_" + variant;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(false)) { name = "Leaves_" + variant };
            var lib = Lib;
            if (lib != null && lib.leafAtlas != null) m.SetTexture("_BaseMap", lib.leafAtlas);
            m.SetColor("_BaseColor", variant == 1 ? new Color(0.95f, 1f, 0.9f) : new Color(0.85f, 0.9f, 0.85f));
            m.SetFloat("_AlphaClip", 1); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cutoff", 0.45f);
            m.SetFloat("_Roughness", 0.75f);
            m.SetFloat("_Translucency", 0.45f);
            m.SetFloat("_Cull", 0);
            m.SetFloat("_Glitchiness", 0.7f);
            return Store(key, m);
        }

        public enum FacadeStyle { GlassTower, Office, Brick, Plaster, Stone, Industrial }

        /// <summary>A building facade style. Each style has a few seeded variants so streets don't repeat.</summary>
        public static Material Facade(FacadeStyle style, int variant, float glitch = 1f)
        {
            string key = $"facade_{style}_{variant}_{glitch:0.0}";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Template(true)) { name = $"Facade_{style}_{variant}" };
            var rnd = new System.Random((int)style * 97 + variant * 13);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            PBRSet wall = null;
            Color tint = Color.white;
            switch (style)
            {
                case FacadeStyle.GlassTower:
                    wall = Lib?.Set("Metal009") ?? Lib?.Set("Metal063");
                    tint = new Color(0.55f, 0.58f, 0.62f);
                    m.SetFloat("_CurtainWall", 1);
                    m.SetFloat("_FloorHeight", R(3.6f, 4.0f));
                    m.SetFloat("_BayWidth", 3f);
                    m.SetFloat("_Mullion", 1.5f);
                    m.SetColor("_GlassTint", Color.Lerp(new Color(0.14f, 0.2f, 0.26f), new Color(0.2f, 0.24f, 0.22f), R(0, 1)));
                    m.SetColor("_FrameColor", new Color(0.16f, 0.17f, 0.19f));
                    m.SetFloat("_RoomLit", R(0.35f, 0.6f));
                    m.SetColor("_RoomCool", new Color(0.88f, 0.94f, 1f));
                    break;
                case FacadeStyle.Office:
                    wall = Lib?.Set(variant % 2 == 0 ? "Concrete046" : "Concrete034") ?? Lib?.Set("Concrete034");
                    tint = Color.Lerp(new Color(0.78f, 0.76f, 0.72f), new Color(0.62f, 0.63f, 0.66f), R(0, 1));
                    m.SetFloat("_FloorHeight", 3.6f);
                    m.SetFloat("_BayWidth", 3f);
                    m.SetFloat("_WindowW", R(0.6f, 0.8f));
                    m.SetFloat("_WindowH", R(0.5f, 0.62f));
                    m.SetFloat("_Sill", 0.26f);
                    m.SetColor("_FrameColor", new Color(0.2f, 0.21f, 0.22f));
                    m.SetFloat("_RoomLit", R(0.3f, 0.55f));
                    break;
                case FacadeStyle.Brick:
                    wall = Lib?.Set(variant % 2 == 0 ? "Bricks097" : "Bricks060") ?? Lib?.Set("Bricks113A");
                    tint = Color.Lerp(new Color(0.95f, 0.9f, 0.88f), new Color(0.8f, 0.72f, 0.68f), R(0, 1));
                    m.SetFloat("_WallTile", 2.2f);
                    m.SetFloat("_FloorHeight", 3.2f);
                    m.SetFloat("_BayWidth", 2.6f);
                    m.SetFloat("_WindowW", 0.46f);
                    m.SetFloat("_WindowH", 0.58f);
                    m.SetFloat("_Sill", 0.24f);
                    m.SetColor("_FrameColor", variant % 3 == 0 ? new Color(0.85f, 0.85f, 0.82f) : new Color(0.12f, 0.13f, 0.12f));
                    m.SetFloat("_RoomLit", R(0.4f, 0.6f));
                    m.SetColor("_RoomWarm", new Color(1f, 0.82f, 0.6f));
                    break;
                case FacadeStyle.Plaster:
                    wall = Lib?.Set(variant % 2 == 0 ? "Plaster001" : "PaintedPlaster017") ?? Lib?.Set("Concrete048");
                    tint = new[] { new Color(0.92f, 0.88f, 0.8f), new Color(0.78f, 0.82f, 0.84f), new Color(0.88f, 0.8f, 0.72f), new Color(0.9f, 0.9f, 0.88f) }[variant % 4];
                    m.SetFloat("_FloorHeight", 3.1f);
                    m.SetFloat("_BayWidth", 2.8f);
                    m.SetFloat("_WindowW", 0.5f);
                    m.SetFloat("_WindowH", 0.55f);
                    m.SetFloat("_Sill", 0.26f);
                    m.SetColor("_FrameColor", new Color(0.92f, 0.92f, 0.9f));
                    m.SetFloat("_RoomLit", R(0.45f, 0.65f));
                    break;
                case FacadeStyle.Stone:
                    wall = Lib?.Set("Granite002A") ?? Lib?.Set("Marble012");
                    tint = new Color(0.86f, 0.84f, 0.8f);
                    m.SetFloat("_WallTile", 4f);
                    m.SetFloat("_FloorHeight", 4.2f);
                    m.SetFloat("_BayWidth", 3.4f);
                    m.SetFloat("_WindowW", 0.55f);
                    m.SetFloat("_WindowH", 0.62f);
                    m.SetFloat("_Sill", 0.2f);
                    m.SetColor("_FrameColor", new Color(0.14f, 0.13f, 0.12f));
                    break;
                default: // Industrial
                    wall = Lib?.Set("CorrugatedSteel009") ?? Lib?.Set("MetalPlates017A");
                    tint = new Color(0.7f, 0.72f, 0.74f);
                    m.SetFloat("_WallTile", 2f);
                    m.SetFloat("_FloorHeight", 5f);
                    m.SetFloat("_BayWidth", 6f);
                    m.SetFloat("_WindowW", 0.7f);
                    m.SetFloat("_WindowH", 0.25f);
                    m.SetFloat("_Sill", 0.62f);
                    m.SetFloat("_GroundFloor", 0);
                    m.SetFloat("_RoomLit", 0.25f);
                    break;
            }
            if (wall != null)
            {
                if (wall.albedo) m.SetTexture("_WallMap", wall.albedo);
                if (wall.normal) m.SetTexture("_WallNormal", wall.normal);
                if (wall.roughness) m.SetTexture("_WallRoughnessMap", wall.roughness);
            }
            m.SetColor("_WallColor", tint);
            m.SetFloat("_Seed", variant * 0.173f + (int)style * 0.31f);
            m.SetFloat("_Glitchiness", glitch);
            return Store(key, m);
        }
    }
}
