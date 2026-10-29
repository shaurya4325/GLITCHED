using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Fonts and colours of the glitch UI style: clean dark panels with cyan and magenta accents.</summary>
    [CreateAssetMenu(menuName = "Glitched/UI Theme", fileName = "UITheme")]
    public class UIThemeSO : ScriptableObject
    {
        public TMP_FontAsset displayFont;
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset monoFont;
        public TMP_FontAsset dyslexicFont;
        public TMP_FontAsset devanagariFont;

        public Color panel = new Color(0.04f, 0.05f, 0.09f, 0.94f);
        public Color panelLight = new Color(0.09f, 0.11f, 0.18f, 0.96f);
        public Color cyan = new Color(0.25f, 0.92f, 1f);
        public Color magenta = new Color(1f, 0.24f, 0.78f);
        public Color yellow = new Color(1f, 0.86f, 0.25f);
        public Color green = new Color(0.35f, 1f, 0.55f);
        public Color red = new Color(1f, 0.32f, 0.35f);
        public Color text = new Color(0.93f, 0.96f, 1f);
        public Color textDim = new Color(0.6f, 0.68f, 0.8f);
        public Color code = new Color(0.07f, 0.09f, 0.14f, 1f);
    }
}
