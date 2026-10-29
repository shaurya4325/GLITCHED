using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 10. Construction: build something that must satisfy rules.
    /// "count" (molecule): add atoms from the palette until the formula is right, then BOND.
    /// "sequence" (food chain, logic chain, energy chain): place parts in order.
    /// </summary>
    public class ConstructionChallenge : ChallengeBase
    {
        readonly List<string> built = new List<string>();
        RectTransform buildArea;
        UIGraph graph;
        TextMeshProUGUI formula;
        bool Sequence => D.mode == "sequence";
        public override string SubmitLabel => Sequence ? "CONNECT  ▶" : "BOND!  ⚡";

        public static Color AtomColor(string el)
        {
            switch (el)
            {
                case "H": return new Color(0.95f, 0.95f, 1f);
                case "O": return new Color(1f, 0.3f, 0.3f);
                case "C": return new Color(0.25f, 0.25f, 0.3f);
                case "N": return new Color(0.35f, 0.5f, 1f);
                case "Cl": return new Color(0.4f, 1f, 0.4f);
                case "Na": return new Color(1f, 0.85f, 0.2f);
                default: return new Color(0.6f, 0.7f, 0.9f);
            }
        }

        protected override void Build()
        {
            UIFactory.VLayout(Area, 14, 0, TextAnchor.UpperLeft);
            var stage = UIFactory.Panel(Area, new Color(0.02f, 0.03f, 0.06f, 1f), 16, "Stage", false);
            UIFactory.Size(stage, -1, 250);
            graph = UIGraph.Create(stage.transform, "Bonds");
            UIFactory.Stretch(graph.rectTransform);
            buildArea = UIFactory.Rect("Built", stage.transform);
            UIFactory.Stretch(buildArea);
            formula = UIFactory.Text(stage.transform, "", 26, UIFactory.Theme.textDim, TextAlignmentOptions.TopRight, true);
            UIFactory.Stretch(formula.rectTransform, 16, 10, 16, 10);

            var palette = UIFactory.Rect("Palette", Area);
            UIFactory.Size(palette, -1, 90);
            UIFactory.HLayout(palette, 14, 0, TextAnchor.MiddleLeft, false);
            var parts = new List<string>(D.parts);
            if (Sequence) Shuffle(parts);
            foreach (var p in parts)
            {
                string part = p;
                var b = UIFactory.Button(palette, Sequence ? part : $"<b>{part}</b>", () => Add(part), ButtonStyle.Primary, Sequence ? 24 : 34);
                UIFactory.Size(b, Sequence ? 240 : 110, 80);
                if (!Sequence)
                {
                    var img = b.GetComponent<Image>();
                    var c = AtomColor(part);
                    img.color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 1f);
                }
            }
            var undo = UIFactory.Button(palette, "⌫ UNDO", () => { if (built.Count > 0 && !Done) { built.RemoveAt(built.Count - 1); Redraw(); } }, ButtonStyle.Ghost, 22);
            UIFactory.Size(undo, 160, 80);
            var clear = UIFactory.Button(palette, "CLEAR", () => { if (!Done) { built.Clear(); Redraw(); } }, ButtonStyle.Ghost, 22);
            UIFactory.Size(clear, 140, 80);
            Ctx.setHelp(Sequence ? "Tap the parts in the right order (start to end), then CONNECT." : $"Target: <b>{D.target}</b>. Tap atoms to add them, then BOND.");
            Redraw();
        }

        void Add(string part)
        {
            if (Done) return;
            if (Sequence && built.Count >= D.sequence.Count) return;
            if (!Sequence && built.Count >= 14) return;
            built.Add(part);
            Services.Audio.Play("drop");
            Redraw();
        }

        void Redraw()
        {
            UIFactory.Clear(buildArea);
            graph.Clear();
            var size = ((RectTransform)graph.transform).rect.size;
            if (size.x < 10) size = new Vector2(1600, 250);
            if (Sequence)
            {
                int n = D.sequence.Count;
                float w = Mathf.Min(260, (size.x - 60) / n - 30);
                for (int i = 0; i < n; i++)
                {
                    float x = 40 + i * (w + 30) + w / 2;
                    var pos = new Vector2(x, size.y / 2);
                    bool has = i < built.Count;
                    var chip = UIFactory.Image(buildArea, has ? new Color(0.06f, 0.2f, 0.26f, 1f) : new Color(1, 1, 1, 0.05f), UISprites.Rounded(12), "Slot");
                    var rt = chip.rectTransform;
                    rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(w, 90);
                    var t = UIFactory.Text(chip.transform, has ? built[i] : (i + 1).ToString(), 24, has ? UIFactory.Theme.text : UIFactory.Theme.textDim, TextAlignmentOptions.Center);
                    UIFactory.Stretch(t.rectTransform, 6, 4, 6, 4);
                    if (i < n - 1) graph.Arrow(pos + new Vector2(w / 2 + 2, 0), pos + new Vector2(w / 2 + 28, 0), has && i + 1 < built.Count ? UIFactory.Theme.cyan : new Color(1, 1, 1, 0.2f), 4, 10);
                }
                formula.text = "";
            }
            else
            {
                // molecule: first atom (or the heaviest) in the centre, others around it
                if (built.Count == 0) { formula.text = $"<color=#5b6a86>target {D.target}</color>"; return; }
                int centre = 0;
                for (int i = 0; i < built.Count; i++) if (built[i] != "H") { centre = i; break; }
                Vector2 c = new Vector2(size.x / 2, size.y / 2);
                Atom(c, built[centre], 46);
                int k = 0, others = built.Count - 1;
                for (int i = 0; i < built.Count; i++)
                {
                    if (i == centre) continue;
                    float a = (k / Mathf.Max(1f, others)) * Mathf.PI * 2 + Mathf.PI / 2;
                    Vector2 p = c + new Vector2(Mathf.Cos(a) * 150, Mathf.Sin(a) * 85);
                    graph.Line(c, p, new Color(1, 1, 1, 0.5f), 8);
                    Atom(p, built[i], built[i] == "H" ? 30 : 40);
                    k++;
                }
                formula.text = "you built: <b>" + Formula() + "</b>";
            }
        }

        void Atom(Vector2 p, string el, float r)
        {
            var img = UIFactory.Image(buildArea, AtomColor(el), UISprites.Circle(128), "Atom");
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = p; rt.sizeDelta = Vector2.one * r * 2;
            var t = UIFactory.Text(img.transform, el, r * 0.8f, el == "C" ? Color.white : new Color(0.05f, 0.05f, 0.1f), TextAlignmentOptions.Center, true);
            UIFactory.Stretch(t.rectTransform);
        }

        string Formula()
        {
            var counts = new Dictionary<string, int>();
            foreach (var b in built) counts[b] = counts.TryGetValue(b, out var n) ? n + 1 : 1;
            var order = new List<string> { "C", "H" };
            foreach (var k in counts.Keys) if (!order.Contains(k)) order.Add(k);
            var sb = new System.Text.StringBuilder();
            foreach (var k in order) if (counts.TryGetValue(k, out var n)) sb.Append(k).Append(n > 1 ? $"<sub>{n}</sub>" : "");
            return sb.ToString();
        }

        public override bool CanSubmit => Sequence ? built.Count == D.sequence.Count : built.Count > 0;

        public override void OnSubmit()
        {
            if (Done || !CanSubmit) return;
            bool ok;
            if (Sequence)
            {
                ok = true;
                for (int i = 0; i < D.sequence.Count; i++) if (built[i] != D.sequence[i]) ok = false;
            }
            else
            {
                var counts = new Dictionary<string, int>();
                foreach (var b in built) counts[b] = counts.TryGetValue(b, out var n) ? n + 1 : 1;
                ok = counts.Count == D.required.Count;
                foreach (var kv in D.required) if (!counts.TryGetValue(kv.Key, out var n) || n != kv.Value) ok = false;
            }
            Finish(ok);
        }

        public override void Reveal(bool correct)
        {
            foreach (var b in Area.GetComponentsInChildren<Button>()) b.interactable = false;
            if (correct)
            {
                Services.Audio.Play("bond");
                for (int i = 0; i < buildArea.childCount; i++) Tween.Punch(buildArea.GetChild(i), 0.3f, 0.4f);
                formula.text = $"<color=#7cffa0>{(Sequence ? "Connected!" : "Bonded: " + Formula())}</color>";
            }
            else formula.text = $"<color=#ff9a3d>Needed: {Q.CorrectAnswerText()}</color>";
        }
    }
}
