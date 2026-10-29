using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The opening cinematic (plays by itself, no input): late at night Shaurya is gaming on his PC, final lap.
    /// 1) the room, the race, his intro card; 2) the game lags, glitches, ERROR; 3) he recoils; 4) three hits crack
    /// the monitor from inside, the screen SHATTERS into real glass and The Glitch bursts into the room; it
    /// scatters onto every surface and corruption veins spread through the room; 5) the room levitates;
    /// 6) the Glitch tears itself off the walls into a whirlpool at the broken screen and pulls him in. BOOM.
    /// </summary>
    public class OpeningCutscene : MonoBehaviour
    {
        IEnumerator Start()
        {
            PauseMenu.Allowed = false;
            var db = Services.Db;
            var room = RoomBuilder.Build(db);
            Services.Corruption.SetupRegion(ContentDefaults.CreateTheme("Room"), RenderSettings.sun, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.18f, 0.26f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.12f, 0.17f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.06f);
            RenderSettings.fog = false;

            Vector3 sit = room.SitRoot;   // captured: the chair floats away later
            var p = PlayerController.Spawn(sit);
            p.CC.enabled = false;
            var lockZ = p.GetComponent<PlaneLock>();
            if (lockZ != null) lockZ.enabled = false;     // he sits at the desk, off the gameplay plane
            p.transform.position = sit;
            p.Visual.FaceYaw(0);
            p.Visual.SetSeated(true, room.seat.position.y - sit.y);
            if (!p.Visual.PlayState("Typing", 0f)) p.Visual.SetSitting(true);   // gaming at the keyboard
            CameraRig.Create(p.transform);
            Services.UI.Hud.Show(false);
            Services.Audio.Music.Cinematic("opening_room");
            CutscenePlayer.Begin();
            CutscenePlayer.AutoAdvance = 1f;   // plays by itself: no clicks, no keys
            p.Lock(this);
            bool expo = Services.Game.IsExpo;
            var fx = room.screenFx;
            Vector3 desk = room.monitor.position;
            Vector3 face = room.seat.position + new Vector3(0, 0.6f, 0.02f);   // leaning in at the keyboard

            // 1. The room at night, lit by the screen and the RGB. The race.
            yield return CutscenePlayer.Shot(new Vector3(-3.6f, 2.4f, -2.6f), desk + Vector3.down * 0.4f, 50, 0);
            yield return CutscenePlayer.Shot(new Vector3(-2.0f, 1.9f, -0.9f), desk + Vector3.down * 0.3f, 44, 3.5f);
            yield return CutscenePlayer.Shot(room.seat.position + new Vector3(-0.55f, 1.15f, -0.6f), desk, 40, 1.2f);   // over the shoulder: the race
            yield return CutscenePlayer.Say("aarav", "Final lap. Final lap. Come on, come on, come on...", "smirk");
            yield return CutscenePlayer.IntroCard(p.transform, new Vector3(0.55f, 0.05f, 0.75f), new Vector3(0, 1.02f, 0.05f),
                "Shaurya", "Class 11. Gamer. 0.3 seconds away from a new lap record.", new Color(0.25f, 0.55f, 1f));

            // 2. Lag. Then the glitch.
            yield return CutscenePlayer.Shot(desk + new Vector3(-0.25f, 0.05f, -1.25f), desk, 34, 0.6f);
            Services.Audio.Play("glitch");
            for (float t = 0; t < 1.2f; t += Time.deltaTime) { fx.glitch = Random.value < 0.25f ? Random.Range(0.2f, 0.5f) : 0.05f; yield return null; }
            fx.glitch = 0.08f;
            yield return CutscenePlayer.Say("aarav", "Lag? NOW?! My ping is twelve!", "flustered");
            if (!expo)
            {
                Services.Audio.Play("glitch");
                for (float t = 0; t < 1.4f; t += Time.deltaTime) { fx.glitch = Mathf.Lerp(0.2f, 0.85f, t / 1.4f) * (Random.value < 0.3f ? 1.2f : 0.7f); yield return null; }
            }
            Services.Audio.Music.Stop(0.2f);
            Services.Audio.Play("glitch_big");
            fx.glitch = 0.6f;
            fx.ShowImage(Resources.Load<Texture2D>("FX/error404"), 0.85f);
            foreach (var l in room.rgb) if (l != null) l.color = new Color(1f, 0.25f, 0.75f);
            GlitchFX.Pulse(0.45f, 0.5f);

            // 3. He recoils
            yield return CutscenePlayer.Shot(face + new Vector3(0.45f, 0.02f, 0.9f), face, 30, 0.25f, Ease.OutCubic);
            p.Visual.Expression?.SetExpression("scared");
            bool disbelief = p.Visual.PlayState("Sitting_Disbelief", 0.15f);
            float recoil = disbelief ? 0.25f : 0.7f;
            for (float t = 0; t < 0.25f; t += Time.deltaTime) { p.Visual.Recoil = Easing.Evaluate(Ease.OutBack, t / 0.25f) * recoil; yield return null; }
            yield return CutscenePlayer.Say("aarav", "That's... not part of the game.", "scared");

            // 4. Something hits the glass. From the inside. Three times.
            yield return CutscenePlayer.Shot(desk + new Vector3(-0.35f, 0.1f, -1.0f), desk, 38, 0.4f);
            float[] cracks = { 0.25f, 0.55f, 1f };
            for (int i = 0; i < cracks.Length; i++)
            {
                yield return CutscenePlayer.Wait(i == 0 ? 0.5f : 0.75f);
                Services.Audio.Play("boom");
                Services.Audio.Play("hit");
                fx.crack = cracks[i];
                fx.glitch = 0.5f + i * 0.2f;
                CameraRig.Instance?.Shake(0.5f + i * 0.3f);
                GlitchFX.Pulse(0.3f + i * 0.15f, 0.15f);
                foreach (var l in room.rgb) if (l != null) l.intensity *= 1.15f;
            }
            // the screen SHATTERS: real glass blows out into the room, in slow motion
            var center = room.screen.transform.position;
            Vector3 outward = (face - center); outward.y = 0f;
            Services.Audio.Play("explosion");
            Services.Audio.Play("glass_break");
            GlassShatter.At(room.screen, outward, 4.2f);
            room.screen.enabled = false;
            VFX.Burst(center, new Color(1f, 0.3f, 0.85f), 70, "spark", 8f);
            VFX.Burst(center, new Color(0.3f, 0.9f, 1f), 50, "spark", 6f);
            fx.image = 0f; fx.glitch = 1f;
            var flash = new GameObject("ShatterFlash").AddComponent<Light>();
            flash.transform.position = center + outward.normalized * 0.4f;
            flash.type = LightType.Point; flash.range = 7f; flash.intensity = 14f; flash.color = new Color(1f, 0.45f, 0.95f);
            Destroy(flash.gameObject, 0.35f);
            CameraRig.Instance?.Shake(1.6f);
            GlitchFX.Pulse(0.9f, 0.35f);
            TimeController.SlowMo(0.25f, 0.9f);
            // and The Glitch is simply THERE: it bursts through the hole at full size
            var glitch = GlitchEntity.Create(room.root, center);
            glitch.jitter = 3f;
            p.Visual.PlayState("Sitting_Yell", 0.1f);
            Vector3 mid = Vector3.Lerp(center, face, 0.45f) + Vector3.up * 0.35f;
            yield return CutscenePlayer.Shot(desk + new Vector3(-1.5f, 0.35f, -2.1f), mid, 48, 0.12f, Ease.OutCubic);
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float k = Easing.Evaluate(Ease.OutCubic, t / 0.3f);
                glitch.transform.position = Vector3.Lerp(center, mid, k);
                glitch.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.4f, k);
                yield return null;
            }
            Services.Audio.Play("glitch_big");
            yield return CutscenePlayer.Wait(0.35f);

            // it SCATTERS: hundreds of pieces fly to every wall, the ceiling, the floor, the furniture, and stick
            Services.Audio.Play("boom");
            Services.Audio.BitCrushBurst(0.6f);
            CameraRig.Instance?.Shake(1.1f);
            GlitchFX.Pulse(0.7f, 0.3f);
            StartCoroutine(glitch.Scatter(room.root, p.transform, 1.1f));
            yield return CutscenePlayer.Shot(new Vector3(-0.4f, 2.1f, -3.0f), new Vector3(0.4f, 1.5f, 2.6f), 58, 0.9f, Ease.OutCubic);
            // the corruption spreads through the room: veins in the paint, lights failing, the colour draining
            Services.Audio.Music.Cinematic("puzzle");
            for (float t = 0; t < 2.6f; t += Time.deltaTime)
            {
                if (CutscenePlayer.Skipping) break;
                float k = t / 2.6f;
                Services.Corruption.Set(k * 0.45f);
                foreach (var l in room.rgb) if (l != null) l.intensity = Random.value < 0.12f ? 0.2f : Mathf.Lerp(l.intensity, 2.2f, Time.deltaTime * 4f);
                if (room.lamp != null) room.lamp.intensity = Random.value < 0.1f ? 0f : 1.4f * (1f - k * 0.6f);
                if (Random.value < Time.deltaTime * 3f) { GlitchFX.Pulse(0.2f + k * 0.2f, 0.1f); Services.Audio.BitCrushBurst(0.25f); }
                yield return null;
            }
            yield return CutscenePlayer.Say("aarav", "It's... in the walls. It's in EVERYTHING.", "scared");

            // 5. The room lifts off the floor; the Glitch reaches for him
            Services.Audio.Play("gravity_flip");
            yield return CutscenePlayer.Shot(new Vector3(0, 2.3f, -3.2f), new Vector3(0.8f, 1.3f, 2.5f), 52, 1.0f);
            float dur = expo ? 3f : 5.5f;
            p.Visual.PlayState("Floating", 0.6f);
            var starts = new System.Collections.Generic.List<Vector3>();
            foreach (var f in room.floaters) starts.Add(f.position);
            glitch.reachTarget = face;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                if (CutscenePlayer.Skipping) break;
                float k = t / dur;
                Services.Corruption.Set(0.45f + k * 0.55f);
                p.Visual.Recoil = Mathf.Lerp(0.3f, 0f, k);
                for (int i = 0; i < room.floaters.Count; i++)
                {
                    var f = room.floaters[i];
                    if (f == null) continue;
                    f.position = starts[i] + Vector3.up * (Mathf.Sin(k * Mathf.PI * 0.5f) * (0.8f + i % 3 * 0.5f)) + Vector3.right * Mathf.Sin(Time.time * (1 + i % 4) + i) * 0.1f * k;
                    f.Rotate(new Vector3(20 + i * 3, 30 - i * 2, 15) * Time.deltaTime * k, Space.World);
                }
                p.transform.position = sit + Vector3.up * Mathf.Sin(k * Mathf.PI * 0.5f) * 0.6f;
                var cam = CameraRig.Instance.CutsceneCam.transform;
                cam.rotation = Quaternion.LookRotation(new Vector3(0.8f, 1.3f, 2.5f) - cam.position) * Quaternion.Euler(0, 0, Mathf.Sin(t * 1.7f) * 8f * k);
                if (Random.value < Time.deltaTime * 2) { GlitchFX.Pulse(0.25f * k, 0.12f); Services.Audio.BitCrushBurst(0.3f); }
                if (t > 1.5f && t < 1.5f + Time.deltaTime * 1.5f) StartCoroutine(SayAsync("aarav", "Is my room... LEVITATING?! Is this an update?!", "sweating"));
                yield return null;
            }
            yield return CutscenePlayer.Wait(0.3f);

            // 6. The Glitch pulls him into the screen. BOOM. Hard cut to black.
            p.Visual.SetSitting(false);
            p.Visual.SetSeated(false);
            p.Visual.Recoil = 0f;
            p.Visual.Expression?.SetExpression("scared");
            Services.Audio.Play("transition");
            Vector3 from = p.transform.position;
            yield return CutscenePlayer.Shot(desk + new Vector3(-0.3f, 0.3f, -2.4f), desk, 44, 0.3f);
            Services.Audio.Play("whoosh");
            yield return glitch.Reform(desk, 0.9f);
            glitch.reach = 1f;
            glitch.reachTarget = p.transform.position + Vector3.up * 0.9f;
            for (float t = 0; t < 1.3f; t += Time.deltaTime)
            {
                float k = Easing.Evaluate(Ease.InCubic, t / 1.3f);
                p.transform.position = Vector3.Lerp(from, desk, k);
                p.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, k);
                p.transform.Rotate(0, 0, 400 * Time.deltaTime, Space.Self);
                glitch.transform.position = Vector3.Lerp(glitch.transform.position, desk, k * 0.1f);
                CameraRig.Instance.CutsceneCam.transform.position = Vector3.Lerp(desk + new Vector3(-0.3f, 0.3f, -2.4f), desk + new Vector3(0, 0, -0.25f), k);
                GlitchFX.Pulse(0.25f + k * 0.5f, 0.1f);
                yield return null;
            }
            Services.Audio.Play("boom");
            Services.UI.Fade.SetBlack(true);
            Services.UI.Banner("BOOM.", Color.white, 180, 0.8f);
            yield return new WaitForSecondsRealtime(1.4f);
            CutscenePlayer.End();
            Services.Corruption.Set(1f);
            Services.Game.Data.SetFlag("opening_seen");
            Services.Save.Write();
            if (lockZ != null) lockZ.enabled = true;
            Services.Scenes.Load("CodeCity_Hub", "default");
            yield return new WaitForSecondsRealtime(0.3f);
            Services.UI.Fade.SetBlack(false);
        }

        IEnumerator SayAsync(string who, string line, string exp)
        {
            Companion.Hide();
            yield return Services.UI.Dialogue.Say(who, line, exp, 2.4f);
            Services.UI.Dialogue.Show(false);
        }
    }
}
