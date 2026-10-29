using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Three save slots stored as JSON in Application.persistentDataPath.
    /// Expo mode plays in a temporary in-memory slot that is never written to disk.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public const int SlotCount = 3;
        public const int ExpoSlot = 99;

        public SaveData Current { get; private set; }
        public int CurrentSlot => Current?.slot ?? -1;
        float sessionStart;

        static string PathFor(int slot) => System.IO.Path.Combine(Application.persistentDataPath, $"glitched_slot{slot}.json");

        static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        /// <summary>Only slots 0-2 are written to disk (Expo and editor test sessions stay in memory).</summary>
        public static bool Persistent(int slot) => slot >= 0 && slot < SlotCount;

        public bool Exists(int slot) => Persistent(slot) && File.Exists(PathFor(slot));

        public SaveData Peek(int slot)
        {
            try { return Exists(slot) ? JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(PathFor(slot)), Json) : null; }
            catch (Exception e) { Debug.LogWarning($"[Save] Slot {slot} is unreadable: {e.Message}"); return null; }
        }

        public SaveData NewGame(int slot, string playerName = "Shaurya")
        {
            QuestionSelector.Reseed();
            Current = new SaveData { slot = slot, created = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), playerName = playerName };
            Current.Arc("CodeCity").unlocked = true;
            sessionStart = Time.realtimeSinceStartup;
            if (Persistent(slot)) Write();
            return Current;
        }

        public bool Load(int slot)
        {
            var d = Peek(slot);
            if (d == null) return false;
            Current = d;
            Current.slot = slot;
            if (Current.playerName == "Aarav") Current.playerName = "Shaurya";   // the hero was renamed
            sessionStart = Time.realtimeSinceStartup;
            return true;
        }

        /// <summary>Writes the current slot to disk (ignored in Expo mode).</summary>
        public void Write()
        {
            if (Current == null || !Persistent(Current.slot)) return;
            Current.playSeconds += Time.realtimeSinceStartup - sessionStart;
            sessionStart = Time.realtimeSinceStartup;
            Current.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            try
            {
                string path = PathFor(Current.slot);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(Current, Json));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e) { Debug.LogError($"[Save] Could not write save: {e.Message}"); }
        }

        public void Delete(int slot)
        {
            if (Exists(slot)) File.Delete(PathFor(slot));
        }

        /// <summary>Drops the in-memory save (used when Expo mode resets).</summary>
        public void Discard() => Current = null;

        void OnApplicationPause(bool paused) { if (paused) Write(); }
        void OnApplicationQuit() => Write();
    }
}
