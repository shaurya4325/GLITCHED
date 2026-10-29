using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// What the verified licence allows. OWNER and FULL: everything in story order. JUDGE: every chapter open from
    /// the start. DEMO: chapter 1 (Code City) only. Editor and development builds are not checked (always FULL).
    /// </summary>
    public static class LicenceAccess
    {
        public static string Type { get; private set; } = "FULL";
        public static string Name { get; private set; } = "";

        /// <summary>Judges can jump to any chapter.</summary>
        public static bool UnlockAll => Type == "JUDGE";
        public static bool IsDemo => Type == "DEMO";

        public static void Set(string type, string name)
        {
            bool changed = Type != type;
            Type = string.IsNullOrEmpty(type) ? "FULL" : type;
            Name = name ?? "";
            if (changed) Debug.Log($"[Licence] access: {Type}");
        }

        public static string Label(string type)
        {
            switch (type)
            {
                case "OWNER": return "OWNER";
                case "JUDGE": return "JUDGE · ALL CHAPTERS OPEN";
                case "DEMO": return "DEMO · CHAPTER 1";
                case "BLOCKED": return "BLOCKED";
                default: return "FULL GAME";
            }
        }

        /// <summary>The demo stops at the end of Code City.</summary>
        public static bool CanEnter(string scene)
        {
            if (!IsDemo || string.IsNullOrEmpty(scene)) return true;
            return scene == "Boot" || scene == "Title" || scene == "Benchmark" || scene == "Opening" || scene == "WorldMap" || scene.StartsWith("CodeCity");
        }

        public static void ShowDemoWall()
        {
            Services.Audio.Play("door_locked");
            Services.UI.Confirm("END OF THE DEMO",
                "This copy is the demo: chapter 1 only.\nAsk the GLITCHED team at the stall to unlock the full game.",
                null, null, "OK", "CLOSE");
        }
    }
}
