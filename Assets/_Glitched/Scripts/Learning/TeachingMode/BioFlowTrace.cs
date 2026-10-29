using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Biology lab: trace a system by tapping along it (blood flow, the airway, a food chain) while the
    /// organism reacts: the heart pulses and cells flow along every step you get right.
    /// </summary>
    public class BioFlowTrace : TeachingLab
    {
        string[] path;
        Vector2[] positions;
        string system;
        int progress = 1;            // first node is given
        readonly List<Button> nodes = new List<Button>();
        UIGraph graph;
        float wince;

        protected override string Title => system == "air" ? "TRACE THE BREATH" : system == "food" ? "TRACE THE ENERGY" : "TRACE THE BLOOD";
        protected override string GoalDescription => $"Tap the next stop, in order ({progress}/{path.Length}).";

        protected override string Opening()
        {
            if (Boss != null && Boss.teachingLines.Count > 0) return Boss.teachingLines[0];
            return system == "air" ? "Follow a breath from your nose to where oxygen enters the blood." :
                   system == "food" ? "Energy flows from the Sun through every eater. Follow it." :
                   "Blood goes round twice: body to lungs to body. Follow one red blood cell.";
        }

        protected override void BuildLab(RectTransform area)
        {
            string c = Q != null ? Q.concept : "Circulation";
            if (c == "Respiration")
            {
                system = "air";
                path = new[] { "Nose", "Pharynx", "Larynx", "Trachea", "Bronchi", "Bronchioles", "Alveoli", "Blood" };
            }
            else if (c == "Ecosystems")
            {
                system = "food";
                path = new[] { "Sun", "Grass", "Grasshopper", "Frog", "Snake", "Eagle" };
            }
            else
            {
                system = "blood";
                path = new[] { "Body", "Vena cava", "Right atrium", "Right ventricle", "Pulmonary artery", "Lungs", "Pulmonary vein", "Left atrium", "Left ventricle", "Aorta" };
            }
            var stage = UIFactory.Panel(area, new Color(0.08f, 0.02f, 0.04f, 1f), 18, "Organism", false);
            UIFactory.Stretch(stage.rectTransform);
            graph = UIGraph.Create(stage.transform);
            UIFactory.Stretch(graph.rectTransform);
            var size = new Vector2(1680, 560);
            positions = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                float a = -i / (float)path.Length * Mathf.PI * 2 + Mathf.PI;
                positions[i] = size / 2 + new Vector2(Mathf.Cos(a) * 690, Mathf.Sin(a) * 210);
            }
            // shuffle which label sits where? No: positions follow the real loop; the player must pick in order.
            var order = new List<int>();
            for (int i = 0; i < path.Length; i++) order.Add(i);
            for (int i = 0; i < path.Length; i++)
            {
                int idx = i;
                var b = UIFactory.Button(stage.transform, path[i], () => Tap(idx), i == 0 ? ButtonStyle.Primary : ButtonStyle.Answer, 22);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = positions[i]; rt.sizeDelta = new Vector2(230, 70);
                nodes.Add(b);
            }
            // scramble the labels so the ring position isn't a giveaway (keep the first fixed)
            var labels = new List<string>(path);
            var rest = labels.GetRange(1, labels.Count - 1);
            for (int i = rest.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (rest[i], rest[j]) = (rest[j], rest[i]); }
            scrambled = new List<string> { labels[0] };
            scrambled.AddRange(rest);
            for (int i = 0; i < nodes.Count; i++) UIFactory.Label(nodes[i]).text = scrambled[i];
            Mark(nodes[0], true);
        }

        List<string> scrambled;

        void Tap(int nodeIndex)
        {
            if (Solved) return;
            Interacted();
            string label = scrambled[nodeIndex];
            if (label == path[progress])
            {
                Mark(nodes[nodeIndex], true);
                linked.Add(nodeIndex);
                progress++;
                Services.Audio.Play("correct", 0.6f);
                if (progress >= path.Length)
                    MarkSolved(system == "blood" ? "Body, right heart, lungs, left heart, body. Twice through the heart in one lap: double circulation!" :
                               system == "air" ? "Air reaches the alveoli, and oxygen diffuses into the blood. That's gas exchange." :
                               "Only about 10% of the energy passes up each step. That's why there are so few eagles.");
                else Say($"Yes: {path[progress - 1]}. Where next?");
            }
            else
            {
                wince = 1f;
                Services.Audio.Play("wrong", 0.6f);
                Say(Boss != null ? $"No, no, NO. That's not where {(system == "blood" ? "blood" : system == "air" ? "air" : "energy")} goes after {path[progress - 1]}." : $"Not quite. What comes right after {path[progress - 1]}?");
            }
            RefreshGoal();
        }

        readonly List<int> linked = new List<int> { 0 };

        static void Mark(Button b, bool ok)
        {
            var img = b.GetComponent<Image>();
            var col = ok ? UIFactory.Theme.green : UIFactory.Theme.red;
            img.color = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 1f);
        }

        protected override void Update()
        {
            base.Update();
            if (graph == null) return;
            graph.Clear();
            float t = Time.unscaledTime;
            var size = new Vector2(1680, 560);
            float beat = Mathf.Repeat(t * 1.2f, 1f);
            float pulse = beat < 0.15f ? Mathf.Sin(beat / 0.15f * Mathf.PI) : 0;
            wince = Mathf.MoveTowards(wince, 0, Time.unscaledDeltaTime * 2);
            // the organism: a pulsing heart in the middle
            Color heart = Color.Lerp(new Color(0.9f, 0.2f, 0.3f), Color.white, wince * 0.5f);
            float r = 70 + pulse * 18 + wince * 10;
            graph.Circle(size / 2 + new Vector2(-r * 0.45f, r * 0.2f), r * 0.6f, heart);
            graph.Circle(size / 2 + new Vector2(r * 0.45f, r * 0.2f), r * 0.6f, heart);
            graph.Circle(size / 2 + new Vector2(0, -r * 0.3f), r * 0.55f, heart);
            // traced links with flowing cells
            for (int k = 0; k < linked.Count - 1; k++)
            {
                Vector2 a = positions[linked[k]], b = positions[linked[k + 1]];
                graph.Line(a, b, new Color(1f, 0.4f, 0.5f, 0.6f), 8);
                for (int c = 0; c < 3; c++)
                {
                    float f = Mathf.Repeat(t * 0.8f + c / 3f, 1f);
                    graph.Circle(Vector2.Lerp(a, b, f), 9, k < 4 && system == "blood" ? new Color(0.4f, 0.5f, 1f) : new Color(1f, 0.25f, 0.3f));
                }
            }
        }
    }
}
