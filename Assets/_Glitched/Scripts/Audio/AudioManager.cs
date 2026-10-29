using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Sound buses (Music, SFX, UI, Voice) with volume settings, pooled one-shots, character voice blips,
    /// the bit-crush glitch burst and the adaptive music director. In a game about feedback, sound is feedback.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int PoolSize = 14;
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource uiSource, voiceSource, lineSource;   // voiceSource: blips; lineSource: recorded lines
        int next;
        public MusicDirector Music { get; private set; }
        readonly HashSet<string> missingLogged = new HashSet<string>();
        float duck = 1f, duckTarget = 1f;
        float crushTimer;
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        SfxLibrarySO Lib => Services.Db.sfx;

        public void Init()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("SFX_" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0f;
                pool.Add(s);
            }
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            lineSource = gameObject.AddComponent<AudioSource>();
            lineSource.playOnAwake = false;
            var mGo = new GameObject("Music");
            mGo.transform.SetParent(transform, false);
            Music = mGo.AddComponent<MusicDirector>();
            Music.Init(this);
            ApplyVolumes();
            GameEvents.SettingsChanged += ApplyVolumes;
        }

        public float Bus(SoundBus b)
        {
            var s = Services.Settings.Data;
            float m = s.masterVolume;
            switch (b)
            {
                case SoundBus.Music: return m * s.musicVolume * duck;
                case SoundBus.UI: return m * s.uiVolume;
                case SoundBus.Voice: return m * s.voiceVolume;
                default: return m * s.sfxVolume;
            }
        }

        public void ApplyVolumes()
        {
            if (Music != null) Music.SetBusVolume(Bus(SoundBus.Music));
        }

        /// <summary>Plays a recorded voice line (replaces the blips for that line).</summary>
        public void PlayVoice(AudioClip clip)
        {
            if (clip == null) return;
            lineSource.Stop();
            lineSource.clip = clip;
            lineSource.volume = Bus(SoundBus.Voice);
            lineSource.Play();
        }

        public void StopVoice() { if (lineSource != null) lineSource.Stop(); }

        /// <summary>True while a recorded line is playing.</summary>
        public bool VoicePlaying => lineSource != null && lineSource.isPlaying;

        /// <summary>
        /// Speaks a line in its recorded voice if there is one (every speech path calls this: dialogue box, bubbles,
        /// enemies, labs). Returns false when there is no recording, so the caller can fall back to blips.
        /// onClip gets the clip once loaded (for pacing text to it).
        /// </summary>
        public bool SpeakLine(string speaker, string text, System.Action<AudioClip> onClip = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            VoiceBank.Note(speaker, text);
            var id = VoiceBank.Id(speaker, text);
            if (!VoiceBank.Has(id)) return false;
            StartCoroutine(VoiceBank.Load(id, clip =>
            {
                if (clip == null) return;
                if (Debug.isDebugBuild) Debug.Log($"[Voice] playing {id} ({clip.length:0.0}s)");
                PlayVoice(clip);
                onClip?.Invoke(clip);
            }));
            return true;
        }

        public void Play(string key, float volume = 1f, float pitch = 1f)
        {
            if (string.IsNullOrEmpty(key)) return;
            var lib = Lib;
            var e = lib != null ? lib.Get(key) : null;
            if (e == null || e.clips == null || e.clips.Count == 0)
            {
                if (missingLogged.Add(key)) Debug.Log($"[Audio] Missing sound '{key}' (silent placeholder). Add it to the SfxLibrary asset.");
                return;
            }
            // avoid machine-gun stacking of the same sound in one frame
            if (lastPlayed.TryGetValue(key, out var t) && Time.unscaledTime - t < 0.03f) return;
            lastPlayed[key] = Time.unscaledTime;
            var clip = e.clips[Random.Range(0, e.clips.Count)];
            if (clip == null) return;
            float vol = e.volume * volume * Bus(e.bus);
            float p = pitch * Random.Range(e.pitchMin, e.pitchMax);
            if (e.bus == SoundBus.UI) { uiSource.pitch = p; uiSource.PlayOneShot(clip, vol); return; }
            var src = pool[next];
            next = (next + 1) % pool.Count;
            src.pitch = p;
            src.volume = vol;
            src.clip = clip;
            src.Play();
        }

        /// <summary>Gibberish talking blips (no voice acting).</summary>
        public void PlayBlip(string key, float pitch)
        {
            var e = Lib != null ? Lib.Get(key) ?? Lib.Get("blip") : null;
            if (e == null || e.clips.Count == 0) return;
            voiceSource.pitch = pitch * Random.Range(0.9f, 1.12f);
            if (VoicePlaying) return;   // never blip over a recorded line
            voiceSource.PlayOneShot(e.clips[Random.Range(0, e.clips.Count)], e.volume * Bus(SoundBus.Voice) * 1.35f);
        }

        /// <summary>Short bit-crush burst on the music (corruption spikes).</summary>
        public void BitCrushBurst(float seconds)
        {
            if (Services.Settings.Data.reduceFlashing) seconds *= 0.5f;
            crushTimer = Mathf.Max(crushTimer, seconds);
        }

        bool voiceDucked, musicDuckRequested;
        public void DuckMusic(bool on) { musicDuckRequested = on; duckTarget = on ? 0.35f : voiceDucked ? 0.4f : 1f; }

        void Update()
        {
            if (crushTimer > 0) crushTimer -= Time.unscaledDeltaTime;
            // music steps back while someone speaks in a recorded voice
            bool talking = VoicePlaying;
            if (talking != voiceDucked) { voiceDucked = talking; duckTarget = talking ? 0.4f : (musicDuckRequested ? 0.35f : 1f); }
            Music.SetCrush(crushTimer > 0 ? 0.75f : 0f);
            if (!Mathf.Approximately(duck, duckTarget))
            {
                duck = Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime * 2f);
                ApplyVolumes();
            }
        }
    }
}
