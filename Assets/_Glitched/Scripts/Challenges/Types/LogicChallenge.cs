using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 2. Logic: solve from rules. Modes: "choice" (pick the answer, optional code/grid),
    /// "order" (arrange items into the right sequence), "truth" (fill a truth table by tapping cells).
    /// </summary>
    public class LogicChallenge : ChallengeBase
    {
        List<Button> buttons;
        ReorderList reorder;
        readonly List<(Button b, int answerIndex)> cells = new List<(Button, int)>();
        readonly List<string> cellValues = new List<string>();
        int picked = -1;

        string Mode => string.IsNullOrEmpty(D.mode) ? "choice" : D.mode;
        public override bool AutoSubmits => Mode == "choice";

        protected override void Build()
        {
            var v = UIFactory.VLayout(Area, 14, 0, TextAnchor.UpperCenter);
            switch (Mode)
            {
                case "order":
                    Ctx.setHelp("Drag the rows (or use ▲▼) into the right order, then SUBMIT.");
                    reorder = ReorderList.Create(Area, D.items, Derange(D.items.Count), false, D.items.Count > 5 ? 50 : 58);
                    break;
                case "truth":
                    Ctx.setHelp("Tap each ? cell to set 0 or 1, then SUBMIT.");
                    BuildTruth();
                    break;
                default:
                    if (!string.IsNullOrEmpty(D.code)) CodeBlock(Area, D.code);
                    buttons = OptionButtons(Area, D.options, Pick);
                    Ctx.setHelp("Click an answer, or press 1-4.");
                    break;
            }
        }

        void BuildTruth()
        {
            var grid = UIFactory.Rect("Truth", Area);
            int cols = D.headers.Count;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(220, 62); gl.spacing = new Vector2(10, 8);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = cols;
            gl.childAlignment = TextAnchor.UpperCenter;
            UIFactory.Size(grid, -1, (D.rows.Count + 1) * 70);
            foreach (var h in D.headers)
            {
                var c = UIFactory.Image(grid, new Color(0.1f, 0.25f, 0.32f, 1f), UISprites.Rounded(10), "H");
                var t = UIFactory.Text(c.transform, h, 28, UIFactory.Theme.cyan, TextAlignmentOptions.Center, true);
                UIFactory.Stretch(t.rectTransform);
            }
            int qi = 0;
            foreach (var row in D.rows)
            {
                foreach (var cell in row)
                {
                    if (cell == "?")
                    {
                        int index = qi++;
                        cellValues.Add("?");
                        Button b = null;
                        b = UIFactory.Button(grid, "?", () => Toggle(index), ButtonStyle.Answer, 30);
                        cells.Add((b, index));
                    }
                    else
                    {
                        var c = UIFactory.Image(grid, new Color(0.06f, 0.07f, 0.12f, 1f), UISprites.Rounded(10), "C");
                        var t = UIFactory.Text(c.transform, cell, 30, UIFactory.Theme.text, TextAlignmentOptions.Center, true);
                        UIFactory.Stretch(t.rectTransform);
                    }
                }
            }
        }

        void Toggle(int index)
        {
            if (Done) return;
            cellValues[index] = cellValues[index] == "1" ? "0" : "1";
            UIFactory.Label(cells[index].b).text = cellValues[index];
        }

        public override bool CanSubmit
        {
            get
            {
                if (Mode == "truth") return !cellValues.Contains("?");
                if (Mode == "order") return true;
                return picked >= 0;
            }
        }

        void Pick(int i)
        {
            if (Done) return;
            picked = i;
            OnSubmit();
        }

        public override void OnAnswerKey(int index)
        {
            if (Mode == "choice" && index < D.options.Count) Pick(index);
        }

        public override void OnSubmit()
        {
            if (Done) return;
            switch (Mode)
            {
                case "order":
                    bool ok = true;
                    for (int i = 0; i < reorder.order.Count; i++) if (reorder.order[i] != i) { ok = false; break; }
                    reorder.Lock();
                    Finish(ok);
                    break;
                case "truth":
                    bool all = true;
                    for (int i = 0; i < cellValues.Count; i++) if (i >= D.answers.Count || cellValues[i] != D.answers[i]) all = false;
                    Finish(all);
                    break;
                default:
                    Finish(picked == D.answerIndex);
                    break;
            }
        }

        public override void Reveal(bool correct)
        {
            if (Mode == "choice" && buttons != null)
            {
                if (picked >= 0 && picked != D.answerIndex) Mark(buttons[picked], false);
                if (D.answerIndex >= 0 && D.answerIndex < buttons.Count) Mark(buttons[D.answerIndex], true);
                foreach (var b in buttons) b.interactable = false;
            }
            else if (Mode == "truth")
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    bool ok = i < D.answers.Count && cellValues[i] == D.answers[i];
                    Mark(cells[i].b, ok);
                    if (!ok && i < D.answers.Count) UIFactory.Label(cells[i].b).text = $"{cellValues[i]} → {D.answers[i]}";
                    cells[i].b.interactable = false;
                }
            }
            else if (Mode == "order" && reorder != null)
            {
                for (int i = 0; i < reorder.rows.Count; i++) MarkGraphic(reorder.rows[i].GetComponent<Image>(), reorder.order[i] == i);
            }
        }
    }
}
