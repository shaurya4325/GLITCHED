using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Drag handler for a ReorderList row.</summary>
    public class ReorderRowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ReorderList list;
        RectTransform rt;
        Vector2 grabOffset;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!enabled) return;
            rt = (RectTransform)transform;
            rt.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(list.RowRect, e.position, e.pressEventCamera, out var p);
            grabOffset = rt.anchoredPosition - p;
            rt.localScale = Vector3.one * 1.03f;
            Services.Audio.Play("pickup", 0.6f);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!enabled || rt == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(list.RowRect, e.position, e.pressEventCamera, out var p);
            var pos = new Vector2(0, (p + grabOffset).y);
            rt.anchoredPosition = pos;
            list.DragTo(rt, pos.y);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (rt == null) return;
            rt.localScale = Vector3.one;
            list.Layout(false);
            Services.Audio.Play("drop", 0.6f);
        }
    }
}
