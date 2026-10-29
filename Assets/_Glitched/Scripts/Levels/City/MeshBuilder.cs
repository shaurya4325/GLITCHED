using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Small procedural mesh toolkit for the hand-made GLITCHED city: boxes with UVs in metres (so the facade shader
    /// lines up floors and windows), cylinders, spheres and leaf cards, collected into one mesh with sub-meshes.
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> norms = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<List<int>> subs = new List<List<int>>();

        public MeshBuilder(int subMeshes = 1) { for (int i = 0; i < subMeshes; i++) subs.Add(new List<int>()); }

        public int VertexCount => verts.Count;

        List<int> Sub(int i) { while (subs.Count <= i) subs.Add(new List<int>()); return subs[i]; }

        /// <summary>Quad a-b-c-d (counter-clockwise seen from the front), with its own UVs.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, int sub)
        {
            var n = Vector3.Cross(d - a, b - a).normalized;   // Unity is left-handed: clockwise triangles face the viewer
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
            uvs.Add(ua); uvs.Add(ub); uvs.Add(uc); uvs.Add(ud);
            var t = Sub(sub);
            t.Add(i); t.Add(i + 2); t.Add(i + 1);
            t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }

        [System.Flags] public enum Faces { None = 0, Front = 1, Back = 2, Left = 4, Right = 8, Top = 16, Bottom = 32, Sides = Front | Back | Left | Right, All = 63 }

        /// <summary>
        /// Axis-aligned box (min corner + size). Side faces get facade UVs: u = metres along the face, v = metres above
        /// <paramref name="baseY"/>; top/bottom get xz in metres. Front is the -Z face (towards the camera).
        /// </summary>
        public void Box(Vector3 min, Vector3 size, int sideSub, int topSub, float baseY = float.NaN, Faces faces = Faces.All, float uOffset = 0f)
        {
            float x0 = min.x, y0 = min.y, z0 = min.z, x1 = min.x + size.x, y1 = min.y + size.y, z1 = min.z + size.z;
            float b = float.IsNaN(baseY) ? y0 : baseY;
            float v0 = y0 - b, v1 = y1 - b;
            float w = size.x, d = size.z;
            float u = uOffset;
            // walk the perimeter so window bays continue round corners: front (+x), right (+z), back (-x), left (-z)
            if ((faces & Faces.Front) != 0)
                Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
                     new Vector2(u, v0), new Vector2(u + w, v0), new Vector2(u + w, v1), new Vector2(u, v1), sideSub);
            u += w;
            if ((faces & Faces.Right) != 0)
                Quad(new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0),
                     new Vector2(u, v0), new Vector2(u + d, v0), new Vector2(u + d, v1), new Vector2(u, v1), sideSub);
            u += d;
            if ((faces & Faces.Back) != 0)
                Quad(new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
                     new Vector2(u, v0), new Vector2(u + w, v0), new Vector2(u + w, v1), new Vector2(u, v1), sideSub);
            u += w;
            if ((faces & Faces.Left) != 0)
                Quad(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x0, y1, z1),
                     new Vector2(u, v0), new Vector2(u + d, v0), new Vector2(u + d, v1), new Vector2(u, v1), sideSub);
            if ((faces & Faces.Top) != 0)
                Quad(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1),
                     new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1), topSub);
            if ((faces & Faces.Bottom) != 0)
                Quad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y0, z0), new Vector3(x0, y0, z0),
                     new Vector2(x0, z1), new Vector2(x1, z1), new Vector2(x1, z0), new Vector2(x0, z0), topSub);
        }

        /// <summary>Box from centre + size, all faces on one sub-mesh (props, trims).</summary>
        public void BoxC(Vector3 center, Vector3 size, int sub, Faces faces = Faces.All) => Box(center - size * 0.5f, size, sub, sub, float.NaN, faces);

        /// <summary>Tapered cylinder along +Y. UV u wraps the circumference in metres, v = height.</summary>
        public void Cylinder(Vector3 basePos, float r0, float r1, float h, int segs, int sub, bool capTop = true, bool capBottom = false)
        {
            float circ = Mathf.PI * 2f * Mathf.Max(r0, r1);
            int start = verts.Count;
            for (int i = 0; i <= segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float slope = (r0 - r1) / Mathf.Max(h, 0.0001f);
                var n = new Vector3(dir.x, slope, dir.z).normalized;
                verts.Add(basePos + dir * r0); norms.Add(n); uvs.Add(new Vector2(i / (float)segs * circ, 0));
                verts.Add(basePos + dir * r1 + Vector3.up * h); norms.Add(n); uvs.Add(new Vector2(i / (float)segs * circ, h));
            }
            var t = Sub(sub);
            for (int i = 0; i < segs; i++)
            {
                int a = start + i * 2;
                t.Add(a); t.Add(a + 1); t.Add(a + 2);
                t.Add(a + 1); t.Add(a + 3); t.Add(a + 2);
            }
            if (capTop) Disc(basePos + Vector3.up * h, r1, segs, sub, true);
            if (capBottom) Disc(basePos, r0, segs, sub, false);
        }

        public void Disc(Vector3 c, float r, int segs, int sub, bool up)
        {
            int center = verts.Count;
            var n = up ? Vector3.up : Vector3.down;
            verts.Add(c); norms.Add(n); uvs.Add(new Vector2(c.x, c.z));
            for (int i = 0; i <= segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                var p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                verts.Add(p); norms.Add(n); uvs.Add(new Vector2(p.x, p.z));
            }
            var t = Sub(sub);
            for (int i = 0; i < segs; i++)
            {
                if (up) { t.Add(center); t.Add(center + i + 2); t.Add(center + i + 1); }
                else { t.Add(center); t.Add(center + i + 1); t.Add(center + i + 2); }
            }
        }

        /// <summary>Cylinder between two arbitrary points (pipes, poles, branches).</summary>
        public void Tube(Vector3 a, Vector3 b, float r0, float r1, int segs, int sub)
        {
            var axis = b - a;
            float len = axis.magnitude;
            if (len < 1e-4f) return;
            var rot = Quaternion.FromToRotation(Vector3.up, axis / len);
            int start = verts.Count;
            Cylinder(Vector3.zero, r0, r1, len, segs, sub, false, false);
            for (int i = start; i < verts.Count; i++) { verts[i] = a + rot * verts[i]; norms[i] = rot * norms[i]; }
        }

        /// <summary>UV sphere, optionally squashed and noise-displaced (tree canopies, rocks).</summary>
        public void Sphere(Vector3 c, Vector3 radius, int rings, int segs, int sub, float noise = 0f, int seed = 0)
        {
            int start = verts.Count;
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;
                float phi = v * Mathf.PI;
                for (int s = 0; s <= segs; s++)
                {
                    float u = s / (float)segs;
                    float th = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    float k = 1f;
                    if (noise > 0f)
                    {
                        int ss = s == segs ? 0 : s;
                        k += (Mathf.PerlinNoise(ss * 0.9f + seed * 3.1f, r * 0.9f + seed) - 0.5f) * 2f * noise;
                    }
                    verts.Add(c + Vector3.Scale(n, radius) * k);
                    norms.Add(n);
                    uvs.Add(new Vector2(u * 4f, v * 2f));
                }
            }
            var t = Sub(sub);
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    int a = start + r * (segs + 1) + s, b = a + segs + 1;
                    t.Add(a); t.Add(a + 1); t.Add(b);
                    t.Add(a + 1); t.Add(b + 1); t.Add(b);
                }
        }

        /// <summary>A double-sided card facing <paramref name="normal"/> (leaf clusters, decals). UV covers 0..1.</summary>
        public void Card(Vector3 center, Vector3 right, Vector3 up, int sub, Vector3 normal, Rect uvRect)
        {
            Vector3 a = center - right - up, b = center + right - up, c = center + right + up, d = center - right + up;
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            for (int k = 0; k < 4; k++) norms.Add(normal);
            uvs.Add(new Vector2(uvRect.xMin, uvRect.yMin)); uvs.Add(new Vector2(uvRect.xMax, uvRect.yMin));
            uvs.Add(new Vector2(uvRect.xMax, uvRect.yMax)); uvs.Add(new Vector2(uvRect.xMin, uvRect.yMax));
            var t = Sub(sub);
            t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>
        /// Extrudes a star-shaped 2D profile (x along length, y up; counter-clockwise) between z0 and z1, with caps.
        /// Used for car bodies and other profiled shapes.
        /// </summary>
        public void ExtrudeProfile(Vector2[] pts, float z0, float z1, int sub, Vector3 offset)
        {
            int n = pts.Length;
            float perim = 0;
            for (int i = 0; i < n; i++)
            {
                var a = pts[i]; var b = pts[(i + 1) % n];
                float len = (b - a).magnitude;
                Quad(offset + new Vector3(a.x, a.y, z1), offset + new Vector3(b.x, b.y, z1), offset + new Vector3(b.x, b.y, z0), offset + new Vector3(a.x, a.y, z0),
                     new Vector2(perim, 0), new Vector2(perim + len, 0), new Vector2(perim + len, z1 - z0), new Vector2(perim, z1 - z0), sub);
                perim += len;
            }
            var c = Vector2.zero;
            foreach (var p in pts) c += p;
            c /= n;
            foreach (bool front in new[] { true, false })
            {
                float z = front ? z0 : z1;
                var nrm = front ? Vector3.back : Vector3.forward;
                int ci = verts.Count;
                verts.Add(offset + new Vector3(c.x, c.y, z)); norms.Add(nrm); uvs.Add(c);
                for (int i = 0; i < n; i++) { verts.Add(offset + new Vector3(pts[i].x, pts[i].y, z)); norms.Add(nrm); uvs.Add(pts[i]); }
                var t = Sub(sub);
                for (int i = 0; i < n; i++)
                {
                    int a = ci + 1 + i, b = ci + 1 + (i + 1) % n;
                    if (front) { t.Add(ci); t.Add(b); t.Add(a); }
                    else { t.Add(ci); t.Add(a); t.Add(b); }
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            int used = subs.Count;
            while (used > 1 && subs[used - 1].Count == 0) used--;
            m.subMeshCount = used;
            for (int i = 0; i < used; i++) m.SetTriangles(subs[i], i, false);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }

        /// <summary>Builds a static GameObject from the mesh with the given materials.</summary>
        public GameObject ToObject(string name, Transform parent, Material[] mats, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = ToMesh(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var use = new Material[mesh.subMeshCount];
            for (int i = 0; i < use.Length; i++) use[i] = mats[Mathf.Min(i, mats.Length - 1)];
            r.sharedMaterials = use;
            r.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            SceneMeshes.Register(mesh);
            return go;
        }
    }

    /// <summary>Generated meshes made while baking a scene; the editor builder saves them as assets so scenes keep them.</summary>
    public static class SceneMeshes
    {
        public static System.Action<Mesh> OnCreated;
        public static void Register(Mesh m) => OnCreated?.Invoke(m);
    }
}
