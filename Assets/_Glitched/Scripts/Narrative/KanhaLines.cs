using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Kanha the Dard's dialogue, grouped by moment (intro, taunts, reactions...). Everything he says comes from here,
    /// so his lines can be rewritten without touching code: in the game (Settings > KANHA'S LINES), or in the text
    /// file it saves (player data folder: Dialogue/kanha.txt), or in StreamingAssets/Dialogue/kanha.txt for a build.
    /// File format: a [section] header, then one line per row. Blank rows and rows starting with # are ignored.
    /// </summary>
    public static class KanhaLines
    {
        public const string Speaker = "kanha";

        /// <summary>The sections, in the order the final exam uses them, with a short note for the writer.</summary>
        public static readonly (string key, string note)[] Sections =
        {
            ("intro", "He turns around in the exam hall (said in order)"),
            ("rules", "He explains the test paper (said in order)"),
            ("section_a", "Start of Section A: Physics"),
            ("section_b", "Start of Section B: Chemistry"),
            ("section_c", "Start of Section C: Biology"),
            ("correct", "You answered right (one is picked at random)"),
            ("wrong", "You answered wrong (random)"),
            ("knockdown", "Your stability hit zero (random)"),
            ("almost", "You are close to passing (random)"),
            ("fail", "You failed the paper: re-exam (random)"),
            ("defeat", "You passed: his last words (said in order)"),
        };

        static readonly Dictionary<string, List<string>> Defaults = new Dictionary<string, List<string>>
        {
            ["intro"] = new List<string>
            {
                "Well, well. Shaurya. The gamer who thought The Nexus was just another level.",
                "You beat my Glitch Lords. Cute. They were homework. I am the EXAM.",
                "I am Kanha. Kanha the Dard. Every glitch you fixed? I wrote it. Line by line.",
            },
            ["rules"] = new List<string>
            {
                "One paper. Physics, Chemistry, Biology. No calculator. No Pixel. No mercy.",
                "Score eight out of twelve and you go home. Fail, and you stay here. Forever. Retaking.",
                "Pens up. Your time starts... now.",
            },
            ["section_a"] = new List<string> { "Section A. Physics. Let's see if your brain obeys the laws of motion." },
            ["section_b"] = new List<string> { "Section B. Chemistry. Try not to react." },
            ["section_c"] = new List<string> { "Section C. Biology. Your nervous system already looks nervous." },
            ["correct"] = new List<string>
            {
                "Lucky guess.",
                "Hmph. Even a stopped clock is right twice a day.",
                "Fine. ONE mark. Don't let it go to your head.",
                "Who leaked the paper?!",
                "That was the easy one. Obviously.",
            },
            ["wrong"] = new List<string>
            {
                "Wrong! That's the sound of your percentage dropping.",
                "Did you even open the textbook? Or just the game?",
                "Negative marking would have destroyed you.",
                "That answer gave ME a headache. And headaches are my job.",
                "Wrong. Feel that? That's the dard.",
            },
            ["knockdown"] = new List<string>
            {
                "Look at you. On the floor, just like your marks.",
                "Get up. The invigilator hasn't called time yet.",
            },
            ["almost"] = new List<string>
            {
                "No. No no no. I studied the WHOLE syllabus!",
                "You're not supposed to know that! That's from the reference book!",
            },
            ["fail"] = new List<string>
            {
                "FAIL. Re-exam. Same hall, new paper. I have infinite papers.",
                "Below the pass mark. Sit down. We go again.",
            },
            ["defeat"] = new List<string>
            {
                "Impossible... I memorised everything. Every chapter. Every diagram.",
                "You didn't memorise it. You... understood it. That's not fair.",
                "The Nexus is yours, gamer. But remember: there is always another exam.",
            },
        };

        static Dictionary<string, List<string>> lines;

        public static string UserFile => Path.Combine(Application.persistentDataPath, "Dialogue", "kanha.txt");
        public static string ShippedFile => Path.Combine(Application.streamingAssetsPath, "Dialogue", "kanha.txt");

        static void Ensure()
        {
            if (lines != null) return;
            lines = new Dictionary<string, List<string>>();
            foreach (var kv in Defaults) lines[kv.Key] = new List<string>(kv.Value);
            foreach (var path in new[] { ShippedFile, UserFile })   // the player's own version wins
            {
                try { if (File.Exists(path)) Merge(Parse(File.ReadAllText(path))); }
                catch (System.Exception e) { Debug.LogWarning("[Kanha] " + e.Message); }
            }
        }

        static void Merge(Dictionary<string, List<string>> d) { foreach (var kv in d) if (kv.Value.Count > 0) lines[kv.Key] = kv.Value; }

        public static Dictionary<string, List<string>> Parse(string text)
        {
            var d = new Dictionary<string, List<string>>();
            List<string> cur = null;
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var l = raw.Trim();
                if (l.Length == 0 || l.StartsWith("#")) continue;
                if (l.StartsWith("[") && l.EndsWith("]")) { cur = d[l.Substring(1, l.Length - 2).Trim().ToLowerInvariant()] = new List<string>(); continue; }
                cur?.Add(l);
            }
            return d;
        }

        public static string Format(Dictionary<string, List<string>> d)
        {
            var sb = new StringBuilder("# Kanha the Dard - dialogue. One line per row under each [section].\n");
            foreach (var (key, note) in Sections)
            {
                sb.Append($"\n# {note}\n[{key}]\n");
                if (d.TryGetValue(key, out var list)) foreach (var l in list) sb.Append(l).Append('\n');
            }
            return sb.ToString();
        }

        public static List<string> Get(string section) { Ensure(); return lines.TryGetValue(section, out var l) ? l : new List<string>(); }
        public static List<string> Default(string section) => Defaults.TryGetValue(section, out var l) ? new List<string>(l) : new List<string>();

        public static string Random(string section)
        {
            var l = Get(section);
            return l.Count == 0 ? "" : l[UnityEngine.Random.Range(0, l.Count)];
        }

        /// <summary>Every line he can say (for the Voice Studio).</summary>
        public static IEnumerable<string> All()
        {
            Ensure();
            foreach (var (key, _) in Sections) foreach (var l in Get(key)) yield return l;
        }

        /// <summary>Saves the player's version of a section (Settings > KANHA'S LINES).</summary>
        public static void Set(string section, List<string> newLines)
        {
            Ensure();
            lines[section] = newLines;
            Directory.CreateDirectory(Path.GetDirectoryName(UserFile));
            File.WriteAllText(UserFile, Format(lines), new UTF8Encoding(false));
        }

        public static void Reload() => lines = null;
    }
}
