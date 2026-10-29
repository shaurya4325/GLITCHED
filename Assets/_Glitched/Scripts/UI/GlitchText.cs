using System.Text;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// TextMeshPro vertex animator for the glitch text effect (Section 10.2): typewriter reveal, letters swapping to
    /// lookalike digits (WELC0ME), jittering and colour-split characters, and the box briefly rewriting itself.
    /// Intensity follows the global Corruption value unless overridden.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class GlitchText : MonoBehaviour
    {
        public float charsPerSecond = 42f;
        [Range(-1, 1)] public float intensityOverride = -1f;   // -1 = follow corruption
        public bool typewriter = true;
        public System.Action onCharacter;

        TMP_Text text;
        string source = "";
        string displayed = "";
        float visible;
        float nextSwap, rewriteUntil;
        public bool Finished { get; private set; } = true;

        static readonly string From = "OoEeAaIiSsTBGgLlZz";
        static readonly string To = "00334411557869112 2".Replace(" ", "");

        void Awake() { text = GetComponent<TMP_Text>(); }

        public float Intensity
        {
            get
            {
                float i = intensityOverride >= 0 ? intensityOverride : (Services.Corruption != null ? Services.Corruption.Value : 0f);
                if (Services.Settings != null && Services.Settings.Data.reduceFlashing) i *= 0.35f;
                return i;
            }
        }

        public void Show(string s, bool instant = false)
        {
            if (text == null) text = GetComponent<TMP_Text>();
            source = s ?? "";
            displayed = Corrupt(source, Intensity * 0.12f);
            text.text = displayed;
            text.ForceMeshUpdate();
            visible = instant || !typewriter ? 99999 : 0;
            text.maxVisibleCharacters = instant || !typewriter ? 99999 : 0;
            Finished = instant || !typewriter;
        }

        public void Complete()
        {
            visible = 99999;
            text.maxVisibleCharacters = 99999;
            Finished = true;
        }

        void Update()
        {
            if (text == null) return;
            float k = Intensity;
            float dt = Time.unscaledDeltaTime;
            if (!Finished)
            {
                int before = (int)visible;
                visible += dt * charsPerSecond;
                int now = (int)visible;
                text.maxVisibleCharacters = now;
                if (now != before && now % 2 == 0) onCharacter?.Invoke();
                if (now >= text.textInfo.characterCount) Finished = true;
            }

            // occasional letter swaps (WELC0ME) and box rewrites
            if (k > 0.05f && Time.unscaledTime > nextSwap)
            {
                nextSwap = Time.unscaledTime + Random.Range(0.12f, 0.5f) / Mathf.Max(0.3f, k);
                if (Random.value < 0.08f * k) rewriteUntil = Time.unscaledTime + 0.09f;
                string next = Time.unscaledTime < rewriteUntil ? Scramble(source, 0.85f) : Corrupt(source, k * 0.12f);
                if (next != displayed) { displayed = next; text.text = displayed; }
            }
            else if (k <= 0.05f && displayed != source)
            {
                displayed = source; text.text = source;
            }

            if (k <= 0.02f) return;
            text.ForceMeshUpdate();
            var info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible || i >= text.maxVisibleCharacters) continue;
                float r = Hash(i * 13.1f + Mathf.Floor(Time.unscaledTime * 14f));
                if (r > 0.06f * k) continue;
                int mi = ch.materialReferenceIndex, vi = ch.vertexIndex;
                var verts = info.meshInfo[mi].vertices;
                var cols = info.meshInfo[mi].colors32;
                Vector3 off = new Vector3((Hash(i + 3.3f + Time.unscaledTime) - 0.5f) * 10f * k, (Hash(i + 7.7f) - 0.5f) * 6f * k, 0);
                Color32 tint = r < 0.03f * k ? new Color32(255, 70, 210, 255) : new Color32(80, 240, 255, 255);
                for (int j = 0; j < 4; j++) { verts[vi + j] += off; cols[vi + j] = tint; }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        static float Hash(float x) => Mathf.Repeat(Mathf.Sin(x * 127.1f) * 43758.5453f, 1f);

        /// <summary>Replaces some letters with lookalike digits, skipping rich-text tags.</summary>
        public static string Corrupt(string s, float chance)
        {
            if (string.IsNullOrEmpty(s) || chance <= 0) return s;
            var sb = new StringBuilder(s.Length);
            bool tag = false;
            foreach (char c in s)
            {
                if (c == '<') tag = true;
                if (!tag)
                {
                    int k = From.IndexOf(c);
                    if (k >= 0 && k < To.Length && Random.value < chance) { sb.Append(To[k]); continue; }
                }
                if (c == '>') tag = false;
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Heavy scramble used for hover glitches and box rewrites.</summary>
        public static string Scramble(string s, float chance)
        {
            if (string.IsNullOrEmpty(s)) return s;
            const string junk = "#%&@$01<>/\\|=+*?!";
            var sb = new StringBuilder(s.Length);
            bool tag = false;
            foreach (char c in s)
            {
                if (c == '<') tag = true;
                if (!tag && c != ' ' && c != '\n' && Random.value < chance) sb.Append(junk[Random.Range(0, junk.Length)]);
                else sb.Append(c);
                if (c == '>') tag = false;
            }
            return sb.ToString();
        }

        /// <summary>Makes a word stutter: "what" -> "w-w-what".</summary>
        public static string Stutter(string s, float chance)
        {
            if (string.IsNullOrEmpty(s) || chance <= 0) return s;
            var words = s.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];
                if (w.Length > 2 && char.IsLetter(w[0]) && !w.Contains("<") && Random.value < chance)
                    words[i] = w[0] + "-" + w[0] + "-" + w;
            }
            return string.Join(" ", words);
        }
    }
}
