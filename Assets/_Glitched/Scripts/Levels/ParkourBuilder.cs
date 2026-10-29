using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Builds a plan's parkour pieces: vault blocks, slide ducts, springboards, ziplines, swing bars, crumbling
    /// slabs, quiz pits (answer pillars + firewall) and the timed run's START / FINISH arches. Each region gets its
    /// own look (city concrete and steel, valley timber, plant steel, lab white).
    /// </summary>
    public class ParkourBuilder
    {
        readonly LevelPlan plan;
        readonly Transform parent;
        readonly GameDatabase db;
        readonly Color accent;
        readonly System.Random rnd;
        const float PitFloor = -8f;
        Material concrete, steel, yellow, dark, wood, glowMat, holo, white;

        public ParkourBuilder(LevelPlan plan, Transform parent, GameDatabase db, Color accent)
        {
            this.plan = plan; this.parent = parent; this.db = db; this.accent = accent;
            rnd = new System.Random(plan.levelId.GetHashCode() ^ 0x5a17);
        }

        bool Valley => plan.arcId == "PhysicsValley";
        bool Bio => plan.arcId == "BioZone";

        void Mats()
        {
            concrete = CityMaterials.PBR("Concrete046", new Color(0.74f, 0.73f, 0.7f), 2f);
            steel = CityMaterials.PBR("PaintedMetal004", new Color(0.36f, 0.4f, 0.44f), 1.5f, 0.7f, 0.6f);
            yellow = CityMaterials.Solid(new Color(0.95f, 0.72f, 0.1f), 0.5f);
            dark = CityMaterials.Solid(new Color(0.08f, 0.08f, 0.09f), 0.5f, 0.6f);
            wood = CityMaterials.PBR("Wood066", new Color(0.66f, 0.52f, 0.4f), 1.2f);
            white = CityMaterials.Solid(new Color(0.9f, 0.92f, 0.93f), 0.35f);
            glowMat = CityMaterials.Emissive(accent * 0.3f, accent * 2.4f);
            holo = CityMaterials.Emissive(new Color(0.03f, 0.04f, 0.08f), new Color(0.02f, 0.05f, 0.1f) + accent * 0.12f);
            if (Valley) { concrete = CityMaterials.PBR("Rock064", new Color(0.8f, 0.78f, 0.74f), 3f); steel = wood; }
            if (Bio) { concrete = white; steel = CityMaterials.Solid(new Color(0.85f, 0.45f, 0.55f), 0.35f); }
        }

        public void Build()
        {
            if (plan.parkour.Count == 0 && plan.run == null) return;
            Mats();
            foreach (var d in plan.parkour)
            {
                switch (d.kind)
                {
                    case "vault": Vault(d); break;
                    case "duct": Duct(d); break;
                    case "spring": Spring(d); break;
                    case "zip": Zip(d); break;
                    case "swing": Swing(d); break;
                    case "crumble": Crumble(d); break;
                    case "quiz": Quiz(d); break;
                }
            }
            if (plan.run != null) Run(plan.run);
        }

        GameObject Obj(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go;
        }

        static BoxCollider Box(GameObject go, Vector3 center, Vector3 size)
        {
            var b = go.AddComponent<BoxCollider>();
            b.center = center; b.size = size;
            return b;
        }

        TextMeshPro Label(Transform t, Vector3 local, string text, float size, Color color, Vector2 box)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(t, false);
            go.transform.localPosition = local;
            var tm = go.AddComponent<TextMeshPro>();
            var font = db != null && db.uiTheme != null && db.uiTheme.displayFont != null ? db.uiTheme.displayFont : TMP_Settings.defaultFontAsset;
            if (font != null) tm.font = font;
            tm.text = text;
            tm.fontSize = size;
            tm.enableAutoSizing = true; tm.fontSizeMin = size * 0.35f; tm.fontSizeMax = size;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = color;
            tm.rectTransform.sizeDelta = box;
            tm.textWrappingMode = TextWrappingModes.Normal;
            return tm;
        }

        float Ground(float x) { float g = plan.GroundY(x); return g < -5f ? PitFloor : g; }

        // ------------------------------------------------------------------ vault blocks
        void Vault(ParkourDef d)
        {
            float y = plan.GroundY(d.x);
            var go = Obj("VaultBlock", new Vector3(d.x, y, 0f));
            Box(go, new Vector3(0, d.h / 2f, 0), new Vector3(d.w, d.h, LevelBuilder.Depth));
            var mb = new MeshBuilder(3);
            if (Valley)
            {
                // a fallen log on a rock
                mb.Tube(new Vector3(0, d.h * 0.5f, -1.6f), new Vector3(0, d.h * 0.5f, 1.6f), d.h * 0.5f, d.h * 0.48f, 12, 0);
                mb.BoxC(new Vector3(0, d.h * 0.5f, -1.62f), new Vector3(d.h * 0.8f, d.h * 0.8f, 0.02f), 1);
            }
            else
            {
                // a concrete block (jersey barrier / plinth) with a hazard band facing the camera
                mb.Box(new Vector3(-d.w / 2f, 0, -1.2f), new Vector3(d.w, d.h, 2.4f), 0, 0);
                mb.BoxC(new Vector3(0, d.h - 0.14f, -1.215f), new Vector3(d.w, 0.16f, 0.03f), 1);
                for (float x = -d.w / 2f + 0.15f; x < d.w / 2f; x += 0.4f) mb.BoxC(new Vector3(x, d.h - 0.14f, -1.23f), new Vector3(0.16f, 0.16f, 0.02f), 2);
            }
            var vis = mb.ToObject("Visual", go.transform, Valley ? new[] { wood, CityMaterials.PBR("Wood066", new Color(0.8f, 0.7f, 0.55f), 0.4f), dark } : new[] { concrete, yellow, dark });
            SceneSpawn.MarkStatic(vis); SceneSpawn.MarkStatic(go);
        }

        // ------------------------------------------------------------------ slide ducts
        void Duct(ParkourDef d)
        {
            float y = plan.GroundY(d.x), bottom = y + d.h;
            var go = Obj("SlideDuct", new Vector3(d.x, bottom, 0f));
            Box(go, new Vector3(0, 7f, 0), new Vector3(d.w, 14f, LevelBuilder.Depth));   // no jumping over: it hangs from high above
            var mb = new MeshBuilder(4);
            mb.Box(new Vector3(-d.w / 2f, 0f, -2.6f), new Vector3(d.w, 1.1f, 5.2f), 0, 0);                    // the duct itself
            for (float x = -d.w / 2f + 0.1f; x < d.w / 2f; x += 0.35f) mb.BoxC(new Vector3(x, 0.14f, -2.62f), new Vector3(0.17f, 0.18f, 0.03f), 1);
            mb.BoxC(new Vector3(0, 0.14f, -2.615f), new Vector3(d.w, 0.18f, 0.02f), 2);
            foreach (float z in new[] { -2.2f, 2.2f }) mb.Tube(new Vector3(0, 1.1f, z), new Vector3(0, 14f, z), 0.06f, 0.06f, 6, 3);   // hangers
            mb.Box(new Vector3(-d.w / 2f - 0.05f, 1.1f, -2.7f), new Vector3(d.w + 0.1f, 12.9f, 0.1f), 3, 3);   // the wall above, so it reads as solid
            var vis = mb.ToObject("Visual", go.transform, new[] { Valley ? wood : steel, yellow, dark, Valley ? CityMaterials.PBR("Rock058", new Color(0.7f, 0.68f, 0.64f), 2f) : concrete });
            SceneSpawn.MarkStatic(vis); SceneSpawn.MarkStatic(go);
            var hint = Label(go.transform, new Vector3(0, -0.45f, -2.75f), "SLIDE ▼", 3f, accent * 1.4f, new Vector2(3f, 0.6f));
            hint.fontSizeMin = 1f;
        }

        // ------------------------------------------------------------------ springboards
        void Spring(ParkourDef d)
        {
            float y = plan.GroundY(d.x);
            var go = Obj("Springboard", new Vector3(d.x, y, 0f));
            var mb = new MeshBuilder(2);
            mb.BoxC(new Vector3(0, 0.04f, 0), new Vector3(1.8f, 0.08f, 2.2f), 1);
            for (int i = 0; i < 4; i++) mb.Cylinder(new Vector3(i < 2 ? -0.55f : 0.55f, 0.08f, i % 2 == 0 ? -0.6f : 0.6f), 0.16f, 0.16f, 0.12f, 8, 0);
            SceneSpawn.MarkStatic(mb.ToObject("Base", go.transform, new[] { CityMaterials.Solid(new Color(0.7f, 0.72f, 0.74f), 0.3f, 0.9f), dark }));
            var pad = new GameObject("Pad").transform;
            pad.SetParent(go.transform, false);
            pad.localPosition = new Vector3(0, 0.2f, 0);
            var pm = new MeshBuilder(2);
            pm.BoxC(new Vector3(0, 0.05f, 0), new Vector3(1.7f, 0.12f, 2.1f), 0);
            for (float x = -0.75f; x < 0.8f; x += 0.3f) pm.BoxC(new Vector3(x, 0.115f, -1.05f), new Vector3(0.15f, 0.02f, 0.02f), 1);
            pm.ToObject("PadVisual", pad, new[] { yellow, dark });
            Box(go, new Vector3(0, 0.15f, 0), new Vector3(1.7f, 0.3f, LevelBuilder.Depth));   // low enough to run straight onto (step offset)
            var sb = go.AddComponent<Springboard>();
            sb.height = d.h; sb.forward = d.value; sb.pad = pad;
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.02f, -1.12f), new Vector3(1.6f, 0.04f, 0.02f), glowMat, "Glow");
        }

        // ------------------------------------------------------------------ ziplines
        void Zip(ParkourDef d)
        {
            var a = new Vector3(d.x, d.y, 0f); var b = new Vector3(d.x1, d.y1, 0f);
            var go = Obj("Zipline", Vector3.zero);
            var mb = new MeshBuilder(2);
            const float z = 0.9f;   // the cable runs just behind the player's line
            foreach (var end in new[] { a, b })
            {
                float g = Ground(end.x);
                mb.Tube(new Vector3(end.x, g - 0.2f, z + 0.25f), new Vector3(end.x, end.y + 0.9f, z + 0.25f), 0.12f, 0.1f, 10, 0);   // pole
                mb.BoxC(new Vector3(end.x, end.y + 0.25f, z + 0.1f), new Vector3(0.4f, 0.3f, 0.3f), 0);
            }
            mb.Tube(a + new Vector3(0, 0.22f, z), b + new Vector3(0, 0.22f, z), 0.025f, 0.025f, 5, 1);   // cable
            SceneSpawn.MarkStatic(mb.ToObject("Visual", go.transform, new[] { steel, dark }));
            var trolley = new GameObject("Trolley").transform;
            trolley.SetParent(go.transform, false);
            var tm = new MeshBuilder(2);
            tm.BoxC(new Vector3(0, 0.25f, z), new Vector3(0.3f, 0.18f, 0.2f), 0);
            tm.Tube(new Vector3(0, 0.2f, z), new Vector3(0, -0.05f, 0.2f), 0.02f, 0.02f, 5, 1);
            tm.Tube(new Vector3(-0.3f, -0.05f, 0.2f), new Vector3(0.3f, -0.05f, 0.2f), 0.025f, 0.025f, 6, 0);   // the handle
            tm.ToObject("TrolleyVisual", trolley, new[] { yellow, dark });
            var zl = go.AddComponent<Zipline>();
            zl.a = a; zl.b = b; zl.trolley = trolley;
            trolley.position = a;
        }

        // ------------------------------------------------------------------ swing bars
        void Swing(ParkourDef d)
        {
            var go = Obj("SwingBar", new Vector3(d.x, d.y, 0f));
            float g = Ground(d.x);
            var mb = new MeshBuilder(2);
            bool overPit = g <= PitFloor + 0.1f;
            foreach (float z in new[] { -1.4f, 1.4f })
            {
                if (overPit) mb.Tube(new Vector3(0, 0.05f, z), new Vector3(0, 14f, z), 0.03f, 0.03f, 5, 1);                // hanging chains
                else mb.Tube(new Vector3(0, g - d.y, z), new Vector3(0, 0.1f, z), 0.08f, 0.07f, 8, 0);                     // posts
            }
            SceneSpawn.MarkStatic(mb.ToObject("Frame", go.transform, new[] { steel, dark }));
            var bar = new GameObject("Bar").transform;
            bar.SetParent(go.transform, false);
            var bm = new MeshBuilder(2);
            bm.Tube(new Vector3(0, 0, -1.45f), new Vector3(0, 0, 1.45f), 0.05f, 0.05f, 10, 0);
            bm.Tube(new Vector3(0, 0, -0.45f), new Vector3(0, 0, 0.45f), 0.058f, 0.058f, 10, 1);   // grip tape
            bm.ToObject("BarVisual", bar, new[] { CityMaterials.Solid(new Color(0.75f, 0.77f, 0.8f), 0.25f, 0.9f), yellow });
            PlaceholderFactory.Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0, -0.5f), new Vector3(0.7f, 0.04f, 0.02f), glowMat, "Glow");
            var sw = go.AddComponent<SwingBar>();
            sw.bar = bar;
        }

        // ------------------------------------------------------------------ crumbling slabs
        void Crumble(ParkourDef d)
        {
            var go = Obj("CrumbleSlab", new Vector3(d.x, d.y, 0f));
            Box(go, new Vector3(0, -0.2f, 0), new Vector3(d.w, 0.4f, LevelBuilder.Depth * 0.7f));
            var vis = new GameObject("Visual").transform;
            vis.SetParent(go.transform, false);
            var mb = new MeshBuilder(2);
            // three cracked pieces with gaps between them
            float pw = d.w / 3f;
            for (int i = 0; i < 3; i++)
                mb.BoxC(new Vector3(-d.w / 2f + pw * (i + 0.5f), -0.2f + (float)rnd.NextDouble() * 0.03f, 0), new Vector3(pw - 0.05f, 0.4f, 3.2f), 0);
            mb.BoxC(new Vector3(0, -0.42f, -1.55f), new Vector3(d.w, 0.05f, 0.04f), 1);
            mb.ToObject("Pieces", vis, new[] { Valley ? wood : Bio ? steel : CityMaterials.PBR("Concrete046", new Color(0.62f, 0.6f, 0.56f), 2f), CityMaterials.Emissive(new Color(0.2f, 0.05f, 0.02f), new Color(1.6f, 0.5f, 0.1f)) });
            var c = go.AddComponent<CrumblePlatform>();
            c.visual = vis;
        }

        // ------------------------------------------------------------------ quiz pits
        void Quiz(ParkourDef d)
        {
            var root = Obj("QuizPit", new Vector3(d.x, d.y, 0f));
            var gate = root.AddComponent<AnswerGate>();
            gate.topic = d.topic;
            float[] xs = { 3f, 7.5f, 12f };
            for (int i = 0; i < xs.Length; i++)
            {
                var pill = new GameObject("Pillar" + i);
                pill.transform.SetParent(root.transform, false);
                pill.transform.localPosition = new Vector3(xs[i], 0f, 0f);
                float h = d.y - PitFloor + 0.5f;
                // 2.6 m wide tops: easy to land on, hard to land on by accident
                Box(pill, new Vector3(0, -h / 2f, 0), new Vector3(2.6f, h, LevelBuilder.Depth));
                var mb = new MeshBuilder(3);
                mb.Box(new Vector3(-1.3f, -h, -1.2f), new Vector3(2.6f, h, 2.4f), 0, 0);
                mb.BoxC(new Vector3(0, -0.05f, -1.21f), new Vector3(2.64f, 0.1f, 0.04f), 1);       // glowing top edge
                mb.BoxC(new Vector3(0, -1.05f, -1.22f), new Vector3(2.4f, 1.5f, 0.02f), 2);         // answer screen
                var vis = mb.ToObject("Visual", pill.transform, new[] { Bio ? steel : concrete, glowMat, holo });
                var lbl = Label(pill.transform, new Vector3(0, -1.05f, -1.26f), "?", 5f, Color.white, new Vector2(2.3f, 1.4f));
                lbl.fontSizeMin = 1.2f;
                gate.pillars.Add(pill.transform);
                gate.labels.Add(lbl);
                pill.AddComponent<AnswerPillar>().gate = gate;
            }
            // the question board floats over the pit
            var board = new GameObject("QuestionBoard").transform;
            board.SetParent(root.transform, false);
            board.localPosition = new Vector3(7.5f, 5.6f, 1.5f);
            var bm = new MeshBuilder(2);
            bm.BoxC(new Vector3(0, 0, 0.05f), new Vector3(11.2f, 3.4f, 0.1f), 0);
            bm.BoxC(new Vector3(0, 1.72f, 0), new Vector3(11.4f, 0.06f, 0.12f), 1);
            bm.BoxC(new Vector3(0, -1.72f, 0), new Vector3(11.4f, 0.06f, 0.12f), 1);
            foreach (float x in new[] { -4.5f, 4.5f }) bm.Tube(new Vector3(x, 1.7f, 0.1f), new Vector3(x, 12f, 0.1f), 0.03f, 0.03f, 5, 1);
            bm.ToObject("Frame", board, new[] { holo, glowMat });
            gate.header = Label(board, new Vector3(0, 1.3f, -0.05f), "QUIZ JUMP", 2.2f, accent * 1.4f, new Vector2(10.6f, 0.5f));
            gate.question = Label(board, new Vector3(0, -0.25f, -0.05f), "", 4.2f, Color.white, new Vector2(10.6f, 2.4f));
            gate.question.fontSizeMin = 1.6f;
            // the firewall: only the right answer brings it down
            var fw = new GameObject("Firewall");
            fw.transform.SetParent(root.transform, false);
            fw.transform.localPosition = new Vector3(15.2f, 0f, 0f);
            gate.firewall = Box(fw, new Vector3(0, 5f, 0), new Vector3(0.6f, 16f, LevelBuilder.Depth));
            var fv = new GameObject("FirewallVisual").transform;
            fv.SetParent(fw.transform, false);
            fv.localPosition = new Vector3(0, -1f, 0);
            var fm = new MeshBuilder(1);
            fm.BoxC(new Vector3(0, 6f, 0), new Vector3(0.25f, 12f, 5f), 0);
            fm.ToObject("Field", fv, new[] { CityMaterials.Emissive(new Color(0.2f, 0.02f, 0.1f), new Color(2.2f, 0.25f, 1.2f)) }, false);
            var fwt = Label(fv, new Vector3(-0.2f, 6f, -2.6f), "F\nI\nR\nE\nW\nA\nL\nL", 3f, new Color(1f, 0.6f, 0.9f), new Vector2(1f, 9f));
            fwt.fontSizeMin = 1f;
            gate.firewallVisual = fv;
            gate.landing = new Vector2(d.x + LevelPlan.QuizWidth + 2.5f, plan.GroundY(d.x + LevelPlan.QuizWidth + 2.5f));
            // pull the camera back so the whole pit and the question fit on screen
            var cam = new GameObject("QuizCamera");
            cam.transform.SetParent(root.transform, false);
            cam.transform.position = new Vector3(d.x + LevelPlan.QuizWidth / 2f, 10f, 0f);
            var cb = cam.AddComponent<BoxCollider>();
            cb.isTrigger = true; cb.size = new Vector3(LevelPlan.QuizWidth + 16f, 60f, 6f);
            var cz = cam.AddComponent<CameraZone>();
            cz.distance = 21f; cz.fov = 42f; cz.heightOffset = 1.6f;
            // the pit floor sends a fall back to the ledge
            var pit = new GameObject("QuizPitRespawn");
            pit.transform.SetParent(root.transform, false);
            pit.transform.position = new Vector3(d.x + LevelPlan.QuizWidth / 2f, PitFloor + 0.8f, 0);
            var pb = pit.AddComponent<BoxCollider>();
            pb.isTrigger = true; pb.size = new Vector3(LevelPlan.QuizWidth + 1f, 1.6f, LevelBuilder.Depth);
            var hz = pit.AddComponent<Hazard>();
            hz.respawn = true;
        }

        // ------------------------------------------------------------------ the timed run
        void Run(RunDef r)
        {
            var go = Obj("ParkourRun", new Vector3(r.start, 0, 0));
            var pr = go.AddComponent<ParkourRun>();
            pr.levelId = plan.levelId; pr.startX = r.start; pr.finishX = r.finish;
            pr.gold = r.gold; pr.silver = r.silver; pr.bronze = r.bronze;
            Arch(r.start, "START", UIFactory.Theme.green);
            Arch(r.finish, "FINISH", UIFactory.Theme.yellow);
        }

        void Arch(float x, string text, Color c)
        {
            float y = plan.GroundY(x);
            var go = Obj("Arch_" + text, new Vector3(x, y, 0));
            // a banner on two posts just behind the running line, and a glowing line across the ground
            var mb = new MeshBuilder(2);
            foreach (float x0 in new[] { -2.4f, 2.4f }) mb.BoxC(new Vector3(x0, 2.3f, 1.6f), new Vector3(0.22f, 4.6f, 0.22f), 0);
            mb.BoxC(new Vector3(0, 4.3f, 1.6f), new Vector3(5.2f, 1.0f, 0.16f), 0);
            mb.BoxC(new Vector3(0, 4.3f, 1.5f), new Vector3(5.0f, 0.82f, 0.02f), 1);
            mb.BoxC(new Vector3(0, 0.012f, 0), new Vector3(0.16f, 0.02f, 5.2f), 1);
            SceneSpawn.MarkStatic(mb.ToObject("Visual", go.transform, new[] { CityMaterials.Solid(new Color(0.1f, 0.11f, 0.13f), 0.4f, 0.6f), CityMaterials.Emissive(c * 0.2f, c * 1.6f) }));
            var t = Label(go.transform, new Vector3(0, 4.3f, 1.46f), text, 4f, new Color(0.05f, 0.05f, 0.08f), new Vector2(4.8f, 0.8f));
            t.fontSizeMin = 1f;
        }
    }
}
