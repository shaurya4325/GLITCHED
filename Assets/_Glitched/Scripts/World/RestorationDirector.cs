using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// World restoration (Section 3.3): about 15 seconds, played rather than watched (the player keeps control).
    /// 1 THE BREAK (boss glitches apart) -> 2 THE DRAIN (corruption drains as a wave moving across the city along X)
    /// -> 3 THE LIGHT (colour returns to the sky) -> 4 THE WAKING (frozen NPCs move and talk) -> 5 REWARDS.
    /// </summary>
    public class RestorationDirector : MonoBehaviour
    {
        public bool Playing { get; private set; }
        static readonly int WaveX = Shader.PropertyToID("_RestoreWaveX");
        static readonly int WaveActive = Shader.PropertyToID("_RestoreWaveActive");

        public IEnumerator Play(ArcDataSO arc, bool debug = false)
        {
            if (Playing) yield break;
            Playing = true;
            GameEvents.RaiseRestorationStarted();
            var rd = RegionDirector.Current;
            float minX = rd != null ? rd.minX : -40, maxX = rd != null ? rd.maxX : 200;
            var p = PlayerController.Instance;
            if (p != null) p.ClearLocks();
            Services.UI.Hud.SetObjective("Watch the region heal around you.");

            // 1 THE BREAK
            Services.UI.Banner("THE BREAK", UIFactory.Theme.magenta, 80, 1.2f);
            GlitchFX.Pulse(1f, 1.2f);
            CameraRig.Instance?.Shake(1.5f);
            Services.Audio.Play("glitch_big");
            yield return new WaitForSeconds(1.8f);

            // 2 THE DRAIN: a wave of healing sweeps along X from the player outward
            Services.Audio.Music.Restoration();
            Services.UI.Banner("THE DRAIN", UIFactory.Theme.cyan, 80, 1.2f);
            Shader.SetGlobalFloat(WaveActive, 1f);
            Shader.SetGlobalFloat("_RestoreWaveWidth", 14f);
            float start = minX - 40, end = maxX + 60;
            for (float t = 0; t < 6.5f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 6.5f);
                Shader.SetGlobalFloat(WaveX, Mathf.Lerp(start, end, k));
                if (Random.value < Time.deltaTime * 6f) Services.Audio.Play("restore_tick", 0.4f, 0.8f + k * 0.6f);
                yield return null;
            }

            // 3 THE LIGHT: colour returns to the sky
            Services.UI.Banner("THE LIGHT", UIFactory.Theme.yellow, 80, 1.2f);
            Services.Audio.Play("restore");
            yield return Services.Corruption.TweenTo(0f, 2.5f);
            Shader.SetGlobalFloat(WaveActive, 0f);

            // 4 THE WAKING
            Services.UI.Banner("THE WAKING", UIFactory.Theme.green, 80, 1.2f);
            if (arc != null && arc.restoredLines.Count > 0) Companion.Say(arc.restoredLines[0], 4f);
            if (p != null && p.Visual.Expression != null) p.Visual.Expression.Play(ExpressionSequence.Victory);
            if (p != null) VFX.Confetti(p.transform.position + Vector3.up * 2f);
            yield return new WaitForSeconds(3f);

            // 5 REWARDS
            if (!debug && arc != null)
            {
                bool hadAbility = Services.Game.HasAbility(arc.abilityReward);
                Services.Game.MarkRestored(arc);
                Services.Audio.Music.Victory();
                yield return RewardsPanel.Show(arc, !hadAbility);
            }
            GameEvents.RaiseRestorationFinished();
            Playing = false;
            if (!debug && arc != null)
            {
                if (arc.id == "TheCore") Services.Scenes.Load(Services.Game.Data.HasFlag("kanha_defeated") ? "Ending" : "Kanha_Final");   // the real final exam
                else OpenChapterPortal(arc, p);
            }
            else RegionDirector.PlayRegionMusic();
        }

        /// <summary>
        /// The story chain: Code City -> Physics Valley -> Chemical Chaos -> Bio-Zone, each through a portal that
        /// opens in the boss arena; after the Bio-Zone a red rift opens into Kanha's Examination Hall.
        /// </summary>
        static void OpenChapterPortal(ArcDataSO arc, PlayerController p)
        {
            var next = Services.Db.arcs.Find(a => a != null && a.order == arc.order + 1);
            bool exam = arc.id == "BioZone" && !Services.Game.Data.HasFlag("kanha_defeated");
            string scene = exam ? "Kanha_Final" : next != null && next.Hub != null ? next.Hub.scene : null;
            if (scene == null || p == null) { Services.Scenes.Load(arc.Hub != null ? arc.Hub.scene : "WorldMap", "fromBoss"); return; }
            var rd = RegionDirector.Current;
            float x = p.transform.position.x + 7f * (p.Visual != null && !p.Visual.FacingRight ? -1f : 1f);
            if (rd != null) x = Mathf.Clamp(x, rd.minX + 3f, rd.maxX - 3f);
            float y = p.transform.position.y;
            if (Physics.Raycast(new Vector3(x, y + 6f, 0f), Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
            string dest = exam ? "THE EXAMINATION HALL" : next.displayName.ToUpperInvariant();
            Color col = exam ? new Color(1f, 0.22f, 0.32f) : LessonCard.SubjectColor(next.subject);
            ChapterPortal.Open(new Vector3(x, y, 0f), scene, dest, col, exam);
            RegionDirector.PlayRegionMusic();
            Services.UI.Hud.SetObjective(exam ? "A rift to the Examination Hall has opened. Step in. Bring your pen." : "A portal to " + next.displayName + " has opened. Step in!");
            Companion.Say(exam ? "That rift... it smells like exam paper. Whoever made the Glitch is on the other side." : "A portal! " + next.displayName + " is next. The Glitch went that way.", 4f, exam ? "alarm" : "happy");
        }
    }
}
