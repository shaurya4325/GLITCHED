using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>The mastery record (Section 9.2): a bar per concept. Encouraging, never shaming.</summary>
    public class MasteryScreen : SubScreen
    {
        public static void Open() => Create<MasteryScreen>(L.T("mastery.title", "MASTERY"), new Vector2(1400, 900));

        protected override void Build()
        {
            var list = UIFactory.Scroll(Body, out _, 10);
            UIFactory.Stretch((RectTransform)list.parent);
            var d = Services.Game.Data;
            var intro = UIFactory.Text(list, $"<color=#8fa3c0>Knowledge Points: </color><b><color=#ff3dc8>{d.knowledgePoints}</color></b>   <color=#8fa3c0>Concepts met: </color><b>{d.mastery.Count}</b>\n<size=80%><color=#8fa3c0>Every bar grows with each correct answer. Lower bars mean the game will give you more practice there, in new situations.</color></size>", 26);
            UIFactory.Size(intro, -1, 100);
            if (d.mastery.Count == 0)
            {
                var e = UIFactory.Text(list, "No concepts met yet. Go and fight something!", 30, UIFactory.Theme.textDim, TextAlignmentOptions.Center);
                UIFactory.Size(e, -1, 120);
                return;
            }
            foreach (var arc in Services.Db.arcs)
            {
                if (arc == null) continue;
                var concepts = arc.concepts.Where(c => d.mastery.ContainsKey(c)).ToList();
                if (concepts.Count == 0) continue;
                var head = UIFactory.Text(list, $"{arc.displayName.ToUpper()}  <size=70%><color=#8fa3c0>{arc.subject}</color></size>", 30, UIFactory.Theme.cyan, TextAlignmentOptions.Left, true);
                UIFactory.Size(head, -1, 50);
                foreach (var c in concepts) Row(list, c, d.mastery[c]);
            }
        }

        void Row(Transform list, string concept, ConceptMastery m)
        {
            var row = UIFactory.Rect("Row_" + concept, list);
            UIFactory.Size(row, -1, 52);
            var name = UIFactory.Text(row, concept, 26);
            UIFactory.Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(380, 50), new Vector2(0, 0.5f));
            var th = UIFactory.Theme;
            Color col = m.value >= 80 ? th.green : m.value >= 50 ? th.cyan : th.yellow;
            var bar = UIFactory.Bar(row, col, null, "Bar", 10);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.offsetMin = new Vector2(400, -13); brt.offsetMax = new Vector2(-180, 13);
            bar.Set(0, true);
            float target = m.value / 100f;
            Tween.After(0.15f, () => { if (bar) bar.Set(target); });
            string label = m.value >= 100 ? "MASTERED" : $"{Mathf.RoundToInt(m.value)}%";
            var pct = UIFactory.Text(row, label, 26, col, TextAlignmentOptions.Right, true);
            UIFactory.Place(pct.rectTransform, new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(160, 50), new Vector2(1, 0.5f));
        }
    }
}
