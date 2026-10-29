using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Shared colour variants of the toon and glitchable materials (so placeholders batch nicely).</summary>
    public static class MaterialCache
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        /// <summary>Set by the editor builder so materials made while baking scenes are saved as assets.</summary>
        public static System.Func<Material, Material> OnCreated;

        static Material Store(string key, Material m)
        {
            if (OnCreated != null) m = OnCreated(m);
            cache[key] = m;
            return m;
        }

        public static void ClearCache() => cache.Clear();

        static Material Template(string kind)
        {
            var lib = Services.Db != null ? Services.Db.materials : null;
            Material m = null;
            if (lib != null)
            {
                switch (kind)
                {
                    case "toon": m = lib.toon; break;
                    case "glitch": m = lib.glitchable; break;
                    case "emissive": m = lib.glitchableEmissive != null ? lib.glitchableEmissive : lib.glitchable; break;
                    case "holo": m = lib.hologram; break;
                    case "flow": m = lib.flow; break;
                    case "face": m = lib.faceDecal; break;
                    case "fg": m = lib.dither; break;
                    case "impostor": m = lib.impostor; break;
                    case "padd": m = lib.particleAdditive; break;
                    case "palpha": m = lib.particleAlpha; break;
                    case "grid": m = lib.grid; break;
                }
            }
            if (m != null) return m;
            string shader = kind == "toon" ? "Glitched/Toon" : kind == "holo" ? "Glitched/Hologram" : kind == "flow" ? "Glitched/Flow" : kind == "face" ? "Glitched/FaceDecal"
                : kind == "fg" ? "Glitched/ForegroundDither" : kind == "impostor" ? "Glitched/Impostor" : kind == "padd" || kind == "palpha" ? "Glitched/Particle" : kind == "grid" ? "Glitched/Grid" : "Glitched/Glitchable";
            var sh = Shader.Find(shader) ?? Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(sh) { name = kind + "_template" };
            if (kind == "palpha") { m.SetFloat("_SrcBlend", 5); m.SetFloat("_DstBlend", 10); }
            return m;
        }

        static string Key(string kind, Color c, float extra = 0) => $"{kind}_{ColorUtility.ToHtmlStringRGBA(c)}_{extra:0.00}";

        public static Material Toon(Color c, float outline = 0.02f)
        {
            string k = Key("toon", c, outline);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Template("toon")) { name = "Toon_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetColor("_ShadeColor", Color.Lerp(new Color(0.45f, 0.4f, 0.7f), c * 0.6f, 0.4f));
            m.SetFloat("_OutlineWidth", outline);
            m.enableInstancing = true;
            return Store(k, m);
        }

        public static Material ToonEmissive(Color c, Color emission)
        {
            string k = Key("toonE", c) + ColorUtility.ToHtmlStringRGB(emission);
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Toon(c)) { name = "ToonE_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_EmissionColor", emission);
            return Store(k, m);
        }

        public static Material Glitch(Color c, float glitchiness = 1f, Texture tex = null)
        {
            string k = Key("glitch", c, glitchiness) + (tex != null ? tex.name : "");
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Template("glitch")) { name = "Glitch_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Glitchiness", glitchiness);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.enableInstancing = true;
            return Store(k, m);
        }

        public static Material Emissive(Color c, Color emission, float flicker = 0f, float glitchiness = 1f)
        {
            string k = Key("emis", c, flicker) + ColorUtility.ToHtmlStringRGB(emission) + glitchiness;
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Template("emissive")) { name = "Emissive_" + ColorUtility.ToHtmlStringRGB(emission) };
            m.SetColor("_BaseColor", c);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_WindowFlicker", flicker);
            m.SetFloat("_Glitchiness", glitchiness);
            m.EnableKeyword("_EMISSION");
            m.enableInstancing = true;
            return Store(k, m);
        }

        /// <summary>World-space (triplanar) textured material for procedural buildings, with flickering windows.</summary>
        public static Material Facade(Texture tex, Color tint, Color emission, float flicker, float uvScale = 0.12f, float glitchiness = 1f)
        {
            string k = Key("facade", tint, flicker) + (tex != null ? tex.name : "none") + ColorUtility.ToHtmlStringRGB(emission) + uvScale + glitchiness;
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Template("emissive")) { name = "Facade_" + (tex != null ? tex.name : "plain") };
            m.SetColor("_BaseColor", tint);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_WindowFlicker", flicker);
            m.SetFloat("_WindowCell", 1.6f);
            m.SetFloat("_Glitchiness", glitchiness);
            m.SetFloat("_Triplanar", 1f);
            m.SetFloat("_TriplanarScale", uvScale);
            m.EnableKeyword("_TRIPLANAR");
            m.EnableKeyword("_EMISSION");
            m.enableInstancing = true;
            return Store(k, m);
        }

        public static Material Get(string kind)
        {
            string k = "raw_" + kind;
            if (cache.TryGetValue(k, out var m) && m != null) return m;
            m = new Material(Template(kind)) { name = kind };
            return Store(k, m);
        }

        public static Material Instance(string kind) => new Material(Template(kind));
    }
}
