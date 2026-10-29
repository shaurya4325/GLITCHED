using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Small procedural meshes for placeholders (cones, crystals, rounded quads, prisms).</summary>
    public static class ProceduralMeshes
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        public static System.Func<Mesh, Mesh> OnCreated;   // editor: save as asset

        static Mesh Store(string key, Mesh m)
        {
            m.name = key;
            m.RecalculateBounds();
            if (OnCreated != null) m = OnCreated(m);
            cache[key] = m;
            return m;
        }

        public static Mesh Cone(int segments = 14)
        {
            string key = "Cone" + segments;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var v = new List<Vector3>(); var t = new List<int>(); var n = new List<Vector3>();
            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2, a1 = (i + 1) / (float)segments * Mathf.PI * 2;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f);
                Vector3 tip = new Vector3(0, 0.5f, 0);
                int b = v.Count;
                v.Add(p0); v.Add(tip); v.Add(p1);
                Vector3 nn = Vector3.Cross(tip - p0, p1 - p0).normalized;
                n.Add(-nn); n.Add(-nn); n.Add(-nn);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(new Vector3(0, -0.5f, 0));
                n.Add(Vector3.down); n.Add(Vector3.down); n.Add(Vector3.down);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }
            m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            FixWinding(m);
            return Store(key, m);
        }

        /// <summary>A gem / crystal (octahedron, slightly stretched).</summary>
        public static Mesh Crystal()
        {
            const string key = "Crystal";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            Vector3[] p = { new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(-0.5f, 0, 0), new Vector3(0, 0, -0.5f), new Vector3(0, -0.5f, 0) };
            int[] faces = { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < faces.Length; i++) { v.Add(p[faces[i]]); t.Add(i); }
            m = new Mesh();
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            FixWinding(m);
            return Store(key, m);
        }

        /// <summary>A triangular prism (roof, ramps, hat brims).</summary>
        public static Mesh Wedge()
        {
            const string key = "Wedge";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            Vector3 a = new Vector3(-0.5f, -0.5f, -0.5f), b = new Vector3(0.5f, -0.5f, -0.5f), c = new Vector3(-0.5f, 0.5f, -0.5f);
            Vector3 d = new Vector3(-0.5f, -0.5f, 0.5f), e = new Vector3(0.5f, -0.5f, 0.5f), f = new Vector3(-0.5f, 0.5f, 0.5f);
            Vector3[][] quads = { new[] { a, b, e, d }, new[] { a, d, f, c }, new[] { b, c, f, e } };
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (var q in quads) { int s = v.Count; v.AddRange(q); t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 }); }
            int s2 = v.Count; v.Add(a); v.Add(c); v.Add(b); t.AddRange(new[] { s2, s2 + 1, s2 + 2 });
            s2 = v.Count; v.Add(d); v.Add(e); v.Add(f); t.AddRange(new[] { s2, s2 + 1, s2 + 2 });
            m = new Mesh();
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals();
            FixWinding(m);
            return Store(key, m);
        }

        /// <summary>A flat quad facing -Z (for decals, impostors, billboards).</summary>
        public static Mesh Quad()
        {
            const string key = "QuadZ";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Mesh();
            m.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            return Store(key, m);
        }

        /// <summary>A bottom-anchored quad (pivot at the bottom centre) for skyline impostor cards.</summary>
        public static Mesh CardBottom()
        {
            const string key = "CardBottom";
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Mesh();
            m.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(-0.5f, 1, 0) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.bounds = new Bounds(new Vector3(0, 0.5f, 0), new Vector3(1.5f, 1.2f, 1.5f));
            return Store(key, m);
        }

        /// <summary>Makes every triangle face outward from the mesh centre (robust for convex shapes).</summary>
        static void FixWinding(Mesh m)
        {
            var v = m.vertices; var t = m.triangles;
            Vector3 c = Vector3.zero; foreach (var p in v) c += p; c /= Mathf.Max(1, v.Length);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], d = v[t[i + 2]];
                Vector3 nn = Vector3.Cross(b - a, d - a);
                Vector3 mid = (a + b + d) / 3f;
                if (Vector3.Dot(nn, mid - c) < 0) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
            }
            m.triangles = t;
            m.RecalculateNormals();
        }
    }
}
