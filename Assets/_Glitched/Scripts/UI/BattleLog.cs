using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The battle log (Section 6.3): a few fading lines at the left edge.
    /// e.g. "BUG MONSTER uses OFF-BY-ONE." / "CORRECT in 4.1s, CRITICAL -120 HP".
    /// </summary>
    public class BattleLog : MonoBehaviour
    {
        readonly List<(TextMeshProUGUI text, float born)> lines = new List<(TextMeshProUGUI, float)>();
        RectTransform rt;
        const int MaxLines = 6;

        public void Build()
        {
            rt = (RectTransform)transform;
            UIFactory.Place(rt, new Vector2(0, 0.5f), new Vector2(32, 80), new Vector2(760, 320), new Vector2(0, 0.5f));
        }

        public void Add(string line, Color? color = null)
        {
            var t = UIFactory.Text(rt, line, 22, color ?? UIFactory.Theme.text, TextAlignmentOptions.BottomLeft);
            t.font = UIFactory.MonoFont;
            t.rectTransform.anchorMin = new Vector2(0, 0); t.rectTransform.anchorMax = new Vector2(1, 0);
            t.rectTransform.pivot = new Vector2(0, 0);
            t.rectTransform.sizeDelta = new Vector2(0, 34);
            t.outlineWidth = 0.2f; t.outlineColor = new Color32(5, 5, 12, 220);
            lines.Add((t, Time.unscaledTime));
            while (lines.Count > MaxLines) { Destroy(lines[0].text.gameObject); lines.RemoveAt(0); }
            Layout();
        }

        void Layout()
        {
            float y = 0;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                lines[i].text.rectTransform.anchoredPosition = new Vector2(0, y);
                y += 36;
            }
        }

        void Update()
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                float age = Time.unscaledTime - lines[i].born;
                if (age > 9f) { Destroy(lines[i].text.gameObject); lines.RemoveAt(i); Layout(); continue; }
                lines[i].text.alpha = age > 7f ? 1f - (age - 7f) / 2f : 1f;
            }
        }

        public void Clear()
        {
            foreach (var l in lines) if (l.text) Destroy(l.text.gameObject);
            lines.Clear();
        }
    }
}
