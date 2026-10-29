using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The L0 foreground (the Limbo / Inside depth trick): dark silhouettes between the camera and the gameplay
    /// plane (utility poles and sagging cables in the city, pine trunks in the valley, pipe runs in the plant, glass
    /// tubes in the bio-centre, lattice masts on the rooftops). They are blurred by depth of field, frame the shot,
    /// and dither away wherever they would cover the player (Glitched/ForegroundDither). Kept clear of anything you
    /// talk to or fight.
    /// </summary>
    public class ForegroundBuilder
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly System.Random rnd;
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        public ForegroundBuilder(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() ^ 0x0f0f);
        }

        bool Clear(float x)
        {
            foreach (var e in plan.enemies) if (Mathf.Abs(e.x - x) < 5f) return false;
            foreach (var n in plan.npcs) if (Mathf.Abs(n.x - x) < 3.5f) return false;
            foreach (var t in plan.terminals) if (Mathf.Abs(t.x - x) < 3.5f) return false;
            foreach (var d in plan.doors) if (Mathf.Abs(d.x - x) < 3.5f) return false;
            foreach (var p in plan.portals) if (Mathf.Abs(p.x - x) < 3f) return false;
            if (plan.boss != null && x > plan.boss.triggerX - 4f && x < plan.boss.x + 8f) return false;
            return true;
        }

        public void Build()
        {
            if (plan.kind == LevelKind.BossArena) return;
            var group = new GameObject("Foreground (L0)");
            group.transform.SetParent(root, false);
            group.AddComponent<DepthLayerTag>().layer = DepthLayer.L0_Foreground;
            Color tone;
            switch (plan.arcId)
            {
                case "PhysicsValley": tone = new Color(0.16f, 0.13f, 0.1f); break;
                case "ChemicalChaos": tone = new Color(0.2f, 0.22f, 0.22f); break;
                case "BioZone": tone = new Color(0.32f, 0.22f, 0.26f); break;
                case "TheCore": tone = new Color(0.08f, 0.08f, 0.12f); break;
                default: tone = new Color(0.12f, 0.12f, 0.14f); break;
            }
            var mat = MaterialCache.Instance("fg");
            mat.SetColor("_BaseColor", tone);
            mat.SetFloat("_Darken", 0.5f);
            mat.SetFloat("_Bands", 0f);   // realistic silhouettes: no glitch stripes
            mat = MaterialCache.OnCreated != null ? MaterialCache.OnCreated(mat) : mat;

            var mb = new MeshBuilder(1);
            float chunkStart = plan.minX, prevX = float.NaN, prevTop = 0f, prevZ = 0f;
            for (float x = plan.minX + R(4f, 10f); x < plan.maxX; x += R(22f, 40f))   // sparse: frame the shot, never crowd it
            {
                if (!Clear(x)) { prevX = float.NaN; continue; }
                float g = plan.GroundY(x);
                if (g < -5f) g = 0f;
                float z = R(-8.6f, -7.6f);   // just in front of the walkway's edge (z = -6), well clear of the camera
                float top = g + R(3.8f, 4.6f);   // where cables / branches / pipe runs cross the top of the frame
                switch (plan.arcId)
                {
                    case "PhysicsValley":
                        // a pine trunk with a few branch stubs
                        mb.Tube(new Vector3(x, g - 30f, z), new Vector3(x + R(-0.3f, 0.3f), g + 40f, z), R(0.2f, 0.3f), 0.14f, 9, 0);
                        for (int i = 0; i < 3; i++) { float by = top + R(-0.6f, 1.6f); float dir = rnd.NextDouble() < 0.5 ? -1 : 1; mb.Tube(new Vector3(x, by, z), new Vector3(x + dir * R(1.2f, 2.6f), by + R(0.2f, 0.9f), z + R(-0.3f, 0.3f)), 0.09f, 0.03f, 6, 0); }
                        break;
                    case "ChemicalChaos":
                        // a vertical pipe with flanges, and a run along the top to the last one
                        mb.Tube(new Vector3(x, g - 30f, z), new Vector3(x, g + 40f, z), 0.2f, 0.2f, 12, 0);
                        for (float fy = g - 3f; fy < g + 12f; fy += 3f) mb.Tube(new Vector3(x, fy, z), new Vector3(x, fy + 0.12f, z), 0.27f, 0.27f, 12, 0);
                        if (!float.IsNaN(prevX) && x - prevX < 32f) mb.Tube(new Vector3(prevX, prevTop + 0.6f, prevZ), new Vector3(x, top + 0.6f, z), 0.14f, 0.14f, 10, 0);
                        break;
                    case "BioZone":
                        // a glass-and-steel culture tube with rings, a cable bundle hanging to the next
                        mb.Tube(new Vector3(x, g - 30f, z), new Vector3(x, g + 40f, z), 0.18f, 0.18f, 14, 0);
                        for (float fy = g - 2f; fy < g + 10f; fy += 2.2f) mb.Tube(new Vector3(x, fy, z), new Vector3(x, fy + 0.08f, z), 0.24f, 0.24f, 14, 0);
                        if (!float.IsNaN(prevX) && x - prevX < 32f) Cable(mb, new Vector3(prevX, prevTop, prevZ), new Vector3(x, top, z), 1.2f, 0.05f);
                        break;
                    case "TheCore":
                        // a lattice antenna mast
                        foreach (float dx in new[] { -0.35f, 0.35f }) mb.Tube(new Vector3(x + dx, g - 30f, z), new Vector3(x + dx, g + 40f, z), 0.06f, 0.06f, 6, 0);
                        for (float fy = g - 3f; fy < g + 12f; fy += 1.1f) mb.Tube(new Vector3(x - 0.35f, fy, z), new Vector3(x + 0.35f, fy + 0.8f, z), 0.035f, 0.035f, 5, 0);
                        break;
                    default:
                        // a utility pole with a cross-arm, sagging cables to the previous pole
                        mb.Tube(new Vector3(x, g - 30f, z), new Vector3(x, g + 40f, z), 0.12f, 0.1f, 10, 0);
                        mb.BoxC(new Vector3(x, top + 0.4f, z), new Vector3(1.8f, 0.12f, 0.12f), 0);
                        if (!float.IsNaN(prevX) && x - prevX < 32f)
                            for (int i = -1; i <= 1; i += 2) Cable(mb, new Vector3(prevX + i * 0.8f, prevTop + 0.4f, prevZ), new Vector3(x + i * 0.8f, top + 0.4f, z), 0.9f, 0.025f);
                        break;
                }
                prevX = x; prevTop = top; prevZ = z;
                if (x - chunkStart > 80f) { Flush(mb, group.transform, mat); mb = new MeshBuilder(1); chunkStart = x; }
            }
            Flush(mb, group.transform, mat);
        }

        /// <summary>A cable sagging between two points (a few straight segments of a catenary).</summary>
        static void Cable(MeshBuilder mb, Vector3 a, Vector3 b, float sag, float r)
        {
            const int n = 8;
            Vector3 prev = a;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                var p = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t);
                mb.Tube(prev, p, r, r, 4, 0);
                prev = p;
            }
        }

        static void Flush(MeshBuilder mb, Transform parent, Material mat)
        {
            if (mb.VertexCount == 0) return;
            var go = mb.ToObject("Silhouettes", parent, new[] { mat }, false);
            SceneSpawn.MarkStatic(go);
        }
    }
}
