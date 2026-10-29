using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Waking up in The Nexus (end of the opening cinematic) and the first five minutes (Section 10.3):
    /// the companion teases movement and interaction, a jammed door with visibly broken logic, then the first
    /// Bug Monster, a fight designed to be lost once. None of it announces itself as a tutorial.
    /// </summary>
    public class CodeCityHubIntro : LevelScript
    {
        public override IEnumerator Run(RegionDirector director)
        {
            var d = Services.Game.Data;
            var p = PlayerController.Instance;
            if (d.HasFlag("cc_intro_done") || p == null) yield break;
            var pixel = Companion.Instance;
            var cam = CameraRig.Instance;

            CutscenePlayer.Begin();
            CutscenePlayer.AutoAdvance = 1f;   // the opening plays straight through, no input
            Services.Corruption.Set(1f);
            if (pixel != null) pixel.holdPosition = p.transform.position + new Vector3(18f, 9f, 0);
            // he wakes up face down
            p.Visual.Model.localRotation = Quaternion.Euler(-80, 120, 0);
            p.Visual.Expression?.SetExpression("defeated");
            yield return CutscenePlayer.Shot(p.transform.position + new Vector3(3, 1.2f, -5), p.transform.position + Vector3.up * 0.3f, 35, 0);
            GlitchFX.Pulse(1f, 1.5f);
            yield return CutscenePlayer.Wait(1.4f);
            Services.Audio.Play("boing");
            p.Visual.Expression?.SetExpression("surprised");
            for (float t = 0; t < 0.5f; t += Time.deltaTime) { p.Visual.Model.localRotation = Quaternion.Slerp(Quaternion.Euler(-80, 120, 0), Quaternion.Euler(0, 160, 0), t / 0.5f); yield return null; }
            yield return CutscenePlayer.Say("aarav", "Ow. My everything.", "flustered");
            // the reveal: floating buildings, NPCs frozen mid-sentence
            yield return CutscenePlayer.Shot(p.transform.position + new Vector3(10, 7, -26), p.transform.position + new Vector3(16, 6, 30), 46, 3f);
            yield return CutscenePlayer.Say("aarav", "Why is that building... floating?", "scared");
            yield return CutscenePlayer.Say("npc", "WELC0ME T0 C0DE C— WELC0ME T0 C0DE C— WELC0ME T0—");
            yield return CutscenePlayer.Say("aarav", "And why is that guy stuck on repeat?!", "sweating");
            // Pixel arrives
            yield return CutscenePlayer.Shot(p.transform.position + new Vector3(2, 2.4f, -9), p.transform.position + new Vector3(1, 1.6f, 0), 38, 1.2f);
            if (pixel != null) { pixel.holdPosition = null; Services.Audio.Play("powerup"); pixel.SetMood("happy", 3f); }
            yield return CutscenePlayer.Wait(0.9f);
            yield return CutscenePlayer.Say("pixel", "You're finally awake!", "happy");
            if (pixel != null)
                yield return CutscenePlayer.IntroCard(pixel.transform, new Vector3(0.6f, 0.1f, -1.3f), Vector3.zero,
                    "Pixel", "Your guide through The Nexus. Allegedly. Mostly harmless.", new Color(0.25f, 0.9f, 1f), 30f);
            yield return CutscenePlayer.Say("aarav", "Where am I?", "surprised");
            yield return CutscenePlayer.Say("pixel", "That's a very long story.", "bored");
            yield return CutscenePlayer.Say("aarav", "...Is it a long story, or do you just not want to tell me?", "smirk");
            yield return CutscenePlayer.Say("pixel", "Yes.", "happy");
            // the sky tears open
            Services.Audio.Play("sky_tear");
            Services.Audio.Play("boss_roar", 0.5f);
            var sky = RenderSettings.skybox;
            yield return CutscenePlayer.Shot(p.transform.position + new Vector3(0, 3, -14), p.transform.position + new Vector3(4, 22, 60), 50, 1.5f);
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { if (sky != null) sky.SetFloat("_Tears", 1f + Mathf.Sin(t / 1.6f * Mathf.PI) * 5f); if (CutscenePlayer.Skipping) break; yield return null; }
            if (sky != null) sky.SetFloat("_Tears", 1f);
            GlitchFX.Pulse(0.9f, 0.6f);
            cam?.Shake(1f);
            yield return CutscenePlayer.Say("pixel", "And we're running out of time. This whole place is called The Nexus. Something called The Glitch broke it.", "alarm");
            yield return CutscenePlayer.Say("aarav", "Cool cool cool. I was in the final lap twenty minutes ago.", "sweating");
            CutscenePlayer.End();
            d.SetFlag("cc_intro_done");
            p.Visual.Model.localRotation = Quaternion.Euler(0, CharacterVisual.YawRight, 0);
            p.Visual.Expression?.SetExpression("neutral");

            // ------------------------------------------------ the invisible tutorial
            yield return new WaitForSeconds(0.6f);
            Companion.Say(InputHub.IsMobile ? "Those are legs. Use the stick to make them do 'walking'." : "Those are legs. Press A and D to make them do 'walking'. Revolutionary, I know.", 5f);
            float startX = p.transform.position.x;
            float t0 = Time.time;
            while (Mathf.Abs(p.transform.position.x - startX) < 4f && Time.time - t0 < 20f) yield return null;
            Companion.Say(InputHub.IsMobile ? "Now JUMP. The big button. Hold it to jump higher." : "Now jump. Space. Hold it longer to jump higher. Gravity still works here. Mostly.", 5f);
            while (p.transform.position.x < 22f && Time.time - t0 < 60f) yield return null;
            Companion.Say("See that terminal? Walk up and press E to read it. Reading: it's like walking, but for your brain.", 5f);
            while (p.transform.position.x < 50f && Time.time - t0 < 120f) yield return null;
            if (!d.flags.Contains("door_cc_hub_gate")) Companion.Say("A jammed gate. The logic's scrambled. Press E and fix it. I'd help, but I'm decorative.", 5f);
        }
    }
}
