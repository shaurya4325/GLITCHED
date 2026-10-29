using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Picks the next question (Section 9.2): weights low-mastery concepts higher, never repeats a question until
    /// the pool for that concept is used up, varies type and situation, and adapts difficulty within +/-1.
    /// "Same concept, different situation": callers can hold the concept fixed and ask for a new problem.
    /// </summary>
    public class QuestionSelector : MonoBehaviour
    {
        string lastType, lastSituation;

        List<string> Used(string concept)
        {
            var d = Services.Game.Data;
            if (!d.usedQuestions.TryGetValue(concept, out var list)) d.usedQuestions[concept] = list = new List<string>();
            return list;
        }

        public void MarkUsed(QuestionData q)
        {
            if (q == null) return;
            var u = Used(q.concept);
            if (!u.Contains(q.id)) u.Add(q.id);
            lastType = q.type.ToString();
            lastSituation = q.situation;
        }

        /// <param name="arc">Arc id ("CodeCity"...). "TheCore" or "*" draws from every arc.</param>
        /// <param name="concept">Fixed concept, or empty for adaptive choice among the arc's concepts.</param>
        /// <param name="minDiff">Lowest difficulty wanted.</param>
        /// <param name="maxDiff">Highest difficulty wanted.</param>
        /// <param name="preferred">Preferred challenge type (soft).</param>
        /// <param name="excludeId">A question id to avoid (the one just failed).</param>
        public QuestionData Pick(string arc, string concept, int minDiff, int maxDiff, ChallengeType? preferred = null, string excludeId = null, IList<ChallengeType> preferredTypes = null, bool adapt = true)
        {
            var bank = Services.Questions.All;
            if (bank.Count == 0) return null;
            int shift = adapt ? Services.Mastery.DifficultyShift() : 0;
            int lo = Mathf.Clamp(minDiff + Mathf.Min(0, shift), 1, 5), hi = Mathf.Clamp(maxDiff + Mathf.Max(0, shift), 1, 5);
            bool anyArc = string.IsNullOrEmpty(arc) || arc == "*" || arc == "TheCore";

            var candidates = new List<QuestionData>();
            for (int pass = 0; pass < 4 && candidates.Count == 0; pass++)
            {
                foreach (var q in bank)
                {
                    if (q.id == excludeId) continue;
                    if (!anyArc && q.arc != arc) continue;
                    if (!string.IsNullOrEmpty(concept) && q.concept != concept && pass < 3) continue;
                    int dl = pass >= 1 ? lo - pass : lo, dh = pass >= 1 ? hi + pass : hi;
                    if (q.difficulty < dl || q.difficulty > dh) continue;
                    candidates.Add(q);
                }
            }
            if (candidates.Count == 0) return null;

            // never repeat until the concept's pool is used up
            var fresh = candidates.FindAll(q => !Used(q.concept).Contains(q.id));
            if (fresh.Count == 0)
            {
                foreach (var q in candidates) Used(q.concept).Remove(q.id);
                fresh = candidates;
            }

            // weighted random
            float total = 0;
            var weights = new float[fresh.Count];
            for (int i = 0; i < fresh.Count; i++)
            {
                var q = fresh[i];
                float w = 1f + (100f - Services.Mastery.Get(q.concept)) / 25f;      // low mastery -> more likely
                if (preferred.HasValue && q.type == preferred.Value) w *= 3f;
                if (preferredTypes != null && preferredTypes.Contains(q.type)) w *= 2.2f;
                if (q.type.ToString() == lastType) w *= 0.45f;                        // vary the interaction type
                if (!string.IsNullOrEmpty(lastSituation) && q.situation == lastSituation) w *= 0.4f;
                weights[i] = w; total += w;
            }
            float r = Random.value * total;
            for (int i = 0; i < fresh.Count; i++)
            {
                r -= weights[i];
                if (r <= 0) { MarkUsed(fresh[i]); return fresh[i]; }
            }
            MarkUsed(fresh[fresh.Count - 1]);
            return fresh[fresh.Count - 1];
        }

        /// <summary>
        /// An MCQ on one Class XI topic (small enemies, answer platforms): never repeats until the topic's pool is used
        /// up, starts easy and gets harder as the topic's mastery grows. shortOptions = only questions whose answers fit
        /// on a platform label.
        /// </summary>
        // own random stream, reseeded for every new save: no two saves meet the same questions in the same order
        static System.Random rng = new System.Random(System.Environment.TickCount);
        public static void Reseed() { int seed = System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode(); rng = new System.Random(seed); QuestionGenerator.Reseed(seed * 31 + 7); }

        public QuestionData PickTopic(string topic, string excludeId = null, bool shortOptions = false)
        {
            // most of the time a freshly generated question (random numbers / facts), otherwise one from the bank
            if (QuestionGenerator.Has(topic) && rng.NextDouble() < 0.6)
                for (int i = 0; i < 4; i++)
                {
                    var g = QuestionGenerator.Make(topic);
                    if (g != null && (!shortOptions || ShortOptions(g))) { lastType = g.type.ToString(); return g; }
                }
            var pool = new List<QuestionData>();
            foreach (var q in Services.Questions.ForTopic(topic))
            {
                if (q.id == excludeId || q.data.options == null || q.data.options.Count < 2) continue;
                if (shortOptions && !ShortOptions(q)) continue;
                pool.Add(q);
            }
            if (pool.Count == 0) return null;
            var fresh = pool.FindAll(q => !Used(q.concept).Contains(q.id));
            if (fresh.Count == 0) { foreach (var q in pool) Used(q.concept).Remove(q.id); fresh = pool; }
            float m = Services.Mastery.Get(pool[0].concept);
            int target = m < 35f ? 1 : m < 70f ? 2 : 3;
            float total = 0f;
            var w = new float[fresh.Count];
            for (int i = 0; i < fresh.Count; i++) { w[i] = 1f / (1f + Mathf.Abs(fresh[i].difficulty - target) * 1.5f); total += w[i]; }
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < fresh.Count; i++)
            {
                r -= w[i];
                if (r <= 0f) { MarkUsed(fresh[i]); return fresh[i]; }
            }
            MarkUsed(fresh[fresh.Count - 1]);
            return fresh[fresh.Count - 1];
        }

        /// <summary>Every answer is short enough to print on a platform (and the question fits a sign).</summary>
        public static bool ShortOptions(QuestionData q)
        {
            if (q.data.options == null || string.IsNullOrEmpty(q.prompt) || q.prompt.Length > 80 || !string.IsNullOrEmpty(q.data.code)) return false;
            foreach (var o in q.data.options) if (System.Text.RegularExpressions.Regex.Replace(o, "<.*?>", "").Length > 13) return false;
            return true;
        }

        /// <summary>The concept most in need of practice among a list (used for normal enemies).</summary>
        public string WeakestConcept(IList<string> concepts)
        {
            if (concepts == null || concepts.Count == 0) return "";
            string best = concepts[0];
            float bestScore = float.MaxValue;
            foreach (var c in concepts)
            {
                float s = Services.Mastery.Get(c) + Random.Range(0f, 25f);
                if (s < bestScore) { bestScore = s; best = c; }
            }
            return best;
        }
    }
}
