using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    public enum TextSize { Small, Medium, Large }
    public enum BackgroundDetail { Low, Medium, High }

    /// <summary>Player settings (shared by all save slots), stored in settings.json.</summary>
    [Serializable]
    public class SettingsData
    {
        public int version = 1;
        public bool tierChosen;               // false until the DeviceProfiler picked one
        public GraphicsTier tier = GraphicsTier.Medium;
        public float renderScale = -1;        // -1 = use the tier default
        public int fpsCap = -1;               // -1 = tier default, 0 = unlimited
        public bool shadows = true;
        public bool postProcessing = true;
        public float particles = 1f;          // multiplier 0..1
        public BackgroundDetail backgroundDetail = BackgroundDetail.High;
        public float cameraSway = 1f;         // multiplier 0..1 (accessibility: motion sensitivity)
        public bool screenShake = true;
        public bool fullscreen = true;        // false = a window (desktop only)

        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.9f;
        public float uiVolume = 0.8f;
        public float voiceVolume = 1f;

        public TextSize textSize = TextSize.Medium;
        public bool dyslexiaFont;
        public bool colorBlindSafe = true;
        public bool reduceFlashing;
        public bool relaxedMode;
        public string language = "en";

        public bool expoMode;
        public bool showTouchControls = true;
        public float touchButtonScale = 1f;
        public List<TouchLayoutEntry> touchLayout = new List<TouchLayoutEntry>();
    }

    [Serializable]
    public class TouchLayoutEntry
    {
        public string id;
        public float x, y;     // anchored position in reference pixels
        public float scale = 1f;
    }
}
