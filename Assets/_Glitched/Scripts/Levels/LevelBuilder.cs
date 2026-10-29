using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Builds a level from a LevelPlan: the gameplay plane (L1, Z = 0) with ground, platforms, walls and every
    /// interactive object, then the region's living 3D backdrop (L0, L2-L5). Works in the editor (scene baking)
    /// and at runtime.
    /// </summary>
    public class LevelBuilder
    {
        readonly LevelPlan plan;
        readonly GameDatabase db;
        readonly ArcDataSO arc;
        readonly RegionThemeSO theme;
        readonly EnvironmentKitSO kit;
        Transform root, gameplay;
        public const float Depth = 5.4f;
        System.Random rnd;

        public LevelBuilder(LevelPlan plan, GameDatabase db)
        {
            this.plan = plan; this.db = db;
            arc = db.Arc(plan.arcId);
            theme = arc != null && arc.theme != null ? arc.theme : ContentDefaults.CreateTheme(plan.arcId);
            kit = db.Kit(plan.arcId);
            if (db.materials != null) CityMaterials.Library = db.materials;
            rnd = new System.Random(plan.levelId.GetHashCode());
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        T Pick<T>(IList<T> list) => list == null || list.Count == 0 ? default : list[rnd.Next(list.Count)];

        Color Accent => theme.accentRestored;

        public GameObject Build()
        {
            var go = new GameObject("Level_" + plan.levelId);
            root = go.transform;
            gameplay = Group("Gameplay (L1)");

            // sun
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(theme.sunEuler);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.color = theme.sunRestored;
            sun.intensity = theme.sunIntensityRestored;

            var director = go.AddComponent<RegionDirector>();
            director.arcId = plan.arcId; director.levelId = plan.levelId; director.sun = sun;
            director.minX = plan.minX; director.maxX = plan.maxX; director.objective = plan.objective;
            director.startHealthy = plan.startHealthy;
            if (!string.IsNullOrEmpty(plan.levelScript))
            {
                var type = Type.GetType("Glitched." + plan.levelScript);
                if (type != null) director.levelScript = (LevelScript)go.AddComponent(type);
                else Debug.LogWarning("[LevelBuilder] Unknown level script " + plan.levelScript);
            }

            if (Realistic(plan.arcId))
            {
                // the hand-made realistic world (no imported kits, no void)
                new CityGround(plan, Group("Geometry", gameplay), rnd, Depth).Build();
                BuildEntities();
                if (plan.arcId == "PhysicsValley") new ValleyBackdrop(plan, root).Build();
                else if (plan.arcId == "ChemicalChaos") new PlantBackdrop(plan, root).Build();
                else if (plan.arcId == "BioZone") new BioBackdrop(plan, root).Build();
                else if (plan.arcId == "TheCore") new CoreBackdrop(plan, root).Build();
                else new CityBackdrop(plan, root).Build();
                new ForegroundBuilder(plan, root).Build();      // L0 silhouettes (depth, like Limbo / Inside)
                new SetPieceBuilder(plan, root).Build();        // demolitions, chase blasts, exploding props
                new LightShaftBuilder(plan, root, theme, db).Build(); // god-rays behind the gameplay plane
                var refl = new GameObject("SceneReflection");
                refl.transform.SetParent(root, false);
                refl.transform.localPosition = new Vector3((plan.minX + plan.maxX) / 2f, 8f, 4f);
                var sr = refl.AddComponent<SceneReflection>();
                sr.minX = plan.minX; sr.maxX = plan.maxX;
            }
            else
            {
                BuildGeometry();
                BuildEntities();
                new BackdropBuilder(plan, db, theme, kit, root).Build();
            }
            return go;
        }

        /// <summary>Regions already rebuilt with the realistic generator (the rest still use the stylised kit).</summary>
        public static bool Realistic(string arcId) => arcId == "CodeCity" || arcId == "PhysicsValley" || arcId == "ChemicalChaos" || arcId == "BioZone" || arcId == "TheCore";

        Transform Group(string name, Transform parent = null)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent ?? root, false);
            return g;
        }

        // ------------------------------------------------------------------ materials
        Material topMat, sideMat, edgeMat, wallMat, platformMat;

        void Materials()
        {
            topMat = kit != null && kit.groundMaterial != null ? kit.groundMaterial : MaterialCache.Glitch(GroundTop(), 0.35f);
            sideMat = kit != null && kit.groundSideMaterial != null ? kit.groundSideMaterial : MaterialCache.Glitch(GroundSide(), 0.5f);
            wallMat = kit != null && kit.wallMaterial != null ? kit.wallMaterial : sideMat;
            edgeMat = MaterialCache.Emissive(Accent * 0.4f, Accent * 2.2f, 0f, 0.3f);
            platformMat = topMat;
        }

        Color GroundTop()
        {
            switch (plan.arcId)
            {
                case "PhysicsValley": return new Color(0.45f, 0.7f, 0.35f);
                case "ChemicalChaos": return new Color(0.75f, 0.82f, 0.85f);
                case "BioZone": return new Color(0.9f, 0.45f, 0.55f);
                case "TheCore": return new Color(0.12f, 0.12f, 0.18f);
                default: return new Color(0.2f, 0.22f, 0.3f);
            }
        }

        Color GroundSide()
        {
            switch (plan.arcId)
            {
                case "PhysicsValley": return new Color(0.45f, 0.35f, 0.3f);
                case "ChemicalChaos": return new Color(0.3f, 0.38f, 0.42f);
                case "BioZone": return new Color(0.6f, 0.2f, 0.3f);
                case "TheCore": return new Color(0.06f, 0.06f, 0.1f);
                default: return new Color(0.14f, 0.15f, 0.22f);
            }
        }

        // ------------------------------------------------------------------ geometry
        void BuildGeometry()
        {
            Materials();
            var geo = Group("Geometry", gameplay);
            foreach (var g in plan.ground) Block(geo, g.x, g.y, g.z - 16f, g.z, Depth, true, "Ground");
            foreach (var p in plan.platforms)
            {
                var b = Block(geo, p.x - p.w / 2, p.x + p.w / 2, p.y - 0.8f, p.y, Depth * 0.7f, true, "Platform");
                if (p.move != Vector2.zero)
                {
                    var mp = b.gameObject.AddComponent<MovingPlatform>();
                    mp.offset = new Vector3(p.move.x, p.move.y, 0);
                    mp.period = p.period;
                    mp.phase = R(0, 1);
                }
                else SceneSpawn.MarkStatic(b.gameObject);
                // glowing underside so platforms read against the backdrop
                var under = PlaceholderFactory.Part(PrimitiveType.Cube, b, new Vector3(0, -0.42f, -Depth * 0.35f), new Vector3(1, 0.06f, 0.02f), edgeMat, "Underglow");
                under.localScale = new Vector3(1f, 0.06f / 0.8f, 0.02f);
            }
            foreach (var w in plan.walls) Block(geo, w.x - 0.5f, w.x + 0.5f, w.y, w.z, Depth, false, "Wall", wallMat);
            foreach (var c in plan.ceilings) Block(geo, c.x, c.y, c.z, c.z + 10f, Depth, false, "Ceiling", wallMat);
            DecorateGround(geo);
        }

        Transform Block(Transform parent, float x0, float x1, float y0, float y1, float depth, bool topCap, string name, Material side = null)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(parent, false);
            float w = x1 - x0, h = y1 - y0;
            t.localPosition = new Vector3((x0 + x1) / 2, (y0 + y1) / 2, 0);
            var body = PlaceholderFactory.Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(w, h, depth), side ?? sideMat, "Body");
            body.GetComponent<Renderer>().receiveShadows = true;
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(w, h, depth);
            if (topCap)
            {
                PlaceholderFactory.Part(PrimitiveType.Cube, t, new Vector3(0, h / 2 - 0.12f, 0), new Vector3(w + 0.08f, 0.3f, depth + 0.1f), topMat, "Top");
                PlaceholderFactory.Part(PrimitiveType.Cube, t, new Vector3(0, h / 2 + 0.01f, -depth / 2 - 0.06f), new Vector3(w + 0.1f, 0.09f, 0.06f), edgeMat, "EdgeGlow");
                PlaceholderFactory.Part(PrimitiveType.Cube, t, new Vector3(0, h / 2 - 0.5f, -depth / 2 - 0.04f), new Vector3(w, 0.5f, 0.04f), MaterialCache.Glitch(GroundSide() * 0.7f, 0.5f), "Skirt");
            }
            else
            {
                PlaceholderFactory.Part(PrimitiveType.Cube, t, new Vector3(0, 0, -depth / 2 - 0.04f), new Vector3(0.08f, h, 0.05f), edgeMat, "EdgeGlow");
            }
            SceneSpawn.MarkStatic(go);
            return t;
        }

        /// <summary>Props on the ground tops (behind the gameplay plane, no colliders).</summary>
        void DecorateGround(Transform geo)
        {
            var deco = Group("GroundProps", geo);
            var props = new List<GameObject>();
            if (kit != null)
            {
                props.AddRange(kit.props);
                if (plan.arcId == "PhysicsValley" || plan.arcId == "BioZone") props.AddRange(kit.nature);
            }
            foreach (var g in plan.ground)
            {
                for (float x = g.x + 1.5f; x < g.y - 1.5f; x += R(3.5f, 7f))
                {
                    var prefab = Pick(props);
                    float z = R(1.0f, 2.3f);
                    if (prefab != null)
                    {
                        float size = plan.arcId == "PhysicsValley" ? R(0.6f, 2.2f) : R(0.5f, 1.3f);
                        var p = SceneSpawn.Normalized(prefab, deco, new Vector3(x, g.z, z), size, R(0, 360));
                        if (p != null && plan.arcId == "BioZone") Tint(p, new Color(1f, 0.45f, 0.55f));
                        if (p != null) SceneSpawn.MarkStatic(p);
                    }
                    else if (rnd.NextDouble() < 0.5)
                    {
                        var c = PlaceholderFactory.Part(PrimitiveType.Cube, deco, new Vector3(x, g.z + 0.3f, z), new Vector3(0.5f, 0.6f, 0.5f), MaterialCache.Glitch(GroundSide() * 1.3f, 0.8f), "Crate");
                        SceneSpawn.MarkStatic(c.gameObject);
                    }
                }
            }
        }

        static void Tint(GameObject go, Color c)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                r.SetPropertyBlock(mpb);
            }
        }

        // ------------------------------------------------------------------ entities
        Vector3 OnGround(float x, float y = float.NaN) => new Vector3(x, float.IsNaN(y) ? plan.GroundY(x) : y, 0);

        GameObject Entity(string name, Vector3 pos, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ?? gameplay, false);
            go.transform.position = pos;
            return go;
        }

        void BuildEntities()
        {
            plan.ResolveTopics();
            var ents = Group("Entities", gameplay);
            foreach (var s in plan.spawns)
            {
                var sp = Entity("Spawn_" + s.id, OnGround(s.x), ents).AddComponent<SpawnPoint>();
                sp.id = s.id; sp.facing = s.facing;
            }
            foreach (var c in plan.checkpoints) CheckpointTerminal(ents, c);
            foreach (var e in plan.enemies)
            {
                var data = db.Enemy(e.enemyId);
                if (data == null) { Debug.LogWarning("[LevelBuilder] Unknown enemy " + e.enemyId); continue; }
                var go = Entity("Enemy_" + e.enemyId, OnGround(e.x, e.y), ents);
                var en = go.AddComponent<Enemy>();
                en.data = data; en.id = e.id; en.tutorialFight = e.tutorial; en.firstQuestionId = e.firstQuestion; en.topic = e.topic;
                go.AddComponent<PlaneLock>();
            }
            foreach (var n in plan.npcs)
            {
                var go = Entity("NPC_" + n.name, OnGround(n.x), ents);
                var npc = go.AddComponent<NPC>();
                npc.displayName = n.name; npc.corruptedLines = new List<string>(n.corrupted); npc.restoredLines = new List<string>(n.restored);
                npc.clue = n.clue; npc.walkIntoWall = n.wallBonk; npc.shirt = n.shirt;
            }
            foreach (var t in plan.terminals) Terminal(ents, t);
            foreach (var d in plan.doors) Door(ents, d);
            foreach (var b in plan.barriers) Barrier(ents, b);
            int si = 0;
            foreach (var s in plan.shards) Shard(ents, s, si++);
            foreach (var p in plan.portals) Portal(ents, p);
            foreach (var z in plan.zones)
            {
                var go = Entity("CameraZone", new Vector3((z.x0 + z.x1) / 2, 10, 0), ents);
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true; box.size = new Vector3(z.x1 - z.x0, 60, 6);
                var cz = go.AddComponent<CameraZone>();
                cz.distance = z.distance; cz.fov = z.fov; cz.yawOffset = z.yaw; cz.heightOffset = z.height; cz.companionLine = z.line;
            }
            foreach (var g in plan.gravity)
            {
                var go = Entity("GravityZone", new Vector3(g.center.x, g.center.y, 0), ents);
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true; box.size = new Vector3(g.width, g.height, 6);
                go.AddComponent<GravityZone>();
                // visual: shimmering upward arrows
                for (float x = g.xMin + 1; x < g.xMax; x += 3)
                {
                    var a = PlaceholderFactory.MeshPart(ProceduralMeshes.Cone(), go.transform, new Vector3(x - g.center.x, 0, 2.4f), new Vector3(0.4f, 0.8f, 0.4f), MaterialCache.Emissive(new Color(0.5f, 0.3f, 1f), new Color(0.8f, 0.4f, 2f), 0, 0.2f), "UpArrow");
                    a.gameObject.AddComponent<FloatingBob>().bobHeight = 0.6f;
                }
            }
            foreach (var h in plan.hazards) HazardVolume(ents, h);
            foreach (var s in plan.signs) Sign(ents, s);
            if (!float.IsNaN(plan.shopX)) ShopKiosk(ents, plan.shopX, true);
            if (!float.IsNaN(plan.masteryX)) ShopKiosk(ents, plan.masteryX, false);
            if (plan.boss != null) BossArena(ents, plan.boss);
            new ParkourBuilder(plan, Group("Parkour", gameplay), db, Accent).Build();
        }

        void CheckpointTerminal(Transform parent, CheckpointDef c)
        {
            var go = Entity("Checkpoint_" + c.id, OnGround(c.x), parent);
            if (Realistic(plan.arcId)) CityProps.Checkpoint(go.transform, Accent);
            else
            {
                var dark = MaterialCache.Glitch(new Color(0.12f, 0.13f, 0.2f), 0.6f);
                PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.9f, 1.2f), new Vector3(0.9f, 1.8f, 0.6f), dark, "Pillar");
                PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 1.45f, 0.88f), new Vector3(0.7f, 0.5f, 0.05f), MaterialCache.Emissive(Color.black, new Color(1.6f, 0.2f, 1.2f), 0f, 1f), "Screen");
            }
            var cp = go.AddComponent<Checkpoint>();
            cp.id = c.id;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(2.4f, 3f, 3f); col.center = new Vector3(0, 1.5f, 0);
        }

        void Terminal(Transform parent, TerminalDef t)
        {
            if (!string.IsNullOrEmpty(t.topic)) { LessonTerminalAt(parent, t); return; }
            var go = Entity("Terminal", OnGround(t.x), parent);
            if (Realistic(plan.arcId))
            {
                CityProps.Terminal(go.transform, Holo("code_lines"), Accent);
                var ct0 = go.AddComponent<ClueTerminal>();
                ct0.title = t.title; ct0.text = t.text; ct0.pixelComment = t.comment;
                return;
            }
            var dark = MaterialCache.Glitch(new Color(0.1f, 0.12f, 0.18f), 0.6f);
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.55f, 1f), new Vector3(1.1f, 1.1f, 0.6f), dark, "Desk");
            var holo = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), go.transform, new Vector3(0, 1.75f, 0.9f), new Vector3(1.6f, 1.0f, 1), MaterialCache.Get("holo"), "Holo");
            var hb = holo.gameObject.AddComponent<HologramBillboard>();
            hb.content = Holo(plan.arcId == "PhysicsValley" ? "equations" : plan.arcId == "ChemicalChaos" ? "molecules" : plan.arcId == "BioZone" ? "dna" : "code_lines");
            hb.color = Accent * 1.6f;
            var ct = go.AddComponent<ClueTerminal>();
            ct.title = t.title; ct.text = t.text; ct.pixelComment = t.comment;
        }

        /// <summary>A lesson terminal: the terminal, a holo-sign with the topic, and a trigger that shows it on the HUD.</summary>
        void LessonTerminalAt(Transform parent, TerminalDef t)
        {
            var go = Entity("LessonTerminal_" + t.topic, OnGround(t.x), parent);
            string holo = plan.arcId == "PhysicsValley" ? "equations" : plan.arcId == "ChemicalChaos" ? "molecules" : plan.arcId == "BioZone" ? "dna" : "code_lines";
            CityProps.Terminal(go.transform, Holo(holo), Accent);
            var lt = go.AddComponent<LessonTerminal>();
            lt.topic = t.topic; lt.pixelComment = t.comment;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(5f, 4f, 4f); col.center = new Vector3(0, 2f, 0);
            string title = LessonTitles.TryGetValue(t.topic, out var tt) ? tt : t.topic;
            var lbl = MakeLabel(go.transform, new Vector3(0, 3.3f, 0.5f), "LESSON ▸ " + title.ToUpperInvariant(), 0.62f);
            lbl.color = Color.white;
            var sub = MakeLabel(go.transform, new Vector3(0, 2.85f, 0.5f), "CBSE CLASS XI", 0.42f);
            sub.color = new Color(1f, 0.85f, 0.3f);
        }

        /// <summary>Topic titles for the terminal signs (the editor has no StreamingAssets loaded; kept in step with Lessons/Class11.json).</summary>
        public static readonly Dictionary<string, string> LessonTitles = new Dictionary<string, string>
        {
            { "cs_bool", "Boolean Logic" }, { "cs_python", "Python Basics" }, { "cs_flow", "Flow of Control" }, { "cs_numsys", "Number Systems" }, { "cs_lists", "Strings & Lists" },
            { "ph_laws", "Laws of Motion" }, { "ph_kinematics", "Kinematics" }, { "ph_work", "Work, Energy & Power" }, { "ph_osc", "Oscillations" }, { "ph_grav", "Gravitation" },
            { "ch_basic", "Mole Concept" }, { "ch_atom", "Structure of Atom" }, { "ch_bond", "Chemical Bonding" }, { "ch_periodic", "Periodic Trends" }, { "ch_redox", "Redox Reactions" },
            { "bio_cell", "The Cell" }, { "bio_breath", "Breathing" }, { "bio_circ", "Circulation" }, { "bio_neural", "Neural Control" }, { "bio_division", "Cell Division" },
        };

        public static Texture2D Holo(string name) => holograms != null && holograms.TryGetValue(name, out var t) ? t : Resources.Load<Texture2D>("Holograms/" + name);

        /// <summary>Hologram textures, filled by the editor builder (name -> texture).</summary>
        public static Dictionary<string, Texture2D> holograms;

        void Door(Transform parent, DoorDef d)
        {
            var go = Entity("JammedDoor_" + d.id, OnGround(d.x), parent);
            if (Realistic(plan.arcId))
            {
                CityProps.Door(go.transform, Depth);
                var jd0 = go.AddComponent<JammedDoor>();
                jd0.id = d.id; jd0.concept = d.concept; jd0.questionId = d.questionId;
                if (!string.IsNullOrEmpty(d.line)) jd0.jammedLine = d.line;
                jd0.radius = 2.6f;
                return;
            }
            var frame = MaterialCache.Glitch(new Color(0.18f, 0.2f, 0.3f), 0.8f);
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 3.6f, 0), new Vector3(2.4f, 0.4f, Depth), frame, "Lintel");
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(-1.05f, 1.7f, 0), new Vector3(0.3f, 3.4f, Depth), frame, "PostL");
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(1.05f, 1.7f, 0), new Vector3(0.3f, 3.4f, Depth), frame, "PostR");
            var leaf = new GameObject("Leaf").transform;
            leaf.SetParent(go.transform, false);
            leaf.localPosition = new Vector3(0, 1.7f, 0);
            PlaceholderFactory.Part(PrimitiveType.Cube, leaf, Vector3.zero, new Vector3(1.8f, 3.4f, Depth * 0.8f), MaterialCache.Glitch(new Color(0.25f, 0.1f, 0.3f), 1.2f), "Panel");
            var stripe = PlaceholderFactory.Part(PrimitiveType.Cube, leaf, new Vector3(0, 0, -Depth * 0.41f), new Vector3(1.6f, 0.25f, 0.05f), MaterialCache.Emissive(Color.black, new Color(1.8f, 0.2f, 1.2f), 0, 1.5f), "ErrorStripe");
            var block = leaf.gameObject.AddComponent<BoxCollider>();
            block.size = new Vector3(1.8f, 3.4f, Depth);
            var holo = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), go.transform, new Vector3(0, 4.6f, -0.5f), new Vector3(2.6f, 1.3f, 1), MaterialCache.Get("holo"), "ErrorHolo");
            var hb = holo.gameObject.AddComponent<HologramBillboard>();
            hb.content = Holo("error404"); hb.color = new Color(2f, 0.4f, 1.2f);
            var jd = go.AddComponent<JammedDoor>();
            jd.id = d.id; jd.concept = d.concept; jd.questionId = d.questionId;
            if (!string.IsNullOrEmpty(d.line)) jd.jammedLine = d.line;
            jd.radius = 2.6f;
        }

        void Barrier(Transform parent, BarrierDef b)
        {
            var go = Entity("SealedBarrier_" + b.ability, OnGround(b.x), parent);
            if (Realistic(plan.arcId)) CityProps.Barrier(go.transform, b.height, Depth, Accent);
            else
            {
                var wall = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), go.transform, new Vector3(0, b.height / 2, -Depth / 2 + 0.2f), new Vector3(1.2f, b.height, 1), MaterialCache.Get("holo"), "Field");
                var hb = wall.gameObject.AddComponent<HologramBillboard>();
                hb.content = Holo("binary_rain"); hb.color = new Color(0.3f, 1.2f, 2.2f); hb.scroll = new Vector2(0, 0.4f);
                for (int i = 0; i < 2; i++)
                    PlaceholderFactory.Part(PrimitiveType.Cylinder, go.transform, new Vector3(i == 0 ? -0.7f : 0.7f, b.height / 2, 0), new Vector3(0.25f, b.height / 2, 0.25f), MaterialCache.Emissive(new Color(0.1f, 0.15f, 0.2f), Accent * 1.5f, 0, 0.6f), "Pylon");
            }
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, b.height / 2, 0); col.size = new Vector3(1.0f, b.height + 30f, Depth);
            var trig = new GameObject("Hint");
            trig.transform.SetParent(go.transform, false);
            var tc = trig.AddComponent<BoxCollider>();
            tc.isTrigger = true; tc.size = new Vector3(4f, 6f, 4f); tc.center = new Vector3(-2f, 2f, 0);
            var sb = go.AddComponent<SealedBarrier>();
            sb.requiredAbility = b.ability;
            if (!string.IsNullOrEmpty(b.line)) sb.lockedLine = b.line;
            trig.AddComponent<TriggerRelay>().target = sb;
        }

        void Shard(Transform parent, Vector3 s, int i)
        {
            var go = Entity("DataShard", new Vector3(s.x, s.y + 0.7f, 0), parent);
            PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), go.transform, Vector3.zero, new Vector3(0.4f, 0.65f, 0.4f), MaterialCache.Emissive(new Color(0.4f, 0.35f, 0.1f), new Color(2.2f, 1.7f, 0.3f), 0, 0.3f), "Crystal");
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.7f;
            var d = go.AddComponent<DataShard>();
            d.id = $"{plan.levelId}_shard{i}";
            d.value = Mathf.RoundToInt(s.z);
        }

        void Portal(Transform parent, PortalDef p)
        {
            var go = Entity("Portal_" + p.scene, OnGround(p.x, p.y), parent);
            if (Realistic(plan.arcId))
            {
                CityProps.Portal(go.transform, Accent);
                MakeLabel(go.transform, new Vector3(0, 3.8f, 0.4f), p.label, 1.0f);
                PortalComponent(go, p);
                return;
            }
            var ringMat = MaterialCache.Emissive(new Color(0.05f, 0.1f, 0.15f), Accent * 2.4f, 0, 0.3f);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2;
                PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(Mathf.Cos(a) * 1.3f, 1.8f + Mathf.Sin(a) * 1.6f, 0.6f), new Vector3(0.28f, 0.28f, 0.28f), ringMat, "Seg" + i, new Vector3(0, 0, a * Mathf.Rad2Deg));
            }
            var inner = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), go.transform, new Vector3(0, 1.8f, 0.62f), new Vector3(2.4f, 3f, 1), MaterialCache.Get("holo"), "Inner");
            var hb = inner.gameObject.AddComponent<HologramBillboard>();
            hb.content = Holo("binary_rain"); hb.color = Accent * 1.2f; hb.scroll = new Vector2(0, -0.3f);
            var spin = new GameObject("Spin").transform; spin.SetParent(go.transform, false); spin.localPosition = new Vector3(0, 1.8f, 0.6f);
            spin.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 0, 40);
            var label = MakeLabel(go.transform, new Vector3(0, 4.1f, 0.4f), p.label, 1.2f);
            PortalComponent(go, p);
        }

        void PortalComponent(GameObject go, PortalDef p)
        {
            var portal = go.AddComponent<Portal>();
            portal.targetScene = p.scene; portal.spawnId = p.spawn; portal.label = "Go: " + p.label; portal.completesLevel = p.completes; portal.auto = p.auto; portal.requiresLevelOpen = p.requires;
            portal.radius = 2.2f;
            if (p.auto)
            {
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true; col.size = new Vector3(2, 3.5f, 4); col.center = new Vector3(0, 1.8f, 0);
            }
        }

        void HazardVolume(Transform parent, HazardDef h)
        {
            var go = Entity("Hazard_" + h.kind, new Vector3((h.x0 + h.x1) / 2, h.y, 0), parent);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(h.x1 - h.x0, 1.4f, Depth); col.center = new Vector3(0, 0.2f, 0);
            var hz = go.AddComponent<Hazard>();
            hz.respawn = h.respawn;
            hz.damage = 12;
            if (h.kind == "sparks")
            {
                hz.line = "Ow! That's static. Physics is literally shocking.";
                for (float x = h.x0; x <= h.x1; x += 1f)
                {
                    var s = PlaceholderFactory.MeshPart(ProceduralMeshes.Crystal(), go.transform, new Vector3(x - (h.x0 + h.x1) / 2, 0.25f, 0), new Vector3(0.35f, 0.6f, 0.35f), MaterialCache.Emissive(new Color(0.3f, 0.3f, 0.1f), new Color(2f, 1.8f, 0.4f), 0, 1.5f), "Spark");
                    s.gameObject.AddComponent<GlitchProp>().wanderRadius = 0.2f;
                }
                return;
            }
            if (Realistic(plan.arcId))
            {
                // a real pool of something you shouldn't touch: dark liquid with a faint glow, in a concrete kerb
                bool blood = h.kind == "blood";
                var liquid = CityMaterials.Water(blood ? new Color(0.12f, 0.02f, 0.03f) : new Color(0.03f, 0.07f, 0.03f), blood ? new Color(0.25f, 0.04f, 0.05f) : new Color(0.06f, 0.13f, 0.05f),
                    new Vector2(blood ? 0.3f : 0.1f, 0f), blood ? new Color(0.1f, 0.01f, 0.01f) : new Color(0.03f, 0.12f, 0.02f));
                var pool = new MeshBuilder(2);
                pool.BoxC(new Vector3(0, 0.02f, 0), new Vector3(h.x1 - h.x0, 0.04f, Depth), 0, MeshBuilder.Faces.Top);
                pool.BoxC(new Vector3(0, 0.08f, -Depth / 2f), new Vector3(h.x1 - h.x0 + 0.3f, 0.16f, 0.15f), 1);
                pool.BoxC(new Vector3(0, 0.08f, Depth / 2f), new Vector3(h.x1 - h.x0 + 0.3f, 0.16f, 0.15f), 1);
                pool.ToObject("Liquid", go.transform, new[] { liquid, CityMaterials.PBR("Concrete046", new Color(0.7f, 0.69f, 0.66f), 2f) }, false);
                hz.line = blood ? "Don't swim in that. That's a sentence I never thought I'd say." : "That's acid. Glowing green means NO.";
                return;
            }
            Material m = MaterialCache.Instance("flow");
            Color c = h.kind == "blood" ? new Color(0.8f, 0.05f, 0.12f) : new Color(0.3f, 1f, 0.2f);
            m.SetColor("_BaseColor", c);
            m.SetColor("_EmissionColor", c * (h.kind == "blood" ? 0.3f : 0.8f));
            m.SetVector("_Flow", new Vector4(h.kind == "blood" ? 0.12f : 0.03f, 0.01f, 0, 0));
            if (kit != null && kit.liquidMaterial != null && kit.liquidMaterial.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", kit.liquidMaterial.GetTexture("_BaseMap"));
            m = Store(m);
            var surf = PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, -0.1f, 0), new Vector3(h.x1 - h.x0, 0.4f, Depth + 0.4f), m, "Liquid");
            hz.line = h.kind == "blood" ? "Don't swim in the blood. That's a sentence I never thought I'd say." : "That's acid. Neon green means NO.";
        }

        static Material Store(Material m) => MaterialCache.OnCreated != null ? MaterialCache.OnCreated(m) : m;

        void Sign(Transform parent, SignDef s)
        {
            var go = Entity("Sign", new Vector3(s.x, s.y, 0.5f), parent);
            MakeLabel(go.transform, Vector3.zero, s.text, 0.9f);
        }

        TextMeshPro MakeLabel(Transform parent, Vector3 local, string text, float size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var t = go.AddComponent<TextMeshPro>();
            var font = db.uiTheme != null && db.uiTheme.displayFont != null ? db.uiTheme.displayFont : TMP_Settings.defaultFontAsset;
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size * 4f;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Accent * 1.3f;
            t.rectTransform.sizeDelta = new Vector2(12, 2);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        void ShopKiosk(Transform parent, float x, bool shop)
        {
            var go = Entity(shop ? "ShopTerminal" : "MasteryTerminal", OnGround(x), parent);
            var body = MaterialCache.Glitch(shop ? new Color(0.35f, 0.15f, 0.4f) : new Color(0.12f, 0.3f, 0.35f), 0.6f);
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 1.1f, 1.1f), new Vector3(1.4f, 2.2f, 0.8f), body, "Kiosk");
            var scr = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), go.transform, new Vector3(0, 1.5f, 0.68f), new Vector3(1.1f, 1.0f, 1), MaterialCache.Get("holo"), "Screen");
            var hb = scr.gameObject.AddComponent<HologramBillboard>();
            hb.content = Holo(shop ? "ad_ram" : "wave"); hb.color = shop ? new Color(2f, 0.5f, 1.6f) : new Color(0.4f, 1.6f, 2f);
            MakeLabel(go.transform, new Vector3(0, 2.8f, 0.6f), shop ? "COSTUMES" : "MASTERY", 0.9f);
            if (shop) go.AddComponent<ShopTerminal>().radius = 2f; else go.AddComponent<MasteryTerminal>().radius = 2f;
        }

        void BossArena(Transform parent, BossDef b)
        {
            BossDataSO data = null;
            foreach (var a in db.arcs)
            {
                if (a == null) continue;
                if (a.boss != null && a.boss.id == b.bossId) data = a.boss;
                if (a.miniBoss != null && a.miniBoss.id == b.bossId) data = a.miniBoss;
            }
            if (data == null) { Debug.LogWarning("[LevelBuilder] Unknown boss " + b.bossId); return; }
            var bossGo = Entity("Boss_" + b.bossId, OnGround(b.x, b.y), parent);
            var en = bossGo.AddComponent<Enemy>();
            en.data = data; en.id = b.bossId;
            var trig = Entity("BossTrigger", OnGround(b.triggerX), parent);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(2.5f, 12, 6); box.center = new Vector3(0, 5, 0);
            var mark = Entity("PlayerMark", OnGround(b.playerX), parent);
            var bf = trig.AddComponent<BossFight>();
            bf.boss = en; bf.isMiniBoss = b.mini; bf.playerMark = mark.transform;
            if (Realistic(plan.arcId)) { CityProps.BossArena(bossGo.transform, data.height, b.mini); return; }
            // arena dressing: an energy ring behind the boss
            var ring = new GameObject("BossRing").transform;
            ring.SetParent(bossGo.transform, false);
            ring.localPosition = new Vector3(0, data.height * 0.7f, 6f);
            var m = MaterialCache.Emissive(new Color(0.1f, 0.05f, 0.1f), theme.accentCorrupted * 2f, 0, 1.2f);
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2;
                PlaceholderFactory.Part(PrimitiveType.Cube, ring, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * data.height * 0.9f, Vector3.one * 0.5f, m, "R" + i, new Vector3(0, 0, a * Mathf.Rad2Deg));
            }
            ring.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 0, b.mini ? 30 : 18);
        }
    }
}
