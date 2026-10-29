using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Tracks a smoothed frame rate. If the average stays below 26 fps for 8 seconds, offers a lower tier
    /// (never automatically, and never during a boss fight).
    /// </summary>
    public class FpsMonitor : MonoBehaviour
    {
        public static float SmoothedFps { get; private set; } = 60f;
        public static float FrameMs { get; private set; } = 16f;
        public static bool SuppressPrompts;     // set by bosses and cutscenes
        float lowTimer;
        float cooldown = 20f;
        bool asked;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0) return;
            SmoothedFps = Mathf.Lerp(SmoothedFps, 1f / dt, 0.05f);
            FrameMs = Mathf.Lerp(FrameMs, dt * 1000f, 0.05f);
            if (cooldown > 0) { cooldown -= dt; return; }
            if (asked || SuppressPrompts || Services.Game == null || Services.Game.Paused) { lowTimer = 0; return; }
            if (Services.Settings.Data.tier == GraphicsTier.Potato) return;

            if (SmoothedFps < 26f) lowTimer += dt; else lowTimer = Mathf.Max(0, lowTimer - dt * 2);
            if (lowTimer > 8f)
            {
                asked = true;
                lowTimer = 0;
                Services.UI.Confirm("Switch to a lower graphics setting?", "The game is running below 26 fps.",
                    () =>
                    {
                        var s = Services.Settings.Data;
                        s.tier = (GraphicsTier)Mathf.Max(0, (int)s.tier - 1);
                        s.renderScale = -1;
                        Services.Settings.Apply();
                        Services.Graphics.ApplyTier(s.tier);
                        asked = false; cooldown = 30f;
                    },
                    () => { cooldown = 120f; asked = false; });
            }
        }
    }
}
