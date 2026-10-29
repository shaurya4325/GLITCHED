using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Natural landscape pieces generated in code: heightfield terrain (chunked), conifers and broadleaf trees from
    /// leaf cards, rocks, wind turbines, power pylons with sagging cables and an observatory. Static pieces are merged
    /// into chunk meshes (few draw calls).
    /// </summary>
    public class NatureGen
    {
        public const int Bark = 0, Leaves = 1, RockM = 2, White = 3, Steel = 4, CableM = 5, Count = 6;

        readonly Transform parent;
        readonly System.Random rnd;
        readonly float glitch;
        readonly Dictionary<int, MeshBuilder> chunks = new Dictionary<int, MeshBuilder>();
        public float chunkSize = 80f;

        public NatureGen(Transform parent, System.Random rnd, float glitch = 1f)
        {
            this.parent = parent; this.rnd = rnd; this.glitch = glitch;
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        MeshBuilder At(float x)
        {
            int k = Mathf.FloorToInt(x / chunkSize);
            if (!chunks.TryGetValue(k, out var mb)) chunks[k] = mb = new MeshBuilder(Count);
            return mb;
        }

        Material[] Mats() => new[]
        {
            CityMaterials.PBR("Bark014", new Color(0.85f, 0.8f, 0.75f), 1.2f, 1f, 0f, glitch),
            CityMaterials.Leaves(1),
            CityMaterials.PBR("Rock058", new Color(0.78f, 0.76f, 0.74f), 3f, 1f, 0f, glitch),
            CityMaterials.Solid(new Color(0.9f, 0.91f, 0.92f), 0.35f, 0f, glitch),
            CityMaterials.PBR("PaintedMetal004", new Color(0.45f, 0.46f, 0.48f), 1.5f, 0.8f, 0.6f, glitch),
            CityMaterials.Solid(new Color(0.06f, 0.06f, 0.06f), 0.4f, 0.8f, 0.5f),
        };

        public void Flush(string name)
        {
            var mats = Mats();
            foreach (var kv in chunks) SceneSpawn.MarkStatic(kv.Value.ToObject($"{name}_{kv.Key}", parent, mats));
            chunks.Clear();
        }

        // ------------------------------------------------------------------ terrain
        /// <summary>Builds a heightfield over [x0,x1]x[z0,z1] with the given cell size, split into chunks along x.</summary>
        public void Terrain(string name, float x0, float x1, float z0, float z1, float cell, System.Func<float, float, float> height, Material mat, float chunk = 120f)
        {
            for (float cx = x0; cx < x1; cx += chunk)
            {
                float ex = Mathf.Min(cx + chunk, x1);
                int nx = Mathf.Max(1, Mathf.CeilToInt((ex - cx) / cell));
                int nz = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / cell));
                var verts = new Vector3[(nx + 1) * (nz + 1)];
                var uvs = new Vector2[verts.Length];
                for (int j = 0; j <= nz; j++)
                    for (int i = 0; i <= nx; i++)
                    {
                        float x = Mathf.Lerp(cx, ex, i / (float)nx), z = Mathf.Lerp(z0, z1, j / (float)nz);
                        verts[j * (nx + 1) + i] = new Vector3(x, height(x, z), z);
                        uvs[j * (nx + 1) + i] = new Vector2(x, z);
                    }
                var tris = new int[nx * nz * 6];
                int t = 0;
                for (int j = 0; j < nz; j++)
                    for (int i = 0; i < nx; i++)
                    {
                        int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                        // clockwise seen from above (Unity front faces)
                        tris[t++] = a; tris[t++] = c; tris[t++] = b;
                        tris[t++] = b; tris[t++] = c; tris[t++] = d;
                    }
                var mesh = new Mesh { name = $"{name}_{cx:0}" };
                if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go = new GameObject(mesh.name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                SceneMeshes.Register(mesh);
                SceneSpawn.MarkStatic(go);
            }
        }

        /// <summary>Fractal value noise in [0,1].</summary>
        public static float Fbm(float x, float z, int octaves = 4)
        {
            float sum = 0, amp = 0.5f, f = 1f, norm = 0;
            for (int o = 0; o < octaves; o++) { sum += Mathf.PerlinNoise(x * f + 13.1f * o, z * f - 7.7f * o) * amp; norm += amp; amp *= 0.5f; f *= 2.03f; }
            return sum / norm;
        }

        /// <summary>Ridged noise for mountain ranges in [0,1].</summary>
        public static float Ridge(float x, float z, int octaves = 5)
        {
            float sum = 0, amp = 0.5f, f = 1f, norm = 0;
            for (int o = 0; o < octaves; o++)
            {
                float n = 1f - Mathf.Abs(Mathf.PerlinNoise(x * f + 3.3f * o, z * f + 9.1f * o) * 2f - 1f);
                sum += n * n * amp; norm += amp; amp *= 0.5f; f *= 2.1f;
            }
            return sum / norm;
        }

        // ------------------------------------------------------------------ vegetation
        public void Conifer(Vector3 p, float h)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.18f * h / 12f + 0.06f, 0.04f, h * 0.92f, 7, Bark, false);
            var uv = new Rect(0.5f, 0.5f, 0.5f, 0.5f);   // hedge / needle cell of the atlas
            int tiers = Mathf.RoundToInt(5 + h * 0.25f);
            for (int t = 0; t < tiers; t++)
            {
                float k = t / (float)(tiers - 1);
                float y = Mathf.Lerp(h * 0.22f, h * 0.98f, k);
                float r = Mathf.Lerp(h * 0.24f, h * 0.04f, k);
                int cards = Mathf.Max(3, Mathf.RoundToInt(7 * (1 - k * 0.6f)));
                for (int c = 0; c < cards; c++)
                {
                    float a = c / (float)cards * Mathf.PI * 2f + t * 0.7f;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var center = p + Vector3.up * (y - r * 0.15f) + dir * r * 0.5f;
                    var right = Vector3.Cross(Vector3.up, dir).normalized * r * 0.55f;
                    var down = (dir * r * 0.5f + Vector3.down * r * 0.35f);
                    var n = (dir + Vector3.up * 0.8f).normalized;
                    mb.Card(center, right, -down * 0.9f, Leaves, n, uv);
                    mb.Card(center + Vector3.up * r * 0.12f, dir * r * 0.55f, Vector3.Cross(dir, Vector3.up) * r * 0.5f, Leaves, Vector3.up, uv);
                }
            }
        }

        public void Broadleaf(Vector3 p, float h)
        {
            var mb = At(p.x);
            mb.Cylinder(p, 0.15f * h / 7f, 0.1f * h / 7f, h * 0.45f, 8, Bark, false);
            var crown = p + Vector3.up * h * 0.68f;
            var uv = new Rect(0f, 0f, 0.5f, 0.5f);
            int cards = Mathf.RoundToInt(28 + h * 3);
            for (int i = 0; i < cards; i++)
            {
                var n = Random.onUnitSphere;
                n.y = Mathf.Abs(n.y) * 0.8f + n.y * 0.2f;
                var c = crown + Vector3.Scale(n, new Vector3(h * 0.33f, h * 0.27f, h * 0.33f)) * R(0.5f, 1f);
                float s = R(0.9f, 1.4f) * h / 7f;
                var right = Vector3.Cross(n, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.01f) right = Vector3.right;
                var up = Vector3.Cross(right, n).normalized;
                mb.Card(c, right * s, up * s, Leaves, (n + Vector3.up * 0.6f).normalized, uv);
            }
        }

        public void Rock(Vector3 p, float size)
        {
            At(p.x).Sphere(p + Vector3.up * size * 0.25f, new Vector3(size, size * R(0.5f, 0.8f), size * R(0.7f, 1f)), 7, 10, RockM, 0.25f, rnd.Next(100));
        }

        public void GrassTuft(Vector3 p, float size)
        {
            var uv = new Rect(0f, 0.5f, 0.5f, 0.5f);
            var mb = At(p.x);
            float a = R(0, Mathf.PI);
            var d1 = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * size;
            var d2 = new Vector3(-d1.z, 0, d1.x);
            mb.Card(p + Vector3.up * size * 0.5f, d1, Vector3.up * size * 0.5f, Leaves, Vector3.up, uv);
            mb.Card(p + Vector3.up * size * 0.5f, d2, Vector3.up * size * 0.5f, Leaves, Vector3.up, uv);
        }

        // ------------------------------------------------------------------ structures
        /// <summary>A three-bladed wind turbine; the rotor spins (backwards when <paramref name="glitched"/>).</summary>
        public void WindTurbine(Vector3 p, float h, bool glitched)
        {
            var mb = new MeshBuilder(1);
            mb.Cylinder(p, h * 0.035f, h * 0.018f, h, 16, 0);
            var top = p + Vector3.up * h;
            mb.BoxC(top + new Vector3(0, 0, h * 0.02f), new Vector3(h * 0.05f, h * 0.05f, h * 0.12f), 0);
            SceneSpawn.MarkStatic(mb.ToObject("TurbineTower", parent, new[] { Mats()[White] }));
            var rotor = new GameObject("TurbineRotor").transform;
            rotor.SetParent(parent, false);
            rotor.localPosition = top + new Vector3(0, 0, -h * 0.05f);
            var blades = new MeshBuilder(1);
            blades.Sphere(Vector3.zero, Vector3.one * h * 0.03f, 8, 10, 0);
            for (int b = 0; b < 3; b++)
            {
                var rot = Quaternion.Euler(0, 0, b * 120f);
                var tip = rot * Vector3.up * h * 0.55f;
                var side = rot * Vector3.right * h * 0.022f;
                blades.Quad(side * 1.4f, -side * 1.4f, -side * 0.4f + tip, side * 0.4f + tip, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, 0);
                blades.Quad(-side * 1.4f, side * 1.4f, side * 0.4f + tip, -side * 0.4f + tip, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, 0);
            }
            blades.ToObject("Blades", rotor, new[] { Mats()[White] });
            rotor.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 0, (glitched ? 1f : -1f) * R(40f, 60f));
        }

        /// <summary>Lattice power pylon (simplified) at p; returns the cable attachment points.</summary>
        public Vector3[] Pylon(Vector3 p, float h)
        {
            var mb = At(p.x);
            float w = h * 0.12f;
            var top = p + Vector3.up * h;
            for (int i = 0; i < 4; i++)
            {
                var f = new Vector3(i % 2 == 0 ? -w : w, 0, i < 2 ? -w : w);
                mb.Tube(p + f, top + f * 0.15f, 0.12f, 0.06f, 5, Steel);
            }
            for (float y = h * 0.15f; y < h * 0.9f; y += h * 0.18f)
            {
                float k = 1f - y / h * 0.85f;
                mb.Tube(p + new Vector3(-w * k, y, -w * k), p + new Vector3(w * k, y + h * 0.09f, -w * k), 0.05f, 0.05f, 4, Steel);
                mb.Tube(p + new Vector3(w * k, y, -w * k), p + new Vector3(-w * k, y + h * 0.09f, -w * k), 0.05f, 0.05f, 4, Steel);
            }
            var arms = new[] { 0.72f, 0.88f };
            var pts = new List<Vector3>();
            foreach (var a in arms)
            {
                float y = h * a, span = h * 0.28f;
                mb.Tube(p + new Vector3(-span, y, 0), p + new Vector3(span, y, 0), 0.08f, 0.08f, 4, Steel);
                pts.Add(p + new Vector3(-span, y - 0.6f, 0)); pts.Add(p + new Vector3(span, y - 0.6f, 0));
            }
            return pts.ToArray();
        }

        public void Cable(Vector3 a, Vector3 b, float sag)
        {
            var mb = At((a.x + b.x) / 2);
            const int seg = 10;
            for (int i = 0; i < seg; i++)
            {
                float t0 = i / (float)seg, t1 = (i + 1) / (float)seg;
                var p0 = Vector3.Lerp(a, b, t0) + Vector3.down * sag * 4f * t0 * (1 - t0);
                var p1 = Vector3.Lerp(a, b, t1) + Vector3.down * sag * 4f * t1 * (1 - t1);
                mb.Tube(p0, p1, 0.04f, 0.04f, 4, CableM);
            }
        }

        /// <summary>An observatory: round concrete drum, metal dome with an opening slit.</summary>
        public void Observatory(Vector3 p, float r)
        {
            var mb = new MeshBuilder(3);
            mb.Cylinder(p, r, r, r * 0.9f, 28, 0, false);
            mb.Sphere(p + Vector3.up * r * 0.9f, new Vector3(r * 1.02f, r * 0.95f, r * 1.02f), 12, 28, 1);
            mb.BoxC(p + new Vector3(0, r * 1.5f, -r * 0.55f), new Vector3(r * 0.35f, r * 0.9f, r * 0.6f), 2);
            mb.Cylinder(p + Vector3.down * 0.5f, r * 1.3f, r * 1.3f, 0.5f, 28, 0);
            SceneSpawn.MarkStatic(mb.ToObject("Observatory", parent, new[]
            {
                CityMaterials.PBR("Concrete046", new Color(0.86f, 0.85f, 0.82f), 3f, 1f, 0f, glitch),
                CityMaterials.Solid(new Color(0.82f, 0.84f, 0.86f), 0.28f, 0.85f, glitch),
                CityMaterials.Solid(new Color(0.05f, 0.05f, 0.06f), 0.5f, 0f, glitch),
            }));
        }
    }
}
