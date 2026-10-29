using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Hover/select feedback for buttons: a small scale pop, accent border and a quick label glitch.</summary>
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image border;
        public TextMeshProUGUI label;
        public Color baseBorder;
        float target = 1f, scale = 1f;
        bool hot;
        string original;
        float glitchUntil;

        public void OnPointerEnter(PointerEventData e) => Hot(true);
        public void OnPointerExit(PointerEventData e) => Hot(false);
        public void OnSelect(BaseEventData e) => Hot(true);
        public void OnDeselect(BaseEventData e) => Hot(false);
        public void OnPointerDown(PointerEventData e) => target = 0.96f;
        public void OnPointerUp(PointerEventData e) => target = hot ? 1.04f : 1f;

        void Hot(bool on)
        {
            if (on == hot) return;
            hot = on;
            target = on ? 1.04f : 1f;
            if (border != null) border.color = on ? UIFactory.Theme.magenta : baseBorder;
            if (on)
            {
                Services.Audio.Play("hover");
                if (label != null) { original = label.text; glitchUntil = Time.unscaledTime + 0.12f; }
            }
        }

        void OnDisable()
        {
            if (label != null && !string.IsNullOrEmpty(original) && Time.unscaledTime < glitchUntil) label.text = original;
            hot = false; scale = target = 1f; transform.localScale = Vector3.one;
            if (border != null) border.color = baseBorder;
        }

        void Update()
        {
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            transform.localScale = new Vector3(scale, scale, 1);
            if (label != null && !string.IsNullOrEmpty(original))
            {
                if (Time.unscaledTime < glitchUntil) label.text = GlitchText.Scramble(original, 0.35f);
                else if (label.text != original) { label.text = original; original = null; }
            }
        }
    }
}
