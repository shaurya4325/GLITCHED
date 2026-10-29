using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 7. Matching: pair concepts with real examples by drawing lines. Click/tap a left item then a right item
    /// (or drag from left to right). Tap a linked left item to unlink it.
    /// </summary>
    public class MatchingChallenge : ChallengeBase
    {
        readonly List<Button> leftB = new List<Button>(), rightB = new List<Button>();
        List<int> rightOrder;                 // rightOrder[displayIndex] = original right index
        readonly Dictionary<int, int> links = new Dictionary<int, int>();   // left -> right display index
        int selectedLeft = -1;
        UIGraph lines;
        static readonly Color[] Palette = { new Color(0.25f, 0.92f, 1f), new Color(1f, 0.24f, 0.78f), new Color(1f, 0.86f, 0.25f), new Color(0.45f, 1f, 0.55f), new Color(0.7f, 0.5f, 1f), new Color(1f, 0.55f, 0.2f) };

        protected override void Build()
        {
            int n = D.left.Count;
            rightOrder = Derange(n);
            float rowH = Mathf.Min(78, 380f / n);
            var root = UIFactory.Rect("Columns", Area);
            UIFactory.Stretch(root);
            lines = UIGraph.Create(root, "Lines");
            UIFactory.Stretch(lines.rectTransform);
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var l = UIFactory.Button(root, D.left[i], () => TapLeft(k), ButtonStyle.Answer, 26);
                UIFactory.Place(l.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, -i * (rowH + 12)), new Vector2(560, rowH), new Vector2(0, 1));
                var dl = l.gameObject.AddComponent<MatchDrag>(); dl.owner = this; dl.left = k;
                leftB.Add(l);
                int disp = i;
                var r = UIFactory.Button(root, D.right[rightOrder[i]], () => TapRight(disp), ButtonStyle.Answer, 24);
                UIFactory.Place(r.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(0, -i * (rowH + 12)), new Vector2(760, rowH), new Vector2(1, 1));
                UIFactory.Label(r).alignment = TextAlignmentOptions.MidlineLeft;
                rightB.Add(r);
            }
            Ctx.setHelp("Tap a left item, then its match on the right (or drag a line). Tap again to unlink.");
        }

        void TapLeft(int l)
        {
            if (Done) return;
            if (links.ContainsKey(l) && selectedLeft != l) { links.Remove(l); Redraw(); }
            selectedLeft = selectedLeft == l ? -1 : l;
            for (int i = 0; i < leftB.Count; i++) leftB[i].transform.localScale = Vector3.one * (i == selectedLeft ? 1.05f : 1f);
            Services.Audio.Play("select");
        }

        void TapRight(int disp)
        {
            if (Done || selectedLeft < 0) return;
            Link(selectedLeft, disp);
        }

        internal void Link(int l, int disp)
        {
            int existing = -1;
            foreach (var kv in links) if (kv.Value == disp) existing = kv.Key;
            if (existing >= 0) links.Remove(existing);
            links[l] = disp;
            selectedLeft = -1;
            foreach (var b in leftB) b.transform.localScale = Vector3.one;
            Services.Audio.Play("drop");
            Redraw();
        }

        internal int RightUnder(Vector2 screen)
        {
            for (int i = 0; i < rightB.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)rightB[i].transform, screen, null)) return i;
            return -1;
        }

        void Redraw(Vector2? dragFrom = null, Vector2? dragTo = null)
        {
            lines.Clear();
            var lr = lines.rectTransform;
            foreach (var kv in links)
            {
                Vector2 a = Edge(leftB[kv.Key], lr, true), b = Edge(rightB[kv.Value], lr, false);
                var c = Palette[kv.Key % Palette.Length];
                lines.Line(a, b, c, 5);
                lines.Circle(a, 8, c); lines.Circle(b, 8, c);
                SetBorder(leftB[kv.Key], c); SetBorder(rightB[kv.Value], c);
            }
            for (int i = 0; i < leftB.Count; i++) if (!links.ContainsKey(i)) SetBorder(leftB[i], new Color(UIFactory.Theme.cyan.r, UIFactory.Theme.cyan.g, UIFactory.Theme.cyan.b, 0.55f));
            for (int i = 0; i < rightB.Count; i++) if (!links.ContainsValue(i)) SetBorder(rightB[i], new Color(UIFactory.Theme.cyan.r, UIFactory.Theme.cyan.g, UIFactory.Theme.cyan.b, 0.55f));
            if (dragFrom.HasValue && dragTo.HasValue) lines.Line(dragFrom.Value, dragTo.Value, Color.white, 4);
        }

        internal void DrawDrag(int l, Vector2 screen)
        {
            var lr = lines.rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lr, screen, null, out var p);
            Redraw(Edge(leftB[l], lr, true), p - lr.rect.min);
        }

        internal void EndDrag() => Redraw();

        static void SetBorder(Button b, Color c)
        {
            var border = b.transform.Find("Border")?.GetComponent<Image>();
            if (border != null) border.color = c;
            var fx = b.GetComponent<UIButtonFX>(); if (fx != null) fx.baseBorder = c;
        }

        static Vector2 Edge(Button b, RectTransform space, bool rightEdge)
        {
            var rt = (RectTransform)b.transform;
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 w = rightEdge ? (c[2] + c[3]) * 0.5f : (c[0] + c[1]) * 0.5f;
            Vector2 local = space.InverseTransformPoint(w);
            return local - space.rect.min;
        }

        public override bool CanSubmit => links.Count == leftB.Count;

        public override void OnSubmit()
        {
            if (Done || !CanSubmit) return;
            bool ok = true;
            foreach (var kv in links) if (rightOrder[kv.Value] != kv.Key) ok = false;
            Finish(ok);
        }

        public override void Reveal(bool correct)
        {
            lines.Clear();
            var lr = lines.rectTransform;
            for (int l = 0; l < leftB.Count; l++)
            {
                int correctDisp = rightOrder.IndexOf(l);
                bool ok = links.TryGetValue(l, out var d) && d == correctDisp;
                Mark(leftB[l], ok);
                lines.Line(Edge(leftB[l], lr, true), Edge(rightB[correctDisp], lr, false), ok ? CorrectColor : WrongColor, 5);
                leftB[l].interactable = false;
            }
            foreach (var r in rightB) r.interactable = false;
        }

        void LateUpdate() { if (!Done && Time.frameCount % 30 == 0) Redraw(); }
    }

}
