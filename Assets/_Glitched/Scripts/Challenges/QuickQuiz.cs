using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The quiz battle against a small enemy: one CBSE Class XI MCQ on the topic the last lesson terminal taught.
    /// A card drops in above the fight (both characters stay in view below it), the enemy's attack charges while
    /// you think, and you answer with 1-4 / A-D, a click or a tap. A wrong answer shows the right one and why,
    /// inside the same card, before the enemy strikes.
    /// </summary>
    public static class QuickQuiz
    {
        public static bool Active { get; private set; }
        /// <summary>Test bot: answer correctly after a beat.</summary>
        public static bool AutoAnswer;

        public class Options
        {
            public string enemyName, attackName;
            public float timeScale = 1f;     // < 1 = less time (the tutorial fight)
            public string recap;             // shown when the player skipped the lesson
            public bool showHint;
            public int number = 1;           // question number in this fight
        }

        static readonly string[] Letters = { "A", "B", "C", "D" };

        // 1-4 only: A and D are the movement keys, and you run into the fight holding one of them
        static int KeyPressed()
        {
            for (int i = 0; i < 4; i++) if (Services.Input.AnswerPressed(i)) return i;
            return -1;
        }

        static bool ContinuePressed()
        {
            var k = Keyboard.current;
            return Services.Input.SubmitPressed || (k != null && (k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame));
        }

        public static void Run(QuestionData q, Options opt, Action<ChallengeOutcome> done) => Services.UI.StartCoroutine(Routine(q, opt ?? new Options(), done));

        static IEnumerator Routine(QuestionData q, Options opt, Action<ChallengeOutcome> done)
        {
            while (Active || ChallengeEngine.Active) yield return null;
            Active = true;
            var th = UIFactory.Theme;
            var lesson = Services.Questions.Lesson(q.topic);
            string subject = lesson != null ? lesson.subject : q.subject;
            Color col = LessonCard.SubjectColor(subject);
            var d = q.data;
            bool hasCode = !string.IsNullOrEmpty(d.code);
            int codeLines = hasCode ? d.code.Split('\n').Length : 0;
            bool hasRecap = !string.IsNullOrEmpty(opt.recap);
            // code questions: the question on the left, the code beside it (so the card stays short)
            float codeH = hasCode ? codeLines * 31f + 26f : 0f;
            float bodyH = hasCode ? Mathf.Max(112f, codeH) + 12f : 112f;
            float height = 150 + (hasRecap ? 58 : 0) + bodyH + 2 * 96 + 86;

            // ---------------------------------------------------------------- card
            var root = UIFactory.Rect("QuickQuiz", Services.UI.PanelLayer);
            UIFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0, height + 40), new Vector2(1480, height), new Vector2(0.5f, 1f));
            var card = UIFactory.Panel(root, new Color(0.025f, 0.03f, 0.06f, 0.96f), 26, "Card");
            UIFactory.Stretch(card.rectTransform);
            card.transform.Find("Outline").GetComponent<Image>().color = new Color(col.r, col.g, col.b, 0.55f);
            var glow = UIFactory.Image(card.transform, new Color(col.r, col.g, col.b, 0.12f), UISprites.Glow(), "Glow");
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 50), new Vector2(1400, 220));
            var accent = UIFactory.Image(card.transform, col, UISprites.Rounded(4), "Accent");
            UIFactory.Place(accent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(1400, 6), new Vector2(0.5f, 1f));

            // header: QUIZ BATTLE · subject · topic   |   attack charge
            var chips = UIFactory.Rect("Chips", card.transform);
            UIFactory.Place(chips, new Vector2(0, 1), new Vector2(40, -24), new Vector2(1000, 40), new Vector2(0, 1));
            UIFactory.HLayout(chips, 10, 0, TextAnchor.MiddleLeft, false);
            UIFactory.Chip(chips, opt.number > 1 ? $"QUIZ BATTLE · Q{opt.number}" : "QUIZ BATTLE", th.magenta, 17);
            UIFactory.Chip(chips, "CLASS XI " + subject.ToUpperInvariant(), col, 17);
            UIFactory.Chip(chips, q.concept.ToUpperInvariant(), th.yellow, 17);
            var chargeLbl = UIFactory.Text(card.transform, "ATTACK CHARGING", 16, th.magenta, TextAlignmentOptions.BottomRight, true);
            UIFactory.Place(chargeLbl.rectTransform, new Vector2(1, 1), new Vector2(-40, -20), new Vector2(330, 22), new Vector2(1, 1));
            var timer = UIFactory.Bar(card.transform, th.magenta, new Color(1, 1, 1, 0.08f), "Charge", 8);
            UIFactory.Place(timer.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-40, -46), new Vector2(330, 16), new Vector2(1, 1));
            timer.Set(0, true);

            float y = -78;
            var who = UIFactory.Text(card.transform, $"<color=#ff3dc8>{opt.enemyName}</color> charges <b>{opt.attackName}</b>. Answer right to strike first!", 22, th.textDim, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(who.rectTransform, new Vector2(0, 1), new Vector2(42, y), new Vector2(1400, 32), new Vector2(0, 1));
            y -= 44;
            if (hasRecap)
            {
                var recap = UIFactory.Panel(card.transform, new Color(0.18f, 0.14f, 0.02f, 0.95f), 12, "Recap", false);
                UIFactory.Place(recap.rectTransform, new Vector2(0, 1), new Vector2(40, y), new Vector2(1400, 48), new Vector2(0, 1));
                var rt = UIFactory.Text(recap.transform, $"<color=#ffd640><b>YOU SKIPPED THE LESSON · QUICK RECAP ▸</b></color> {opt.recap}", 20, th.text, TextAlignmentOptions.MidlineLeft);
                UIFactory.Stretch(rt.rectTransform, 16, 2, 16, 2);
                rt.enableAutoSizing = true; rt.fontSizeMin = 14; rt.fontSizeMax = 20;
                y -= 58;
            }
            var prompt = UIFactory.Text(card.transform, q.prompt, 38, th.text, TextAlignmentOptions.TopLeft, false, "Prompt");
            UIFactory.Place(prompt.rectTransform, new Vector2(0, 1), new Vector2(42, y), new Vector2(hasCode ? 600 : 1400, bodyH - 8), new Vector2(0, 1));
            prompt.enableAutoSizing = true; prompt.fontSizeMin = 24; prompt.fontSizeMax = 38 * Services.Settings.TextScale;
            if (hasCode)
            {
                var code = UIFactory.Panel(card.transform, th.code, 12, "Code", false);
                UIFactory.Place(code.rectTransform, new Vector2(0, 1), new Vector2(660, y), new Vector2(780, codeH), new Vector2(0, 1));
                var sb = new System.Text.StringBuilder();
                foreach (var line in d.code.Split('\n')) sb.Append(ChallengeBase.Syntax(line)).Append('\n');
                var ct = UIFactory.Mono(code.transform, sb.ToString().TrimEnd('\n'), 24);
                UIFactory.Stretch(ct.rectTransform, 18, 10, 18, 8);
                ct.textWrappingMode = TextWrappingModes.NoWrap;
            }
            y -= bodyH;

            // answer tiles (shuffled each time)
            var order = new List<int>();
            for (int i = 0; i < d.options.Count && i < 4; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (order[i], order[j]) = (order[j], order[i]); }
            var grid = UIFactory.Rect("Answers", card.transform);
            UIFactory.Place(grid, new Vector2(0, 1), new Vector2(40, y), new Vector2(1400, 2 * 96), new Vector2(0, 1));
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(691, 84); gl.spacing = new Vector2(18, 12);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 2;
            int picked = -1;
            var tiles = new List<Button>();
            var badges = new List<Image>();
            for (int s = 0; s < order.Count; s++)
            {
                int slot = s;
                var b = UIFactory.Button(grid, d.options[order[s]], () => { if (picked < 0) picked = slot; }, ButtonStyle.Answer, 28);
                var lbl = UIFactory.Label(b);
                lbl.alignment = TextAlignmentOptions.MidlineLeft;
                UIFactory.Stretch(lbl.rectTransform, 92, 6, 18, 6);
                lbl.enableAutoSizing = true; lbl.fontSizeMin = 18; lbl.fontSizeMax = 28;
                var badge = UIFactory.Image(b.transform, new Color(col.r, col.g, col.b, 0.9f), UISprites.Rounded(12), "Badge");
                UIFactory.Place(badge.rectTransform, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(62, 58), new Vector2(0, 0.5f));
                var bl = UIFactory.Text(badge.transform, Letters[s], 32, new Color(0.03f, 0.04f, 0.08f), TextAlignmentOptions.Center, true);
                UIFactory.Stretch(bl.rectTransform);
                var keyT = UIFactory.Text(b.transform, InputHub.IsMobile ? "" : $"KEY {s + 1}", 15, th.textDim, TextAlignmentOptions.TopRight, true);
                UIFactory.Stretch(keyT.rectTransform, 0, 6, 12, 0);
                b.gameObject.AddComponent<CanvasGroup>();
                b.transform.localScale = Vector3.zero;
                tiles.Add(b); badges.Add(badge);
            }
            y -= 2 * 96;
            if (DevMode.On)
            {
                // developer mode: the right answer is marked before you pick
                int rs = order.IndexOf(d.answerIndex);
                if (rs >= 0)
                {
                    var bd = tiles[rs].transform.Find("Border")?.GetComponent<Image>();
                    if (bd != null) bd.color = th.yellow;
                    var fx0 = tiles[rs].GetComponent<UIButtonFX>(); if (fx0 != null) fx0.baseBorder = th.yellow;
                    var lb = UIFactory.Label(tiles[rs]);
                    lb.text = "<color=#ffd640>★ DEV</color>  " + lb.text;
                }
            }

            // footer: controls, then the explanation after a wrong answer
            var foot = UIFactory.Text(card.transform, opt.showHint && !string.IsNullOrEmpty(q.hint)
                    ? $"<color=#ffd640>HINT ▸</color> {q.hint}"
                    : "<color=#40ebff>1–4</color> or click to answer  ·  answer fast for a <color=#ffd640>CRITICAL</color> hit",
                21, th.textDim, TextAlignmentOptions.MidlineLeft, false, "Footer");
            UIFactory.Place(foot.rectTransform, new Vector2(0, 0), new Vector2(42, 18), new Vector2(960, 56), new Vector2(0, 0));
            foot.enableAutoSizing = true; foot.fontSizeMin = 15; foot.fontSizeMax = 22;
            // rough paper + calculator (they open below the card; the attack waits while they are open)
            var toolRow = UIFactory.Rect("ScratchButtons", card.transform);
            UIFactory.Place(toolRow, new Vector2(1, 0), new Vector2(-38, 26), new Vector2(440, 40), new Vector2(1, 0));
            ScratchTools.Attach(root, toolRow, false, 1080f - 22f - height - 30f);

            // ---------------------------------------------------------------- in
            Services.Audio.Play("panel_in");
            CameraRig.Instance?.SetQuizOpen(true);
            GlitchFX.Pulse(0.25f, 0.2f);
            yield return Tween.Anchored(root, new Vector2(0, -22), 0.34f, Ease.OutBack);
            for (int s = 0; s < tiles.Count; s++) { Tween.Scale(tiles[s].transform, Vector3.one, 0.22f, Ease.OutBack); Services.Audio.Play("click", 0.4f); yield return new WaitForSecondsRealtime(0.06f); }
            if (!InputHub.IsMobile && tiles.Count > 0) UIFactory.Select(tiles[0]);

            float limit = q.TimeLimit(Services.Settings.Data.relaxedMode) * opt.timeScale;
            float start = Time.unscaledTime, lastTick = 0f;
            bool timedOut = false;
            while (picked < 0)
            {
                if (root == null) { Active = false; yield break; }   // scene changed mid-question
                if (ScratchTools.Open) start += Time.unscaledDeltaTime;   // thinking time on the rough paper is free
                chargeLbl.text = ScratchTools.Open ? "PAUSED · ROUGH WORK" : "ATTACK CHARGING";
                float k = Mathf.Clamp01((Time.unscaledTime - start) / limit);
                timer.Set(k, true);
                ChallengeEngine.RaiseTimer(k);
                if (k > 0.75f && Time.unscaledTime - lastTick > 0.5f) { lastTick = Time.unscaledTime; Services.Audio.Play("timer_tick"); chargeLbl.color = Mathf.Repeat(Time.unscaledTime * 2, 1) < 0.5f ? th.red : th.magenta; }
                if (k >= 1f) { timedOut = true; break; }
                int key = Time.unscaledTime - start > 0.3f && !ScratchTools.Capturing ? KeyPressed() : -1;
                if (key >= 0 && key < tiles.Count) picked = key;
                if (AutoAnswer && Time.unscaledTime - start > 1.8f) picked = order.IndexOf(d.answerIndex);
                yield return null;
            }
            ChallengeEngine.RaiseTimer(0f);
            if (root == null) { Active = false; yield break; }
            float seconds = timedOut ? limit : Time.unscaledTime - start;
            bool correct = !timedOut && order[picked] == d.answerIndex;
            bool fast = correct && seconds <= q.fastSeconds * (Services.Settings.Data.relaxedMode ? 1.5f : 1f);
            foreach (var t in tiles) t.interactable = false;
            if (toolRow != null) toolRow.gameObject.SetActive(false);

            // ---------------------------------------------------------------- reveal
            int right = order.IndexOf(d.answerIndex);
            Color good = Services.Settings.Data.colorBlindSafe ? new Color(0.25f, 0.6f, 1f) : th.green;
            Color bad = Services.Settings.Data.colorBlindSafe ? new Color(1f, 0.55f, 0.1f) : th.red;
            void Mark(int slot, Color c, string mark)
            {
                if (slot < 0 || slot >= tiles.Count) return;
                var img = tiles[slot].GetComponent<Image>();
                img.color = new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, 1f);
                var border = tiles[slot].transform.Find("Border")?.GetComponent<Image>();
                if (border != null) border.color = c;
                var fx = tiles[slot].GetComponent<UIButtonFX>(); if (fx != null) fx.baseBorder = c;
                badges[slot].color = c;
                var l = UIFactory.Label(tiles[slot]);
                l.text = mark + "  " + l.text;
                Tween.Punch(tiles[slot].transform, 0.12f, 0.3f);
            }
            if (right >= 0) Mark(right, good, "✔");
            if (!correct && picked >= 0 && picked != right) Mark(picked, bad, "✖");
            for (int s = 0; s < tiles.Count; s++) if (s != right && s != picked) tiles[s].GetComponent<CanvasGroup>().alpha = 0.35f;

            var stamp = UIFactory.Text(card.transform, timedOut ? "TOO SLOW!" : correct ? (fast ? $"CRITICAL!  {seconds:0.0}s" : $"CORRECT!  {seconds:0.0}s") : "WRONG!", 50,
                correct ? (fast ? th.yellow : good) : bad, TextAlignmentOptions.Center, true);
            UIFactory.Place(stamp.rectTransform, new Vector2(1, 1), new Vector2(-40, -70), new Vector2(520, 70), new Vector2(1, 1));
            stamp.outlineWidth = 0.2f; stamp.outlineColor = new Color32(5, 5, 12, 255);
            stamp.rectTransform.localScale = Vector3.one * 1.7f;
            Tween.Scale(stamp.rectTransform, Vector3.one, 0.25f);
            Services.Audio.Play(correct ? "correct" : "wrong");
            if (!correct) { GlitchFX.Pulse(0.5f, 0.4f); Tween.Punch(card.transform, 0.04f, 0.3f); }

            if (correct) yield return new WaitForSecondsRealtime(fast ? 0.85f : 0.7f);
            else
            {
                // the why, right where the question was asked
                foot.text = $"<color=#ffd640><b>ANSWER ▸</b></color> {d.options[d.answerIndex]}   <color=#9fb0cc>WHY ▸</color> {q.explanation}   <color=#40ebff>[ENTER]</color>";
                foot.color = th.text;
                foot.fontSizeMax = 24;
                float t0 = Time.unscaledTime;
                bool go = false;
                var ok = UIFactory.Button(card.transform, "GOT IT", () => go = true, ButtonStyle.Primary, 22);
                UIFactory.Place(ok.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-30, 18), new Vector2(170, 52), new Vector2(1, 0));
                UIFactory.Place(foot.rectTransform, new Vector2(0, 0), new Vector2(42, 14), new Vector2(1220, 64), new Vector2(0, 0));
                while (!go && Time.unscaledTime - t0 < 9f)
                {
                    if (root == null) { Active = false; yield break; }
                    if (Time.unscaledTime - t0 > 0.8f && ContinuePressed()) go = true;
                    yield return null;
                }
            }

            // ---------------------------------------------------------------- out
            CameraRig.Instance?.SetQuizOpen(false);
            if (root == null) { Active = false; yield break; }   // the scene changed under the card
            yield return Tween.Anchored(root, new Vector2(0, height + 60), 0.24f, Ease.InCubic);
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            Active = false;
            done?.Invoke(new ChallengeOutcome { question = q, correct = correct, fast = fast, timedOut = timedOut, seconds = seconds });
        }
    }
}
