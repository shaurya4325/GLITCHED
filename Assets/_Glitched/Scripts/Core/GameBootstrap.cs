using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Boot scene: makes sure the services exist, shows the splash, verifies this copy with the stall server (release
    /// builds), runs the 3-second benchmark on first launch, then loads the title screen.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        IEnumerator Start()
        {
            Services.Boot();
            PauseMenu.Allowed = false;
            Services.UI.Hud.Show(false);
            var cam = new GameObject("BootCamera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.025f);
            while (!Services.Questions.Loaded) yield return null;

            // splash
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("Splash", Services.UI.SceneLayer));
            var bg = UIFactory.Image(root, new Color(0.01f, 0.01f, 0.025f), null, "Bg");
            UIFactory.Stretch(bg.rectTransform);
            var school = UIFactory.Text(root, "DAV PUBLIC SCHOOL\n<size=55%>DEFENCE COLONY, DEHRADUN</size>", 58, th.text, TextAlignmentOptions.Center, true);
            UIFactory.Place(school.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(1600, 200));
            var presents = UIFactory.Text(root, "presents a project for the CBSE Skill Expo & Guidance Festival 2026-27", 26, th.textDim, TextAlignmentOptions.Center);
            UIFactory.Place(presents.rectTransform, new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(1600, 60));
            var g = school.gameObject.AddComponent<GlitchText>();
            g.intensityOverride = 0.5f; g.charsPerSecond = 60;
            g.Show(school.text);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            yield return Tween.Fade(cg, 1, 0.6f);
            Services.Audio.Play("glitch");
            yield return new WaitForSecondsRealtime(2.2f);
            g.intensityOverride = 0f;
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Tween.Fade(cg, 0, 0.5f);
            Destroy(root.gameObject);

            yield return Services.Licence.WaitGranted();   // copy verification (release builds)

            string next = Services.Settings.Data.tierChosen ? "Title" : "Benchmark";
            Services.Scenes.Load(Application.CanStreamedLevelBeLoaded(next) ? next : "Title");
        }
    }
}
