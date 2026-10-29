using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>The ending: The Nexus is saved, a goodbye with Pixel, back in the bedroom, and the credits.</summary>
    public class EndingCutscene : MonoBehaviour
    {
        IEnumerator Start()
        {
            PauseMenu.Allowed = false;
            var room = RoomBuilder.Build(Services.Db);
            Services.Corruption.SetupRegion(ContentDefaults.CreateTheme("Room"), RenderSettings.sun, 0f);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.65f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.5f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);
            Vector3 sit = room.SitRoot;   // captured: the chair floats away later
            var p = PlayerController.Spawn(sit);
            p.CC.enabled = false;
            var lockZ = p.GetComponent<PlaneLock>();
            if (lockZ != null) lockZ.enabled = false;     // seated at the desk, off the gameplay plane
            p.transform.position = sit;
            p.Visual.FaceYaw(0);
            p.Visual.SetSeated(true, room.seat.position.y - sit.y);
            if (!p.Visual.PlayState("Sitting_Idle", 0f)) p.Visual.SetSitting(true);
            CameraRig.Create(p.transform);
            Services.UI.Hud.Show(false);
            CutscenePlayer.Begin();
            p.Lock(this);
            Services.Audio.Music.Cinematic("ending");
            Vector3 desk = room.monitor.position;
            yield return CutscenePlayer.Shot(new Vector3(-2.4f, 2.1f, -1.4f), desk + Vector3.down * 0.3f, 46, 0);
            Services.UI.Fade.SetBlack(false);
            yield return CutscenePlayer.Wait(1f);
            yield return CutscenePlayer.Say("aarav", "...I'm home. I'm HOME. The race is still paused. Was that a dream?", "surprised");
            yield return CutscenePlayer.Shot(desk + new Vector3(-0.5f, 0.1f, -1.5f), desk, 34, 2f);
            p.Visual.PlayState("Sitting_Thumbs_Up", 0.2f);
            yield return CutscenePlayer.Say("aarav", "And the city... the whole Nexus... I actually FIXED it.", "cool");
            room.screenFx.ShowImage(Resources.Load<Texture2D>("Faces/Pixel/eye_happy"), 1f);
            Services.Audio.Play("powerup");
            yield return CutscenePlayer.Say("pixel", "Psst. Nice loop.", "happy");
            yield return CutscenePlayer.Say("aarav", "PIXEL?! How are you in my computer?!", "scared");
            yield return CutscenePlayer.Say("pixel", "That's a very long story.", "bored");
            yield return CutscenePlayer.Say("pixel", "The Nexus is stable. Every region is healthy. Because you understood how it works.", "happy");
            yield return CutscenePlayer.Say("pixel", "Knowledge isn't something you collect. It's something you use. See you around, student.", "heart");
            yield return CutscenePlayer.Say("aarav", "...I'm definitely going to fail tomorrow's exam because I didn't sleep.", "melting");
            yield return CutscenePlayer.Say("pixel", "Nah. You've already passed the hard one.", "happy");
            CutscenePlayer.End();

            // credits roll
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("CreditsRoll", Services.UI.SceneLayer));
            UIFactory.Image(root, new Color(0, 0, 0, 0.75f), null, "Shade").rectTransform.anchorMax = Vector2.one;
            UIFactory.Stretch(root.GetChild(0) as RectTransform);
            var logo = GlitchLogo.Create(root, "GLITCHED", 150);
            var lrt = (RectTransform)logo.transform;
            UIFactory.Place(lrt, new Vector2(0.5f, 0), new Vector2(0, -300), new Vector2(1400, 220));
            var t = UIFactory.Text(root, CreditsScreen.Text, 34, th.text, TextAlignmentOptions.Top);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -600), new Vector2(1400, 2600), new Vector2(0.5f, 1));
            float speed = 70f;
            for (float time = 0; time < 50f; time += Time.unscaledDeltaTime)
            {
                lrt.anchoredPosition += Vector2.up * speed * Time.unscaledDeltaTime;
                t.rectTransform.anchoredPosition += Vector2.up * speed * Time.unscaledDeltaTime;
                if (Services.Input.AdvancePressed && time > 2f) break;
                yield return null;
            }
            Services.Save.Write();
            if (Services.Game.IsExpo) ExpoMode.EndDemo(); else Services.Scenes.Load("Title");
        }
    }
}
