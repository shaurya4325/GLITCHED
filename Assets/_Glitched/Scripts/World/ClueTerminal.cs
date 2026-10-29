using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>A terminal holding a short clue about the concept that the next fight needs.</summary>
    public class ClueTerminal : Interactable
    {
        public string title = "TERMINAL";
        [TextArea] public string text = "";
        public string pixelComment = "";
        bool reading;

        void Start() { prompt = "Read terminal"; }

        public override void Interact(PlayerController p)
        {
            if (!reading) StartCoroutine(Read(p));
        }

        IEnumerator Read(PlayerController p)
        {
            reading = true;
            p.Lock(this);
            Services.Audio.Play("terminal");
            yield return Services.UI.Dialogue.Say(title, text);
            if (!string.IsNullOrEmpty(pixelComment)) yield return Services.UI.Dialogue.Say("pixel", pixelComment);
            Services.UI.Dialogue.Show(false);
            if (!used) { used = true; Services.Game.AddXP(10); }
            p.Unlock(this);
            reading = false;
        }
    }
}
