using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A lesson terminal: teaches one CBSE Class XI topic (LessonCard). The small enemies after it, up to the next
    /// lesson terminal, ask MCQs on that topic only.
    /// </summary>
    public class LessonTerminal : Interactable
    {
        public string topic;
        public string pixelComment = "";
        bool reading;

        LessonData Lesson => Services.Questions.Lesson(topic);

        void Start()
        {
            radius = 2.2f;
            var l = Lesson;
            prompt = l != null ? $"Study: <b>{l.title}</b>" : "Read terminal";
        }

        public override string PromptText
        {
            get
            {
                var l = Lesson;
                if (l == null) return prompt;
                bool read = Services.Game.Data.HasFlag("lesson_" + l.id);
                return (read ? "Review" : "Study") + $": <b>{l.title}</b>  <color=#9fb0cc>(Class XI {l.subject})</color>";
            }
        }

        public override void Interact(PlayerController p)
        {
            if (!reading && Lesson != null) StartCoroutine(Read(p));
        }

        IEnumerator Read(PlayerController p)
        {
            reading = true;
            var l = Lesson;
            p.Lock(this);
            Services.Audio.Play("terminal");
            Services.UI.Hud.HidePrompt();
            yield return LessonCard.Show(l);
            bool first = !Services.Game.Data.HasFlag("lesson_" + l.id);
            Services.Game.Data.SetFlag("lesson_" + l.id);
            Services.UI.Hud.SetTopic(l);
            if (first)
            {
                Services.Game.AddXP(15);
                Services.UI.Toast($"LESSON LEARNED: {l.title.ToUpperInvariant()}");
            }
            string line = !string.IsNullOrEmpty(pixelComment) ? pixelComment : l.pixel;
            if (!string.IsNullOrEmpty(line)) Companion.Say(line, 4f);
            Services.Save.Write();
            p.Unlock(this);
            reading = false;
        }

        void OnTriggerEnter(Collider other)
        {
            // walking past shows what's taught here
            if (other.CompareTag("Player") && Lesson != null) Services.UI.Hud.SetTopic(Lesson);
        }
    }
}
