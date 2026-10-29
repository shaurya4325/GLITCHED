using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A Glitch Lord (5 phases: Recall -> Analysis -> Reasoning -> Application -> Synthesis) or a mini-boss (3 phases).
    /// The final test IS the health bar. Wrong answers make the boss regenerate. Failure is the teaching system:
    /// defeat 1 unlocks a HINT (the villain mocks the exact mistake), defeat 2 pauses the fight for INTERACTIVE
    /// TEACHING MODE, then a REMATCH on the same concept with a new problem that is guaranteed to be a CRITICAL.
    /// After the final attack: a 3-question mastery check, then the region restores itself.
    /// </summary>
    public class BossFight : MonoBehaviour
    {
        public Enemy boss;
        public bool isMiniBoss;
        public Transform playerMark;
        public string afterScene = "";
        bool started, finished;
        static readonly string[] CoreOrder = { "CodeCity", "PhysicsValley", "ChemicalChaos", "BioZone", "TheCore" };

        BossDataSO Data => boss != null ? boss.data as BossDataSO : null;

        void Start()
        {
            var box = gameObject.GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            if (box.size == Vector3.one) { box.size = new Vector3(3, 8, 4); box.center = new Vector3(0, 4, 0); }
            var rb = gameObject.GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var arc = Services.Game.CurrentArc;
            if (arc != null && Data != null)
            {
                var prog = Services.Game.Data.Arc(arc.id);
                bool beaten = isMiniBoss ? prog.miniBossDefeated : prog.bossDefeated;
                if (beaten) { boss.gameObject.SetActive(false); finished = true; }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (started || finished || !other.CompareTag("Player") || boss == null || Data == null) return;
            started = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var p = PlayerController.Instance;
            var data = Data;
            var arc = Services.Game.CurrentArc;
            string arcId = arc != null ? arc.id : data.arc;
            var box = Services.UI.Dialogue;
            var enc = EncounterManager.Instance;
            p.Lock(this);
            FpsMonitor.SuppressPrompts = true;
            boss.InEncounter = true;
            CameraRig.Instance?.FrameEncounter(boss.transform);
            CameraRig.Instance?.SetExtraDistance(isMiniBoss ? 3f : 6f);
            if (playerMark != null) { p.Teleport(playerMark.position); CameraRig.Instance?.FrameEncounter(boss.transform); }
            p.Visual.Face(1);
            Services.Audio.Music.Boss(1);
            Services.Audio.Play("boss_roar");
            CameraRig.Instance?.Shake(1.2f);
            GlitchFX.Pulse(0.7f, 0.6f);

            // intro
            Services.UI.Banner(data.displayName, UIFactory.Theme.magenta, 110, 1.6f, isMiniBoss ? "MINI-BOSS" : "GLITCH LORD");
            yield return new WaitForSecondsRealtime(1.2f);
            box.ClearSkip();
            foreach (var line in data.introLines) yield return box.Say(data.profile != null ? data.profile.id : data.displayName, line);
            yield return box.Say("aarav", isMiniBoss ? "Great. A boss. Before the BOSS boss." : "Okay. Okay okay okay. Knowledge is my weapon. Knowledge is my weapon.", "sweating");
            box.Show(false);
            Services.UI.Hud.ShowBoss(data.displayName, data.maxHP, data.maxHP);
            boss.ShowBar(false);

            int phase = 0, defeats = 0, wrongs = 0, fastCorrect = 0;
            bool hintUnlocked = false, rematchCrit = false;
            QuestionData failedQ = null, lastQ = null;
            enc.ResetCombo();
            var phases = data.phases;

            while (phase < phases.Count)
            {
                var ph = phases[phase];
                bool final = phase == phases.Count - 1;
                if (!isMiniBoss) Services.Audio.Music.Boss(phase + 1);
                Services.UI.Banner("PHASE " + ph.name, UIFactory.Theme.cyan, 70, 0.9f, final ? "THE FINAL TEST IS THE HEALTH BAR" : null);
                yield return new WaitForSecondsRealtime(0.7f);

                // choose the question
                QuestionData q;
                string pickArc = arcId;
                if (arcId == "TheCore") pickArc = CoreOrder[Mathf.Clamp(phase, 0, CoreOrder.Length - 1)];
                if (rematchCrit && failedQ != null)
                    q = Services.Selector.Pick(failedQ.arc, failedQ.concept, Mathf.Max(1, failedQ.difficulty - 1), Mathf.Min(5, failedQ.difficulty + 1), null, failedQ.id, null, false);
                else
                {
                    string concept = !string.IsNullOrEmpty(ph.concept) ? ph.concept : (phase % 2 == 0 && pickArc == arcId ? data.bossConcept : "");
                    if (arcId == "TheCore" && pickArc != "TheCore") concept = "";
                    if (concept == "Synthesis") concept = "";
                    q = Services.Selector.Pick(pickArc, concept, ph.difficulty, ph.difficulty, null, lastQ != null ? lastQ.id : null, ph.types, false);
                    if (q == null) q = Services.Selector.Pick(pickArc, "", ph.difficulty, ph.difficulty, null, null, ph.types, false);
                }
                if (q == null) { Debug.LogWarning("[Boss] no question for phase " + phase); phase++; continue; }
                lastQ = q;
                string attack = boss.RandomOf(data.attackNames);
                Services.UI.Log.Add($"<color=#ff3dc8>{data.displayName}</color> uses <b>{attack}</b>.");
                if (rematchCrit) { yield return box.Say(data.profile != null ? data.profile.id : data.displayName, data.rematchLine); box.Show(false); }

                ChallengeOutcome o = default; bool done = false;
                ChallengeEngine.Run(q, "", 0, r => { o = r; done = true; }, new ChallengeEngine.Options { enemyName = data.displayName, attackName = attack, showHint = hintUnlocked });
                while (!done) yield return null;
                Services.Mastery.Record(q, o.correct, o.fast);

                if (o.correct)
                {
                    bool crit = o.fast || rematchCrit;
                    if (crit) fastCorrect++;
                    rematchCrit = false;
                    enc.AddCombo();
                    int dmg;
                    if (final)
                    {
                        dmg = boss.HP;
                        Services.UI.Log.Add($"CORRECT in {o.seconds:0.0}s: <color=#ffd640>FINAL ATTACK</color>");
                        yield return FinalAttack(p);
                    }
                    else
                    {
                        dmg = Mathf.RoundToInt(ph.damage * (crit ? 1.6f : 1f) * Mathf.Lerp(1f, DamageFeedback.ComboMultiplier(enc.Combo), 0.35f));
                        dmg = Mathf.Min(dmg, boss.HP - 10 * (phases.Count - 1 - phase));
                        Services.UI.Log.Add($"CORRECT in {o.seconds:0.0}s, " + (crit ? "<color=#ffd640>CRITICAL</color>" : "ATTACK") + $" -{dmg} HP");
                        if (crit) DamageFeedback.Critical(boss, dmg, p); else DamageFeedback.Attack(boss, dmg, p);
                    }
                    if (!final) boss.Say(boss.RandomOf(data.hitLines.Count > 0 ? data.hitLines : data.taunts));
                    Services.UI.Hud.UpdateBoss(boss.HP, data.maxHP);
                    Services.Game.AddXP(30 * q.difficulty);
                    phase++;
                    yield return new WaitForSecondsRealtime(1.1f);
                }
                else
                {
                    wrongs++;
                    failedQ = q;
                    enc.ResetCombo();
                    boss.Heal(data.regenOnWrong);
                    Services.UI.Hud.UpdateBoss(boss.HP, data.maxHP);
                    Services.UI.Log.Add("WRONG. <color=#7cffa0>BOSS REGENERATES</color> +" + data.regenOnWrong);
                    Services.Audio.Play("boss_regen");
                    boss.Say(boss.RandomOf(data.taunts));
                    yield return DamageFeedback.EnemyTurn(boss, p);
                    yield return ChallengeEngine.ShowExplanation(q, o, "ENEMY TURN: THE BOSS REGENERATES");

                    if (p.Stability <= 0)
                    {
                        defeats++;
                        if (arc != null) Services.Game.Data.Arc(arc.id).bossDefeats++;
                        Services.UI.Banner("DEFEATED", UIFactory.Theme.red, 120, 1f, defeats == 1 ? "HINT UNLOCKED" : "TEACHING MODE");
                        p.Visual.Trigger("Defeat");
                        yield return new WaitForSecondsRealtime(1.2f);
                        if (defeats == 1)
                        {
                            hintUnlocked = true;
                            string sp = data.profile != null ? data.profile.id : data.displayName;
                            yield return box.Say(sp, data.hintTaunts.Count > 0 ? data.hintTaunts[0] : "Look at your mistake. LOOK at it.");
                            if (!string.IsNullOrEmpty(q.hint)) yield return box.Say(sp, $"HINT, since you're SO slow: \"{q.hint}\"");
                            yield return box.Say("pixel", "He just told us the answer's weak spot. Villains love to talk. RETRY!", "happy");
                            box.Show(false);
                        }
                        else
                        {
                            string sp = data.profile != null ? data.profile.id : data.displayName;
                            yield return box.Say(sp, "STOP. Stop stop stop. This is embarrassing for BOTH of us.");
                            box.Show(false);
                            yield return TeachingMode.RunForBoss(q, data);
                            rematchCrit = true;
                            if (!isMiniBoss) Services.Audio.Music.Boss(phase + 1);
                        }
                        p.RestoreFull();
                    }
                }
            }

            // ---------------- defeat of the boss
            Services.UI.Hud.HideBoss();
            yield return box.Say(data.profile != null ? data.profile.id : data.displayName, data.finalDefeatLine);
            box.Show(false);
            if (wrongs == 0) Services.Achievements.Add("perfect_boss");
            if (defeats >= 2) Services.Achievements.Add("never_give_up");
            yield return boss.Defeat();
            if (arc != null) Services.Game.MarkBossDefeated(arc, isMiniBoss);
            Services.Game.AddXP(data.rewardXP);
            Services.Game.AddCoins(data.rewardCoins);
            enc.ResetCombo();
            CameraRig.Instance?.EndEncounter();
            CameraRig.Instance?.SetExtraDistance(0);

            // ---------------- mastery check (bosses), ability (mini-bosses)
            if (!isMiniBoss) yield return MasteryCheck.Run(arcId == "TheCore" ? "*" : arcId, data.bossConcept, arc);
            if (isMiniBoss && arc != null && !string.IsNullOrEmpty(arc.abilityReward) && !Services.Game.HasAbility(arc.abilityReward))
            {
                Services.Game.UnlockAbility(arc.abilityReward);
                Services.Audio.Play("powerup");
                Services.UI.Banner("NEW ABILITY", UIFactory.Theme.green, 100, 1.6f, GameManager.AbilityName(arc.abilityReward));
                yield return new WaitForSecondsRealtime(1.5f);
                yield return box.Say("pixel", AbilityLine(arc.abilityReward), "happy");
                box.Show(false);
            }

            p.Unlock(this);
            FpsMonitor.SuppressPrompts = false;
            boss.InEncounter = false;
            finished = true;
            Services.Save.Write();

            if (!isMiniBoss)
            {
                var director = FindFirstObjectByType<RestorationDirector>();
                if (director == null) director = new GameObject("RestorationDirector").AddComponent<RestorationDirector>();
                yield return director.Play(arc);
            }
            else
            {
                RegionDirector.PlayRegionMusic();
                Services.UI.Hud.SetObjective("Find the exit to the next area.");
            }
        }

        IEnumerator FinalAttack(PlayerController p)
        {
            Services.Audio.Music.DropOut(0.7f);
            TimeController.SlowMo(0.15f, 0.8f);
            CameraRig.Instance?.PunchIn(14f);
            GlitchFX.Pulse(1f, 0.9f);
            Services.UI.Banner("FINAL ATTACK", UIFactory.Theme.yellow, 160, 1.2f);
            if (p.Visual.Expression != null) p.Visual.Expression.Play(ExpressionSequence.Victory);
            p.Visual.Trigger("Victory");
            yield return new WaitForSecondsRealtime(0.7f);
            boss.TakeHit(boss.HP, true);
            Services.Audio.Play("explosion");
            VFX.Explosion(boss.transform.position + Vector3.up * boss.data.height * 0.5f, UIFactory.Theme.yellow, 2f);
            // the boss blows apart in a chain of blasts
            var style = Blast.StyleFor(boss.data.arc);
            Blast.At(boss.transform.position + Vector3.up * boss.data.height * 0.5f, Mathf.Clamp(boss.data.height * 0.8f, 2.5f, 5f), style, true, true);
            Services.UI.StartCoroutine(Blast.Chain(boss.transform.position + Vector3.up * boss.data.height * 0.5f, new Vector3(boss.data.height * 0.4f, boss.data.height * 0.4f, 0.5f), isMiniBoss ? 4 : 7, 0.18f, 1.8f, style));
            CameraRig.Instance?.Shake(2.5f);
            PostFXController.Kick(1f);
        }

        static string AbilityLine(string id)
        {
            switch (id)
            {
                case "double_jump": return "RECURSIVE LEAP! Jump, then jump again while in the air. A jump that calls itself. Very loop-y.";
                case "dash": return "MOMENTUM DASH! Press Shift (or DASH) to zoom. An object in motion stays in motion. Until a wall.";
                case "wall_jump": return "BOND GRIP! Slide down walls and jump off them. You're bonding with walls now. Covalently.";
                case "shrink": return "SHRINK! Press Q (or SIZE) to get tiny and fit through small gaps.";
                default: return "New ability unlocked!";
            }
        }
    }
}
