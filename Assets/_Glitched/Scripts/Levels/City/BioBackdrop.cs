using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Bio-Zone as a real place: the atrium of a biotech research centre, under a glass roof.
    ///   z  6..16   atrium floor with planted beds, benches, specimen tanks glowing with cultures
    ///   z 18..34   the back wall: two storeys of glazed labs (interior-mapped rooms), a mezzanine walkway
    ///   overhead   steel roof trusses and glazing (sunlight casts their shadows), a DNA helix sculpture, big screens
    /// Corruption hint: emergency red tint in the lab rooms and on the screens; restored = clean daylight.
    /// </summary>
    public class BioBackdrop
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly System.Random rnd;
        readonly float x0, x1;
        Transform near, mid;

        public BioBackdrop(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() * 37 + 1);
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

        public void Build()
        {
            var bg = new GameObject("BioCentre").transform;
            bg.SetParent(root, false);
            near = Layer("L2 Atrium", DepthLayer.L2_Near); near.SetParent(bg, true);
            mid = Layer("L3 Building", DepthLayer.L3_Mid); mid.SetParent(bg, true);
            float sx0 = x0 - 120, sx1 = x1 + 120;
            var floor = CityMaterials.PBR("Concrete046", new Color(0.93f, 0.93f, 0.92f), 4f, 0.45f);
            var steel = CityMaterials.Solid(new Color(0.88f, 0.89f, 0.9f), 0.35f, 0.7f);
            var glass = CityMaterials.Glass(new Color(0.6f, 0.75f, 0.8f));

            // floor (back and front of the path)
            var fl = new MeshBuilder(1);
            fl.Box(new Vector3(sx0, -0.3f, 5.9f), new Vector3(sx1 - sx0, 0.3f, 30f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            fl.Box(new Vector3(sx0, -0.32f, -40f), new Vector3(sx1 - sx0, 0.3f, 34f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(fl.ToObject("AtriumFloor", near, new[] { floor }, false));

            // back wall: glazed labs over two storeys (the facade shader draws the rooms)
            var wallSpec = new BuildingGen.Spec { style = CityMaterials.FacadeStyle.GlassTower, variant = 2, width = sx1 - sx0, depth = 12f, height = 16f, glitch = 1f };
            WallCrawlers.BeginCollect();
            var labs = BuildingGen.Build(mid, new Vector3((sx0 + sx1) / 2f, 0f, 34f), wallSpec, rnd, "LabWing");
            WallCrawlers.Finish(mid, 9);
            // mezzanine walkway with a glass balustrade in front of the upper labs
            var mez = new MeshBuilder(3);
            mez.BoxC(new Vector3((sx0 + sx1) / 2f, 7.4f, 26.5f), new Vector3(sx1 - sx0, 0.35f, 3f), 0);
            mez.BoxC(new Vector3((sx0 + sx1) / 2f, 8.1f, 25.1f), new Vector3(sx1 - sx0, 1.0f, 0.04f), 1);
            mez.BoxC(new Vector3((sx0 + sx1) / 2f, 8.62f, 25.1f), new Vector3(sx1 - sx0, 0.05f, 0.08f), 2);
            for (float x = sx0; x < sx1; x += 9f) mez.BoxC(new Vector3(x, 3.7f, 26.5f), new Vector3(0.3f, 7.4f, 0.3f), 2);
            SceneSpawn.MarkStatic(mez.ToObject("Mezzanine", mid, new[] { CityMaterials.PBR("Concrete046", new Color(0.9f, 0.89f, 0.86f), 3f), glass, steel }));

            // glass roof on steel trusses (casts patterned shadows)
            var roof = new MeshBuilder(2);
            float ry = 24f;
            for (float x = sx0; x < sx1; x += 6f)
            {
                roof.Tube(new Vector3(x, ry, -12f), new Vector3(x, ry + 5f, 12f), 0.15f, 0.15f, 6, 0);
                roof.Tube(new Vector3(x, ry + 5f, 12f), new Vector3(x, ry, 36f), 0.15f, 0.15f, 6, 0);
                roof.Tube(new Vector3(x, ry, -12f), new Vector3(x, ry, 36f), 0.1f, 0.1f, 6, 0);
            }
            roof.Tube(new Vector3(sx0, ry + 5f, 12f), new Vector3(sx1, ry + 5f, 12f), 0.2f, 0.2f, 6, 0);
            SceneSpawn.MarkStatic(roof.ToObject("RoofTrusses", mid, new[] { steel, glass }));
            // columns holding the roof at the front (out of the walking line) and back
            var cols = new MeshBuilder(1);
            for (float x = sx0; x < sx1; x += 24f) { cols.Cylinder(new Vector3(x, 0, 17f), 0.35f, 0.35f, ry, 14, 0); cols.Cylinder(new Vector3(x, 0, -12f), 0.35f, 0.35f, ry, 14, 0); }
            SceneSpawn.MarkStatic(cols.ToObject("Columns", mid, new[] { steel }));

            // planted beds with trees, benches, specimen tanks
            var street = new StreetGen(near, rnd, 1f);
            for (float x = sx0 + 6; x < sx1; x += R(14f, 22f))
            {
                street.Planter(new Vector3(x, 0f, R(9f, 12f)), R(3f, 5f));
                if (rnd.NextDouble() < 0.6) street.Tree(new Vector3(x + R(-1f, 1f), 0.6f, 10.5f), R(4.5f, 6f));
                if (rnd.NextDouble() < 0.4) street.Bench(new Vector3(x + 4f, 0f, 8f));
            }
            street.Flush("Atrium");
            for (float x = sx0 + 12; x < sx1; x += R(26f, 40f)) Tank(new Vector3(x, 0, R(14f, 18f)));
            Helix(new Vector3((x0 + x1) * 0.5f, 2f, 15f), 14f);
            for (float x = sx0 + 20; x < sx1; x += R(45f, 70f)) Screen(new Vector3(x, 12f, 27.9f), rnd.NextDouble() < 0.5 ? "dna" : "ecg");
        }

        void Tank(Vector3 p)
        {
            var mb = new MeshBuilder(3);
            mb.Cylinder(p, 1.3f, 1.3f, 0.5f, 20, 0);
            mb.Cylinder(p + Vector3.up * 3.6f, 1.35f, 1.35f, 0.4f, 20, 0);
            mb.Cylinder(p + Vector3.up * 0.5f, 1.22f, 1.22f, 3.1f, 24, 1, false);
            SceneSpawn.MarkStatic(mb.ToObject("SpecimenTank", near, new[]
            {
                CityMaterials.Solid(new Color(0.85f, 0.86f, 0.87f), 0.35f, 0.7f),
                CityMaterials.Water(new Color(0.04f, 0.12f, 0.1f), new Color(0.1f, 0.3f, 0.24f), new Vector2(0f, 0.05f), new Color(0.1f, 0.5f, 0.35f)),
                CityMaterials.Glass(Color.white),
            }));
        }

        void Helix(Vector3 p, float h)
        {
            var mb = new MeshBuilder(3);
            int n = 40;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, y = t * h, a = t * Mathf.PI * 6f;
                var A = p + new Vector3(Mathf.Cos(a) * 1.4f, y, Mathf.Sin(a) * 1.4f);
                var B = p + new Vector3(Mathf.Cos(a + Mathf.PI) * 1.4f, y, Mathf.Sin(a + Mathf.PI) * 1.4f);
                mb.Sphere(A, Vector3.one * 0.22f, 6, 8, 0);
                mb.Sphere(B, Vector3.one * 0.22f, 6, 8, 1);
                if (i % 2 == 0) mb.Tube(A, B, 0.05f, 0.05f, 5, 2);
            }
            mb.Cylinder(p + Vector3.down * 2f, 2.4f, 2.4f, 2f, 24, 2);
            var go = mb.ToObject("DNAHelix", near, new[]
            {
                CityMaterials.Solid(new Color(0.3f, 0.55f, 0.9f), 0.25f, 0.4f),
                CityMaterials.Solid(new Color(0.9f, 0.35f, 0.45f), 0.25f, 0.4f),
                CityMaterials.Solid(new Color(0.85f, 0.86f, 0.87f), 0.35f, 0.7f),
            });
            go.AddComponent<Spinner>().speed = new Vector3(0, 8f, 0);
        }

        void Screen(Vector3 p, string holo)
        {
            var mb = new MeshBuilder(1);
            mb.BoxC(p + Vector3.back * 0.06f, new Vector3(9.4f, 5.4f, 0.12f), 0);
            SceneSpawn.MarkStatic(mb.ToObject("ScreenFrame", near, new[] { CityMaterials.Solid(new Color(0.08f, 0.08f, 0.09f), 0.4f, 0.5f) }));
            var sc = new MeshBuilder(1);
            sc.BoxC(p + Vector3.back * 0.13f, new Vector3(9f, 5f, 0.01f), 0, MeshBuilder.Faces.Front);
            SceneSpawn.MarkStatic(sc.ToObject("Screen", near, new[] { CityMaterials.Screen(LevelBuilder.Holo(holo), new Color(1.1f, 1.2f, 1.3f)) }, false));
        }
    }
}
