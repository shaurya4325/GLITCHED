using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Expo mode (Section 12): a hidden setting for the exhibition stall. Starts a 10-minute guided demo route
    /// (opening -> Code City tutorial -> a fight -> the Lord Infinite Loop boss -> restoration), hides quit
    /// buttons, and resets all progress when the demo ends. Toggle it on the title screen: click the logo 5 times
    /// (or press Ctrl+Shift+E).
    /// </summary>
    public static class ExpoMode
    {
        public const float DemoSeconds = 600f;
        public static float StartTime { get; private set; } = -1;
        public static bool Running => Services.Game != null && Services.Game.IsExpo && StartTime >= 0;
        public static float Remaining => Running ? Mathf.Max(0, DemoSeconds - (Time.realtimeSinceStartup - StartTime)) : 0;

        public static void Toggle()
        {
            var s = Services.Settings.Data;
            s.expoMode = !s.expoMode;
            Services.Settings.Apply();
            Services.UI.Toast(s.expoMode ? "EXPO MODE ON: 10-minute demo, progress resets after each run" : "Expo mode off");
        }

        public static void StartDemo()
        {
            Services.Save.NewGame(SaveSystem.ExpoSlot, "Shaurya");
            Services.Game.Data.SetFlag("expo_route");
            StartTime = Time.realtimeSinceStartup;
            Services.Scenes.Load("Opening");
        }

        public static void EndDemo()
        {
            StartTime = -1;
            Services.Save.Discard();
            Services.Scenes.Load("Title");
        }

        /// <summary>Called every frame by the title/HUD timer: ends the demo when time runs out.</summary>
        public static void Tick()
        {
            if (!Running) return;
            if (Remaining <= 0)
            {
                Services.UI.Toast("Thanks for playing GLITCHED! The demo will restart.");
                EndDemo();
            }
        }
    }
}
