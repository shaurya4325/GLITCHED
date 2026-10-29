using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// "The Glitch" as it first appears: a dark mass of glossy shards with magenta / cyan seams that jitter and snap
    /// out of place, reaching out with tendrils. It pushes through the gaming monitor in the opening cinematic.
    /// </summary>
    public class GlitchEntity : MonoBehaviour
    {
        class Shard { public Transform t; public Vector3 home; public float phase, snap; public bool loose; }
        readonly List<Shard> shards = new List<Shard>();
        readonly List<List<Transform>> tendrils = new List<List<Transform>>();
        public float jitter = 1f;
        public Vector3 reachTarget;
        public float reach;

        public static GlitchEntity Create(Transform parent, Vector3 pos, int count = 70)
        {
            var go = new GameObject("TheGlitch");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var e = go.AddComponent<GlitchEntity>();
            e.Build(count);
            return e;
        }

        void Build(int count)
        {
            var rnd = new System.Random(404);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var dark = CityMaterials.Solid(new Color(0.03f, 0.02f, 0.05f), 0.12f, 0.6f, 0f);
            var seamA = CityMaterials.Emissive(new Color(0.4f, 0.05f, 0.3f), new Color(4f, 0.6f, 3f));
            var seamB = CityMaterials.Emissive(new Color(0.05f, 0.3f, 0.4f), new Color(0.6f, 3.2f, 4f));
            var cube = new MeshBuilder(1);
            cube.BoxC(Vector3.zero, Vector3.one, 0);
            var mesh = cube.ToMesh("GlitchShard");
            for (int i = 0; i < count; i++)
            {
                var dir = Random.onUnitSphere;
                var home = Vector3.Scale(dir, new Vector3(0.45f, 0.6f, 0.35f)) * Mathf.Pow(R(0f, 1f), 0.5f);
                var t = new GameObject("Shard").transform;
                t.SetParent(transform, false);
                t.localPosition = home;
                t.localRotation = Random.rotation;
                float sz = R(0.04f, 0.16f);
                t.localScale = new Vector3(sz, sz * R(0.5f, 1.6f), sz * R(0.4f, 1.2f));
                t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = t.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = i % 5 == 0 ? (i % 10 == 0 ? seamA : seamB) : dark;
                shards.Add(new Shard { t = t, home = home, phase = R(0f, 10f) });
            }
            for (int k = 0; k < 4; k++)
            {
                var chain = new List<Transform>();
                for (int j = 0; j < 10; j++)
                {
                    var t = new GameObject("Tendril").transform;
                    t.SetParent(transform, false);
                    float sz = 0.07f * (1f - j / 12f);
                    t.localScale = Vector3.one * sz;
                    t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = j % 3 == 0 ? seamA : dark;
                    chain.Add(t);
                }
                tendrils.Add(chain);
            }
            var light = new GameObject("GlitchLight").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.type = LightType.Point; light.range = 5f; light.intensity = 2.5f; light.color = new Color(1f, 0.3f, 0.8f);
        }

        void Update()
        {
            float t = Time.time;
            foreach (var s in shards)
            {
                if (s.loose) continue;   // scattered across the room: it lives on the surface it hit
                // most of the time shards drift; now and then one snaps to a wrong place for a frame or two
                if (Random.value < 0.02f * jitter) s.snap = 0.08f;
                s.snap -= Time.deltaTime;
                var drift = new Vector3(Mathf.Sin(t * 2.1f + s.phase), Mathf.Sin(t * 1.7f + s.phase * 1.3f), Mathf.Sin(t * 2.6f + s.phase * 0.7f)) * 0.04f * jitter;
                var off = s.snap > 0 ? Random.insideUnitSphere * 0.25f * jitter : Vector3.zero;
                s.t.localPosition = s.home + drift + off;
                s.t.Rotate(new Vector3(30f, 45f, 20f) * Time.deltaTime * jitter, Space.Self);
            }
            for (int k = 0; k < tendrils.Count; k++)
            {
                var chain = tendrils[k];
                var dir = (Quaternion.Euler(0, k * 90f + t * 20f, 25f * Mathf.Sin(t + k)) * Vector3.forward);
                var target = reach > 0 ? transform.InverseTransformPoint(reachTarget) : dir * 0.9f;
                for (int j = 0; j < chain.Count; j++)
                {
                    float f = (j + 1) / (float)chain.Count;
                    var basePos = Vector3.Lerp(dir * 0.3f, Vector3.Lerp(dir * 0.9f, target, reach), f);
                    basePos += new Vector3(Mathf.Sin(t * 3f + j + k), Mathf.Cos(t * 2.4f + j * 0.7f + k), 0) * 0.06f * f;
                    chain[j].localPosition = basePos;
                }
            }
        }

        /// <summary>
        /// Bursts apart: every shard flies to a different surface of the room (walls, floor, ceiling, the furniture)
        /// and sticks there, glowing veins of corruption spreading out from it. Shards that land on a floater ride
        /// with it when the room lifts off. <paramref name="exclude"/> (the player) is never hit.
        /// </summary>
        public IEnumerator Scatter(Transform room, Transform exclude, float seconds)
        {
            var targets = new List<Renderer>();
            foreach (var r in room.GetComponentsInChildren<Renderer>())
            {
                if (r.transform.IsChildOf(transform) || (exclude != null && r.transform.IsChildOf(exclude))) continue;
                if (r is ParticleSystemRenderer || r.bounds.size.magnitude < 0.15f) continue;
                // only the bedroom itself (the race on the monitor is a whole city built under the same root)
                if (Vector3.Distance(r.bounds.ClosestPoint(transform.position), transform.position) > 5.5f || r.bounds.size.magnitude > 16f) continue;
                targets.Add(r);
            }
            if (targets.Count == 0) yield break;
            foreach (var c in tendrils) foreach (var t in c) t.gameObject.SetActive(false);
            Vector3 c0 = transform.position;
            var roomB = targets[0].bounds;
            foreach (var r in targets) roomB.Encapsulate(r.bounds);
            var flights = new List<(Shard s, Vector3 from, Vector3 to, Vector3 normal, Transform host, float delay)>();
            for (int i = 0; i < shards.Count; i++)
            {
                var s = shards[i];
                // big surfaces (walls, floor, ceiling) get most of it; the rest lands on the things in the room
                var r = i % 3 == 0 ? targets[Random.Range(0, targets.Count)] : Biggest(targets);
                var probe = r.bounds.center + Random.onUnitSphere * r.bounds.extents.magnitude * 2f;
                var to = r.bounds.ClosestPoint(probe);
                if (i % 3 != 0) to = Wallish(roomB, c0);   // a point on the room's own shell
                var normal = (c0 - to).normalized;
                s.loose = true;
                s.t.SetParent(null, true);
                flights.Add((s, s.t.position, to + normal * 0.02f, normal, i % 3 == 0 ? r.transform : room, Random.Range(0f, seconds * 0.35f)));
            }
            var veins = VeinTexture();
            for (float x = 0; x < seconds + 0.6f; x += Time.deltaTime)
            {
                foreach (var f in flights)
                {
                    float k = Mathf.Clamp01((x - f.delay) / (seconds * 0.6f));
                    if (k <= 0f) continue;
                    if (f.s.t.parent == f.host) continue;
                    float e = Easing.Evaluate(Ease.OutCubic, k);
                    var arc = Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
                    f.s.t.position = Vector3.Lerp(f.from, f.to, e) + arc;
                    f.s.t.Rotate(new Vector3(400f, 300f, 200f) * Time.deltaTime * (1f - k), Space.Self);
                    if (k >= 1f)
                    {
                        // it hits and sticks: flattened against the surface, veins of corruption grow out of it
                        f.s.t.SetParent(f.host, true);
                        f.s.t.rotation = Quaternion.LookRotation(-f.normal) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                        f.s.t.localScale *= 1.8f;   // big enough to read on the wall
                        if (Random.value < 0.7f) StartCoroutine(GrowVein(f.host, f.to + f.normal * 0.01f, f.normal, veins));
                        if (Random.value < 0.15f) VFX.Burst(f.to, Random.value < 0.5f ? new Color(1f, 0.3f, 0.85f) : new Color(0.3f, 0.9f, 1f), 6, "spark", 2.5f, 0.12f, 0.4f);
                    }
                }
                yield return null;
            }
        }

        static Renderer Biggest(List<Renderer> rs)
        {
            Renderer best = rs[0];
            foreach (var r in rs) if (r.bounds.size.sqrMagnitude > best.bounds.size.sqrMagnitude) best = r;
            return best;
        }

        /// <summary>A random point on the inside of the room's box: back wall, side walls, ceiling or floor.</summary>
        static Vector3 Wallish(Bounds b, Vector3 from)
        {
            var p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), Random.Range(b.min.z, b.max.z));
            switch (Random.Range(0, 6))
            {
                case 0: p.x = b.min.x + 0.02f; break;
                case 1: p.x = b.max.x - 0.02f; break;
                case 2: case 3: p.z = b.max.z - 0.02f; break;   // the wall behind the desk, in shot
                case 4: p.y = b.max.y - 0.02f; break;
                default: p.y = b.min.y + 0.02f; break;
            }
            return p;
        }

        static Texture2D veinTex;
        static Texture2D VeinTexture()
        {
            if (veinTex != null) return veinTex;
            const int N = 128;
            var px = new Color32[N * N];
            var rnd = new System.Random(7);
            // a few branching random walks from the middle: glitch veins in the paint
            for (int w = 0; w < 9; w++)
            {
                float x = N / 2, y = N / 2, a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                for (int s = 0; s < 70; s++)
                {
                    a += ((float)rnd.NextDouble() - 0.5f) * 0.9f;
                    x += Mathf.Cos(a) * 0.9f; y += Mathf.Sin(a) * 0.9f;
                    if (x < 1 || y < 1 || x >= N - 1 || y >= N - 1) break;
                    float fade = 1f - s / 70f;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int i = ((int)y + dy) * N + (int)x + dx;
                            byte v = (byte)Mathf.Max(px[i].a, (dx == 0 && dy == 0 ? 255 : 120) * fade);
                            px[i] = new Color32(255, 255, 255, v);
                        }
                }
            }
            for (int i = 0; i < px.Length; i++) px[i] = new Color32((byte)(px[i].a), (byte)(px[i].a), (byte)(px[i].a), px[i].a);
            veinTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "GlitchVeins" };
            veinTex.SetPixels32(px); veinTex.Apply();
            return veinTex;
        }

        IEnumerator GrowVein(Transform host, Vector3 pos, Vector3 normal, Texture2D tex)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "GlitchVein";
            q.transform.SetParent(host, true);
            q.transform.position = pos;
            q.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            var m = MaterialCache.Instance("padd");
            m.SetTexture("_BaseMap", tex);
            var col = Random.value < 0.6f ? new Color(1.6f, 0.25f, 1.3f) : new Color(0.25f, 1.2f, 1.8f);
            m.SetColor("_BaseColor", col);
            q.GetComponent<Renderer>().sharedMaterial = m;
            float size = Random.Range(0.6f, 1.5f);
            for (float x = 0; x < 2.2f; x += Time.deltaTime)
            {
                if (q == null) yield break;
                float k = Easing.Evaluate(Ease.OutCubic, x / 2.2f);
                q.transform.localScale = Vector3.one * size * k / Mathf.Max(0.001f, host.lossyScale.x);
                // the veins pulse as they spread
                m.SetColor("_BaseColor", col * (0.6f + 0.4f * Mathf.Sin(Time.time * 9f + size * 10f)));
                yield return null;
            }
            while (q != null) { m.SetColor("_BaseColor", col * (0.55f + 0.35f * Mathf.Sin(Time.time * 5f + size * 10f))); yield return null; }
        }

        /// <summary>Tears every scattered shard off the room and pulls them into a whirlpool at <paramref name="center"/>.</summary>
        public IEnumerator Reform(Vector3 center, float seconds)
        {
            transform.position = center;
            var from = new List<Vector3>();
            foreach (var s in shards) { from.Add(s.t.position); s.t.SetParent(transform, true); }
            for (float x = 0; x < seconds; x += Time.deltaTime)
            {
                float k = Easing.Evaluate(Ease.InCubic, x / seconds);
                for (int i = 0; i < shards.Count; i++)
                {
                    var s = shards[i];
                    // spiral in around the screen
                    float a = (1f - k) * 6f + i;
                    var swirl = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (1f - k) * 0.6f;
                    s.t.position = Vector3.Lerp(from[i], center + Vector3.Scale(s.home, new Vector3(1.2f, 1.2f, 0.4f)), k) + swirl;
                    s.t.Rotate(new Vector3(300f, 200f, 100f) * Time.deltaTime, Space.Self);
                }
                yield return null;
            }
            foreach (var s in shards) s.loose = false;
            foreach (var c in tendrils) foreach (var t in c) t.gameObject.SetActive(true);
        }

        /// <summary>Pushes out of a surface: grows from nothing while moving from 'from' to 'to'.</summary>
        public IEnumerator Emerge(Vector3 from, Vector3 to, float seconds)
        {
            for (float x = 0; x < seconds; x += Time.deltaTime)
            {
                float k = Easing.Evaluate(Ease.OutCubic, x / seconds);
                transform.position = Vector3.Lerp(from, to, k);
                transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, k);
                jitter = Mathf.Lerp(2.5f, 1f, k);
                yield return null;
            }
            transform.position = to;
            transform.localScale = Vector3.one;
        }
    }
}
