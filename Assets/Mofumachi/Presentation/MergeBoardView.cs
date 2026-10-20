using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class MergeBoardView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UIFlowController controller;private int cell;private RectTransform ghost;private ItemGraphic heldArt;private bool heldWasEnabled;private int suppressThrough=-1;
        private static MergeBoardView activeDrag;
        private readonly List<GameObject> matchGuides=new List<GameObject>();
        private readonly List<RaycastResult> hits=new List<RaycastResult>();
        private MergeBoardView hovered;private GameObject hoverGuide;private int dragPointer;
        public bool SuppressClick=>ghost!=null || Time.frameCount<=suppressThrough;
        public void Initialize(UIFlowController flow,int index){controller=flow;cell=index;}
        public void OnPointerDown(PointerEventData e)=>MergeInputDiagnostics.Record(controller,"press",cell,-1,"pressed",e);
        public void OnBeginDrag(PointerEventData e)
        {
            if(!controller.CanInteractBoard || controller.Game.Board.At(cell)==null || controller.Game.Board.IsLocked(cell)||(activeDrag!=null&&activeDrag!=this))
            {MergeInputDiagnostics.Record(controller,"drag-begin",cell,-1,"blocked",e);return;}
            MergeInputDiagnostics.Record(controller,"drag-begin",cell,-1,"started",e);
            ClearGhost();activeDrag=this;dragPointer=e.pointerId;e.eligibleForClick=false;suppressThrough=Time.frameCount+1;
            var parent=(RectTransform)GetComponentInParent<Canvas>().transform;
            ghost=UIWidgets.Node("Dragged tea",parent,new Rect(.5f,.5f,0,0));float side=((RectTransform)transform).rect.width;ghost.sizeDelta=Vector2.one*Mathf.Max(56,side*1.15f);
            var shadow=UIWidgets.Node("Lifted item shadow",ghost,new Rect(.14f,.04f,.72f,.15f)).gameObject.AddComponent<RoundedPanelGraphic>();shadow.color=new Color(.27f,.20f,.24f,.22f);shadow.radius=100;shadow.decorated=false;shadow.raycastTarget=false;
            var item=controller.Game.Board.At(cell);var icon=UIWidgets.Node("Lifted item art",ghost,new Rect(0,.18f,1,.82f)).gameObject.AddComponent<ItemGraphic>();icon.Bind(item.itemId,item.level);icon.SetPolished(true);icon.raycastTarget=false;
            heldArt=GetComponentInChildren<ItemGraphic>();if(heldArt!=null){heldWasEnabled=heldArt.enabled;heldArt.enabled=false;}
            foreach(var view in transform.parent.GetComponentsInChildren<MergeBoardView>())
                if(view!=this&&MergePresentation.IsMatch(item,controller.Game.Board.At(view.cell),false,controller.Game.Board.IsLocked(view.cell)))
                {var guide=UIWidgets.Node("Drag match guidance",(RectTransform)view.transform,new Rect(0,0,1,1)).gameObject;guide.AddComponent<MergeSparkleGraphic>().BindMatch();matchGuides.Add(guide);}
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if(ghost==null||e.pointerId!=dragPointer)return;
            var target=FindTarget(e);SetHover(target);
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent,e.position,e.pressEventCamera,out var point))return;
            var lifted=point+Vector2.up*(ghost.rect.height*.24f);
            if(target!=null&&target!=this&&CanLand(target))
            {
                var rect=(RectTransform)target.transform;var center=(Vector2)ghost.parent.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                if(Vector2.Distance(point,center)<rect.rect.width*.38f)lifted=Vector2.Lerp(lifted,center+Vector2.up*6,.45f);
            }
            ghost.anchoredPosition=lifted;
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(ghost==null||e.pointerId!=dragPointer){MergeInputDiagnostics.Record(controller,"drag-end",cell,-1,"no ghost/cancelled",e);return;}
            var target=FindTarget(e);
            ClearGhost();e.eligibleForClick=false;suppressThrough=Time.frameCount+1;
            if(target!=null)
            {
                int landing=target.cell;bool empty=controller.Game.Board.At(landing)==null;
                MergeInputDiagnostics.Record(controller,"drag-end",cell,landing,"cell hit "+target.name,e);
                bool changed=controller.DropItem(cell,landing);
                if(changed&&empty)foreach(var view in controller.GetComponentsInChildren<MergeBoardView>())
                    if(view.cell==landing){var art=view.GetComponentInChildren<ItemGraphic>();if(art!=null)art.gameObject.AddComponent<ItemLandingFeedback>();break;}
                return;
            }
            MergeInputDiagnostics.Record(controller,"drag-end",cell,-1,"no cell hit; top="+(hits.Count>0?hits[0].gameObject.name:"none"),e);
            controller.ReportBoardMessage(UIStrings.OutsideBoard);
        }
        private MergeBoardView FindTarget(PointerEventData e)
        {
            hits.Clear();if(EventSystem.current==null)return null;EventSystem.current.RaycastAll(e,hits);
            foreach(var hit in hits){var view=hit.gameObject.GetComponentInParent<MergeBoardView>();if(view!=null&&view.controller==controller)return view;}
            return null;
        }
        private bool CanLand(MergeBoardView target)=>!controller.Game.Board.IsLocked(target.cell)&&(controller.Game.Board.At(target.cell)==null||MergePresentation.IsMatch(controller.Game.Board.At(cell),controller.Game.Board.At(target.cell),false,false));
        private void SetHover(MergeBoardView target)
        {
            if(hovered==target)return;if(hoverGuide!=null){hoverGuide.SetActive(false);Destroy(hoverGuide);}hoverGuide=null;hovered=target;
            if(target==null||target==this)return;
            hoverGuide=UIWidgets.Node("Drag target glow",(RectTransform)target.transform,new Rect(0,0,1,1)).gameObject;
            hoverGuide.AddComponent<MergeSparkleGraphic>().BindDrop(!CanLand(target));
        }
        private void ClearGhost()
        {
            if(heldArt!=null)heldArt.enabled=heldWasEnabled;heldArt=null;if(ghost!=null){ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);}ghost=null;
            SetHover(null);foreach(var guide in matchGuides)if(guide!=null){guide.SetActive(false);Destroy(guide);}matchGuides.Clear();if(activeDrag==this)activeDrag=null;
        }
        private void OnDisable(){if(ghost!=null)MergeInputDiagnostics.Record(controller,"drag-cancel",cell,-1,"view disabled");ClearGhost();}
        private void OnRectTransformDimensionsChange(){if(ghost!=null){MergeInputDiagnostics.Record(controller,"drag-cancel",cell,-1,"layout changed");ClearGhost();suppressThrough=Time.frameCount+1;controller.ReportBoardMessage("画面サイズが変わりました。もう一度操作してください。");}}
    }
}
