using System;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// "Before you begin": GLITCHED is a real Class XI revision game, so keep a pen and a notebook beside you for the
    /// working (the in-game rough paper and calculator help, but real notes stick). Opened from the title menu, and
    /// shown before every new game with an I'M READY button.
    /// </summary>
    public class DisclaimerScreen : SubScreen
    {
        static Action ready;

        public static void Open() { ready = null; Create<DisclaimerScreen>("DISCLAIMER", new Vector2(1300, 800)); }

        /// <summary>Shown before a new game: I'M READY carries on.</summary>
        public static void BeforeStart(Action onReady) { ready = onReady; Create<DisclaimerScreen>("BEFORE YOU BEGIN", new Vector2(1300, 800)); }

        protected override void Build()
        {
            var th = UIFactory.Theme;
            // a notebook and a pen, drawn in UI
            var book = UIFactory.Panel(Body, new Color(0.96f, 0.94f, 0.86f, 1f), 14, "Notebook", false);
            UIFactory.Place(book.rectTransform, new Vector2(0, 1), new Vector2(10, -10), new Vector2(300, 380), new Vector2(0, 1));
            var spine = UIFactory.Image(book.transform, new Color(0.2f, 0.35f, 0.75f), UISprites.Rounded(8), "Spine");
            UIFactory.Place(spine.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(34, 380), new Vector2(0, 0.5f));
            for (int i = 0; i < 11; i++)
            {
                var line = UIFactory.Image(book.transform, new Color(0.62f, 0.72f, 0.9f, 0.9f), null, "Rule");
                UIFactory.Place(line.rectTransform, new Vector2(0, 1), new Vector2(50, -60 - i * 28), new Vector2(232, 2), new Vector2(0, 1));
            }
            var margin = UIFactory.Image(book.transform, new Color(0.92f, 0.45f, 0.45f, 0.9f), null, "Margin");
            UIFactory.Place(margin.rectTransform, new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(2, 380), new Vector2(0, 0.5f));
            var note = UIFactory.Text(book.transform, "<i>v = u + at\ns = ut + ½at²\nn = m / M</i>", 26, new Color(0.1f, 0.2f, 0.55f), TextAlignmentOptions.TopLeft);
            UIFactory.Place(note.rectTransform, new Vector2(0, 1), new Vector2(92, -56), new Vector2(200, 120), new Vector2(0, 1));
            var pen = UIFactory.Image(Body, new Color(0.12f, 0.14f, 0.2f), UISprites.Rounded(10), "Pen");
            UIFactory.Place(pen.rectTransform, new Vector2(0, 1), new Vector2(250, -250), new Vector2(26, 260), new Vector2(0.5f, 0.5f));
            pen.rectTransform.localRotation = Quaternion.Euler(0, 0, -28f);
            var tip = UIFactory.Image(pen.transform, new Color(0.85f, 0.7f, 0.3f), UISprites.Rounded(6), "Tip");
            UIFactory.Place(tip.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -14), new Vector2(14, 30), new Vector2(0.5f, 0.5f));
            var clip = UIFactory.Image(pen.transform, th.cyan, UISprites.Rounded(4), "Clip");
            UIFactory.Place(clip.rectTransform, new Vector2(1, 1), new Vector2(4, -30), new Vector2(6, 80), new Vector2(0, 1));

            var text = UIFactory.Text(Body,
                "<size=120%><b><color=#ffd640>KEEP A PEN AND A NOTEBOOK WITH YOU.</color></b></size>\n\n" +
                "GLITCHED is a real <b>CBSE Class XI</b> revision game. The enemies ask genuine NCERT questions from Physics, Chemistry, Biology and Computer Science, and the bosses ask harder ones.\n\n" +
                "• Work the numericals out on paper: write the formula, put in the values, check the units.\n" +
                "• Note down anything a lesson terminal teaches that you didn't know. Notes you write yourself stick.\n" +
                "• Every question screen also has <color=#40ebff>ROUGH PAPER</color> and a <color=#40ebff>CALCULATOR</color> (R / C). The attack timer waits while they are open.\n\n" +
                "<color=#9fb0cc>The game is a revision aid. It does not replace your NCERT textbook, your teachers or your own practice.</color>",
                26, th.text, TextAlignmentOptions.TopLeft);
            UIFactory.Place(text.rectTransform, new Vector2(0, 1), new Vector2(350, -6), new Vector2(840, 520), new Vector2(0, 1));
            text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSizeMax = 26;

            var row = UIFactory.Rect("Buttons", Body);
            UIFactory.Place(row, new Vector2(1, 0), new Vector2(0, 0), new Vector2(ready != null ? 560 : 260, 76), new Vector2(1, 0));
            UIFactory.HLayout(row, 14);
            if (ready != null)
            {
                var go = ready;
                var b = UIFactory.Button(row, "I'M READY  ▶", () => { ready = null; CloseAll(); go(); }, ButtonStyle.Primary, 28);
                UIFactory.Button(row, "BACK", () => { ready = null; Close(); }, ButtonStyle.Secondary, 24);
                UIFactory.Select(b);
            }
            else UIFactory.Select(UIFactory.Button(row, "GOT IT", Close, ButtonStyle.Primary, 28));
        }
    }
}
