using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    public enum MusicState { None, Title, Explore, Puzzle, Combat, Boss, Victory, Restoration, Cinematic, Ending }

    /// <summary>
    /// Layered adaptive music (Section 11): Exploration (base + lead stems), Puzzle, Combat, Boss (layers added per
    /// phase), Critical (music drops out for a beat, then BOOM), Victory. All stems of a state play in sync.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        class Layer
        {
            public AudioSource src;
            public BitCrusher crush;
            public float target, current;
        }

        readonly List<Layer> layers = new List<Layer>();
        readonly List<Layer> fading = new List<Layer>();
        AudioManager audioMgr;
        float busVolume = 1f;
        float dropOut = 1f, dropTimer;
        public MusicState State { get; private set; }
        string currentKey;
        float crush;

        public void Init(AudioManager a) { audioMgr = a; }

        public void SetBusVolume(float v) => busVolume = v;
        public void SetCrush(float v) => crush = v;

        AudioClip Clip(string key) => Services.Db.sfx != null ? Services.Db.sfx.GetMusic(key) : null;

        Layer NewLayer(AudioClip clip, double startDsp)
        {
            var go = new GameObject("Layer_" + (clip != null ? clip.name : "none"));
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.playOnAwake = false; s.volume = 0; s.spatialBlend = 0;
            s.ignoreListenerPause = true;
            var c = go.AddComponent<BitCrusher>();
            if (clip != null) s.PlayScheduled(startDsp);
            return new Layer { src = s, crush = c };
        }

        /// <summary>Plays a set of synchronised stems. Stem volumes start at 'mix' (1 = audible, 0 = silent).</summary>
        public void PlayStems(string id, string[] keys, float[] mix, float fade = 1.5f)
        {
            if (currentKey == id) { SetMix(mix); return; }
            currentKey = id;
            foreach (var l in layers) { l.target = 0; fading.Add(l); }
            layers.Clear();
            double start = AudioSettings.dspTime + 0.08;
            for (int i = 0; i < keys.Length; i++)
            {
                var clip = Clip(keys[i]);
                if (clip == null) { Debug.Log($"[Music] Missing music '{keys[i]}'"); continue; }
                var l = NewLayer(clip, start);
                l.target = mix != null && i < mix.Length ? mix[i] : 1f;
                layers.Add(l);
            }
            fadeSpeed = 1f / Mathf.Max(0.05f, fade);
        }

        float fadeSpeed = 1f;

        public void SetMix(float[] mix)
        {
            for (int i = 0; i < layers.Count; i++) layers[i].target = mix != null && i < mix.Length ? mix[i] : 1f;
        }

        // ------------------------------------------------------------------ states
        public void Title() { State = MusicState.Title; PlayStems("title", new[] { "title" }, new[] { 1f }); }

        public void Explore(string regionKey)
        {
            State = MusicState.Explore;
            PlayStems("explore_" + regionKey, new[] { regionKey, regionKey + "_lead" }, new[] { 1f, 0.85f }, 2f);
        }

        /// <summary>Puzzle: keep the region base, but the room goes quiet (lead off, base lowered).</summary>
        public void Puzzle()
        {
            State = MusicState.Puzzle;
            if (currentKey != null && currentKey.StartsWith("explore_")) SetMix(new[] { 0.45f, 0f });
            else PlayStems("puzzle", new[] { "puzzle" }, new[] { 1f });
        }

        public void ResumeExplore()
        {
            if (currentKey != null && currentKey.StartsWith("explore_")) { State = MusicState.Explore; SetMix(new[] { 1f, 0.85f }); }
        }

        public void Combat() { State = MusicState.Combat; PlayStems("combat", new[] { "combat" }, new[] { 1f }, 0.8f); }

        /// <summary>Boss music: layer 1 from phase 1, the drive layer from phase 3, the lead from phase 5.</summary>
        public void Boss(int phase)
        {
            State = MusicState.Boss;
            float drive = phase >= 3 ? 1f : 0f;
            float lead = phase >= 5 ? 1f : 0f;
            PlayStems("boss", new[] { "boss_1_base", "boss_2_drive", "boss_3_lead" }, new[] { 1f, drive, lead }, 1f);
        }

        public void Victory() { State = MusicState.Victory; PlayStems("victory", new[] { "victory" }, new[] { 1f }, 0.3f); }
        public void Restoration() { State = MusicState.Restoration; PlayStems("restoration", new[] { "restoration" }, new[] { 1f }, 2f); }
        public void Cinematic(string key) { State = MusicState.Cinematic; PlayStems(key, new[] { key }, new[] { 1f }, 1f); }
        public void Stop(float fade = 1f) { currentKey = null; State = MusicState.None; foreach (var l in layers) { l.target = 0; fading.Add(l); } layers.Clear(); fadeSpeed = 1f / Mathf.Max(0.05f, fade); }

        /// <summary>CRITICAL / ULTIMATE: the music drops out for a beat, then comes back with a BOOM.</summary>
        public void DropOut(float seconds)
        {
            dropTimer = seconds;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dropTimer > 0)
            {
                dropTimer -= dt;
                dropOut = Mathf.MoveTowards(dropOut, 0f, dt * 12f);
                if (dropTimer <= 0) audioMgr.Play("boom");
            }
            else dropOut = Mathf.MoveTowards(dropOut, 1f, dt * 6f);

            foreach (var l in layers)
            {
                l.current = Mathf.MoveTowards(l.current, l.target, dt * fadeSpeed);
                l.src.volume = l.current * busVolume * dropOut;
                l.crush.amount = crush;
            }
            for (int i = fading.Count - 1; i >= 0; i--)
            {
                var l = fading[i];
                l.current = Mathf.MoveTowards(l.current, 0, dt * fadeSpeed);
                l.src.volume = l.current * busVolume;
                if (l.current <= 0.001f) { Destroy(l.src.gameObject); fading.RemoveAt(i); }
            }
        }
    }
}
