using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Per-concept mastery (0-100), updated with an exponential moving average of recent accuracy and speed
    /// (Section 9.2). Saved in the slot. Also tracks the last 5 answers for adaptive difficulty.
    /// </summary>
    public class MasteryTracker : MonoBehaviour
    {
        public const float Alpha = 0.3f;

        public float Get(string concept)
        {
            var d = Services.Game.Data;
            return d.mastery.TryGetValue(concept, out var m) ? m.value : 0f;
        }

        public bool Seen(string concept) => Services.Game.Data.mastery.ContainsKey(concept);

        public void Record(QuestionData q, bool correct, bool fast)
        {
            if (q == null) return;
            var d = Services.Game.Data;
            if (!d.mastery.TryGetValue(q.concept, out var m)) d.mastery[q.concept] = m = new ConceptMastery { subject = q.subject };
            float score = correct ? (fast ? 100f : 85f) : 0f;
            // harder questions move the needle more
            float a = Alpha * Mathf.Lerp(0.8f, 1.25f, (q.difficulty - 1) / 4f);
            m.value = Mathf.Clamp(m.value + a * (score - m.value), 0f, 100f);
            m.seen++;
            if (correct) m.correct++;
            if (fast) m.fast++;
            d.recentAnswers.Add(correct ? 1 : 0);
            while (d.recentAnswers.Count > 5) d.recentAnswers.RemoveAt(0);

            if (correct && q.type == ChallengeType.Debugging) Services.Achievements.Add("bugs_fixed");
            if (correct && fast) Services.Achievements.Add("fast_solves");
            if (correct && q.type == ChallengeType.Construction && q.data != null && q.data.display == "molecule") Services.Achievements.Add("molecules_built");
            GameEvents.RaiseMastery(q.concept, m.value);
        }

        /// <summary>The mastery check (3 in a row) marks a concept as fully mastered.</summary>
        public void MarkMastered(string concept, string subject)
        {
            var d = Services.Game.Data;
            if (!d.mastery.TryGetValue(concept, out var m)) d.mastery[concept] = m = new ConceptMastery { subject = subject };
            m.value = 100f;
            GameEvents.RaiseMastery(concept, 100f);
        }

        /// <summary>-1, 0 or +1 difficulty shift from the last 5 answers.</summary>
        public int DifficultyShift()
        {
            var r = Services.Game.Data.recentAnswers;
            if (r.Count < 3) return 0;
            int sum = 0; foreach (var x in r) sum += x;
            if (sum >= 4) return 1;
            if (sum <= 1) return -1;
            return 0;
        }
    }
}
