using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Chemical Chaos as a real place: a chemical plant.
    ///   z  6..9    the yard continues; z 10..13 a long pipe rack (pipes, cable tray) running the whole level
    ///   z 16..65   tank farm: storage tanks, distillation columns, pump houses
    ///   z 70..220  cooling towers with steam, warehouses, a flare stack;  z 260+ distant stacks and the city
    /// Corruption hint: a faint green vapour leaks from the pipe rack, the flare burns a little green.
    /// </summary>
    public class PlantBackdrop
    {
        readonly LevelPlan plan;
        readonly Transform root;
        readonly System.Random rnd;
        readonly float x0, x1;
        Transform near, mid, far;

        Material white, steel, pipeMat, rust, concrete, darkSteel;

        public PlantBackdrop(LevelPlan plan, Transform root)
        {
            this.plan = plan; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() * 29 + 11);
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
            var bg = new GameObject("Plant").transform;
            bg.SetParent(root, false);
            near = Layer("L2 Yard", DepthLayer.L2_Near); near.SetParent(bg, true);
            mid = Layer("L3 Plant", DepthLayer.L3_Mid); mid.SetParent(bg, true);
            far = Layer("L4 Stacks", DepthLayer.L4_Far); far.SetParent(bg, true);

            white = CityMaterials.Solid(new Color(0.86f, 0.87f, 0.86f), 0.4f, 0.15f);
            steel = CityMaterials.PBR("PaintedMetal004", new Color(0.42f, 0.44f, 0.46f), 1.5f, 0.8f, 0.6f);
            darkSteel = CityMaterials.PBR("Metal009", new Color(0.2f, 0.21f, 0.22f), 1f, 0.7f, 0.8f);
            pipeMat = CityMaterials.Solid(new Color(0.62f, 0.64f, 0.66f), 0.3f, 0.85f);
            rust = CityMaterials.PBR("CorrugatedSteel009", new Color(0.7f, 0.66f, 0.62f), 2f, 1f, 0.4f);
            concrete = CityMaterials.PBR("Concrete042A", new Color(0.74f, 0.73f, 0.7f), 4f);

            float sx0 = x0 - 300, sx1 = x1 + 300;
            var ground = new MeshBuilder(1);
            ground.Box(new Vector3(sx0 - 1500, -0.3f, 6f), new Vector3(sx1 - sx0 + 3000, 0.3f, 2200f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            ground.Box(new Vector3(sx0 - 400, -0.32f, -80f), new Vector3(sx1 - sx0 + 800, 0.3f, 74f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(ground.ToObject("PlantGround", near, new[] { concrete }, false));

            PipeRack(sx0, sx1);
            for (float x = sx0; x < sx1; x += R(20f, 36f)) TankCluster(new Vector3(x, 0, R(20f, 55f)));
            for (float x = sx0 + 30; x < sx1; x += R(55f, 90f)) Column(new Vector3(x, 0, R(30f, 60f)), R(28f, 46f));
            for (float x = sx0 + 60; x < sx1; x += R(140f, 200f)) CoolingTower(new Vector3(x, 0, R(110f, 170f)), R(60f, 85f));
            WallCrawlers.BeginCollect();
            for (float x = sx0; x < sx1; x += R(40f, 70f))
            {
                var spec = new BuildingGen.Spec { style = CityMaterials.FacadeStyle.Industrial, variant = rnd.Next(4), width = R(24f, 48f), depth = R(18f, 30f), height = R(10f, 16f), glitch = 1f };
                BuildingGen.Build(mid, new Vector3(x, 0, R(80f, 100f)), spec, rnd, "Warehouse");
            }
            WallCrawlers.Finish(mid, 10);
            Flare(new Vector3((x0 + x1) * 0.5f + R(-40f, 40f), 0, 140f), 55f);
            for (float x = x0 - 600; x < x1 + 600; x += R(30f, 70f))
            {
                var m = new MeshBuilder(1);
                float h = R(50f, 110f);
                m.Cylinder(new Vector3(x, 0, R(300f, 600f)), R(2.5f, 4f), R(1.8f, 3f), h, 14, 0);
                SceneSpawn.MarkStatic(m.ToObject("Stack", far, new[] { rnd.NextDouble() < 0.5 ? white : steel }));
            }
            for (float x = x0 - 500; x < x1 + 500; x += R(40f, 90f))
            {
                var spec = BuildingGen.Random(rnd, R(18f, 36f), R(18f, 36f), 40f, 140f, true, 0.6f);
                BuildingGen.Build(far, new Vector3(x, 0, R(650f, 900f)), spec, rnd, "DistantCity");
            }
        }

        void PipeRack(float sx0, float sx1)
        {
            var mb = new MeshBuilder(3);
            float h = 6.5f;
            for (float x = sx0; x < sx1; x += 8f)
                foreach (float z in new[] { 10f, 13f })
                {
                    mb.BoxC(new Vector3(x, h / 2, z), new Vector3(0.3f, h, 0.3f), 0);
                    mb.BoxC(new Vector3(x, 0.15f, z), new Vector3(0.8f, 0.3f, 0.8f), 2);
                }
            for (float x = sx0; x < sx1; x += 8f) { mb.BoxC(new Vector3(x, h - 0.2f, 11.5f), new Vector3(0.25f, 0.3f, 3.3f), 0); mb.BoxC(new Vector3(x, h - 2.4f, 11.5f), new Vector3(0.25f, 0.25f, 3.3f), 0); }
            float[] zs = { 10.4f, 11.1f, 11.9f, 12.6f };
            float[] rs = { 0.28f, 0.42f, 0.22f, 0.34f };
            for (int i = 0; i < zs.Length; i++) mb.Tube(new Vector3(sx0, h + rs[i] - 0.05f, zs[i]), new Vector3(sx1, h + rs[i] - 0.05f, zs[i]), rs[i], rs[i], 12, 1);
            mb.BoxC(new Vector3((sx0 + sx1) / 2, h - 2.1f, 11.5f), new Vector3(sx1 - sx0, 0.12f, 1.2f), 0);   // cable tray
            SceneSpawn.MarkStatic(mb.ToObject("PipeRack", near, new[] { steel, pipeMat, concrete }));
            // corruption hint: a faint green vapour leak somewhere along the rack
            var leak = new GameObject("VapourLeak");
            leak.transform.SetParent(near, false);
            leak.transform.localPosition = new Vector3(Mathf.Lerp(x0, x1, 0.4f), h + 0.5f, 11.1f);
            Plume(leak, new Color(0.55f, 0.9f, 0.45f, 0.18f), 0.4f, 3f, 6);
        }

        void TankCluster(Vector3 p)
        {
            var mb = new MeshBuilder(3);
            int n = rnd.Next(1, 4);
            for (int i = 0; i < n; i++)
            {
                float r = R(4f, 10f), h = R(8f, 18f);
                var c = p + new Vector3(i * (r * 2.3f), 0, R(-4f, 4f));
                mb.Cylinder(c, r, r, h, 28, 0, false);
                mb.Sphere(c + Vector3.up * h, new Vector3(r, r * 0.18f, r), 6, 28, 0);
                mb.Cylinder(c + Vector3.down * 0.3f, r + 1.5f, r + 1.5f, 0.35f, 28, 2);                     // bund ring
                for (float a = 0; a < 6.28f; a += 0.6f)                                                  // roof railing posts
                    mb.Tube(c + new Vector3(Mathf.Cos(a) * r * 0.9f, h + r * 0.1f, Mathf.Sin(a) * r * 0.9f), c + new Vector3(Mathf.Cos(a) * r * 0.9f, h + r * 0.1f + 1.1f, Mathf.Sin(a) * r * 0.9f), 0.04f, 0.04f, 4, 1);
                // a stair spiralling up the side
                for (int s = 0; s < 14; s++)
                {
                    float a = -1.2f + s * 0.12f, y = s / 14f * h;
                    mb.BoxC(c + new Vector3(Mathf.Cos(a) * (r + 0.5f), y, -Mathf.Abs(Mathf.Sin(a)) * (r + 0.5f)), new Vector3(1f, 0.08f, 0.6f), 1);
                }
            }
            SceneSpawn.MarkStatic(mb.ToObject("Tanks", mid, new[] { white, steel, concrete }));
        }

        void Column(Vector3 p, float h)
        {
            var mb = new MeshBuilder(2);
            float r = R(1.6f, 2.6f);
            mb.Cylinder(p, r, r, h, 18, 0);
            for (float y = 5f; y < h; y += 6f)
            {
                mb.Cylinder(p + Vector3.up * y, r + 1.2f, r + 1.2f, 0.15f, 18, 1);
                for (float a = 0; a < 6.28f; a += 0.9f) mb.Tube(p + new Vector3(Mathf.Cos(a) * (r + 1.1f), y, Mathf.Sin(a) * (r + 1.1f)), p + new Vector3(Mathf.Cos(a) * (r + 1.1f), y + 1.1f, Mathf.Sin(a) * (r + 1.1f)), 0.04f, 0.04f, 4, 1);
            }
            mb.Tube(p + new Vector3(r, h * 0.85f, 0), p + new Vector3(r + 6f, h * 0.85f, 0), 0.4f, 0.4f, 10, 0);
            mb.Tube(p + new Vector3(r + 6f, h * 0.85f, 0), p + new Vector3(r + 6f, 0, 0), 0.4f, 0.4f, 10, 0);
            SceneSpawn.MarkStatic(mb.ToObject("Column", mid, new[] { pipeMat, steel }));
        }

        void CoolingTower(Vector3 p, float h)
        {
            var mb = new MeshBuilder(1);
            int segs = 10;
            float rb = h * 0.42f;
            for (int i = 0; i < segs; i++)
            {
                float t0 = i / (float)segs, t1 = (i + 1) / (float)segs;
                float r0 = rb * (0.62f + 0.38f * Mathf.Pow(Mathf.Abs(t0 - 0.72f) / 0.72f, 1.6f));   // hyperbolic waist near the top
                float r1 = rb * (0.62f + 0.38f * Mathf.Pow(Mathf.Abs(t1 - 0.72f) / 0.72f, 1.6f));
                mb.Cylinder(p + Vector3.up * h * t0, r0, r1, h / segs, 40, 0, false);
            }
            SceneSpawn.MarkStatic(mb.ToObject("CoolingTower", mid, new[] { CityMaterials.PBR("Concrete034", new Color(0.82f, 0.81f, 0.78f), 6f) }));
            var steam = new GameObject("Steam");
            steam.transform.SetParent(mid, false);
            steam.transform.localPosition = p + Vector3.up * h;
            Plume(steam, new Color(0.92f, 0.92f, 0.92f, 0.35f), rb * 0.5f, h * 0.6f, 14);
        }

        void Flare(Vector3 p, float h)
        {
            var mb = new MeshBuilder(1);
            mb.Cylinder(p, 1.2f, 0.7f, h, 12, 0);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.09f;
                mb.Tube(p + new Vector3(Mathf.Cos(a) * 8f, 0, Mathf.Sin(a) * 8f), p + Vector3.up * h * 0.7f, 0.12f, 0.08f, 5, 0);
            }
            SceneSpawn.MarkStatic(mb.ToObject("FlareStack", mid, new[] { darkSteel }));
            var flame = new GameObject("Flame");
            flame.transform.SetParent(mid, false);
            flame.transform.localPosition = p + Vector3.up * (h + 1.5f);
            var fm = new MeshBuilder(1);
            fm.Sphere(Vector3.zero, new Vector3(1.2f, 3f, 1.2f), 8, 12, 0, 0.3f, 2);
            fm.ToObject("FlameCore", flame.transform, new[] { CityMaterials.Emissive(new Color(1f, 0.6f, 0.2f), new Color(9f, 4.5f, 1.2f)) }, false);
            flame.AddComponent<FloatingBob>().bobHeight = 0.2f;
            var l = flame.AddComponent<Light>();
            l.type = LightType.Point; l.range = 40f; l.intensity = 3f; l.color = new Color(1f, 0.65f, 0.3f); l.shadows = LightShadows.None;
            flame.AddComponent<WorldLight>();
        }

        /// <summary>A soft rising plume (steam, vapour) from a particle system using the Kenney smoke texture.</summary>
        void Plume(GameObject host, Color color, float radius, float rise, int maxRate)
        {
            var ps = host.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(rise / 8f, rise / 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 1.2f, radius * 2.2f);
            main.startColor = color;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = maxRate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.rotation = new Vector3(-90, 0, 0);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.2f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var vel = ps.velocityOverLifetime; vel.enabled = true;
            // all three axes must use the same curve mode
            vel.x = new ParticleSystem.MinMaxCurve(0.6f, 1.4f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var r = host.GetComponent<ParticleSystemRenderer>();
            var mat = MaterialCache.Instance("palpha");
            var lib = Services.Db != null ? Services.Db.materials : null;
            var smoke = CityMaterials.Library != null ? CityMaterials.Library.Particle("smoke") : (lib != null ? lib.Particle("smoke") : null);
            if (smoke != null) mat.SetTexture("_BaseMap", smoke);
            mat.name = "Plume";
            r.sharedMaterial = MaterialCache.OnCreated != null ? MaterialCache.OnCreated(mat) : mat;
        }
    }
}
