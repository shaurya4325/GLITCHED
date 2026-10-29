using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 1. Drag &amp; Drop: drag items into the slots of a template (connect circuit parts, fill a condition, balance
    /// an equation). Touch-friendly: tap an item, then tap a slot. Tap a filled slot to empty it.
    /// </summary>
    public class DragDropChallenge : ChallengeBase
    {
        readonly List<Button> slots = new List<Button>();
        readonly List<int> filled = new List<int>();
        readonly List<Button> chips = new List<Button>();
        int selected = -1;
        bool mono;

        protected override void Build()
        {
            UIFactory.VLayout(Area, 18, 0, TextAnchor.UpperLeft);
            mono = D.template.Contains(":") && (D.template.Contains("(") || D.template.Contains("for ") || D.template.Contains("while"));
            var box = UIFactory.Panel(Area, mono ? UIFactory.Theme.code : new Color(0.05f, 0.07f, 0.12f, 1f), 14, "Template", false);
            var lines = D.template.Split('\n');
            UIFactory.VLayout(box, 10, 18, TextAnchor.UpperLeft, false);
            UIFactory.Size(box, -1, lines.Length * 76 + 40);
            var rx = new Regex(@"\{(\d+)\}");
            foreach (var line in lines)
            {
                var row = UIFactory.Rect("Line", box.transform);
                UIFactory.Size(row, -1, 66);
                var h = UIFactory.HLayout(row, 8, 0, TextAnchor.MiddleLeft, false);
                h.childForceExpandHeight = false;
                int last = 0;
                foreach (Match m in rx.Matches(line))
                {
                    if (m.Index > last) Segment(row, line.Substring(last, m.Index - last));
                    int slotIndex = int.Parse(m.Groups[1].Value);
                    while (filled.Count <= slotIndex) { filled.Add(-1); slots.Add(null); }
                    int s = slotIndex;
                    var b = UIFactory.Button(row, "____", () => TapSlot(s), ButtonStyle.Answer, 28);
                    UIFactory.Size(b, 190, 60);
                    UIFactory.Label(b).font = mono ? UIFactory.MonoFont : UIFactory.BodyFont;
                    slots[slotIndex] = b;
                    b.gameObject.AddComponent<DropSlot>().index = s;
                    last = m.Index + m.Length;
                }
                if (last < line.Length) Segment(row, line.Substring(last));
            }

            var palette = UIFactory.Rect("Items", Area);
            var gl = palette.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(260, 70); gl.spacing = new Vector2(14, 12);
            gl.childAlignment = TextAnchor.UpperLeft;
            UIFactory.Size(palette, -1, Mathf.CeilToInt(D.items.Count / 6f) * 82);
            for (int i = 0; i < D.items.Count; i++)
            {
                int k = i;
                var c = UIFactory.Button(palette, D.items[i], () => Select(k), ButtonStyle.Primary, 28);
                UIFactory.Label(c).font = mono ? UIFactory.MonoFont : UIFactory.BodyFont;
                var drag = c.gameObject.AddComponent<DragChip>();
                drag.owner = this; drag.index = k;
                chips.Add(c);
            }
            Ctx.setHelp("Drag an item into a slot, or tap an item then tap a slot. Tap a slot to clear it.");
        }

        void Segment(Transform row, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var t = mono ? UIFactory.Mono(row, ChallengeBase.Syntax(text), 30) : UIFactory.Text(row, text, 30);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var fit = t.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.Size(t, -1, 60);
        }

        void Select(int item)
        {
            if (Done) return;
            selected = selected == item ? -1 : item;
            for (int i = 0; i < chips.Count; i++) chips[i].transform.localScale = Vector3.one * (i == selected ? 1.08f : 1f);
            // auto-place into the first empty slot when there is only one slot
            if (selected >= 0 && slots.Count == 1) TapSlot(0);
        }

        void TapSlot(int s)
        {
            if (Done) return;
            if (selected >= 0) { Place(selected, s); return; }
            if (filled[s] >= 0) { Clear(s); }
        }

        internal void Place(int item, int s)
        {
            if (Done || s < 0 || s >= slots.Count) return;
            if (!D.reuse)
            {
                for (int i = 0; i < filled.Count; i++) if (filled[i] == item) Clear(i);
                if (filled[s] >= 0) chips[filled[s]].interactable = true;
                chips[item].interactable = false;
            }
            filled[s] = item;
            UIFactory.Label(slots[s]).text = D.items[item];
            Tween.Punch(slots[s].transform, 0.15f, 0.2f);
            Services.Audio.Play("drop");
            selected = -1;
            foreach (var c in chips) c.transform.localScale = Vector3.one;
        }

        void Clear(int s)
        {
            if (filled[s] >= 0 && !D.reuse) chips[filled[s]].interactable = true;
            filled[s] = -1;
            UIFactory.Label(slots[s]).text = "____";
        }

        public override bool CanSubmit => !filled.Contains(-1);

        public override void OnSubmit()
        {
            if (Done || !CanSubmit) return;
            Finish(Check());
        }

        bool Check()
        {
            for (int s = 0; s < filled.Count; s++)
            {
                if (D.answer != null && s < D.answer.Count) { if (filled[s] != D.answer[s]) return false; }
                else if (D.answerSets != null && s < D.answerSets.Count) { if (!D.answerSets[s].Contains(filled[s])) return false; }
            }
            if (D.answerSets != null && !D.reuse)
            {
                var seen = new HashSet<int>();
                foreach (var f in filled) if (!seen.Add(f)) return false;
            }
            return true;
        }

        public override void Reveal(bool correct)
        {
            for (int s = 0; s < slots.Count; s++)
            {
                bool ok = D.answer != null && s < D.answer.Count ? filled[s] == D.answer[s] : D.answerSets != null && s < D.answerSets.Count && D.answerSets[s].Contains(filled[s]);
                Mark(slots[s], ok);
                if (!ok && D.answer != null && s < D.answer.Count) UIFactory.Label(slots[s]).text = D.items[D.answer[s]];
                slots[s].interactable = false;
            }
            foreach (var c in chips) c.interactable = false;
        }

        internal int SlotUnder(Vector2 screen)
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)slots[i].transform, screen, null)) return i;
            return -1;
        }
    }


}
