using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The lesson a terminal teaches: a holographic textbook page for one CBSE Class XI topic (subject, unit /
    /// NCERT chapter, title), three short pages with a worked example each, and the key idea to remember.
    /// Next: Enter / Space / E / → (A on a pad), back: ← / Backspace (B on a pad), or the on-screen buttons.
    /// </summary>
    public static class LessonCard
    {
        public static bool Open { get; private set; }
        /// <summary>Test bot: turn the pages on their own.</summary>
        public static bool AutoAdvance;

        public static Color SubjectColor(string subject)
        {
            switch (subject)
            {
                case "Physics": return new Color(1f, 0.62f, 0.22f);
                case "Chemistry": return new Color(0.4f, 1f, 0.55f);
                case "Biology": return new Color(1f, 0.42f, 0.62f);
                default: return UIFactory.Theme.cyan;
            }
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        static bool NextPressed()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return Services.Input.SubmitPressed || (k != null && (k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame))
                   || (g != null && g.buttonSouth.wasPressedThisFrame);
        }

        static bool BackPressed()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)) || (g != null && g.buttonEast.wasPressedThisFrame);
        }

        public static IEnumerator Show(LessonData l)
        {
            if (l == null) yield break;
            Open = true;
            var th = UIFactory.Theme;
            Color col = SubjectColor(l.subject);
            var root = UIFactory.Rect("LessonCard", Services.UI.PopupLayer);
            UIFactory.Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<DestroyOnSceneChange>().onGone = () => Open = false;   // never left over in the next level
            UIFactory.Blocker(root, 0.62f);

            var card = UIFactory.Panel(root, new Color(0.025f, 0.035f, 0.065f, 0.97f), 30, "Card");
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(1600, 880));
            card.transform.Find("Outline").GetComponent<Image>().color = new Color(col.r, col.g, col.b, 0.5f);
            var glow = UIFactory.Image(card.transform, new Color(col.r, col.g, col.b, 0.14f), UISprites.Glow(), "Glow");
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 40), new Vector2(1500, 260));
            var bar = UIFactory.Image(card.transform, col, UISprites.Rounded(4), "Accent");
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(1520, 6), new Vector2(0.5f, 1f));

            // header: subject, class, unit / chapter, title
            var chips = UIFactory.Rect("Chips", card.transform);
            UIFactory.Place(chips, new Vector2(0, 1), new Vector2(56, -34), new Vector2(1300, 40), new Vector2(0, 1));
            UIFactory.HLayout(chips, 10, 0, TextAnchor.MiddleLeft, false);
            UIFactory.Chip(chips, "LESSON TERMINAL", th.textDim, 17);
            UIFactory.Chip(chips, "CBSE CLASS XI", th.yellow, 17);
            UIFactory.Chip(chips, l.subject.ToUpperInvariant(), col, 17);
            var unit = UIFactory.Text(chips, l.unit, 22, th.textDim, TextAlignmentOptions.MidlineLeft);
            UIFactory.Size(unit, 640, 40);
            var title = UIFactory.Text(card.transform, l.title.ToUpperInvariant(), 64, col, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(56, -86), new Vector2(1490, 84), new Vector2(0, 1));
            title.enableAutoSizing = true; title.fontSizeMin = 40; title.fontSizeMax = 64;
            var rule = UIFactory.Image(card.transform, new Color(1, 1, 1, 0.08f), null, "Rule");
            UIFactory.Place(rule.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -182), new Vector2(1490, 2), new Vector2(0.5f, 1));

            // page
            var heading = UIFactory.Text(card.transform, "", 40, th.text, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(heading.rectTransform, new Vector2(0, 1), new Vector2(56, -206), new Vector2(1490, 56), new Vector2(0, 1));
            var body = UIFactory.Text(card.transform, "", 31, th.text, TextAlignmentOptions.TopLeft);
            UIFactory.Place(body.rectTransform, new Vector2(0, 1), new Vector2(56, -272), new Vector2(1490, 250), new Vector2(0, 1));
            body.enableAutoSizing = true; body.fontSizeMin = 22; body.fontSizeMax = 31 * Services.Settings.TextScale;
            body.lineSpacing = 12;
            var exBox = UIFactory.Panel(card.transform, th.code, 16, "Example", false);
            UIFactory.Place(exBox.rectTransform, new Vector2(0, 1), new Vector2(56, -540), new Vector2(1490, 150), new Vector2(0, 1));
            var exTag = UIFactory.Text(exBox.transform, "EXAMPLE", 18, col, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(exTag.rectTransform, new Vector2(0, 1), new Vector2(22, -12), new Vector2(400, 24), new Vector2(0, 1));
            var example = UIFactory.Mono(exBox.transform, "", 28);
            UIFactory.Stretch(example.rectTransform, 22, 42, 22, 12);
            example.enableAutoSizing = true; example.fontSizeMin = 18; example.fontSizeMax = 28;
            example.textWrappingMode = TextWrappingModes.Normal;

            // key idea (last page)
            var key = UIFactory.Panel(card.transform, new Color(0.16f, 0.13f, 0.03f, 0.95f), 16, "KeyIdea", false);
            UIFactory.Place(key.rectTransform, new Vector2(0, 1), new Vector2(56, -708), new Vector2(1490, 70), new Vector2(0, 1));
            var keyO = UIFactory.Image(key.transform, th.yellow, UISprites.Rounded(16, 2), "Border");
            UIFactory.Stretch(keyO.rectTransform);
            var keyT = UIFactory.Text(key.transform, $"<color=#ffd640><b>KEY IDEA ▸</b></color>  {l.keyIdea}", 25, th.text, TextAlignmentOptions.MidlineLeft);
            UIFactory.Stretch(keyT.rectTransform, 22, 6, 22, 6);
            keyT.enableAutoSizing = true; keyT.fontSizeMin = 17; keyT.fontSizeMax = 25;

            // footer
            var dots = UIFactory.Text(card.transform, "", 30, col, TextAlignmentOptions.MidlineLeft, true);
            UIFactory.Place(dots.rectTransform, new Vector2(0, 0), new Vector2(58, 34), new Vector2(400, 50), new Vector2(0, 0));
            var hint = UIFactory.Text(card.transform, "<color=#40ebff>ENTER</color> next   <color=#40ebff>←</color> back", 21, th.textDim, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(hint.rectTransform, new Vector2(0, 0), new Vector2(250, 34), new Vector2(520, 50), new Vector2(0, 0));
            int page = 0, pages = Mathf.Max(1, l.slides.Count);
            bool next = false, back = false;
            var backBtn = UIFactory.Button(card.transform, "◀  BACK", () => back = true, ButtonStyle.Secondary, 26);
            UIFactory.Place(backBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-400, 26), new Vector2(220, 68), new Vector2(1, 0));
            var nextBtn = UIFactory.Button(card.transform, "NEXT  ▶", () => next = true, ButtonStyle.Primary, 28);
            UIFactory.Place(nextBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-52, 26), new Vector2(330, 68), new Vector2(1, 0));
            UIFactory.Select(nextBtn);

            var pageGroup = heading.gameObject.AddComponent<CanvasGroup>();
            void Render()
            {
                var s = page < l.slides.Count ? l.slides[page] : new LessonSlide { heading = l.title, body = l.keyIdea };
                heading.text = $"<color={Hex(col)}>{page + 1}.</color> {s.heading}";
                body.text = s.body;
                example.text = s.example ?? "";
                exBox.gameObject.SetActive(!string.IsNullOrEmpty(s.example));
                bool last = page == pages - 1;
                key.gameObject.SetActive(last);
                backBtn.gameObject.SetActive(page > 0);
                UIFactory.Label(nextBtn).text = last ? "GOT IT  ✔" : "NEXT  ▶";
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < pages; i++) sb.Append(i == page ? "● " : "<alpha=#55>● <alpha=#FF>");
                dots.text = sb.ToString();
                body.alpha = 0f; example.alpha = 0f; heading.alpha = 0f;
                Tween.Value(0f, 1f, 0.25f, a => { if (body != null) { body.alpha = a; example.alpha = a; heading.alpha = a; } });
            }
            Render();

            Services.Audio.Play("panel_in");
            group.alpha = 0f;
            card.rectTransform.localScale = Vector3.one * 0.92f;
            Tween.Fade(group, 1f, 0.2f);
            yield return Tween.Scale(card.rectTransform, Vector3.one, 0.3f, Ease.OutBack);

            float opened = Time.unscaledTime;
            while (true)
            {
                if (AutoAdvance && Time.unscaledTime - opened > 1.6f * (page + 1)) next = true;
                if (Time.unscaledTime - opened > 0.25f)
                {
                    if (NextPressed()) next = true;
                    if (BackPressed()) back = true;
                }
                if (back && page > 0) { page--; Services.Audio.Play("click"); Render(); }
                else if (next)
                {
                    if (page >= pages - 1) break;
                    page++; Services.Audio.Play("click"); Render();
                }
                back = next = false;
                if (root == null) { Open = false; yield break; }
                yield return null;
            }

            Services.Audio.Play("correct");
            Tween.Fade(group, 0f, 0.2f);
            yield return Tween.Scale(card.rectTransform, Vector3.one * 0.95f, 0.2f, Ease.InCubic);
            Object.Destroy(root.gameObject);
            Open = false;
        }
    }
}
