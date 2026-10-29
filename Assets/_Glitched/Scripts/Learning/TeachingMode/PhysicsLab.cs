using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Physics lab: sliders for velocity, angle, gravity, mass and force. Change one, watch the world respond
    /// immediately, and find the relationship yourself (projectile, F = ma cart, Ohm's law circuit, pendulum).
    /// </summary>
    public class PhysicsLab : TeachingLab
    {
        SimBase sim;
        UIGraph graph;
        TextMeshProUGUI readout;
        string simKey;
        float playStart = -1;
        float target;
        string metric;
        float tolerance;
        string unit;
        bool massTouched;

        protected override string Title => simKey == "cart" ? "THE FORCE LAB" : simKey == "ohm" ? "THE CIRCUIT LAB" : simKey == "pendulum" ? "THE PENDULUM LAB" : "THE LAUNCH LAB";

        protected override string GoalDescription => simKey == "projectile" ? $"Land the probe on the platform at <b>{target:0} m</b>, then RUN."
            : simKey == "cart" ? $"Give the cart an acceleration of <b>{target:0} m/s²</b>, then RUN."
            : simKey == "ohm" ? $"Make the current exactly <b>{target:0.0} A</b>."
            : $"Make the pendulum's period <b>{target:0.0} s</b>.";

        protected override string Opening()
        {
            if (Boss != null && Boss.teachingLines.Count > 0) return Boss.teachingLines[0];
            switch (simKey)
            {
                case "cart": return "Push harder, it speeds up faster. Make it heavier, it gets lazy. That's F = ma.";
                case "ohm": return "Voltage pushes, resistance resists. The current is what's left: I = V / R.";
                case "pendulum": return "Longer string, slower swing. Stronger gravity, faster swing. Try it.";
                default: return "Change ONE slider at a time and watch where it lands. Hint: does mass even matter here?";
            }
        }

        protected override void BuildLab(RectTransform area)
        {
            string c = Q != null ? Q.concept : "Motion";
            simKey = c == "Newton's Laws" ? "cart" : (c == "Electricity" || c == "Magnetism") ? "ohm" : (c == "Work & Energy" || c == "Waves") ? "pendulum" : "projectile";
            sim = SimBase.Create(simKey);
            UIFactory.HLayout(area, 24, 0, TextAnchor.UpperLeft, false);
            var stage = UIFactory.Panel(area, new Color(0.02f, 0.03f, 0.06f, 1f), 16, "Stage", false);
            UIFactory.Size(stage, 1060, -1, 1, 1);
            graph = UIGraph.Create(stage.transform);
            UIFactory.Stretch(graph.rectTransform, 10, 10, 10, 60);
            readout = UIFactory.Text(stage.transform, "", 26, UIFactory.Theme.text, TextAlignmentOptions.Center);
            UIFactory.Place(readout.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(1000, 50), new Vector2(0.5f, 0));
            var right = UIFactory.Rect("Sliders", area);
            UIFactory.Size(right, 600, -1);
            UIFactory.VLayout(right, 8);

            switch (simKey)
            {
                case "cart":
                    target = 4; metric = "a"; tolerance = 0.3f; unit = "m/s²";
                    S(right, "F", "Force", 0, 200, 40, "N"); S(right, "m", "Mass", 5, 50, 20, "kg");
                    break;
                case "ohm":
                    target = 0.5f; metric = "I"; tolerance = 0.03f; unit = "A";
                    S(right, "V", "Voltage", 1, 24, 12, "V"); S(right, "R", "Resistance", 2, 48, 6, "Ω");
                    break;
                case "pendulum":
                    target = 2f; metric = "T"; tolerance = 0.1f; unit = "s";
                    S(right, "L", "Length", 0.2f, 4, 2.5f, "m"); S(right, "g", "Gravity", 1, 20, 9.8f, "m/s²");
                    break;
                default:
                    target = Random.Range(30, 50); metric = "range"; tolerance = 3f; unit = "m";
                    sim.Set("_goalMin", target - tolerance); sim.Set("_goalMax", target + tolerance);
                    S(right, "v", "Speed", 5, 30, 14, "m/s"); S(right, "angle", "Angle", 10, 80, 25, "°");
                    S(right, "g", "Gravity", 2, 20, 10, "m/s²"); S(right, "mass", "Mass", 1, 50, 5, "kg");
                    break;
            }
            if (simKey == "projectile" || simKey == "cart")
            {
                var run = UIFactory.Button(right, "RUN  ▶", Run, ButtonStyle.Primary, 30);
                UIFactory.Size(run, -1, 80);
            }
        }

        void S(Transform parent, string id, string label, float min, float max, float value, string unit)
        {
            sim.Set(id, value);
            UIFactory.Slider(parent, label, min, max, value, v =>
            {
                sim.Set(id, v);
                Interacted();
                if (id == "mass" && !massTouched) { massTouched = true; Say("Notice something? The mass changed... and the landing spot didn't. Without air, mass doesn't affect a projectile's path."); }
                if (simKey == "ohm" || simKey == "pendulum") Check();
            }, v => $"{v:0.#} {unit}", false, 170, 150);
        }

        void Run()
        {
            if (playStart >= 0 && Time.unscaledTime - playStart < sim.PlayDuration) return;
            playStart = Time.unscaledTime;
            Interacted();
            Services.Audio.Play("launch");
        }

        void Check()
        {
            float m = sim.Metric(metric);
            if (Mathf.Abs(m - target) <= tolerance)
                MarkSolved(simKey == "ohm" ? "I = V / R. Double the resistance and you halve the current." :
                           simKey == "pendulum" ? "T = 2π√(L/g). Only length and gravity matter, never the mass." :
                           simKey == "cart" ? "F = ma. Same force on a heavier cart means less acceleration." :
                           "Bullseye! R = v² sin 2θ / g. 45° goes furthest, and mass never appears in it.");
            else if (simKey != "ohm" && simKey != "pendulum") Say(m < target ? "Too short. What makes it go further?" : "Too far! Ease off something.");
        }

        protected override void Update()
        {
            base.Update();
            if (sim == null) return;
            float t = playStart < 0 ? -1 : Mathf.Clamp01((Time.unscaledTime - playStart) / sim.PlayDuration);
            sim.Draw(graph, t, Time.unscaledTime);
            readout.text = sim.Readout();
            if (t >= 1f) { playStart = -1; Check(); }
        }
    }
}
