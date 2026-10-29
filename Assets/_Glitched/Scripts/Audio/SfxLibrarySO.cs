using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    public enum SoundBus { Music, SFX, UI, Voice }

    /// <summary>Named sound effects and music. Add or replace clips here (no code changes).</summary>
    [CreateAssetMenu(menuName = "Glitched/Sfx Library", fileName = "SfxLibrary")]
    public class SfxLibrarySO : ScriptableObject
    {
        public List<SfxEntry> sounds = new List<SfxEntry>();
        public List<MusicEntry> music = new List<MusicEntry>();

        Dictionary<string, SfxEntry> sfxMap;
        Dictionary<string, AudioClip> musicMap;

        public SfxEntry Get(string key)
        {
            if (sfxMap == null)
            {
                sfxMap = new Dictionary<string, SfxEntry>();
                foreach (var s in sounds) if (s != null && !string.IsNullOrEmpty(s.key)) sfxMap[s.key] = s;
            }
            return sfxMap.TryGetValue(key, out var e) ? e : null;
        }

        public AudioClip GetMusic(string key)
        {
            if (musicMap == null)
            {
                musicMap = new Dictionary<string, AudioClip>();
                foreach (var m in music) if (m != null && m.clip != null) musicMap[m.key] = m.clip;
            }
            return musicMap.TryGetValue(key, out var c) ? c : null;
        }
    }

    [Serializable]
    public class SfxEntry
    {
        public string key;
        public SoundBus bus = SoundBus.SFX;
        public List<AudioClip> clips = new List<AudioClip>();
        [Range(0, 1)] public float volume = 0.8f;
        public float pitchMin = 0.95f, pitchMax = 1.05f;
    }

    [Serializable]
    public class MusicEntry
    {
        public string key;
        public AudioClip clip;
    }
}
