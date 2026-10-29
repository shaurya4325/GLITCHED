using UnityEngine;

namespace Glitched
{
    /// <summary>One place that owns Time.timeScale: pause, slow-motion (criticals) and hit-stop.</summary>
    public static class TimeController
    {
        static bool paused;
        static float slowScale = 1f;
        static float slowUntil;

        public static void Reset()
        {
            paused = false; slowScale = 1f; slowUntil = 0;
            Apply();
        }

        public static void SetPaused(bool p) { paused = p; Apply(); }

        /// <summary>Slows time to 'scale' for 'seconds' of real time (CRITICAL hits use 0.15 s).</summary>
        public static void SlowMo(float scale, float seconds)
        {
            slowScale = Mathf.Clamp(scale, 0.02f, 1f);
            slowUntil = Time.unscaledTime + seconds;
            Apply();
        }

        /// <summary>Called every frame by the UIRoot.</summary>
        public static void Tick()
        {
            if (slowScale < 1f && Time.unscaledTime >= slowUntil)
            {
                slowScale = 1f;
                Apply();
            }
        }

        static void Apply()
        {
            Time.timeScale = paused ? 0f : slowScale;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(0.05f, Time.timeScale);
        }
    }
}
