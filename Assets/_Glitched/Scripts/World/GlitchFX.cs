using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Short screen glitches (impacts, wrong answers, transitions). Drives the _ScreenGlitch global used by
    /// the full-screen post pass (Medium+) and the UI overlay (all tiers). "Reduce flashing" scales it down.
    /// </summary>
    public static class GlitchFX
    {
        static float level;
        static TweenHandle handle;
        static readonly int Id = Shader.PropertyToID("_ScreenGlitch");

        public static float Level => level;

        public static void Pulse(float strength, float duration)
        {
            if (Services.Settings != null && Services.Settings.Data.reduceFlashing) strength *= 0.3f;
            strength = Mathf.Clamp01(strength);
            if (strength < level) return;
            handle?.Kill();
            Set(strength);
            handle = Tween.Value(strength, 0f, Mathf.Max(0.05f, duration), Set, Ease.InQuad, true);
        }

        public static void Set(float v)
        {
            level = v;
            Shader.SetGlobalFloat(Id, v);
            var f = Services.Db != null ? Services.Db.fullscreenGlitchFeature : null;
            if (f != null && Services.Graphics != null && Services.Graphics.Current != null)
                f.SetActive(Services.Graphics.Current.fullscreenGlitch && Services.Graphics.PostEnabled && v > 0.001f);
        }
    }
}
