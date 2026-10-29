using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Full-screen fades with scanline tearing on transitions.</summary>
    public class FadeController : MonoBehaviour
    {
        Image black;
        CanvasGroup group;
        public bool IsBlack => group != null && group.alpha > 0.99f;

        public void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            black = UIFactory.Image(transform, new Color(0.01f, 0.01f, 0.025f, 1f), null, "Black");
            UIFactory.Stretch(black.rectTransform);
            group.alpha = 0;
        }

        public IEnumerator FadeOut(float seconds, bool glitchy = true)
        {
            group.blocksRaycasts = true;
            if (glitchy) GlitchFX.Pulse(0.3f, seconds + 0.2f);   // a light flicker, not a wall of noise
            float a = group.alpha;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                // stepped fade looks like a signal breaking up
                group.alpha = glitchy ? Mathf.Max(a, Mathf.Round(Mathf.Lerp(a, 1, k) * 6f) / 6f) : Mathf.Lerp(a, 1, k);
                yield return null;
            }
            group.alpha = 1;
        }

        public IEnumerator FadeIn(float seconds, bool glitchy = true)
        {
            if (glitchy) GlitchFX.Pulse(0.22f, seconds);
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                group.alpha = glitchy ? Mathf.Round((1 - k) * 6f) / 6f : 1 - k;
                yield return null;
            }
            group.alpha = 0;
            group.blocksRaycasts = false;
        }

        public void SetBlack(bool on) { group.alpha = on ? 1 : 0; group.blocksRaycasts = on; }
    }
}
