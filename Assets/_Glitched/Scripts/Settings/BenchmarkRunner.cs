using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The 3-second benchmark scene (first launch, or Settings -> Run benchmark again). Renders a busy corrupted
    /// city at the guessed tier, measures the average fps, refines the tier and saves it.
    /// </summary>
    public class BenchmarkRunner : MonoBehaviour
    {
        IEnumerator Start()
        {
            PauseMenu.Allowed = false;
            var s = Services.Settings.Data;
            var guess = DeviceProfiler.GuessTier();
            Services.Graphics.ApplyTier(guess);
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("Benchmark", Services.UI.SceneLayer));
            var t = UIFactory.Text(root, "CALIBRATING YOUR DEVICE...", 46, th.cyan, TextAlignmentOptions.Center, true);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(1600, 80));
            t.gameObject.AddComponent<GlitchText>().intensityOverride = 0.4f;
            var info = UIFactory.Text(root, $"{SystemInfo.graphicsDeviceName}  ·  {SystemInfo.systemMemorySize} MB RAM  ·  testing {guess}", 24, th.textDim, TextAlignmentOptions.Center);
            UIFactory.Place(info.rectTransform, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(1600, 50));
            var bar = UIFactory.Bar(root, th.cyan, null, "Progress", 8);
            UIFactory.Place(bar.GetComponent<RectTransform>(), new Vector2(0.5f, 0.09f), Vector2.zero, new Vector2(800, 18));
            bar.Set(0, true);

            // the scene itself (city, traffic, particles) is baked by the builder; give it a moment to warm up
            Services.Corruption.SetupRegion(Services.Db.Arc("CodeCity")?.theme, RenderSettings.sun, 1f);
            var cam = FindFirstObjectByType<Camera>();
            if (cam != null) Services.Graphics.ConfigureCamera(cam);
            yield return new WaitForSecondsRealtime(0.6f);
            int frames = 0; float time = 0;
            while (time < 3f)
            {
                float dt = Time.unscaledDeltaTime;
                time += dt; frames++;
                bar.Set(time / 3f, true);
                if (cam != null) cam.transform.position += Vector3.right * dt * 6f;
                yield return null;
            }
            float avg = frames / Mathf.Max(0.01f, time);
            var tier = DeviceProfiler.Refine(guess, avg);
            s.tierChosen = true;
            s.renderScale = -1;
            Services.Graphics.ApplyTier(tier);
            Services.Settings.Apply();
            t.text = $"{avg:0} FPS  ·  QUALITY: {tier.ToString().ToUpper()}";
            info.text = "You can change this any time in Settings -> Graphics.";
            yield return new WaitForSecondsRealtime(1.6f);
            Services.Scenes.Load("Title");
        }
    }
}
