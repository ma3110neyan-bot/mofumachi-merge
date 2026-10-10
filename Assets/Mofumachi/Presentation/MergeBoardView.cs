using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class MergeBoardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UIFlowController controller;private int cell;private RectTransform ghost;private int suppressThrough=-1;
        public bool SuppressClick=>ghost!=null || Time.frameCount<=suppressThrough;
        public void Initialize(UIFlowController flow,int index){controller=flow;cell=index;}
        public void OnBeginDrag(PointerEventData e)
        {
            if(controller.Game.Board.At(cell)==null || controller.Game.Board.IsLocked(cell))return;
            ClearGhost();e.eligibleForClick=false;suppressThrough=Time.frameCount+1;
            var parent=(RectTransform)GetComponentInParent<Canvas>().transform;
            ghost=UIWidgets.Node("Dragged tea",parent,new Rect(.5f,.5f,0,0));ghost.sizeDelta=new Vector2(56,56);
            var icon=ghost.gameObject.AddComponent<UIIconGraphic>();icon.icon=controller.Game.Board.At(cell).level>=2?UIIcon.TeaPot:UIIcon.Tea;icon.color=new Color(1,1,1,.9f);icon.raycastTarget=false;OnDrag(e);
        }
        public void OnDrag(PointerEventData e){if(ghost!=null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent,e.position,e.pressEventCamera,out var p))ghost.anchoredPosition=p;}
        public void OnEndDrag(PointerEventData e)
        {
            if(ghost==null)return;ClearGhost();e.eligibleForClick=false;suppressThrough=Time.frameCount+1;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            foreach(var hit in hits){var target=hit.gameObject.GetComponentInParent<MergeBoardView>();if(target!=null && target.controller==controller){controller.DropItem(cell,target.cell);return;}}
        }
        private void ClearGhost(){if(ghost!=null){ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);}ghost=null;}
        private void OnDisable()=>ClearGhost();
    }
}
