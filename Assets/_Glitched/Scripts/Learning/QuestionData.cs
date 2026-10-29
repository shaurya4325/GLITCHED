using System;
using System.Collections.Generic;

namespace Glitched
{
    /// <summary>The ten challenge types (Section 8 of the design).</summary>
    public enum ChallengeType { DragDrop, Logic, Numerical, Debugging, Coding, Simulation, Matching, Prediction, ShortAnswer, Construction }

    /// <summary>One question from StreamingAssets/Questions/*.json. See README in that folder for the format.</summary>
    [Serializable]
    public class QuestionData
    {
        public string id;
        public string arc;
        public string subject;
        public string concept;
        public string topic;                 // Class XI lesson id (LessonData.id): the MCQs small enemies ask after that lesson
        public int difficulty = 1;           // 1 Recall .. 5 Synthesis
        public ChallengeType type;
        public string situation;
        public string prompt;
        public QuestionPayload data = new QuestionPayload();
        public float fastSeconds = 8;
        public float timeLimit;              // 0 = automatic
        public string explanation;
        public string hint;
        public string teachingModule;
        public bool needsTeacherReview;

        /// <summary>Seconds before the enemy's attack lands.</summary>
        public float TimeLimit(bool relaxed)
        {
            float t = timeLimit > 0 ? timeLimit : Math.Max(14f, fastSeconds * 2.6f + 4f);
            return relaxed ? t * 1.75f : t;
        }

        /// <summary>A short human-readable version of the correct answer (shown after a wrong answer).</summary>
        public string CorrectAnswerText()
        {
            var d = data;
            switch (type)
            {
                case ChallengeType.Numerical:
                    return $"{FormatNumber(d.value)} {d.unit}".Trim();
                case ChallengeType.Debugging:
                    return d.lines != null && d.answerIndex >= 0 && d.answerIndex < d.lines.Count
                        ? $"Line {d.answerIndex + 1}: <b>{d.lines[d.answerIndex].Trim()}</b>" + (string.IsNullOrEmpty(d.fix) ? "" : $"  ->  <b>{d.fix.Trim()}</b>")
                        : "";
                case ChallengeType.ShortAnswer:
                    return d.accepted != null && d.accepted.Count > 0 ? d.accepted[0] : "";
                case ChallengeType.Matching:
                    {
                        var parts = new List<string>();
                        for (int i = 0; i < d.left.Count && i < d.right.Count; i++) parts.Add($"{d.left[i]} = {d.right[i]}");
                        return string.Join("   ", parts);
                    }
                case ChallengeType.DragDrop:
                    {
                        if (d.items == null) return "";
                        var parts = new List<string>();
                        if (d.answer != null)
                        {
                            foreach (var a in d.answer) if (a >= 0 && a < d.items.Count) parts.Add(d.items[a]);
                        }
                        else if (d.answerSets != null)
                        {
                            foreach (var set in d.answerSets) if (set.Count > 0) parts.Add(d.items[set[0]]);
                        }
                        return string.Join(", ", parts);
                    }
                case ChallengeType.Coding:
                    if (d.mode == "arrange") return "the full working order (shown in the code panel)";
                    return d.options != null && d.answerIndex >= 0 && d.answerIndex < d.options.Count ? d.options[d.answerIndex] : "";
                case ChallengeType.Construction:
                    if (d.mode == "sequence" && d.sequence != null) return string.Join(" -> ", d.sequence);
                    if (d.required != null)
                    {
                        var parts = new List<string>();
                        foreach (var kv in d.required) parts.Add($"{kv.Value} x {kv.Key}");
                        return (string.IsNullOrEmpty(d.target) ? "" : d.target + ": ") + string.Join(" + ", parts);
                    }
                    return "";
                case ChallengeType.Logic:
                    if (d.mode == "order" && d.items != null) return string.Join(" -> ", d.items);
                    if (d.mode == "truth" && d.answers != null) return "Outputs: " + string.Join(" ", d.answers);
                    return d.options != null && d.answerIndex >= 0 && d.answerIndex < d.options.Count ? d.options[d.answerIndex] : "";
                case ChallengeType.Simulation:
                    if (d.mode == "goal" && d.goal != null) return $"{d.goal.metric} between {FormatNumber(d.goal.min)} and {FormatNumber(d.goal.max)} {d.goal.unit}";
                    return d.options != null && d.answerIndex >= 0 && d.answerIndex < d.options.Count ? d.options[d.answerIndex] : "";
                default:
                    return d.options != null && d.answerIndex >= 0 && d.answerIndex < d.options.Count ? d.options[d.answerIndex] : "";
            }
        }

        public static string FormatNumber(double v)
        {
            if (Math.Abs(v - Math.Round(v)) < 1e-6) return ((long)Math.Round(v)).ToString();
            return v.ToString("0.###");
        }
    }

    /// <summary>Type-specific data. Each challenge type only reads the fields it needs.</summary>
    [Serializable]
    public class QuestionPayload
    {
        public string mode;                  // Logic: choice/order/truth, Coding: blank/arrange, Simulation: goal/read, Construction: count/sequence
        public string code;                  // optional code shown above the question
        public List<string> options;
        public int answerIndex = -1;

        // Drag & Drop
        public string template;              // text with {0}, {1}, ... slots
        public List<string> items;           // draggable items (Logic order: items in the CORRECT order)
        public List<int> answer;             // correct item index per slot
        public List<List<int>> answerSets;   // or: acceptable item indices per slot
        public bool reuse;                   // items can be used more than once

        // Logic truth tables
        public List<string> headers;
        public List<List<string>> rows;
        public List<string> answers;

        // Numerical
        public double value;
        public double tolerance;
        public string unit;
        public List<string> givens;

        // Debugging
        public List<string> lines;           // Debugging + Coding
        public string fix;

        // Coding
        public string output;

        // Simulation
        public string sim;
        public List<SliderDef> sliders;
        public Dictionary<string, double> @fixed;
        public SimGoal goal;

        // Matching
        public List<string> left;
        public List<string> right;

        // Prediction
        public string outcome;
        public string anim;

        // Short answer
        public List<string> accepted;
        public List<List<string>> keywords;
        public string placeholder;

        // Construction
        public string display;
        public List<string> parts;
        public Dictionary<string, int> required;
        public List<string> sequence;
        public string target;

    }

    [Serializable]
    public class SliderDef
    {
        public string id;
        public string label;
        public float min, max, step = 1, value;
        public string unit;
    }

    [Serializable]
    public class SimGoal
    {
        public string metric;
        public double min, max;
        public string unit;
    }
}
