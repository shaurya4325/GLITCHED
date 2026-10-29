using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Shared materials built by the editor builder (so their shaders are included in builds).</summary>
    [CreateAssetMenu(menuName = "Glitched/Material Library", fileName = "MaterialLibrary")]
    public class MaterialLibrarySO : ScriptableObject
    {
        public Material glitchable;        // environment props (corruption-aware)
        public Material glitchableEmissive;
        public Material toon;              // characters
        public Material faceDecal;
        public Material sky;
        public Material hologram;
        public Material impostor;          // far skyline cards
        public Material flow;              // scrolling liquid / blood / waterfalls
        public Material particleAdditive;
        public Material particleAlpha;
        public Material uiGlitch;
        public Material fullscreenGlitch;
        public Material platformTop;
        public Material grid;              // Final Void floor
        public Material dither;            // L0 occluder fade
        public Material realistic;         // PBR master for the realistic world
        public Material facade;            // procedural building facades (interior-mapped windows)
        public Material terrain;           // grass / rock / snow by slope and height
        public Material water;             // rivers, lakes, liquids

        [Tooltip("PBR texture sets (ambientCG) found under Art/Textures/PBR, filled by the editor builder.")]
        public List<PBRSet> pbr = new List<PBRSet>();
        [Tooltip("Generated leaf / grass alpha cards (Tools/make_foliage.py).")]
        public Texture2D leafAtlas, leafAtlasNormal;

        public Texture2D[] particleTextures;   // Kenney particle pack (spark, flare, smoke, magic, star, circle)
        public Texture2D binaryRain;

        public PBRSet Set(string id)
        {
            if (pbr == null) return null;
            foreach (var p in pbr) if (p != null && p.id == id) return p;
            return null;
        }

        public Texture2D Particle(string nameContains)
        {
            if (particleTextures == null) return null;
            foreach (var t in particleTextures)
                if (t != null && t.name.ToLowerInvariant().Contains(nameContains)) return t;
            return particleTextures.Length > 0 ? particleTextures[0] : null;
        }
    }

    [System.Serializable]
    public class PBRSet
    {
        public string id;
        public Texture2D albedo, normal, roughness, occlusion, metalness;
    }
}
