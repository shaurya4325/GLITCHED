using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// In-level HUD: Stability bar, XP bar and level, coins, knowledge points, ability icons, combo counter,
    /// a minimal companion/interaction prompt, the current objective and the boss health bar.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        UIBar stability, xp, bossBar;
        TextMeshProUGUI stabilityText, levelText, coinsText, kpText, comboText, promptText, objectiveText, bossName, bossHpText, areaText;
        RectTransform abilityRow, comboRt, promptRt, bossRt, objectiveRt;
        CanvasGroup group, bossGroup, promptGroup, comboGroup;
        readonly Dictionary<string, Image> abilityIcons = new Dictionary<string, Image>();
        int shownCoins, targetCoins;
        float comboTimer;
        public Button PauseButton { get; private set; }

        public void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var th = UIFactory.Theme;

            // --- top-left: stability + xp
            var left = UIFactory.Panel(transform, new Color(0.03f, 0.04f, 0.08f, 0.82f), 18, "Status");
            UIFactory.Place(left.rectTransform, new Vector2(0, 1), new Vector2(28, -24), new Vector2(560, 132), new Vector2(0, 1));
            var lbl = UIFactory.Text(left.transform, "STABILITY", 20, th.cyan, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(lbl.rectTransform, new Vector2(0, 1), new Vector2(22, -14), new Vector2(260, 28), new Vector2(0, 1));
            stabilityText = UIFactory.Text(left.transform, "100", 20, th.text, TextAlignmentOptions.TopRight, true);
            UIFactory.Place(stabilityText.rectTransform, new Vector2(1, 1), new Vector2(-22, -14), new Vector2(200, 28), new Vector2(1, 1));
            stability = UIFactory.Bar(left.transform, th.cyan, null, "StabilityBar", 9);
            UIFactory.Place(stability.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(22, -46), new Vector2(516, 26), new Vector2(0, 1));
            levelText = UIFactory.Text(left.transform, "LV 1", 20, th.yellow, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(levelText.rectTransform, new Vector2(0, 1), new Vector2(22, -84), new Vector2(100, 28), new Vector2(0, 1));
            xp = UIFactory.Bar(left.transform, th.yellow, null, "XPBar", 6);
            UIFactory.Place(xp.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(110, -90), new Vector2(428, 14), new Vector2(0, 1));

            // --- top-right: coins, knowledge, abilities
            var right = UIFactory.Panel(transform, new Color(0.03f, 0.04f, 0.08f, 0.82f), 18, "Currencies");
            UIFactory.Place(right.rectTransform, new Vector2(1, 1), new Vector2(-150, -24), new Vector2(420, 70), new Vector2(1, 1));
            UIFactory.HLayout(right, 10, 12);
            coinsText = UIFactory.Text(right.transform, "◆ 0", 28, th.yellow, TextAlignmentOptions.Center, true);
            kpText = UIFactory.Text(right.transform, "KP 0", 28, th.magenta, TextAlignmentOptions.Center, true);
            abilityRow = UIFactory.Rect("Abilities", transform);
            UIFactory.Place(abilityRow, new Vector2(1, 1), new Vector2(-150, -104), new Vector2(420, 56), new Vector2(1, 1));
            var ah = UIFactory.HLayout(abilityRow, 8, 0, TextAnchor.MiddleRight, false);

            // pause button (always; essential on touch)
            PauseButton = UIFactory.Button(transform, "II", () => Services.UI.Pause.Toggle(), ButtonStyle.Secondary, 30, "PauseButton");
            UIFactory.Place(PauseButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-28, -24), new Vector2(96, 96), new Vector2(1, 1));

            // --- objective (under status)
            objectiveRt = UIFactory.Rect("Objective", transform);
            UIFactory.Place(objectiveRt, new Vector2(0, 1), new Vector2(32, -168), new Vector2(700, 40), new Vector2(0, 1));
            objectiveText = UIFactory.Text(objectiveRt, "", 22, th.textDim, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(objectiveText.rectTransform);

            // --- the Class XI topic being studied (under the objective)
            topicRt = UIFactory.Rect("Topic", transform);
            UIFactory.Place(topicRt, new Vector2(0, 1), new Vector2(28, -206), new Vector2(760, 44), new Vector2(0, 1));
            var tp = UIFactory.Panel(topicRt, new Color(0.03f, 0.04f, 0.08f, 0.8f), 14, "TopicPanel");
            UIFactory.Stretch(tp.rectTransform);
            topicText = UIFactory.Text(tp.transform, "", 20, th.text, TextAlignmentOptions.MidlineLeft);
            UIFactory.Stretch(topicText.rectTransform, 16, 2, 12, 2);
            topicGroup = topicRt.gameObject.AddComponent<CanvasGroup>();
            topicGroup.alpha = 0;

            // --- parkour run timer (top centre)
            runRt = UIFactory.Rect("RunTimer", transform);
            UIFactory.Place(runRt, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(520, 96), new Vector2(0.5f, 1));
            var rp = UIFactory.Panel(runRt, new Color(0.03f, 0.04f, 0.08f, 0.85f), 18, "RunPanel");
            UIFactory.Stretch(rp.rectTransform);
            runText = UIFactory.Text(rp.transform, "", 44, th.yellow, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(runText.rectTransform, 10, 4, 10, 4);
            runGroup = runRt.gameObject.AddComponent<CanvasGroup>();
            runGroup.alpha = 0;

            // --- area name (fades in on level start)
            areaText = UIFactory.Text(transform, "", 64, th.text, TextAlignmentOptions.Center, true);
            UIFactory.Place(areaText.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1400, 120));
            areaText.alpha = 0;

            // --- combo (right middle)
            comboRt = UIFactory.Rect("Combo", transform);
            UIFactory.Place(comboRt, new Vector2(1, 0.6f), new Vector2(-60, 0), new Vector2(360, 160), new Vector2(1, 0.5f));
            comboGroup = comboRt.gameObject.AddComponent<CanvasGroup>();
            comboText = UIFactory.Text(comboRt, "", 72, th.magenta, TextAlignmentOptions.Right, true);
            UIFactory.Stretch(comboText.rectTransform);
            comboText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            comboText.outlineWidth = 0.2f; comboText.outlineColor = new Color32(10, 5, 20, 255);
            comboGroup.alpha = 0;

            // --- prompt (bottom centre)
            promptRt = UIFactory.Rect("Prompt", transform);
            UIFactory.Place(promptRt, new Vector2(0.5f, 0), new Vector2(0, 210), new Vector2(700, 70), new Vector2(0.5f, 0));
            var pp = UIFactory.Panel(promptRt, new Color(0.03f, 0.04f, 0.08f, 0.9f), 16, "PromptPanel");
            UIFactory.Stretch(pp.rectTransform);
            promptText = UIFactory.Text(pp.transform, "", 28, th.text, TextAlignmentOptions.Center);
            UIFactory.Stretch(promptText.rectTransform, 16, 4, 16, 4);
            promptGroup = promptRt.gameObject.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0;

            // --- boss bar (top centre)
            bossRt = UIFactory.Rect("BossBar", transform);
            UIFactory.Place(bossRt, new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(900, 100), new Vector2(0.5f, 1));
            bossGroup = bossRt.gameObject.AddComponent<CanvasGroup>();
            bossName = UIFactory.Text(bossRt, "", 30, th.magenta, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(bossName.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(700, 40), new Vector2(0, 1));
            bossHpText = UIFactory.Text(bossRt, "", 26, th.text, TextAlignmentOptions.TopRight, true);
            UIFactory.Place(bossHpText.rectTransform, new Vector2(1, 1), Vector2.zero, new Vector2(300, 40), new Vector2(1, 1));
            bossBar = UIFactory.Bar(bossRt, th.magenta, new Color(1, 1, 1, 0.1f), "BossHP", 10);
            UIFactory.Place(bossBar.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(900, 30), new Vector2(0.5f, 1));
            bossGroup.alpha = 0;

            GameEvents.StabilityChanged += OnStability;
            GameEvents.XPChanged += _ => RefreshXP();
            GameEvents.CoinsChanged += c => targetCoins = c;
            GameEvents.KnowledgeChanged += k => { kpText.text = "KP " + k; Tween.Punch(kpText.transform, 0.4f, 0.4f); };
            GameEvents.ComboChanged += OnCombo;
            GameEvents.AbilityUnlocked += _ => RefreshAbilities();
        }

        public void Show(bool on)
        {
            gameObject.SetActive(on);
            if (on) RefreshAll();
        }

        public void RefreshAll()
        {
            var d = Services.Game.Data;
            shownCoins = targetCoins = d.coins;
            coinsText.text = "◆ " + d.coins;
            kpText.text = "KP " + d.knowledgePoints;
            RefreshXP();
            RefreshAbilities();
            PauseButton.gameObject.SetActive(true);
        }

        void RefreshXP()
        {
            var d = Services.Game.Data;
            int lv = GameManager.LevelForXP(d.xp);
            int a = GameManager.XPForLevel(lv), b = GameManager.XPForLevel(lv + 1);
            levelText.text = "LV " + lv;
            xp.Set(b > a ? (d.xp - a) / (float)(b - a) : 0);
        }

        void RefreshAbilities()
        {
            UIFactory.Clear(abilityRow);
            abilityIcons.Clear();
            foreach (var id in Services.Game.Data.abilities)
            {
                string glyph = id == "double_jump" ? "⇈" : id == "dash" ? "»" : id == "wall_jump" ? "⊣" : id == "shrink" ? "◦" : "?";
                var chip = UIFactory.Chip(abilityRow, glyph, UIFactory.Theme.cyan, 26);
                abilityIcons[id] = chip;
            }
        }

        void OnStability(float cur, float max)
        {
            stability.Set(max > 0 ? cur / max : 0);
            stabilityText.text = Mathf.CeilToInt(cur).ToString();
            var th = UIFactory.Theme;
            float k = max > 0 ? cur / max : 0;
            stability.SetColor(k > 0.5f ? th.cyan : k > 0.25f ? th.yellow : th.red);
        }

        void OnCombo(int c)
        {
            if (c < 2) { comboTimer = 0; return; }
            comboText.text = $"COMBO\n<size=140%>x{c}</size>";
            comboGroup.alpha = 1;
            comboTimer = 3f;
            Tween.Punch(comboRt, 0.35f, 0.35f);
        }

        public void SetObjective(string text) { if (objectiveText != null) objectiveText.text = string.IsNullOrEmpty(text) ? "" : "▸ " + text; }

        RectTransform topicRt, runRt;
        TextMeshProUGUI topicText, runText;
        CanvasGroup topicGroup, runGroup;

        /// <summary>Shows the Class XI topic the enemies here are made of (null hides it).</summary>
        public void SetTopic(LessonData l)
        {
            if (topicGroup == null) return;
            if (l == null) { topicGroup.alpha = 0; return; }
            var c = LessonCard.Hex(LessonCard.SubjectColor(l.subject));
            topicText.text = $"<color={c}><b>CLASS XI ▸</b></color> <b>{l.title}</b>   <color=#9fb0cc>{l.subject}</color>";
            if (topicGroup.alpha < 0.5f) { Tween.Fade(topicGroup, 1, 0.4f); Tween.Punch(topicRt, 0.15f, 0.35f); }
        }

        /// <summary>The parkour run clock (null hides it).</summary>
        public void SetRunTimer(string text, Color? color = null)
        {
            if (runGroup == null) return;
            if (string.IsNullOrEmpty(text)) { if (runGroup.alpha > 0) Tween.Fade(runGroup, 0, 0.4f); return; }
            runText.text = text;
            runText.color = color ?? UIFactory.Theme.yellow;
            runGroup.alpha = 1;
        }

        public void ShowPrompt(string text)
        {
            if (string.IsNullOrEmpty(text)) { promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, 0, 0.2f); return; }
            promptText.text = text;
            promptGroup.alpha = 1;
        }

        public void HidePrompt() { if (promptGroup != null) promptGroup.alpha = 0; }

        public void AreaTitle(string title, string sub = "")
        {
            areaText.text = string.IsNullOrEmpty(sub) ? title : $"{title}\n<size=45%><color=#9fb0cc>{sub}</color></size>";
            areaText.alpha = 0;
            Tween.Value(0, 1, 0.6f, a => areaText.alpha = a);
            Tween.After(3.2f, () => Tween.Value(1, 0, 0.8f, a => { if (areaText) areaText.alpha = a; }));
        }

        public void ShowBoss(string name, int hp, int max)
        {
            bossName.text = name;
            UpdateBoss(hp, max, true);
            Tween.Fade(bossGroup, 1, 0.4f);
        }

        public void UpdateBoss(int hp, int max, bool instant = false)
        {
            bossHpText.text = $"HP {Mathf.Max(0, hp)} / {max}";
            bossBar.Set(max > 0 ? hp / (float)max : 0, instant);
        }

        public void HideBoss() { if (bossGroup != null) Tween.Fade(bossGroup, 0, 0.4f); }

        void Update()
        {
            if (shownCoins != targetCoins)
            {
                shownCoins = (int)Mathf.MoveTowards(shownCoins, targetCoins, Mathf.Max(1, Mathf.Abs(targetCoins - shownCoins) * Time.unscaledDeltaTime * 6f));
                coinsText.text = "◆ " + shownCoins;
            }
            if (comboTimer > 0)
            {
                comboTimer -= Time.unscaledDeltaTime;
                if (comboTimer <= 0) Tween.Fade(comboGroup, 0, 0.4f);
            }
        }
    }
}
