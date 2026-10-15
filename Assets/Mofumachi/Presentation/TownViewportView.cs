using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Owns only Home's transient UV; no persistence or gameplay mutation.
    public sealed class TownViewportView : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerUpHandler,IPointerClickHandler
    {
        public int? OwnedPointerId {get;private set;} public bool IsDragging=>OwnedPointerId.HasValue;
        private TownViewport model;private RawImage background;private BackgroundCover cover;private TownLifeView life;private Image input;
        private TownAmbientGraphic water;private Rect previousRect;private float elapsed;private Vector2 lastPointer,pendingPan;private TownPoint? focus;
        private bool dragged;
        public TownPoint Center=>model==null?new TownPoint(.5f,.5f):new TownPoint(model.UvRect.X+model.UvRect.Width/2,model.UvRect.Y+model.UvRect.Height/2);
        public void Bind(ScreenContext c,RawImage image,TownLifeView town)
        {
            background=image;life=town;cover=image.GetComponent<BackgroundCover>();if(cover!=null)cover.enabled=false;
            model=new TownViewport((float)image.texture.width/image.texture.height);
            input=gameObject.AddComponent<Image>();input.color=new Color(1,1,1,.001f);input.raycastTarget=true;
            water=town.transform.parent.GetComponentInChildren<TownAmbientGraphic>();Configure();
        }
        public void ChangeRegion(RawImage image)
        {
            CancelGesture();focus=null;elapsed=0;background=image;
            model=new TownViewport((float)image.texture.width/image.texture.height);Configure();
        }
        public void RestoreCenter(TownPoint center)
        {
            if(model==null||!float.IsFinite(center.X)||!float.IsFinite(center.Y))return;
            var current=Center;model.TryPan((center.X-current.X)/model.UvRect.Width,(center.Y-current.Y)/model.UvRect.Height);Apply();
        }
        public void FrameImmediately(TownPoint point){if(model==null)return;Frame(point);Apply();}
        private void Configure()
        {
            if(model==null||background==null)return;var rect=background.rectTransform.rect;
            if(rect.width<=0||rect.height<=0)return;model.Configure(rect.width/rect.height,1.10f);previousRect=rect;Apply();
        }
        public void Focus(TownPoint point)=>focus=point;
        public void Advance(float seconds)
        {
            if(model==null||life==null)return;if(!life.CanInteract){CancelGesture();focus=null;return;}
            if(background.rectTransform.rect!=previousRect){CancelGesture();Configure();}
            if(!float.IsFinite(seconds)||seconds<=0)return;elapsed+=Mathf.Min(seconds,.1f);if(elapsed<1f/30)return;elapsed=Mathf.Repeat(elapsed,1f/30);
            if(IsDragging||pendingPan!=Vector2.zero)model.TryPan(pendingPan.x,pendingPan.y);
            else if(focus.HasValue&&!life.IsManuallyDragging)
            {
                Frame(focus.Value);
            }
            pendingPan=Vector2.zero;focus=null;Apply();
        }
        private void Frame(TownPoint point)
        {
            var world=(RectTransform)life.transform;var r=world.rect;var br=background.rectTransform.rect;
            var low=background.rectTransform.InverseTransformPoint(world.TransformPoint(new Vector2(r.xMin+44,r.yMin+26)));
            var high=background.rectTransform.InverseTransformPoint(world.TransformPoint(new Vector2(r.xMax-48,r.yMax-132)));
            var min=(new Vector2(low.x,low.y)-br.min)/br.size;var max=(new Vector2(high.x,high.y)-br.min)/br.size;
            min=Vector2.Max(Vector2.zero,min);max=Vector2.Min(Vector2.one,max);
            model.TryFrame(point,new TownViewportRect(min.x,min.y,max.x-min.x,max.y-min.y));
        }
        private void Apply()
        {
            var uv=model.UvRect;var next=new Rect(uv.X,uv.Y,uv.Width,uv.Height);
            bool changed=background.uvRect!=next;background.uvRect=next;
            life.SetViewScale(1.10f);if(changed){life.RefreshViewport();water?.NotifyViewportChanged();}
        }
        public void OnPointerDown(PointerEventData e)
        {if(life==null||!life.CanInteract||life.IsManuallyDragging||OwnedPointerId.HasValue)return;OwnedPointerId=e.pointerId;lastPointer=e.position;pendingPan=Vector2.zero;dragged=false;}
        public void OnBeginDrag(PointerEventData e){if(OwnedPointerId==e.pointerId){dragged=true;e.eligibleForClick=false;}}
        public void OnDrag(PointerEventData e)
        {if(OwnedPointerId!=e.pointerId)return;if(!life.CanInteract){CancelGesture();return;}var r=background.rectTransform.rect;var a=Vector2.zero;var b=Vector2.zero;RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform,lastPointer,e.pressEventCamera,out a);RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform,e.position,e.pressEventCamera,out b);if(r.width>0&&r.height>0)pendingPan-=(b-a)/r.size;lastPointer=e.position;e.eligibleForClick=false;}
        public void OnEndDrag(PointerEventData e){if(OwnedPointerId==e.pointerId)OwnedPointerId=null;}
        public void OnPointerUp(PointerEventData e){if(OwnedPointerId==e.pointerId)OwnedPointerId=null;}
        public void OnPointerClick(PointerEventData e){if(!dragged&&life!=null&&life.CanInteract)life.GetComponent<TownStreetWalkView>()?.Tap(life.Source(e.position,e.pressEventCamera));}
        public void CancelGesture(){OwnedPointerId=null;pendingPan=Vector2.zero;}
        private void OnRectTransformDimensionsChange(){CancelGesture();if(model!=null)Configure();}
        private void OnDisable(){CancelGesture();focus=null;if(input!=null)input.raycastTarget=false;if(cover!=null){cover.enabled=true;cover.FitNow();}}
    }
}
