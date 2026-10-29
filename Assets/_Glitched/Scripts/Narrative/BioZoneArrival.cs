using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The Bio-Zone shrink scene: Pixel zaps Shaurya down to microscopic size, then hands over the SHRINK / GROW
    /// ability (shrink even further to fit through capillaries).
    /// </summary>
    public class BioZoneArrival : LevelScript
    {
        public override IEnumerator Run(RegionDirector director)
        {
            var d = Services.Game.Data;
            var p = PlayerController.Instance;
            if (p == null) yield break;
            if (d.HasFlag("arrived_BioZone")) { if (!Services.Game.HasAbility("shrink")) Services.Game.UnlockAbility("shrink"); yield break; }
            d.SetFlag("arrived_BioZone");
            CutscenePlayer.Begin();
            Vector3 c = p.transform.position;
            yield return CutscenePlayer.Shot(c + new Vector3(2, 2.4f, -9), c + Vector3.up * 1.4f, 38, 0);
            p.transform.localScale = Vector3.one * 2.6f;
            yield return CutscenePlayer.Say("pixel", "Okay. The Lord Within is inside a living organism. So we need to go inside too.", "normal");
            yield return CutscenePlayer.Say("aarav", "Inside... how? Through the mouth? Please don't say the mouth.", "scared");
            yield return CutscenePlayer.Say("pixel", "Hold still.", "alarm");
            Services.Audio.Play("shrink");
            GlitchFX.Pulse(1f, 0.8f);
            VFX.Burst(p.transform.position + Vector3.up, UIFactory.Theme.green, 60, "magic", 8f);
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { p.transform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, Easing.Evaluate(Ease.OutBounce, t / 1.2f)); yield return null; }
            p.transform.localScale = Vector3.one;
            yield return CutscenePlayer.Shot(c + new Vector3(10, 10, -32), c + new Vector3(20, 6, 30), 48, 2.5f);
            yield return CutscenePlayer.Say("aarav", "I'm... a germ. I'm germ-sized. My mum is going to be SO disappointed.", "melting");
            yield return CutscenePlayer.Say("pixel", "You're about the size of a red blood cell. Try not to get swept into a kidney.", "happy");
            Services.Game.UnlockAbility("shrink");
            Services.UI.Banner("NEW ABILITY", UIFactory.Theme.green, 100, 1.6f, "SHRINK / GROW   (Q  or  SIZE)");
            yield return CutscenePlayer.Say("pixel", "I've left the shrink ray on 'go even smaller' too. Press Q to squeeze through capillaries. Press it again to grow back.", "happy");
            CutscenePlayer.End();
            foreach (var line in director.Arc != null ? director.Arc.arrivalLines : new System.Collections.Generic.List<string>()) { Companion.Say(line, 4f); break; }
        }
    }
}
