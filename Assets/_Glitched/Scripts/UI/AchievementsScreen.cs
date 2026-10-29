using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>All achievements with progress.</summary>
    public class AchievementsScreen : SubScreen
    {
        public static void Open() => Create<AchievementsScreen>(L.T("achievements.title", "ACHIEVEMENTS"), new Vector2(1400, 900));

        protected override void Build()
        {
            var list = UIFactory.Scroll(Body, out _, 10);
            UIFactory.Stretch((RectTransform)list.parent);
            var d = Services.Game.Data;
            foreach (var a in Services.Db.achievements)
            {
                if (a == null) continue;
                bool done = d.achievements.Contains(a.id);
                int prog = Mathf.Min(d.Stat(a.statKey), a.target);
                var row = UIFactory.Panel(list, done ? new Color(0.1f, 0.08f, 0.02f, 0.95f) : new Color(0.05f, 0.06f, 0.1f, 0.95f), 14, "Ach", done);
                UIFactory.Size(row, -1, 100);
                var icon = UIFactory.Image(row.transform, done ? UIFactory.Theme.yellow : new Color(1, 1, 1, 0.12f), UISprites.Circle(64), "Icon");
                UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(54, 0), new Vector2(64, 64), new Vector2(0.5f, 0.5f));
                var star = UIFactory.Text(icon.transform, done ? "★" : "?", 38, done ? new Color(0.2f, 0.1f, 0) : UIFactory.Theme.textDim, TextAlignmentOptions.Center, true);
                UIFactory.Stretch(star.rectTransform);
                var t = UIFactory.Text(row.transform, $"<b>{a.title}</b>\n<size=75%><color=#9fb0cc>{a.description}</color></size>", 28);
                UIFactory.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(900, 90), new Vector2(0, 0.5f));
                var p = UIFactory.Text(row.transform, done ? "UNLOCKED" : $"{prog} / {a.target}", 24, done ? UIFactory.Theme.yellow : UIFactory.Theme.textDim, TextAlignmentOptions.Right, true);
                UIFactory.Place(p.rectTransform, new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(260, 60), new Vector2(1, 0.5f));
            }
        }
    }
}
