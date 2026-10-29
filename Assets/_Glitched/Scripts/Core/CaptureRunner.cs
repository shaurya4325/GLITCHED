using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>
    /// Automated screenshots for testing builds:
    ///   GLITCHED.exe -capture CodeCity_L1,PhysicsValley_L1 -captureDir C:\shots -captureDelay 6 [-captureTier High] [-captureWalk 1] [-captureCorruption 0.5]
    /// Loads each scene, waits, saves a PNG, then quits.
    /// </summary>
    public class CaptureRunner : MonoBehaviour
    {
        public static bool Active { get; private set; }
        string[] scenes;
        string dir;
        float delay = 6f;
        float corruption = -1;
        int shotsPerScene = 1;

        public static void TryStart()
        {
            var args = System.Environment.GetCommandLineArgs();
            string list = Arg(args, "-capture");
            if (string.IsNullOrEmpty(list)) return;
            Active = true;
            var r = new GameObject("[CaptureRunner]").AddComponent<CaptureRunner>();
            DontDestroyOnLoad(r.gameObject);
            r.scenes = list.Split(',');
            r.dir = Arg(args, "-captureDir") ?? Path.Combine(Application.persistentDataPath, "captures");
            if (float.TryParse(Arg(args, "-captureDelay"), out var d)) r.delay = d;
            if (float.TryParse(Arg(args, "-captureCorruption"), out var c)) r.corruption = c;
            if (int.TryParse(Arg(args, "-captureShots"), out var n)) r.shotsPerScene = Mathf.Max(1, n);
            if (float.TryParse(Arg(args, "-captureWalk"), out var w)) PlayerController.AutoWalk = w;
            if (Arg(args, "-captureBot") == "1") r.gameObject.AddComponent<ParkourBot>();
            if (Arg(args, "-captureDev") == "1") DevMode.ForceOn = true;
            if (Arg(args, "-captureTools") == "1") ScratchTools.Demo = true;
            string tier = Arg(args, "-captureTier");
            if (!string.IsNullOrEmpty(tier) && System.Enum.TryParse<GraphicsTier>(tier, true, out var t))
            {
                Services.Settings.Data.tier = t; Services.Settings.Data.tierChosen = true;
                Services.Graphics.ApplyTier(t);
            }
            Services.Settings.Data.tierChosen = true;
            r.StartCoroutine(r.Run());
        }

        static string Arg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        IEnumerator Run()
        {
            Directory.CreateDirectory(dir);
            foreach (var dev in UnityEngine.InputSystem.InputSystem.devices) Debug.Log("[Capture] input device: " + dev.name + " (" + dev.layout + ")");
            // Captures must not be driven by stray input (a phantom Submit would press the selected button).
            UnityEngine.InputSystem.InputSystem.onActionChange += (o, change) =>
            {
                if (change == UnityEngine.InputSystem.InputActionChange.ActionPerformed && o is UnityEngine.InputSystem.InputAction a)
                    Debug.Log("[Capture] action " + a.name + " from " + a.activeControl?.path);
            };
            yield return new WaitForSecondsRealtime(1f);
            yield return Services.Licence.WaitGranted();   // a release build can't be played through capture mode unverified
            foreach (var s in scenes)
            {
                // "Scene@x" starts the player at x
                string scene = s; float startX = float.NaN;
                int at = s.IndexOf('@');
                if (at > 0) { scene = s.Substring(0, at); float.TryParse(s.Substring(at + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out startX); }
                Debug.Log("[Capture] loading " + scene);
                SceneManager.LoadScene(scene);
                yield return null;
                if (!float.IsNaN(startX))
                {
                    yield return new WaitForSecondsRealtime(1.5f);
                    var plan = LevelPlans.Get(scene);
                    if (PlayerController.Instance != null && plan != null) PlayerController.Instance.Teleport(new Vector3(startX, plan.GroundY(startX) + 0.2f, 0));
                }
                if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.enabled = false;
                float wait = delay;
                for (int i = 0; i < shotsPerScene; i++)
                {
                    float t = 0;
                    while (t < wait) { t += Time.unscaledDeltaTime; if (corruption >= 0 && t > 0.5f && t < 0.6f) Services.Corruption.Set(corruption); yield return null; }
                    yield return new WaitForEndOfFrame();
                    string path = Path.Combine(dir, $"{s.Replace('@', '_')}_{i}.png");
                    ScreenCapture.CaptureScreenshot(path);
                    Debug.Log("[Capture] saved " + path);
                    yield return new WaitForSecondsRealtime(0.4f);
                    wait = delay * 0.6f;
                }
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }
    }
}
