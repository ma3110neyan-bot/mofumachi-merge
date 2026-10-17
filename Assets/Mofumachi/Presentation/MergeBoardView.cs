using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class MergeBoardView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UIFlowController controller;private int cell;private RectTransform ghost;private ItemGraphic heldArt;private bool heldWasEnabled;private int suppressThrough=-1;
        public bool SuppressClick=>ghost!=null || Time.frameCount<=suppressThrough;
        public void Initialize(UIFlowController flow,int index){controller=flow;cell=index;}
        public void OnPointerDown(PointerEventData e)=>MergeInputDiagnostics.Record(controller,"press",cell,-1,"pressed",e);
        public void OnBeginDrag(PointerEventData e)
        {
            if(!controller.CanInteractBoard || controller.Game.Board.At(cell)==null || controller.Game.Board.IsLocked(cell))
            {MergeInputDiagnostics.Record(controller,"drag-begin",cell,-1,"blocked",e);return;}
            MergeInputDiagnostics.Record(controller,"drag-begin",cell,-1,"started",e);
            ClearGhost();e.eligibleForClick=false;suppressThrough=Time.frameCount+1;
            var parent=(RectTransform)GetComponentInParent<Canvas>().transform;
            ghost=UIWidgets.Node("Dragged tea",parent,new Rect(.5f,.5f,0,0));ghost.sizeDelta=new Vector2(64,64);
            var shadow=UIWidgets.Node("Lifted item shadow",ghost,new Rect(.14f,.04f,.72f,.15f)).gameObject.AddComponent<RoundedPanelGraphic>();shadow.color=new Color(.27f,.20f,.24f,.22f);shadow.radius=100;shadow.decorated=false;shadow.raycastTarget=false;
            var item=controller.Game.Board.At(cell);var icon=UIWidgets.Node("Lifted item art",ghost,new Rect(0,.18f,1,.82f)).gameObject.AddComponent<ItemGraphic>();icon.Bind(item.itemId,item.level);icon.SetPolished(true);icon.raycastTarget=false;
            heldArt=GetComponentInChildren<ItemGraphic>();if(heldArt!=null){heldWasEnabled=heldArt.enabled;heldArt.enabled=false;}OnDrag(e);
        }
        public void OnDrag(PointerEventData e){if(ghost!=null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent,e.position,e.pressEventCamera,out var p))ghost.anchoredPosition=p;}
        public void OnEndDrag(PointerEventData e)
        {
            if(ghost==null){MergeInputDiagnostics.Record(controller,"drag-end",cell,-1,"no ghost/cancelled",e);return;}
            ClearGhost();e.eligibleForClick=false;suppressThrough=Time.frameCount+1;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            foreach(var hit in hits){var target=hit.gameObject.GetComponentInParent<MergeBoardView>();if(target!=null && target.controller==controller){MergeInputDiagnostics.Record(controller,"drag-end",cell,target.cell,"cell hit "+hit.gameObject.name,e);controller.DropItem(cell,target.cell);return;}}
            MergeInputDiagnostics.Record(controller,"drag-end",cell,-1,"no cell hit; top="+(hits.Count>0?hits[0].gameObject.name:"none"),e);
            controller.ReportBoardMessage(UIStrings.OutsideBoard);
        }
        private void ClearGhost(){if(heldArt!=null)heldArt.enabled=heldWasEnabled;heldArt=null;if(ghost!=null){ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);}ghost=null;}
        private void OnDisable(){if(ghost!=null)MergeInputDiagnostics.Record(controller,"drag-cancel",cell,-1,"view disabled");ClearGhost();}
        private void OnRectTransformDimensionsChange(){if(ghost!=null){MergeInputDiagnostics.Record(controller,"drag-cancel",cell,-1,"layout changed");ClearGhost();suppressThrough=Time.frameCount+1;controller.ReportBoardMessage("画面サイズが変わりました。もう一度操作してください。");}}
    }
}
