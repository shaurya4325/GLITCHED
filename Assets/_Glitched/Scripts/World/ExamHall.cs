using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The Examination Hall, Kanha the Dard's arena: a real school exam hall, late afternoon. Wooden floor, rows of
    /// exam desks with question papers on them, tall windows with sun shafts, ceiling fans, tube lights, a stage with
    /// the invigilator's desk and a blackboard that reads FINAL EXAMINATION. The Glitch has got in: a few papers
    /// float, the clock runs far too fast, a tube light stutters. Built in code (like the bedroom).
    /// Gameplay runs along x on the z = 0 line; the camera looks in +z, so the open side faces it.
    /// </summary>
    public static class ExamHall
    {
        public class Hall
        {
            public Transform root;
            public Vector3 playerSpot, kanhaSpot, stageTop;
            public readonly List<Transform> papers = new List<Transform>();
            public readonly List<Light> tubeLights = new List<Light>();
            public Transform clockHand, clockHandFast;
            public readonly List<Transform> fans = new List<Transform>();
            public readonly List<Vector3> throwFrom = new List<Vector3>();   // where flying question papers come from
        }

        const float X0 = -16f, X1 = 44f, Z0 = -8f, Z1 = 16f, H = 9f, StageX = 23f, StageH = 0.6f;

        public static Hall Build()
        {
            var hall = new Hall { root = new GameObject("ExamHall").transform };
            var rnd = new System.Random(2027);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var floor = CityMaterials.PBR("WoodFloor064", new Color(0.78f, 0.6f, 0.45f), 2.2f, 0.55f, 0f, 0.4f);
            var plaster = CityMaterials.PBR("Plaster001", new Color(0.74f, 0.9f, 0.82f),   // classic mint-green school walls
                3f, 0.95f, 0f, 0.4f);
            var panel = CityMaterials.PBR("Wood066", new Color(0.42f, 0.27f, 0.17f), 1.6f, 0.6f, 0f, 0.4f);
            var deskWood = CityMaterials.PBR("Wood066", new Color(0.72f, 0.52f, 0.34f), 1.2f, 0.5f, 0f, 0.4f);
            var steel = CityMaterials.Solid(new Color(0.18f, 0.2f, 0.22f), 0.4f, 0.8f, 0.4f);
            var paper = CityMaterials.Solid(new Color(0.95f, 0.95f, 0.92f), 0.85f, 0f, 0.3f);
            var board = CityMaterials.Solid(new Color(0.08f, 0.15f, 0.11f), 0.9f, 0f, 0.6f);
            var curtain = CityMaterials.PBR("Fabric081C", new Color(0.62f, 0.06f, 0.1f), 1.2f, 0.9f, 0f, 0.5f);
            var sky = CityMaterials.Emissive(new Color(0.7f, 0.85f, 1f), new Color(1.5f, 2.2f, 3.1f));   // blue sky outside, warm sun inside
            var tube = CityMaterials.Emissive(new Color(0.95f, 0.97f, 1f), new Color(2.4f, 2.5f, 2.6f));
            var glass = CityMaterials.Glass(new Color(0.8f, 0.85f, 0.9f));

            // ---- shell: floor, stage, walls with windows, ceiling
            var shell = new MeshBuilder(6);
            shell.Box(new Vector3(X0, -0.5f, Z0), new Vector3(X1 - X0, 0.5f, Z1 - Z0), 0, 0);                                          // floor
            shell.Box(new Vector3(StageX, 0f, -3f), new Vector3(X1 - StageX, StageH, Z1 + 3f), 2, 2);                                  // stage
            shell.Box(new Vector3(StageX - 0.9f, 0f, -3f), new Vector3(0.9f, StageH * 0.5f, 10f), 2, 2);                                // step
            shell.Box(new Vector3(X0, H, Z0), new Vector3(X1 - X0, 0.3f, Z1 - Z0), 1, 1);                                              // ceiling
            // back wall: wooden wainscot, then plaster with tall window openings
            shell.Box(new Vector3(X0, 0f, Z1), new Vector3(X1 - X0, 1.2f, 0.3f), 2, 2);
            var windowsX = new List<float>();
            for (float x = X0 + 3f; x < StageX - 2f; x += 6f) windowsX.Add(x);
            float wx0 = X0;
            foreach (var x in windowsX)
            {
                shell.Box(new Vector3(wx0, 1.2f, Z1), new Vector3(x - wx0, H - 1.2f, 0.3f), 1, 1);
                shell.Box(new Vector3(x, 1.2f, Z1), new Vector3(2.6f, 1.0f, 0.3f), 1, 1);                // sill
                shell.Box(new Vector3(x, 6.6f, Z1), new Vector3(2.6f, H - 6.6f, 0.3f), 1, 1);           // lintel
                wx0 = x + 2.6f;
                hall.throwFrom.Add(new Vector3(x + 1.3f, 4.5f, Z1 - 0.5f));
            }
            shell.Box(new Vector3(wx0, 1.2f, Z1), new Vector3(X1 - wx0, H - 1.2f, 0.3f), 1, 1);
            shell.Box(new Vector3(X0 - 0.3f, 0f, Z0), new Vector3(0.3f, H, Z1 - Z0), 1, 1);                // side walls
            shell.Box(new Vector3(X1, 0f, Z0), new Vector3(0.3f, H, Z1 - Z0), 1, 1);
            // pillars between the windows
            foreach (var x in windowsX) shell.BoxC(new Vector3(x - 1.7f, H * 0.5f, Z1 - 0.3f), new Vector3(0.6f, H, 0.6f), 1);
            var shellGo = shell.ToObject("Shell", hall.root, new[] { floor, plaster, panel, steel, curtain, board });
            SceneSpawn.MarkStatic(shellGo);

            // windows: glass, bright late-afternoon sky behind, frames
            var win = new MeshBuilder(3);
            foreach (var x in windowsX)
            {
                win.BoxC(new Vector3(x + 1.3f, 3.9f, Z1 + 0.8f), new Vector3(2.6f, 5.4f, 0.05f), 0);              // sky glow
                win.BoxC(new Vector3(x + 1.3f, 3.9f, Z1 + 0.15f), new Vector3(2.6f, 5.4f, 0.02f), 1);             // glass
                win.BoxC(new Vector3(x + 1.3f, 3.9f, Z1 + 0.1f), new Vector3(0.08f, 5.4f, 0.1f), 2);               // mullion
                for (float y = 2.2f; y < 6.6f; y += 1.1f) win.BoxC(new Vector3(x + 1.3f, y, Z1 + 0.1f), new Vector3(2.6f, 0.06f, 0.1f), 2);
            }
            win.ToObject("Windows", hall.root, new[] { sky, glass, steel }, false);

            // ---- stage dressing: curtains, blackboard, clock, invigilator's desk
            var st = new MeshBuilder(4);
            st.BoxC(new Vector3(StageX + 0.6f, H * 0.5f, Z1 - 1.5f), new Vector3(1.2f, H, 0.4f), 0);
            st.BoxC(new Vector3(X1 - 0.6f, H * 0.5f, Z1 - 1.5f), new Vector3(1.2f, H, 0.4f), 0);
            st.BoxC(new Vector3((StageX + X1) / 2f, 3.6f, Z1 - 0.12f), new Vector3(11f, 3.4f, 0.1f), 1);                                 // blackboard
            st.BoxC(new Vector3((StageX + X1) / 2f, 3.6f, Z1 - 0.1f), new Vector3(11.3f, 3.7f, 0.06f), 2);                               // frame
            st.BoxC(new Vector3(36f, StageH + 0.8f, 6f), new Vector3(2.4f, 0.08f, 1.0f), 3);                                             // desk top
            st.BoxC(new Vector3(36f, StageH + 0.4f, 6.45f), new Vector3(2.4f, 0.8f, 0.06f), 3);                                          // modesty panel
            foreach (var dx in new[] { -1.1f, 1.1f }) foreach (var dz in new[] { -0.4f, 0.4f })
                    st.BoxC(new Vector3(36f + dx, StageH + 0.4f, 6f + dz), new Vector3(0.06f, 0.8f, 0.06f), 2);
            st.ToObject("Stage", hall.root, new[] { curtain, board, panel, deskWood });
            var title = MakeText(hall.root, "FINAL EXAMINATION", new Vector3((StageX + X1) / 2f, 4.6f, Z1 - 0.2f), 1.1f, new Color(0.95f, 0.95f, 0.9f));
            MakeText(hall.root, "SCIENCE  ·  PHYSICS  ·  CHEMISTRY  ·  BIOLOGY", new Vector3((StageX + X1) / 2f, 3.7f, Z1 - 0.2f), 0.48f, new Color(0.9f, 0.92f, 0.85f));
            MakeText(hall.root, "Time: 3 hrs        Max. Marks: 12        Pass: 8", new Vector3((StageX + X1) / 2f, 2.9f, Z1 - 0.2f), 0.4f, new Color(0.85f, 0.88f, 0.8f));
            MakeText(hall.root, "GLITCHED BOARD OF SECONDARY EDUCATION", new Vector3(10f, 7.6f, Z1 - 0.2f), 0.55f, new Color(0.35f, 0.22f, 0.14f));
            // clock above the board: the minute hand races (the Glitch has got into time)
            var clock = new GameObject("Clock").transform; clock.SetParent(hall.root, false); clock.localPosition = new Vector3(18f, 6.4f, Z1 - 0.2f);
            var cm = new MeshBuilder(2);
            cm.Cylinder(Vector3.zero, 0.55f, 0.55f, 0.08f, 32, 0); cm.Cylinder(new Vector3(0, 0, 0), 0.58f, 0.58f, 0.06f, 32, 1);
            var face = cm.ToObject("Face", clock, new[] { paper, steel });
            face.transform.localRotation = Quaternion.Euler(90, 0, 0);
            hall.clockHand = Hand(clock, 0.32f, 0.05f, steel);
            hall.clockHandFast = Hand(clock, 0.48f, 0.025f, steel);

            // ---- exam desks in rows behind the walking line, each with a question paper (some float)
            var desks = new MeshBuilder(3);
            for (float z = 3f; z < Z1 - 1.5f; z += 2.6f)
                for (float x = X0 + 2f; x < StageX - 1.5f; x += 2.7f)
                {
                    float jx = x + R(-0.1f, 0.1f);
                    desks.BoxC(new Vector3(jx, 0.76f, z), new Vector3(1.1f, 0.05f, 0.62f), 0);
                    foreach (var dx in new[] { -0.5f, 0.5f }) foreach (var dz in new[] { -0.26f, 0.26f })
                            desks.BoxC(new Vector3(jx + dx, 0.37f, z + dz), new Vector3(0.04f, 0.74f, 0.04f), 1);
                    // chair behind the desk
                    desks.BoxC(new Vector3(jx, 0.45f, z + 0.65f), new Vector3(0.44f, 0.04f, 0.42f), 0);
                    desks.BoxC(new Vector3(jx, 0.75f, z + 0.86f), new Vector3(0.44f, 0.5f, 0.03f), 0);
                    foreach (var dx in new[] { -0.2f, 0.2f }) foreach (var dz in new[] { 0.46f, 0.84f })
                            desks.BoxC(new Vector3(jx + dx, 0.22f, z + dz), new Vector3(0.03f, 0.45f, 0.03f), 1);
                    if (rnd.NextDouble() < 0.82)
                    {
                        bool floats = rnd.NextDouble() < 0.18;
                        var p = Paper(hall.root, paper, new Vector3(jx + R(-0.2f, 0.2f), 0.79f, z + R(-0.1f, 0.1f)), R(-25f, 25f));
                        if (floats) { hall.papers.Add(p); p.gameObject.AddComponent<FloatingPaper>().Init(R(0.4f, 1.6f), R(0f, 6f)); }
                    }
                }
            SceneSpawn.MarkStatic(desks.ToObject("Desks", hall.root, new[] { deskWood, steel, paper }));

            // ---- ceiling: tube lights and fans
            var lights = new MeshBuilder(2);
            for (float x = X0 + 5f; x < X1 - 2f; x += 9f)
                for (float z = 1f; z < Z1; z += 7f)
                {
                    lights.BoxC(new Vector3(x, H - 0.12f, z), new Vector3(1.3f, 0.06f, 0.12f), 0);
                    lights.BoxC(new Vector3(x, H - 0.06f, z), new Vector3(1.4f, 0.04f, 0.18f), 1);
                    // every tube is a light: they are baked into the hall lightmap (ExamHallBake), so there is no realtime light limit
                    {
                        var lgo = new GameObject("Tube"); lgo.transform.SetParent(hall.root, false); lgo.transform.localPosition = new Vector3(x, H - 0.6f, z);
                        var l = lgo.AddComponent<Light>(); l.type = LightType.Point; l.range = 16f; l.intensity = TubeIntensity * (z < 5f ? 1f : 0.7f); l.color = new Color(0.92f, 0.96f, 1f); l.shadows = LightShadows.None;
                        hall.tubeLights.Add(l);
                    }
                }
            lights.ToObject("TubeLights", hall.root, new[] { tube, steel }, false);
            for (float x = X0 + 9f; x < StageX; x += 11f)
            {
                var fan = new GameObject("Fan").transform; fan.SetParent(hall.root, false); fan.localPosition = new Vector3(x, H - 1.1f, 6f);
                var fm = new MeshBuilder(1);
                fm.Cylinder(new Vector3(0, 0, 0), 0.15f, 0.15f, 0.18f, 12, 0); fm.Tube(new Vector3(0, 0.18f, 0), new Vector3(0, 1.1f, 0), 0.025f, 0.025f, 6, 0);
                for (int i = 0; i < 3; i++) { float a = i * 120f * Mathf.Deg2Rad; fm.BoxC(new Vector3(Mathf.Cos(a) * 0.65f, 0.05f, Mathf.Sin(a) * 0.65f), new Vector3(i == 0 ? 1.1f : 0.16f, 0.02f, i == 0 ? 0.16f : 1.1f), 0); }
                fm.ToObject("FanMesh", fan, new[] { steel });
                hall.fans.Add(fan);
            }

            // ---- light: low sun through the windows, warm fill, cool tubes
            var sunGo = new GameObject("Sun"); sunGo.transform.SetParent(hall.root, false);
            var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, 0.82f, 0.6f); sun.intensity = 2.2f; sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(32f, 200f, 0f);   // from behind the windows, down into the hall
            RenderSettings.sun = sun;
            // stage lights: warm pools over the stage so Kanha stands in the light (point lights: inverse-square, ~4 m up)
            foreach (var sp in new[] { new Vector3(27.5f, 4.8f, -1.5f), new Vector3(27.5f, 4.8f, 3f), new Vector3(35f, 4.8f, 0.5f) })
            {
                var spGo = new GameObject("StageLight"); spGo.transform.SetParent(hall.root, false);
                spGo.transform.localPosition = sp;
                var sl = spGo.AddComponent<Light>(); sl.type = LightType.Point; sl.color = new Color(1f, 0.86f, 0.66f);
                sl.intensity = 5f; sl.range = 13f; sl.shadows = LightShadows.None;
            }
            Light(hall);
            // sun shafts: soft additive slabs from each window down to the floor
            var shaft = CityMaterials.Emissive(new Color(0, 0, 0), new Color(0.34f, 0.26f, 0.15f));
            var sh = new MeshBuilder(1);
            foreach (var x in windowsX)
                sh.Card(new Vector3(x + 1.3f, 2.6f, Z1 - 3.5f), new Vector3(1.3f, 0, 0), new Vector3(0, 3.3f, 3.2f), 0, Vector3.back, new Rect(0, 0, 1, 1));
            var shafts = sh.ToObject("SunShafts", hall.root, new[] { shaft }, false);
            foreach (var r in shafts.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ---- colliders: floor, stage, step, walls the player cannot leave through
            Box(hall.root, "FloorCol", new Vector3((X0 + X1) / 2f, -0.5f, 4f), new Vector3(X1 - X0, 1f, 30f));
            Box(hall.root, "StageCol", new Vector3((StageX + X1) / 2f, StageH / 2f, 4f), new Vector3(X1 - StageX, StageH, 30f));
            Box(hall.root, "StepCol", new Vector3(StageX - 0.45f, StageH * 0.25f, 4f), new Vector3(0.9f, StageH * 0.5f, 30f));
            Box(hall.root, "WallL", new Vector3(X0 + 0.5f, 5f, 0f), new Vector3(1f, 12f, 30f));
            Box(hall.root, "WallR", new Vector3(X1 - 0.5f, 5f, 0f), new Vector3(1f, 12f, 30f));

            hall.playerSpot = new Vector3(15f, 0.05f, 0f);
            hall.kanhaSpot = new Vector3(31f, StageH, 0f);
            hall.stageTop = new Vector3(31f, StageH, 0f);
            hall.throwFrom.Add(new Vector3(12f, H - 0.6f, 4f));     // from the ceiling
            hall.throwFrom.Add(new Vector3(X0 + 1f, 3f, 2f));        // from the far wall
            var refs = hall.root.gameObject.AddComponent<ExamHallRefs>();
            refs.papers = hall.papers; refs.tubeLights = hall.tubeLights; refs.fans = hall.fans; refs.throwFrom = hall.throwFrom;
            refs.clockHand = hall.clockHand; refs.clockHandFast = hall.clockHandFast;
            refs.playerSpot = hall.playerSpot; refs.kanhaSpot = hall.kanhaSpot; refs.stageTop = hall.stageTop;
            return hall;
        }

        /// <summary>The hall already in the scene (built and light-baked in the editor: ExamHallBake), or null.</summary>
        public static Hall Find()
        {
            var r = Object.FindFirstObjectByType<ExamHallRefs>();
            if (r == null) return null;
            var hall = new Hall { root = r.transform, clockHand = r.clockHand, clockHandFast = r.clockHandFast, playerSpot = r.playerSpot, kanhaSpot = r.kanhaSpot, stageTop = r.stageTop };
            hall.papers.AddRange(r.papers); hall.tubeLights.AddRange(r.tubeLights); hall.fans.AddRange(r.fans); hall.throwFrom.AddRange(r.throwFrom);
            return hall;
        }

        /// <summary>The hall's bounds, for the editor bake (light probes, reflection probe).</summary>
        /// <summary>Tube light strength: URP lights fall off with distance squared, and the tubes hang 8 m above the floor.</summary>
        public const float TubeIntensity = 7f;

        public static Bounds Interior => new Bounds(new Vector3((X0 + X1) / 2f, H / 2f, (Z0 + Z1) / 2f), new Vector3(X1 - X0, H, Z1 - Z0));

        /// <summary>The hall's light mood (call again after anything that resets RenderSettings, e.g. the corruption setup).</summary>
        public static void Light(Hall hall)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.76f, 0.9f);   // cool sky fill against the warm sunbeams
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.56f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.22f, 0.16f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(0.62f, 0.52f, 0.42f);
            RenderSettings.fogStartDistance = 30f; RenderSettings.fogEndDistance = 90f;
        }

        static Transform Paper(Transform parent, Material m, Vector3 pos, float yaw)
        {
            var mb = new MeshBuilder(1);
            mb.BoxC(Vector3.zero, new Vector3(0.21f, 0.004f, 0.297f), 0);   // an A4 sheet
            var go = mb.ToObject("Paper", parent, new[] { m }, false);
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return go.transform;
        }

        static Transform Hand(Transform clock, float len, float w, Material m)
        {
            var piv = new GameObject("Hand").transform; piv.SetParent(clock, false); piv.localPosition = new Vector3(0, 0, -0.06f);
            var mb = new MeshBuilder(1); mb.BoxC(new Vector3(0, len * 0.45f, 0), new Vector3(w, len, 0.01f), 0);
            mb.ToObject("HandMesh", piv, new[] { m }, false);
            return piv;
        }

        static void Box(Transform parent, string name, Vector3 c, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = c;
            go.AddComponent<BoxCollider>().size = size;
        }

        static TextMeshPro MakeText(Transform parent, string text, Vector3 pos, float size, Color color)
        {
            var go = new GameObject("Chalk"); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var t = go.AddComponent<TextMeshPro>();
            t.text = text; t.fontSize = size * 10f; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(12f, 1.5f);
            t.enableWordWrapping = false;
            if (UIFactory.Theme != null && UIFactory.Theme.bodyFont != null) t.font = UIFactory.Theme.bodyFont;
            return t;
        }
    }

}
