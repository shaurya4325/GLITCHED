using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Runs normal and elite encounters (Section 6.1): no hard pause, the world keeps animating, the enemy winds up
    /// while the challenge is open, and every answer is a blow in the fight. Losing all Stability is never a game
    /// over: it triggers the failure-teaching flow, and the fight continues.
    /// </summary>
    public class EncounterManager : MonoBehaviour
    {
        static EncounterManager instance;
        public static EncounterManager Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("EncounterManager").AddComponent<EncounterManager>();
                return instance;
            }
        }
        public static bool Busy { get; private set; }
        public int Combo { get; private set; }

        void OnDestroy() { if (instance == this) { instance = null; Busy = false; } }

        public void Begin(Enemy e)
        {
            if (Busy || e == null || e.Defeated || PlayerController.Instance == null) return;
            StartCoroutine(Fight(e));
        }

        public void ResetCombo() { Combo = 0; GameEvents.RaiseCombo(0); }

        public void AddCombo() { Combo++; GameEvents.RaiseCombo(Combo); DamageFeedback.Combo(Combo); }

        IEnumerator Fight(Enemy e)
        {
            Busy = true;
            var p = PlayerController.Instance;
            var data = e.data;
            var arc = Services.Game.CurrentArc;
            string arcId = arc != null ? arc.id : data.arc;
            p.Lock(this);
            e.InEncounter = true;
            e.ShowBar(true);
            CameraRig.Instance?.FrameEncounter(e.transform);
            Services.Audio.Music.Combat();
            GameEvents.RaiseEncounter(true);
            Services.UI.Log.Add($"<color=#ff3dc8>{data.displayName}</color> blocks the way!");
            e.Say(e.RandomOf(data.taunts));
            p.Visual.Face(Mathf.Sign(e.transform.position.x - p.transform.position.x));
            yield return new WaitForSeconds(0.9f);

            bool tutorialLoss = e.tutorialFight && !Services.Game.Data.HasFlag("tut_lost_once");
            // small enemies after a lesson terminal: quick Class XI MCQs on that terminal's topic only
            bool quiz = !string.IsNullOrEmpty(e.topic) && Services.Questions.ForTopic(e.topic).Count > 0 && (data.tier == EnemyTier.Normal || data.tier == EnemyTier.Elite);
            var lesson = quiz ? Services.Questions.Lesson(e.topic) : null;
            bool skippedLesson = lesson != null && !Services.Game.Data.HasFlag("lesson_" + lesson.id);
            if (lesson != null) Services.UI.Hud.SetTopic(lesson);
            int answered = 0, correctCount = 0, fastCount = 0;
            bool allPerfect = true;
            int wrongInFight = 0;
            string lastConcept = null;
            QuestionData lastQ = null;

            while (e.HP > 0)
            {
                // choose the question: concept held for a retry, otherwise the weakest concept
                var concepts = data.concepts != null && data.concepts.Count > 0 ? data.concepts : (arc != null ? arc.concepts : new List<string>());
                string concept = wrongInFight > 0 && lastConcept != null ? lastConcept : Services.Selector.WeakestConcept(concepts);
                int lo = data.tier == EnemyTier.Elite ? 2 : 1, hi = data.tier == EnemyTier.Elite ? 4 : 2;
                QuestionData q = null;
                if (quiz) q = Services.Selector.PickTopic(e.topic, lastQ != null ? lastQ.id : null);
                if (q == null && answered == 0 && !string.IsNullOrEmpty(e.firstQuestionId)) q = Services.Questions.Get(e.firstQuestionId);
                if (q == null) q = Services.Selector.Pick(arcId, concept, lo, hi, null, lastQ != null ? lastQ.id : null,
                    data.preferredTypes != null && data.preferredTypes.Count > 0 ? data.preferredTypes : arc != null ? arc.emphasisTypes : null);
                if (q == null) { Debug.LogWarning("[Encounter] No question available"); break; }
                lastConcept = q.concept; lastQ = q;
                string attack = e.RandomOf(data.attackNames);
                Services.UI.Log.Add($"<color=#ff3dc8>{data.displayName}</color> uses <b>{attack}</b>.");

                ChallengeOutcome o = default;
                bool done = false;
                var opt = new ChallengeEngine.Options
                {
                    enemyName = data.displayName,
                    attackName = attack,
                    showHint = wrongInFight > 0,
                    timeScale = tutorialLoss ? 0.32f : 1f,
                };
                if (quiz && q.topic == e.topic)
                    QuickQuiz.Run(q, new QuickQuiz.Options
                    {
                        enemyName = data.displayName, attackName = attack, timeScale = opt.timeScale, showHint = wrongInFight > 0, number = answered + 1,
                        recap = answered == 0 && skippedLesson ? lesson.keyIdea : null,
                    }, r => { o = r; done = true; });
                else ChallengeEngine.Run(q, "", 0, r => { o = r; done = true; }, opt);
                while (!done) yield return null;
                answered++;
                Services.Mastery.Record(q, o.correct, o.fast);

                if (o.correct)
                {
                    correctCount++;
                    if (o.fast) fastCount++; else allPerfect = false;
                    AddCombo();
                    int baseDmg = data.tier == EnemyTier.Elite ? Mathf.CeilToInt(data.maxHP / (float)Mathf.Max(1, data.challengesToDefeat)) : data.maxHP;
                    int dmg = Mathf.RoundToInt(baseDmg * DamageFeedback.ComboMultiplier(Combo) * (o.fast ? 1.6f : 1f));
                    Services.UI.Log.Add($"CORRECT in {o.seconds:0.0}s, " + (o.fast ? "<color=#ffd640>CRITICAL</color>" : "ATTACK") + $" -{dmg} HP" + (Combo > 1 ? $"  <color=#ff3dc8>COMBO x{Combo}</color>" : ""));
                    bool finalBlow = e.HP - dmg <= 0;
                    bool ultimate = finalBlow && allPerfect && wrongInFight == 0 && answered >= 2;
                    if (ultimate) { e.TakeHit(0, true); yield return DamageFeedback.Ultimate(e, p); e.TakeHit(dmg, true); }
                    else if (o.fast) DamageFeedback.Critical(e, dmg, p);
                    else DamageFeedback.Attack(e, dmg, p);
                    Services.Game.AddXP(Mathf.RoundToInt(20 * q.difficulty * (o.fast ? 1.5f : 1f)));
                    if (e.HP > 0) { yield return new WaitForSeconds(0.6f); e.Say(e.RandomOf(data.hitLines)); }
                    yield return new WaitForSeconds(0.7f);
                }
                else
                {
                    wrongInFight++;
                    allPerfect = false;
                    ResetCombo();
                    Services.UI.Log.Add(o.timedOut ? "TOO SLOW. <color=#ff5a6a>ENEMY TURN</color>" : "WRONG. <color=#ff5a6a>ENEMY TURN</color>");
                    e.Say(e.RandomOf(data.winLines));
                    yield return DamageFeedback.EnemyTurn(e, p);
                    if (tutorialLoss)
                    {
                        Services.Game.Data.SetFlag("tut_lost_once");
                        tutorialLoss = false;
                        p.TakeDamage(p.Stability, e.transform.position, false);
                        yield return TutorialLossTalk(p);
                        p.RestoreFull();
                    }
                    if (!(quiz && q.topic == e.topic)) yield return ChallengeEngine.ShowExplanation(q, o);   // the quiz card already showed the why
                    if (p.Stability <= 0) yield return FailureTeaching(p, q);
                }
            }

            // victory
            if (e.HP <= 0)
            {
                int xp = data.rewardXP, coins = data.rewardCoins;
                bool kp = data.tier == EnemyTier.Elite && wrongInFight == 0;
                Services.Game.AddXP(xp);
                Services.Game.AddCoins(coins);
                if (kp) Services.Game.AddKnowledge(1, lastConcept);
                Services.UI.Log.Add($"<color=#ff3dc8>{data.displayName}</color> glitches, splits into colour channels, and stops existing.");
                Services.UI.Log.Add($"<color=#ffd640>+{xp} XP   +{coins} COINS</color>" + (kp ? $"   <color=#ff3dc8>+1 KNOWLEDGE POINT ({lastConcept})</color>" : ""));
                yield return e.Defeat();
                if (Random.value < 0.5f) Companion.Say(VictoryLines[Random.Range(0, VictoryLines.Length)], 3f);
            }

            e.InEncounter = false;
            CameraRig.Instance?.EndEncounter();
            Services.Audio.Music.ResumeExplore();
            if (Services.Audio.Music.State == MusicState.Combat) RegionDirector.PlayRegionMusic();
            p.Unlock(this);
            GameEvents.RaiseEncounter(false);
            Services.Save.Write();
            Busy = false;
        }

        static readonly string[] VictoryLines =
        {
            "Nice! You understood it so hard it stopped existing.",
            "That's what we call 'applied knowledge'. Also 'violence', but educational.",
            "Bug squashed. Well. Bug... understood to death.",
            "You're getting good at this. I'm slightly concerned.",
        };

        IEnumerator TutorialLossTalk(PlayerController p)
        {
            var box = Services.UI.Dialogue;
            yield return box.Say("aarav", "WHAT? I didn't even have time to read it!", "scared");
            yield return box.Say("pixel", "Yeah, that one was rigged. Everyone loses the first one.", "happy");
            yield return box.Say("pixel", "Here's the thing: in The Nexus, failing is SAFE. You don't lose. The game just... helps.");
            yield return box.Say("pixel", "Watch: it'll show you what went wrong. Then you hit back.");
            box.Show(false);
        }

        /// <summary>Stability hit zero: a short reboot + teaching demo, then the fight goes on (never a game over).</summary>
        public static IEnumerator FailureTeaching(PlayerController p, QuestionData q)
        {
            var box = Services.UI.Dialogue;
            GlitchFX.Pulse(1f, 0.8f);
            Services.UI.Banner("STABILITY CRITICAL", UIFactory.Theme.red, 90, 0.8f, "rebooting...");
            if (p.Visual.Expression != null) p.Visual.Expression.Play(ExpressionSequence.Impossible);
            yield return new WaitForSecondsRealtime(1f);
            yield return box.Say("pixel", "Whoa, whoa. Reboot. Let me show you instead of telling you. I never tell anyone anything.", "alarm");
            box.Show(false);
            if (TeachingMode.HasModule(q)) yield return TeachingMode.QuickDemo(q);
            p.RestoreFull();
            Services.UI.Toast("STABILITY RESTORED");
            Services.Audio.Play("heal");
        }
    }
}
