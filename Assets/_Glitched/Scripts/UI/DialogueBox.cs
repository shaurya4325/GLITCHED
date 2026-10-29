using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The dialogue box: speaker name, portrait, glitch-text typewriter and gibberish voice blips.
    /// Tap/click/Space advances (first press completes the line); hold Esc to skip a whole conversation.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        RectTransform box;
        CanvasGroup group;
        TextMeshProUGUI nameText, lineText, hint;
        Image portraitBg, portrait, nameChip;
        GlitchText glitch;
        RectTransform choiceRow;
        public bool Open { get; private set; }
        public bool SkipRequested { get; private set; }
        float holdSkip;
        float blipPitch = 1f;
        bool voiced;                // a recorded voice line is playing: no blips
        float baseCps = -1f;
        string blipKey = "blip";

        public void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false;
            var panel = UIFactory.Panel(transform, new Color(0.03f, 0.04f, 0.08f, 0.96f), 22, "Box");
            box = panel.rectTransform;
            SetTop(false);
            portraitBg = UIFactory.Image(box, new Color(0.08f, 0.1f, 0.18f, 1f), UISprites.Rounded(18), "PortraitBg");
            UIFactory.Place(portraitBg.rectTransform, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(170, 170), new Vector2(0, 0.5f));
            portrait = UIFactory.Image(portraitBg.transform, Color.white, null, "Portrait");
            UIFactory.Stretch(portrait.rectTransform, 8, 8, 8, 8);
            portrait.preserveAspect = true;
            nameChip = UIFactory.Image(box, UIFactory.Theme.cyan, UISprites.Rounded(12), "NameChip");
            UIFactory.Place(nameChip.rectTransform, new Vector2(0, 1), new Vector2(216, 22), new Vector2(380, 50), new Vector2(0, 0.5f));
            nameText = UIFactory.Text(nameChip.transform, "", 26, new Color(0.02f, 0.03f, 0.06f), TextAlignmentOptions.Center, true);
            UIFactory.Stretch(nameText.rectTransform, 10, 0, 10, 0);
            lineText = UIFactory.Text(box, "", 34, UIFactory.Theme.text, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(lineText.rectTransform, 220, 42, 40, 30);
            glitch = lineText.gameObject.AddComponent<GlitchText>();
            glitch.onCharacter = () => { if (!voiced) Services.Audio.PlayBlip(blipKey, blipPitch); };
            hint = UIFactory.Text(box, "▼", 26, UIFactory.Theme.cyan, TextAlignmentOptions.BottomRight, true);
            UIFactory.Stretch(hint.rectTransform, 0, 0, 24, 14);
            choiceRow = UIFactory.Rect("Choices", box);
            UIFactory.Place(choiceRow, new Vector2(0.5f, 1), new Vector2(0, 90), new Vector2(1300, 80), new Vector2(0.5f, 0));
            UIFactory.HLayout(choiceRow, 18);
        }

        /// <summary>Puts the box at the top of the screen (while a challenge panel uses the bottom).</summary>
        public void SetTop(bool top)
        {
            if (top) UIFactory.Place(box, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1500, 230), new Vector2(0.5f, 1));
            else UIFactory.Place(box, new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(1500, 250), new Vector2(0.5f, 0));
        }

        public void Show(bool on)
        {
            if (on == Open) return;
            Open = on;
            group.blocksRaycasts = on;
            Tween.Fade(group, on ? 1 : 0, 0.18f);
            if (on) { box.localScale = new Vector3(1, 0.85f, 1); Tween.Scale(box, Vector3.one, 0.22f, Ease.OutBack); SkipRequested = false; }
        }

        void ApplySpeaker(string speaker, string expression)
        {
            var db = Services.Db;
            var prof = db.Character(speaker) ?? db.characters.Find(c => c != null && c.displayName == speaker);
            string display = prof != null ? prof.displayName : speaker;
            if (prof != null && prof.id == "aarav") display = Services.Game.Data.playerName;
            nameText.text = display.ToUpperInvariant();
            Color col = prof != null ? prof.primaryColor : UIFactory.Theme.magenta;
            if (prof != null && prof.id == "aarav") col = new Color(0.45f, 0.7f, 1f);
            nameChip.color = Color.Lerp(col, Color.white, 0.25f);
            blipPitch = prof != null ? prof.voicePitch : 1f;
            blipKey = prof != null && !string.IsNullOrEmpty(prof.voiceBlipKey) ? prof.voiceBlipKey : "blip";
            portraitBg.color = new Color(col.r * 0.25f, col.g * 0.25f, col.b * 0.3f, 1f);

            Texture2D face = null;
            if (prof != null && !string.IsNullOrEmpty(prof.faceFolder))
            {
                string exp = string.IsNullOrEmpty(expression) ? (prof.id == "pixel" ? "normal" : "neutral") : expression;
                string prefix = prof.id == "pixel" ? "eye_" : "face_";
                face = Resources.Load<Texture2D>($"{prof.faceFolder}/{prefix}{exp}") ?? Resources.Load<Texture2D>($"{prof.faceFolder}/{prefix}{(prof.id == "pixel" ? "normal" : "neutral")}");
            }
            else if (prof != null) face = Resources.Load<Texture2D>("Portraits/" + prof.id);
            portrait.sprite = face != null ? Sprite.Create(face, new Rect(0, 0, face.width, face.height), new Vector2(0.5f, 0.5f)) : UISprites.Circle(128);
            portrait.color = face != null ? Color.white : col;
        }

        /// <summary>Shows one line and waits for the player to advance. Returns early if the conversation is skipped.</summary>
        public IEnumerator Say(string speaker, string line, string expression = null, float autoAdvance = 0f)
        {
            if (SkipRequested) yield break;
            Show(true);
            ApplySpeaker(speaker, expression);
            choiceRow.gameObject.SetActive(false);
            string text = line;
            if (Services.Corruption.Value > 0.5f && speaker != "pixel") text = GlitchText.Stutter(text, 0.06f * Services.Corruption.Value);
            // recorded voice for this line? play it and pace the typewriter to it
            voiced = false;
            if (baseCps < 0) baseCps = glitch.charsPerSecond;
            glitch.charsPerSecond = baseCps;
            voiced = Services.Audio.SpeakLine(speaker, line, clip =>
                glitch.charsPerSecond = Mathf.Clamp(text.Length / Mathf.Max(0.4f, clip.length * 0.92f), 12f, 90f));
            glitch.Show(text);
            hint.alpha = 0;
            yield return null;
            float waited = 0;
            while (true)
            {
                if (SkipRequested) yield break;
                if (Services.Input.AdvancePressed && !Services.UI.Pause.IsOpen)
                {
                    if (!glitch.Finished) glitch.Complete();
                    else break;
                }
                if (glitch.Finished)
                {
                    hint.alpha = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                    waited += Time.unscaledDeltaTime;
                    if (autoAdvance > 0 && waited >= autoAdvance) break;
                    if (CaptureRunner.Active && waited >= 1.2f) break;   // automated screenshot runs play cutscenes through
                }
                yield return null;
            }
            if (voiced) Services.Audio.StopVoice();
            voiced = false;
            Services.Audio.Play("dialogue_next");
        }

        public IEnumerator Choose(string speaker, string line, IList<string> options, Action<int> picked)
        {
            Show(true);
            ApplySpeaker(speaker, null);
            glitch.Show(line, true);
            UIFactory.Clear(choiceRow);
            choiceRow.gameObject.SetActive(true);
            int result = -1;
            Button first = null;
            for (int i = 0; i < options.Count; i++)
            {
                int k = i;
                var b = UIFactory.Button(choiceRow, options[i], () => result = k, ButtonStyle.Answer, 26);
                if (first == null) first = b;
            }
            UIFactory.Select(first);
            while (result < 0) yield return null;
            choiceRow.gameObject.SetActive(false);
            picked?.Invoke(result);
        }

        void Update()
        {
            if (!Open) { holdSkip = 0; return; }
            if (Services.Input.PauseHeld) holdSkip += Time.unscaledDeltaTime; else holdSkip = 0;
            if (holdSkip > 0.6f) SkipRequested = true;
        }

        public void ClearSkip() => SkipRequested = false;
    }
}
