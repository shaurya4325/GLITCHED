using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Glitched creatures crawling over the building fronts while a region is corrupted:
    ///   Crawler   - an eight-legged glitch spider, glossy black with magenta / cyan seams, scuttles and pauses
    ///   Centipede - a long segmented body snaking across the facade
    ///   Mites     - a small swarm of glitch beetles clustering together
    /// They fade out as the region is restored. Count follows the graphics tier.
    /// </summary>
    public class WallCrawlers : MonoBehaviour
    {
        [System.Serializable] public class WallRect { public float x0, x1, y0, y1, z; }
        public List<WallRect> walls = new List<WallRect>();
        public int count = 14;

        enum Kind { Crawler, Centipede, Mites }

        class Creature
        {
            public Kind kind;
            public Transform root;
            public WallRect wall;
            public Vector2 pos, target;
            public float speed, pause, heading;
            public readonly List<Transform> legs = new List<Transform>();
            public readonly List<Transform> segments = new List<Transform>();
            public readonly List<Vector2> trail = new List<Vector2>();
            public readonly List<Vector3> mites = new List<Vector3>();
        }

        /// <summary>Starts collecting building walls (call before building a row, then Finish).</summary>
        public static void BeginCollect() => BuildingGen.CollectWalls = new List<BuildingGen.Wall>();

        /// <summary>Creates the crawlers for the walls collected since BeginCollect.</summary>
        public static WallCrawlers Finish(Transform parent, int count)
        {
            var list = BuildingGen.CollectWalls;
            BuildingGen.CollectWalls = null;
            if (list == null || list.Count == 0) return null;
            var go = new GameObject("WallCrawlers");
            go.transform.SetParent(parent, false);
            var wc = go.AddComponent<WallCrawlers>();
            foreach (var w in list) if (w.y1 > w.y0 + 2f) wc.walls.Add(new WallRect { x0 = w.x0, x1 = w.x1, y0 = w.y0, y1 = w.y1, z = w.z });
            wc.count = count;
            return wc;
        }

        readonly List<Creature> creatures = new List<Creature>();
        Mesh cube, sphere;
        Material shell, seamA, seamB;
        System.Random rnd;

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        void Start()
        {
            if (walls.Count == 0) return;
            rnd = new System.Random(GetInstanceID());
            var cb = new MeshBuilder(1); cb.BoxC(Vector3.zero, Vector3.one, 0); cube = cb.ToMesh("CrawlerCube");
            var sb = new MeshBuilder(1); sb.Sphere(Vector3.zero, Vector3.one * 0.5f, 8, 12, 0); sphere = sb.ToMesh("CrawlerSphere");
            shell = CityMaterials.Solid(new Color(0.03f, 0.03f, 0.04f), 0.18f, 0.4f, 0f);
            seamA = CityMaterials.Emissive(new Color(0.4f, 0.05f, 0.3f), new Color(3.5f, 0.5f, 2.6f));
            seamB = CityMaterials.Emissive(new Color(0.05f, 0.3f, 0.4f), new Color(0.5f, 2.8f, 3.5f));
            float density = Services.Graphics != null ? Services.Graphics.DensityMultiplier : 1f;
            int n = Mathf.Max(3, Mathf.RoundToInt(count * density));
            for (int i = 0; i < n; i++) Spawn((Kind)(i % 3));
        }

        Transform Part(Transform parent, Mesh m, Material mat, Vector3 pos, Vector3 scale)
        {
            var t = new GameObject("Part").transform;
            t.SetParent(parent, false);
            t.localPosition = pos; t.localScale = scale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = m;
            var r = t.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        void Spawn(Kind kind)
        {
            var w = walls[rnd.Next(walls.Count)];
            var c = new Creature { kind = kind, wall = w, speed = R(0.8f, 1.8f) };
            c.root = new GameObject("Crawler_" + kind).transform;
            c.root.SetParent(transform, false);
            c.pos = new Vector2(R(w.x0, w.x1), R(w.y0, w.y1));
            c.target = c.pos;
            // creatures lie flat on the wall: local up = out of the wall (-Z), local forward = direction of travel
            switch (kind)
            {
                case Kind.Crawler:
                {
                    float s = R(0.8f, 1.4f);
                    var body = new GameObject("Body").transform; body.SetParent(c.root, false); body.localScale = Vector3.one * s;
                    Part(body, sphere, shell, new Vector3(0, 0.12f, -0.15f), new Vector3(0.5f, 0.3f, 0.65f));
                    Part(body, sphere, shell, new Vector3(0, 0.1f, 0.25f), new Vector3(0.28f, 0.22f, 0.3f));
                    Part(body, cube, seamA, new Vector3(0, 0.27f, -0.15f), new Vector3(0.04f, 0.02f, 0.5f));
                    foreach (float x in new[] { -0.07f, 0.07f }) Part(body, sphere, seamB, new Vector3(x, 0.16f, 0.38f), Vector3.one * 0.05f);
                    for (int i = 0; i < 8; i++)
                    {
                        float side = i < 4 ? -1f : 1f;
                        var hip = new GameObject("Leg").transform; hip.SetParent(body, false);
                        hip.localPosition = new Vector3(side * 0.18f, 0.1f, 0.2f - (i % 4) * 0.13f);
                        hip.localRotation = Quaternion.Euler(0, side * (60f - (i % 4) * 30f), 0);
                        Part(hip, cube, shell, new Vector3(side * 0.18f, 0.08f, 0), new Vector3(0.34f, 0.025f, 0.025f)).localRotation = Quaternion.Euler(0, 0, side * 25f);
                        Part(hip, cube, shell, new Vector3(side * 0.42f, -0.02f, 0), new Vector3(0.3f, 0.02f, 0.02f)).localRotation = Quaternion.Euler(0, 0, side * -40f);
                        c.legs.Add(hip);
                    }
                    break;
                }
                case Kind.Centipede:
                {
                    int segs = rnd.Next(9, 15);
                    for (int i = 0; i < segs; i++)
                    {
                        var seg = new GameObject("Segment").transform; seg.SetParent(transform, false);
                        float sz = Mathf.Lerp(0.32f, 0.16f, i / (float)segs);
                        Part(seg, sphere, shell, new Vector3(0, 0.08f, 0), new Vector3(sz, sz * 0.6f, sz * 0.9f));
                        if (i % 2 == 0) Part(seg, cube, i % 4 == 0 ? seamA : seamB, new Vector3(0, 0.08f + sz * 0.3f, 0), new Vector3(sz * 0.3f, 0.02f, sz * 0.5f));
                        foreach (float x in new[] { -1f, 1f }) Part(seg, cube, shell, new Vector3(x * sz * 0.7f, 0.03f, 0), new Vector3(sz * 0.8f, 0.015f, 0.015f));
                        c.segments.Add(seg);
                    }
                    c.speed *= 1.3f;
                    break;
                }
                default:
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var m = new GameObject("Mite").transform; m.SetParent(c.root, false);
                        Part(m, sphere, shell, Vector3.zero, new Vector3(0.12f, 0.07f, 0.16f));
                        Part(m, sphere, i % 2 == 0 ? seamA : seamB, new Vector3(0, 0.03f, 0.07f), Vector3.one * 0.03f);
                        c.mites.Add(new Vector3(R(-0.5f, 0.5f), 0, R(-0.5f, 0.5f)));
                        c.legs.Add(m);
                    }
                    c.speed *= 0.7f;
                    break;
                }
            }
            creatures.Add(c);
        }

        static Quaternion OnWall(float heading) => Quaternion.LookRotation(new Vector3(Mathf.Cos(heading), Mathf.Sin(heading), 0), Vector3.back);

        void Update()
        {
            float c = Services.Corruption != null ? Services.Corruption.Value : 1f;
            bool show = c > 0.3f;
            float dt = Time.deltaTime, t = Time.time;
            foreach (var k in creatures)
            {
                if (k.root.gameObject.activeSelf != show) k.root.gameObject.SetActive(show);
                foreach (var s in k.segments) if (s.gameObject.activeSelf != show) s.gameObject.SetActive(show);
                if (!show) continue;
                // wander: pick a nearby point on the wall, scuttle there, pause, repeat
                if (k.pause > 0) { k.pause -= dt; }
                else
                {
                    var d = k.target - k.pos;
                    if (d.magnitude < 0.2f)
                    {
                        k.target = new Vector2(Mathf.Clamp(k.pos.x + R(-6f, 6f), k.wall.x0, k.wall.x1), Mathf.Clamp(k.pos.y + R(-4f, 4f), k.wall.y0, k.wall.y1));
                        if (k.kind == Kind.Crawler && rnd.NextDouble() < 0.5) k.pause = R(0.4f, 2.2f);
                    }
                    else
                    {
                        float want = Mathf.Atan2(d.y, d.x);
                        k.heading = Mathf.LerpAngle(k.heading * Mathf.Rad2Deg, want * Mathf.Rad2Deg, dt * 6f) * Mathf.Deg2Rad;
                        k.pos += new Vector2(Mathf.Cos(k.heading), Mathf.Sin(k.heading)) * k.speed * dt;
                    }
                }
                var p3 = new Vector3(k.pos.x, k.pos.y, k.wall.z - 0.04f);
                bool moving = k.pause <= 0;
                switch (k.kind)
                {
                    case Kind.Crawler:
                        k.root.SetPositionAndRotation(p3, OnWall(k.heading));
                        for (int i = 0; i < k.legs.Count; i++)
                        {
                            float ph = (i % 2 == 0 ? 0 : Mathf.PI) + i;
                            float swing = moving ? Mathf.Sin(t * 14f + ph) * 18f : Mathf.Sin(t * 2f + ph) * 2f;
                            float side = i < 4 ? -1f : 1f;
                            k.legs[i].localRotation = Quaternion.Euler(0, side * (60f - (i % 4) * 30f) + swing, moving ? Mathf.Abs(Mathf.Cos(t * 14f + ph)) * 10f : 0);
                        }
                        break;
                    case Kind.Centipede:
                        k.trail.Insert(0, k.pos);
                        if (k.trail.Count > 200) k.trail.RemoveAt(k.trail.Count - 1);
                        for (int i = 0; i < k.segments.Count; i++)
                        {
                            int idx = Mathf.Min(k.trail.Count - 1, i * 5);
                            var a = k.trail[idx];
                            var b = k.trail[Mathf.Min(k.trail.Count - 1, idx + 2)];
                            float hd = (a - b).sqrMagnitude > 1e-6f ? Mathf.Atan2(a.y - b.y, a.x - b.x) : k.heading;
                            hd += Mathf.Sin(t * 8f - i * 0.6f) * 0.25f;
                            k.segments[i].SetPositionAndRotation(new Vector3(a.x, a.y, k.wall.z - 0.04f), OnWall(hd));
                        }
                        break;
                    default:
                        k.root.SetPositionAndRotation(p3, OnWall(k.heading));
                        for (int i = 0; i < k.legs.Count; i++)
                        {
                            var o = k.mites[i];
                            float jx = Mathf.Sin(t * (5f + i) + i * 2f) * 0.15f, jz = Mathf.Cos(t * (4f + i) + i) * 0.15f;
                            if (rnd.NextDouble() < 0.01) k.mites[i] = new Vector3(R(-0.6f, 0.6f), 0, R(-0.6f, 0.6f));   // twitch to a new spot
                            k.legs[i].localPosition = Vector3.Lerp(k.legs[i].localPosition, o + new Vector3(jx, 0.02f, jz), dt * 10f);
                            k.legs[i].localRotation = Quaternion.Euler(0, t * 90f * (i % 2 == 0 ? 1 : -1), 0);
                        }
                        break;
                }
            }
        }
    }
}
