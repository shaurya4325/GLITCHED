using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Glitched
{
    /// <summary>
    /// Builds the living 3D backdrop of a level as REAL geometry at real depths (Section 3.1), so the perspective
    /// camera produces parallax and turning building sides for free:
    ///   L0 foreground occluders (z -7..-3), L2 near city (z 12..32), L3 mid city (z 45..130),
    ///   L4 far skyline impostors (z 160..400), L5 sky events. Plus traffic, crowds, particles and region flashes.
    /// </summary>
    public class BackdropBuilder
    {
        readonly LevelPlan plan;
        readonly GameDatabase db;
        readonly RegionThemeSO theme;
        readonly EnvironmentKitSO kit;
        readonly Transform root;
        readonly System.Random rnd;
        Transform L0, L2, L2d, L3, L4, L5, life;
        float x0, x1;

        public BackdropBuilder(LevelPlan plan, GameDatabase db, RegionThemeSO theme, EnvironmentKitSO kit, Transform root)
        {
            this.plan = plan; this.db = db; this.theme = theme; this.kit = kit; this.root = root;
            rnd = new System.Random(plan.levelId.GetHashCode() * 31 + 7);
            x0 = plan.minX; x1 = plan.maxX;
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        bool Chance(float p) => rnd.NextDouble() < p;
        T Pick<T>(IList<T> l) => l == null || l.Count == 0 ? default : l[rnd.Next(l.Count)];
        Texture2D Tex(IList<Texture2D> l) => Pick(l);

        Transform Layer(string name, DepthLayer layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<DepthLayerTag>().layer = layer;
            return go.transform;
        }

        Transform Group(string name, Transform parent)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        public void Build()
        {
            var bg = new GameObject("Backdrop").transform;
            bg.SetParent(root, false);
            L0 = Layer("L0 Foreground", DepthLayer.L0_Foreground); L0.SetParent(bg, true);
            L2 = Layer("L2 Near", DepthLayer.L2_Near); L2.SetParent(bg, true);
            L2d = Layer("L2 Near Detail", DepthLayer.L2_NearDetail); L2d.SetParent(bg, true);
            L3 = Layer("L3 Mid", DepthLayer.L3_Mid); L3.SetParent(bg, true);
            L4 = Layer("L4 Far", DepthLayer.L4_Far); L4.SetParent(bg, true);
            L5 = Layer("L5 Sky Events", DepthLayer.L5_SkyEvents); L5.SetParent(bg, true);
            life = Group("BackgroundLife", bg);

            switch (plan.arcId)
            {
                case "PhysicsValley": Physics(); break;
                case "ChemicalChaos": Chem(); break;
                case "BioZone": Bio(); break;
                case "TheCore": Core(); break;
                default: CodeCity(); break;
            }
            Skyline();
            Foreground();
            Life();
            if (plan.kind == LevelKind.BossArena) ArenaCentrepiece();
        }

        void Static(GameObject g) => SceneSpawn.MarkStatic(g);

        Transform Box(Transform parent, Vector3 pos, Vector3 size, Material m, string name = "Box", bool isStatic = true)
        {
            var t = PlaceholderFactory.Part(PrimitiveType.Cube, parent, pos, size, m, name);
            t.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (isStatic) Static(t.gameObject);
            return t;
        }

        // ================================================================== CODE CITY
        void CodeCity()
        {
            var street = MaterialCache.Glitch(new Color(0.22f, 0.23f, 0.3f), 0.4f, kit != null && kit.surfaceTextures.Count > 0 ? kit.surfaceTextures[0] : null);
            Box(L2, new Vector3((x0 + x1) / 2, -4.2f, 24), new Vector3(x1 - x0 + 200, 0.4f, 40), street, "Street");
            var lane = MaterialCache.Emissive(Color.black, theme.accentRestored * 1.8f, 0f, 0.6f);
            for (int i = 0; i < 2; i++) Box(L2, new Vector3((x0 + x1) / 2, -3.98f, 14 + i * 14), new Vector3(x1 - x0 + 200, 0.05f, 0.3f), lane, "LaneLine");

            // L2: near city
            for (float x = x0 - 70; x < x1 + 70; x += R(13f, 21f))
            {
                float z = R(22f, 38f);
                if (kit != null && kit.buildings.Count > 0 && Chance(0.35f)) KitBuilding(L2, new Vector3(x, -4, z), R(11, 18));
                else Tower(L2, new Vector3(x, -4, z), R(6, 10), R(10, 26), R(6, 10), true);
            }
            // street-level detail: props and a crowd
            if (kit != null)
                for (float x = x0 - 40; x < x1 + 40; x += R(5f, 11f))
                {
                    var p = Pick(kit.props);
                    if (p != null) Static(SceneSpawn.Normalized(p, L2d, new Vector3(x, -4, R(7f, 11f)), R(0.8f, 1.6f), R(0, 360)));
                }
            var crowd = Group("Crowd", L2d);
            crowd.localPosition = new Vector3(x0 - 10, -4, 9);
            crowd.gameObject.AddComponent<CrowdLoop>().length = x1 - x0 + 20;

            // L3: mid city with floating towers
            for (float x = x0 - 110; x < x1 + 110; x += R(11f, 22f))
                Tower(L3, new Vector3(x, R(-10f, -4f), R(48f, 125f)), R(9, 18), R(30, 95), R(9, 18), false);

            // flying traffic and drones
            TrafficLane(L3, 50, 16, "car", 14, 0.012f, kit != null ? kit.vehicles : null, 1.8f);
            TrafficLane(L3, 70, 26, "car", 10, 0.009f, kit != null ? kit.vehicles : null, 2.4f);
            TrafficLane(L2d, 34, 10, "drone", 6, 0.02f, null, 1f);

            // binary rain
            var rain = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), L3, new Vector3((x0 + x1) / 2, 45, 138), new Vector3(x1 - x0 + 260, 120, 1), MaterialCache.Get("holo"), "BinaryRain");
            var hb = rain.gameObject.AddComponent<HologramBillboard>();
            hb.content = LevelBuilder.Holo("binary_rain");
            hb.color = new Color(0.1f, 0.5f, 0.6f);
            hb.scroll = new Vector2(0, -0.06f);
            var mpbRain = new MaterialPropertyBlock();
            rain.GetComponent<Renderer>().GetPropertyBlock(mpbRain);
            mpbRain.SetVector("_BaseMap_ST", new Vector4((x1 - x0 + 260) / 60f, 3f, 0, 0));
            rain.GetComponent<Renderer>().SetPropertyBlock(mpbRain);
        }

        static readonly string[] HoloAds = { "welcome", "ad_ram", "error404", "code_lines", "binary_rain" };

        void Tower(Transform parent, Vector3 basePos, float w, float h, float d, bool near)
        {
            var g = Group("Tower", parent);
            g.localPosition = basePos;
            var tex = kit != null ? Tex(kit.facadeTextures) : null;
            Color tint = Color.Lerp(new Color(0.55f, 0.6f, 0.75f), Color.white, R(0f, 0.4f));
            Color win = Chance(0.5f) ? new Color(1.3f, 1.0f, 0.55f) : new Color(0.4f, 1.1f, 1.6f);
            var m = MaterialCache.Facade(tex, tint, win * (near ? 0.6f : 0.9f), 1f, near ? 0.11f : 0.06f, 1f);
            var body = Box(g, new Vector3(0, h / 2, 0), new Vector3(w, h, d), m, "Body", false);
            // a setback crown
            if (Chance(0.5f)) Box(g, new Vector3(0, h + h * 0.08f, 0), new Vector3(w * 0.7f, h * 0.16f, d * 0.7f), m, "Crown", false);
            // neon strip
            var neon = MaterialCache.Emissive(Color.black, (Chance(0.5f) ? theme.accentRestored : theme.accentCorrupted) * 2f, 0, 1f);
            Box(g, new Vector3(0, h * R(0.3f, 0.8f), -d / 2 - 0.05f), new Vector3(w * 0.9f, 0.25f, 0.06f), neon, "Neon", false);
            // antenna with a blinking light
            if (Chance(0.6f))
            {
                Box(g, new Vector3(w * 0.25f, h + 3f, 0), new Vector3(0.2f, 6f, 0.2f), MaterialCache.Glitch(new Color(0.2f, 0.2f, 0.25f), 1f), "Antenna", false);
                var tip = PlaceholderFactory.Part(PrimitiveType.Sphere, g, new Vector3(w * 0.25f, h + 6.2f, 0), Vector3.one * 0.6f, MaterialCache.Emissive(Color.black, new Color(2.5f, 0.2f, 0.3f), 1f, 1f), "Beacon");
                tip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // roof props from the kit
            if (near && kit != null && kit.props.Count > 0 && Chance(0.6f))
            {
                var p = Pick(kit.props);
                var pr = SceneSpawn.Normalized(p, g, g.position + new Vector3(R(-w * 0.3f, w * 0.3f), h, R(-d * 0.3f, d * 0.3f)), R(1.2f, 2.2f), R(0, 360));
                if (pr != null) pr.AddComponent<GlitchProp>();
            }
            // hologram billboard on the front face
            if (near && Chance(0.35f))
            {
                var q = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), g, new Vector3(0, h * R(0.45f, 0.75f), -d / 2 - 0.2f), new Vector3(w * 0.9f, w * 0.45f, 1), MaterialCache.Get("holo"), "Billboard");
                var hb = q.gameObject.AddComponent<HologramBillboard>();
                hb.content = LevelBuilder.Holo(HoloAds[rnd.Next(HoloAds.Length)]);
                hb.color = (Chance(0.5f) ? theme.accentRestored : theme.accentCorrupted) * 1.8f;
                hb.scroll = Chance(0.5f) ? new Vector2(0, 0.05f) : Vector2.zero;
            }
            // floating when corrupted
            if (Chance(near ? 0.4f : 0.3f))
            {
                var bob = g.gameObject.AddComponent<FloatingBob>();
                bob.bobHeight = 0.3f; bob.bobSpeed = R(0.3f, 0.7f); bob.tilt = 1f;
                bob.corruptedLift = new Vector3(0, R(2f, near ? 7f : 18f), 0);
                bob.corruptedTilt = new Vector3(0, 0, R(-9f, 9f));
            }
            else Static(g.gameObject);
        }

        void KitBuilding(Transform parent, Vector3 pos, float height)
        {
            var b = SceneSpawn.Normalized(Pick(kit.buildings), parent, pos, height, Chance(0.5f) ? 0 : 180);
            if (b == null) return;
            if (Chance(0.45f))
            {
                var bob = b.AddComponent<FloatingBob>();
                bob.bobHeight = 0.25f; bob.tilt = 0.8f;
                bob.corruptedLift = new Vector3(0, R(2f, 6f), 0);
                bob.corruptedTilt = new Vector3(0, 0, R(-8f, 8f));
            }
            else Static(b);
            b.AddComponent<WindowLightFlicker>();
        }

        // ================================================================== PHYSICS VALLEY
        void Physics()
        {
            var rock = MaterialCache.Glitch(new Color(0.55f, 0.45f, 0.42f), 0.8f, kit != null && kit.surfaceTextures.Count > 0 ? kit.surfaceTextures[0] : null);
            var grass = MaterialCache.Glitch(new Color(0.45f, 0.75f, 0.35f), 0.5f, kit != null && kit.surfaceTextures.Count > 1 ? kit.surfaceTextures[1] : null);
            Box(L3, new Vector3((x0 + x1) / 2, -42, 120), new Vector3(x1 - x0 + 500, 1, 240), grass, "ValleyFloor");
            for (float x = x0 - 60; x < x1 + 60; x += R(12f, 20f)) Island(L2, new Vector3(x, R(-6f, 10f), R(14f, 34f)), R(5f, 10f), rock, grass, true);
            for (float x = x0 - 120; x < x1 + 120; x += R(18f, 32f)) Island(L3, new Vector3(x, R(-15f, 30f), R(55f, 130f)), R(14f, 30f), rock, grass, false);
            // wind turbines, pendulums and Tesla towers
            for (float x = x0 - 20; x < x1 + 40; x += R(40f, 70f)) Turbine(L2d, new Vector3(x, R(-4f, 4f), R(26f, 36f)));
            for (float x = x0 + 10; x < x1; x += R(55f, 90f)) Pendulum(L2d, new Vector3(x, 26, 22));
            for (float x = x0 - 30; x < x1 + 30; x += R(45f, 80f)) Tesla(L3, new Vector3(x, R(-10f, 0f), R(50f, 80f)));
            // debris falling UPWARD (vertical spline lanes)
            for (int i = 0; i < 3; i++)
            {
                float lx = Mathf.Lerp(x0, x1, (i + 0.5f) / 3f);
                var go = new GameObject("UpwardDebris");
                go.transform.SetParent(L2d, false);
                var sc = go.AddComponent<SplineContainer>();
                var s = sc.Spline;
                s.Clear();
                s.Add(new BezierKnot(new float3(lx, -30, 24 + i * 6)));
                s.Add(new BezierKnot(new float3(lx + 20, 15, 26 + i * 6)));
                s.Add(new BezierKnot(new float3(lx - 10, 70, 30 + i * 6)));
                s.SetTangentMode(TangentMode.AutoSmooth);
                var tr = go.AddComponent<SplineTraffic>();
                tr.placeholderShape = "debris"; tr.placeholderColor = new Color(0.6f, 0.5f, 0.45f); tr.maxCount = 8; tr.speed = 0.035f; tr.faceAlong = false; tr.scale = R(0.6f, 1.4f);
            }
            // upward waterfalls
            for (float x = x0; x < x1 + 40; x += R(50f, 90f))
            {
                var f = MaterialCache.Instance("flow");
                f.SetColor("_BaseColor", new Color(0.5f, 0.8f, 1f, 0.8f));
                f.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.5f));
                f.SetVector("_Flow", new Vector4(0, 0.35f, 0, 0));
                f.SetFloat("_Alpha", 0.75f);
                if (kit != null && kit.surfaceTextures.Count > 2) f.SetTexture("_BaseMap", kit.surfaceTextures[2]);
                f = MaterialCache.OnCreated != null ? MaterialCache.OnCreated(f) : f;
                var q = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), L2, new Vector3(x, 6, R(26f, 34f)), new Vector3(3.5f, 40, 1), f, "UpwardWaterfall");
            }
        }

        void Island(Transform parent, Vector3 pos, float size, Material rock, Material grass, bool near)
        {
            var g = Group("Island", parent);
            g.localPosition = pos;
            PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), g, new Vector3(0, -size * 0.55f, 0), new Vector3(size, size * 1.4f, size * 0.8f), rock, "Rock");
            var top = PlaceholderFactory.Part(PrimitiveType.Cylinder, g, new Vector3(0, 0, 0), new Vector3(size * 0.95f, 0.35f, size * 0.75f), grass, "Turf");
            if (kit != null && kit.nature.Count > 0)
            {
                int n = near ? rnd.Next(2, 5) : rnd.Next(2, 4);
                for (int i = 0; i < n; i++)
                    SceneSpawn.Normalized(Pick(kit.nature), g, g.position + new Vector3(R(-size * 0.35f, size * 0.35f), 0.3f, R(-size * 0.25f, size * 0.25f)), R(near ? 1.5f : 4f, near ? 5f : 12f), R(0, 360));
            }
            var bob = g.gameObject.AddComponent<FloatingBob>();
            bob.bobHeight = R(0.4f, 1.2f); bob.bobSpeed = R(0.2f, 0.5f); bob.tilt = 2f;
            bob.corruptedLift = new Vector3(0, R(2f, 10f), 0);
            bob.corruptedTilt = new Vector3(R(-6, 6), 0, R(-14f, 14f));
            bob.spin = R(-6f, 6f);
        }

        void Turbine(Transform parent, Vector3 pos)
        {
            var g = Group("WindTurbine", parent);
            g.localPosition = pos;
            var white = MaterialCache.Glitch(new Color(0.9f, 0.92f, 0.95f), 0.6f);
            Box(g, new Vector3(0, 10, 0), new Vector3(0.7f, 20, 0.7f), white, "Mast", false);
            Box(g, new Vector3(0, 20.3f, -0.4f), new Vector3(1.2f, 1.2f, 2.6f), white, "Nacelle", false);
            var hub = new GameObject("Rotor").transform;
            hub.SetParent(g, false); hub.localPosition = new Vector3(0, 20.3f, -1.8f);
            for (int i = 0; i < 3; i++)
            {
                var b = new GameObject("Blade").transform; b.SetParent(hub, false); b.localRotation = Quaternion.Euler(0, 0, i * 120);
                Box(b, new Vector3(0, 4.5f, 0), new Vector3(0.6f, 9f, 0.15f), white, "BladeMesh", false);
            }
            var sp = hub.gameObject.AddComponent<Spinner>(); sp.speed = new Vector3(0, 0, R(40f, 90f) * (Chance(0.5f) ? 1 : -1));
        }

        void Pendulum(Transform parent, Vector3 pos)
        {
            var g = Group("GiantPendulum", parent);
            g.localPosition = pos;
            var metal = MaterialCache.Glitch(new Color(0.75f, 0.6f, 0.3f), 1f);
            Box(g, new Vector3(0, 0, 0), new Vector3(8, 0.8f, 0.8f), metal, "Beam", false);
            var arm = new GameObject("Arm").transform; arm.SetParent(g, false);
            Box(arm, new Vector3(0, -12, 0), new Vector3(0.25f, 24, 0.25f), metal, "Rod", false);
            PlaceholderFactory.Part(PrimitiveType.Sphere, arm, new Vector3(0, -24, 0), Vector3.one * 3.5f, MaterialCache.Emissive(new Color(0.5f, 0.4f, 0.2f), new Color(1.2f, 0.7f, 0.2f), 0, 1f), "Bob");
            var sw = arm.gameObject.AddComponent<PendulumSwing>(); sw.amplitude = R(18f, 32f); sw.period = R(4f, 7f);
        }

        void Tesla(Transform parent, Vector3 pos)
        {
            var g = Group("TeslaTower", parent);
            g.localPosition = pos;
            var dark = MaterialCache.Glitch(new Color(0.25f, 0.25f, 0.3f), 0.8f);
            float h = R(25f, 40f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90 * Mathf.Deg2Rad;
                Box(g, new Vector3(Mathf.Cos(a) * 1.5f, h / 2, Mathf.Sin(a) * 1.5f), new Vector3(0.3f, h, 0.3f), dark, "Leg", false);
            }
            for (float y = 3; y < h; y += 4) Box(g, new Vector3(0, y, 0), new Vector3(3.4f, 0.2f, 3.4f), dark, "Brace", false);
            var orb = PlaceholderFactory.Part(PrimitiveType.Sphere, g, new Vector3(0, h + 1.5f, 0), Vector3.one * 3f, MaterialCache.Emissive(new Color(0.2f, 0.3f, 0.5f), new Color(0.6f, 1.2f, 2.5f), 0.5f, 1.2f), "Orb");
            orb.gameObject.AddComponent<GlitchProp>().wanderRadius = 0.3f;
        }

        // ================================================================== CHEMICAL CHAOS
        void Chem()
        {
            var tiles = MaterialCache.Glitch(new Color(0.75f, 0.85f, 0.88f), 0.4f, kit != null && kit.surfaceTextures.Count > 0 ? kit.surfaceTextures[0] : null);
            Box(L2, new Vector3((x0 + x1) / 2, -4.2f, 26), new Vector3(x1 - x0 + 220, 0.4f, 46), tiles, "LabFloor");
            Color[] liquids = { new Color(0.4f, 1.6f, 0.4f), new Color(0.3f, 0.9f, 1.8f), new Color(1.8f, 0.4f, 1.2f), new Color(1.6f, 1.2f, 0.3f) };
            for (float x = x0 - 70; x < x1 + 70; x += R(9f, 15f))
            {
                float z = R(14f, 32f);
                if (Chance(0.55f)) GlassTower(L2, new Vector3(x, -4, z), R(2.5f, 5f), R(14f, 34f), liquids[rnd.Next(liquids.Length)]);
                else Flask(L2, new Vector3(x, -4, z), R(4f, 9f), liquids[rnd.Next(liquids.Length)]);
            }
            // pipes connecting the towers
            var pipeMat = MaterialCache.Glitch(new Color(0.6f, 0.65f, 0.7f), 0.7f);
            for (float x = x0 - 40; x < x1 + 40; x += R(25f, 45f))
            {
                var p = PlaceholderFactory.Part(PrimitiveType.Cylinder, L2, new Vector3(x, R(6f, 18f), R(16f, 30f)), new Vector3(0.9f, R(8f, 16f), 0.9f), pipeMat, "Pipe", new Vector3(0, 0, 90));
                Static(p.gameObject);
            }
            // lab props (Sci-Fi kit)
            if (kit != null)
                for (float x = x0 - 40; x < x1 + 40; x += R(5f, 10f))
                {
                    var pr = Pick(kit.props);
                    if (pr != null) Static(SceneSpawn.Normalized(pr, L2d, new Vector3(x, -4, R(7f, 11f)), R(0.9f, 2.2f), R(0, 360)));
                }
            var crowd = Group("ElementCreatures", L2d);
            crowd.localPosition = new Vector3(x0 - 10, -4, 10);
            var cl = crowd.gameObject.AddComponent<CrowdLoop>(); cl.length = x1 - x0 + 20; cl.style = "blobs"; cl.maxCount = 14;
            // the periodic table wall
            var wall = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), L2, new Vector3((x0 + x1) / 2, 16, 36), new Vector3(70, 35, 1), MaterialCache.Get("holo"), "PeriodicWall");
            var hb = wall.gameObject.AddComponent<HologramBillboard>(); hb.content = LevelBuilder.Holo("periodic"); hb.color = new Color(0.4f, 1.6f, 1.2f);
            // L3: lab towers + molecules in the sky
            var labTex = kit != null ? Tex(kit.facadeTextures) : null;
            for (float x = x0 - 110; x < x1 + 110; x += R(14f, 26f))
            {
                float h = R(30f, 80f), w = R(10f, 18f);
                var t = Box(L3, new Vector3(x, h / 2 - 8, R(50f, 120f)), new Vector3(w, h, w), MaterialCache.Facade(labTex, new Color(0.6f, 0.75f, 0.75f), new Color(0.3f, 1.2f, 0.6f) * 0.6f, 1f, 0.06f), "LabTower", false);
            }
            for (float x = x0; x < x1 + 30; x += R(30f, 50f)) Molecule(L3, new Vector3(x, R(20f, 40f), R(50f, 80f)));
            TrafficLane(L2d, 20, 2, "bubble", 10, 0.02f, null, 1f, true);
        }

        void GlassTower(Transform parent, Vector3 pos, float r, float h, Color liquid)
        {
            var g = Group("GlassTower", parent);
            g.localPosition = pos;
            var glass = MaterialCache.Emissive(new Color(0.55f, 0.75f, 0.85f), new Color(0.05f, 0.12f, 0.15f), 0, 1f);
            PlaceholderFactory.Part(PrimitiveType.Cylinder, g, new Vector3(0, h / 2, 0), new Vector3(r * 2, h / 2, r * 2), glass, "Glass");
            float fill = R(0.3f, 0.8f);
            PlaceholderFactory.Part(PrimitiveType.Cylinder, g, new Vector3(0, h * fill / 2, -r * 0.05f), new Vector3(r * 1.9f, h * fill / 2, r * 1.9f), MaterialCache.Emissive(liquid * 0.3f, liquid, 0, 0.8f), "Liquid");
            PlaceholderFactory.Part(PrimitiveType.Cylinder, g, new Vector3(0, h + 0.3f, 0), new Vector3(r * 2.2f, 0.3f, r * 2.2f), MaterialCache.Glitch(new Color(0.3f, 0.32f, 0.36f), 1f), "Cap");
            if (Chance(0.4f)) { var bob = g.gameObject.AddComponent<FloatingBob>(); bob.corruptedLift = new Vector3(0, R(2, 6), 0); bob.corruptedTilt = new Vector3(0, 0, R(-8, 8)); }
            else Static(g.gameObject);
        }

        void Flask(Transform parent, Vector3 pos, float size, Color liquid)
        {
            var g = Group("Flask", parent);
            g.localPosition = pos;
            var glass = MaterialCache.Emissive(new Color(0.6f, 0.8f, 0.9f), new Color(0.05f, 0.1f, 0.12f), 0, 1f);
            PlaceholderFactory.Part(PrimitiveType.Sphere, g, new Vector3(0, size * 0.5f, 0), Vector3.one * size, glass, "Bulb");
            PlaceholderFactory.Part(PrimitiveType.Sphere, g, new Vector3(0, size * 0.38f, -size * 0.02f), new Vector3(size * 0.9f, size * 0.62f, size * 0.9f), MaterialCache.Emissive(liquid * 0.3f, liquid, 0, 0.8f), "Liquid");
            PlaceholderFactory.Part(PrimitiveType.Cylinder, g, new Vector3(0, size * 1.25f, 0), new Vector3(size * 0.3f, size * 0.35f, size * 0.3f), glass, "Neck");
            Static(g.gameObject);
        }

        void Molecule(Transform parent, Vector3 pos)
        {
            var g = Group("Molecule", parent);
            g.localPosition = pos;
            string[] els = { "O", "H", "H" };
            if (Chance(0.4f)) els = new[] { "C", "O", "O" };
            if (Chance(0.3f)) els = new[] { "C", "H", "H", "H", "H" };
            for (int i = 0; i < els.Length; i++)
            {
                Vector3 p = i == 0 ? Vector3.zero : Quaternion.Euler(0, 0, i * 360f / (els.Length - 1)) * Vector3.up * 4f;
                var a = PlaceholderFactory.Part(PrimitiveType.Sphere, g, p, Vector3.one * (i == 0 ? 3.2f : 2.2f), MaterialCache.Emissive(ConstructionChallenge.AtomColor(els[i]) * 0.6f, ConstructionChallenge.AtomColor(els[i]) * 0.5f, 0, 0.6f), "Atom_" + els[i]);
            }
            g.gameObject.AddComponent<MoleculeAssembler>();
        }

        // ================================================================== BIO-ZONE
        void Bio()
        {
            var tissueTex = kit != null && kit.surfaceTextures.Count > 0 ? kit.surfaceTextures[0] : null;
            var tissue = MaterialCache.Glitch(new Color(1f, 0.55f, 0.6f), 0.5f, tissueTex);
            var deep = MaterialCache.Glitch(new Color(0.75f, 0.2f, 0.3f), 0.5f, tissueTex);
            // vessel walls (floor and ceiling of organic blobs) near layer
            for (float x = x0 - 60; x < x1 + 60; x += R(7f, 13f))
            {
                var b = PlaceholderFactory.Part(PrimitiveType.Sphere, L2, new Vector3(x, R(-14f, -8f), R(12f, 34f)), new Vector3(R(10, 20), R(8, 14), R(8, 14)), tissue, "TissueFloor");
                b.gameObject.AddComponent<HeartPulse>().amount = 0.03f;
                if (Chance(0.6f))
                {
                    var c = PlaceholderFactory.Part(PrimitiveType.Sphere, L2, new Vector3(x + R(-3, 3), R(26f, 34f), R(16f, 34f)), new Vector3(R(12, 22), R(10, 16), R(8, 14)), deep, "TissueCeiling");
                    c.gameObject.AddComponent<HeartPulse>().amount = 0.04f;
                }
            }
            // villi (nature kit plants, tinted red) and mitochondria
            if (kit != null && kit.nature.Count > 0)
                for (float x = x0 - 30; x < x1 + 30; x += R(4f, 8f))
                {
                    var v = SceneSpawn.Normalized(Pick(kit.nature), L2d, new Vector3(x, -4, R(7f, 12f)), R(2f, 5f), R(0, 360));
                    if (v != null) { TintRed(v); Static(v); }
                }
            for (float x = x0 - 20; x < x1 + 20; x += R(20f, 35f))
            {
                var m = PlaceholderFactory.Part(PrimitiveType.Capsule, L2d, new Vector3(x, R(6f, 18f), R(18f, 30f)), new Vector3(2.5f, 4f, 2.5f), MaterialCache.Emissive(new Color(0.9f, 0.5f, 0.3f), new Color(1.5f, 0.6f, 0.2f), 0.6f, 0.6f), "Mitochondrion", new Vector3(0, 0, R(60, 120)));
                m.gameObject.AddComponent<FloatingBob>().bobHeight = 1f;
            }
            // L3 cave walls
            for (float x = x0 - 120; x < x1 + 120; x += R(18f, 30f))
            {
                var w = PlaceholderFactory.Part(PrimitiveType.Sphere, L3, new Vector3(x, R(-20f, 40f), R(70f, 130f)), Vector3.one * R(30f, 60f), deep, "CaveWall");
                w.gameObject.AddComponent<HeartPulse>().amount = 0.025f;
            }
            // flowing red blood cells
            TrafficLane(L2d, 12, 3, "cell", 16, 0.016f, null, 1f);
            TrafficLane(L2, 28, 9, "cell", 18, 0.012f, null, 1.4f);
            // virus swarm (only while corrupted)
            var swarm = Group("VirusSwarm", L2d);
            swarm.gameObject.AddComponent<CorruptionVisibility>();
            for (float x = x0; x < x1; x += R(15f, 28f))
            {
                var ring = Group("Swarm", swarm);
                ring.localPosition = new Vector3(x, R(4f, 14f), R(14f, 22f));
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 2;
                    PlaceholderFactory.BuildEnemy(ring, "virus", new Color(0.7f, 0.2f, 1f), R(0.6f, 1.2f), out _).localPosition = new Vector3(Mathf.Cos(a) * 3f, Mathf.Sin(a) * 2f, 0);
                }
                ring.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 0, R(20f, 50f));
            }
        }

        void TintRed(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(1f, 0.35f, 0.45f));
                r.SetPropertyBlock(mpb);
            }
        }

        // ================================================================== THE CORE
        void Core()
        {
            var gridMat = MaterialCache.Get("grid");
            PlaceholderFactory.Part(PrimitiveType.Cube, L2, new Vector3((x0 + x1) / 2, -6, 120), new Vector3(x1 - x0 + 600, 0.2f, 260), gridMat, "GridFloor");
            // floating chunks of all four regions
            var alt = new List<GameObject>();
            for (float x = x0 - 40; x < x1 + 40; x += R(16f, 28f))
            {
                var chunk = Group("RegionChunk", L2);
                chunk.localPosition = new Vector3(x, R(-2f, 16f), R(16f, 40f));
                int kind = rnd.Next(4);
                var rock = MaterialCache.Glitch(new Color(0.2f, 0.2f, 0.25f), 1f);
                PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), chunk, new Vector3(0, -3, 0), new Vector3(7, 7, 6), rock, "Base");
                switch (kind)
                {
                    case 0: Tower(chunk, new Vector3(0, -0.5f, 0), 3, R(6f, 12f), 3, true); break;
                    case 1: Island(chunk, new Vector3(0, 0, 0), 5, rock, MaterialCache.Glitch(new Color(0.45f, 0.75f, 0.35f), 0.6f), true); break;
                    case 2: Flask(chunk, new Vector3(0, -0.5f, 0), 4, new Color(0.4f, 1.6f, 0.4f)); break;
                    default:
                        PlaceholderFactory.Part(PrimitiveType.Sphere, chunk, new Vector3(0, 1.5f, 0), new Vector3(5, 4, 4), MaterialCache.Glitch(new Color(1f, 0.5f, 0.55f), 0.6f), "Tissue").gameObject.AddComponent<HeartPulse>();
                        break;
                }
                var bob = chunk.gameObject.AddComponent<FloatingBob>();
                bob.bobHeight = 1f; bob.spin = 8f; bob.corruptedLift = new Vector3(0, R(0, 4), 0);
            }
            // space kit debris
            if (kit != null)
                for (float x = x0 - 60; x < x1 + 60; x += R(10f, 20f))
                {
                    var p = Pick(kit.props);
                    var d = SceneSpawn.Normalized(p, L3, new Vector3(x, R(0f, 40f), R(50f, 120f)), R(3f, 9f), R(0, 360));
                    if (d != null) d.AddComponent<Spinner>().speed = new Vector3(R(-10, 10), R(-15, 15), R(-10, 10));
                }
            // alternate-region props for the constant region-swap glitch
            for (int i = 0; i < 3; i++)
            {
                var g = Group("AltRegion" + i, L2);
                for (float x = x0 - 20; x < x1 + 20; x += 30)
                {
                    if (i == 0) Tower(g, new Vector3(x, -6, 26), 7, R(18f, 30f), 7, true);
                    else if (i == 1) Box(g, new Vector3(x, 2, 30), new Vector3(10, 16, 10), MaterialCache.Glitch(new Color(0.45f, 0.75f, 0.35f), 1f), "AltIsland", false);
                    else GlassTower(g, new Vector3(x, -6, 26), 3, R(16f, 26f), new Color(0.4f, 1.6f, 0.4f));
                }
                alt.Add(g.gameObject);
            }
            var flashGo = new GameObject("RegionFlash");
            flashGo.transform.SetParent(life, false);
            var rf = flashGo.AddComponent<RegionFlash>();
            rf.altProps = alt.ToArray();
            rf.constant = true;
        }

        // ================================================================== shared
        void Skyline()
        {
            var card = ProceduralMeshes.CardBottom();
            Color body, top;
            switch (plan.arcId)
            {
                case "PhysicsValley": body = new Color(0.32f, 0.22f, 0.4f); top = new Color(0.6f, 0.4f, 0.55f); break;
                case "ChemicalChaos": body = new Color(0.1f, 0.25f, 0.25f); top = new Color(0.2f, 0.45f, 0.4f); break;
                case "BioZone": body = new Color(0.35f, 0.06f, 0.1f); top = new Color(0.6f, 0.15f, 0.2f); break;
                case "TheCore": body = new Color(0.05f, 0.05f, 0.08f); top = new Color(0.15f, 0.12f, 0.2f); break;
                default: body = new Color(0.08f, 0.09f, 0.18f); top = new Color(0.15f, 0.16f, 0.32f); break;
            }
            bool mountains = plan.arcId == "PhysicsValley";
            var mats = new List<Material>();
            for (int i = 0; i < 4; i++)
            {
                var m = MaterialCache.Instance("impostor");
                m.name = $"Impostor_{plan.arcId}_{i}";
                m.SetColor("_BaseColor", Color.Lerp(body, Color.black, i * 0.12f));
                m.SetColor("_TopColor", top);
                m.SetColor("_WindowColor", plan.arcId == "BioZone" ? new Color(1.2f, 0.3f, 0.4f) : plan.arcId == "ChemicalChaos" ? new Color(0.4f, 1.4f, 0.6f) : new Color(1.2f, 1f, 0.6f));
                m.SetFloat("_WindowDensity", mountains ? 0f : 0.45f);
                m.SetFloat("_Taper", mountains ? 0.48f : (i % 2 == 0 ? 0.0f : 0.12f));
                m.SetFloat("_Seed", i * 1.7f);
                m.enableInstancing = true;
                mats.Add(MaterialCache.OnCreated != null ? MaterialCache.OnCreated(m) : m);
            }
            for (float x = x0 - 260; x < x1 + 260; x += R(8f, 18f))
            {
                float w = mountains ? R(40f, 90f) : R(10f, 30f), h = mountains ? R(40f, 110f) : R(40f, 140f);
                var go = new GameObject("SkylineCard");
                go.transform.SetParent(L4, false);
                go.transform.localPosition = new Vector3(x, -12, R(170f, 380f));
                go.transform.localScale = new Vector3(w, h, 1);
                go.AddComponent<MeshFilter>().sharedMesh = card;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mats[rnd.Next(mats.Count)];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        void Foreground()
        {
            var fg = MaterialCache.Get("fg");
            var mat = new Material(fg) { name = "Foreground_" + plan.arcId };
            mat.SetColor("_BaseColor", plan.arcId == "PhysicsValley" ? new Color(0.15f, 0.2f, 0.12f) : plan.arcId == "BioZone" ? new Color(0.35f, 0.08f, 0.12f) : new Color(0.07f, 0.08f, 0.12f));
            mat.SetColor("_EmissionColor", theme.accentRestored * 0.25f);
            mat = MaterialCache.OnCreated != null ? MaterialCache.OnCreated(mat) : mat;
            for (float x = x0 - 10; x < x1 + 10; x += R(16f, 30f))
            {
                float z = R(-7f, -3.5f);
                float gy = plan.GroundY(x);
                if (gy < -10) gy = 0;
                int kind = rnd.Next(3);
                if (plan.arcId == "PhysicsValley" || plan.arcId == "BioZone") kind = 3;
                switch (kind)
                {
                    case 0: // pipe
                        var p = PlaceholderFactory.Part(PrimitiveType.Cylinder, L0, new Vector3(x, gy + R(5f, 9f), z), new Vector3(0.6f, R(4f, 8f), 0.6f), mat, "Pipe", new Vector3(0, 0, 90));
                        break;
                    case 1: // cable bundle
                        for (int i = 0; i < 3; i++)
                            PlaceholderFactory.Part(PrimitiveType.Cylinder, L0, new Vector3(x, gy + 8 + i * 0.35f, z), new Vector3(0.12f, 9f, 0.12f), mat, "Cable", new Vector3(0, 0, 90 + R(-6, 6)));
                        break;
                    case 2: // sign post with a little hologram
                        PlaceholderFactory.Part(PrimitiveType.Cylinder, L0, new Vector3(x, gy + 2, z), new Vector3(0.18f, 2f, 0.18f), mat, "Post");
                        PlaceholderFactory.Part(PrimitiveType.Cube, L0, new Vector3(x, gy + 4.2f, z), new Vector3(1.8f, 0.9f, 0.1f), mat, "Sign");
                        break;
                    default: // leaves / organic tendrils
                        for (int i = 0; i < 3; i++)
                            PlaceholderFactory.Part(PrimitiveType.Sphere, L0, new Vector3(x + R(-1.5f, 1.5f), gy + R(6f, 10f), z), new Vector3(R(1.5f, 3f), R(0.6f, 1.2f), 0.6f), mat, "Leaf");
                        break;
                }
            }
        }

        void TrafficLane(Transform parent, float z, float y, string shape, int count, float speed, List<GameObject> prefabs, float scale, bool vertical = false)
        {
            var go = new GameObject("TrafficLane_" + shape);
            go.transform.SetParent(parent, false);
            var sc = go.AddComponent<SplineContainer>();
            var s = sc.Spline;
            s.Clear();
            if (vertical)
            {
                for (int i = 0; i < 4; i++) s.Add(new BezierKnot(new float3(Mathf.Lerp(x0, x1, i / 3f), -6 + i * 3, z)));
                s.Add(new BezierKnot(new float3(x1, 40, z + 6)));
            }
            else
            {
                int knots = 6;
                for (int i = 0; i < knots; i++)
                {
                    float k = i / (float)(knots - 1);
                    s.Add(new BezierKnot(new float3(Mathf.Lerp(x0 - 120, x1 + 120, k), y + R(-3f, 3f), z + R(-4f, 4f))));
                }
            }
            s.SetTangentMode(TangentMode.AutoSmooth);
            var tr = go.AddComponent<SplineTraffic>();
            tr.placeholderShape = shape;
            tr.maxCount = count;
            tr.speed = speed;
            tr.scale = scale;
            tr.faceAlong = shape == "car" || shape == "drone";
            tr.placeholderColor = shape == "cell" ? new Color(0.9f, 0.1f, 0.15f) : shape == "bubble" ? new Color(0.6f, 1f, 0.7f) : theme.accentRestored;
            if (prefabs != null && prefabs.Count > 0) tr.vehiclePrefabs = new List<GameObject>(prefabs);
        }

        void Life()
        {
            var amb = new GameObject("AmbientParticles");
            amb.transform.SetParent(life, false);
            amb.AddComponent<AmbientParticles>().style = theme.ambientParticles;
            var sky = new GameObject("SkyEvents");
            sky.transform.SetParent(L5, false);
            var se = sky.AddComponent<SkyEvents>();
            se.lightning = plan.arcId == "PhysicsValley" || plan.arcId == "TheCore";
            se.whales = plan.arcId != "BioZone";
            se.blimps = plan.arcId == "CodeCity" || plan.arcId == "ChemicalChaos";
            se.blimpAd = LevelBuilder.Holo(plan.arcId == "ChemicalChaos" ? "ad_water" : "ad_ram");
            if (plan.arcId != "TheCore")
            {
                var flashGo = new GameObject("RegionFlash");
                flashGo.transform.SetParent(life, false);
                flashGo.AddComponent<RegionFlash>().altProps = new GameObject[0];
            }
        }

        void ArenaCentrepiece()
        {
            var g = Group("ArenaCentrepiece", L2);
            float cx = plan.boss != null ? plan.boss.x : (x0 + x1) / 2;
            g.localPosition = new Vector3(cx, 8, 22);
            var m = MaterialCache.Emissive(new Color(0.1f, 0.05f, 0.12f), theme.accentCorrupted * 2.2f, 0, 1.2f);
            switch (plan.arcId)
            {
                case "CodeCity": // the infinite loop: nested spinning rings
                    for (int ringI = 0; ringI < 3; ringI++)
                    {
                        var ring = Group("LoopRing" + ringI, g);
                        float r = 9 + ringI * 4;
                        for (int i = 0; i < 28; i++) { float a = i / 28f * Mathf.PI * 2; Box(ring, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), new Vector3(1.2f, 1.2f, 1.2f), m, "Seg", false); }
                        ring.gameObject.AddComponent<Spinner>().speed = new Vector3(ringI * 10, 0, (ringI % 2 == 0 ? 1 : -1) * (20 + ringI * 10));
                    }
                    break;
                case "PhysicsValley": // orbiting rocks
                    var orbit = Group("OrbitingRocks", g);
                    for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2; PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), orbit, new Vector3(Mathf.Cos(a) * 14, Mathf.Sin(a) * 6, Mathf.Sin(a) * 6), Vector3.one * R(1.5f, 3f), MaterialCache.Glitch(new Color(0.6f, 0.5f, 0.45f), 1f), "Rock"); }
                    orbit.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 25, 8);
                    break;
                case "ChemicalChaos": // the grand reactor core
                    PlaceholderFactory.Part(PrimitiveType.Sphere, g, Vector3.zero, Vector3.one * 14, MaterialCache.Emissive(new Color(0.2f, 0.5f, 0.2f), new Color(0.6f, 2.2f, 0.5f), 0.4f, 1.2f), "ReactorCore").gameObject.AddComponent<HeartPulse>().amount = 0.06f;
                    break;
                case "BioZone": // a giant heart
                    var heart = Group("Heart", g);
                    var red = MaterialCache.Emissive(new Color(0.8f, 0.15f, 0.2f), new Color(0.6f, 0.05f, 0.1f), 0, 0.8f);
                    PlaceholderFactory.Part(PrimitiveType.Sphere, heart, new Vector3(-4, 2, 0), Vector3.one * 11, red, "LobeL");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, heart, new Vector3(4, 2, 0), Vector3.one * 11, red, "LobeR");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, heart, new Vector3(0, -4, 0), Vector3.one * 10, red, "Apex");
                    heart.gameObject.AddComponent<HeartPulse>().amount = 0.08f;
                    break;
                default: // The Source: a monolith of light with orbiting chunks
                    PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), g, Vector3.zero, new Vector3(8, 22, 8), MaterialCache.Emissive(new Color(0.9f, 0.9f, 1f), new Color(1.5f, 1.5f, 2f), 0.6f, 1.6f), "Monolith");
                    var orbitC = Group("Orbit", g);
                    for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2; Box(orbitC, new Vector3(Mathf.Cos(a) * 16, Mathf.Sin(a) * 8, Mathf.Sin(a) * 4), Vector3.one * 2.5f, m, "Chunk", false); }
                    orbitC.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 30, 0);
                    break;
            }
        }
    }
}
