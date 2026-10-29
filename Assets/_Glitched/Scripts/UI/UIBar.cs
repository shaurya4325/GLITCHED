using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>A rounded progress bar with a trailing "ghost" segment (shows recent damage).</summary>
    public class UIBar : MonoBehaviour
    {
        public Image fill;
        public Image ghost;
        public float Value { get; private set; } = 1f;
        float ghostValue = 1f;
        float shown = 1f;

        public void Set(float v, bool instant = false)
        {
            v = Mathf.Clamp01(v);
            if (v > Value) ghostValue = v;
            Value = v;
            if (instant) { shown = ghostValue = v; Apply(); }
        }

        void Update()
        {
            shown = Mathf.Lerp(shown, Value, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
            ghostValue = Mathf.MoveTowards(ghostValue, Value, Time.unscaledDeltaTime * 0.6f);
            Apply();
        }

        void Apply()
        {
            if (fill != null) fill.rectTransform.anchorMax = new Vector2(shown, 1);
            if (ghost != null) ghost.rectTransform.anchorMax = new Vector2(Mathf.Max(ghostValue, shown), 1);
        }

        public void SetColor(Color c) { if (fill != null) fill.color = c; }
    }
}
