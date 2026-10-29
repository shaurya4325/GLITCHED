using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Glitched
{
    /// <summary>Loads/saves SettingsData and notifies everyone when it changes.</summary>
    public class SettingsStore : MonoBehaviour
    {
        public SettingsData Data { get; private set; } = new SettingsData();
        static string FilePath => Path.Combine(Application.persistentDataPath, "glitched_settings.json");

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(FilePath))
                    Data = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] Could not read settings, using defaults: " + e.Message);
                Data = new SettingsData();
            }
        }

        public void SaveSettings()
        {
            try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(Data, Formatting.Indented)); }
            catch (Exception e) { Debug.LogError("[Settings] Could not save settings: " + e.Message); }
        }

        void OnApplicationQuit() => SaveSettings();   // whatever was changed last is kept for next time

        /// <summary>Call after changing any value in Data.</summary>
        public void Apply()
        {
            SaveSettings();
            GameEvents.RaiseSettings();
        }

        /// <summary>Text scale multiplier for the chosen text size.</summary>
        public float TextScale => Data.textSize == TextSize.Small ? 0.88f : Data.textSize == TextSize.Large ? 1.2f : 1f;
    }
}
