using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>The region-restored rewards: XP + coins, new ability, new cosmetic, new region.</summary>
    public static class RewardsPanel
    {
        public static IEnumerator Show(ArcDataSO arc, bool newAbility)
        {
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("Rewards", Services.UI.PopupLayer));
            UIFactory.Blocker(root, 0.7f);
            var p = UIFactory.Panel(root, null, 28, "Panel");
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 800));
            var title = UIFactory.Text(p.transform, $"{arc.displayName.ToUpper()} RESTORED", 64, th.green, TextAlignmentOptions.Center, true);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1200, 90), new Vector2(0.5f, 1));
            var sub = UIFactory.Text(p.transform, "Restoration is the report card: the lights are on because you understood something.", 26, th.textDim, TextAlignmentOptions.Center);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1200, 50), new Vector2(0.5f, 1));
            var list = UIFactory.Rect("List", p.transform);
            UIFactory.Stretch(list, 120, 220, 120, 150);
            UIFactory.VLayout(list, 16);
            var d = Services.Game.Data;
            Row(list, "◆", "XP + COINS", $"Level {d.level}   ·   {d.xp} XP   ·   {d.coins} coins", th.yellow);
            if (!string.IsNullOrEmpty(arc.abilityReward)) Row(list, "⇈", newAbility ? "NEW ABILITY" : "ABILITY", GameManager.AbilityName(arc.abilityReward), th.cyan);
            var cos = Services.Db.Cosmetic(arc.cosmeticReward);
            if (cos != null) Row(list, "★", "NEW COSMETIC", $"{cos.displayName}   <size=75%><color=#9fb0cc>(equip it at any costume terminal)</color></size>", th.magenta);
            ArcDataSO next = null;
            foreach (var a in Services.Db.arcs) if (a != null && a.order == arc.order + 1) next = a;
            if (next != null) Row(list, "▶", "NEW REGION", $"{next.displayName}: {next.subject}", th.green);
            Services.Audio.Play("rewards");
            bool close = false;
            var b = UIFactory.Button(p.transform, next != null ? "CONTINUE" : "FINISH", () => close = true, ButtonStyle.Primary, 30);
            UIFactory.Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(360, 86), new Vector2(0.5f, 0));
            UIFactory.Select(b);
            p.transform.localScale = Vector3.one * 0.8f;
            Tween.Scale(p.transform, Vector3.one, 0.4f);
            float t0 = Time.unscaledTime;
            while (!close) { if (Time.unscaledTime - t0 > 1f && Services.Input.SubmitPressed) close = true; if (QuickQuiz.AutoAnswer && Time.unscaledTime - t0 > 2.5f) close = true; yield return null; }   // test bot: moves on
            Object.Destroy(root.gameObject);
        }

        static void Row(Transform parent, string icon, string label, string value, Color c)
        {
            var row = UIFactory.Panel(parent, new Color(c.r * 0.12f, c.g * 0.12f, c.b * 0.15f, 1f), 16, "Row");
            UIFactory.Size(row, -1, 96);
            var i = UIFactory.Text(row.transform, icon, 48, c, TextAlignmentOptions.Center, true);
            UIFactory.Place(i.rectTransform, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(80, 80), new Vector2(0.5f, 0.5f));
            var t = UIFactory.Text(row.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(c)}><size=70%>{label}</size></color>\n{value}", 30);
            UIFactory.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(900, 90), new Vector2(0, 0.5f));
            row.transform.localScale = new Vector3(0.9f, 0.9f, 1);
            Tween.Scale(row.transform, Vector3.one, 0.35f);
        }
    }
}
