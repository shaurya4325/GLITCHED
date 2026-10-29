using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A flat pane (a monitor screen, a window) bursting into real glass: thin triangular shards cut from the pane's
    /// rectangle, thrown out along its normal with spin, falling under gravity and bouncing off whatever they hit.
    /// </summary>
    public static class GlassShatter
    {
        /// <summary>Breaks the pane drawn by <paramref name="pane"/> (its local XY quad), outward along <paramref name="outward"/>.</summary>
        public static void At(Renderer pane, Vector3 outward, float force = 3.5f, int columns = 7, int rows = 5, float seconds = 9f)
        {
            if (pane == null) return;
            var t = pane.transform;
            var b = pane.localBounds;
            // the pane's corners in world space (the quad is flat in its local XY)
            Vector3 P(float u, float v) => t.TransformPoint(new Vector3(Mathf.Lerp(b.min.x, b.max.x, u), Mathf.Lerp(b.min.y, b.max.y, v), b.center.z));
            var glass = CityMaterials.Glass(new Color(0.75f, 0.85f, 1f));
            var edge = CityMaterials.Emissive(new Color(0.2f, 0.1f, 0.3f), new Color(2.2f, 0.6f, 2.6f));
            var root = new GameObject("Shatter").transform;
            var rnd = new System.Random(911);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            // a jittered grid: every cell is split into two triangles, so the pieces tile the whole pane
            var grid = new Vector2[columns + 1, rows + 1];
            for (int i = 0; i <= columns; i++)
                for (int j = 0; j <= rows; j++)
                {
                    float u = i / (float)columns, v = j / (float)rows;
                    if (i > 0 && i < columns) u += R(-0.35f, 0.35f) / columns;
                    if (j > 0 && j < rows) v += R(-0.35f, 0.35f) / rows;
                    grid[i, j] = new Vector2(u, v);
                }
            Vector3 center = P(0.5f, 0.5f);
            for (int i = 0; i < columns; i++)
                for (int j = 0; j < rows; j++)
                    for (int half = 0; half < 2; half++)
                    {
                        Vector2 a = grid[i, j], c = grid[i + 1, j + 1];
                        Vector2 m = half == 0 ? grid[i + 1, j] : grid[i, j + 1];
                        Vector3 wa = P(a.x, a.y), wb = P(m.x, m.y), wc = P(c.x, c.y);
                        Vector3 mid = (wa + wb + wc) / 3f;
                        var go = new GameObject("Shard");
                        go.transform.SetParent(root, false);
                        go.transform.position = mid;
                        var mb = new MeshBuilder(2);
                        Vector3 n = Vector3.Cross(wb - wa, wc - wa).normalized * 0.006f;
                        Vector3 la = wa - mid, lb = wb - mid, lc = wc - mid;
                        // the two faces (a quad with its last corner doubled is a triangle)
                        mb.Quad(la + n, lb + n, lc + n, lc + n, a, m, c, c, 0);
                        mb.Quad(lc - n, lb - n, la - n, la - n, c, m, a, a, 0);
                        // thin glowing edges (the Glitch light caught in the cut)
                        mb.Quad(la + n, la - n, lb - n, lb + n, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, 1);
                        mb.Quad(lb + n, lb - n, lc - n, lc + n, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, 1);
                        mb.Quad(lc + n, lc - n, la - n, la + n, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, 1);
                        var mesh = mb.ToMesh("GlassShard");
                        go.AddComponent<MeshFilter>().sharedMesh = mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { glass, edge };
                        var col = go.AddComponent<BoxCollider>();
                        col.size = new Vector3(Mathf.Max(0.02f, mesh.bounds.size.x), Mathf.Max(0.02f, mesh.bounds.size.y), 0.02f);
                        var rb = go.AddComponent<Rigidbody>();
                        rb.mass = 0.05f; rb.linearDamping = 0.15f; rb.angularDamping = 0.2f;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        // the middle blows out hardest; edges fall away
                        float k = 1f - Mathf.Clamp01(Vector3.Distance(mid, center) / Mathf.Max(0.01f, b.size.magnitude * t.lossyScale.x * 0.6f));
                        rb.linearVelocity = outward.normalized * force * R(0.5f, 1f) * (0.4f + k) + (mid - center).normalized * R(0.6f, 2f) + Vector3.up * R(0f, 1.2f);
                        rb.angularVelocity = new Vector3(R(-14f, 14f), R(-14f, 14f), R(-14f, 14f));
                    }
            Object.Destroy(root.gameObject, seconds);
        }
    }
}
