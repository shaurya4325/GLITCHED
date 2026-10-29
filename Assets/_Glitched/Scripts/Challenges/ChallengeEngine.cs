using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Runs one challenge (Section 6.1 / 8): the panel slides in at the bottom (never covering both characters),
    /// the enemy's wind-up bar is the timer, the world keeps moving, and the result comes back as a ChallengeOutcome.
    /// </summary>
    public static class ChallengeEngine
    {
        public static bool Active { get; private set; }
        public static event Action<float> TimerProgress;   // 0..1 wind-up (enemies animate with it)
        public static void RaiseTimer(float k) => TimerProgress?.Invoke(k);   // the quick quiz drives the same wind-up

        public class Options
        {
            public string enemyName;
            public string attackName;
            public bool showHint;
            public float timeScale = 1f;      // < 1 = less time (the tutorial "designed to lose" fight)
            public bool noTimer;
            public string header;
        }

        public static void Run(QuestionData q, string header, float urgency, Action<ChallengeOutcome> done, Options opt = null)
        {
            opt = opt ?? new Options { header = header };
            if (string.IsNullOrEmpty(opt.header)) opt.header = header;
            Services.UI.StartCoroutine(RunRoutine(q, opt, done));
        }

        public static Type TypeFor(ChallengeType t)
        {
            switch (t)
            {
                case ChallengeType.DragDrop: return typeof(DragDropChallenge);
                case ChallengeType.Logic: return typeof(LogicChallenge);
                case ChallengeType.Numerical: return typeof(NumericalChallenge);
                case ChallengeType.Debugging: return typeof(DebuggingChallenge);
                case ChallengeType.Coding: return typeof(CodingChallenge);
                case ChallengeType.Simulation: return typeof(SimulationChallenge);
                case ChallengeType.Matching: return typeof(MatchingChallenge);
                case ChallengeType.Prediction: return typeof(PredictionChallenge);
                case ChallengeType.ShortAnswer: return typeof(ShortAnswerChallenge);
                default: return typeof(ConstructionChallenge);
            }
        }

        static string TypeLabel(ChallengeType t)
        {
            switch (t)
            {
                case ChallengeType.DragDrop: return "DRAG & DROP";
                case ChallengeType.ShortAnswer: return "SHORT ANSWER";
                default: return t.ToString().ToUpperInvariant();
            }
        }

        static IEnumerator RunRoutine(QuestionData q, Options opt, Action<ChallengeOutcome> done)
        {
            while (Active) yield return null;
            Active = true;
            var th = UIFactory.Theme;
            bool tall = q.type == ChallengeType.Simulation || q.type == ChallengeType.Construction || q.type == ChallengeType.Matching || (q.type == ChallengeType.Coding && q.data.mode == "arrange") || (q.type == ChallengeType.Logic && q.data.mode != "choice");
            float height = tall ? 650 : 580;

            var root = UIFactory.Rect("ChallengePanel", Services.UI.PanelLayer);
            root.anchorMin = new Vector2(0.5f, 0); root.anchorMax = new Vector2(0.5f, 0); root.pivot = new Vector2(0.5f, 0);
            root.sizeDelta = new Vector2(1780, height);
            root.anchoredPosition = new Vector2(0, -height - 40);
            var panel = UIFactory.Panel(root, new Color(0.03f, 0.04f, 0.08f, 0.97f), 26, "Panel");
            UIFactory.Stretch(panel.rectTransform);
            var accent = UIFactory.Image(panel.transform, th.magenta, UISprites.Rounded(4), "Accent");
            UIFactory.Place(accent.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(1700, 5), new Vector2(0.5f, 1));

            // header
            var head = UIFactory.Rect("Header", panel.transform);
            UIFactory.Place(head, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(1700, 54), new Vector2(0.5f, 1));
            string title = !string.IsNullOrEmpty(opt.enemyName) ? $"<color=#ff3dc8>{opt.enemyName}</color> uses <b>{opt.attackName}</b>" : opt.header;
            var titleT = UIFactory.Text(head, title, 30, th.text, TextAlignmentOptions.MidlineLeft, true);
            UIFactory.Place(titleT.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(800, 54), new Vector2(0, 0.5f));
            var chips = UIFactory.Rect("Chips", head);
            UIFactory.Place(chips, new Vector2(0, 0.5f), new Vector2(810, 0), new Vector2(560, 44), new Vector2(0, 0.5f));
            UIFactory.HLayout(chips, 8, 0, TextAnchor.MiddleLeft, false);
            UIFactory.Chip(chips, q.concept.ToUpperInvariant(), th.cyan, 18);
            UIFactory.Chip(chips, TypeLabel(q.type), th.yellow, 18);
            UIFactory.Chip(chips, new string('◆', Mathf.Clamp(q.difficulty, 1, 5)), th.magenta, 16);

            var timerBar = UIFactory.Bar(head, th.magenta, new Color(1, 1, 1, 0.08f), "WindUp", 8);
            UIFactory.Place(timerBar.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(0, -6), new Vector2(330, 20), new Vector2(1, 0.5f));
            var timerLbl = UIFactory.Text(head, "ATTACK INCOMING", 16, th.magenta, TextAlignmentOptions.BottomRight, true);
            UIFactory.Place(timerLbl.rectTransform, new Vector2(1, 0.5f), new Vector2(0, 16), new Vector2(330, 24), new Vector2(1, 0.5f));
            timerBar.Set(0, true);

            // prompt
            var promptT = UIFactory.Text(panel.transform, q.prompt, 32, th.text, TextAlignmentOptions.TopLeft);
            UIFactory.Place(promptT.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(1700, 90), new Vector2(0.5f, 1));
            promptT.textWrappingMode = TextWrappingModes.Normal;
            promptT.enableAutoSizing = true; promptT.fontSizeMin = 22; promptT.fontSizeMax = 32 * Services.Settings.TextScale;

            // widget area
            var area = UIFactory.Rect("Area", panel.transform);
            UIFactory.Stretch(area, 40, 180, 40, 96);

            // footer
            var help = UIFactory.Text(panel.transform, "", 22, th.textDim, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(help.rectTransform, new Vector2(0, 0), new Vector2(42, 24), new Vector2(1200, 56), new Vector2(0, 0));
            if (opt.showHint && !string.IsNullOrEmpty(q.hint)) help.text = $"<color=#ffd640>HINT:</color> {q.hint}";
            if (DevMode.On)
            {
                // developer mode: the answer on a strip just above the panel
                var dev = UIFactory.Panel(root, new Color(0.16f, 0.13f, 0.02f, 0.95f), 14, "DevAnswer");
                UIFactory.Place(dev.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 12), new Vector2(1500, 58), new Vector2(0.5f, 0));
                var dt = UIFactory.Text(dev.transform, "<color=#ffd640><b>DEV ANSWER ▸</b></color> " + q.CorrectAnswerText(), 24, th.text, TextAlignmentOptions.MidlineLeft);
                UIFactory.Stretch(dt.rectTransform, 18, 4, 18, 4);
                dt.enableAutoSizing = true; dt.fontSizeMin = 14; dt.fontSizeMax = 24;
            }
            // rough paper + calculator (they open above the panel; the attack waits while they are open)
            var toolRow = UIFactory.Rect("ScratchButtons", panel.transform);
            UIFactory.Place(toolRow, new Vector2(1, 0), new Vector2(-356, 32), new Vector2(440, 40), new Vector2(1, 0));
            UIFactory.Place(help.rectTransform, new Vector2(0, 0), new Vector2(42, 24), new Vector2(900, 56), new Vector2(0, 0));
            ScratchTools.Attach(root, toolRow, true, 1080f - 14f - height - (DevMode.On ? 90f : 30f));
            var submit = UIFactory.Button(panel.transform, "SUBMIT  ⏎", null, ButtonStyle.Primary, 28);
            UIFactory.Place(submit.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-36, 18), new Vector2(300, 70), new Vector2(1, 0));

            var ctx = new ChallengeContext
            {
                startTime = Time.unscaledTime,
                timeLimit = q.TimeLimit(Services.Settings.Data.relaxedMode) * opt.timeScale,
                showHint = opt.showHint,
                relaxed = Services.Settings.Data.relaxedMode,
                setHelp = s => { if (!(opt.showHint && !string.IsNullOrEmpty(q.hint))) help.text = s; }
            };

            var widget = (ChallengeBase)area.gameObject.AddComponent(TypeFor(q.type));
            bool finished = false, correct = false; float seconds = 0;
            widget.Result += (c, t) => { finished = true; correct = c; seconds = t; };
            try { widget.Setup(q, area, ctx); }
            catch (Exception e) { Debug.LogException(e); finished = true; correct = true; }
            submit.onClick.AddListener(() => { if (widget.CanSubmit && !finished) widget.OnSubmit(); });
            submit.gameObject.SetActive(!widget.AutoSubmits);
            UIFactory.Label(submit).text = widget.SubmitLabel;

            // slide in
            Services.Audio.Play("panel_in");
            CameraRig.Instance?.SetPanelOpen(true);
            Services.UI.Dialogue.SetTop(true);
            yield return Tween.Anchored(root, new Vector2(0, 14), 0.32f, Ease.OutBack);
            ctx.startTime = Time.unscaledTime;

            bool timedOut = false;
            float lastTick = 0;
            while (!finished)
            {
                if (ScratchTools.Open) ctx.startTime += Time.unscaledDeltaTime;   // thinking time on the rough paper is free
                timerLbl.text = ScratchTools.Open ? "PAUSED · ROUGH WORK" : "ATTACK INCOMING";
                float k = opt.noTimer ? 0 : Mathf.Clamp01(ctx.Elapsed / ctx.timeLimit);
                timerBar.Set(k, true);
                TimerProgress?.Invoke(k);
                if (!opt.noTimer && k > 0.75f && Time.unscaledTime - lastTick > 0.5f) { lastTick = Time.unscaledTime; Services.Audio.Play("timer_tick"); timerLbl.color = Mathf.Repeat(Time.unscaledTime * 2, 1) < 0.5f ? th.red : th.magenta; }
                if (!opt.noTimer && k >= 1f) { timedOut = true; break; }
                if (!ScratchTools.Capturing)
                {
                    for (int i = 0; i < 4; i++) if (Services.Input.AnswerPressed(i)) widget.OnAnswerKey(i);
                    if (Services.Input.SubmitPressed && widget.CanSubmit && !widget.AutoSubmits) widget.OnSubmit();
                }
                if (submit == null) { Active = false; yield break; } // panel destroyed (scene changed mid-challenge)
                if (QuickQuiz.AutoAnswer && ctx.Elapsed > 1.6f) { finished = true; correct = true; seconds = ctx.Elapsed; break; }   // test bot only
                submit.interactable = widget.CanSubmit;
                yield return null;
            }
            TimerProgress?.Invoke(0);
            if (root == null) { Active = false; CameraRig.Instance?.SetPanelOpen(false); yield break; }   // the scene changed under the panel
            if (timedOut) { correct = false; seconds = ctx.timeLimit; }
            submit.interactable = false;
            toolRow.gameObject.SetActive(false);
            widget.Reveal(correct);
            bool fast = correct && seconds <= q.fastSeconds * (ctx.relaxed ? 1.5f : 1f);

            // stamp
            var stamp = UIFactory.Text(panel.transform, timedOut ? "TOO SLOW!" : correct ? (fast ? $"CORRECT  {seconds:0.0}s  FAST!" : $"CORRECT  {seconds:0.0}s") : "WRONG!", 52,
                correct ? (fast ? th.yellow : th.green) : th.red, TextAlignmentOptions.Center, true);
            UIFactory.Place(stamp.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 40), new Vector2(1200, 90), new Vector2(0.5f, 0));
            stamp.outlineWidth = 0.2f; stamp.outlineColor = new Color32(5, 5, 12, 255);
            stamp.rectTransform.localScale = Vector3.one * 1.6f;
            Tween.Scale(stamp.rectTransform, Vector3.one, 0.25f);
            Services.Audio.Play(correct ? "correct" : "wrong");
            if (!correct) GlitchFX.Pulse(0.5f, 0.4f);

            yield return new WaitForSecondsRealtime(0.7f + widget.PlaybackSeconds);
            if (root == null) { Active = false; yield break; }
            yield return Tween.Anchored(root, new Vector2(0, -height - 60), 0.25f, Ease.InCubic);
            UnityEngine.Object.Destroy(root.gameObject);
            CameraRig.Instance?.SetPanelOpen(false);
            Services.UI.Dialogue.SetTop(false);
            Active = false;
            done?.Invoke(new ChallengeOutcome { question = q, correct = correct, fast = fast, timedOut = timedOut, seconds = seconds });
        }

        // ------------------------------------------------------------------ explanation after a wrong answer
        /// <summary>Shows the correct answer, a one-line why and an optional "Show me" demo (Section 7).</summary>
        public static IEnumerator ShowExplanation(QuestionData q, ChallengeOutcome o, string intro = "ENEMY TURN")
        {
            var th = UIFactory.Theme;
            var root = UIFactory.Rect("Explanation", Services.UI.PanelLayer);
            UIFactory.Place(root, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1500, 380), new Vector2(0.5f, 0));
            var p = UIFactory.Panel(root, new Color(0.06f, 0.03f, 0.07f, 0.97f), 24, "Panel");
            UIFactory.Stretch(p.rectTransform);
            var b = UIFactory.Image(p.transform, th.red, UISprites.Rounded(4), "Accent");
            UIFactory.Place(b.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(1420, 5), new Vector2(0.5f, 1));
            var h = UIFactory.Text(p.transform, intro, 30, th.red, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(h.rectTransform, new Vector2(0, 1), new Vector2(40, -22), new Vector2(1400, 50), new Vector2(0, 1));
            string ans = q.CorrectAnswerText();
            var body = UIFactory.Text(p.transform,
                (string.IsNullOrEmpty(ans) ? "" : $"<color=#9fb0cc>Correct answer:</color> <b>{ans}</b>\n") +
                $"<color=#9fb0cc>Why:</color> {q.explanation}", 30, th.text, TextAlignmentOptions.TopLeft);
            UIFactory.Place(body.rectTransform, new Vector2(0, 1), new Vector2(40, -80), new Vector2(1420, 190), new Vector2(0, 1));
            body.enableAutoSizing = true; body.fontSizeMin = 20; body.fontSizeMax = 30;
            bool close = false, showMe = false;
            var row = UIFactory.Rect("Buttons", p.transform);
            UIFactory.Place(row, new Vector2(1, 0), new Vector2(-30, 22), new Vector2(700, 76), new Vector2(1, 0));
            UIFactory.HLayout(row, 16);
            if (TeachingMode.HasModule(q)) UIFactory.Button(row, "SHOW ME", () => showMe = true, ButtonStyle.Secondary, 26);
            var ok = UIFactory.Button(row, "GOT IT", () => close = true, ButtonStyle.Primary, 26);
            UIFactory.Select(ok);
            root.localScale = new Vector3(1, 0.7f, 1);
            Tween.Scale(root, Vector3.one, 0.25f);
            float t0 = Time.unscaledTime;
            while (!close)
            {
                if (showMe)
                {
                    showMe = false;
                    root.gameObject.SetActive(false);
                    yield return TeachingMode.QuickDemo(q);
                    root.gameObject.SetActive(true);
                }
                if (Time.unscaledTime - t0 > 0.4f && Services.Input.SubmitPressed) close = true;
                yield return null;
            }
            UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
