using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    public sealed class MergeBoardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UIFlowController controller;
        private int cell;
        private RectTransform ghost;
        public void Initialize(UIFlowController flow, int index) { controller = flow; cell = index; }
        public void OnBeginDrag(PointerEventData e)
        {
            if (controller.Game.Board.At(cell) == null || controller.Game.Board.IsLocked(cell)) return;
            var parent = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            ghost = new GameObject("Dragged tea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<RectTransform>();
            ghost.SetParent(parent, false); ghost.sizeDelta = new Vector2(64, 64);
            var original = GetComponentInChildren<Text>(); var text = ghost.GetComponent<Text>();
            text.text = original.text; text.font = original.font; text.fontSize = original.fontSize;
            text.color = original.color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (ghost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent, e.position, e.pressEventCamera, out var local)) ghost.anchoredPosition = local;
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (ghost == null) return;
            Destroy(ghost.gameObject); ghost = null;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, hits);
            foreach (var hit in hits)
            {
                var target = hit.gameObject.GetComponentInParent<MergeBoardView>();
                if (target != null && target.controller == controller) { controller.DropItem(cell, target.cell); return; }
            }
        }
        private void OnDisable() { if (ghost != null) Destroy(ghost.gameObject); ghost = null; }
    }
}
