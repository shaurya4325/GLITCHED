using UnityEngine;
using UnityEngine.EventSystems;

namespace Glitched
{
    /// <summary>Lets a touch control be dragged around while the layout editor is on.</summary>
    public class TouchDraggable : MonoBehaviour, IDragHandler, IScrollHandler
    {
        TouchControls owner;
        string id;
        public void Init(TouchControls o, string i) { owner = o; id = i; }

        public void OnDrag(PointerEventData e)
        {
            if (owner == null || !owner.EditMode) return;
            var rt = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            rt.anchoredPosition += e.delta / (canvas != null ? canvas.scaleFactor : 1f);
            owner.Store(id, rt.anchoredPosition, rt.localScale.x / Mathf.Max(0.01f, Services.Settings.Data.touchButtonScale));
        }

        public void OnScroll(PointerEventData e)
        {
            if (owner == null || !owner.EditMode) return;
            var rt = (RectTransform)transform;
            float s = Mathf.Clamp(rt.localScale.x + e.scrollDelta.y * 0.05f, 0.6f, 1.8f);
            rt.localScale = Vector3.one * s;
            owner.Store(id, rt.anchoredPosition, s / Mathf.Max(0.01f, Services.Settings.Data.touchButtonScale));
        }
    }
}
