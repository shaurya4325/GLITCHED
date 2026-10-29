using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Settings: Graphics, Audio, Controls, Accessibility, Language (Section 4.2 + 12).</summary>
    public class SettingsScreen : SubScreen
    {
        RectTransform tabs, content;
        string current = "Graphics";
        static readonly string[] Tabs = { "Graphics", "Audio", "Controls", "Accessibility", "Language" };

        public static void Open() => Create<SettingsScreen>(L.T("settings.title", "SETTINGS"), new Vector2(1500, 920));

        protected override void Build()
        {
            tabs = UIFactory.Rect("Tabs", Body);
            UIFactory.Place(tabs, new Vector2(0.5f, 1), Vector2.zero, new Vector2(1400, 70), new Vector2(0.5f, 1));
            UIFactory.HLayout(tabs, 12);
            content = UIFactory.Rect("Content", Body);
            UIFactory.Stretch(content, 0, 90, 0, 0);
            Show(current);
        }

        void Show(string tab)
        {
            current = tab;
            UIFactory.Clear(tabs);
            foreach (var t in Tabs)
            {
                string k = t;
                var b = UIFactory.Button(tabs, L.T("settings." + t.ToLower(), t.ToUpper()), () => Show(k), t == tab ? ButtonStyle.Primary : ButtonStyle.Ghost, 24);
            }
            UIFactory.Clear(content);
            var list = UIFactory.Scroll(content, out _, 8);
            UIFactory.Stretch((RectTransform)list.parent);
            var s = Services.Settings.Data;
            var g = Services.Graphics;
            switch (tab)
            {
                case "Graphics":
                    if (!Application.isMobilePlatform)
                        UIFactory.Selector(list, "Display", new[] { "FULLSCREEN", "WINDOWED" }, s.fullscreen ? 0 : 1, i => { s.fullscreen = i == 0; g.ApplyDisplay(); Services.Settings.Apply(); });
                    UIFactory.Selector(list, "Quality preset", new[] { "POTATO", "LOW", "MEDIUM", "HIGH", "ULTRA" }, (int)s.tier, i =>
                    {
                        s.renderScale = -1; s.tierChosen = true;
                        g.ApplyTier((GraphicsTier)i); Services.Settings.Apply();
                    });
                    UIFactory.Slider(list, "Render scale", 0.5f, 1f, s.renderScale > 0 ? s.renderScale : (g.Current != null ? g.Current.renderScale : 1f), v =>
                    { s.renderScale = v; g.ApplyTier(s.tier); Services.Settings.Apply(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    var caps = new[] { "TIER DEFAULT", "30", "60", "UNLIMITED" };
                    int capIndex = s.fpsCap < 0 ? 0 : s.fpsCap == 30 ? 1 : s.fpsCap == 60 ? 2 : 3;
                    UIFactory.Selector(list, "FPS cap", caps, capIndex, i => { s.fpsCap = i == 0 ? -1 : i == 1 ? 30 : i == 2 ? 60 : 0; g.ApplyTier(s.tier); Services.Settings.Apply(); });
                    UIFactory.Toggle(list, "Shadows", s.shadows, v => { s.shadows = v; g.ApplyTier(s.tier); Services.Settings.Apply(); });
                    UIFactory.Toggle(list, "Post-processing", s.postProcessing, v => { s.postProcessing = v; g.ApplyTier(s.tier); Services.Settings.Apply(); });
                    UIFactory.Slider(list, "Particles", 0f, 1f, s.particles, v => { s.particles = v; Services.Settings.Apply(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Selector(list, "Background detail", new[] { "LOW", "MEDIUM", "HIGH" }, (int)s.backgroundDetail, i => { s.backgroundDetail = (BackgroundDetail)i; Services.Settings.Apply(); GameEvents.RaiseTier(s.tier); });
                    UIFactory.Slider(list, "Camera sway", 0f, 1f, s.cameraSway, v => { s.cameraSway = v; Services.Settings.Apply(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Toggle(list, "Screen shake", s.screenShake, v => { s.screenShake = v; Services.Settings.Apply(); });
                    var info = UIFactory.Text(list, $"<color=#8fa3c0>Detected: {SystemInfo.graphicsDeviceName}, {SystemInfo.systemMemorySize} MB RAM. Suggested tier: {DeviceProfiler.GuessTier()}</color>", 22);
                    UIFactory.Size(info, -1, 60);
                    UIFactory.Size(UIFactory.Button(list, "VOICE STUDIO: RECORD YOUR VOICE", () => VoiceStudio.Open(), ButtonStyle.Primary, 24), -1, 70);
                    UIFactory.Size(UIFactory.Button(list, "KANHA'S LINES: WRITE THE VILLAIN'S DIALOGUE", () => KanhaWriter.Open(), ButtonStyle.Secondary, 24), -1, 70);
                    UIFactory.Size(UIFactory.Button(list, "RUN BENCHMARK AGAIN", () => { CloseAll(); Services.UI.Pause.Close(); Services.Settings.Data.tierChosen = false; Services.Scenes.Load("Benchmark"); }, ButtonStyle.Secondary, 24), -1, 70);
                    break;

                case "Audio":
                    UIFactory.Slider(list, "Master", 0, 1, s.masterVolume, v => { s.masterVolume = v; Services.Audio.ApplyVolumes(); Services.Settings.SaveSettings(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Slider(list, "Music", 0, 1, s.musicVolume, v => { s.musicVolume = v; Services.Audio.ApplyVolumes(); Services.Settings.SaveSettings(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Slider(list, "Sound effects", 0, 1, s.sfxVolume, v => { s.sfxVolume = v; Services.Audio.ApplyVolumes(); Services.Settings.SaveSettings(); Services.Audio.Play("hit"); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Slider(list, "Interface", 0, 1, s.uiVolume, v => { s.uiVolume = v; Services.Audio.ApplyVolumes(); Services.Settings.SaveSettings(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Slider(list, "Voices", 0, 1, s.voiceVolume, v => { s.voiceVolume = v; Services.Audio.ApplyVolumes(); Services.Settings.SaveSettings(); Services.Audio.PlayBlip("blip", 1f); }, v => Mathf.RoundToInt(v * 100) + "%");
                    break;

                case "Controls":
                    var keys = UIFactory.Text(list,
                        "<b>KEYBOARD + MOUSE</b>\nA / D or Arrows: move (keep running to sprint)     Space / W: jump     E: interact\nS / Down at a run: slide     Shift: dash     Q: shrink / grow     Esc: pause\nQuiz battles: 1-4 or click     Mouse: challenges (click, drag & drop)     F3: debug overlay\n\n" +
                        "<b>PARKOUR</b>\nJump at a ledge to climb it, run at a low block to vault it, jump at a bar to swing (jump again to let go), jump at a cable to zipline.\n\n" +
                        "<b>GAMEPAD</b>\nLeft stick: move (tilt a little to walk, down to slide)   A: jump   X: interact   B / RB: dash   LB: shrink   Start: pause\n\n" +
                        "<b>TOUCH</b>\nJoystick (left; pull down to slide), JUMP / E / DASH / SIZE buttons (right), tap the answers in quiz battles.", 24, UIFactory.Theme.textDim, TextAlignmentOptions.TopLeft);
                    UIFactory.Size(keys, -1, 470);
                    UIFactory.Toggle(list, "Show touch controls", s.showTouchControls, v => { s.showTouchControls = v; Services.Settings.Apply(); });
                    UIFactory.Slider(list, "Touch button size", 0.7f, 1.5f, s.touchButtonScale, v => { s.touchButtonScale = v; Services.UI.Touch.ApplyLayout(); Services.Settings.Apply(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    var row = UIFactory.Rect("TouchButtons", list);
                    UIFactory.Size(row, -1, 80);
                    UIFactory.HLayout(row, 16);
                    UIFactory.Button(row, "EDIT TOUCH LAYOUT", () => TouchLayoutEditor.Begin(), ButtonStyle.Secondary, 24);
                    UIFactory.Button(row, "RESET LAYOUT", () => Services.UI.Touch.ResetLayout(), ButtonStyle.Ghost, 24);
                    break;

                case "Accessibility":
                    UIFactory.Selector(list, "Text size", new[] { "SMALL", "MEDIUM", "LARGE" }, (int)s.textSize, i => { s.textSize = (TextSize)i; Services.Settings.Apply(); });
                    UIFactory.Toggle(list, "Dyslexia-friendly font", s.dyslexiaFont, v => { s.dyslexiaFont = v; Services.Settings.Apply(); });
                    UIFactory.Toggle(list, "Colour-blind-safe answers (shape + colour)", s.colorBlindSafe, v => { s.colorBlindSafe = v; Services.Settings.Apply(); });
                    UIFactory.Toggle(list, "Reduce flashing", s.reduceFlashing, v => { s.reduceFlashing = v; Services.Settings.Apply(); });
                    UIFactory.Toggle(list, "Relaxed mode (longer challenge timers)", s.relaxedMode, v => { s.relaxedMode = v; Services.Settings.Apply(); });
                    UIFactory.Slider(list, "Camera sway (motion)", 0f, 1f, s.cameraSway, v => { s.cameraSway = v; Services.Settings.Apply(); }, v => Mathf.RoundToInt(v * 100) + "%");
                    UIFactory.Toggle(list, "Screen shake", s.screenShake, v => { s.screenShake = v; Services.Settings.Apply(); });
                    var note = UIFactory.Text(list, "<color=#8fa3c0>Text size and font changes apply to newly opened screens.</color>", 22);
                    UIFactory.Size(note, -1, 50);
                    break;

                case "Language":
                    var langs = Services.Loc.Languages;
                    var names = new List<string>();
                    foreach (var l in langs) names.Add(l == "en" ? "ENGLISH" : l == "hi" ? "हिन्दी (HINDI, PARTIAL)" : l.ToUpper());
                    UIFactory.Selector(list, "Language", names, Mathf.Max(0, langs.IndexOf(s.language)), i => { s.language = langs[i]; Services.Loc.SetLanguage(s.language); Services.Settings.Apply(); });
                    var ln = UIFactory.Text(list, "<color=#8fa3c0>Menus use the localisation table (Resources/Localization/strings.csv). Add a column to add a language.</color>", 22);
                    UIFactory.Size(ln, -1, 80);
                    break;
            }
        }
    }
}
