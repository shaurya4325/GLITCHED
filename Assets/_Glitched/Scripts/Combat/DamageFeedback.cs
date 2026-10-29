using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The game feel of every result (Section 6.1 + design doc 13):
    /// ATTACK (spark burst, flash), CRITICAL (camera punch-in, shake, 0.15 s slow-mo, big hit), COMBO xN,
    /// ULTIMATE (full-screen effect, music drops out for a beat, then BOOM), ENEMY TURN (glitch distortion, shake).
    /// </summary>
    public static class DamageFeedback
    {
        public static float ComboMultiplier(int combo) => Mathf.Min(2.5f, 1f + 0.25f * Mathf.Max(0, combo - 1));

        public static void Attack(Enemy e, int dmg, PlayerController p)
        {
            e.TakeHit(dmg, false);
            Services.Audio.Play("hit");
            Services.UI.DamageNumber(e.transform.position + Vector3.up * (e.data.height + 0.4f), "-" + dmg, UIFactory.Theme.cyan, 64);
            Services.UI.Banner("ATTACK!", UIFactory.Theme.cyan, 90, 0.45f);
            CameraRig.Instance?.Shake(0.25f);
            PostFXController.Kick(0.25f);
            p.Visual.Expression?.Play(ExpressionSequence.Correct);
            p.Visual.Trigger("Cast");
            Beam(p, e, UIFactory.Theme.cyan);
        }

        public static void Critical(Enemy e, int dmg, PlayerController p)
        {
            TimeController.SlowMo(0.12f, 0.15f);
            CameraRig.Instance?.PunchIn(8f);
            CameraRig.Instance?.Shake(1.0f);
            e.TakeHit(dmg, true);
            Services.Audio.Play("crit");
            Services.UI.DamageNumber(e.transform.position + Vector3.up * (e.data.height + 0.5f), "-" + dmg + "!", UIFactory.Theme.yellow, 96);
            Services.UI.Banner("CRITICAL!", UIFactory.Theme.yellow, 130, 0.6f);
            PostFXController.Kick(0.8f);
            GlitchFX.Pulse(0.25f, 0.2f);
            p.Visual.Expression?.Play(ExpressionSequence.Correct);
            p.Visual.Trigger("Cast");
            Beam(p, e, UIFactory.Theme.yellow);
        }

        public static void Combo(int combo)
        {
            if (combo < 2) return;
            Services.Audio.Play("combo", 1f, 1f + Mathf.Min(combo, 8) * 0.05f);
            Services.Achievements.SetMax("max_combo", combo);
        }

        public static IEnumerator Ultimate(Enemy e, PlayerController p)
        {
            Services.Audio.Music.DropOut(0.55f);
            Services.UI.Banner("ULTIMATE", UIFactory.Theme.magenta, 170, 1.0f, "PERFECT SEQUENCE");
            TimeController.SlowMo(0.2f, 0.6f);
            CameraRig.Instance?.PunchIn(12f);
            GlitchFX.Pulse(0.9f, 0.6f);
            if (p.Visual.Expression != null) p.Visual.Expression.Play(ExpressionSequence.Victory);
            p.Visual.Trigger("Victory");
            yield return new WaitForSecondsRealtime(0.55f);
            CameraRig.Instance?.Shake(2f);
            VFX.Explosion(e.transform.position + Vector3.up * e.data.height * 0.5f, UIFactory.Theme.magenta, 1.4f);
            VFX.Confetti(p.transform.position + Vector3.up);
            PostFXController.Kick(1f);
        }

        public static IEnumerator EnemyTurn(Enemy e, PlayerController p)
        {
            GlitchFX.Pulse(0.7f, 0.5f);
            Services.Audio.Play("comedy_fail");
            if (p.Visual.Expression != null) p.Visual.Expression.Play(ExpressionSequence.Wrong);
            Services.UI.Banner("ENEMY TURN", UIFactory.Theme.red, 100, 0.5f);
            yield return e.Attack(p);
            PostFXController.Kick(0.6f);
        }

        /// <summary>Knowledge-beam from the player to the enemy (the "attack" is understanding).</summary>
        static void Beam(PlayerController p, Enemy e, Color c)
        {
            Vector3 a = p.transform.position + Vector3.up * 1.2f, b = e.transform.position + Vector3.up * e.data.height * 0.6f;
            for (int i = 0; i <= 8; i++)
            {
                float k = i / 8f;
                Vector3 pt = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                float delay = k * 0.12f;
                Tween.After(delay, () => VFX.Burst(pt, c, 4, "spark", 1.5f, 0.35f, 0.3f));
            }
        }
    }
}
