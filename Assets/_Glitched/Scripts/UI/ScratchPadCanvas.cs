using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The rough-work sheet of ScratchTools: squared paper you write on with the mouse, a pen or a finger.
    /// Strokes are drawn into a Texture2D (round brush stamped along each segment); ERASER rubs out, CLEAR wipes.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ScratchPadCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public const int W = 1024, H = 480;
        static readonly Color32 Paper = new Color32(246, 243, 232, 255), Rule = new Color32(205, 218, 236, 255), Margin = new Color32(238, 170, 170, 255);
        static readonly Color32 Ink = new Color32(22, 46, 120, 255);

        public bool eraser;
        Texture2D tex;
        Color32[] px, blank;
        Vector2 last;
        bool down, dirty;
        RawImage img;

        void Awake()
        {
            img = GetComponent<RawImage>();
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "RoughWork", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            blank = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // squared maths paper: light blue 24 px squares, a red margin on the left
                    bool line = x % 24 == 0 || (H - 1 - y) % 24 == 0;
                    blank[y * W + x] = x == 60 || x == 61 ? Margin : line ? Rule : Paper;
                }
            px = (Color32[])blank.Clone();
            tex.SetPixels32(px); tex.Apply();
            img.texture = tex;
        }

        void OnDestroy() { if (tex != null) Destroy(tex); }

        public void Clear()
        {
            System.Array.Copy(blank, px, px.Length);
            dirty = true;
        }

        bool ToPixel(PointerEventData e, out Vector2 p)
        {
            p = default;
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return false;
            var r = rt.rect;
            p = new Vector2((local.x - r.xMin) / r.width * W, (local.y - r.yMin) / r.height * H);
            return true;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!ToPixel(e, out var p)) return;
            down = true; last = p;
            Stamp(p);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!down || !ToPixel(e, out var p)) return;
            float d = Vector2.Distance(last, p);
            float step = eraser ? 6f : 1.2f;
            for (float t = step; t < d; t += step) Stamp(Vector2.Lerp(last, p, t / d));
            Stamp(p);
            last = p;
        }

        public void OnPointerUp(PointerEventData e) => down = false;

        /// <summary>Capture runs: a little working on the sheet (v = u + at sketched as strokes).</summary>
        public void DemoScribble()
        {
            void Line(float x0, float y0, float x1, float y1) { var a = new Vector2(x0, y0); var b = new Vector2(x1, y1); float d = Vector2.Distance(a, b); for (float t = 0; t <= d; t += 1.2f) Stamp(Vector2.Lerp(a, b, t / d)); }
            Line(100, 380, 130, 330); Line(130, 330, 160, 380);   // v
            Line(190, 360, 220, 360); Line(190, 345, 220, 345);   // =
            Line(250, 375, 250, 340); Line(250, 340, 275, 340); Line(275, 340, 275, 375);   // u
            Line(300, 357, 330, 357); Line(315, 342, 315, 372);   // +
            for (int i = 0; i < 40; i++) { float a = i / 40f * Mathf.PI * 2f; Stamp(new Vector2(370 + Mathf.Cos(a) * 14, 350 + Mathf.Sin(a) * 14)); }
            Line(384, 365, 384, 336); Line(420, 380, 420, 330); Line(408, 360, 432, 360);   // a t
            Line(100, 280, 600, 280);
        }

        void Stamp(Vector2 c)
        {
            float r = eraser ? 16f : 2.6f;
            int x0 = Mathf.Max(0, (int)(c.x - r - 1)), x1 = Mathf.Min(W - 1, (int)(c.x + r + 1));
            int y0 = Mathf.Max(0, (int)(c.y - r - 1)), y1 = Mathf.Min(H - 1, (int)(c.y + r + 1));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y));
                    if (d > r + 0.5f) continue;
                    int i = y * W + x;
                    if (eraser) { px[i] = blank[i]; continue; }
                    // soft edge: ink blends into the paper over the last pixel
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    px[i] = Color32.Lerp(px[i], Ink, a);
                }
            dirty = true;
        }

        void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            tex.SetPixels32(px);
            tex.Apply(false);
        }
    }
}
