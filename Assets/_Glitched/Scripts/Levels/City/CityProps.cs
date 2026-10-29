using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Realistic versions of the gameplay objects for the city levels. The child names the gameplay scripts look for
    /// ("Screen" for checkpoints, "Leaf" for jammed doors) are kept.
    /// </summary>
    public static class CityProps
    {
        static Material Steel => CityMaterials.PBR("PaintedMetal004", new Color(0.26f, 0.27f, 0.29f), 1.5f, 0.8f, 0.6f);
        static Material Brushed => CityMaterials.PBR("Metal009", new Color(0.62f, 0.63f, 0.65f), 1f, 0.5f, 0.9f);
        static Material Concrete => CityMaterials.PBR("Concrete046", new Color(0.7f, 0.69f, 0.66f), 2f);

        static GameObject Obj(MeshBuilder mb, string name, Transform parent, Material[] mats, Vector3 local)
        {
            var go = mb.ToObject(name, parent, mats);
            go.transform.localPosition = local;
            return go;
        }

        /// <summary>Checkpoint: a city information kiosk (its "Screen" turns from magenta to green when saved).</summary>
        public static void Checkpoint(Transform go, Color accent)
        {
            var mb = new MeshBuilder(2);
            mb.BoxC(new Vector3(0, 0.08f, 1.2f), new Vector3(0.8f, 0.16f, 0.5f), 1);
            mb.BoxC(new Vector3(0, 1.0f, 1.2f), new Vector3(0.62f, 1.7f, 0.32f), 0);
            mb.BoxC(new Vector3(0, 1.9f, 1.2f), new Vector3(0.7f, 0.08f, 0.4f), 0);
            Obj(mb, "Kiosk", go, new[] { Brushed, Concrete }, Vector3.zero);
            var scr = new MeshBuilder(1);
            scr.BoxC(Vector3.zero, new Vector3(0.5f, 0.8f, 0.02f), 0);
            Obj(scr, "Screen", go, new[] { CityMaterials.Screen(null, new Color(1.6f, 0.2f, 1.2f)) }, new Vector3(0, 1.2f, 1.03f));
        }

        /// <summary>Clue terminal: a digital sign pillar showing the clue image.</summary>
        public static void Terminal(Transform go, Texture content, Color accent)
        {
            var mb = new MeshBuilder(1);
            mb.BoxC(new Vector3(0, 1.25f, 1.1f), new Vector3(1.3f, 2.5f, 0.22f), 0);
            mb.BoxC(new Vector3(0, 0.05f, 1.1f), new Vector3(1.5f, 0.1f, 0.4f), 0);
            Obj(mb, "SignPillar", go, new[] { Steel }, Vector3.zero);
            var scr = new MeshBuilder(1);
            scr.BoxC(Vector3.zero, new Vector3(1.1f, 1.5f, 0.02f), 0);
            Obj(scr, "Display", go, new[] { CityMaterials.Screen(content, accent * 1.4f) }, new Vector3(0, 1.55f, 0.98f));
        }

        /// <summary>Jammed door: a steel roller shutter in a concrete frame with a red status light.</summary>
        public static Transform Door(Transform go, float depth)
        {
            var mb = new MeshBuilder(2);
            mb.BoxC(new Vector3(0, 3.7f, 0), new Vector3(2.8f, 0.6f, depth), 0);
            mb.BoxC(new Vector3(-1.2f, 1.7f, 0), new Vector3(0.4f, 3.4f, depth), 0);
            mb.BoxC(new Vector3(1.2f, 1.7f, 0), new Vector3(0.4f, 3.4f, depth), 0);
            mb.BoxC(new Vector3(0.72f, 3.55f, -depth / 2 - 0.02f), new Vector3(0.12f, 0.12f, 0.04f), 1);
            Obj(mb, "Frame", go, new[] { Concrete, CityMaterials.Emissive(new Color(0.5f, 0.05f, 0.04f), new Color(3f, 0.2f, 0.12f)) }, Vector3.zero);
            var leaf = new GameObject("Leaf").transform;
            leaf.SetParent(go, false);
            leaf.localPosition = new Vector3(0, 1.7f, 0);
            var sh = new MeshBuilder(1);
            for (int i = 0; i < 17; i++) sh.BoxC(new Vector3(0, -1.6f + i * 0.2f, 0), new Vector3(2.0f, 0.18f, depth * 0.7f), 0);   // shutter slats
            Obj(sh, "Shutter", leaf, new[] { Brushed }, Vector3.zero);
            var block = leaf.gameObject.AddComponent<BoxCollider>();
            block.size = new Vector3(2.0f, 3.4f, depth);
            return leaf;
        }

        /// <summary>Ability barrier: a security light curtain between two steel posts.</summary>
        public static void Barrier(Transform go, float height, float depth, Color accent)
        {
            var mb = new MeshBuilder(1);
            foreach (float x in new[] { -0.7f, 0.7f })
            {
                mb.BoxC(new Vector3(x, height / 2, 0), new Vector3(0.16f, height, 0.16f), 0);
                mb.BoxC(new Vector3(x, 0.05f, 0), new Vector3(0.35f, 0.1f, 0.35f), 0);
            }
            Obj(mb, "Posts", go, new[] { Steel }, Vector3.zero);
            var beams = new MeshBuilder(1);
            for (float y = 0.4f; y < height; y += 0.45f) beams.BoxC(new Vector3(0, y, 0), new Vector3(1.3f, 0.02f, 0.02f), 0);
            Obj(beams, "LightCurtain", go, new[] { CityMaterials.Emissive(accent * 0.3f, accent * 3f) }, Vector3.zero);
        }

        /// <summary>Portal: a slim brushed-metal frame holding a soft, slowly moving sheet of light.</summary>
        public static void Portal(Transform go, Color accent)
        {
            var mb = new MeshBuilder(1);
            mb.BoxC(new Vector3(-1.15f, 1.6f, 0.6f), new Vector3(0.14f, 3.2f, 0.3f), 0);
            mb.BoxC(new Vector3(1.15f, 1.6f, 0.6f), new Vector3(0.14f, 3.2f, 0.3f), 0);
            mb.BoxC(new Vector3(0, 3.25f, 0.6f), new Vector3(2.44f, 0.14f, 0.3f), 0);
            mb.BoxC(new Vector3(0, 0.03f, 0.6f), new Vector3(2.6f, 0.06f, 0.6f), 0);
            Obj(mb, "Frame", go, new[] { Brushed }, Vector3.zero);
            var sheet = PlaceholderFactory.MeshPart(ProceduralMeshes.Quad(), go, new Vector3(0, 1.6f, 0.6f), new Vector3(2.16f, 3.1f, 1), MaterialCache.Get("holo"), "Light");
            var hb = sheet.gameObject.AddComponent<HologramBillboard>();
            hb.content = null; hb.color = accent * 0.8f; hb.scroll = new Vector2(0, -0.05f);
        }

        /// <summary>Boss arena in the city: a cordoned-off plaza with two floodlight towers lighting the boss.</summary>
        public static void BossArena(Transform bossT, float bossHeight, bool mini)
        {
            // the boss moves during the fight: the arena gets its own static root at the boss's spot
            var boss = new GameObject("BossArena").transform;
            boss.SetParent(bossT.parent, false);
            boss.localPosition = bossT.localPosition;
            float span = Mathf.Max(6f, bossHeight * 1.6f);
            foreach (float side in new[] { -1f, 1f })
            {
                var basePos = new Vector3(side * span, 0f, 4.2f);
                var mb = new MeshBuilder(2);
                mb.BoxC(basePos + new Vector3(0, 0.15f, 0), new Vector3(1.2f, 0.3f, 1.2f), 0);
                mb.Tube(basePos + Vector3.up * 0.3f, basePos + Vector3.up * 8f, 0.09f, 0.07f, 8, 0);
                var head = basePos + Vector3.up * 8.2f;
                mb.BoxC(head, new Vector3(1.6f, 0.5f, 0.3f), 0);
                for (int i = 0; i < 3; i++) mb.BoxC(head + new Vector3(-0.5f + i * 0.5f, 0, -0.16f), new Vector3(0.36f, 0.36f, 0.02f), 1);
                Obj(mb, "FloodlightTower", boss, new[] { Steel, CityMaterials.Emissive(new Color(1f, 0.97f, 0.9f), new Color(6f, 5.6f, 5f)) }, Vector3.zero);
                var lg = new GameObject("Floodlight");
                lg.transform.SetParent(boss, false);
                lg.transform.localPosition = head + new Vector3(0, 0, -0.3f);
                lg.transform.LookAt(boss.position + Vector3.up * bossHeight * 0.5f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Spot; l.range = 30f; l.spotAngle = 45f; l.intensity = mini ? 8f : 12f;
                l.color = new Color(1f, 0.96f, 0.9f); l.shadows = LightShadows.None;
                lg.AddComponent<WorldLight>();
            }
            // crowd-control barriers behind the walking line
            var bar = new MeshBuilder(1);
            for (float x = -span + 1f; x < span; x += 2.6f)
            {
                var p = new Vector3(x, 0, 3.6f);
                bar.BoxC(p + new Vector3(0, 1.0f, 0), new Vector3(2.3f, 0.05f, 0.05f), 0);
                bar.BoxC(p + new Vector3(0, 0.25f, 0), new Vector3(2.3f, 0.05f, 0.05f), 0);
                for (float k = -1.1f; k <= 1.11f; k += 0.25f) bar.BoxC(p + new Vector3(k, 0.62f, 0), new Vector3(0.025f, 0.74f, 0.025f), 0);
                bar.BoxC(p + new Vector3(-1.1f, 0.03f, 0), new Vector3(0.06f, 0.06f, 0.6f), 0);
            }
            Obj(bar, "Barriers", boss, new[] { Brushed }, Vector3.zero);
        }
    }
}
