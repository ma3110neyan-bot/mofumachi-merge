using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class TownActor : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerClickHandler
    {
        public string CharacterId { get; private set; }
        public bool Controlled { get; private set; }
        public TownMovement Movement { get; private set; }
        public TownPoint Position=>Controlled?Movement.Position:npcPosition;
        public TownPoint RenderPosition { get; private set; }
        public RectTransform VisualRoot { get; private set; }
        public bool IsGuided { get; private set; }
        private const int GuidePointer=int.MinValue;
        private TownPoint guideTarget;
        private System.Collections.Generic.IReadOnlyList<TownPoint> walkRoute;
        private int routeIndex,routeDirection=1;
        private TownNavigation navigation;
        private TownPoint npcPosition,npcA,npcB,npcTarget,previous;
        private float waitRemaining,reactionRemaining,stride,amplitude,breath;
        private TownLifeView life;
        private CharacterBodyGraphic body;
        private RectTransform rect;
        private bool dragged,surprised,moving;
        public void Bind(string characterId,bool controlled,TownMovement movement,TownNavigation navigation)
        {
            CharacterId=characterId;Controlled=controlled;Movement=controlled?movement:null;
            this.navigation=navigation;npcPosition=movement.Position;previous=Position;rect=(RectTransform)transform;
            VisualRoot=UIWidgets.Node("Town actor visual",rect,new Rect(0,0,1,1));VisualRoot.pivot=new Vector2(.5f,0);
            body=VisualRoot.gameObject.AddComponent<CharacterBodyGraphic>();body.Bind(characterId);body.raycastTarget=true;
        }
        internal void Connect(TownLifeView view,TownPoint a,TownPoint b,float delay)
        {life=view;npcA=a;npcB=b;npcTarget=b;waitRemaining=delay;ResetVisual();}
        internal void SetWalkRoute(System.Collections.Generic.IReadOnlyList<TownPoint> route,int start)
        {if(Controlled||route==null||route.Count<3)return;walkRoute=route;routeDirection=start%2==0?1:-1;routeIndex=(start+routeDirection+route.Count)%route.Count;npcTarget=route[routeIndex];}
        public bool GuideTo(TownPoint target)
        {
            if(!Controlled||life==null||!life.CanInteract || !float.IsFinite(target.X)||!float.IsFinite(target.Y) || (Movement.OwnedPointerId.HasValue&&!IsGuided))return false;
            Movement.CancelInput();IsGuided=false;
            if(!Movement.TryBeginDrag(GuidePointer))return false;
            guideTarget=target;Movement.SetTarget(GuidePointer,target);IsGuided=true;dragged=false;life.StartInteraction();return true;
        }
        internal void Advance(float dt)
        {
            previous=Position;
            if(Controlled)
            {
                Movement.Tick(dt);
                if(IsGuided&&Vector2.Distance(new Vector2(Position.X,Position.Y),new Vector2(guideTarget.X,guideTarget.Y))<.001f)
                {Movement.ReleasePointer(GuidePointer);IsGuided=false;FreezeMovement();life.ActorReleased(this);}
            }
            else if(waitRemaining>0)waitRemaining=Mathf.Max(0,waitRemaining-dt);
            else
            {
                npcPosition=navigation.Move(npcPosition,npcTarget,.028f*dt);
                if(Vector2.Distance(new Vector2(npcPosition.X,npcPosition.Y),new Vector2(npcTarget.X,npcTarget.Y))<.001f)
                {
                    if(walkRoute!=null){routeIndex=(routeIndex+routeDirection+walkRoute.Count)%walkRoute.Count;npcTarget=walkRoute[routeIndex];}
                    else npcTarget=npcTarget.Equals(npcB)?npcA:npcB;
                    waitRemaining=1.6f+(routeIndex%3)*.45f;
                }
            }
            float distance=navigation.DistanceAlongPath(previous,Position);
            moving=distance>.000001f;stride=Mathf.Repeat(stride+distance*140,Mathf.PI*2);
        }
        internal void Render(float alpha,float elapsedSeconds)
        {
            if(life==null||!float.IsFinite(elapsedSeconds)||elapsedSeconds<0)return;
            float dt=Mathf.Min(elapsedSeconds,.1f);
            RenderPosition=navigation.InterpolateAlongPath(previous,Position,alpha);
            rect.anchoredPosition=life.Local(RenderPosition);
            amplitude=Mathf.MoveTowards(amplitude,moving?1:0,dt/.12f);
            breath+=dt;reactionRemaining=Mathf.Max(0,reactionRemaining-dt);
            var expression=reactionRemaining>0?(surprised?CharacterExpression.Surprise:CharacterExpression.Smile):(!Controlled&&waitRemaining>0?CharacterExpression.Smile:CharacterExpression.Normal);
            if(body.Expression!=expression)body.Bind(CharacterId,expression);
            float reaction=reactionRemaining>0?Mathf.Sin((1-reactionRemaining/.65f)*Mathf.PI)*8:0;
            VisualRoot.anchoredPosition=Vector2.up*(Mathf.Abs(Mathf.Sin(stride))*2*amplitude+reaction);
            VisualRoot.localScale=Vector3.one*(1+.005f*Mathf.Sin(breath*2));
            VisualRoot.localRotation=Quaternion.Euler(0,0,reactionRemaining>0?Mathf.Sin((1-reactionRemaining/.65f)*Mathf.PI*2)*3:0);
        }
        internal void FreezeMovement()
        {previous=Position;RenderPosition=Position;moving=false;if(life!=null)rect.anchoredPosition=life.Local(Position);}
        internal void ResetVisual()
        {IsGuided=false;FreezeMovement();stride=amplitude=breath=reactionRemaining=0;if(VisualRoot!=null){VisualRoot.anchoredPosition=Vector2.zero;VisualRoot.localScale=Vector3.one;VisualRoot.localRotation=Quaternion.identity;}if(body!=null&&body.Expression!=CharacterExpression.Normal)body.Bind(CharacterId);}
        public void React(){if(life==null||!life.CanInteract)return;surprised=false;reactionRemaining=.65f;life.React(this);}
        internal void Surprise(){surprised=true;reactionRemaining=.65f;}
        public void OnPointerDown(PointerEventData e)
        {if(!Controlled||!life.CanInteract)return;if(IsGuided){Movement.CancelInput();IsGuided=false;FreezeMovement();}if(Movement.TryBeginDrag(e.pointerId)){dragged=false;life.StartInteraction();}}
        public void OnBeginDrag(PointerEventData e)
        {if(!Controlled||!life.CanInteract)return;if(!Movement.OwnedPointerId.HasValue)OnPointerDown(e);if(Movement.OwnedPointerId==e.pointerId){dragged=true;e.eligibleForClick=false;}}
        public void OnDrag(PointerEventData e)
        {if(Controlled&&life.CanInteract&&Movement.OwnedPointerId==e.pointerId){dragged=true;Movement.SetTarget(e.pointerId,life.Source(e.position,e.pressEventCamera));}}
        public void OnEndDrag(PointerEventData e)=>Release(e);
        public void OnPointerUp(PointerEventData e)=>Release(e);
        private void Release(PointerEventData e)
        {if(!Controlled||Movement.OwnedPointerId!=e.pointerId)return;Movement.ReleasePointer(e.pointerId);FreezeMovement();life.ActorReleased(this);if(dragged)e.eligibleForClick=false;}
        public void OnPointerClick(PointerEventData e){if(!dragged)React();}
        private void OnDisable(){Movement?.CancelInput();ResetVisual();dragged=false;}
    }
}
