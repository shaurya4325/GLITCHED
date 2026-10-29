using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Places a plan's set pieces once the backdrop exists: picks a real backdrop building (tower, lab, warehouse,
    /// cooling tower) for each demolition, adds the chase blasts of a parkour stretch, builds the exploding props
    /// beside the path, and gives corrupted regions their distant skyline blasts.
    /// </summary>
    public class SetPieceBuilder
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly BlastStyle style;
        static readonly HashSet<string> Demolishable = new HashSet<string> { "Building", "Warehouse", "CoolingTower", "Column", "ResearchLab", "Workshop", "Tower" };

        public SetPieceBuilder(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            style = Blast.StyleFor(plan.arcId);
        }

        string DefaultLine()
        {
            switch (plan.arcId)
            {
                case "PhysicsValley": return "The lab! The Glitch just deleted the lab! Gravity included.";
                case "ChemicalChaos": return "That's not a controlled demolition. That's a CHAOS-trolled demolition.";
                case "TheCore": return "The Source is deleting the skyline. Floor by floor.";
                default: return "Did that tower just... uninstall itself?!";
            }
        }

        public void Build()
        {
            var used = new HashSet<Transform>();
            foreach (var d in plan.setPieces)
            {
                if (d.kind == "demolish") Demolish(d, used);
                else if (d.kind == "chase")
                {
                    var go = new GameObject("ChaseBlasts");
                    go.transform.SetParent(root, false);
                    var c = go.AddComponent<ChaseBlasts>();
                    c.x0 = d.x; c.x1 = d.x1; c.style = style;
                }
                else if (d.kind == "prop") Prop(d);
            }
            if (plan.arcId != "BioZone" && plan.kind != LevelKind.BossArena)
            {
                var amb = new GameObject("AmbientBlasts");
                amb.transform.SetParent(root, false);
                amb.AddComponent<AmbientBlasts>().style = style;
            }
        }

        void Demolish(SetPieceDef d, HashSet<Transform> used)
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!Demolishable.Contains(t.name) || used.Contains(t)) continue;
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y < 10f || b.center.z < 15f || b.center.z > 330f) continue;
                if (b.center.x < d.x + 4f || b.center.x > d.x + 75f) continue;
                float score = Mathf.Abs(b.center.x - (d.x + 22f)) + b.center.z * 0.15f;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            if (best == null)
            {
                // nothing standing there: put up a tower of our own for the Glitch to knock down
                bool core = plan.arcId == "TheCore";   // the rooftops: towers rise from the streets 220 m below
                var spec = core ? BuildingGen.Random(new System.Random((int)d.x), 26f, 24f, 250f, 262f, true, 1f) : BuildingGen.Random(new System.Random((int)d.x), 18f, 16f, 30f, 55f, true, 1f);
                if (core) spec.style = CityMaterials.FacadeStyle.GlassTower;
                best = BuildingGen.Build(root, new Vector3(d.x + 30f, core ? -220f : 0f, core ? 95f : 62f), spec, new System.Random((int)d.x + 1), "Building").transform;
                Debug.Log($"[SetPiece] {plan.levelId}: built a tower to demolish at x={d.x + 26f}");
            }
            used.Add(best);
            SceneSpawn.ClearStatic(best.gameObject);   // it has to move
            var dm = best.gameObject.AddComponent<Demolition>();
            dm.triggerX = d.x; dm.style = style; dm.line = string.IsNullOrEmpty(d.line) ? DefaultLine() : d.line;
        }

        /// <summary>A region-styled prop just behind the walking line that blows when the player gets close.</summary>
        void Prop(SetPieceDef d)
        {
            float y = plan.GroundY(d.x);
            var go = new GameObject("ExplodingProp");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(d.x, y, 2.6f);
            var prop = new GameObject("Prop").transform;
            prop.SetParent(go.transform, false);
            var mb = new MeshBuilder(3);
            Material body, trim, glow;
            switch (plan.arcId)
            {
                case "ChemicalChaos":
                    // a pressure tank on legs, toxic stripes
                    mb.Tube(new Vector3(-1.3f, 1.2f, 0), new Vector3(1.3f, 1.2f, 0), 0.75f, 0.75f, 16, 0);
                    foreach (float x in new[] { -0.9f, 0.9f }) mb.BoxC(new Vector3(x, 0.25f, 0), new Vector3(0.12f, 0.5f, 1.1f), 1);
                    mb.Tube(new Vector3(-0.2f, 1.2f, -0.02f), new Vector3(0.2f, 1.2f, -0.02f), 0.77f, 0.77f, 16, 2);
                    body = CityMaterials.Solid(new Color(0.82f, 0.84f, 0.8f), 0.35f, 0.6f); trim = CityMaterials.Solid(new Color(0.2f, 0.2f, 0.22f), 0.5f, 0.5f);
                    glow = CityMaterials.Emissive(new Color(0.1f, 0.4f, 0.05f), new Color(0.4f, 2.2f, 0.3f));
                    break;
                case "PhysicsValley":
                    // a transformer on a short concrete plinth, with insulators
                    mb.BoxC(new Vector3(0, 0.3f, 0), new Vector3(1.6f, 0.6f, 1.2f), 1);
                    mb.BoxC(new Vector3(0, 1.2f, 0), new Vector3(1.3f, 1.2f, 0.9f), 0);
                    foreach (float x in new[] { -0.4f, 0f, 0.4f }) mb.Tube(new Vector3(x, 1.8f, 0), new Vector3(x, 2.3f, 0), 0.07f, 0.05f, 8, 2);
                    body = CityMaterials.Solid(new Color(0.42f, 0.46f, 0.42f), 0.5f, 0.5f); trim = CityMaterials.PBR("Concrete046", new Color(0.7f, 0.7f, 0.68f), 2f);
                    glow = CityMaterials.Emissive(new Color(0.2f, 0.3f, 0.5f), new Color(0.6f, 1.2f, 2.5f));
                    break;
                case "BioZone":
                    // a specimen pod: glass, glowing liquid, steel caps
                    mb.Cylinder(new Vector3(0, 0f, 0), 0.6f, 0.6f, 0.3f, 16, 1);
                    mb.Cylinder(new Vector3(0, 0.3f, 0), 0.5f, 0.5f, 1.6f, 16, 2);
                    mb.Cylinder(new Vector3(0, 1.9f, 0), 0.6f, 0.45f, 0.3f, 16, 1);
                    body = CityMaterials.Glass(new Color(0.8f, 0.9f, 1f)); trim = CityMaterials.Solid(new Color(0.75f, 0.77f, 0.8f), 0.25f, 0.9f);
                    glow = CityMaterials.Emissive(new Color(0.5f, 0.1f, 0.2f), new Color(2.4f, 0.4f, 0.8f));
                    break;
                case "TheCore":
                    // a rooftop generator cabinet
                    mb.BoxC(new Vector3(0, 0.9f, 0), new Vector3(1.6f, 1.8f, 1f), 0);
                    mb.BoxC(new Vector3(0, 1.85f, 0), new Vector3(1.7f, 0.1f, 1.1f), 1);
                    for (int i = 0; i < 4; i++) mb.BoxC(new Vector3(-0.5f + i * 0.33f, 1.3f, -0.51f), new Vector3(0.16f, 0.06f, 0.02f), 2);
                    body = CityMaterials.Solid(new Color(0.12f, 0.13f, 0.16f), 0.4f, 0.7f); trim = CityMaterials.Solid(new Color(0.3f, 0.32f, 0.35f), 0.4f, 0.8f);
                    glow = CityMaterials.Emissive(new Color(0.4f, 0.1f, 0.4f), new Color(2.5f, 0.4f, 2.2f));
                    break;
                default:
                    // a street utility cabinet with a hazard band and a status screen
                    mb.BoxC(new Vector3(0, 0.85f, 0), new Vector3(1.3f, 1.7f, 0.8f), 0);
                    mb.BoxC(new Vector3(0, 1.75f, 0), new Vector3(1.4f, 0.1f, 0.9f), 1);
                    mb.BoxC(new Vector3(0, 0.25f, -0.41f), new Vector3(1.3f, 0.2f, 0.02f), 1);
                    mb.BoxC(new Vector3(0, 1.25f, -0.41f), new Vector3(0.5f, 0.3f, 0.02f), 2);
                    body = CityMaterials.Solid(new Color(0.3f, 0.36f, 0.33f), 0.45f, 0.5f); trim = CityMaterials.Solid(new Color(0.9f, 0.7f, 0.1f), 0.5f);
                    glow = CityMaterials.Emissive(new Color(0.3f, 0.1f, 0.1f), new Color(2.5f, 0.3f, 0.3f));
                    break;
            }
            mb.ToObject("Visual", prop, new[] { body, trim, glow });
            var pb = go.AddComponent<PropBlast>();
            pb.triggerX = d.x - 5f; pb.style = style; pb.prop = prop;
        }
    }
}
