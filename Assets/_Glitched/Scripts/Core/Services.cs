using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Creates (once) and holds every persistent manager of the game.
    /// Any scene can be pressed Play on directly: the services boot themselves before the scene loads.
    /// Access a manager with, for example, Services.Audio or Services.Game.
    /// </summary>
    public static class Services
    {
        public static GameObject Root { get; private set; }
        public static GameManager Game { get; private set; }
        public static SaveSystem Save { get; private set; }
        public static SettingsStore Settings { get; private set; }
        public static AudioManager Audio { get; private set; }
        public static GraphicsManager Graphics { get; private set; }
        public static CorruptionManager Corruption { get; private set; }
        public static QuestionBank Questions { get; private set; }
        public static MasteryTracker Mastery { get; private set; }
        public static QuestionSelector Selector { get; private set; }
        public static AchievementManager Achievements { get; private set; }
        public static InputHub Input { get; private set; }
        public static UIRoot UI { get; private set; }
        public static SceneLoader Scenes { get; private set; }
        public static Localization Loc { get; private set; }
        public static GameDatabase Db { get; private set; }
        public static LicenceClient Licence { get; private set; }

        public static bool Ready => Root != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoBoot() => Boot();

        /// <summary>Builds the service root. Safe to call many times.</summary>
        public static void Boot()
        {
            if (Root != null) return;
            Root = new GameObject("[Glitched Services]");
            Object.DontDestroyOnLoad(Root);

            Db = GameDatabase.Load();
            Loc = Root.AddComponent<Localization>();
            Settings = Root.AddComponent<SettingsStore>();
            Save = Root.AddComponent<SaveSystem>();
            Input = Root.AddComponent<InputHub>();
            Graphics = Root.AddComponent<GraphicsManager>();
            Audio = Root.AddComponent<AudioManager>();
            Corruption = Root.AddComponent<CorruptionManager>();
            Questions = Root.AddComponent<QuestionBank>();
            Mastery = Root.AddComponent<MasteryTracker>();
            Selector = Root.AddComponent<QuestionSelector>();
            Achievements = Root.AddComponent<AchievementManager>();
            Game = Root.AddComponent<GameManager>();
            Scenes = Root.AddComponent<SceneLoader>();
            Licence = Root.AddComponent<LicenceClient>();
            Root.AddComponent<TweenRunner>();
            Root.AddComponent<FpsMonitor>();

            var uiGo = new GameObject("[UI Root]");
            Object.DontDestroyOnLoad(uiGo);
            UI = uiGo.AddComponent<UIRoot>();

            Settings.LoadSettings();
            Loc.Init(Settings.Data.language);
            Graphics.Init();
            Audio.Init();
            Questions.LoadAll();
            UI.Init();
            Game.Init();
            CaptureRunner.TryStart();
        }
    }
}
