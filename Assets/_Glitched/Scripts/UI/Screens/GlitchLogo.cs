using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>The GLITCHED logo: three stacked copies (cyan, magenta, white) that split and jitter like a broken signal.</summary>
    public class GlitchLogo : MonoBehaviour
    {
        TextMeshProUGUI cyan, magenta, white;
        float spikeUntil;

        public static GlitchLogo Create(Transform parent, string text, float size)
        {
            var rt = UIFactory.Rect("Logo", parent);
            var logo = rt.gameObject.AddComponent<GlitchLogo>();
            logo.cyan = Layer(rt, text, size, UIFactory.Theme.cyan);
            logo.magenta = Layer(rt, text, size, UIFactory.Theme.magenta);
            logo.white = Layer(rt, text, size, Color.white);
            return logo;
        }

        static TextMeshProUGUI Layer(RectTransform parent, string text, float size, Color c)
        {
            var t = UIFactory.Text(parent, text, size, c, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold | FontStyles.Italic;
            t.characterSpacing = 6;
            return t;
        }

        void Update()
        {
            float time = Time.unscaledTime;
            if (Random.value < Time.unscaledDeltaTime * 0.6f) spikeUntil = time + Random.Range(0.08f, 0.25f);
            bool spike = time < spikeUntil;
            float s = spike ? Random.Range(8f, 22f) : 3f + Mathf.Sin(time * 2f) * 1.5f;
            cyan.rectTransform.anchoredPosition = new Vector2(-s, spike ? Random.Range(-4f, 4f) : 0);
            magenta.rectTransform.anchoredPosition = new Vector2(s, spike ? Random.Range(-4f, 4f) : 0);
            white.rectTransform.anchoredPosition = spike ? new Vector2(Random.Range(-3f, 3f), 0) : Vector2.zero;
            cyan.alpha = magenta.alpha = 0.85f;
            white.text = spike ? GlitchText.Corrupt("GLITCHED", 0.4f) : "GLITCHED";
        }
    }
}
