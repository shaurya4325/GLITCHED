using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>3. Numerical: calculate and type a value (with tolerance and units). Includes an on-screen keypad.</summary>
    public class NumericalChallenge : ChallengeBase
    {
        TMP_InputField input;
        TextMeshProUGUI unitText;

        protected override void Build()
        {
            UIFactory.HLayout(Area, 30, 0, TextAnchor.UpperLeft, false);
            var left = UIFactory.Rect("Left", Area);
            UIFactory.Size(left, 900, -1, 1, 1);
            UIFactory.VLayout(left, 12);
            if (!string.IsNullOrEmpty(D.code)) CodeBlock(left, D.code);
            if (D.givens != null && D.givens.Count > 0)
            {
                var g = UIFactory.Text(left, "<color=#9fb0cc>GIVEN</color>   " + string.Join("     ", D.givens), 26, UIFactory.Theme.text);
                UIFactory.Size(g, -1, 50);
            }
            var row = UIFactory.Rect("InputRow", left);
            UIFactory.Size(row, -1, 96);
            UIFactory.HLayout(row, 16, 0, TextAnchor.MiddleLeft, false);
            input = UIFactory.Input(row, "type your answer", 40);
            UIFactory.Size(input, 520, 90);
            input.contentType = TMP_InputField.ContentType.Custom;
            input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            input.onSubmit.AddListener(_ => { if (CanSubmit) OnSubmit(); });
            unitText = UIFactory.Text(row, D.unit ?? "", 36, UIFactory.Theme.cyan, TextAlignmentOptions.MidlineLeft, true);
            UIFactory.Size(unitText, 220, 90);
            if (!InputHub.IsMobile) { input.Select(); input.ActivateInputField(); }

            // keypad (touch friendly, also clickable with the mouse)
            var pad = UIFactory.Rect("Keypad", Area);
            UIFactory.Size(pad, 560, -1);
            var gl = pad.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(132, 72); gl.spacing = new Vector2(10, 10);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            string[] keys = { "7", "8", "9", "⌫", "4", "5", "6", "-", "1", "2", "3", ".", "0", "C" };
            foreach (var k in keys)
            {
                string key = k;
                UIFactory.Button(pad, k, () => Key(key), ButtonStyle.Secondary, 30);
            }
            Ctx.setHelp("Type the number (keyboard or keypad), then SUBMIT or press Enter.");
        }

        void Key(string k)
        {
            if (Done) return;
            string t = input.text;
            if (k == "⌫") t = t.Length > 0 ? t.Substring(0, t.Length - 1) : t;
            else if (k == "C") t = "";
            else if (k == "-") t = t.StartsWith("-") ? t.Substring(1) : "-" + t;
            else if (k == "." && t.Contains(".")) { }
            else t += k;
            input.text = t;
        }

        public override bool CanSubmit => input != null && TryValue(out _);

        bool TryValue(out double v) => double.TryParse((input.text ?? "").Trim().Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        public override void OnSubmit()
        {
            if (Done || !TryValue(out var v)) return;
            double tol = Mathf.Max(0, (float)D.tolerance) + 1e-6;
            Finish(System.Math.Abs(v - D.value) <= tol);
        }

        public override void Reveal(bool correct)
        {
            input.interactable = false;
            var img = input.GetComponent<Image>();
            MarkGraphic(img, correct);
            if (!correct) unitText.text = $"{D.unit}   <color=#7cffa0>→ {QuestionData.FormatNumber(D.value)} {D.unit}</color>";
        }
    }
}
