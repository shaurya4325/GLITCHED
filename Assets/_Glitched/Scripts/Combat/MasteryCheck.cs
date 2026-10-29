using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// After every boss, before the celebration: a 3-question mastery check (easy -> medium -> application).
    /// All 3 correct = CONCEPT MASTERED + 1 Knowledge Point (Section 9.2).
    /// </summary>
    public static class MasteryCheck
    {
        public static IEnumerator Run(string arcId, string concept, ArcDataSO arc)
        {
            var box = Services.UI.Dialogue;
            if (concept == "Synthesis") concept = "";
            yield return box.Say("pixel", "Beating a boss proves you solved five problems. Let's prove you UNDERSTAND it. Three quick ones. No pressure. Well, some pressure.", "normal");
            box.Show(false);
            int[,] bands = { { 1, 2 }, { 2, 3 }, { 4, 5 } };
            int correct = 0;
            string subject = "";
            for (int i = 0; i < 3; i++)
            {
                var q = Services.Selector.Pick(arcId, concept, bands[i, 0], bands[i, 1], null, null, null, false);
                if (q == null) { correct++; continue; }
                subject = q.subject;
                ChallengeOutcome o = default; bool done = false;
                ChallengeEngine.Run(q, $"MASTERY CHECK {i + 1}/3", 0, r => { o = r; done = true; }, new ChallengeEngine.Options { header = $"<color=#ffd640>MASTERY CHECK {i + 1}/3</color>  ·  {(string.IsNullOrEmpty(concept) ? "EVERYTHING" : concept.ToUpper())}", noTimer = true });
                while (!done) yield return null;
                Services.Mastery.Record(q, o.correct, o.fast);
                if (o.correct) { correct++; Services.Audio.Play("correct"); }
                else yield return ChallengeEngine.ShowExplanation(q, o, "NOT QUITE, BUT HERE'S WHY:");
            }
            if (correct == 3)
            {
                if (!string.IsNullOrEmpty(concept)) Services.Mastery.MarkMastered(concept, subject);
                Services.Game.AddKnowledge(1, concept);
                if (arc != null) Services.Game.Data.Arc(arc.id).conceptMastered = true;
                if (arc != null && arc.id == "PhysicsValley") Services.Achievements.Add("physics_mastered");
                Services.Audio.Play("mastered");
                Services.UI.Banner("CONCEPT MASTERED", UIFactory.Theme.yellow, 110, 2f, $"{(string.IsNullOrEmpty(concept) ? "THE NEXUS" : concept.ToUpper())}   +1 KNOWLEDGE POINT");
                VFX.Confetti(PlayerController.Instance != null ? PlayerController.Instance.transform.position + Vector3.up : Vector3.zero);
                yield return new WaitForSecondsRealtime(2.2f);
            }
            else
            {
                Services.UI.Banner($"MASTERY {correct}/3", UIFactory.Theme.cyan, 90, 1.6f, "Great progress. You'll meet this concept again in new situations.");
                yield return new WaitForSecondsRealtime(1.8f);
            }
        }
    }
}
