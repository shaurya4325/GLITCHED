using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>The cosmetic shop terminal: spend coins on ridiculous costumes, equip owned ones.</summary>
    public class ShopScreen : SubScreen
    {
        TextMeshProUGUI coins;

        public static void Open() => Create<ShopScreen>(L.T("shop.title", "COSTUME TERMINAL"), new Vector2(1500, 900), () => Services.Game.SetPaused(false));

        protected override void Build()
        {
            Services.Game.SetPaused(true);
            coins = UIFactory.Text(Panel, "", 34, UIFactory.Theme.yellow, TextAlignmentOptions.Right, true);
            UIFactory.Place(coins.rectTransform, new Vector2(1, 1), new Vector2(-250, -36), new Vector2(400, 60), new Vector2(1, 1));
            Refresh();
        }

        void Refresh()
        {
            UIFactory.Clear(Body);
            coins.text = "◆ " + Services.Game.Data.coins;
            var list = UIFactory.Scroll(Body, out _, 10);
            UIFactory.Stretch((RectTransform)list.parent);
            var pitch = UIFactory.Text(list, "<color=#8fa3c0>\"Every costume is 100% ridiculous and 0% armour.\" - Pixel</color>", 24);
            UIFactory.Size(pitch, -1, 50);
            foreach (var c in Services.Db.cosmetics)
            {
                if (c == null) continue;
                bool owned = Services.Game.OwnsCosmetic(c.id);
                bool equipped = Services.Game.Data.cosmeticEquipped == c.id;
                var row = UIFactory.Panel(list, equipped ? new Color(0.04f, 0.14f, 0.18f, 0.95f) : new Color(0.05f, 0.06f, 0.1f, 0.95f), 14, "Item", equipped);
                UIFactory.Size(row, -1, 104);
                var sw = UIFactory.Image(row.transform, c.hat != "none" ? c.hatColor : c.bodyTint, UISprites.Rounded(14), "Swatch");
                UIFactory.Place(sw.rectTransform, new Vector2(0, 0.5f), new Vector2(54, 0), new Vector2(70, 70), new Vector2(0.5f, 0.5f));
                var t = UIFactory.Text(row.transform, $"<b>{c.displayName}</b>\n<size=75%><color=#9fb0cc>{c.description}</color></size>", 28);
                UIFactory.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(900, 96), new Vector2(0, 0.5f));
                string label; ButtonStyle style; System.Action act;
                if (equipped) { label = "EQUIPPED"; style = ButtonStyle.Ghost; act = null; }
                else if (owned) { label = "EQUIP"; style = ButtonStyle.Primary; act = () => { Services.Game.EquipCosmetic(c.id); Services.Audio.Play("equip"); Refresh(); }; }
                else if (c.rewardOnly) { label = "REWARD"; style = ButtonStyle.Ghost; act = () => Services.UI.Toast("Earn this by restoring a region!"); }
                else { label = $"◆ {c.price}"; style = Services.Game.Data.coins >= c.price ? ButtonStyle.Primary : ButtonStyle.Secondary; act = () => { if (Services.Game.BuyCosmetic(c)) { Services.Audio.Play("buy"); Services.Game.EquipCosmetic(c.id); Refresh(); } else Services.UI.Toast("Not enough coins. Explore and find data shards!"); }; }
                var b = UIFactory.Button(row.transform, label, act, style, 26);
                UIFactory.Place(b.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(220, 72), new Vector2(1, 0.5f));
            }
        }
    }
}
