using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// KANHA'S LINES: write Kanha the Dard's dialogue inside the game. Pick a moment (intro, taunts, reactions...),
    /// type one line per row, SAVE. The final exam uses the new lines straight away, and they appear in the
    /// Voice Studio (KANHA group) so they can be recorded. Open from Settings.
    /// </summary>
    public class KanhaWriter : SubScreen
    {
        public static void Open() => Create<KanhaWriter>("KANHA'S LINES", new Vector2(1600, 940));

        string section = "intro";
        TMP_InputField input;
        TextMeshProUGUI note, status;
        readonly Dictionary<string, Image> tabs = new Dictionary<string, Image>();

        protected override void Build()
        {
            var th = UIFactory.Theme;
            KanhaLines.Reload();
            // the moments, as a column of buttons on the left
            var list = UIFactory.Rect("Sections", Body);
            UIFactory.Place(list, new Vector2(0, 1), Vector2.zero, new Vector2(330, 760), new Vector2(0, 1));
            var vl = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8; vl.childForceExpandHeight = false; vl.childControlHeight = false; vl.childControlWidth = true;
            foreach (var (key, _) in KanhaLines.Sections)
            {
                string k = key;
                var b = UIFactory.Button(list, key.Replace('_', ' ').ToUpperInvariant(), () => Pick(k), ButtonStyle.Secondary, 22);
                UIFactory.Size(b, 330, 60);
                tabs[k] = b.GetComponent<Image>();
            }
            note = UIFactory.Text(Body, "", 26, th.textDim, TextAlignmentOptions.Left);
            UIFactory.Place(note.rectTransform, new Vector2(0, 1), new Vector2(370, 0), new Vector2(1130, 40), new Vector2(0, 1));
            input = UIFactory.Input(Body, "One line per row...", 30);
            UIFactory.Place(input.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(370, -50), new Vector2(1130, 560), new Vector2(0, 1));
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.textComponent.alignment = TextAlignmentOptions.TopLeft;
            input.textComponent.enableWordWrapping = true;
            if (input.placeholder is TextMeshProUGUI ph) ph.alignment = TextAlignmentOptions.TopLeft;
            status = UIFactory.Text(Body, "", 24, th.yellow, TextAlignmentOptions.Left);
            UIFactory.Place(status.rectTransform, new Vector2(0, 0), new Vector2(370, 100), new Vector2(1130, 40), new Vector2(0, 0));
            var row = UIFactory.Rect("Buttons", Body);
            UIFactory.Place(row, new Vector2(0, 0), new Vector2(370, 0), new Vector2(1130, 80), new Vector2(0, 0));
            UIFactory.HLayout(row, 14);
            UIFactory.Size(UIFactory.Button(row, "SAVE", Save, ButtonStyle.Primary, 28), 260, 76);
            UIFactory.Size(UIFactory.Button(row, "RESET TO DEFAULT", ResetSection, ButtonStyle.Secondary, 22), 300, 76);
            UIFactory.Size(UIFactory.Button(row, "RECORD HIS VOICE", () => { Save(); VoiceStudio.OpenGroup("Kanha"); }, ButtonStyle.Secondary, 22), 320, 76);
            Pick(section);
        }

        void Pick(string key)
        {
            section = key;
            foreach (var kv in tabs) kv.Value.color = kv.Key == key ? UIFactory.Theme.cyan : Color.white;
            foreach (var (k, n) in KanhaLines.Sections) if (k == key) note.text = n;
            input.text = string.Join("\n", KanhaLines.Get(key));
            status.text = $"{KanhaLines.Get(key).Count} line(s). Edit, then SAVE.";
        }

        List<string> Current()
        {
            var l = new List<string>();
            foreach (var raw in input.text.Replace("\r", "").Split('\n'))
            {
                var t = raw.Trim().Replace(";", ",");
                if (t.Length > 0) l.Add(t);
            }
            return l;
        }

        void Save()
        {
            var l = Current();
            if (l.Count == 0) { status.text = "<color=#ff5a6a>Write at least one line (or press RESET TO DEFAULT).</color>"; return; }
            KanhaLines.Set(section, l);
            Services.Audio.Play("correct");
            status.text = $"<color=#7cffa0>Saved {l.Count} line(s).</color> Kanha will say them in the final exam.";
        }

        void ResetSection()
        {
            input.text = string.Join("\n", KanhaLines.Default(section));
            Save();
        }
    }
}
