using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>
    /// The heart of every level scene: applies the region theme and corruption state, spawns the player, Pixel and
    /// the camera rig, shows the HUD, plays the region music and the area title. One per level scene.
    /// </summary>
    public class RegionDirector : MonoBehaviour
    {
        public static RegionDirector Current { get; private set; }
        public string arcId = "CodeCity";
        public string levelId = "";
        public Light sun;
        public float minX = -20, maxX = 300;
        public string objective = "";
        public bool forceCorrupted;
        public bool startHealthy;
        [Tooltip("Played the first time the level loads (Arrival cutscene, tutorial...).")]
        public LevelScript levelScript;

        public ArcDataSO Arc { get; private set; }

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }

        IEnumerator Start()
        {
            Arc = Services.Db.Arc(arcId);
            var theme = Arc != null ? Arc.theme : ContentDefaults.CreateTheme(arcId);
            float c = startHealthy ? 0f : forceCorrupted ? 1f : Services.Game.StartCorruptionFor(Arc);
            Services.Corruption.SetupRegion(theme, sun, c);

            // player at the right spawn point
            Vector3 pos = transform.position + Vector3.up;
            int facing = 1;
            var spawns = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            string want = SceneLoader.NextSpawnId;
            SpawnPoint chosen = null;
            foreach (var s in spawns) if (s.id == want) chosen = s;
            if (chosen == null) foreach (var s in spawns) if (s.id == "default") chosen = s;
            if (chosen == null && spawns.Length > 0) chosen = spawns[0];
            // continuing a save: start at the last checkpoint of this scene
            var d = Services.Game.Data;
            if (string.IsNullOrEmpty(want) && d.currentScene == SceneManager.GetActiveScene().name && !string.IsNullOrEmpty(d.checkpointId))
                foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                    if (cp.id == d.checkpointId) { pos = cp.SpawnPosition; chosen = null; }
            if (chosen != null) { pos = chosen.transform.position; facing = chosen.facing; }
            SceneLoader.NextSpawnId = "";

            var player = PlayerController.Spawn(pos);
            player.SpawnPoint = pos;
            player.ClearLocks();
            player.Visual.Face(facing);
            var rig = CameraRig.Create(player.transform);
            Companion.Spawn(player.transform);
            Services.UI.Hud.Show(true);
            Services.UI.Touch.SetVisible(true);
            Services.UI.Hud.SetObjective(objective);
            Services.UI.Hud.SetTopic(null);
            Services.UI.Hud.SetRunTimer(null);
            PauseMenu.Allowed = true;
            d.currentScene = SceneManager.GetActiveScene().name;
            PlayRegionMusic();

            var lvl = Arc != null ? Arc.Level(levelId) : null;
            if (lvl != null)
                Services.UI.Hud.AreaTitle(lvl.displayName.ToUpperInvariant(), Arc.displayName + (Services.Game.IsArcRestored(Arc.id) ? "  ·  RESTORED" : "  ·  CORRUPTED"));
            yield return null;
            if (levelScript != null) yield return levelScript.Run(this);
        }

        public static void PlayRegionMusic()
        {
            var arc = Current != null ? Current.Arc : Services.Game.CurrentArc;
            string key = arc != null && arc.theme != null ? arc.theme.musicKey : "explore_CodeCity";
            Services.Audio.Music.Explore(key);
        }

        void Update()
        {
            if (Services.Input.DebugRestorePressed && (Application.isEditor || Debug.isDebugBuild))
            {
                var dir = FindFirstObjectByType<RestorationDirector>() ?? new GameObject("RestorationDirector").AddComponent<RestorationDirector>();
                if (!dir.Playing) StartCoroutine(dir.Play(Arc, true));
            }
        }
    }
}
