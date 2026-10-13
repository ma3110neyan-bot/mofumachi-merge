using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Owns only Home's transient UV; no persistence or gameplay mutation.
    public sealed class TownViewportView : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerUpHandler
    {
        public int? OwnedPointerId {get;private set;} public bool IsDragging=>OwnedPointerId.HasValue;
        private TownViewport model;private RawImage background;private BackgroundCover cover;private TownLifeView life;private Image input;
        private TownAmbientGraphic water;private Rect previousRect;private float elapsed;private Vector2 lastPointer,pendingPan;private TownPoint? focus;
        public void Bind(ScreenContext c,RawImage image,TownLifeView town)
        {
            background=image;life=town;cover=image.GetComponent<BackgroundCover>();if(cover!=null)cover.enabled=false;
            model=new TownViewport((float)image.texture.width/image.texture.height);
            input=gameObject.AddComponent<Image>();input.color=new Color(1,1,1,.001f);input.raycastTarget=true;
            water=c.Flow.GetComponentInChildren<TownAmbientGraphic>();Configure();
        }
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
                var world=(RectTransform)life.transform;var r=world.rect;var br=background.rectTransform.rect;
                var low=background.rectTransform.InverseTransformPoint(world.TransformPoint(new Vector2(r.xMin+44,r.yMin+26)));
                // The entrance marker reaches128 above its foot, beyond the actor's body.
                var high=background.rectTransform.InverseTransformPoint(world.TransformPoint(new Vector2(r.xMax-48,r.yMax-132)));
                var min=(new Vector2(low.x,low.y)-br.min)/br.size;var max=(new Vector2(high.x,high.y)-br.min)/br.size;
                min=Vector2.Max(Vector2.zero,min);max=Vector2.Min(Vector2.one,max);
                model.TryFrame(focus.Value,new TownViewportRect(min.x,min.y,max.x-min.x,max.y-min.y));
            }
            pendingPan=Vector2.zero;focus=null;Apply();
        }
        private void Apply()
        {
            var uv=model.UvRect;var next=new Rect(uv.X,uv.Y,uv.Width,uv.Height);
            bool changed=background.uvRect!=next;background.uvRect=next;
            life.SetViewScale(1.10f);if(changed){life.RefreshViewport();water?.NotifyViewportChanged();}
        }
        public void OnPointerDown(PointerEventData e)
        {if(life==null||!life.CanInteract||life.IsManuallyDragging||OwnedPointerId.HasValue)return;OwnedPointerId=e.pointerId;lastPointer=e.position;pendingPan=Vector2.zero;}
        public void OnBeginDrag(PointerEventData e){if(OwnedPointerId==e.pointerId)e.eligibleForClick=false;}
        public void OnDrag(PointerEventData e)
        {if(OwnedPointerId!=e.pointerId)return;if(!life.CanInteract){CancelGesture();return;}var r=background.rectTransform.rect;var a=Vector2.zero;var b=Vector2.zero;RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform,lastPointer,e.pressEventCamera,out a);RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform,e.position,e.pressEventCamera,out b);if(r.width>0&&r.height>0)pendingPan-=(b-a)/r.size;lastPointer=e.position;e.eligibleForClick=false;}
        public void OnEndDrag(PointerEventData e){if(OwnedPointerId==e.pointerId)OwnedPointerId=null;}
        public void OnPointerUp(PointerEventData e){if(OwnedPointerId==e.pointerId)OwnedPointerId=null;}
        public void CancelGesture(){OwnedPointerId=null;pendingPan=Vector2.zero;}
        private void OnRectTransformDimensionsChange(){CancelGesture();if(model!=null)Configure();}
        private void OnDisable(){CancelGesture();focus=null;if(input!=null)input.raycastTarget=false;if(cover!=null){cover.enabled=true;cover.FitNow();}}
    }
}
