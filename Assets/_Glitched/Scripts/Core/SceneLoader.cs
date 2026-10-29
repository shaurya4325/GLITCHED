using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>Loads scenes behind a glitch transition. Remembers which spawn point the player should use.</summary>
    public class SceneLoader : MonoBehaviour
    {
        public static string NextSpawnId = "";
        public bool Busy { get; private set; }

        public void Load(string sceneName, string spawnId = "", bool glitchy = true)
        {
            if (Busy) return;
            if (!LicenceAccess.CanEnter(sceneName)) { LicenceAccess.ShowDemoWall(); return; }
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings. Run Glitched -> Build Everything.");
                Services.UI.Toast($"Missing scene: {sceneName}");
                return;
            }
            NextSpawnId = spawnId;
            StartCoroutine(LoadRoutine(sceneName, glitchy));
        }

        IEnumerator LoadRoutine(string sceneName, bool glitchy)
        {
            Busy = true;
            Services.Audio.Play(glitchy ? "transition" : "whoosh");
            yield return Services.UI.Fade.FadeOut(glitchy ? 0.45f : 0.3f, glitchy);
            Services.Save.Write();
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return null;
            yield return Services.UI.Fade.FadeIn(0.5f, glitchy);
            Busy = false;
        }

        public void ReloadCurrent(string spawnId = "") => Load(SceneManager.GetActiveScene().name, spawnId);
    }
}
