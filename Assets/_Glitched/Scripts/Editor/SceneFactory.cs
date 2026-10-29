using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Glitched.EditorTools
{
    /// <summary>Bakes every scene from code (level plans + region backdrops) and writes the Build Settings list.</summary>
    public static class SceneFactory
    {
        const string Scenes = GlitchedBuilder.Root + "/Scenes";
        static GameDatabase db;
        static LightingSettings lighting;

        public static void BuildAll(GameDatabase database)
        {
            db = database;
            GlitchedBuilder.LoadHolograms();
            if (MaterialCache.OnCreated == null) { MaterialCache.ClearCache(); }
            SceneSpawn.InstantiatePrefab = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            SceneSpawn.MarkStatic = MarkStaticRecursive;
            SceneSpawn.ClearStatic = go => { foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); };
            lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(GlitchedBuilder.Root + "/Settings/Lighting.lighting");
            if (lighting == null)
            {
                lighting = new LightingSettings { name = "Lighting", bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(lighting, GlitchedBuilder.Root + "/Settings/Lighting.lighting");
            }

            var list = new List<EditorBuildSettingsScene>();
            void Add(string path) => list.Add(new EditorBuildSettingsScene(path, true));

            Add(Make("Boot", Scenes, () => new GameObject("Bootstrap").AddComponent<GameBootstrap>()));
            Add(Make("Title", Scenes, BuildTitle));
            Add(Make("Benchmark", Scenes, BuildBenchmark));
            Add(Make("Opening", Scenes, () => { new GameObject("OpeningCutscene").AddComponent<OpeningCutscene>(); }));
            Add(Make("WorldMap", Scenes, BuildWorldMap));
            foreach (var plan in LevelPlans.All)
            {
                var p = plan;
                Add(Make(p.scene, Scenes + "/Arcs", () => new LevelBuilder(p, db).Build()));
            }
            Add(Make("Kanha_Final", Scenes, () => { ExamHall.Build(); new GameObject("FinalExam").AddComponent<FinalExam>(); }));   // Kanha the Dard (hall light-baked below)
            Add(Make("Ending", Scenes, () => { new GameObject("EndingCutscene").AddComponent<EndingCutscene>(); }));
            Add(Make("Test_Challenges", Scenes + "/Test", BuildTestChallenges));
            Add(Make("Test_Corruption", Scenes + "/Test", () => new LevelBuilder(TestCorruptionPlan(), db).Build()));
            Add(Make("Test_Greybox", Scenes + "/Test", () => new LevelBuilder(GreyboxPlan(), db).Build()));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            try { ExamHallBake.Bake(); } catch (System.Exception e) { Debug.LogError("[GLITCHED] Exam hall bake failed: " + e); }
            Debug.Log($"[GLITCHED] Built {list.Count} scenes.");
        }

        static void MarkStaticRecursive(GameObject go)
        {
            if (go == null) return;
            if (go.GetComponent<MonoBehaviour>() != null) return;   // anything with behaviour may move
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            foreach (Transform c in go.transform) MarkStaticRecursive(c.gameObject);
        }

        static string Make(string name, string folder, System.Action build)
        {
            GlitchedBuilder.EnsureFolder(folder);
            string path = $"{folder}/{name}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lightmapping.lightingSettings = lighting;
            RenderSettings.skybox = db.materials != null ? db.materials.sky : null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.3f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.15f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.15f);
            try { build(); }
            catch (System.Exception e) { Debug.LogError($"[GLITCHED] Scene {name} failed: {e}"); }
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[GLITCHED] Scene " + path);
            return path;
        }

        static Camera MakeCamera(Vector3 pos, Vector3 euler, float fov = 40f, Transform parent = null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.3f; cam.farClipPlane = 900;
            cam.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            go.AddComponent<AudioListener>();
            return cam;
        }

        static GameObject BackdropOnly(LevelPlan plan)
        {
            var root = new LevelBuilder(plan, db).Build();
            var rd = root.GetComponent<RegionDirector>();
            if (rd != null) Object.DestroyImmediate(rd);
            var ents = root.transform.Find("Gameplay (L1)/Entities");
            if (ents != null) Object.DestroyImmediate(ents.gameObject);
            return root;
        }

        static void BuildTitle()
        {
            var plan = new LevelPlan("CodeCity", "Title", LevelKind.Cinematic, -60, 60);
            plan.G(-60, 60, 0f);
            var root = BackdropOnly(plan);
            var pivot = new GameObject("CameraPivot").transform;
            pivot.position = new Vector3(0, 0, 24);
            var cam = MakeCamera(new Vector3(0, 7, -20), new Vector3(-6, 0, 0), 44, pivot);
            var t = new GameObject("TitleScreen").AddComponent<TitleScreen>();
            t.cameraPivot = pivot;
            t.sun = root.GetComponentInChildren<Light>();
        }

        static void BuildBenchmark()
        {
            var plan = LevelPlans.Get("CodeCity_L1");
            var root = BackdropOnly(plan);
            MakeCamera(new Vector3(0, 3.5f, -18), new Vector3(2, 0, 0), 40);
            new GameObject("Benchmark").AddComponent<BenchmarkRunner>();
        }

        static void BuildTestChallenges()
        {
            var plan = new LevelPlan("CodeCity", "Test_Challenges_Backdrop", LevelKind.Cinematic, -40, 40);
            plan.G(-40, 40, 0f);
            BackdropOnly(plan);
            MakeCamera(new Vector3(0, 6, -18), new Vector3(-4, 0, 0), 44);
            new GameObject("TestChallenges").AddComponent<TestChallengesScreen>();
        }

        static LevelPlan TestCorruptionPlan()
        {
            var p = new LevelPlan("CodeCity", "Test_Corruption", LevelKind.Level, -20, 220);
            p.Spawn("default", 0).G(-20, 220).P(20, 3, 4).P(28, 5.5f, 4).P(60, 3, 6, 6, 0, 4).Script("TestCorruptionPanel");
            p.startHealthy = false;
            p.Npc("Test NPC", 12, new[] { "T3ST. T3ST. T3ST." }, new[] { "Test passed." });
            return p;
        }

        static LevelPlan GreyboxPlan()
        {
            var p = new LevelPlan("CodeCity", "Test_Greybox", LevelKind.Level, -20, 260);
            p.Spawn("default", 0).G(-20, 40).G(44, 80, 1.5f).P(86, 3.5f, 4).P(94, 6, 4, 0, 2, 3).G(100, 140, 3).G(146, 200, 0).W(170, 0, 7).G(174, 260, 8);
            p.Zone(100, 140, 26, 44, 0, 3, "Camera zone: pulling back to reveal the city.");
            p.Grav(150, 165, 0, 12).Ceil(148, 168, 12);
            return p;
        }

        // ------------------------------------------------------------------ world map diorama
        static void BuildWorldMap()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            var map = new GameObject("WorldMap").AddComponent<WorldMapScreen>();
            map.sun = sun;
            var cam = MakeCamera(new Vector3(-30, 14, -30), new Vector3(15, 30, 0), 42);
            map.cameraTransform = cam.transform;
            var defs = new (string arc, Vector3 pos)[]
            {
                ("CodeCity", new Vector3(-36, 0, 0)), ("PhysicsValley", new Vector3(-14, 4, 26)), ("ChemicalChaos", new Vector3(14, 0, 34)),
                ("BioZone", new Vector3(36, 3, 8)), ("TheCore", new Vector3(0, 10, -2)),
            };
            System.Func<float, float, float> landH = (x, z) =>
            {
                float dd = Mathf.Sqrt(x * x * 0.6f + (z - 16f) * (z - 16f)) / 70f;
                float hh = (1f - Mathf.SmoothStep(0.75f, 1.1f, dd)) * (3f + NatureGen.Fbm(x * 0.03f, z * 0.03f) * 6f) - 2.5f * Mathf.SmoothStep(0.9f, 1.2f, dd);
                hh += Mathf.Exp(-((x + 14f) * (x + 14f) + (z - 26f) * (z - 26f)) / 300f) * 4f;
                return hh;
            };
            foreach (var d in defs)
            {
                var root = new GameObject("Island_" + d.arc).transform;
                root.position = new Vector3(d.pos.x, landH(d.pos.x, d.pos.z), d.pos.z);
                IslandFor(d.arc, root);
                map.islands.Add(new WorldMapScreen.Island { arcId = d.arc, root = root });
            }
            // no void: one landmass (gentle terrain) ringed by the sea to the horizon
            CityMaterials.Library = db.materials;
            var land = new GameObject("Land").transform;
            var nat = new NatureGen(land, new System.Random(7));
            System.Func<float, float, float> height = (x, z) =>
            {
                float d = Mathf.Sqrt(x * x * 0.6f + (z - 16f) * (z - 16f)) / 70f;
                float h = (1f - Mathf.SmoothStep(0.75f, 1.1f, d)) * (3f + NatureGen.Fbm(x * 0.03f, z * 0.03f) * 6f) - 2.5f * Mathf.SmoothStep(0.9f, 1.2f, d);
                // the valley rises behind, the Core plateau sits in the middle
                h += Mathf.Exp(-((x + 14f) * (x + 14f) + (z - 26f) * (z - 26f)) / 300f) * 4f;
                return h;
            };
            nat.Terrain("Land", -140f, 140f, -90f, 130f, 2f, height, CityMaterials.Terrain("Grass005", "Rock064", new Color(0.95f, 1f, 0.85f), new Color(0.8f, 0.78f, 0.75f)));
            var sea = new MeshBuilder(1);
            sea.Box(new Vector3(-3000f, -1.2f, -3000f), new Vector3(6000f, 0.01f, 6000f), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            SceneSpawn.MarkStatic(sea.ToObject("Sea", land, new[] { CityMaterials.Water(new Color(0.03f, 0.08f, 0.12f), new Color(0.06f, 0.14f, 0.18f), new Vector2(0.3f, 0.1f)) }, false));
        }

        /// <summary>A recognisable miniature of each region on the world-map landmass (real objects, aerial view).</summary>
        static void RealIsland(string arc, Transform root)
        {
            var rnd = new System.Random(arc.GetHashCode());
            float R(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
            var nat = new NatureGen(root, rnd);
            switch (arc)
            {
                case "CodeCity":
                    for (int i = 0; i < 12; i++)
                    {
                        var spec = BuildingGen.Random(rnd, R(4f, 7f), R(4f, 7f), 6f, 18f, true, 1f);
                        spec.podiumStyle = null; spec.foundation = 3f;
                        BuildingGen.Build(root, new Vector3(R(-7f, 7f), -0.3f, R(-5f, 5f)), spec, rnd, "MiniTower");
                    }
                    break;
                case "PhysicsValley":
                    for (int i = 0; i < 18; i++) nat.Conifer(new Vector3(R(-8f, 8f), -0.2f, R(-6f, 6f)), R(2f, 3.5f));
                    nat.Observatory(new Vector3(0, 0, 0), 1.8f);
                    for (int i = 0; i < 3; i++) nat.WindTurbine(new Vector3(-6f + i * 5f, 0, 5f), 9f, false);
                    break;
                case "ChemicalChaos":
                    {
                        var mb = new MeshBuilder(2);
                        for (int i = 0; i < 5; i++) { float r = R(1f, 1.8f); var c = new Vector3(R(-6f, 6f), 0, R(-4f, 4f)); mb.Cylinder(c, r, r, R(2f, 3.5f), 20, 0); }
                        for (int i = 0; i < 6; i++) { float t0 = i / 6f, t1 = (i + 1) / 6f; float rr(float t) => 3.2f * (0.62f + 0.38f * Mathf.Pow(Mathf.Abs(t - 0.72f) / 0.72f, 1.6f)); mb.Cylinder(new Vector3(4f, 9f * t0, 4f), rr(t0), rr(t1), 1.5f, 28, 1, false); }
                        SceneSpawn.MarkStatic(mb.ToObject("MiniPlant", root, new[] { CityMaterials.Solid(new Color(0.86f, 0.87f, 0.86f), 0.4f, 0.15f), CityMaterials.PBR("Concrete034", new Color(0.82f, 0.81f, 0.78f), 3f) }));
                        break;
                    }
                case "BioZone":
                    {
                        var spec = new BuildingGen.Spec { style = CityMaterials.FacadeStyle.GlassTower, variant = 2, width = 14f, depth = 9f, height = 6f, glitch = 1f, foundation = 3f };
                        BuildingGen.Build(root, new Vector3(0, -0.3f, 0), spec, rnd, "ResearchCentre");
                        for (int i = 0; i < 8; i++) nat.Broadleaf(new Vector3(R(-8f, 8f), -0.2f, R(-6f, -4f)), R(2f, 3f));
                        break;
                    }
                default:
                    {
                        var spec = new BuildingGen.Spec { style = CityMaterials.FacadeStyle.GlassTower, variant = 1, width = 5f, depth = 5f, height = 13f, setbacks = true, glitch = 1f, foundation = 3f };
                        BuildingGen.Build(root, new Vector3(0, -0.3f, 0), spec, rnd, "TallestTower");
                        break;
                    }
            }
            nat.Flush("MiniNature");
            var label = new GameObject("Label");
            label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0, arc == "TheCore" ? 18 : 13, -6);
            var tmp = label.AddComponent<TMPro.TextMeshPro>();
            if (db.uiTheme != null && db.uiTheme.displayFont != null) tmp.font = db.uiTheme.displayFont;
            var ad = db.Arc(arc);
            tmp.text = ad != null ? ad.displayName.ToUpper() : arc;
            tmp.fontSize = 14;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(30, 4);
            tmp.color = new Color(0.92f, 0.96f, 1f);
        }

        static void IslandFor(string arc, Transform root)
        {
            if (LevelBuilder.Realistic(arc)) { RealIsland(arc, root); return; }
            var rock = MaterialCache.Glitch(new Color(0.3f, 0.28f, 0.34f), 1f);
            PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), root, new Vector3(0, -5, 0), new Vector3(16, 12, 14), rock, "Rock");
            Color top = arc == "PhysicsValley" ? new Color(0.45f, 0.75f, 0.35f) : arc == "ChemicalChaos" ? new Color(0.8f, 0.88f, 0.92f) : arc == "BioZone" ? new Color(1f, 0.5f, 0.6f) : arc == "TheCore" ? new Color(0.15f, 0.15f, 0.2f) : new Color(0.25f, 0.28f, 0.38f);
            PlaceholderFactory.Part(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(15, 0.5f, 13), MaterialCache.Glitch(top, 1f), "Top");
            var kit = db.Kit(arc);
            var rnd = new System.Random(arc.GetHashCode());
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            switch (arc)
            {
                case "CodeCity":
                    for (int i = 0; i < 7; i++)
                    {
                        float h = R(3, 9);
                        var tex = kit != null && kit.facadeTextures.Count > 0 ? kit.facadeTextures[0] : null;
                        PlaceholderFactory.Part(PrimitiveType.Cube, root, new Vector3(R(-5, 5), h / 2, R(-4, 4)), new Vector3(R(1.5f, 2.5f), h, R(1.5f, 2.5f)), MaterialCache.Facade(tex, new Color(0.6f, 0.65f, 0.8f), new Color(0.4f, 1.2f, 1.6f) * 0.7f, 1f, 0.3f), "Tower");
                    }
                    break;
                case "PhysicsValley":
                    if (kit != null)
                        for (int i = 0; i < 6 && kit.nature.Count > 0; i++)
                            SceneSpawn.Normalized(kit.nature[rnd.Next(kit.nature.Count)], root, root.position + new Vector3(R(-5, 5), 0.3f, R(-4, 4)), R(2, 5), R(0, 360));
                    var arm = new GameObject("Pendulum").transform; arm.SetParent(root, false); arm.localPosition = new Vector3(0, 9, 0);
                    PlaceholderFactory.Part(PrimitiveType.Cylinder, arm, new Vector3(0, -3, 0), new Vector3(0.15f, 3, 0.15f), rock, "Rod");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, arm, new Vector3(0, -6, 0), Vector3.one * 1.4f, MaterialCache.Emissive(new Color(0.6f, 0.45f, 0.2f), new Color(1.2f, 0.7f, 0.2f)), "Bob");
                    arm.gameObject.AddComponent<PendulumSwing>().amplitude = 25;
                    break;
                case "ChemicalChaos":
                    Color[] liq = { new Color(0.4f, 1.6f, 0.4f), new Color(0.3f, 0.9f, 1.8f), new Color(1.8f, 0.4f, 1.2f) };
                    for (int i = 0; i < 5; i++)
                    {
                        float h = R(3, 7);
                        var pos = new Vector3(R(-5, 5), h / 2, R(-4, 4));
                        PlaceholderFactory.Part(PrimitiveType.Cylinder, root, pos, new Vector3(1.6f, h / 2, 1.6f), MaterialCache.Emissive(new Color(0.55f, 0.75f, 0.85f), new Color(0.05f, 0.1f, 0.12f)), "Glass");
                        PlaceholderFactory.Part(PrimitiveType.Cylinder, root, pos - new Vector3(0, h * 0.2f, 0), new Vector3(1.4f, h * 0.3f, 1.4f), MaterialCache.Emissive(liq[i % 3] * 0.3f, liq[i % 3]), "Liquid");
                    }
                    break;
                case "BioZone":
                    var heart = new GameObject("Heart").transform; heart.SetParent(root, false); heart.localPosition = new Vector3(0, 3, 0);
                    var red = MaterialCache.Emissive(new Color(0.85f, 0.2f, 0.25f), new Color(0.5f, 0.05f, 0.1f));
                    PlaceholderFactory.Part(PrimitiveType.Sphere, heart, new Vector3(-1.2f, 0.6f, 0), Vector3.one * 3.2f, red, "L");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, heart, new Vector3(1.2f, 0.6f, 0), Vector3.one * 3.2f, red, "R");
                    PlaceholderFactory.Part(PrimitiveType.Sphere, heart, new Vector3(0, -1.2f, 0), Vector3.one * 2.8f, red, "Apex");
                    heart.gameObject.AddComponent<HeartPulse>().amount = 0.08f;
                    break;
                default:
                    PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), root, new Vector3(0, 6, 0), new Vector3(3, 12, 3), MaterialCache.Emissive(new Color(0.9f, 0.9f, 1f), new Color(1.5f, 1.5f, 2f), 0.6f, 1.6f), "Monolith");
                    break;
            }
            // label
            var label = new GameObject("Label");
            label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0, arc == "TheCore" ? 14 : 11, -6);
            var tmp = label.AddComponent<TMPro.TextMeshPro>();
            if (db.uiTheme != null && db.uiTheme.displayFont != null) tmp.font = db.uiTheme.displayFont;
            var a = db.Arc(arc);
            tmp.text = a != null ? a.displayName.ToUpper() : arc;
            tmp.fontSize = 14;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(30, 4);
            tmp.color = new Color(0.4f, 1f, 1f);
        }
    }
}
