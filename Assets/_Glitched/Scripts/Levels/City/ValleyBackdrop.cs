using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Physics Valley as a real place: an alpine valley with a physics research campus.
    ///   z  6..12   meadow behind the path (grass tufts, rocks, a fence)
    ///   z 14..24   a river running parallel to the path
    ///   z 26..160  rolling hills rising to a plateau with the observatory and lab buildings, conifers on the slopes
    ///   z 120..220 wind turbines on the ridge, a power line;  z 260..900 snow-capped mountains
    /// Corruption hint (gravity is broken): a few boulders hover just above the meadow, one turbine turns backwards.
    /// </summary>
    public class ValleyBackdrop
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly System.Random rnd;
        readonly float x0, x1;
        Transform near, mid, far;

        public ValleyBackdrop(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() * 13 + 5);
            x0 = plan.minX; x1 = plan.maxX;
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        Transform Layer(string name, DepthLayer layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<DepthLayerTag>().layer = layer;
            return go.transform;
        }

        // the landscape height everywhere behind and in front of the path
        static float Height(float x, float z)
        {
            float h;
            if (z < 12f) h = NatureGen.Fbm(x * 0.05f, z * 0.05f) * 0.6f - 0.3f;                      // meadow
            else if (z < 26f)
            {
                float riverT = Mathf.Abs(z - 19f) / 7f;                                              // river channel
                h = Mathf.Lerp(-1.8f, 0f, Mathf.SmoothStep(0, 1, riverT));
            }
            else
            {
                float rise = Mathf.SmoothStep(0, 1, (z - 26f) / 140f);
                h = rise * 34f + NatureGen.Fbm(x * 0.012f, z * 0.012f) * 26f * rise;
                // plateau for the campus
                float plateau = Mathf.Exp(-Mathf.Pow((z - 80f) / 26f, 2f));
                h = Mathf.Lerp(h, 16f, plateau * 0.85f);
            }
            if (z > 220f)
            {
                float m = Mathf.SmoothStep(0, 1, (z - 220f) / 260f);
                h += m * (60f + NatureGen.Ridge(x * 0.0035f, z * 0.0035f) * 300f);
            }
            return h;
        }

        /// <summary>Highest terrain point under a footprint: the building floor sits on it, the plinth reaches down the slope.</summary>
        static float MaxHeight(float cx, float cz, float hw, float hd)
        {
            float m = float.MinValue;
            for (int i = -2; i <= 2; i++) for (int j = -2; j <= 2; j++) m = Mathf.Max(m, Height(cx + hw * i / 2f, cz + hd * j / 2f));
            return m;
        }

        public void Build()
        {
            var bg = new GameObject("Valley").transform;
            bg.SetParent(root, false);
            near = Layer("L2 Meadow", DepthLayer.L2_Near); near.SetParent(bg, true);
            mid = Layer("L3 Hills", DepthLayer.L3_Mid); mid.SetParent(bg, true);
            far = Layer("L4 Mountains", DepthLayer.L4_Far); far.SetParent(bg, true);

            float sx0 = x0 - 300, sx1 = x1 + 300;
            var hills = CityMaterials.Terrain("Grass005", "Rock064", new Color(1f, 1.05f, 0.88f), new Color(0.8f, 0.78f, 0.75f), 9999f);
            var mountains = CityMaterials.Terrain("Grass005", "Rock058", new Color(0.7f, 0.76f, 0.62f), new Color(0.75f, 0.74f, 0.73f), 190f);
            var terrainNear = new NatureGen(near, rnd);
            terrainNear.Terrain("Meadow", sx0, sx1, 6f, 170f, 2.5f, Height, hills);
            terrainNear.Terrain("Front", sx0, sx1, -90f, -5.9f, 6f, (x, z) => NatureGen.Fbm(x * 0.05f, z * 0.05f) * 0.5f - 0.35f, hills);
            new NatureGen(far, rnd).Terrain("Mountains", sx0 - 600, sx1 + 600, 165f, 1100f, 14f, Height, mountains, 300f);

            // river
            var riverMb = new MeshBuilder(1);
            riverMb.Box(new Vector3(sx0, -1.0f, 13f), new Vector3(sx1 - sx0, 0.01f, 12f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(riverMb.ToObject("River", near, new[] { CityMaterials.Water(new Color(0.03f, 0.07f, 0.07f), new Color(0.07f, 0.13f, 0.12f), new Vector2(0.6f, 0f)) }, false));

            var nat = new NatureGen(near, rnd);
            // meadow: tufts, rocks, a wooden fence along the river bank
            for (float x = sx0; x < sx1; x += R(1.5f, 4f)) nat.GrassTuft(new Vector3(x, Height(x, 8f), R(6.5f, 11.5f)), R(0.3f, 0.6f));
            for (float x = sx0; x < sx1; x += R(10f, 25f)) nat.Rock(new Vector3(x, Height(x, 9f), R(7f, 11f)), R(0.4f, 1.3f));
            var fence = new MeshBuilder(1);
            for (float x = sx0; x < sx1; x += 2.5f)
            {
                float y = Height(x, 12.2f);
                fence.BoxC(new Vector3(x, y + 0.55f, 12.2f), new Vector3(0.1f, 1.1f, 0.1f), 0);
                fence.BoxC(new Vector3(x + 1.25f, y + 0.85f, 12.2f), new Vector3(2.5f, 0.07f, 0.05f), 0);
                fence.BoxC(new Vector3(x + 1.25f, y + 0.45f, 12.2f), new Vector3(2.5f, 0.07f, 0.05f), 0);
            }
            SceneSpawn.MarkStatic(fence.ToObject("Fence", near, new[] { CityMaterials.PBR("Wood066", new Color(0.7f, 0.62f, 0.52f), 1.2f) }));
            // trees: broadleaf along the river, conifers on the slopes
            for (float x = sx0; x < sx1; x += R(9f, 18f)) nat.Broadleaf(new Vector3(x, Height(x, 27f), R(26f, 30f)), R(7f, 10f));
            nat.Flush("Meadow");
            var forest = new NatureGen(mid, rnd);
            for (int i = 0; i < 520; i++)
            {
                float x = R(sx0, sx1), z = R(32f, 165f);
                if (Mathf.Abs(z - 80f) < 20f && Mathf.Abs(x - (x0 + x1) * 0.5f) < 70f) continue;   // campus clearing
                if (NatureGen.Fbm(x * 0.02f, z * 0.02f) < 0.42f) continue;                           // clumps, not a carpet
                forest.Conifer(new Vector3(x, Height(x, z) - 0.2f, z), R(9f, 17f));
            }
            for (int i = 0; i < 40; i++) { float x = R(sx0, sx1), z = R(30f, 160f); forest.Rock(new Vector3(x, Height(x, z), z), R(1.5f, 4f)); }

            // research campus on the plateau
            float cx = (x0 + x1) * 0.5f;
            forest.Observatory(new Vector3(cx - 22f, Height(cx - 22f, 82f), 82f), 9f);
            WallCrawlers.BeginCollect();
            var lab = BuildingGen.Random(rnd, 30f, 18f, 12f, 16f, true, 1f);
            lab.style = CityMaterials.FacadeStyle.GlassTower; lab.setbacks = false; lab.foundation = 10f; lab.podiumStyle = CityMaterials.FacadeStyle.Stone; lab.podiumFloors = 1;
            BuildingGen.Build(mid, new Vector3(cx + 18f, MaxHeight(cx + 18f, 80f, 16f, 10f) + 0.2f, 80f), lab, rnd, "ResearchLab");
            var lab2 = BuildingGen.Random(rnd, 21f, 15f, 8f, 10f, false, 1f);
            lab2.style = CityMaterials.FacadeStyle.Office; lab2.foundation = 10f;
            BuildingGen.Build(mid, new Vector3(cx + 50f, MaxHeight(cx + 50f, 88f, 12f, 8f) + 0.2f, 88f), lab2, rnd, "Workshop");

            WallCrawlers.Finish(mid, 5);
            // wind turbines on the ridge and a power line
            int t = 0;
            for (float x = sx0 + 40; x < sx1; x += R(70f, 110f), t++)
            {
                float z = R(150f, 200f);
                forest.WindTurbine(new Vector3(x, Height(x, z) - 1f, z), R(55f, 70f), t == 2);
            }
            Vector3[] prev = null;
            for (float x = sx0; x < sx1; x += 90f)
            {
                float z = 120f + Mathf.Sin(x * 0.01f) * 8f;
                var pts = forest.Pylon(new Vector3(x, Height(x, z) - 0.5f, z), 28f);
                if (prev != null) for (int k = 0; k < pts.Length; k++) forest.Cable(prev[k], pts[k], 2.8f);
                prev = pts;
            }
            forest.Flush("Hills");

            // corruption hint: boulders hovering a little above the meadow
            for (int i = 0; i < 5; i++)
            {
                float x = R(x0, x1), z = R(7f, 11f);
                var hb = new MeshBuilder(1);
                hb.Sphere(Vector3.zero, new Vector3(1f, 0.7f, 0.9f) * R(0.6f, 1.2f), 8, 12, 0, 0.25f, i);
                var go = hb.ToObject("FloatingBoulder", near, new[] { CityMaterials.PBR("Rock058", new Color(0.78f, 0.76f, 0.74f), 3f) });
                go.transform.localPosition = new Vector3(x, Height(x, z) + R(1.2f, 2.2f), z);
                var bob = go.AddComponent<FloatingBob>();
                bob.bobHeight = 0.12f; bob.bobSpeed = 0.35f; bob.tilt = 1.5f;
                bob.corruptedMultiplier = 1.5f; bob.corruptedLift = new Vector3(0, 0.5f, 0); bob.corruptedTilt = new Vector3(0, 0, 3f);
            }
        }
    }
}
