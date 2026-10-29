using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Voice Studio: record your own voice for every spoken line in the game (Shaurya, Pixel, NPCs, enemies, bosses,
    /// terminals), inside the game. Shows each line of the script, records it from the microphone (R or the button),
    /// trims the silence, saves it to the player's data folder and moves on to the next line still to record.
    /// Recorded lines replace the gibberish blips everywhere: cutscenes, speech bubbles, fights and labs.
    /// Open from Settings or press F8 anywhere.
    /// </summary>
    public class VoiceStudio : SubScreen
    {
        public static void Open() => Create<VoiceStudio>("VOICE STUDIO", new Vector2(1600, 940));
        public static void OpenGroup(string group) { filter = group; Open(); }

        static readonly string[] Groups = { "Shaurya", "Kanha", "Pixel", "NPCs", "Enemies", "Bosses", "Terminals", "Heard in game", "*" };
        static string filter = "Shaurya";   // remembered while the game runs
        List<(string id, string speaker, string group, string text)> all, lines;
        int index;
        TextMeshProUGUI who, line, status, counter, mic;
        readonly Dictionary<string, TextMeshProUGUI> groupLabels = new Dictionary<string, TextMeshProUGUI>();
        Button recordBtn;
        Image meter;
        bool recording;
        float advanceAt = -1f;
        string device;
        AudioClip recClip;
        AudioSource preview;

        protected override void Build()
        {
            var th = UIFactory.Theme;
            VoiceBank.Refresh();
            all = VoiceBank.Script();
            preview = gameObject.AddComponent<AudioSource>();
            preview.ignoreListenerPause = true;
            Services.Audio.Music.Stop(0.5f);   // quiet while recording
            device = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;

            var top = UIFactory.Rect("Filters", Body);
            UIFactory.Place(top, new Vector2(0, 1), Vector2.zero, new Vector2(1520, 64), new Vector2(0, 1));
            UIFactory.HLayout(top, 10);
            foreach (var g in Groups)
            {
                if (g != "*" && !all.Exists(l => l.group == g)) continue;
                string key = g;
                var b = UIFactory.Button(top, g == "*" ? "ALL" : g.ToUpperInvariant(), () => { filter = key; ApplyFilter(); }, ButtonStyle.Secondary, 20);
                UIFactory.Size(b, g == "Heard in game" ? 210 : 150, 58);
                groupLabels[g] = UIFactory.Label(b);
            }
            mic = UIFactory.Text(Body, "", 20, th.textDim, TextAlignmentOptions.Right);
            UIFactory.Place(mic.rectTransform, new Vector2(1, 1), new Vector2(0, -74), new Vector2(700, 34), new Vector2(1, 1));
            counter = UIFactory.Text(Body, "", 24, th.textDim, TextAlignmentOptions.Left);
            UIFactory.Place(counter.rectTransform, new Vector2(0, 1), new Vector2(0, -78), new Vector2(900, 34), new Vector2(0, 1));
            who = UIFactory.Text(Body, "", 40, th.cyan, TextAlignmentOptions.Left, true);
            UIFactory.Place(who.rectTransform, new Vector2(0, 1), new Vector2(0, -130), new Vector2(1520, 60), new Vector2(0, 1));
            line = UIFactory.Text(Body, "", 44, th.text, TextAlignmentOptions.TopLeft);
            UIFactory.Place(line.rectTransform, new Vector2(0, 1), new Vector2(0, -200), new Vector2(1520, 300), new Vector2(0, 1));
            line.enableWordWrapping = true;

            // input level meter (shows the mic is hearing you)
            var meterBg = UIFactory.Image(Body, new Color(1, 1, 1, 0.08f), UISprites.Rounded(8), "MeterBg");
            UIFactory.Place(meterBg.rectTransform, new Vector2(0, 0), new Vector2(0, 190), new Vector2(1520, 16), new Vector2(0, 0));
            meter = UIFactory.Image(meterBg.transform, th.cyan, UISprites.Rounded(8), "Meter");
            UIFactory.Place(meter.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 16), new Vector2(0, 0.5f));

            status = UIFactory.Text(Body, "", 26, th.yellow, TextAlignmentOptions.Left);
            UIFactory.Place(status.rectTransform, new Vector2(0, 0), new Vector2(0, 120), new Vector2(1520, 50), new Vector2(0, 0));
            var row = UIFactory.Rect("Controls", Body);
            UIFactory.Place(row, new Vector2(0, 0), Vector2.zero, new Vector2(1520, 90), new Vector2(0, 0));
            UIFactory.HLayout(row, 14);
            UIFactory.Size(UIFactory.Button(row, "◀ PREV", () => Move(-1), ButtonStyle.Secondary, 24), 180, 80);
            recordBtn = UIFactory.Button(row, "● RECORD (R)", ToggleRecord, ButtonStyle.Primary, 28);
            UIFactory.Size(recordBtn, 320, 80);
            UIFactory.Size(UIFactory.Button(row, "▶ PLAY (P)", Play, ButtonStyle.Secondary, 24), 220, 80);
            UIFactory.Size(UIFactory.Button(row, "NEXT ▶", () => Move(1), ButtonStyle.Secondary, 24), 180, 80);
            UIFactory.Size(UIFactory.Button(row, "TO DO (N)", NextTodo, ButtonStyle.Secondary, 24), 200, 80);
            UIFactory.Size(UIFactory.Button(row, "DELETE", Delete, ButtonStyle.Danger, 22), 170, 80);
            mic.text = device != null ? "Mic: " + device : "<color=#ff5a6a>No microphone found: plug one in and reopen (F8)</color>";
            ApplyFilter();
        }

        void ApplyFilter()
        {
            lines = filter == "*" ? all : all.FindAll(l => l.group == filter);
            index = 0;
            for (int i = 0; i < lines.Count; i++) if (!VoiceBank.Has(lines[i].id)) { index = i; break; }   // first line still to record
            Refresh();
        }

        void Refresh()
        {
            foreach (var kv in groupLabels)
            {
                var inG = kv.Key == "*" ? all : all.FindAll(l => l.group == kv.Key);
                int d = 0; foreach (var x in inG) if (VoiceBank.Has(x.id)) d++;
                string name = kv.Key == "*" ? "ALL" : kv.Key.ToUpperInvariant();
                kv.Value.text = $"{(kv.Key == filter ? "<u>" : "")}{name}{(kv.Key == filter ? "</u>" : "")}  <size=75%>{d}/{inG.Count}</size>";
            }
            if (lines == null || lines.Count == 0)
            {
                line.text = all.Count == 0 ? "No script found. Rebuild the game (Build Everything writes Voice/script.csv)." : "Nothing in this group yet.";
                who.text = ""; counter.text = ""; status.text = "";
                return;
            }
            var l = lines[index];
            int done = 0; foreach (var x in lines) if (VoiceBank.Has(x.id)) done++;
            counter.text = $"Line {index + 1} / {lines.Count}     ·     recorded {done}" + (done == lines.Count ? "   <color=#7cffa0>ALL DONE</color>" : "");
            who.text = Speaker(l.speaker) + (filter == "*" || l.group != filter ? $"  <size=60%><color=#8a93a8>{l.group}</color></size>" : "");
            line.text = "“" + l.text + "”";
            status.text = recording ? "<color=#ff5a6a>● RECORDING... read the line, then press R again to stop</color>"
                        : VoiceBank.Has(l.id) ? "<color=#7cffa0>✓ recorded</color>   R = record again   P = listen   N = next line to record"
                        : "not recorded yet: press R and read the line";
            UIFactory.Label(recordBtn).text = recording ? "■ STOP (R)" : "● RECORD (R)";
        }

        static string Speaker(string sp)
        {
            if (sp == "aarav") return "SHAURYA";
            var prof = Services.Db != null ? Services.Db.Character(sp) : null;
            if (prof != null && !string.IsNullOrEmpty(prof.displayName)) return prof.displayName.ToUpperInvariant();
            return sp.Replace('_', ' ').ToUpperInvariant();
        }

        void Move(int d)
        {
            if (recording || lines.Count == 0) return;
            advanceAt = -1f;
            index = (index + d + lines.Count) % lines.Count;
            Refresh();
        }

        void NextTodo()
        {
            if (recording || lines.Count == 0) return;
            advanceAt = -1f;
            for (int k = 1; k <= lines.Count; k++)
            {
                int i = (index + k) % lines.Count;
                if (!VoiceBank.Has(lines[i].id)) { index = i; break; }
            }
            Refresh();
        }

        void ToggleRecord()
        {
            if (device == null || lines.Count == 0) return;
            advanceAt = -1f;
            if (!recording)
            {
                preview.Stop();
                recClip = Microphone.Start(device, false, 30, 44100);
                recording = true;
            }
            else
            {
                int pos = Microphone.GetPosition(device);
                Microphone.End(device);
                recording = false;
                if (recClip != null && pos > 0)
                {
                    var data = new float[pos * recClip.channels];
                    recClip.GetData(data, 0);
                    data = Trim(data);
                    VoiceBank.SaveWav(lines[index].id, data, recClip.channels, recClip.frequency);
                    Services.Audio.Play("correct");
                    advanceAt = Time.unscaledTime + 0.8f;   // then on to the next line still to record
                }
            }
            Refresh();
        }

        /// <summary>Cuts the silence before and after the line and normalises the level.</summary>
        static float[] Trim(float[] d)
        {
            float peak = 0; foreach (var s in d) peak = Mathf.Max(peak, Mathf.Abs(s));
            float th = Mathf.Max(0.02f, peak * 0.08f);
            int a = 0, b = d.Length - 1;
            while (a < b && Mathf.Abs(d[a]) < th) a++;
            while (b > a && Mathf.Abs(d[b]) < th) b--;
            a = Mathf.Max(0, a - 2200); b = Mathf.Min(d.Length - 1, b + 4400);
            float gain = peak > 0.01f ? Mathf.Min(6f, 0.95f / peak) : 1f;
            var o = new float[b - a + 1];
            for (int i = 0; i < o.Length; i++) o[i] = d[a + i] * gain;
            return o;
        }

        void Play()
        {
            if (recording || lines.Count == 0) return;
            advanceAt = -1f;
            var id = lines[index].id;
            StartCoroutine(VoiceBank.Load(id, c => { if (c != null) { preview.Stop(); preview.clip = c; preview.volume = Mathf.Max(0.6f, Services.Audio.Bus(SoundBus.Voice)); preview.Play(); } }));
        }

        void Delete()
        {
            if (recording || lines.Count == 0) return;
            VoiceBank.Delete(lines[index].id);
            Refresh();
        }

        void Update()
        {
            if (advanceAt > 0f && Time.unscaledTime >= advanceAt) { advanceAt = -1f; NextTodo(); }
            // live input level
            float level = 0f;
            if (recording && recClip != null && device != null)
            {
                int pos = Microphone.GetPosition(device);
                if (pos > 512)
                {
                    var buf = new float[512];
                    recClip.GetData(buf, pos - 512);
                    foreach (var s in buf) level = Mathf.Max(level, Mathf.Abs(s));
                }
            }
            if (meter != null)
            {
                var r = meter.rectTransform;
                r.sizeDelta = new Vector2(Mathf.Lerp(r.sizeDelta.x, 1520f * Mathf.Clamp01(level * 1.6f), 0.5f), 16);
                meter.color = level > 0.9f ? new Color(1f, 0.35f, 0.4f) : UIFactory.Theme.cyan;
            }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame) ToggleRecord();
            if (kb.pKey.wasPressedThisFrame) Play();
            if (kb.nKey.wasPressedThisFrame) NextTodo();
            if (kb.rightArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Move(1);
            if (kb.leftArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Move(-1);
        }

        public override void Close()
        {
            if (recording && device != null) Microphone.End(device);
            base.Close();
        }
    }
}
