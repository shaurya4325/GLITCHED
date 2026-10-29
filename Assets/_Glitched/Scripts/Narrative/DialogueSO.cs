using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>A written conversation: speaker, line, expression, sound and optional choices.</summary>
    [CreateAssetMenu(menuName = "Glitched/Dialogue", fileName = "Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Character id: aarav, pixel, lord_loop, npc ... or any display name.")]
        public string speaker;
        [TextArea] public string text;
        [Tooltip("Face expression to show while speaking (neutral, surprised, smirk, cool, flustered, scared, defeated, sweating, melting).")]
        public string expression;
        public string sound;
        public List<string> choices = new List<string>();

        public DialogueLine() { }
        public DialogueLine(string speaker, string text, string expression = null)
        {
            this.speaker = speaker; this.text = text; this.expression = expression;
        }
    }
}
