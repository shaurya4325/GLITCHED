using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// 9. Short answer: type a short answer, checked offline with keyword sets, synonyms and fuzzy matching
    /// (Levenshtein distance). No internet needed.
    /// </summary>
    public class ShortAnswerChallenge : ChallengeBase
    {
        TMP_InputField input;
        TextMeshProUGUI feedback;

        protected override void Build()
        {
            UIFactory.VLayout(Area, 18, 0, TextAnchor.UpperLeft);
            input = UIFactory.Input(Area, string.IsNullOrEmpty(D.placeholder) ? "type your answer" : D.placeholder, 40);
            UIFactory.Size(input, -1, 96);
            input.characterLimit = 80;
            input.onSubmit.AddListener(_ => { if (CanSubmit) OnSubmit(); });
            if (!InputHub.IsMobile) { input.Select(); input.ActivateInputField(); }
            feedback = UIFactory.Text(Area, "<color=#8fa3c0>Spelling doesn't have to be perfect. Understanding does.</color>", 24);
            UIFactory.Size(feedback, -1, 50);
            Ctx.setHelp("Type your answer, then press Enter or SUBMIT.");
        }

        public override bool CanSubmit => input != null && input.text.Trim().Length > 0;

        public override void OnSubmit()
        {
            if (Done || !CanSubmit) return;
            Finish(Check(input.text, D.accepted, D.keywords));
        }

        public override void Reveal(bool correct)
        {
            input.interactable = false;
            MarkGraphic(input.GetComponent<Image>(), correct);
            if (D.accepted != null && D.accepted.Count > 0)
                feedback.text = correct ? $"<color=#7cffa0>Accepted.</color> Model answer: <b>{D.accepted[0]}</b>" : $"Model answer: <b><color=#7cffa0>{D.accepted[0]}</color></b>";
        }

        // ------------------------------------------------------------------ offline answer checking
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (char.IsWhiteSpace(c) || c == '-' || c == '_') sb.Append(' ');
            }
            string r = sb.ToString().Trim();
            while (r.Contains("  ")) r = r.Replace("  ", " ");
            foreach (var article in new[] { "the ", "a ", "an " }) if (r.StartsWith(article)) r = r.Substring(article.Length);
            return r;
        }

        public static int Levenshtein(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Mathf.Min(Mathf.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                (prev, cur) = (cur, prev);
            }
            return prev[b.Length];
        }

        static bool Fuzzy(string word, string target)
        {
            if (word.Length < 3 || target.Length < 3) return word == target;
            int allowed = target.Length <= 5 ? 1 : target.Length <= 9 ? 2 : 3;
            return Levenshtein(word, target) <= allowed;
        }

        /// <summary>True if the answer matches an accepted answer (fuzzily) or every keyword group.</summary>
        public static bool Check(string answer, List<string> accepted, List<List<string>> keywords)
        {
            string a = Normalize(answer);
            if (a.Length == 0) return false;
            if (accepted != null)
                foreach (var acc in accepted)
                {
                    string n = Normalize(acc);
                    if (a == n || Fuzzy(a, n)) return true;
                }
            if (keywords != null && keywords.Count > 0)
            {
                var words = a.Split(' ');
                foreach (var group in keywords)
                {
                    bool hit = false;
                    foreach (var kw in group)
                    {
                        string k = Normalize(kw);
                        if (k.Length == 0) continue;
                        if (a.Contains(k)) { hit = true; break; }
                        foreach (var w in words) if (Fuzzy(w, k) || (k.Length >= 6 && w.StartsWith(k.Substring(0, 5)))) { hit = true; break; }
                        if (hit) break;
                    }
                    if (!hit) return false;
                }
                return true;
            }
            return false;
        }
    }
}
