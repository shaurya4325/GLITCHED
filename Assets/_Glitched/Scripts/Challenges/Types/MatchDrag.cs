using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>Drag from a left item to a right item to link them.</summary>
    public class MatchDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public MatchingChallenge owner;
        public int left;
        public void OnBeginDrag(PointerEventData e) => owner.DrawDrag(left, e.position);
        public void OnDrag(PointerEventData e) => owner.DrawDrag(left, e.position);
        public void OnEndDrag(PointerEventData e)
        {
            int r = owner.RightUnder(e.position);
            if (r >= 0) owner.Link(left, r); else owner.EndDrag();
        }
    }
}
