using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Builds Shaurya's bedroom (the real world) for the opening and ending cinematics, all generated in code with real
    /// materials: wooden floor, plaster walls with a skirting board, a window with a night sky, a desk with a monitor,
    /// an office chair, a bed with a duvet, a bookshelf, posters, a rug and a desk lamp.
    /// The cutscenes use: monitor, screen (its _BaseMap / _BaseColor are swapped), chair, seat, lamp, floaters.
    /// </summary>
    public static class RoomBuilder
    {
        public class Room
        {
            public Transform root, monitor, chair, seat;
            /// <summary>Where a seated character's root goes: below the seat, on the floor (sitting clips lift the hips onto the seat).</summary>
            public Vector3 SitRoot => new Vector3(seat.position.x, chair.position.y, seat.position.z);
            public Renderer screen;
            public MonitorScreenFx screenFx;
            public GameFeed feed;
            public Transform pcTower;
            public readonly List<Light> rgb = new List<Light>();
            public readonly List<Transform> floaters = new List<Transform>();
            public Light lamp;
        }

        static GameObject Obj(MeshBuilder mb, string name, Transform parent, Vector3 pos, params Material[] mats)
        {
            var go = mb.ToObject(name, parent, mats);
            go.transform.localPosition = pos;
            return go;
        }

        public static Room Build(GameDatabase db)
        {
            if (db != null && db.materials != null) CityMaterials.Library = db.materials;
            var room = new Room();
            var root = new GameObject("Bedroom").transform;
            room.root = root;
            var floorM = CityMaterials.PBR("WoodFloor064", new Color(0.9f, 0.82f, 0.72f), 1.8f, 0.8f, 0f, 1f);
            var wallM = CityMaterials.PBR("Plaster001", new Color(0.86f, 0.84f, 0.8f), 2.5f, 1f, 0f, 1f);
            var ceilM = CityMaterials.PBR("Plaster001", new Color(0.95f, 0.95f, 0.94f), 3f, 1f, 0f, 1f);
            var wood = CityMaterials.PBR("Wood066", new Color(0.78f, 0.62f, 0.48f), 1f, 0.8f, 0f, 1f);
            var white = CityMaterials.Solid(new Color(0.92f, 0.92f, 0.9f), 0.5f, 0f, 1f);
            var plastic = CityMaterials.Solid(new Color(0.07f, 0.07f, 0.08f), 0.35f, 0f, 1f);
            var metal = CityMaterials.Solid(new Color(0.55f, 0.56f, 0.58f), 0.3f, 0.9f, 1f);
            var fabricBlue = CityMaterials.PBR("Fabric086", new Color(0.22f, 0.32f, 0.62f), 0.3f, 1f, 0f, 1f, false);
            var duvet = CityMaterials.PBR("Fabric086", new Color(0.72f, 0.3f, 0.32f), 0.4f, 1f, 0f, 1f, false);
            var rugM = CityMaterials.PBR("Fabric081C", new Color(0.55f, 0.52f, 0.48f), 0.6f, 1f, 0f, 1f, false);

            // shell: floor, walls with a window, ceiling, skirting board
            var shell = new MeshBuilder(4);
            shell.Box(new Vector3(-5, -0.1f, -4), new Vector3(10, 0.1f, 8), 0, 0, float.NaN, MeshBuilder.Faces.Top);
            shell.Box(new Vector3(-5, 0, 3.9f), new Vector3(10, 4.1f, 0.2f), 1, 1, float.NaN, MeshBuilder.Faces.Front);
            shell.Box(new Vector3(4.9f, 0, -4), new Vector3(0.2f, 4.1f, 8), 1, 1, float.NaN, MeshBuilder.Faces.Left);
            // left wall with a window opening (four pieces around the hole)
            shell.Box(new Vector3(-5.1f, 0, -4), new Vector3(0.2f, 1.0f, 8), 1, 1, float.NaN, MeshBuilder.Faces.Right);
            shell.Box(new Vector3(-5.1f, 3.0f, -4), new Vector3(0.2f, 1.1f, 8), 1, 1, float.NaN, MeshBuilder.Faces.Right);
            shell.Box(new Vector3(-5.1f, 1.0f, -4), new Vector3(0.2f, 2.0f, 3.2f), 1, 1, float.NaN, MeshBuilder.Faces.Right);
            shell.Box(new Vector3(-5.1f, 1.0f, 1.2f), new Vector3(0.2f, 2.0f, 2.8f), 1, 1, float.NaN, MeshBuilder.Faces.Right);
            shell.Box(new Vector3(-5, 4.0f, -4), new Vector3(10, 0.1f, 8), 2, 2, float.NaN, MeshBuilder.Faces.Bottom);
            shell.BoxC(new Vector3(0, 0.06f, 3.88f), new Vector3(10, 0.12f, 0.03f), 3);
            shell.BoxC(new Vector3(4.88f, 0.06f, 0), new Vector3(0.03f, 0.12f, 8), 3);
            Obj(shell, "RoomShell", root, Vector3.zero, floorM, wallM, ceilM, white);
            // window frame and mullion (the night sky shows through)
            var win = new MeshBuilder(1);
            win.BoxC(new Vector3(-5.0f, 1.0f, -0.4f), new Vector3(0.26f, 0.06f, 2.4f), 0);
            win.BoxC(new Vector3(-5.0f, 3.0f, -0.4f), new Vector3(0.26f, 0.06f, 2.4f), 0);
            win.BoxC(new Vector3(-5.0f, 2.0f, -0.4f), new Vector3(0.12f, 2.0f, 0.05f), 0);
            win.BoxC(new Vector3(-4.9f, 0.98f, -0.4f), new Vector3(0.3f, 0.04f, 2.5f), 0);
            Obj(win, "Window", root, Vector3.zero, white);

            // rug
            var rug = new MeshBuilder(1);
            rug.BoxC(new Vector3(0.2f, 0.01f, 0.8f), new Vector3(3.2f, 0.02f, 2.2f), 0, MeshBuilder.Faces.Top);
            Obj(rug, "Rug", root, Vector3.zero, rugM);

            // gaming desk (black), ultrawide monitor, RGB keyboard, mouse on a big pad, mug
            var deskBlack = CityMaterials.Solid(new Color(0.05f, 0.05f, 0.055f), 0.4f, 0.2f, 1f);
            var desk = new MeshBuilder(1);
            desk.BoxC(new Vector3(1.2f, 0.75f, 3.2f), new Vector3(2.6f, 0.04f, 1.1f), 0);
            foreach (float x in new[] { 0.0f, 2.4f })
            {
                desk.BoxC(new Vector3(x, 0.37f, 3.2f), new Vector3(0.08f, 0.72f, 0.9f), 0);
                desk.BoxC(new Vector3(x, 0.03f, 3.2f), new Vector3(0.12f, 0.06f, 1.0f), 0);
            }
            Obj(desk, "GamingDesk", root, Vector3.zero, deskBlack);
            var pad = new MeshBuilder(1);
            pad.BoxC(new Vector3(1.2f, 0.775f, 2.92f), new Vector3(1.5f, 0.006f, 0.45f), 0);
            Obj(pad, "DeskPad", root, Vector3.zero, CityMaterials.PBR("Fabric081C", new Color(0.12f, 0.12f, 0.13f), 0.3f, 1f, 0f, 1f, false));

            var mon = new GameObject("Monitor").transform; mon.SetParent(root, false); mon.localPosition = new Vector3(1.2f, 1.2f, 3.45f);
            room.monitor = mon;
            var bez = new MeshBuilder(1);
            bez.BoxC(Vector3.zero, new Vector3(1.36f, 0.6f, 0.05f), 0);
            bez.BoxC(new Vector3(0, -0.32f, 0.07f), new Vector3(0.08f, 0.22f, 0.05f), 0);
            bez.BoxC(new Vector3(0, -0.425f, 0.02f), new Vector3(0.38f, 0.015f, 0.24f), 0);
            Obj(bez, "Bezel", mon, Vector3.zero, plastic);
            room.feed = GameFeed.Create(root);
            var screenMat = new Material(Shader.Find("Glitched/MonitorScreen")) { name = "GamingScreen" };
            var scrMb = new MeshBuilder(1);
            scrMb.Quad(new Vector3(-0.66f, -0.28f, -0.027f), new Vector3(0.66f, -0.28f, -0.027f), new Vector3(0.66f, 0.28f, -0.027f), new Vector3(-0.66f, 0.28f, -0.027f),
                       new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), 0);
            var scrGo = scrMb.ToObject("Screen", mon, new[] { screenMat }, false);
            room.screen = scrGo.GetComponent<Renderer>();
            room.screenFx = scrGo.AddComponent<MonitorScreenFx>();
            room.screenFx.Setup(screenMat, room.feed.Texture);
            // the screen lights the room (cool blue glow)
            var glow = new GameObject("ScreenGlow").AddComponent<Light>();
            glow.transform.SetParent(mon, false);
            glow.transform.localPosition = new Vector3(0, 0, -0.4f);
            glow.transform.localRotation = Quaternion.Euler(0, 180, 0);
            glow.type = LightType.Spot; glow.spotAngle = 120f; glow.range = 5f; glow.intensity = 3f; glow.color = new Color(0.55f, 0.7f, 1f);
            room.rgb.Add(glow);

            var kb = new MeshBuilder(2);
            kb.BoxC(Vector3.zero, new Vector3(0.46f, 0.025f, 0.15f), 0);
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 14; c++)
                    kb.BoxC(new Vector3(-0.2f + c * 0.031f, 0.017f, -0.05f + r * 0.032f), new Vector3(0.024f, 0.008f, 0.024f), 1, MeshBuilder.Faces.Top);
            room.floaters.Add(Obj(kb, "Keyboard", root, new Vector3(1.15f, 0.79f, 2.9f), plastic, CityMaterials.Emissive(new Color(0.2f, 0.1f, 0.3f), new Color(1.6f, 0.4f, 2.2f))).transform);
            var mouse = new MeshBuilder(2);
            mouse.Sphere(Vector3.zero, new Vector3(0.035f, 0.02f, 0.06f), 6, 10, 0);
            mouse.BoxC(new Vector3(0, 0.0f, -0.055f), new Vector3(0.03f, 0.004f, 0.01f), 1);
            room.floaters.Add(Obj(mouse, "Mouse", root, new Vector3(1.62f, 0.8f, 2.9f), plastic, CityMaterials.Emissive(new Color(0.1f, 0.3f, 0.4f), new Color(0.5f, 2.4f, 3f))).transform);
            var mug = new MeshBuilder(1);
            mug.Cylinder(Vector3.zero, 0.045f, 0.045f, 0.1f, 14, 0);
            room.floaters.Add(Obj(mug, "Mug", root, new Vector3(2.15f, 0.78f, 3.05f), white).transform);

            // PC tower beside the desk: tempered glass side, three spinning RGB fans, a glowing interior
            var pc = new GameObject("PCTower").transform; pc.SetParent(root, false); pc.localPosition = new Vector3(2.95f, 0f, 3.15f);
            room.pcTower = pc;
            var tower = new MeshBuilder(3);
            tower.BoxC(new Vector3(0, 0.25f, 0), new Vector3(0.24f, 0.5f, 0.48f), 0, MeshBuilder.Faces.All & ~MeshBuilder.Faces.Left);
            tower.BoxC(new Vector3(-0.121f, 0.25f, 0), new Vector3(0.005f, 0.48f, 0.46f), 1);
            tower.BoxC(new Vector3(0.0f, 0.18f, 0.02f), new Vector3(0.12f, 0.14f, 0.24f), 2);
            Obj(tower, "Case", pc, Vector3.zero, plastic, CityMaterials.Glass(new Color(0.3f, 0.32f, 0.36f)), CityMaterials.Solid(new Color(0.15f, 0.15f, 0.17f), 0.4f, 0.7f, 1f));
            for (int i = 0; i < 3; i++)
            {
                var fan = new GameObject("Fan" + i).transform;
                fan.SetParent(pc, false);
                fan.localPosition = new Vector3(0, 0.1f + i * 0.14f, -0.235f);
                var fm = new MeshBuilder(2);
                for (int b = 0; b < 7; b++) { float a = b / 7f * Mathf.PI * 2f; fm.BoxC(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.03f, new Vector3(0.035f, 0.012f, 0.004f), 0); }
                fm.Tube(new Vector3(0, 0, -0.003f), new Vector3(0, 0, 0.003f), 0.06f, 0.06f, 20, 1);
                Obj(fm, "FanBlades", fan, Vector3.zero, plastic, CityMaterials.Emissive(new Color(0.3f, 0.1f, 0.4f), i == 1 ? new Color(0.6f, 2.6f, 3.2f) : new Color(3f, 0.6f, 2.4f)));
                fan.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 0, 420f);
            }
            var inside = new GameObject("CaseGlow").AddComponent<Light>();
            inside.transform.SetParent(pc, false);
            inside.transform.localPosition = new Vector3(-0.3f, 0.3f, 0);
            inside.type = LightType.Point; inside.range = 1.6f; inside.intensity = 1.4f; inside.color = new Color(0.9f, 0.35f, 1f);
            room.rgb.Add(inside);

            // RGB LED strip behind the monitor and under the desk edge
            var strip = new MeshBuilder(1);
            strip.BoxC(new Vector3(1.2f, 1.0f, 3.86f), new Vector3(2.4f, 0.02f, 0.02f), 0);
            strip.BoxC(new Vector3(1.2f, 0.72f, 2.66f), new Vector3(2.5f, 0.01f, 0.01f), 0);
            Obj(strip, "LedStrip", root, Vector3.zero, CityMaterials.Emissive(new Color(0.2f, 0.4f, 0.5f), new Color(0.8f, 2.2f, 3.2f)));
            foreach (var (pos, c) in new[] { (new Vector3(0.4f, 1.0f, 3.75f), new Color(0.3f, 0.7f, 1f)), (new Vector3(2.0f, 1.0f, 3.75f), new Color(1f, 0.3f, 0.85f)) })
            {
                var l = new GameObject("RGB").AddComponent<Light>();
                l.transform.SetParent(root, false); l.transform.localPosition = pos;
                l.type = LightType.Point; l.range = 2.6f; l.intensity = 1.6f; l.color = c;
                room.rgb.Add(l);
            }

            // racing-style gaming chair: high back with side wings, black with red trim
            var chair = new GameObject("Chair").transform; chair.SetParent(root, false); chair.localPosition = new Vector3(1.2f, 0, 2.05f);
            room.chair = chair;
            var red = CityMaterials.Solid(new Color(0.62f, 0.08f, 0.07f), 0.42f, 0f, 1f);
            var blackLeather = CityMaterials.Solid(new Color(0.045f, 0.045f, 0.05f), 0.38f, 0f, 1f);   // smooth faux leather
            var ch = new MeshBuilder(3);
            ch.BoxC(new Vector3(0, 0.49f, 0.02f), new Vector3(0.56f, 0.1f, 0.54f), 0);
            foreach (float x in new[] { -0.27f, 0.27f }) ch.BoxC(new Vector3(x, 0.56f, 0.02f), new Vector3(0.06f, 0.08f, 0.5f), 1);
            ch.BoxC(new Vector3(0, 1.08f, -0.27f), new Vector3(0.5f, 1.05f, 0.1f), 0);
            foreach (float x in new[] { -0.24f, 0.24f }) ch.BoxC(new Vector3(x, 1.0f, -0.23f), new Vector3(0.07f, 0.8f, 0.12f), 1);
            ch.BoxC(new Vector3(0, 1.48f, -0.24f), new Vector3(0.28f, 0.14f, 0.06f), 1);
            foreach (float x in new[] { -0.3f, 0.3f }) { ch.BoxC(new Vector3(x, 0.72f, 0.0f), new Vector3(0.05f, 0.03f, 0.3f), 2); ch.Tube(new Vector3(x, 0.52f, 0.0f), new Vector3(x, 0.72f, 0.0f), 0.015f, 0.015f, 6, 2); }
            ch.Tube(new Vector3(0, 0.08f, 0), new Vector3(0, 0.44f, 0), 0.03f, 0.03f, 10, 2);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.32f;
                ch.Tube(new Vector3(0, 0.08f, 0), d + Vector3.up * 0.06f, 0.02f, 0.02f, 6, 2);
                ch.Sphere(d + Vector3.up * 0.03f, Vector3.one * 0.03f, 4, 6, 2);
            }
            Obj(ch, "ChairBody", chair, Vector3.zero, blackLeather, red, plastic);
            room.seat = new GameObject("SeatPoint").transform; room.seat.SetParent(chair, false); room.seat.localPosition = new Vector3(0, 0.42f, 0.05f);
            room.floaters.Add(chair);

            // bed: frame, mattress, duvet, pillow
            var bed = new MeshBuilder(3);
            bed.BoxC(new Vector3(0, 0.15f, 0), new Vector3(2.1f, 0.3f, 3.4f), 0);
            bed.BoxC(new Vector3(0, 0.6f, 1.68f), new Vector3(2.1f, 0.9f, 0.06f), 0);
            bed.BoxC(new Vector3(0, 0.4f, 0.05f), new Vector3(1.95f, 0.22f, 3.25f), 1);
            bed.Sphere(new Vector3(0, 0.54f, -0.25f), new Vector3(1.02f, 0.1f, 1.45f), 6, 14, 2, 0.06f, 3);
            bed.Sphere(new Vector3(0, 0.58f, 1.3f), new Vector3(0.6f, 0.08f, 0.22f), 5, 12, 1);
            room.floaters.Add(Obj(bed, "Bed", root, new Vector3(-3.3f, 0, 1.5f), wood, white, duvet).transform);

            // shelf with books, posters
            var shelf = new MeshBuilder(1);
            shelf.BoxC(new Vector3(3.6f, 2.2f, 3.75f), new Vector3(1.6f, 0.04f, 0.3f), 0);
            Obj(shelf, "Shelf", root, Vector3.zero, wood);
            Color[] spines = { new Color(0.55f, 0.12f, 0.1f), new Color(0.12f, 0.25f, 0.45f), new Color(0.85f, 0.75f, 0.3f), new Color(0.2f, 0.4f, 0.25f), new Color(0.9f, 0.9f, 0.88f), new Color(0.3f, 0.3f, 0.32f) };
            for (int i = 0; i < 6; i++)
            {
                var b = new MeshBuilder(1);
                float h = 0.24f + (i % 3) * 0.04f;
                b.BoxC(new Vector3(0, h / 2, 0), new Vector3(0.05f + (i % 2) * 0.02f, h, 0.2f), 0);
                room.floaters.Add(Obj(b, "Book" + i, root, new Vector3(3.0f + i * 0.09f, 2.22f, 3.75f), CityMaterials.PBR("Fabric086", spines[i], 0.2f, 1f, 0f, 1f, false)).transform);
            }
            foreach (var (x, tex) in new[] { (-1.6f, "equations"), (-0.3f, "periodic") })
            {
                var poster = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), root, new Vector3(x, 2.7f, 3.87f), new Vector3(1.0f, 0.7f, 1), CityMaterials.Screen(LevelBuilder.Holo(tex), new Color(0.35f, 0.35f, 0.35f)), "Poster");
                room.floaters.Add(poster);
            }

            // desk lamp (warm light)
            var lampObj = new MeshBuilder(2);
            lampObj.Cylinder(Vector3.zero, 0.08f, 0.08f, 0.02f, 14, 0);
            lampObj.Tube(new Vector3(0, 0.02f, 0), new Vector3(0.05f, 0.3f, 0), 0.01f, 0.01f, 6, 0);
            lampObj.Tube(new Vector3(0.05f, 0.3f, 0), new Vector3(0.18f, 0.42f, -0.05f), 0.01f, 0.01f, 6, 0);
            lampObj.Cylinder(new Vector3(0.18f, 0.36f, -0.05f), 0.07f, 0.035f, 0.08f, 14, 0);
            lampObj.Disc(new Vector3(0.18f, 0.36f, -0.05f), 0.065f, 14, 1, false);
            room.floaters.Add(Obj(lampObj, "DeskLampBody", root, new Vector3(0.35f, 0.78f, 3.3f), metal, CityMaterials.Emissive(Color.white, new Color(4f, 3.4f, 2.4f))).transform);
            var lampGo = new GameObject("DeskLamp");
            lampGo.transform.SetParent(root, false);
            lampGo.transform.localPosition = new Vector3(0.5f, 1.05f, 3.2f);
            room.lamp = lampGo.AddComponent<Light>();
            room.lamp.type = LightType.Point; room.lamp.range = 5; room.lamp.intensity = 0.9f; room.lamp.color = new Color(1f, 0.85f, 0.6f);
            room.lamp.shadows = LightShadows.Soft;
            // moonlight through the window
            var sun = new GameObject("WindowLight").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional; sun.intensity = 0.45f; sun.color = new Color(0.62f, 0.7f, 1f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(30, 80, 0);
            RenderSettings.sun = sun;
            return room;
        }
    }
}
