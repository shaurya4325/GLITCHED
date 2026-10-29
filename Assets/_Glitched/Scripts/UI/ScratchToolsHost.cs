using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Glitched
{
    /// <summary>
    /// Lives on the ScratchTools panel of a question screen: R / C open the rough paper and the calculator, the
    /// keyboard types into the calculator while it is open, and the tools close when the question goes away.
    /// </summary>
    public class ScratchToolsHost : MonoBehaviour
    {
        void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnChar; }
        void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnChar; }
        void OnDestroy() => ScratchTools.Detach();

        /// <summary>A text box (a coding challenge) has the keyboard.</summary>
        static bool Typing()
        {
            var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return sel != null && sel.GetComponent<TMP_InputField>() is TMP_InputField f && f.isFocused;
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || Typing()) return;
            if (k.rKey.wasPressedThisFrame) ScratchTools.Toggle(true);
            if (k.cKey.wasPressedThisFrame) ScratchTools.Toggle(false);
            if (!ScratchTools.Capturing) return;
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) ScratchTools.Press("=");
            if (k.backspaceKey.wasPressedThisFrame) ScratchTools.Press("⌫");
            if (k.deleteKey.wasPressedThisFrame) ScratchTools.Press("C");
        }

        void OnChar(char c)
        {
            if (!ScratchTools.Capturing || Typing()) return;
            switch (c)
            {
                case '+': ScratchTools.Press("+"); break;
                case '-': ScratchTools.Press("−"); break;
                case '*': case 'x': ScratchTools.Press("×"); break;
                case '/': ScratchTools.Press("÷"); break;
                case '^': ScratchTools.Press("^"); break;
                case '(': case ')': case '.': ScratchTools.Press(c.ToString()); break;
                case '=': ScratchTools.Press("="); break;
                case 'e': case 'E': ScratchTools.Press("E"); break;
                case 'p': ScratchTools.Press("π"); break;
                default: if (char.IsDigit(c)) ScratchTools.Press(c.ToString()); break;
            }
        }
    }
}
