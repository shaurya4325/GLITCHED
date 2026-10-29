using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>4. Debugging: find and tap the one wrong line.</summary>
    public class DebuggingChallenge : ChallengeBase
    {
        readonly List<Button> lineButtons = new List<Button>();
        int picked = -1;
        public override bool AutoSubmits => true;

        protected override void Build()
        {
            UIFactory.VLayout(Area, 6, 0, TextAnchor.UpperCenter);
            var code = UIFactory.Panel(Area, UIFactory.Theme.code, 14, "Code", false);
            UIFactory.VLayout(code, 4, 14);
            float rowH = D.lines.Count > 5 ? 46 : 54;
            UIFactory.Size(code, -1, D.lines.Count * (rowH + 4) + 30);
            for (int i = 0; i < D.lines.Count; i++)
            {
                int k = i;
                var b = UIFactory.Button(code.transform, $"<color=#5b6a86>{i + 1,2}</color>   {Syntax(D.lines[i])}", () => Pick(k), ButtonStyle.Ghost, 26);
                var lbl = UIFactory.Label(b);
                lbl.font = UIFactory.MonoFont;
                lbl.alignment = TextAlignmentOptions.MidlineLeft;
                lbl.textWrappingMode = TextWrappingModes.NoWrap;
                UIFactory.Size(b, -1, rowH);
                lineButtons.Add(b);
            }
            Ctx.setHelp("Tap the line with the bug (or press its number).");
        }

        void Pick(int i)
        {
            if (Done) return;
            picked = i;
            OnSubmit();
        }

        public override void OnAnswerKey(int index) { if (index < lineButtons.Count) Pick(index); }
        public override bool CanSubmit => picked >= 0;
        public override void OnSubmit() { if (!Done) Finish(picked == D.answerIndex); }

        public override void Reveal(bool correct)
        {
            if (picked >= 0 && picked != D.answerIndex) Mark(lineButtons[picked], false);
            var right = lineButtons[D.answerIndex];
            Mark(right, true);
            if (!string.IsNullOrEmpty(D.fix))
                UIFactory.Label(right).text += $"     <color=#7cffa0>→ {Syntax(D.fix.Trim())}</color>";
            foreach (var b in lineButtons) b.interactable = false;
        }
    }
}
