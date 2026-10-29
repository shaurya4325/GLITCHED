using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Base class with helpers shared by the ten challenge types.</summary>
    public abstract class ChallengeBase : MonoBehaviour, IChallenge
    {
        protected QuestionData Q;
        protected QuestionPayload D => Q.data;
        protected RectTransform Area;
        protected ChallengeContext Ctx;
        protected bool Done;
        public event Action<bool, float> Result;

        public virtual bool CanSubmit => true;
        public virtual bool AutoSubmits => false;
        public virtual float PlaybackSeconds => 0f;
        public virtual string SubmitLabel => "SUBMIT  ⏎";

        public void Setup(QuestionData q, RectTransform area, ChallengeContext ctx)
        {
            Q = q; Area = area; Ctx = ctx;
            Build();
        }

        protected abstract void Build();
        public abstract void OnSubmit();
        public virtual void OnAnswerKey(int index) { }
        public virtual void Reveal(bool correct) { }

        protected void Finish(bool correct)
        {
            if (Done) return;
            Done = true;
            Result?.Invoke(correct, Ctx.Elapsed);
        }

        // ------------------------------------------------------------------ shared UI bits
        protected static Color CorrectColor => Services.Settings.Data.colorBlindSafe ? new Color(0.25f, 0.6f, 1f) : UIFactory.Theme.green;
        protected static Color WrongColor => Services.Settings.Data.colorBlindSafe ? new Color(1f, 0.55f, 0.1f) : UIFactory.Theme.red;
        protected const string Tick = "✔";
        protected const string Cross = "✖";

        protected static void Mark(Button b, bool correct)
        {
            if (b == null) return;
            var img = b.GetComponent<Image>();
            var col = correct ? CorrectColor : WrongColor;
            img.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);
            var border = b.transform.Find("Border")?.GetComponent<Image>();
            if (border != null) border.color = col;
            var lbl = UIFactory.Label(b);
            if (lbl != null && !lbl.text.StartsWith(Tick) && !lbl.text.StartsWith(Cross)) lbl.text = (correct ? Tick : Cross) + "  " + lbl.text;
            var fx = b.GetComponent<UIButtonFX>();
            if (fx != null) fx.baseBorder = col;
        }

        protected static void MarkGraphic(Graphic g, bool correct)
        {
            if (g == null) return;
            var col = correct ? CorrectColor : WrongColor;
            g.color = new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f);
        }

        /// <summary>A code block (monospace, dark background, line numbers).</summary>
        protected RectTransform CodeBlock(Transform parent, string code, float fontSize = 26)
        {
            var p = UIFactory.Panel(parent, UIFactory.Theme.code, 12, "Code", false);
            var lines = code.Split('\n');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++) sb.Append($"<color=#5b6a86>{i + 1,2}</color>  {Syntax(lines[i])}\n");
            var t = UIFactory.Mono(p.transform, sb.ToString().TrimEnd('\n'), fontSize);
            UIFactory.Stretch(t.rectTransform, 18, 12, 18, 12);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Size(p, -1, lines.Length * (fontSize * 1.32f) + 30);
            return p.rectTransform;
        }

        static readonly string[] Keywords = { "for", "while", "if", "elif", "else", "in", "range", "print", "def", "return", "and", "or", "not", "True", "False", "int", "input", "len" };

        /// <summary>Very small Python syntax highlighter for code blocks.</summary>
        public static string Syntax(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];
                if (c == '\'' || c == '"')
                {
                    int j = line.IndexOf(c, i + 1);
                    if (j < 0) j = line.Length - 1;
                    sb.Append("<color=#ffd166>").Append(Escape(line.Substring(i, j - i + 1))).Append("</color>");
                    i = j + 1; continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int j = i;
                    while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] == '_')) j++;
                    string w = line.Substring(i, j - i);
                    if (Array.IndexOf(Keywords, w) >= 0) sb.Append("<color=#ff5fd2>").Append(w).Append("</color>");
                    else sb.Append(w);
                    i = j; continue;
                }
                if (char.IsDigit(c))
                {
                    int j = i;
                    while (j < line.Length && (char.IsDigit(line[j]) || line[j] == '.')) j++;
                    sb.Append("<color=#7ee8fa>").Append(line.Substring(i, j - i)).Append("</color>");
                    i = j; continue;
                }
                sb.Append(Escape(c.ToString()));
                i++;
            }
            return sb.ToString();
        }

        static string Escape(string s) => s.Replace("<", "<noparse><</noparse>");

        /// <summary>Row of up to 4 answer buttons with key hints [1]..[4].</summary>
        protected List<Button> OptionButtons(Transform parent, IList<string> options, Action<int> onPick, bool twoColumns = true, float fontSize = 28)
        {
            var grid = UIFactory.Rect("Options", parent);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            int cols = twoColumns && options.Count > 2 ? 2 : Mathf.Max(1, Mathf.Min(options.Count, 4));
            float w = (1500 - (cols - 1) * 18) / cols;
            gl.cellSize = new Vector2(w, 86);
            gl.spacing = new Vector2(18, 14);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = cols;
            gl.childAlignment = TextAnchor.MiddleCenter;
            int rows = Mathf.CeilToInt(options.Count / (float)cols);
            UIFactory.Size(grid, -1, rows * 100);
            var list = new List<Button>();
            for (int i = 0; i < options.Count; i++)
            {
                int k = i;
                var b = UIFactory.Button(grid, $"<color=#40ebff><size=75%>[{i + 1}]</size></color>  {options[i]}", () => onPick(k), ButtonStyle.Answer, fontSize);
                UIFactory.Label(b).alignment = TextAlignmentOptions.MidlineLeft;
                list.Add(b);
            }
            if (list.Count > 0 && !InputHub.IsMobile) UIFactory.Select(list[0]);
            return list;
        }

        protected static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Returns a shuffled order that is not the identity (so ordering puzzles never start solved).</summary>
        protected static List<int> Derange(int n)
        {
            var idx = new List<int>();
            for (int i = 0; i < n; i++) idx.Add(i);
            if (n < 2) return idx;
            for (int tries = 0; tries < 10; tries++)
            {
                Shuffle(idx);
                bool same = true;
                for (int i = 0; i < n; i++) if (idx[i] != i) { same = false; break; }
                if (!same) break;
            }
            return idx;
        }
    }
}
