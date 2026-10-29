using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Three save slots: continue, start new, or delete.</summary>
    public class SaveSlotsScreen : SubScreen
    {
        public static void Open() => Create<SaveSlotsScreen>("SAVE SLOTS", new Vector2(1400, 820));

        protected override void Build() => Refresh();

        void Refresh()
        {
            UIFactory.Clear(Body);
            UIFactory.VLayout(Body, 20);
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                int slot = i;
                var d = Services.Save.Peek(i);
                var row = UIFactory.Panel(Body, new Color(0.05f, 0.07f, 0.12f, 0.95f), 18, "Slot" + i);
                UIFactory.Size(row, -1, 170);
                string text;
                if (d == null) text = $"<b>SLOT {i + 1}</b>\n<color=#8fa3c0>Empty</color>";
                else
                {
                    string region = "Code City";
                    foreach (var a in Services.Db.arcs) if (a != null && d.arcs.TryGetValue(a.id, out var ap) && ap.unlocked) region = a.displayName;
                    int restored = 0; foreach (var kv in d.arcs) if (kv.Value.restored) restored++;
                    var time = System.TimeSpan.FromSeconds(d.playSeconds);
                    text = $"<b>SLOT {i + 1}</b>  ·  {d.playerName}  ·  LV {d.level}\n<color=#8fa3c0>{region}  ·  {restored}/5 regions restored  ·  {(int)time.TotalHours}h {time.Minutes:00}m  ·  {d.knowledgePoints} KP\nlast played {d.lastPlayed}</color>";
                }
                var t = UIFactory.Text(row.transform, text, 28);
                UIFactory.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(820, 150), new Vector2(0, 0.5f));
                var btns = UIFactory.Rect("Buttons", row.transform);
                UIFactory.Place(btns, new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(430, 80), new Vector2(1, 0.5f));
                UIFactory.HLayout(btns, 12);
                if (d == null) UIFactory.Button(btns, "NEW GAME", () => DisclaimerScreen.BeforeStart(() => NewGame(slot)), ButtonStyle.Primary, 26);
                else
                {
                    UIFactory.Button(btns, "PLAY", () => Continue(slot), ButtonStyle.Primary, 26);
                    UIFactory.Button(btns, "DELETE", () => Services.UI.Confirm("Delete this save?", "This cannot be undone.", () => { Services.Save.Delete(slot); Refresh(); }), ButtonStyle.Danger, 22);
                }
            }
        }

        public static void NewGame(int slot)
        {
            CloseAll();
            Services.Save.NewGame(slot, "Shaurya");
            Services.Scenes.Load("Opening");
        }

        public static void Continue(int slot)
        {
            CloseAll();
            if (!Services.Save.Load(slot)) { Services.UI.Toast("That save could not be read."); return; }
            var d = Services.Save.Current;
            if (!d.HasFlag("opening_seen")) { Services.Scenes.Load("Opening"); return; }
            string scene = d.currentScene;
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene) || scene == "Title" || scene == "Opening") scene = "WorldMap";
            Services.Scenes.Load(scene);
        }
    }
}
