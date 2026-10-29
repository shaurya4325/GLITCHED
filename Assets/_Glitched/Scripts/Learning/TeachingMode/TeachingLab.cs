using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Base of the four teaching labs: frame, villain/companion commentary, goal line and the DONE button.</summary>
    public abstract class TeachingLab : MonoBehaviour
    {
        protected QuestionData Q;
        protected BossDataSO Boss;
        protected RectTransform Content;
        TextMeshProUGUI speakerName, speech, goalText;
        GlitchText speechGlitch;
        Button done;
        bool requireSolve;
        int lineIndex;
        float nextLine;
        public bool Closed { get; private set; }
        protected bool Solved { get; private set; }
        protected int Interactions;

        public void Init(QuestionData q, BossDataSO boss, bool mustSolve)
        {
            Q = q; Boss = boss; requireSolve = mustSolve;
            var th = UIFactory.Theme;
            UIFactory.Blocker(transform, 0.55f);
            var panel = UIFactory.Panel(transform, new Color(0.03f, 0.05f, 0.09f, 0.98f), 26, "Lab");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1760, 900));
            var stripes = UIFactory.Image(panel.transform, new Color(th.yellow.r, th.yellow.g, th.yellow.b, 0.18f), UISprites.Stripes(), "Stripes");
            stripes.type = Image.Type.Tiled;
            UIFactory.Place(stripes.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(1700, 12), new Vector2(0.5f, 1));
            var title = UIFactory.Text(panel.transform, (boss != null ? "TEACHING MODE: " : "SHOW ME: ") + Title, 40, th.yellow, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(40, -26), new Vector2(1200, 60), new Vector2(0, 1));
            var conceptChip = UIFactory.Chip(panel.transform, Q != null ? Q.concept.ToUpperInvariant() : "", th.cyan, 20);
            UIFactory.Place(conceptChip.rectTransform, new Vector2(1, 1), new Vector2(-40, -34), new Vector2(240, 40), new Vector2(1, 1));

            // speech line (the villain teaches, or Pixel in quick demos)
            var sp = UIFactory.Panel(panel.transform, new Color(0.07f, 0.04f, 0.1f, 1f), 16, "Speech", false);
            UIFactory.Place(sp.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(1680, 110), new Vector2(0.5f, 1));
            speakerName = UIFactory.Text(sp.transform, boss != null ? boss.displayName : "PIXEL", 20, boss != null ? th.magenta : th.cyan, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(speakerName.rectTransform, new Vector2(0, 1), new Vector2(20, -8), new Vector2(800, 26), new Vector2(0, 1));
            speech = UIFactory.Text(sp.transform, "", 28, th.text, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(speech.rectTransform, 20, 36, 20, 8);
            speechGlitch = speech.gameObject.AddComponent<GlitchText>();
            speechGlitch.intensityOverride = boss != null ? 0.2f : 0.05f;
            speechGlitch.onCharacter = () => Services.Audio.PlayBlip(boss != null ? "blip" : "blip_pixel", boss != null ? 0.6f : 1.8f);

            Content = UIFactory.Rect("Content", panel.transform);
            UIFactory.Stretch(Content, 40, 226, 40, 110);

            goalText = UIFactory.Text(panel.transform, "", 26, th.textDim, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(goalText.rectTransform, new Vector2(0, 0), new Vector2(40, 26), new Vector2(1300, 60), new Vector2(0, 0));
            done = UIFactory.Button(panel.transform, boss != null ? "I GET IT. REMATCH!" : "GOT IT", Close, ButtonStyle.Primary, 26);
            UIFactory.Place(done.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(-36, 20), new Vector2(380, 76), new Vector2(1, 0));
            panel.transform.localScale = Vector3.one * 0.92f;
            Tween.Scale(panel.transform, Vector3.one, 0.3f);
            GlitchFX.Pulse(0.6f, 0.5f);
            Services.Audio.Play("lab_open");

            BuildLab(Content);
            Say(Opening());
            RefreshGoal();
        }

        protected abstract string Title { get; }
        protected abstract string GoalDescription { get; }
        protected abstract void BuildLab(RectTransform area);
        protected abstract string Opening();

        protected virtual List<string> Lines()
        {
            if (Boss != null && Boss.teachingLines != null && Boss.teachingLines.Count > 0) return Boss.teachingLines;
            return new List<string>();
        }

        protected void Say(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            speechGlitch.Show(text);
            Services.Audio.SpeakLine(Boss != null ? VoiceBank.SpeakerOf(Boss) : "pixel", text);
            nextLine = Time.unscaledTime + 7f + text.Length / 30f;
        }

        protected void MarkSolved(string message)
        {
            if (Solved) return;
            Solved = true;
            Services.Audio.Play("correct");
            Say(message);
            RefreshGoal();
            Tween.Punch(done.transform, 0.2f, 0.4f);
        }

        protected void RefreshGoal()
        {
            goalText.text = (Solved ? "<color=#7cffa0>✔ GOAL COMPLETE</color>   " : "<color=#ffd640>GOAL:</color> ") + GoalDescription;
            done.interactable = Solved || !requireSolve || Interactions >= 8;
        }

        protected void Interacted()
        {
            Interactions++;
            if (!Solved) RefreshGoal();
        }

        protected virtual void Update()
        {
            var lines = Lines();
            if (lines.Count > 0 && Time.unscaledTime > nextLine && !Solved && lineIndex < lines.Count)
                Say(lines[lineIndex++]);
            if (Services.Input.SubmitPressed && done.interactable) Close();
        }

        void Close()
        {
            if (Closed) return;
            Closed = true;
            Services.Audio.Play("lab_close");
        }
    }
}
