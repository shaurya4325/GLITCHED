using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Original machine enemies for the realistic city, generated in code (faces +Z, feet / base at y = 0):
    ///   Drone    - Loop Drone: a quadcopter with a red camera eye, circling forever
    ///   Bug      - Bug Monster: a robotic beetle with six walking legs
    ///   Slime    - Syntax Slime: a blob of corrupted liquid metal
    ///   Delivery - Off-By-One: a six-wheeled delivery robot that is always one step off
    /// </summary>
    public static class MachineGen
    {
        public enum Kind { Drone, Bug, Slime, Delivery }

        static Material White => CityMaterials.PBR("Plastic006", new Color(0.88f, 0.88f, 0.86f), 0.4f, 0.6f, 0f, 0.8f, false);
        static Material Dark => CityMaterials.PBR("Metal009", new Color(0.14f, 0.15f, 0.16f), 0.4f, 0.55f, 0.85f, 0.8f, false);
        static Material Rubber => CityMaterials.PBR("Rubber004", new Color(0.15f, 0.15f, 0.15f), 0.3f, 1f, 0f, 0.8f, false);
        static Material Chrome => CityMaterials.Solid(new Color(0.55f, 0.57f, 0.6f), 0.12f, 1f, 0.8f);
        static Material Eye(Color c) => CityMaterials.Emissive(c * 0.4f, c * 4f);

        public static GameObject Build(Kind kind, Transform parent = null)
        {
            var root = new GameObject("Machine_" + kind);
            if (parent != null) root.transform.SetParent(parent, false);
            var mm = root.AddComponent<MachineMotion>();
            switch (kind)
            {
                case Kind.Drone: Drone(root.transform, mm); break;
                case Kind.Bug: Bug(root.transform, mm); break;
                case Kind.Slime: Slime(root.transform, mm); break;
                default: Delivery(root.transform, mm); break;
            }
            return root;
        }

        static Transform Part(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static void Drone(Transform root, MachineMotion mm)
        {
            var body = Part(root, "Body", new Vector3(0, 0.25f, 0));
            mm.body = body; mm.bobAmount = 0.02f; mm.legSpeed = 2f;
            var hull = new MeshBuilder(2);
            hull.Sphere(Vector3.zero, new Vector3(0.22f, 0.09f, 0.28f), 10, 16, 0);
            hull.BoxC(new Vector3(0, -0.09f, 0), new Vector3(0.18f, 0.05f, 0.2f), 1);
            foreach (float x in new[] { -1f, 1f })
            {
                hull.Tube(new Vector3(x * 0.12f, -0.1f, -0.12f), new Vector3(x * 0.14f, -0.22f, -0.14f), 0.012f, 0.012f, 6, 1);
                hull.Tube(new Vector3(x * 0.12f, -0.1f, 0.12f), new Vector3(x * 0.14f, -0.22f, 0.14f), 0.012f, 0.012f, 6, 1);
                hull.Tube(new Vector3(x * 0.14f, -0.22f, -0.2f), new Vector3(x * 0.14f, -0.22f, 0.2f), 0.012f, 0.012f, 6, 1);
            }
            for (int i = 0; i < 4; i++)
            {
                var d = new Vector3(i % 2 == 0 ? -1 : 1, 0, i < 2 ? -1 : 1).normalized;
                hull.Tube(d * 0.12f, d * 0.4f, 0.02f, 0.018f, 8, 1);
                hull.Cylinder(d * 0.4f + Vector3.down * 0.03f, 0.04f, 0.035f, 0.07f, 12, 1);
            }
            hull.ToObject("Hull", body, new[] { White, Dark });
            for (int i = 0; i < 4; i++)
            {
                var d = new Vector3(i % 2 == 0 ? -1 : 1, 0, i < 2 ? -1 : 1).normalized;
                var rotor = Part(body, "Rotor" + i, d * 0.4f + Vector3.up * 0.05f);
                var rb = new MeshBuilder(1);
                rb.BoxC(Vector3.zero, new Vector3(0.36f, 0.006f, 0.035f), 0);
                rb.ToObject("Blades", rotor, new[] { Dark });
                mm.rotors.Add(rotor);
            }
            var eye = new MeshBuilder(1);
            eye.Sphere(new Vector3(0, -0.03f, 0.26f), Vector3.one * 0.045f, 8, 12, 0);
            mm.eye = eye.ToObject("Eye", body, new[] { Eye(new Color(1f, 0.15f, 0.12f)) }).GetComponent<Renderer>();
        }

        static void Bug(Transform root, MachineMotion mm)
        {
            var body = Part(root, "Body", new Vector3(0, 0.32f, 0));
            mm.body = body; mm.legSpeed = 9f; mm.legSwing = 25f; mm.bobAmount = 0.015f;
            var shell = new MeshBuilder(3);
            shell.Sphere(new Vector3(0, 0.02f, -0.05f), new Vector3(0.32f, 0.18f, 0.48f), 12, 18, 0);     // carapace
            shell.BoxC(new Vector3(0, 0.12f, -0.05f), new Vector3(0.012f, 0.1f, 0.85f), 1);               // wing seam
            shell.Sphere(new Vector3(0, -0.02f, 0.42f), new Vector3(0.16f, 0.11f, 0.13f), 10, 14, 1);    // head
            foreach (float x in new[] { -1f, 1f })
                shell.Tube(new Vector3(x * 0.06f, -0.05f, 0.52f), new Vector3(x * 0.1f, -0.08f, 0.66f), 0.018f, 0.006f, 6, 1);   // mandibles
            shell.Sphere(new Vector3(0, -0.08f, -0.05f), new Vector3(0.24f, 0.06f, 0.36f), 8, 14, 1);   // belly
            shell.ToObject("Shell", body, new[] { CityMaterials.Solid(new Color(0.1f, 0.13f, 0.12f), 0.22f, 0.7f, 0.8f), Dark, White });
            var eye = new MeshBuilder(1);
            foreach (float x in new[] { -1f, 1f }) eye.Sphere(new Vector3(x * 0.07f, 0.02f, 0.53f), Vector3.one * 0.028f, 8, 10, 0);
            mm.eye = eye.ToObject("Eyes", body, new[] { Eye(new Color(1f, 0.4f, 0.1f)) }).GetComponent<Renderer>();
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                float z = 0.2f - (i % 3) * 0.22f;
                var hip = Part(body, "Leg" + i, new Vector3(side * 0.22f, -0.05f, z));
                var leg = new MeshBuilder(1);
                var knee = new Vector3(side * 0.22f, 0.12f, 0.02f);
                leg.Tube(Vector3.zero, knee, 0.022f, 0.018f, 6, 0);
                leg.Tube(knee, new Vector3(side * 0.36f, -0.27f, 0.05f), 0.018f, 0.008f, 6, 0);
                leg.ToObject("Segments", hip, new[] { Dark });
                mm.legs.Add(hip);
            }
        }

        static void Slime(Transform root, MachineMotion mm)
        {
            var blob = Part(root, "Blob", Vector3.zero);
            mm.blob = blob;
            var mb = new MeshBuilder(2);
            mb.Sphere(new Vector3(0, 0.32f, 0), new Vector3(0.5f, 0.34f, 0.46f), 16, 24, 0, 0.12f, 3);
            // droplets and a pool at the base
            mb.Sphere(new Vector3(0.38f, 0.06f, 0.25f), Vector3.one * 0.07f, 6, 10, 0);
            mb.Sphere(new Vector3(-0.42f, 0.05f, -0.1f), Vector3.one * 0.06f, 6, 10, 0);
            mb.Sphere(new Vector3(0, 0.01f, 0), new Vector3(0.62f, 0.02f, 0.58f), 4, 20, 0);
            mb.ToObject("LiquidMetal", blob, new[] { Chrome });
            var eye = new MeshBuilder(1);
            foreach (float x in new[] { -1f, 1f }) eye.Sphere(new Vector3(x * 0.13f, 0.42f, 0.4f), new Vector3(0.05f, 0.07f, 0.03f), 8, 10, 0);
            mm.eye = eye.ToObject("Eyes", blob, new[] { Eye(new Color(0.4f, 0.9f, 1f)) }).GetComponent<Renderer>();
        }

        static void Delivery(Transform root, MachineMotion mm)
        {
            var body = Part(root, "Body", new Vector3(0, 0.12f, 0));
            mm.body = body; mm.bobAmount = 0.008f; mm.legSpeed = 6f;
            var mb = new MeshBuilder(3);
            mb.BoxC(new Vector3(0, 0.3f, 0), new Vector3(0.55f, 0.42f, 0.7f), 0);                 // cargo body
            mb.BoxC(new Vector3(0, 0.53f, 0), new Vector3(0.52f, 0.05f, 0.67f), 1);                // lid
            mb.BoxC(new Vector3(0, 0.12f, 0), new Vector3(0.5f, 0.08f, 0.66f), 1);                 // chassis
            mb.BoxC(new Vector3(0, 0.33f, 0.351f), new Vector3(0.3f, 0.12f, 0.01f), 2);            // front display
            mb.Tube(new Vector3(-0.2f, 0.55f, -0.3f), new Vector3(-0.2f, 1.25f, -0.3f), 0.008f, 0.008f, 5, 1);   // safety flag pole
            mb.BoxC(new Vector3(-0.13f, 1.18f, -0.3f), new Vector3(0.14f, 0.1f, 0.005f), 2);
            mb.ToObject("Hull", body, new[] { White, Dark, Eye(new Color(1f, 0.6f, 0.1f)) });
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                var w = Part(root, "Wheel" + i, new Vector3(side * 0.27f, 0.09f, 0.24f - (i % 3) * 0.24f));
                var wm = new MeshBuilder(2);
                wm.Tube(new Vector3(-0.03f, 0, 0), new Vector3(0.03f, 0, 0), 0.09f, 0.09f, 14, 0);
                wm.Tube(new Vector3(side * 0.031f, 0, 0), new Vector3(side * 0.035f, 0, 0), 0.05f, 0.05f, 10, 1);
                wm.ToObject("Tyre", w, new[] { Rubber, Dark });
                if (i == 4) w.gameObject.SetActive(false);   // off by one: one wheel short
                mm.wheels.Add(w);
            }
        }
    }
}
