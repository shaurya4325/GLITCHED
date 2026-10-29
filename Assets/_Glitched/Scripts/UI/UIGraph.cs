using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Immediate-mode vector drawing inside a UI rect (lines, polylines, circles, rectangles).
    /// Coordinates are in the rect's local pixels, origin bottom-left. Call Clear(), draw, then it rebuilds.
    /// Used by simulations, prediction playbacks, matching lines and the teaching labs.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIGraph : MaskableGraphic
    {
        struct Cmd { public int kind; public Vector2 a, b; public float r, w; public Color c; public List<Vector2> pts; public bool fill; }
        readonly List<Cmd> cmds = new List<Cmd>();

        public Vector2 Size => rectTransform.rect.size;

        public void Clear() { cmds.Clear(); SetVerticesDirty(); }

        public void Line(Vector2 a, Vector2 b, Color c, float width = 4f) { cmds.Add(new Cmd { kind = 0, a = a, b = b, c = c, w = width }); SetVerticesDirty(); }

        public void Polyline(List<Vector2> pts, Color c, float width = 4f)
        {
            if (pts == null || pts.Count < 2) return;
            cmds.Add(new Cmd { kind = 1, pts = new List<Vector2>(pts), c = c, w = width });
            SetVerticesDirty();
        }

        public void Circle(Vector2 centre, float radius, Color c, bool fill = true, float width = 3f)
        {
            cmds.Add(new Cmd { kind = 2, a = centre, r = radius, c = c, fill = fill, w = width });
            SetVerticesDirty();
        }

        public void Box(Vector2 min, Vector2 max, Color c, bool fill = true, float width = 3f)
        {
            cmds.Add(new Cmd { kind = 3, a = min, b = max, c = c, fill = fill, w = width });
            SetVerticesDirty();
        }

        public void Arrow(Vector2 a, Vector2 b, Color c, float width = 4f, float head = 14f)
        {
            Line(a, b, c, width);
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x);
            Line(b, b - d * head + n * head * 0.6f, c, width);
            Line(b, b - d * head - n * head * 0.6f, c, width);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vector2 o = new Vector2(r.xMin, r.yMin);
            foreach (var c in cmds)
            {
                if (vh.currentVertCount > 60000) break;   // a uGUI mesh holds 65 000 vertices at most
                switch (c.kind)
                {
                    case 0: Seg(vh, o + c.a, o + c.b, c.w, c.c); break;
                    case 1:
                        for (int i = 0; i < c.pts.Count - 1 && vh.currentVertCount < 64000; i++) Seg(vh, o + c.pts[i], o + c.pts[i + 1], c.w, c.c);
                        break;
                    case 2:
                        {
                            int seg = Mathf.Clamp((int)(c.r * 0.8f), 12, 48);
                            if (c.fill)
                            {
                                int centre = vh.currentVertCount;
                                vh.AddVert(o + c.a, c.c, Vector2.zero);
                                for (int i = 0; i <= seg; i++)
                                {
                                    float a = i / (float)seg * Mathf.PI * 2;
                                    vh.AddVert(o + c.a + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * c.r, c.c, Vector2.zero);
                                    if (i > 0) vh.AddTriangle(centre, centre + i, centre + i + 1);
                                }
                            }
                            else
                            {
                                Vector2 prev = o + c.a + new Vector2(c.r, 0);
                                for (int i = 1; i <= seg; i++)
                                {
                                    float a = i / (float)seg * Mathf.PI * 2;
                                    Vector2 p = o + c.a + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * c.r;
                                    Seg(vh, prev, p, c.w, c.c);
                                    prev = p;
                                }
                            }
                            break;
                        }
                    case 3:
                        if (c.fill)
                        {
                            int s = vh.currentVertCount;
                            vh.AddVert(o + c.a, c.c, Vector2.zero);
                            vh.AddVert(o + new Vector2(c.a.x, c.b.y), c.c, Vector2.zero);
                            vh.AddVert(o + c.b, c.c, Vector2.zero);
                            vh.AddVert(o + new Vector2(c.b.x, c.a.y), c.c, Vector2.zero);
                            vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
                        }
                        else
                        {
                            Vector2 a = o + c.a, b = o + c.b;
                            Seg(vh, a, new Vector2(a.x, b.y), c.w, c.c); Seg(vh, new Vector2(a.x, b.y), b, c.w, c.c);
                            Seg(vh, b, new Vector2(b.x, a.y), c.w, c.c); Seg(vh, new Vector2(b.x, a.y), a, c.w, c.c);
                        }
                        break;
                }
            }
        }

        static void Seg(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
            int s = vh.currentVertCount;
            vh.AddVert(a - n, c, Vector2.zero); vh.AddVert(a + n, c, Vector2.zero);
            vh.AddVert(b + n, c, Vector2.zero); vh.AddVert(b - n, c, Vector2.zero);
            vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
        }

        public static UIGraph Create(Transform parent, string name = "Graph")
        {
            var rt = UIFactory.Rect(name, parent);
            var g = rt.gameObject.AddComponent<UIGraph>();
            g.raycastTarget = false;
            return g;
        }
    }
}
