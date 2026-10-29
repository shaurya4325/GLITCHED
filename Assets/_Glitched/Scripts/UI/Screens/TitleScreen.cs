using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Title screen over a live, glitching 3D Code City. Hidden Expo toggle: click the logo 5 times or Ctrl+Shift+E.</summary>
    public class TitleScreen : MonoBehaviour
    {
        public Light sun;
        public Transform cameraPivot;
        RectTransform menu;
        int logoClicks;
        float lastClick;
        TextMeshProUGUI expoTimer;
        Button devButton;
        static string DevLabel => DevMode.On ? "<color=#ffd640>DEV MODE ✔</color>" : "DEV";

        void Start()
        {
            PauseMenu.Allowed = false;
            Services.Save.Discard();
            ExpoMode.Tick();
            Services.Corruption.SetupRegion(Services.Db.Arc("CodeCity")?.theme, sun, 0.85f);
            var cam = FindFirstObjectByType<Camera>();
            if (cam != null) Services.Graphics.ConfigureCamera(cam);
            Services.Audio.Music.Title();
            Build();
        }

        void Build()
        {
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("Title", Services.UI.SceneLayer));
            var shade = UIFactory.Image(root, new Color(0, 0, 0, 0.35f), null, "Shade");
            UIFactory.Stretch(shade.rectTransform);
            var logo = GlitchLogo.Create(root, "GLITCHED", 190);
            UIFactory.Place((RectTransform)logo.transform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1500, 260));
            var hit = logo.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            var b = logo.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(LogoClicked);
            var tag = UIFactory.Text(root, "K N O W L E D G E   I S   Y O U R   W E A P O N", 30, th.text, TextAlignmentOptions.Center, true);
            UIFactory.Place(tag.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1500, 50));
            var footer = UIFactory.Text(root, "CBSE Skill Expo & Guidance Festival 2026-27  ·  DAV Public School, Defence Colony, Dehradun", 20, th.textDim, TextAlignmentOptions.Center);
            UIFactory.Place(footer.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1600, 40), new Vector2(0.5f, 0));
            var ver = UIFactory.Text(root, "v1.0", 18, th.textDim, TextAlignmentOptions.BottomRight);
            UIFactory.Place(ver.rectTransform, new Vector2(1, 0), new Vector2(-24, 24), new Vector2(200, 30), new Vector2(1, 0));

            menu = UIFactory.Rect("Menu", root);
            UIFactory.Place(menu, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(560, 446), new Vector2(0.5f, 1f));
            UIFactory.VLayout(menu, 10);
            expoTimer = UIFactory.Text(root, "", 26, th.yellow, TextAlignmentOptions.TopRight, true);
            UIFactory.Place(expoTimer.rectTransform, new Vector2(1, 1), new Vector2(-30, -100), new Vector2(600, 40), new Vector2(1, 1));
            // developer mode (password): shows the answer to every question
            devButton = UIFactory.Button(root, DevLabel, () => DevMode.Prompt(() => UIFactory.Label(devButton).text = DevLabel), ButtonStyle.Secondary, 22, "DevButton");
            UIFactory.Place(devButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-30, -26), new Vector2(200, 58), new Vector2(1, 1));
            RebuildMenu();
        }

        /// <summary>Straight into Kanha's Final Exam (for showing the judges): a fresh game in the expo slot, so no save is touched.</summary>
        static void PlayFinalBoss()
        {
            Services.Save.NewGame(SaveSystem.ExpoSlot, "Shaurya");
            Services.Scenes.Load("Kanha_Final");
        }

        void RebuildMenu()
        {
            UIFactory.Clear(menu);
            Button first;
            if (Services.Game.IsExpo)
            {
                first = UIFactory.Button(menu, "▶  START DEMO", ExpoMode.StartDemo, ButtonStyle.Primary, 38);
                UIFactory.Size(first, -1, 100);
                UIFactory.Size(UIFactory.Button(menu, "FINAL BOSS: KANHA THE DARD", PlayFinalBoss, ButtonStyle.Secondary), -1, 76);
                UIFactory.Size(UIFactory.Button(menu, "SETTINGS", SettingsScreen.Open, ButtonStyle.Secondary), -1, 76);
                var expoRow = UIFactory.Rect("Row", menu);
                UIFactory.Size(expoRow, -1, 70, -1, 0);
                UIFactory.HLayout(expoRow, 10);
                UIFactory.Button(expoRow, "CREDITS", CreditsScreen.Open, ButtonStyle.Ghost, 26);
                UIFactory.Button(expoRow, "DISCLAIMER", DisclaimerScreen.Open, ButtonStyle.Ghost, 26);
                expoTimer.text = "EXPO MODE";
            }
            else
            {
                int recent = MostRecentSlot();
                if (recent >= 0)
                {
                    first = UIFactory.Button(menu, "CONTINUE", () => SaveSlotsScreen.Continue(recent), ButtonStyle.Primary, 34);
                    UIFactory.Size(first, -1, 84);
                    UIFactory.Size(UIFactory.Button(menu, "SAVE SLOTS", () => SaveSlotsScreen.Open(), ButtonStyle.Secondary), -1, 64);
                }
                else
                {
                    first = UIFactory.Button(menu, "NEW GAME", () => SaveSlotsScreen.Open(), ButtonStyle.Primary, 34);
                    UIFactory.Size(first, -1, 84);
                }
                UIFactory.Size(UIFactory.Button(menu, "FINAL BOSS: KANHA THE DARD", PlayFinalBoss, ButtonStyle.Secondary), -1, 64);
                UIFactory.Size(UIFactory.Button(menu, "SETTINGS", SettingsScreen.Open, ButtonStyle.Secondary), -1, 64);
                // credits and quit share a row (the menu has to fit above the school footer)
                var row = UIFactory.Rect("Row", menu);
                UIFactory.Size(row, -1, 56, -1, 0);
                UIFactory.HLayout(row, 10);
                UIFactory.Button(row, "CREDITS", CreditsScreen.Open, ButtonStyle.Ghost, 24);
                UIFactory.Button(row, "DISCLAIMER", DisclaimerScreen.Open, ButtonStyle.Ghost, 24);
                UIFactory.Button(row, "QUIT", Quit, ButtonStyle.Ghost, 24);
                expoTimer.text = "";
            }
            UIFactory.Select(first);
        }

        static int MostRecentSlot()
        {
            int best = -1; string bestTime = "";
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                var d = Services.Save.Peek(i);
                if (d != null && string.CompareOrdinal(d.lastPlayed ?? "", bestTime) >= 0) { best = i; bestTime = d.lastPlayed ?? ""; }
            }
            return best;
        }

        void LogoClicked()
        {
            if (Time.unscaledTime - lastClick > 1.2f) logoClicks = 0;
            lastClick = Time.unscaledTime;
            logoClicks++;
            GlitchFX.Pulse(0.2f, 0.15f);
            if (logoClicks >= 5) { logoClicks = 0; ExpoMode.Toggle(); RebuildMenu(); }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 0.12f) * 14f, 0);
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && kb.ctrlKey.isPressed && kb.shiftKey.isPressed) { ExpoMode.Toggle(); RebuildMenu(); }
        }
    }
}
