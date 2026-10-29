using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Pause menu: Resume, Mastery, Achievements, Settings, World Map, Quit to Title (hidden in Expo mode).</summary>
    public class PauseMenu : MonoBehaviour
    {
        CanvasGroup group;
        RectTransform panel;
        RectTransform list;
        public bool IsOpen { get; private set; }
        public static bool Allowed = true;   // false on title / during cutscenes

        public void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            UIFactory.Blocker(transform, 0.72f);
            panel = UIFactory.Panel(transform, null, 24, "Panel").rectTransform;
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 820));
            var title = UIFactory.Text(panel, "PAUSED", 64, UIFactory.Theme.cyan, TextAlignmentOptions.Center, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(560, 90), new Vector2(0.5f, 1));
            title.gameObject.AddComponent<GlitchText>().intensityOverride = 0.4f;
            list = UIFactory.Rect("List", panel);
            UIFactory.Stretch(list, 60, 150, 60, 50);
            UIFactory.VLayout(list, 16);
            Close(true);
        }

        void Rebuild()
        {
            UIFactory.Clear(list);
            Button first = UIFactory.Button(list, L.T("pause.resume", "RESUME"), () => Close());
            UIFactory.Size(first, -1, 84);
            UIFactory.Size(UIFactory.Button(list, L.T("pause.mastery", "MASTERY"), () => { MasteryScreen.Open(); }, ButtonStyle.Secondary), -1, 78);
            UIFactory.Size(UIFactory.Button(list, L.T("pause.achievements", "ACHIEVEMENTS"), () => { AchievementsScreen.Open(); }, ButtonStyle.Secondary), -1, 78);
            UIFactory.Size(UIFactory.Button(list, L.T("pause.settings", "SETTINGS"), () => { SettingsScreen.Open(); }, ButtonStyle.Secondary), -1, 78);
            bool inLevel = Services.Game.CurrentArc != null;
            if (inLevel && !Services.Game.IsExpo)
            {
                UIFactory.Size(UIFactory.Button(list, L.T("pause.checkpoint", "RESPAWN AT CHECKPOINT"), () => { Close(); PlayerController.Instance?.RespawnAtCheckpoint(false); }, ButtonStyle.Secondary), -1, 78);
                UIFactory.Size(UIFactory.Button(list, L.T("pause.worldmap", "WORLD MAP"), () => { Close(); Services.Scenes.Load("WorldMap"); }, ButtonStyle.Secondary), -1, 78);
            }
            if (!Services.Game.IsExpo)
                UIFactory.Size(UIFactory.Button(list, L.T("pause.title", "QUIT TO TITLE"), () => { Close(); Services.Scenes.Load("Title"); }, ButtonStyle.Danger), -1, 78);
            else
                UIFactory.Size(UIFactory.Button(list, "END EXPO DEMO", () => { Close(); ExpoMode.EndDemo(); }, ButtonStyle.Danger), -1, 78);
            UIFactory.Select(first);
        }

        public void Toggle() { if (IsOpen) Close(); else OpenMenu(); }

        public void OpenMenu()
        {
            if (IsOpen || !Allowed) return;
            IsOpen = true;
            Rebuild();
            group.alpha = 0; group.blocksRaycasts = true; group.interactable = true;
            Tween.Fade(group, 1, 0.15f);
            panel.localScale = Vector3.one * 0.9f;
            Tween.Scale(panel, Vector3.one, 0.25f);
            Services.Game.SetPaused(true);
            Services.Audio.Play("pause");
            Services.Audio.DuckMusic(true);
        }

        public void Close(bool instant = false)
        {
            IsOpen = false;
            group.blocksRaycasts = false; group.interactable = false;
            if (instant) group.alpha = 0; else Tween.Fade(group, 0, 0.12f);
            if (!instant) { Services.Game.SetPaused(false); Services.Audio.DuckMusic(false); }
            SubScreen.CloseAll();
        }

        /// <summary>A text box (a coding answer, a name) has the keyboard.</summary>
        static bool TypingInField()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            var sel = es != null ? es.currentSelectedGameObject : null;
            var f = sel != null ? sel.GetComponent<TMPro.TMP_InputField>() : null;
            return f != null && f.isFocused;
        }

        void Update()
        {
            // ESC (or the gamepad's start) opens the menu; P no longer does, since it is a letter you type in answers
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (!Services.Input.PausePressed && !esc) return;
            if (TypingInField()) return;   // ESC inside a text box only leaves the box
            if (SubScreen.AnyOpen) { SubScreen.CloseTop(); return; }
            if (Services.UI.Dialogue.Open || CutscenePlayer.Active || !Allowed) return;
            string scene = SceneManager.GetActiveScene().name;
            if (Services.Game.CurrentArc == null && scene != "WorldMap" && scene != "Kanha_Final") return;   // not on the title screen or in the opening
            Toggle();
        }
    }
}
