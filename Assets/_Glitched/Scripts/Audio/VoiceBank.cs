using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Glitched
{
    /// <summary>
    /// Recorded voice lines. Every spoken line (dialogue box, Pixel's bubbles, enemy taunts, NPCs, the labs) has a
    /// stable id (speaker + a hash of its text); a recording named &lt;id&gt;.wav or &lt;id&gt;.ogg is played instead of the
    /// gibberish blips. Recordings made in the Voice Studio go to the player's data folder (UserFolder), which a new
    /// build never touches; recordings shipped with the game live in StreamingAssets/Voice next to script.csv.
    /// </summary>
    public static class VoiceBank
    {
        /// <summary>Shipped recordings and the script (read-only in an installed game).</summary>
        public static string Folder => Path.Combine(Application.streamingAssetsPath, "Voice");
        /// <summary>Recordings made on this PC (Voice Studio). Survives rebuilds and reinstalls.</summary>
        public static string UserFolder => Path.Combine(Application.persistentDataPath, "Voice");

        static HashSet<string> available;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        /// <summary>Stable id for a line: speaker_hash, where the hash ignores case, punctuation and spacing.</summary>
        public static string Id(string speaker, string text)
        {
            var sb = new StringBuilder();
            foreach (char ch in (text ?? "").ToLowerInvariant()) if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            uint h = 2166136261;
            foreach (char ch in sb.ToString()) { h ^= ch; h *= 16777619; }
            return $"{(speaker ?? "x").ToLowerInvariant()}_{h:x8}";
        }

        /// <summary>The voice speaker key of an enemy or boss (the same one the fights use).</summary>
        public static string SpeakerOf(EnemyDataSO e) => e == null ? "enemy" : e is BossDataSO b && b.profile != null ? b.profile.id : e.displayName;

        public static void Refresh()
        {
            available = new HashSet<string>();
            cache.Clear();
            foreach (var dir in new[] { Folder, UserFolder })
            {
                try
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext == ".wav" || ext == ".ogg") available.Add(Path.GetFileNameWithoutExtension(f));
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("[Voice] " + e.Message); }
            }
        }

        public static bool Has(string id)
        {
            if (available == null) Refresh();
            return available.Contains(id);
        }

        /// <summary>The recording for a line: this PC's own take first, then the one shipped with the game.</summary>
        public static string PathFor(string id)
        {
            foreach (var dir in new[] { UserFolder, Folder })
                foreach (var ext in new[] { ".wav", ".ogg" })
                {
                    var p = Path.Combine(dir, id + ext);
                    if (File.Exists(p)) return p;
                }
            return null;
        }

        public static IEnumerator Load(string id, System.Action<AudioClip> done)
        {
            if (cache.TryGetValue(id, out var c) && c != null) { done(c); yield break; }
            var path = PathFor(id);
            if (path == null) { done(null); yield break; }
            var type = path.EndsWith(".ogg") ? AudioType.OGGVORBIS : AudioType.WAV;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri, type))   // escapes the spaces in the data folder path
            {
                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = false;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning("[Voice] " + req.error); done(null); yield break; }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                clip.name = id;
                Loudness(clip);
                cache[id] = clip;
                done(clip);
            }
        }

        /// <summary>
        /// Brings every take to the same, clearly audible speech loudness (RMS of the voiced part), with a soft limiter
        /// so loud words never clip. Quiet mics and far-away takes end up as loud as the rest of the game.
        /// </summary>
        static void Loudness(AudioClip clip)
        {
            try
            {
                var d = new float[clip.samples * clip.channels];
                if (d.Length == 0 || !clip.GetData(d, 0)) return;
                double sum = 0; int n = 0;
                foreach (var s in d) { float a = Mathf.Abs(s); if (a > 0.02f) { sum += a * a; n++; } }
                if (n < 100) return;
                float rms = Mathf.Sqrt((float)(sum / n));
                float gain = Mathf.Clamp(0.2f / Mathf.Max(rms, 1e-4f), 0.6f, 8f);
                for (int i = 0; i < d.Length; i++)
                {
                    float v = d[i] * gain, a = Mathf.Abs(v);
                    if (a > 0.7f) v = Mathf.Sign(v) * (0.7f + 0.29f * (float)System.Math.Tanh((a - 0.7f) / 0.29f));
                    d[i] = v;
                }
                clip.SetData(d, 0);
            }
            catch (System.Exception e) { Debug.LogWarning("[Voice] loudness: " + e.Message); }
        }

        public static void Forget(string id) { cache.Remove(id); available?.Remove(id); }

        /// <summary>Deletes this PC's take of a line (a shipped recording, if any, plays again).</summary>
        public static void Delete(string id)
        {
            foreach (var ext in new[] { ".wav", ".ogg" })
            {
                var p = Path.Combine(UserFolder, id + ext);
                if (File.Exists(p)) File.Delete(p);
            }
            Forget(id);
            Refresh();
        }

        /// <summary>Saves samples as a 16-bit PCM WAV file in the user folder.</summary>
        public static void SaveWav(string id, float[] samples, int channels, int rate)
        {
            Directory.CreateDirectory(UserFolder);
            var path = Path.Combine(UserFolder, id + ".wav");
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = samples.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels);
                w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
            Forget(id);
            if (available == null) Refresh(); else available.Add(id);
        }

        static HashSet<string> scripted;

        /// <summary>Adds a line met during play to the script (met.csv in the user folder) if it is not there yet.</summary>
        public static void Note(string speaker, string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                if (scripted == null) { scripted = new HashSet<string>(); foreach (var l in Script()) scripted.Add(l.id); }
                var id = Id(speaker, text);
                if (!scripted.Add(id)) return;
                Directory.CreateDirectory(UserFolder);
                File.AppendAllText(Path.Combine(UserFolder, "met.csv"), $"{id};{speaker};Heard in game;{Clean(text)}\n");
            }
            catch { }
        }

        static string Clean(string t) => t.Replace("\n", " ").Replace(";", ",");

        /// <summary>
        /// The lines to record: StreamingAssets/Voice/script.csv (id;speaker;group;text), written by the editor builder,
        /// plus lines heard during play that the builder could not find (met.csv).
        /// </summary>
        public static List<(string id, string speaker, string group, string text)> Script()
        {
            var list = new List<(string, string, string, string)>();
            var seen = new HashSet<string>();
            foreach (var path in new[] { Path.Combine(Folder, "script.csv"), Path.Combine(UserFolder, "met.csv") })
            {
                if (!File.Exists(path)) continue;
                foreach (var line in File.ReadAllLines(path))
                {
                    var parts = line.Split(new[] { ';' }, 4);
                    if (parts.Length < 3 || parts[0] == "id" || !seen.Add(parts[0])) continue;
                    if (parts.Length == 4) list.Add((parts[0], parts[1], parts[2], parts[3]));
                    else list.Add((parts[0], parts[1], "Heard in game", parts[2]));   // older 3-column files
                }
            }
            // Kanha's lines can be rewritten in the game (KanhaLines), so they are listed live, not from the csv
            list.RemoveAll(l => l.Item2 == KanhaLines.Speaker);
            var kanha = new List<(string, string, string, string)>();
            foreach (var t in KanhaLines.All())
            {
                var id = Id(KanhaLines.Speaker, t);
                if (!kanha.Exists(k => k.Item1 == id)) kanha.Add((id, KanhaLines.Speaker, "Kanha", t));
            }
            list.InsertRange(0, kanha);
            return list;
        }
    }
}
