using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The Core as a real place (no void): the roof of the city's tallest tower, at night, where The Source runs.
    /// The gameplay ground is a row of skyscraper rooftops (CityGround "roof" style); around and below them a dense
    /// downtown drops 220 m to lit streets. An antenna mast with aircraft lights, rooftop plant, a helipad.
    /// Corruption hint: some distant towers have their top floors hovering a little off their base.
    /// </summary>
    public class CoreBackdrop
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly System.Random rnd;
        readonly float x0, x1;
        const float Street = -220f;
        Transform near, mid, far;

        public CoreBackdrop(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() * 41 + 9);
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
            var bg = new GameObject("Downtown").transform;
            bg.SetParent(root, false);
            near = Layer("L2 Roofs", DepthLayer.L2_Near); near.SetParent(bg, true);
            mid = Layer("L3 Towers", DepthLayer.L3_Mid); mid.SetParent(bg, true);
            far = Layer("L4 City", DepthLayer.L4_Far); far.SetParent(bg, true);

            // streets far below, faintly lit
            var st = new MeshBuilder(2);
            st.Box(new Vector3(x0 - 1500, Street - 0.3f, -600f), new Vector3(x1 - x0 + 3000, 0.3f, 2600f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            for (float z = -400f; z < 1800f; z += 90f) st.BoxC(new Vector3((x0 + x1) / 2f, Street + 0.05f, z), new Vector3(x1 - x0 + 3000, 0.02f, 0.5f), 1, MeshBuilder.Faces.Top);
            for (float x = x0 - 1400; x < x1 + 1400; x += 110f) st.BoxC(new Vector3(x, Street + 0.05f, 600f), new Vector3(0.5f, 0.02f, 2600f), 1, MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(st.ToObject("Streets", far, new[]
            {
                CityMaterials.PBR("Asphalt031", new Color(0.4f, 0.4f, 0.4f), 8f),
                CityMaterials.Emissive(new Color(1f, 0.85f, 0.6f), new Color(2.5f, 1.9f, 1.1f)),
            }, false));

            // the tower the level stands on continues behind the path (one big roof plane at the back)
            var back = new MeshBuilder(3);
            back.Box(new Vector3(x0 - 40, Street, CityBackdrop.RoadNear + 8f), new Vector3(x1 - x0 + 80, -Street - 0.05f, 28f), 0, 1, Street, MeshBuilder.Faces.Sides | MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(back.ToObject("TowerBack", near, new[] { CityMaterials.Facade(CityMaterials.FacadeStyle.GlassTower, 1), CityMaterials.PBR("Concrete042A", new Color(0.6f, 0.6f, 0.6f), 3f), BuildingGen.Trim(CityMaterials.FacadeStyle.GlassTower, 1f) }));
            Mast(new Vector3((x0 + x1) * 0.5f + R(-30f, 30f), 0f, 24f), 46f);
            Helipad(new Vector3(x0 + (x1 - x0) * 0.25f, 0.02f, 28f));

            // the downtown around: towers rising from the street, many taller than the player's roof, some below
            int corrupted = 0;
            WallCrawlers.BeginCollect();
            for (int i = 0; i < 150; i++)
            {
                float x = R(x0 - 700f, x1 + 700f), z = R(55f, 900f);
                if (z < 120f && x > x0 - 40 && x < x1 + 40) continue;
                var spec = BuildingGen.Random(rnd, R(20f, 44f), R(20f, 44f), 0f, 0f, true, 0.8f);
                spec.style = rnd.NextDouble() < 0.65 ? CityMaterials.FacadeStyle.GlassTower : CityMaterials.FacadeStyle.Office;
                spec.height = -Street + R(-120f, 90f);
                spec.setbacks = rnd.NextDouble() < 0.5;
                spec.podiumStyle = null;
                if (corrupted < 4 && spec.height > 200f && rnd.NextDouble() < 0.2) { spec.corrupted = true; spec.setbacks = true; corrupted++; }
                BuildingGen.Build(z < 300f ? mid : far, new Vector3(x, Street, z), spec, rnd, "Tower");
            }
            WallCrawlers.Finish(mid, 16);
            // a few towers in front (below the camera), so looking down shows a real city
            for (float x = x0 - 120; x < x1 + 120; x += R(40f, 70f))
            {
                var spec = BuildingGen.Random(rnd, R(20f, 36f), R(20f, 30f), 0f, 0f, true, 0.8f);
                spec.height = -Street - R(25f, 90f); spec.podiumStyle = null; spec.setbacks = false;
                BuildingGen.Build(mid, new Vector3(x, Street, R(-70f, -40f)), spec, rnd, "TowerFront");
            }
        }

        void Mast(Vector3 p, float h)
        {
            var mb = new MeshBuilder(2);
            mb.BoxC(p + Vector3.up * 1f, new Vector3(4f, 2f, 4f), 0);
            for (int i = 0; i < 4; i++)
            {
                var f = new Vector3(i % 2 == 0 ? -1.2f : 1.2f, 0, i < 2 ? -1.2f : 1.2f);
                mb.Tube(p + Vector3.up * 2f + f, p + Vector3.up * h + f * 0.1f, 0.12f, 0.06f, 5, 0);
            }
            for (float y = 6f; y < h; y += 6f) mb.Tube(p + new Vector3(-1.2f * (1 - y / h), y, -1.2f * (1 - y / h)), p + new Vector3(1.2f * (1 - y / h), y + 3f, 1.2f * (1 - y / h)), 0.05f, 0.05f, 4, 0);
            SceneSpawn.MarkStatic(mb.ToObject("AntennaMast", near, new[] { CityMaterials.Solid(new Color(0.75f, 0.76f, 0.78f), 0.45f, 0.7f), CityMaterials.Solid(Color.white, 0.5f) }));
            // aircraft warning lights (slow blink)
            for (float y = h * 0.5f; y <= h + 0.01f; y += h * 0.5f)
            {
                var l = new MeshBuilder(1);
                l.Sphere(Vector3.zero, Vector3.one * 0.35f, 6, 8, 0);
                var go = l.ToObject("AircraftLight", near, new[] { CityMaterials.Emissive(new Color(0.6f, 0.05f, 0.04f), new Color(6f, 0.3f, 0.2f)) }, false);
                go.transform.localPosition = p + Vector3.up * y;
                go.AddComponent<WindowLightFlicker>();
            }
        }

        void Helipad(Vector3 p)
        {
            var mb = new MeshBuilder(2);
            mb.Cylinder(p, 9f, 9f, 0.05f, 40, 0);
            mb.BoxC(p + new Vector3(-1.8f, 0.06f, 0), new Vector3(0.6f, 0.02f, 5f), 1, MeshBuilder.Faces.Top);
            mb.BoxC(p + new Vector3(1.8f, 0.06f, 0), new Vector3(0.6f, 0.02f, 5f), 1, MeshBuilder.Faces.Top);
            mb.BoxC(p + new Vector3(0, 0.06f, 0), new Vector3(3f, 0.02f, 0.6f), 1, MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(mb.ToObject("Helipad", near, new[] { CityMaterials.Solid(new Color(0.22f, 0.23f, 0.25f), 0.6f), CityMaterials.Solid(new Color(0.92f, 0.92f, 0.9f), 0.6f) }, false));
        }
    }
}
