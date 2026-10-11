using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class TownActor : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerClickHandler
    {
        public string CharacterId { get; private set; } public bool Controlled { get; private set; }
        public TownMovement Movement { get; private set; }
        public TownPoint Position=>Controlled?Movement.Position:npcPosition;
        private TownNavigation navigation;private TownPoint npcPosition,npcA,npcB,npcTarget;private float waitUntil,reactionUntil;
        private TownLifeView life;private CharacterBodyGraphic body;private RectTransform rect;private bool dragged;private bool surprised;
        public void Bind(string characterId,bool controlled,TownMovement movement,TownNavigation navigation)
        {CharacterId=characterId;Controlled=controlled;Movement=controlled?movement:null;this.navigation=navigation;npcPosition=movement.Position;body=GetComponent<CharacterBodyGraphic>();body.Bind(characterId);body.raycastTarget=true;rect=(RectTransform)transform;}
        internal void Connect(TownLifeView view,TownPoint a,TownPoint b,float delay)
        {life=view;npcA=a;npcB=b;npcTarget=b;waitUntil=Time.unscaledTime+delay;}
        internal void Advance(float dt)
        {
            if(Controlled)Movement.Tick(dt);
            else if(Time.unscaledTime>=waitUntil)
            {
                npcPosition=navigation.Move(npcPosition,npcTarget,.028f*dt);
                if(Vector2.Distance(new Vector2(npcPosition.X,npcPosition.Y),new Vector2(npcTarget.X,npcTarget.Y))<.001f){npcTarget=npcTarget.Equals(npcB)?npcA:npcB;waitUntil=Time.unscaledTime+2.5f;}
            }
            var expression=Time.unscaledTime<reactionUntil?(surprised?CharacterExpression.Surprise:CharacterExpression.Smile):CharacterExpression.Normal;
            if(body.Expression!=expression)body.Bind(CharacterId,expression);
            float bob=Controlled?(Movement.OwnedPointerId.HasValue?Mathf.Abs(Mathf.Sin(Time.unscaledTime*10))*2:0):Time.unscaledTime>=waitUntil?Mathf.Abs(Mathf.Sin(Time.unscaledTime*8))*1.5f:0;
            if(Time.unscaledTime<reactionUntil)bob+=Mathf.Sin((reactionUntil-Time.unscaledTime)/.65f*Mathf.PI)*9;
            rect.anchoredPosition=life.Local(Position)+Vector2.up*bob;
        }
        public void React(){if(life==null||!life.CanInteract)return;surprised=false;reactionUntil=Time.unscaledTime+.65f;life.React(this);}
        internal void Surprise(){surprised=true;reactionUntil=Time.unscaledTime+.65f;}
        public void OnPointerDown(PointerEventData e)
        {if(!Controlled||!life.CanInteract)return;if(Movement.TryBeginDrag(e.pointerId)){dragged=false;life.StartInteraction();}}
        public void OnBeginDrag(PointerEventData e)
        {if(!Controlled||!life.CanInteract)return;if(!Movement.OwnedPointerId.HasValue)OnPointerDown(e);if(Movement.OwnedPointerId==e.pointerId){dragged=true;e.eligibleForClick=false;}}
        public void OnDrag(PointerEventData e)
        {if(Controlled&&life.CanInteract&&Movement.OwnedPointerId==e.pointerId){dragged=true;Movement.SetTarget(e.pointerId,life.Source(e.position,e.pressEventCamera));}}
        public void OnEndDrag(PointerEventData e)=>Release(e);
        public void OnPointerUp(PointerEventData e)=>Release(e);
        private void Release(PointerEventData e)
        {if(!Controlled||Movement.OwnedPointerId!=e.pointerId)return;Movement.ReleasePointer(e.pointerId);life.ActorReleased(this);if(dragged)e.eligibleForClick=false;}
        public void OnPointerClick(PointerEventData e){if(!dragged)React();}
        private void OnDisable(){Movement?.CancelInput();reactionUntil=0;dragged=false;}
    }
}
