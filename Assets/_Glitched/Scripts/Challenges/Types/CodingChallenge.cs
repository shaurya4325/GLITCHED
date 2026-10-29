using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 5. Coding: complete or repair code (Python-like, as in the CBSE CS syllabus).
    /// "blank": pick what fills the ____ line. "arrange": drag the shuffled blocks into a working program, then RUN.
    /// </summary>
    public class CodingChallenge : ChallengeBase
    {
        List<Button> buttons;
        ReorderList reorder;
        TextMeshProUGUI console;
        int picked = -1;
        string Mode => string.IsNullOrEmpty(D.mode) ? "blank" : D.mode;
        public override bool AutoSubmits => Mode == "blank";
        public override float PlaybackSeconds => 1.0f;
        public override string SubmitLabel => "RUN  ▶";

        protected override void Build()
        {
            UIFactory.HLayout(Area, 24, 0, TextAnchor.UpperLeft, false);
            var left = UIFactory.Rect("Left", Area);
            UIFactory.Size(left, 1100, -1, 1, 1);
            UIFactory.VLayout(left, 12);
            if (Mode == "arrange")
            {
                reorder = ReorderList.Create(left, D.lines, Derange(D.lines.Count), true, D.lines.Count > 5 ? 48 : 56);
                Ctx.setHelp("Drag the blocks into a working order (indentation matters!), then RUN.");
            }
            else
            {
                var code = new System.Text.StringBuilder();
                foreach (var l in D.lines) code.AppendLine(l.Replace("____", "<color=#ff3dc8><b>[ ? ]</b></color>"));
                var block = CodeBlock(left, code.ToString().TrimEnd());
                buttons = OptionButtons(left, D.options, Pick, true, 24);
                foreach (var b in buttons) UIFactory.Label(b).font = UIFactory.MonoFont;
                Ctx.setHelp("Choose the code that fills the gap (or press 1-4).");
            }
            var term = UIFactory.Panel(Area, new Color(0.01f, 0.03f, 0.02f, 1f), 14, "Console", false);
            UIFactory.Size(term, 520, -1, 0, 1);
            var title = UIFactory.Text(term.transform, "OUTPUT", 18, new Color(0.3f, 1f, 0.5f, 0.7f), TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(16, -10), new Vector2(300, 30), new Vector2(0, 1));
            console = UIFactory.Mono(term.transform, "<color=#3f6b4a>> waiting for RUN...</color>", 24, new Color(0.45f, 1f, 0.6f));
            UIFactory.Stretch(console.rectTransform, 16, 44, 16, 16);
            console.textWrappingMode = TextWrappingModes.Normal;
        }

        void Pick(int i)
        {
            if (Done) return;
            picked = i;
            OnSubmit();
        }

        public override void OnAnswerKey(int index) { if (Mode == "blank" && index < D.options.Count) Pick(index); }
        public override bool CanSubmit => Mode == "arrange" || picked >= 0;

        public override void OnSubmit()
        {
            if (Done) return;
            bool ok;
            if (Mode == "arrange")
            {
                ok = true;
                // accept any order that produces identical code (duplicate lines may swap)
                for (int i = 0; i < reorder.order.Count; i++) if (D.lines[reorder.order[i]] != D.lines[i]) { ok = false; break; }
                reorder.Lock();
            }
            else ok = picked == D.answerIndex;
            Finish(ok);
        }

        public override void Reveal(bool correct)
        {
            if (buttons != null)
            {
                if (picked >= 0 && picked != D.answerIndex) Mark(buttons[picked], false);
                if (D.answerIndex >= 0 && D.answerIndex < buttons.Count) Mark(buttons[D.answerIndex], true);
                foreach (var b in buttons) b.interactable = false;
            }
            if (reorder != null)
                for (int i = 0; i < reorder.rows.Count; i++) MarkGraphic(reorder.rows[i].GetComponent<Image>(), D.lines[reorder.order[i]] == D.lines[i]);
            string outText = correct
                ? $"> run program.py\n{(string.IsNullOrEmpty(D.output) ? "Program finished OK." : D.output)}\n<color=#7cffa0>Process exited with code 0</color>"
                : "> run program.py\n<color=#ff5a6a>" + (Q.concept == "Loops" ? "...\n...\n...\nERROR: loop never ended (stopped after 10,000 steps)" : "Traceback (most recent call last):\n  LogicError: the program does not do what it should") + "</color>";
            console.text = "";
            var g = console.gameObject.AddComponent<GlitchText>();
            g.intensityOverride = correct ? 0f : 0.4f;
            g.charsPerSecond = 90;
            g.Show(outText);
            Services.Audio.Play(correct ? "type" : "error");
        }
    }
}
