using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Builds cartoony placeholder characters out of primitives (Section 0.5: swap for real models any time
    /// by filling the modelPrefab slot of the CharacterProfile / Enemy asset).
    /// </summary>
    public static class PlaceholderFactory
    {
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        public static Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, string name, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Kill(go.GetComponent<Collider>());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = scale;
            if (euler.HasValue) t.localEulerAngles = euler.Value;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return t;
        }

        public static Transform MeshPart(Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material mat, string name, Vector3? euler = null)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localScale = scale;
            if (euler.HasValue) t.localEulerAngles = euler.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return t;
        }

        static Transform Pivot(Transform parent, Vector3 pos, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        // ------------------------------------------------------------------ protagonist / NPC humanoid
        /// <summary>A chibi student: big head, hoodie, jeans, sneakers, headphones and a school bag.</summary>
        public static Transform BuildHumanoid(Transform parent, Color hoodie, Color skin, Color hair, Color pants, bool headphones, bool backpack, out ProceduralRig rig)
        {
            var root = new GameObject("PlaceholderModel").transform;
            root.SetParent(parent, false);
            var mHoodie = MaterialCache.Toon(hoodie);
            var mSkin = MaterialCache.Toon(skin);
            var mHair = MaterialCache.Toon(hair);
            var mPants = MaterialCache.Toon(pants);
            var mShoe = MaterialCache.Toon(new Color(0.95f, 0.95f, 0.97f));
            var mSole = MaterialCache.Toon(new Color(0.15f, 0.15f, 0.2f));
            rig = new ProceduralRig();

            var hips = Pivot(root, new Vector3(0, 0.78f, 0), "Hips");
            rig.hips = hips;
            var torso = Part(PrimitiveType.Capsule, hips, new Vector3(0, 0.3f, 0), new Vector3(0.46f, 0.36f, 0.32f), mHoodie, "Torso");
            Part(PrimitiveType.Sphere, hips, new Vector3(0, 0.56f, -0.1f), new Vector3(0.34f, 0.18f, 0.22f), mHoodie, "Hood");
            Part(PrimitiveType.Cube, hips, new Vector3(0, 0.22f, 0.15f), new Vector3(0.22f, 0.12f, 0.04f), MaterialCache.Toon(hoodie * 0.8f), "Pocket");
            var neck = Pivot(hips, new Vector3(0, 0.6f, 0), "Neck");
            rig.head = neck;
            var head = Part(PrimitiveType.Sphere, neck, new Vector3(0, 0.25f, 0.02f), new Vector3(0.5f, 0.5f, 0.48f), mSkin, "Head");
            rig.headMesh = head;
            Part(PrimitiveType.Sphere, neck, new Vector3(0, 0.38f, -0.03f), new Vector3(0.54f, 0.34f, 0.52f), mHair, "Hair");
            Part(PrimitiveType.Sphere, neck, new Vector3(0.05f, 0.46f, 0.12f), new Vector3(0.3f, 0.14f, 0.28f), mHair, "Fringe", new Vector3(0, 0, -15));
            Part(PrimitiveType.Sphere, neck, new Vector3(0.25f, 0.24f, 0.0f), new Vector3(0.08f, 0.12f, 0.08f), mSkin, "EarR");
            Part(PrimitiveType.Sphere, neck, new Vector3(-0.25f, 0.24f, 0.0f), new Vector3(0.08f, 0.12f, 0.08f), mSkin, "EarL");
            if (headphones)
            {
                var mPhone = MaterialCache.Toon(new Color(0.12f, 0.12f, 0.16f));
                var mPad = MaterialCache.ToonEmissive(new Color(1f, 0.25f, 0.6f), new Color(0.6f, 0.1f, 0.35f));
                Part(PrimitiveType.Cube, neck, new Vector3(0, 0.53f, -0.02f), new Vector3(0.56f, 0.05f, 0.07f), mPhone, "Band");
                Part(PrimitiveType.Cylinder, neck, new Vector3(0.27f, 0.25f, 0), new Vector3(0.16f, 0.04f, 0.16f), mPad, "CupR", new Vector3(0, 0, 90));
                Part(PrimitiveType.Cylinder, neck, new Vector3(-0.27f, 0.25f, 0), new Vector3(0.16f, 0.04f, 0.16f), mPad, "CupL", new Vector3(0, 0, 90));
                Part(PrimitiveType.Cube, neck, new Vector3(0.28f, 0.42f, -0.02f), new Vector3(0.04f, 0.22f, 0.06f), mPhone, "ArmR");
                Part(PrimitiveType.Cube, neck, new Vector3(-0.28f, 0.42f, -0.02f), new Vector3(0.04f, 0.22f, 0.06f), mPhone, "ArmL");
            }
            if (backpack)
            {
                var mBag = MaterialCache.Toon(new Color(0.95f, 0.55f, 0.15f));
                Part(PrimitiveType.Cube, hips, new Vector3(0, 0.34f, -0.24f), new Vector3(0.36f, 0.4f, 0.16f), mBag, "Backpack");
                Part(PrimitiveType.Cube, hips, new Vector3(0, 0.2f, -0.33f), new Vector3(0.26f, 0.14f, 0.05f), MaterialCache.Toon(new Color(0.85f, 0.4f, 0.1f)), "BagPocket");
            }
            // arms
            rig.armL = Pivot(hips, new Vector3(-0.27f, 0.5f, 0), "ArmL");
            rig.armR = Pivot(hips, new Vector3(0.27f, 0.5f, 0), "ArmR");
            foreach (var arm in new[] { rig.armL, rig.armR })
            {
                Part(PrimitiveType.Capsule, arm, new Vector3(0, -0.2f, 0), new Vector3(0.14f, 0.2f, 0.14f), mHoodie, "Sleeve");
                Part(PrimitiveType.Sphere, arm, new Vector3(0, -0.43f, 0), new Vector3(0.12f, 0.12f, 0.12f), mSkin, "Hand");
            }
            // legs
            rig.legL = Pivot(hips, new Vector3(-0.11f, 0.02f, 0), "LegL");
            rig.legR = Pivot(hips, new Vector3(0.11f, 0.02f, 0), "LegR");
            foreach (var leg in new[] { rig.legL, rig.legR })
            {
                Part(PrimitiveType.Capsule, leg, new Vector3(0, -0.33f, 0), new Vector3(0.17f, 0.34f, 0.17f), mPants, "Leg");
                Part(PrimitiveType.Cube, leg, new Vector3(0, -0.72f, 0.05f), new Vector3(0.17f, 0.1f, 0.3f), mShoe, "Shoe");
                Part(PrimitiveType.Cube, leg, new Vector3(0, -0.77f, 0.05f), new Vector3(0.18f, 0.03f, 0.31f), mSole, "Sole");
            }
            rig.root = root;
            return root;
        }

        // ------------------------------------------------------------------ Pixel the companion
        public static Transform BuildCompanion(Transform parent, out Renderer screen, out Transform body)
        {
            var root = new GameObject("PixelModel").transform;
            root.SetParent(parent, false);
            var mBody = MaterialCache.Toon(new Color(0.88f, 0.92f, 1f), 0.015f);
            var mDark = MaterialCache.Toon(new Color(0.1f, 0.12f, 0.18f), 0.01f);
            var mGlow = MaterialCache.ToonEmissive(new Color(0.3f, 0.9f, 1f), new Color(0.3f, 1.4f, 1.8f));
            body = Part(PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.5f, 0.42f, 0.42f), mBody, "Body");
            Part(PrimitiveType.Cube, root, new Vector3(0, 0, 0.2f), new Vector3(0.42f, 0.34f, 0.04f), mDark, "Bezel");
            var scr = MeshPart(ProceduralMeshes.Quad(), root, new Vector3(0, 0, 0.225f), new Vector3(0.36f, 0.28f, 1f), MaterialCache.Instance("holo"), "Screen", new Vector3(0, 180, 0));
            screen = scr.GetComponent<Renderer>();
            screen.sharedMaterial.SetVector("_Scroll", Vector4.zero);
            screen.sharedMaterial.SetFloat("_Scanlines", 60f);
            screen.sharedMaterial.SetFloat("_Alpha", 1f);
            Part(PrimitiveType.Cylinder, root, new Vector3(0.12f, 0.3f, 0), new Vector3(0.025f, 0.1f, 0.025f), mDark, "Antenna");
            Part(PrimitiveType.Sphere, root, new Vector3(0.12f, 0.42f, 0), Vector3.one * 0.07f, mGlow, "AntennaTip");
            Part(PrimitiveType.Sphere, root, new Vector3(-0.3f, -0.02f, 0), Vector3.one * 0.12f, mDark, "ThrusterL");
            Part(PrimitiveType.Sphere, root, new Vector3(0.3f, -0.02f, 0), Vector3.one * 0.12f, mDark, "ThrusterR");
            Part(PrimitiveType.Sphere, root, new Vector3(0, -0.24f, 0), new Vector3(0.18f, 0.06f, 0.18f), mGlow, "Hover");
            return root;
        }

        // ------------------------------------------------------------------ enemies
        public static Transform BuildEnemy(Transform parent, string shape, Color tint, float height, out Transform eyes)
        {
            var root = new GameObject("EnemyModel_" + shape).transform;
            root.SetParent(parent, false);
            var m = MaterialCache.Toon(tint, 0.025f);
            var dark = MaterialCache.Toon(Color.Lerp(tint, Color.black, 0.55f), 0.02f);
            var glow = MaterialCache.ToonEmissive(tint, tint * 1.6f);
            float h = 1f;
            eyes = Pivot(root, new Vector3(0, 0.62f, 0.4f), "Eyes");
            switch (shape)
            {
                case "spiky":
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.5f, 0), new Vector3(1f, 0.9f, 0.9f), m, "Body");
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i / 9f * 360f;
                        var dir = Quaternion.Euler(0, 0, a) * Vector3.up;
                        MeshPart(ProceduralMeshes.Cone(), root, new Vector3(0, 0.5f, 0) + dir * 0.48f, new Vector3(0.22f, 0.38f, 0.22f), dark, "Spike" + i, new Vector3(0, 0, a));
                    }
                    break;
                case "robot":
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.45f, 0), new Vector3(0.8f, 0.6f, 0.6f), m, "Body");
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.95f, 0), new Vector3(0.6f, 0.4f, 0.5f), m, "Head");
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.97f, 0.25f), new Vector3(0.5f, 0.16f, 0.04f), glow, "Visor");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 1.25f, 0), new Vector3(0.04f, 0.12f, 0.04f), dark, "Antenna");
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 1.4f, 0), Vector3.one * 0.1f, glow, "Tip");
                    Part(PrimitiveType.Cube, root, new Vector3(0.5f, 0.45f, 0), new Vector3(0.16f, 0.5f, 0.2f), dark, "ArmR");
                    Part(PrimitiveType.Cube, root, new Vector3(-0.5f, 0.45f, 0), new Vector3(0.16f, 0.5f, 0.2f), dark, "ArmL");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.1f, 0), new Vector3(0.5f, 0.1f, 0.5f), dark, "Wheel");
                    eyes.localPosition = new Vector3(0, 0.97f, 0.27f);
                    h = 1.45f;
                    break;
                case "ghost":
                    Part(PrimitiveType.Capsule, root, new Vector3(0, 0.65f, 0), new Vector3(0.8f, 0.55f, 0.6f), m, "Body");
                    for (int i = 0; i < 4; i++)
                        Part(PrimitiveType.Sphere, root, new Vector3(-0.3f + i * 0.2f, 0.12f, 0), Vector3.one * 0.26f, m, "Tail" + i);
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.75f, 0), Vector3.one * 0.95f, MaterialCache.Get("holo"), "Aura");
                    eyes.localPosition = new Vector3(0, 0.78f, 0.3f);
                    h = 1.3f;
                    break;
                case "crystal":
                    MeshPart(ProceduralMeshes.Crystal(), root, new Vector3(0, 0.65f, 0), new Vector3(0.8f, 1.3f, 0.8f), m, "Body");
                    MeshPart(ProceduralMeshes.Crystal(), root, new Vector3(0.45f, 0.3f, 0), new Vector3(0.3f, 0.5f, 0.3f), glow, "ShardR");
                    MeshPart(ProceduralMeshes.Crystal(), root, new Vector3(-0.45f, 0.4f, 0), new Vector3(0.25f, 0.45f, 0.25f), glow, "ShardL");
                    eyes.localPosition = new Vector3(0, 0.8f, 0.3f);
                    h = 1.3f;
                    break;
                case "cell":
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.55f, 0), new Vector3(1.05f, 0.95f, 0.9f), MaterialCache.Get("holo"), "Membrane");
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.55f, 0), new Vector3(0.85f, 0.75f, 0.7f), m, "Cytoplasm");
                    Part(PrimitiveType.Sphere, root, new Vector3(0.12f, 0.6f, -0.05f), Vector3.one * 0.35f, dark, "Nucleus");
                    break;
                case "virus":
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.6f, 0), Vector3.one * 0.85f, m, "Capsid");
                    for (int i = 0; i < 12; i++)
                    {
                        var dir = Quaternion.Euler(i * 37f, i * 71f, 0) * Vector3.up;
                        Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.6f, 0) + dir * 0.5f, new Vector3(0.05f, 0.12f, 0.05f), dark, "Stalk" + i, Quaternion.FromToRotation(Vector3.up, dir).eulerAngles);
                        Part(PrimitiveType.Sphere, root, new Vector3(0, 0.6f, 0) + dir * 0.62f, Vector3.one * 0.12f, glow, "Knob" + i);
                    }
                    eyes.localPosition = new Vector3(0, 0.65f, 0.38f);
                    h = 1.25f;
                    break;
                case "mech":
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.1f, 0), new Vector3(1.4f, 1.1f, 0.9f), m, "Torso");
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.95f, 0), new Vector3(1f, 0.7f, 0.7f), dark, "HeadFrame");
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.95f, 0.36f), new Vector3(0.85f, 0.55f, 0.04f), glow, "ScreenFace");
                    Part(PrimitiveType.Cube, root, new Vector3(0.95f, 1.15f, 0), new Vector3(0.4f, 1.2f, 0.5f), dark, "ArmR");
                    Part(PrimitiveType.Cube, root, new Vector3(-0.95f, 1.15f, 0), new Vector3(0.4f, 1.2f, 0.5f), dark, "ArmL");
                    Part(PrimitiveType.Sphere, root, new Vector3(0.95f, 0.45f, 0), Vector3.one * 0.5f, m, "FistR");
                    Part(PrimitiveType.Sphere, root, new Vector3(-0.95f, 0.45f, 0), Vector3.one * 0.5f, m, "FistL");
                    Part(PrimitiveType.Cube, root, new Vector3(0.35f, 0.3f, 0), new Vector3(0.4f, 0.6f, 0.5f), dark, "LegR");
                    Part(PrimitiveType.Cube, root, new Vector3(-0.35f, 0.3f, 0), new Vector3(0.4f, 0.6f, 0.5f), dark, "LegL");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 1.15f, 0.46f), new Vector3(0.5f, 0.02f, 0.5f), glow, "Core", new Vector3(90, 0, 0));
                    eyes.localPosition = new Vector3(0, 1.98f, 0.4f);
                    h = 2.4f;
                    break;
                default: // blob
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.42f, 0), new Vector3(1.05f, 0.85f, 0.9f), m, "Body");
                    Part(PrimitiveType.Sphere, root, new Vector3(0.25f, 0.85f, 0), new Vector3(0.18f, 0.25f, 0.18f), m, "Antenna");
                    Part(PrimitiveType.Sphere, root, new Vector3(-0.3f, 0.08f, 0.1f), new Vector3(0.3f, 0.14f, 0.3f), dark, "FootL");
                    Part(PrimitiveType.Sphere, root, new Vector3(0.3f, 0.08f, 0.1f), new Vector3(0.3f, 0.14f, 0.3f), dark, "FootR");
                    eyes.localPosition = new Vector3(0, 0.55f, 0.38f);
                    h = 0.95f;
                    break;
            }
            // big cartoon eyes with angry brows (for every shape)
            var white = MaterialCache.Toon(Color.white, 0.01f);
            var black = MaterialCache.Toon(new Color(0.05f, 0.03f, 0.08f), 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                var eye = Part(PrimitiveType.Sphere, eyes, new Vector3(side * 0.16f, 0, 0), new Vector3(0.22f, 0.26f, 0.12f), white, side < 0 ? "EyeL" : "EyeR");
                Part(PrimitiveType.Sphere, eye, new Vector3(0, -0.05f, 0.4f), new Vector3(0.45f, 0.45f, 0.4f), black, "Pupil");
                Part(PrimitiveType.Cube, eyes, new Vector3(side * 0.16f, 0.17f, 0.03f), new Vector3(0.22f, 0.05f, 0.04f), black, "Brow", new Vector3(0, 0, side * -18f));
            }
            float s = height / h;
            root.localScale = Vector3.one * s;
            return root;
        }

        // ------------------------------------------------------------------ hats (cosmetics)
        public static Transform BuildHat(Transform head, string hat, Color c, float headSize, float topOffset)
        {
            if (string.IsNullOrEmpty(hat) || hat == "none") return null;
            var root = new GameObject("Hat_" + hat).transform;
            root.SetParent(head, false);
            root.localPosition = new Vector3(0, topOffset, 0);
            float k = headSize * 2f;   // head diameter
            var m = MaterialCache.Toon(c);
            var dark = MaterialCache.Toon(c * 0.6f);
            switch (hat)
            {
                case "cone":
                    MeshPart(ProceduralMeshes.Cone(), root, new Vector3(0, k * 0.45f, 0), new Vector3(k * 0.9f, k * 1.1f, k * 0.9f), m, "Cone");
                    Part(PrimitiveType.Cube, root, new Vector3(0, -k * 0.05f, 0), new Vector3(k * 1.2f, k * 0.08f, k * 1.2f), m, "Base");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, k * 0.45f, 0), new Vector3(k * 0.62f, k * 0.06f, k * 0.62f), MaterialCache.Toon(Color.white), "Stripe");
                    break;
                case "crown":
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, k * 0.15f, 0), new Vector3(k * 0.95f, k * 0.15f, k * 0.95f), m, "Band");
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2;
                        MeshPart(ProceduralMeshes.Cone(), root, new Vector3(Mathf.Cos(a) * k * 0.42f, k * 0.42f, Mathf.Sin(a) * k * 0.42f), new Vector3(k * 0.22f, k * 0.35f, k * 0.22f), m, "Point" + i);
                        Part(PrimitiveType.Sphere, root, new Vector3(Mathf.Cos(a) * k * 0.47f, k * 0.15f, Mathf.Sin(a) * k * 0.47f), Vector3.one * k * 0.1f, MaterialCache.ToonEmissive(new Color(1, 0.2f, 0.6f), new Color(0.8f, 0.1f, 0.4f)), "Gem" + i);
                    }
                    break;
                case "propeller":
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0, 0), new Vector3(k * 1.05f, k * 0.55f, k * 1.05f), m, "Beanie");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, k * 0.32f, 0), new Vector3(k * 0.06f, k * 0.12f, k * 0.06f), dark, "Stem");
                    var prop = new GameObject("Propeller").transform;
                    prop.SetParent(root, false); prop.localPosition = new Vector3(0, k * 0.46f, 0);
                    Part(PrimitiveType.Cube, prop, Vector3.zero, new Vector3(k * 1.1f, k * 0.03f, k * 0.14f), MaterialCache.Toon(new Color(1f, 0.3f, 0.3f)), "Blade1");
                    Part(PrimitiveType.Cube, prop, Vector3.zero, new Vector3(k * 0.14f, k * 0.03f, k * 1.1f), MaterialCache.Toon(new Color(0.3f, 0.6f, 1f)), "Blade2");
                    prop.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 720, 0);
                    break;
                case "chef":
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, k * 0.25f, 0), new Vector3(k * 0.8f, k * 0.3f, k * 0.8f), m, "Tube");
                    Part(PrimitiveType.Sphere, root, new Vector3(0, k * 0.75f, 0), new Vector3(k * 1.05f, k * 0.6f, k * 1.05f), m, "Puff");
                    break;
                case "halo":
                    var ring = new GameObject("Halo").transform;
                    ring.SetParent(root, false); ring.localPosition = new Vector3(0, k * 0.55f, 0);
                    for (int i = 0; i < 16; i++)
                    {
                        float a = i / 16f * Mathf.PI * 2;
                        Part(PrimitiveType.Cube, ring, new Vector3(Mathf.Cos(a) * k * 0.5f, 0, Mathf.Sin(a) * k * 0.5f), new Vector3(k * 0.12f, k * 0.06f, k * 0.12f), MaterialCache.ToonEmissive(c, c * 1.8f), (i % 2 == 0 ? "1" : "0"));
                    }
                    ring.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 60, 0);
                    break;
                case "antenna":
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Part(PrimitiveType.Cylinder, root, new Vector3(side * k * 0.3f, k * 0.35f, 0), new Vector3(k * 0.05f, k * 0.35f, k * 0.05f), dark, "Stalk", new Vector3(0, 0, -side * 15));
                        Part(PrimitiveType.Sphere, root, new Vector3(side * k * 0.42f, k * 0.72f, 0), Vector3.one * k * 0.16f, MaterialCache.ToonEmissive(c, c * 1.5f), "Ball");
                    }
                    break;
                case "tophat":
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0, 0), new Vector3(k * 1.3f, k * 0.03f, k * 1.3f), m, "Brim");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, k * 0.4f, 0), new Vector3(k * 0.8f, k * 0.4f, k * 0.8f), m, "Crown");
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, k * 0.12f, 0), new Vector3(k * 0.82f, k * 0.08f, k * 0.82f), MaterialCache.Toon(new Color(0.8f, 0.1f, 0.2f)), "Ribbon");
                    break;
                case "goggles":
                    root.localPosition = new Vector3(0, topOffset - headSize * 0.7f, headSize * 0.1f);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0, 0), new Vector3(k * 1.05f, k * 0.12f, k * 1.02f), dark, "Strap");
                    foreach (float side in new[] { -1f, 1f })
                        Part(PrimitiveType.Cylinder, root, new Vector3(side * k * 0.2f, k * 0.04f, k * 0.46f), new Vector3(k * 0.3f, k * 0.06f, k * 0.3f), MaterialCache.ToonEmissive(c, c * 1.2f), "Lens", new Vector3(90, 0, 0));
                    break;
                case "duck":
                    var y = MaterialCache.Toon(new Color(1f, 0.85f, 0.1f));
                    Part(PrimitiveType.Sphere, root, new Vector3(0, k * 0.25f, 0), new Vector3(k * 0.75f, k * 0.5f, k * 0.9f), y, "DuckBody");
                    Part(PrimitiveType.Sphere, root, new Vector3(0, k * 0.65f, k * 0.25f), Vector3.one * k * 0.45f, y, "DuckHead");
                    Part(PrimitiveType.Cube, root, new Vector3(0, k * 0.6f, k * 0.5f), new Vector3(k * 0.22f, k * 0.08f, k * 0.2f), MaterialCache.Toon(new Color(1f, 0.5f, 0.1f)), "Beak");
                    Part(PrimitiveType.Sphere, root, new Vector3(k * 0.1f, k * 0.72f, k * 0.42f), Vector3.one * k * 0.07f, MaterialCache.Toon(Color.black), "DuckEye");
                    break;
            }
            return root;
        }
    }

    /// <summary>Limb pivots of a primitive-built humanoid, animated by ProceduralAnimator.</summary>
    public class ProceduralRig
    {
        public Transform root, hips, head, headMesh, armL, armR, legL, legR;
    }
}
