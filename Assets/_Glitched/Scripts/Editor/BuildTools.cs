using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Glitched.EditorTools
{
    /// <summary>Player builds: Glitched -> Build Windows / Build Android (also callable in batch mode).</summary>
    public static class BuildTools
    {
        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("Glitched/Build Windows (.exe)", priority = 20)]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/GLITCHED.exe", false);

        [MenuItem("Glitched/Build Windows Development", priority = 21)]
        public static void BuildWindowsDev() => Build(BuildTarget.StandaloneWindows64, "Builds/WindowsDev/GLITCHED.exe", true);

        [MenuItem("Glitched/Build Android (.apk)", priority = 22)]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/GLITCHED.apk", false);
        }

        public static void BuildWindowsBatch() => Exit(Build(BuildTarget.StandaloneWindows64, "Builds/Windows/GLITCHED.exe", false));
        public static void BuildWindowsDevBatch() => Exit(Build(BuildTarget.StandaloneWindows64, "Builds/WindowsDev/GLITCHED.exe", true));
        /// <summary>Same as BuildWindowsDevBatch, into the folder in GLITCHED_OUT (so a running copy is left alone).</summary>
        public static void BuildWindowsDevToBatch() => Exit(Build(BuildTarget.StandaloneWindows64, (System.Environment.GetEnvironmentVariable("GLITCHED_OUT") ?? "Builds/WindowsDev2") + "/GLITCHED.exe", true));
        /// <summary>Release build (what judges get: no dev overlay, copy verification on) into the folder in GLITCHED_OUT.</summary>
        public static void BuildWindowsReleaseToBatch() => Exit(Build(BuildTarget.StandaloneWindows64, (System.Environment.GetEnvironmentVariable("GLITCHED_OUT") ?? "Builds/WindowsRelease") + "/GLITCHED.exe", false));
        public static void BuildAndroidBatch() { EditorUserBuildSettings.buildAppBundle = false; Exit(Build(BuildTarget.Android, "Builds/Android/GLITCHED.apk", false)); }

        static void Exit(bool ok) => EditorApplication.Exit(ok ? 0 : 1);

        /// <summary>
        /// Copies the voice lines recorded on this PC (Voice Studio, saved in the player data folder) into
        /// StreamingAssets/Voice, so every build ships with them: the game sounds the same on the Expo PCs.
        /// </summary>
        static void ShipRecordedVoices()
        {
            string src = Path.Combine(Application.persistentDataPath, "Voice"), dst = "Assets/StreamingAssets/Voice";
            if (!Directory.Exists(src)) return;
            Directory.CreateDirectory(dst);
            int n = 0;
            foreach (var f in Directory.GetFiles(src))
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext != ".wav" && ext != ".ogg") continue;
                var to = Path.Combine(dst, Path.GetFileName(f));
                if (File.Exists(to) && File.GetLastWriteTimeUtc(to) >= File.GetLastWriteTimeUtc(f)) continue;
                File.Copy(f, to, true);
                n++;
            }
            Debug.Log($"[GLITCHED] Voice: shipped {n} new or changed recordings from {src}");
        }

        static bool Build(BuildTarget target, string output, bool dev)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            ShipRecordedVoices();
            // the game talks to the stall server over plain http on the local network (copy verification)
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            if (!dev && Resources.Load<TextAsset>("Licence/licence_key") == null)
                Debug.LogWarning("[GLITCHED] No licence key in Resources/Licence: release copies won't be able to verify. Run Server/tools/export-game-key.cmd first.");
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = output,
                target = target,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[GLITCHED] Build {target}: {s.result}, {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:0}s -> {output}");
            return s.result == BuildResult.Succeeded;
        }
    }
}
