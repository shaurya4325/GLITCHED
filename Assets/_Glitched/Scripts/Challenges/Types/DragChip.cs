using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Drag behaviour for an item chip: a ghost follows the pointer and drops on a slot.</summary>
    public class DragChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public DragDropChallenge owner;
        public int index;
        RectTransform ghost;

        public void OnBeginDrag(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            ghost = (RectTransform)Instantiate(gameObject, Services.UI.PopupLayer).transform;
            foreach (var c in ghost.GetComponentsInChildren<MonoBehaviour>()) if (c is DragChip || c is Button) Destroy(c);
            var cg = ghost.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.alpha = 0.85f;
            ghost.sizeDelta = ((RectTransform)transform).rect.size;
            Services.Audio.Play("pickup");
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (ghost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent, e.position, null, out var p);
            ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.anchoredPosition = p;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (ghost == null) return;
            Destroy(ghost.gameObject);
            ghost = null;
            int s = owner.SlotUnder(e.position);
            if (s >= 0) owner.Place(index, s);
        }
    }
}
