using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Glitched.EditorTools
{
    /// <summary>
    /// Writes the voice-acting script: every line the game can speak, with its speaker, group and voice id.
    ///   Assets/StreamingAssets/Voice/script.csv  (id;speaker;group;text) - read by the in-game Voice Studio
    ///   Docs/Voice_Script.txt                    - a printable script, grouped by character
    /// Sources: Say/Companion.Say calls and line arrays in the code, the labs, enemy and boss data, region data, and
    /// the level plans (NPCs, terminals, doors, barriers, camera zones). Lines built at runtime from numbers or
    /// names ($"...") cannot be pre-recorded; lines heard in play that are missing here are added by VoiceBank.Note.
    /// </summary>
    public static class VoiceScript
    {
        static readonly Regex SayRx = new Regex(@"\b(?:Say|SayAsync)\(\s*""(?<sp>[a-z_]+)""\s*,");
        static readonly Regex CompanionRx = new Regex(@"Companion\.Say\(");
        static readonly Regex VarSpeakerSayRx = new Regex(@"\.Say\(\s*(?:sp|data\.profile[^,;]*)\s*,");
        static readonly Regex LabSayRx = new Regex(@"(?<![\w.])Say\(");
        static readonly Regex LineArrayRx = new Regex(@"string\[\]\s+\w*Lines\s*=\s*\{(?<e>.*?)\};", RegexOptions.Singleline);
        static readonly Regex LineFieldRx = new Regex(@"public string \w*(?:Line|line)\s*=\s*(?<e>[^;]*);");
        static readonly Regex LineAssignRx = new Regex(@"\.(?:line|jammedLine|lockedLine|enterLine|companionLine)\s*=\s*(?<e>[^;]*?);", RegexOptions.Singleline);
        static readonly Regex ReactionRx = new Regex(@"string reaction\s*=\s*(?<e>[^;]*?);", RegexOptions.Singleline);
        static readonly Regex LabOpeningRx = new Regex(@"string Opening\(\)\s*\{(?<b>.*?)\n        \}", RegexOptions.Singleline);
        static readonly Regex BossTernaryRx = new Regex(@"Boss\s*!=\s*null\s*\?\s*$");

        static string Unescape(string s) => s.Replace("\\\"", "\"").Replace("\\n", " ").Replace("\\'", "'").Replace("\\\\", "\\");

        /// <summary>
        /// The plain string literals of a C# expression, each with the code just before it. Interpolated ($"") and
        /// verbatim (@"") strings are skipped whole: they are built at runtime and cannot be pre-recorded.
        /// </summary>
        static List<(string text, string before)> Tokens(string e)
        {
            var list = new List<(string, string)>();
            for (int i = 0; i < e.Length; i++)
            {
                if (e[i] == '\'') { i += i + 1 < e.Length && e[i + 1] == '\\' ? 3 : 2; continue; }   // char literal
                if (e[i] != '"') continue;
                if (i > 0 && (e[i - 1] == '$' || e[i - 1] == '@')) { i = SkipString(e, i); continue; }
                var sb = new StringBuilder();
                int j = i + 1;
                for (; j < e.Length; j++)
                {
                    if (e[j] == '\\' && j + 1 < e.Length) { sb.Append(e[j]).Append(e[j + 1]); j++; continue; }
                    if (e[j] == '"') break;
                    sb.Append(e[j]);
                }
                // a value being compared or switched on (arc.id == "BioZone", case "air":) is not a spoken line
                string b = e.Substring(0, i).TrimEnd();
                bool compared = b.EndsWith("==") || b.EndsWith("!=") || b.EndsWith("case") || (j + 1 < e.Length && e.Substring(j + 1).TrimStart().StartsWith("=="));
                if (!compared) list.Add((Unescape(sb.ToString()), e.Substring(0, i)));
                i = j;
            }
            return list;
        }

        /// <summary>Index of the closing quote of the string that opens at i (handles $"...{expr "with" quotes}..." and @"").</summary>
        static int SkipString(string e, int i)
        {
            bool interp = i > 0 && (e[i - 1] == '$' || (e[i - 1] == '@' && i > 1 && e[i - 2] == '$'));
            bool verbatim = i > 0 && (e[i - 1] == '@' || (e[i - 1] == '$' && i > 1 && e[i - 2] == '@'));
            int depth = 0;
            for (int j = i + 1; j < e.Length; j++)
            {
                char c = e[j];
                if (depth == 0)
                {
                    if (c == '\\' && !verbatim) { j++; continue; }
                    if (c == '"') { if (verbatim && j + 1 < e.Length && e[j + 1] == '"') { j++; continue; } return j; }
                    if (interp && c == '{') { if (j + 1 < e.Length && e[j + 1] == '{') { j++; continue; } depth = 1; }
                }
                else
                {
                    if (c == '"') { j = SkipString(e, j); continue; }   // a string inside an interpolation hole
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                }
            }
            return e.Length - 1;
        }

        static IEnumerable<string> Literals(string e) => Tokens(e).Select(t => t.text);

        /// <summary>The argument expression that starts at index start (up to the next top-level ',' or ')').</summary>
        static string ArgExpr(string src, int start)
        {
            int depth = 0;
            for (int i = start; i < src.Length; i++)
            {
                char c = src[i];
                if (c == '"') { i = SkipString(src, i); continue; }
                if (c == '\'') { i += i + 1 < src.Length && src[i + 1] == '\\' ? 3 : 2; continue; }
                if (c == '(' || c == '[' || c == '{') depth++;
                else if (c == ')' || c == ']' || c == '}') { if (depth == 0) return src.Substring(start, i - start); depth--; }
                else if (c == ',' && depth == 0) return src.Substring(start, i - start);
                else if (c == ';') return src.Substring(start, i - start);
            }
            return src.Substring(start);
        }

        static IEnumerable<string> Args(Regex rx, string src) { foreach (Match m in rx.Matches(src)) yield return ArgExpr(src, m.Index + m.Length); }

        // lab file -> the region whose boss teaches in it
        static readonly Dictionary<string, string> LabArc = new Dictionary<string, string>
        {
            { "CSLoopLab", "CodeCity" }, { "PhysicsLab", "PhysicsValley" }, { "ChemBondLab", "ChemicalChaos" }, { "BioFlowTrace", "BioZone" },
        };

        public static void Generate(GameDatabase db)
        {
            var bossSpeakers = new HashSet<string>();
            var npcSpeakers = new HashSet<string>();
            var termSpeakers = new HashSet<string>();
            var enemySpeakers = new HashSet<string>();
            string Group(string sp)
            {
                if (sp == "aarav") return "Shaurya";
                if (sp == "pixel") return "Pixel";
                if (sp == "npc") return "NPCs";
                if (sp == KanhaLines.Speaker) return "Kanha";
                if (bossSpeakers.Contains(sp)) return "Bosses";
                if (enemySpeakers.Contains(sp)) return "Enemies";
                if (npcSpeakers.Contains(sp)) return "NPCs";
                if (termSpeakers.Contains(sp)) return "Terminals";
                return "Others";
            }
            var seen = new HashSet<string>();
            var pending = new List<(string sp, string t)>();
            void Add(string sp, string t)
            {
                if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(sp)) return;
                t = t.Trim();
                if (t.Length < 2 || !t.Any(char.IsLetter)) return;
                if (seen.Add(VoiceBank.Id(sp, t))) pending.Add((sp, t));
            }

            // ---- data: bosses, enemies, regions
            var bosses = new List<BossDataSO>();
            if (db != null)
            {
                foreach (var arc in db.arcs)
                {
                    if (arc == null) continue;
                    foreach (var b in new[] { arc.miniBoss, arc.boss }) if (b != null) { bosses.Add(b); bossSpeakers.Add(VoiceBank.SpeakerOf(b)); }
                    foreach (var l in arc.arrivalLines) Add("pixel", l);
                    foreach (var l in arc.restoredLines) Add("pixel", l);
                }
                foreach (var b in bosses)
                {
                    string sp = VoiceBank.SpeakerOf(b);
                    foreach (var list in new[] { b.introLines, b.taunts, b.hitLines, b.hintTaunts, b.teachingLines }) foreach (var l in list) Add(sp, l);
                    Add(sp, b.rematchLine);
                    Add(sp, b.finalDefeatLine);
                }
                foreach (var e in db.enemies)
                {
                    if (e == null || e is BossDataSO) continue;
                    string sp = VoiceBank.SpeakerOf(e);
                    enemySpeakers.Add(sp);
                    foreach (var list in new[] { e.taunts, e.hitLines, e.winLines, e.defeatLines }) foreach (var l in list) Add(sp, l);
                }
            }

            // ---- level plans: NPCs, terminals, doors, barriers, camera zones
            foreach (var plan in LevelPlans.All)
            {
                foreach (var n in plan.npcs)
                {
                    npcSpeakers.Add(n.name);
                    foreach (var l in n.corrupted ?? new string[0]) Add(n.name, l);
                    foreach (var l in n.restored ?? new string[0]) Add(n.name, l);
                    if (!string.IsNullOrEmpty(n.clue))
                    {
                        Add("pixel", "He's stuck. But listen between the glitches: " + n.clue);
                        Add(n.name, "Oh, and a tip: " + n.clue);
                    }
                }
                foreach (var t in plan.terminals)
                {
                    if (!string.IsNullOrEmpty(t.title)) { termSpeakers.Add(t.title); Add(t.title, t.text); }
                    Add("pixel", t.comment);
                }
                foreach (var d in plan.doors) Add("pixel", d.line);
                foreach (var b in plan.barriers) Add("pixel", b.line);
                foreach (var z in plan.zones) Add("pixel", z.line);
            }

            // ---- code
            foreach (var f in Directory.GetFiles("Assets/_Glitched/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string norm = f.Replace('\\', '/');
                if (norm.Contains("/Editor/")) continue;
                var src = File.ReadAllText(f);
                string file = Path.GetFileNameWithoutExtension(f);
                bool lab = norm.Contains("/TeachingMode/");
                bool world = norm.Contains("/World/") || norm.Contains("/Levels/") || norm.Contains("/Camera/");
                foreach (Match m in SayRx.Matches(src)) foreach (var t in Literals(ArgExpr(src, m.Index + m.Length))) Add(m.Groups["sp"].Value, t);
                foreach (var e in Args(CompanionRx, src)) foreach (var t in Literals(e)) Add("pixel", t);
                foreach (Match m in LineArrayRx.Matches(src)) foreach (var t in Literals(m.Groups["e"].Value)) Add("pixel", t);      // Pixel's idle / fall / victory quips
                if (world)
                {
                    foreach (Match m in LineFieldRx.Matches(src)) foreach (var t in Literals(m.Groups["e"].Value)) Add("pixel", t);  // default door / barrier / zone lines
                    foreach (Match m in LineAssignRx.Matches(src)) foreach (var t in Literals(m.Groups["e"].Value)) Add("pixel", t); // hazard and pit lines
                }
                foreach (Match m in ReactionRx.Matches(src)) foreach (var t in Literals(m.Groups["e"].Value)) Add("aarav", t);       // Shaurya's arrival reactions
                if (file == "BossFight")
                    foreach (var e in Args(VarSpeakerSayRx, src))
                        foreach (var t in Literals(e)) foreach (var b in bosses) Add(VoiceBank.SpeakerOf(b), t);                    // lines every boss says
                if (lab)
                {
                    string bossSp = null;
                    if (LabArc.TryGetValue(file, out var arcId) && db != null)
                    {
                        var arc = db.arcs.FirstOrDefault(a => a != null && a.id == arcId);
                        if (arc != null && arc.boss != null) bossSp = VoiceBank.SpeakerOf(arc.boss);
                    }
                    var exprs = Args(LabSayRx, src).ToList();
                    foreach (Match m in LabOpeningRx.Matches(src)) exprs.Add(m.Groups["b"].Value);
                    foreach (var e in exprs)
                        foreach (var (t, before) in Tokens(e))
                        {
                            // the first branch of "Boss != null ? ..." is the boss teaching; everything else is Pixel
                            if (BossTernaryRx.IsMatch(before.TrimEnd())) { if (bossSp != null) Add(bossSp, t); }
                            else Add("pixel", t);
                        }
                }
            }

            foreach (var l in KanhaLines.All()) Add(KanhaLines.Speaker, l);   // his default lines, for the printed script
            string[] order = { "Shaurya", "Kanha", "Pixel", "NPCs", "Enemies", "Bosses", "Terminals", "Others" };
            var lines = pending.Select(l => (l.sp, group: Group(l.sp), l.t))
                               .OrderBy(l => System.Array.IndexOf(order, l.group)).ThenBy(l => l.sp).ToList();

            Directory.CreateDirectory("Assets/StreamingAssets/Voice");
            var csv = new StringBuilder("id;speaker;group;text\n");
            foreach (var (sp, g, t) in lines)
                csv.Append(VoiceBank.Id(sp, t)).Append(';').Append(sp).Append(';').Append(g).Append(';').Append(t.Replace("\n", " ").Replace(";", ",")).Append('\n');
            File.WriteAllText("Assets/StreamingAssets/Voice/script.csv", csv.ToString(), new UTF8Encoding(false));

            Directory.CreateDirectory("Docs");
            var txt = new StringBuilder("GLITCHED - VOICE SCRIPT\nRecord in the game: press F8 (or Settings > VOICE STUDIO). R = record/stop, P = play, arrows = previous/next line.\n");
            foreach (var g in order)
            {
                var inGroup = lines.Where(l => l.group == g).ToList();
                if (inGroup.Count == 0) continue;
                txt.Append($"\n=== {g.ToUpperInvariant()} ({inGroup.Count} lines) ===\n");
                foreach (var sp in inGroup.Select(l => l.sp).Distinct())
                {
                    if (g != "Shaurya" && g != "Pixel") txt.Append($"\n  -- {sp.Replace('_', ' ').ToUpperInvariant()} --\n");
                    int n = 1;
                    foreach (var l in inGroup.Where(l => l.sp == sp)) txt.Append($"{n++,4}. {l.t}\n");
                }
            }
            File.WriteAllText("Docs/Voice_Script.txt", txt.ToString(), new UTF8Encoding(false));
            Debug.Log($"[GLITCHED] Voice script: {lines.Count} lines (" + string.Join(", ", order.Select(g => $"{g} {lines.Count(l => l.group == g)}")) + ")");
        }

        /// <summary>Batch entry: regenerate only the voice script.</summary>
        public static void GenerateBatch()
        {
            var db = UnityEditor.AssetDatabase.FindAssets("t:GameDatabase").Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
            Generate(db);
        }
    }
}
