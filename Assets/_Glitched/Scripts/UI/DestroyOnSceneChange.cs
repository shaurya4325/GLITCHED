using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>Removes a piece of the persistent UI (a lesson card, a pop-up) when a new scene loads.</summary>
    public class DestroyOnSceneChange : MonoBehaviour
    {
        public System.Action onGone;
        void OnEnable() => SceneManager.sceneLoaded += Loaded;
        void OnDisable() => SceneManager.sceneLoaded -= Loaded;
        void Loaded(Scene s, LoadSceneMode m) { if (m == LoadSceneMode.Single && this != null) Destroy(gameObject); }
        void OnDestroy() => onGone?.Invoke();
    }
}
