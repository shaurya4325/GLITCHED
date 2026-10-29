using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Soft god-rays slanting down through the space behind the gameplay plane (between the street and the
    /// buildings, under the trees, from the bio-centre's skylights), in the sun's colour. Pure atmosphere: additive
    /// cards with a generated beam texture (Art/Textures/VFX/shaft_01.png), switched off with the L2 detail layer.
    /// </summary>
    public class LightShaftBuilder
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly RegionThemeSO theme;
        readonly GameDatabase db;
        readonly System.Random rnd;
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        public LightShaftBuilder(LevelPlan plan, Transform root, RegionThemeSO theme, GameDatabase db)
        {
            this.plan = plan; this.root = root; this.theme = theme; this.db = db;
            rnd = new System.Random(plan.levelId.GetHashCode() ^ 0x5a5a);
        }

        public void Build()
        {
            if (plan.kind == LevelKind.BossArena || plan.arcId == "TheCore") return;   // the rooftops are open sky at night
            var lib = db != null ? db.materials : null;   // the scene is built in the editor: no Services there
            var tex = lib != null ? lib.Particle("shaft") : null;
            if (tex == null || !tex.name.Contains("shaft")) return;
            var group = new GameObject("LightShafts");
            group.transform.SetParent(root, false);
            group.AddComponent<DepthLayerTag>().layer = DepthLayer.L2_NearDetail;
            Color sun = theme != null ? Color.Lerp(theme.sunCorrupted, theme.sunRestored, 0.3f) : new Color(1f, 0.85f, 0.6f);
            float strength = plan.arcId == "BioZone" ? 0.26f : plan.arcId == "PhysicsValley" ? 0.22f : 0.24f;
            var mat = MaterialCache.Instance("padd");
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(sun.r * strength, sun.g * strength, sun.b * strength, 1f));
            mat = MaterialCache.OnCreated != null ? MaterialCache.OnCreated(mat) : mat;
            float slant = theme != null ? Mathf.Clamp(-theme.sunEuler.y * 0.35f, -24f, 24f) : -16f;   // lean with the sun
            var mb = new MeshBuilder(1);
            for (float x = plan.minX + R(5f, 20f); x < plan.maxX; x += R(22f, 42f))
            {
                float g = plan.GroundY(x);
                if (g < -5f) g = 0f;
                float z = R(6f, 26f), w = R(3f, 7f), h = R(22f, 34f);
                var up = Quaternion.Euler(0f, 0f, slant + R(-5f, 5f)) * Vector3.up;
                var right = Vector3.Cross(up, Vector3.forward).normalized * -1f;
                var bottom = new Vector3(x, g - 1f, z);
                var top = bottom + up * h;
                // a card facing the camera, wide at the top (light spreads as it falls... from the top down)
                Vector3 a = bottom - right * w * 0.35f, b = bottom + right * w * 0.35f, c = top + right * w * 0.5f, d = top - right * w * 0.5f;
                mb.Quad(a, b, c, d, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), 0);
                // a second, fainter beam beside it
                if (rnd.NextDouble() < 0.5)
                {
                    var o = right * w * R(0.8f, 1.3f);
                    mb.Quad(a + o, b + o * 0.9f, c + o * 1.2f, d + o * 1.2f, new Vector2(0.2f, 0), new Vector2(0.8f, 0), new Vector2(0.8f, 1), new Vector2(0.2f, 1), 0);
                }
            }
            if (mb.VertexCount == 0) return;
            var go = mb.ToObject("Shafts", group.transform, new[] { mat }, false);
            SceneSpawn.MarkStatic(go);
        }
    }
}
