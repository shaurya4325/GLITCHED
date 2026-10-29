using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Unique Glitch Lord bodies that are not robots (built in code, animated by MachineMotion):
    ///   VirusTitan  - The Lord Within: a giant viral organism, pulsing core, glowing veins, writhing spiked tendrils
    ///   CrystalJudge - The Lord of Broken Laws: a tall robed judge with a crystal head, orbiting prisms, a floating gavel
    ///   ShardTitan  - The Source: a titan of floating glitch shards around a blazing core, with a slow halo
    /// Models face +Z and stand on y = 0.
    /// </summary>
    public static class CreatureGen
    {
        public enum Kind { VirusTitan, CrystalJudge, ShardTitan }

        public static GameObject Build(Kind kind)
        {
            var root = new GameObject("Creature_" + kind);
            var mm = root.AddComponent<MachineMotion>();
            var rnd = new System.Random((int)kind * 71 + 5);
            switch (kind)
            {
                case Kind.VirusTitan: Virus(root.transform, mm, rnd); break;
                case Kind.CrystalJudge: Judge(root.transform, mm, rnd); break;
                default: Shards(root.transform, mm, rnd); break;
            }
            return root;
        }

        static Transform Node(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static void Virus(Transform root, MachineMotion mm, System.Random rnd)
        {
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var flesh = CityMaterials.Solid(new Color(0.32f, 0.04f, 0.07f), 0.22f, 0.1f, 0.8f);
            var vein = CityMaterials.Emissive(new Color(0.5f, 0.05f, 0.1f), new Color(3.2f, 0.25f, 0.45f));
            var spikeM = CityMaterials.Solid(new Color(0.55f, 0.45f, 0.42f), 0.35f, 0f, 0.8f);
            var body = Node(root, "Body", new Vector3(0, 2.2f, 0));
            mm.body = body; mm.bobAmount = 0.12f; mm.legSpeed = 1.6f; mm.legSwing = 14f;
            var core = Node(body, "Core", Vector3.zero);
            mm.blob = core;
            var mb = new MeshBuilder(2);
            mb.Sphere(Vector3.zero, Vector3.one * 1.8f, 18, 28, 0, 0.12f, 7);
            for (int i = 0; i < 26; i++)
            {
                var a = Random.onUnitSphere;
                var p0 = a * 1.75f;
                var p1 = Quaternion.AngleAxis(R(20f, 60f), Random.onUnitSphere) * a * 1.82f;
                mb.Tube(p0, p1, 0.06f, 0.03f, 5, 1);
            }
            mb.ToObject("Membrane", core, new[] { flesh, vein });
            // spiked receptor tendrils, each a swinging limb
            for (int i = 0; i < 12; i++)
            {
                var dir = Random.onUnitSphere;
                var limb = Node(body, "Tendril" + i, dir * 1.7f);
                limb.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                var tm = new MeshBuilder(2);
                float len = R(1.2f, 2.2f);
                tm.Tube(Vector3.zero, Vector3.up * len, 0.16f, 0.05f, 8, 0);
                tm.Sphere(Vector3.up * len, Vector3.one * 0.22f, 6, 10, 1);
                tm.ToObject("Spike", limb, new[] { spikeM, vein });
                mm.legs.Add(limb);
            }
            // orbiting cells
            var orbit = Node(body, "CellOrbit", Vector3.zero);
            var om = new MeshBuilder(1);
            for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2f; om.Sphere(new Vector3(Mathf.Cos(a) * 3.4f, Mathf.Sin(a * 2f) * 0.6f, Mathf.Sin(a) * 3.4f), Vector3.one * 0.3f, 6, 8, 0); }
            om.ToObject("Cells", orbit, new[] { CityMaterials.Solid(new Color(0.85f, 0.2f, 0.25f), 0.3f, 0f, 0.8f) });
            mm.rotors.Add(orbit); mm.rotorSpeed = 25f;
            mm.eye = core.GetComponentInChildren<Renderer>();
        }

        static void Judge(Transform root, MachineMotion mm, System.Random rnd)
        {
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var robe = CityMaterials.PBR("Fabric081C", new Color(0.12f, 0.08f, 0.2f), 0.6f, 1f, 0f, 0.8f, false);
            var trim = CityMaterials.Solid(new Color(0.85f, 0.7f, 0.35f), 0.3f, 0.9f, 0.8f);
            var crystal = CityMaterials.Emissive(new Color(0.45f, 0.3f, 0.9f), new Color(1.6f, 0.9f, 3.2f), 0.05f);
            var body = Node(root, "Body", Vector3.zero);
            mm.body = body; mm.bobAmount = 0.05f; mm.legSpeed = 1.2f;
            var mb = new MeshBuilder(2);
            // a tall flared robe (stacked tapered cylinders), collar and shoulders
            float[] ys = { 0f, 1.2f, 2.4f, 3.2f };
            float[] rs = { 1.5f, 1.15f, 0.8f, 0.7f };
            for (int i = 0; i < ys.Length - 1; i++) mb.Cylinder(new Vector3(0, ys[i], 0), rs[i], rs[i + 1], ys[i + 1] - ys[i], 28, 0, false);
            mb.Sphere(new Vector3(0, 3.25f, 0), new Vector3(1.1f, 0.35f, 0.75f), 8, 20, 0);
            mb.Cylinder(new Vector3(0, 0.02f, 0), 1.52f, 1.52f, 0.12f, 28, 1);
            mb.Cylinder(new Vector3(0, 3.2f, 0), 0.5f, 0.42f, 0.25f, 20, 1);
            mb.ToObject("Robe", body, new[] { robe, trim });
            // crystal head (an elongated octahedron-ish gem)
            var head = Node(body, "Head", new Vector3(0, 4.1f, 0));
            var hm = new MeshBuilder(1);
            hm.Sphere(Vector3.zero, new Vector3(0.45f, 0.75f, 0.45f), 4, 6, 0);
            mm.eye = hm.ToObject("CrystalHead", head, new[] { crystal }).GetComponent<Renderer>();
            // orbiting prisms (the laws, out of order) and a floating gavel
            var orbit = Node(body, "Prisms", new Vector3(0, 3.6f, 0));
            var pm = new MeshBuilder(1);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                pm.Sphere(new Vector3(Mathf.Cos(a) * 2.4f, R(-0.5f, 0.6f), Mathf.Sin(a) * 2.4f), new Vector3(0.18f, 0.5f, 0.18f), 3, 4, 0);
            }
            pm.ToObject("Orbit", orbit, new[] { crystal });
            mm.rotors.Add(orbit); mm.rotorSpeed = 35f;
            var gavel = Node(body, "Gavel", new Vector3(1.8f, 2.6f, 0.6f));
            var gm = new MeshBuilder(2);
            gm.Tube(Vector3.zero, new Vector3(0, -1.2f, 0), 0.06f, 0.06f, 8, 0);
            gm.Tube(new Vector3(-0.35f, 0, 0), new Vector3(0.35f, 0, 0), 0.22f, 0.22f, 12, 1);
            gm.ToObject("GavelMesh", gavel, new[] { CityMaterials.PBR("Wood066", new Color(0.5f, 0.32f, 0.2f), 0.5f), trim });
            mm.legs.Add(gavel);
        }

        static void Shards(Transform root, MachineMotion mm, System.Random rnd)
        {
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var dark = CityMaterials.Solid(new Color(0.85f, 0.87f, 0.92f), 0.1f, 0.9f, 0.6f);
            var core = CityMaterials.Emissive(new Color(0.9f, 0.7f, 1f), new Color(6f, 4f, 8f));
            var seam = CityMaterials.Emissive(new Color(0.4f, 0.1f, 0.5f), new Color(3f, 0.8f, 4f));
            var body = Node(root, "Body", Vector3.zero);
            mm.body = body; mm.bobAmount = 0.08f; mm.legSpeed = 1.4f; mm.legSwing = 10f;
            // a humanoid silhouette of floating shards: legs, torso, shoulders, head (each body part swings a little)
            (string name, Vector3 c, Vector3 size, int n)[] parts =
            {
                ("LegL", new Vector3(-0.7f, 1.6f, 0), new Vector3(0.6f, 3.0f, 0.6f), 18),
                ("LegR", new Vector3(0.7f, 1.6f, 0), new Vector3(0.6f, 3.0f, 0.6f), 18),
                ("Torso", new Vector3(0, 4.3f, 0), new Vector3(2.0f, 2.4f, 1.1f), 40),
                ("ArmL", new Vector3(-1.9f, 4.4f, 0), new Vector3(0.5f, 2.6f, 0.5f), 16),
                ("ArmR", new Vector3(1.9f, 4.4f, 0), new Vector3(0.5f, 2.6f, 0.5f), 16),
                ("Head", new Vector3(0, 6.2f, 0), new Vector3(0.9f, 1.0f, 0.9f), 14),
            };
            foreach (var part in parts)
            {
                var t = Node(body, part.name, part.c);
                var m = new MeshBuilder(2);
                for (int i = 0; i < part.n; i++)
                {
                    var p = new Vector3(R(-0.5f, 0.5f) * part.size.x, R(-0.5f, 0.5f) * part.size.y, R(-0.5f, 0.5f) * part.size.z);
                    var s = new Vector3(R(0.15f, 0.45f), R(0.2f, 0.7f), R(0.1f, 0.35f));
                    m.BoxC(p, s, i % 6 == 0 ? 1 : 0);
                }
                m.ToObject("Shards", t, new[] { dark, seam });
                if (part.name.StartsWith("Arm") || part.name.StartsWith("Leg")) mm.legs.Add(t);
            }
            var heart = Node(body, "Core", new Vector3(0, 4.5f, 0.2f));
            var cm = new MeshBuilder(1);
            cm.Sphere(Vector3.zero, Vector3.one * 0.6f, 10, 14, 0);
            mm.eye = cm.ToObject("CoreMesh", heart, new[] { core }).GetComponent<Renderer>();
            mm.blob = heart;
            var halo = Node(body, "Halo", new Vector3(0, 6.3f, -0.4f));
            var hm = new MeshBuilder(1);
            for (int i = 0; i < 24; i++) { float a = i / 24f * Mathf.PI * 2f; hm.BoxC(new Vector3(Mathf.Cos(a) * 1.6f, Mathf.Sin(a) * 1.6f, 0), new Vector3(0.18f, 0.18f, 0.18f), 0); }
            hm.ToObject("HaloRing", halo, new[] { core });
            halo.localRotation = Quaternion.Euler(90, 0, 0);
            mm.rotors.Add(halo); mm.rotorSpeed = 20f;
            var l = heart.gameObject.AddComponent<Light>();
            l.type = LightType.Point; l.range = 12f; l.intensity = 4f; l.color = new Color(0.85f, 0.6f, 1f);
        }
    }
}
