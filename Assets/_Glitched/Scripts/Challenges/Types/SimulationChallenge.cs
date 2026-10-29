using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 6. Simulation: run a mini experiment with sliders. "goal": tune the sliders so the result lands in range,
    /// then RUN to see it play out. "read": experiment freely, then answer what you observed.
    /// </summary>
    public class SimulationChallenge : ChallengeBase
    {
        SimBase sim;
        UIGraph graph;
        TextMeshProUGUI readout;
        List<Button> buttons;
        float playStart = -1;
        bool pendingResult;
        bool resultOk;
        int picked = -1;
        bool Goal => D.mode != "read";
        public override bool AutoSubmits => !Goal;
        public override string SubmitLabel => "RUN  ▶";
        public override float PlaybackSeconds => 0.3f;

        protected override void Build()
        {
            sim = SimBase.Create(D.sim);
            sim.SetFixed(D.@fixed);
            if (D.goal != null) { sim.Set("_goalMin", (float)D.goal.min); sim.Set("_goalMax", (float)D.goal.max); }
            UIFactory.HLayout(Area, 22, 0, TextAnchor.UpperLeft, false);
            var stage = UIFactory.Panel(Area, new Color(0.02f, 0.03f, 0.06f, 1f), 16, "Stage", false);
            UIFactory.Size(stage, 980, -1, 1, 1);
            graph = UIGraph.Create(stage.transform);
            UIFactory.Stretch(graph.rectTransform, 8, 8, 8, 52);
            readout = UIFactory.Text(stage.transform, "", 24, UIFactory.Theme.text, TextAlignmentOptions.Center);
            UIFactory.Place(readout.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(940, 44), new Vector2(0.5f, 0));

            var right = UIFactory.Rect("Controls", Area);
            UIFactory.Size(right, 660, -1, 0, 1);
            UIFactory.VLayout(right, 6);
            if (D.sliders != null)
                foreach (var s in D.sliders)
                {
                    var def = s;
                    sim.Set(def.id, def.value);
                    var slider = UIFactory.Slider(right, def.label, def.min, def.max, def.value, v => { if (!Done && playStart < 0) sim.Set(def.id, v); },
                        v => $"{v:0.##} {def.unit}", false, 200, 150);
                    if (def.step > 0) slider.onValueChanged.AddListener(v => { float q = Mathf.Round(v / def.step) * def.step; if (!Mathf.Approximately(q, v)) slider.SetValueWithoutNotify(q); sim.Set(def.id, q); });
                }
            if (!Goal)
            {
                buttons = OptionButtons(right, D.options, Pick, false, 22);
                var gl = buttons.Count > 0 ? buttons[0].transform.parent.GetComponent<GridLayoutGroup>() : null;
                if (gl != null) { gl.constraintCount = 1; gl.cellSize = new Vector2(640, 56); gl.spacing = new Vector2(8, 8); UIFactory.Size(gl, -1, D.options.Count * 64); }
                Ctx.setHelp("Move the sliders and watch. Then pick what you found.");
            }
            else Ctx.setHelp($"Goal: {D.goal.metric} between {QuestionData.FormatNumber(D.goal.min)} and {QuestionData.FormatNumber(D.goal.max)} {D.goal.unit}. Set the sliders, then RUN.");
        }

        void Update()
        {
            if (sim == null) return;
            float t = playStart < 0 ? -1 : Mathf.Clamp01((Time.unscaledTime - playStart) / sim.PlayDuration);
            sim.Draw(graph, t, Time.unscaledTime);
            readout.text = sim.Readout();
            if (pendingResult && t >= 1f)
            {
                pendingResult = false;
                Finish(resultOk);
            }
        }

        void Pick(int i) { if (!Done) { picked = i; OnSubmit(); } }
        public override void OnAnswerKey(int index) { if (!Goal && index < D.options.Count) Pick(index); }
        public override bool CanSubmit => Goal ? playStart < 0 : picked >= 0;

        public override void OnSubmit()
        {
            if (Done) return;
            if (Goal)
            {
                if (playStart >= 0) return;
                float m = sim.Metric(D.goal.metric);
                resultOk = m >= D.goal.min - 1e-4 && m <= D.goal.max + 1e-4;
                playStart = Time.unscaledTime;
                pendingResult = true;
                Services.Audio.Play("launch");
                foreach (var s in Area.GetComponentsInChildren<Slider>()) s.interactable = false;
            }
            else Finish(picked == D.answerIndex);
        }

        public override void Reveal(bool correct)
        {
            foreach (var s in Area.GetComponentsInChildren<Slider>()) s.interactable = false;
            if (buttons != null)
            {
                if (picked >= 0 && picked != D.answerIndex) Mark(buttons[picked], false);
                Mark(buttons[D.answerIndex], true);
                foreach (var b in buttons) b.interactable = false;
            }
        }
    }
}
