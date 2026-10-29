using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>8. Prediction: choose what happens next, then the game plays it out.</summary>
    public class PredictionChallenge : ChallengeBase
    {
        List<Button> buttons;
        int picked = -1;
        UIGraph stage;
        TextMeshProUGUI outcomeText;
        float playStart = -1;
        public override bool AutoSubmits => true;
        public override float PlaybackSeconds => 2.4f;

        protected override void Build()
        {
            var h = UIFactory.HLayout(Area, 24, 0, TextAnchor.UpperLeft, false);
            var left = UIFactory.Rect("Left", Area);
            UIFactory.Size(left, 1000, -1, 1, 1);
            UIFactory.VLayout(left, 12);
            if (!string.IsNullOrEmpty(D.code)) CodeBlock(left, D.code);
            buttons = OptionButtons(left, D.options, Pick, false, 26);
            var gl = buttons.Count > 0 ? buttons[0].transform.parent.GetComponent<GridLayoutGroup>() : null;
            if (gl != null)
            {
                // with code above them, four options go in two columns so they stay inside the panel
                bool twoCols = !string.IsNullOrEmpty(D.code) && D.options.Count > 2;
                int rows = twoCols ? (D.options.Count + 1) / 2 : D.options.Count;
                gl.constraintCount = twoCols ? 2 : 1;
                gl.cellSize = twoCols ? new Vector2(484, 60) : new Vector2(980, 64);
                gl.spacing = new Vector2(12, 10);
                UIFactory.Size(gl, -1, rows * (twoCols ? 70 : 78));
            }

            var right = UIFactory.Panel(Area, new Color(0.02f, 0.03f, 0.06f, 1f), 16, "Stage");
            UIFactory.Size(right, 600, -1, 0, 1);
            stage = UIGraph.Create(right.transform, "StageGraph");
            UIFactory.Stretch(stage.rectTransform, 10, 10, 10, 60);
            outcomeText = UIFactory.Text(right.transform, "?", 24, UIFactory.Theme.textDim, TextAlignmentOptions.Center);
            UIFactory.Place(outcomeText.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(580, 56), new Vector2(0.5f, 0));
            Ctx.setHelp("Predict what happens, then watch.");
        }

        void Pick(int i)
        {
            if (Done) return;
            picked = i;
            OnSubmit();
        }

        public override void OnAnswerKey(int index) { if (index < D.options.Count) Pick(index); }
        public override bool CanSubmit => picked >= 0;
        public override void OnSubmit() { if (!Done) Finish(picked == D.answerIndex); }

        public override void Reveal(bool correct)
        {
            if (picked >= 0 && picked != D.answerIndex) Mark(buttons[picked], false);
            Mark(buttons[D.answerIndex], true);
            foreach (var b in buttons) b.interactable = false;
            outcomeText.text = D.outcome;
            outcomeText.color = UIFactory.Theme.text;
            playStart = Time.unscaledTime;
        }

        void Update()
        {
            if (stage == null) return;
            float t = playStart < 0 ? 0 : Mathf.Clamp01((Time.unscaledTime - playStart) / 2.2f);
            PredictionAnimator.Draw(stage, D.anim, t, playStart >= 0, Time.unscaledTime);
        }
    }
}
