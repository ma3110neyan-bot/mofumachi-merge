using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Mofumachi.Presentation
{
    public sealed class TownActor : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerClickHandler
    {
        public string CharacterId { get; private set; }
        public bool Controlled { get; private set; }
        public TownRegionId RegionId { get; private set; }=TownRegionId.Central;
        public bool RegionVisible { get; private set; }=true;
        public bool IsRegionFading=>journeyFade>0;
        public TownMovement Movement { get; private set; }
        private TownWorldResident worldResident;
        public TownPoint WorldPosition=>worldResident?.Position??TownWorldLayout.ToWorld(RegionId,Position);
        public TownPoint Position=>worldResident==null?(Controlled?Movement.Position:npcPosition):TownWorldLayout.ToLocal(worldResident.Region,worldResident.Position);
        public TownPoint RenderPosition { get; private set; }
        public RectTransform VisualRoot { get; private set; }
        public bool IsGuided { get; private set; }
        public TownPoint GuidedTarget=>guideTarget;
        public TownPlace Place {get;private set;} public string DestinationId=>Place?.Id;
        public bool IsResting=>!Controlled&&(worldResident==null?waitRemaining>0:worldResident.Rest>0);
        private const int GuidePointer=int.MinValue;
        private TownPoint guideTarget;
        private System.Collections.Generic.IReadOnlyList<TownPoint> walkRoute;
        private int routeIndex,routeDirection=1;
        private TownNavigation navigation;
        private TownPoint npcPosition,npcA,npcB,npcTarget,previous;
        private float waitRemaining,reactionRemaining,stride,amplitude,breath,socialRemaining,viewScale=1;
        private TownLifeView life;
        private CharacterBodyGraphic body;
        private RectTransform rect;
        private bool dragged,surprised,moving;
        private bool regionalGuidance,transitionHeld;
        private CanvasGroup visibility;
        private float journeyFade;
        private float facing,speedBlend;
        public void Bind(string characterId,bool controlled,TownMovement movement,TownNavigation navigation)
        {
            CharacterId=characterId;Controlled=controlled;Movement=controlled?movement:null;
            this.navigation=navigation;npcPosition=movement.Position;previous=Position;rect=(RectTransform)transform;
            VisualRoot=UIWidgets.Node("Town actor visual",rect,new Rect(0,0,1,1));VisualRoot.pivot=new Vector2(.5f,0);
            body=VisualRoot.gameObject.AddComponent<CharacterBodyGraphic>();body.Bind(characterId);body.raycastTarget=true;
            visibility=gameObject.AddComponent<CanvasGroup>();
        }
        internal void Connect(TownLifeView view,TownPoint a,TownPoint b,float delay)
        {life=view;npcA=a;npcB=b;npcTarget=b;waitRemaining=delay;ResetVisual();}
        internal void BindWorld(TownWorldResident resident){worldResident=resident;ApplyWorldStep(0);}
        internal void ApplyWorldStep(float dt)
        {
            if(worldResident==null)return;RegionId=worldResident.Region;IsGuided=worldResident.IsWalking;moving=worldResident.IsWalking&&worldResident.PauseRemaining<=0;
            facing=Mathf.MoveTowards(facing,worldResident.Facing,dt*6);stride=worldResident.Stride;waitRemaining=worldResident.Rest;
            TownRegionCatalog.TryFindPlace(worldResident.PlaceId,life.TownLevel,out _,out var place);Place=place;
        }
        internal void SetWorldPlace(TownPlace place){Place=place;IsGuided=false;}
        internal void SetWalkRoute(System.Collections.Generic.IReadOnlyList<TownPoint> route,int start)
        {if(Controlled||route==null||route.Count<3)return;walkRoute=route;routeDirection=start%2==0?1:-1;routeIndex=(start+routeDirection+route.Count)%route.Count;npcTarget=route[routeIndex];}
        internal void SetDestination(TownPlace place,float wait){if(Controlled||place==null)return;regionalGuidance=transitionHeld=false;Place=place;npcTarget=place.Position;waitRemaining=wait;}
        internal void Greet(){if(worldResident!=null)life.World.Model.Pause(CharacterId,1.4f);socialRemaining=1;reactionRemaining=.65f;surprised=false;}
        internal void FaceTowards(TownPoint other){if(worldResident!=null)life.World.Model.Face(CharacterId,TownWorldLayout.ToWorld(RegionId,other));float x=other.X-Position.X;if(Mathf.Abs(x)>.002f)facing=Mathf.Sign(x);}
        public bool GuideTo(TownPoint target)=>BeginGuide(target,false);
        private bool BeginGuide(TownPoint target,bool preserveJourney)
        {
            if(worldResident!=null)return Controlled&&life.CanInteract&&life.World.GuidePoint(TownWorldLayout.ToWorld(RegionId,target));
            if(!Controlled||life==null||!life.CanInteract || !float.IsFinite(target.X)||!float.IsFinite(target.Y) || (Movement.OwnedPointerId.HasValue&&(worldResident!=null||!IsGuided)))return false;
            life.StartInteraction(preserveJourney);Movement.CancelInput();IsGuided=false;transitionHeld=false;
            if(!Movement.TryBeginDrag(GuidePointer))return false;
            guideTarget=target;Movement.SetTarget(GuidePointer,target);IsGuided=true;dragged=false;return true;
        }
        internal bool GuideForJourney(TownPoint target)
        {
            regionalGuidance=true;transitionHeld=false;
            if(Controlled)return BeginGuide(target,true);
            Place=null;walkRoute=null;npcTarget=target;waitRemaining=0;return true;
        }
        internal void HoldForTransition(){transitionHeld=true;Movement?.CancelInput();IsGuided=false;FreezeMovement();}
        internal void FinishRegionalGuide(TownPlace place)
        {
            regionalGuidance=transitionHeld=false;Movement?.CancelInput();IsGuided=false;FreezeMovement();
            if(!Controlled)
            {
                if(place==null&&life!=null)place=TownRegionCatalog.ForLevel(RegionId,life.TownLevel).Map.Places.Where(p=>p.Activity!=TownActivity.Entrance).OrderBy(p=>Vector2.Distance(new Vector2(p.Position.X,p.Position.Y),new Vector2(Position.X,Position.Y))).FirstOrDefault();
                if(place!=null)SetDestination(place,place.Activity==TownActivity.Bench?3:1.6f);
            }
        }
        public void TransferToRegion(TownRegionDefinition region,TownPoint entry)
        {
            Movement?.CancelInput();RegionId=region.Id;navigation=new TownNavigation(region.Map.Nodes,region.Map.Edges,.028f);
            var movement=new TownMovement(navigation,entry);Movement=Controlled?movement:null;npcPosition=movement.Position;
            npcA=npcB=npcTarget=movement.Position;previous=movement.Position;Place=null;walkRoute=null;
            regionalGuidance=transitionHeld=false;waitRemaining=0;ResetVisual();
        }
        public void SetRegionVisible(bool visible)
        {
            RegionVisible=visible;ApplyVisibility();
            if(!visible){var name=transform.Find("Town actor name panel");if(name!=null)name.gameObject.SetActive(false);}
        }
        internal void SetJourneyFade(float opacity){journeyFade=Mathf.Clamp01(opacity);ApplyVisibility();}
        private void ApplyVisibility()
        {
            bool interactive=RegionVisible&&journeyFade<=0;
            if(visibility!=null){visibility.alpha=RegionVisible?1-journeyFade:0;visibility.blocksRaycasts=interactive;visibility.interactable=interactive;}
            if(body!=null)body.raycastTarget=interactive;
        }
        internal void Advance(float dt)
        {
            if(worldResident!=null){ApplyWorldStep(dt);return;}
            previous=Position;
            if(transitionHeld){moving=false;return;}
            if(socialRemaining>0){socialRemaining=Mathf.Max(0,socialRemaining-dt);moving=false;return;}
            if(Controlled)
            {
                speedBlend=Mathf.MoveTowards(speedBlend,Movement.OwnedPointerId.HasValue?1:0,dt/.16f);
                float brake=IsGuided?Mathf.Clamp(navigation.DistanceAlongPath(Position,guideTarget)/.025f,.3f,1):1;
                Movement.Tick(dt*speedBlend*brake);
                if(IsGuided&&Vector2.Distance(new Vector2(Position.X,Position.Y),new Vector2(guideTarget.X,guideTarget.Y))<.001f)
                {Movement.ReleasePointer(GuidePointer);IsGuided=false;FreezeMovement();life.ActorReleased(this);}
            }
            else if(waitRemaining>0){waitRemaining=Mathf.Max(0,waitRemaining-dt);if(waitRemaining==0&&Place!=null)life.ChooseNextStop(this);}
            else
            {
                npcPosition=navigation.Move(npcPosition,npcTarget,.028f*dt*life.MovementScale(this));
                if(Vector2.Distance(new Vector2(npcPosition.X,npcPosition.Y),new Vector2(npcTarget.X,npcTarget.Y))<.001f)
                {
                    if(regionalGuidance){transitionHeld=true;moving=false;return;}
                    if(Place!=null){waitRemaining=Place.Activity==TownActivity.Bench?3:1.6f;return;}
                    if(walkRoute!=null){routeIndex=(routeIndex+routeDirection+walkRoute.Count)%walkRoute.Count;npcTarget=walkRoute[routeIndex];}
                    else npcTarget=npcTarget.Equals(npcB)?npcA:npcB;
                    waitRemaining=1.6f+(routeIndex%3)*.45f;
                }
            }
            float distance=navigation.DistanceAlongPath(previous,Position);
            if(Mathf.Abs(Position.X-previous.X)>.000001f)facing=Mathf.MoveTowards(facing,Mathf.Sign(Position.X-previous.X),dt*6);
            moving=distance>.000001f;stride=Mathf.Repeat(stride+distance*140,Mathf.PI*2);
        }
        internal void Render(float alpha,float elapsedSeconds)
        {
            if(life==null||!RegionVisible||!float.IsFinite(elapsedSeconds)||elapsedSeconds<0)return;
            float dt=Mathf.Min(elapsedSeconds,.1f);
            RenderPosition=worldResident==null?navigation.InterpolateAlongPath(previous,Position,alpha):Position;
            rect.anchoredPosition=worldResident==null?life.Local(RenderPosition):life.World.Local(worldResident.Position);
            amplitude=Mathf.MoveTowards(amplitude,moving?1:0,dt/.12f);
            breath+=dt;reactionRemaining=Mathf.Max(0,reactionRemaining-dt);
            var expression=reactionRemaining>0?(surprised?CharacterExpression.Surprise:CharacterExpression.Smile):(!Controlled&&waitRemaining>0?CharacterExpression.Smile:CharacterExpression.Normal);
            if(body.Expression!=expression)body.Bind(CharacterId,expression);
            float reaction=reactionRemaining>0?Mathf.Sin((1-reactionRemaining/.65f)*Mathf.PI)*8:0;
            var pose=TownCharacterMotion.Sample(stride,amplitude,facing,IsResting?1:0);
            float rest=IsResting&&Place?.Activity==TownActivity.Bench?-2:0;
            VisualRoot.anchoredPosition=Vector2.up*(pose.Hop+reaction+rest);
            VisualRoot.localScale=Vector3.one*(viewScale+.005f*Mathf.Sin(breath*2));
            float looking=IsResting&&Place?.Activity==TownActivity.Flowers?2*Mathf.Sin(breath*.8f):0;
            VisualRoot.localRotation=Quaternion.Euler(0,0,pose.Lean+looking+(reactionRemaining>0?Mathf.Sin((1-reactionRemaining/.65f)*Mathf.PI*2)*3:0));
            body.Pose(pose.Foot,reactionRemaining>0?Mathf.Sin((1-reactionRemaining/.65f)*Mathf.PI*3):0,facing,TownCharacterMotion.Blink(breath,CharacterCatalog.Find(CharacterId).PortraitIndex));
        }
        internal void FreezeMovement()
        {previous=Position;RenderPosition=Position;moving=false;if(life!=null)rect.anchoredPosition=worldResident==null?life.Local(Position):life.World.Local(worldResident.Position);}
        internal void ResetVisual()
        {IsGuided=false;FreezeMovement();stride=amplitude=breath=reactionRemaining=speedBlend=0;if(VisualRoot!=null){VisualRoot.anchoredPosition=Vector2.zero;VisualRoot.localScale=Vector3.one;VisualRoot.localRotation=Quaternion.identity;}if(body!=null){if(body.Expression!=CharacterExpression.Normal)body.Bind(CharacterId);body.Pose(0,0,0,false);}}
        public void React(){if(life==null||!life.CanInteract||!RegionVisible||journeyFade>0||life.IsManuallyDragging||life.NearbyFacility!=null)return;if(worldResident!=null&&!Controlled)life.World.Model.Pause(CharacterId,2);surprised=false;reactionRemaining=.65f;life.React(this);}
        internal void SetViewScale(float scale){viewScale=Mathf.Clamp(scale,1,1.15f);}
        internal void Surprise(){surprised=true;reactionRemaining=.65f;}
        public void OnPointerDown(PointerEventData e)
        {if(!Controlled||!life.CanInteract||!RegionVisible||Movement.OwnedPointerId.HasValue&&(worldResident!=null||!IsGuided))return;life.StartInteraction();if(IsGuided){Movement.CancelInput();IsGuided=false;FreezeMovement();}if(Movement.TryBeginDrag(e.pointerId))dragged=false;}
        public void OnBeginDrag(PointerEventData e)
        {if(!Controlled||!life.CanInteract)return;if(!Movement.OwnedPointerId.HasValue)OnPointerDown(e);if(Movement.OwnedPointerId==e.pointerId){dragged=true;e.eligibleForClick=false;}}
        public void OnDrag(PointerEventData e)
        {if(Controlled&&life.CanInteract&&Movement.OwnedPointerId==e.pointerId){dragged=true;if(worldResident!=null)life.World.GuidePoint(life.World.Source(e.position,e.pressEventCamera));else Movement.SetTarget(e.pointerId,life.Source(e.position,e.pressEventCamera));}}
        public void OnEndDrag(PointerEventData e)=>Release(e);
        public void OnPointerUp(PointerEventData e)=>Release(e);
        private void Release(PointerEventData e)
        {if(!Controlled||Movement.OwnedPointerId!=e.pointerId)return;Movement.ReleasePointer(e.pointerId);FreezeMovement();life.ActorReleased(this);if(dragged)e.eligibleForClick=false;}
        public void OnPointerClick(PointerEventData e){if(!dragged){if(Controlled&&life.CanInteract)life.GetComponent<TownStreetWalkView>()?.Arm();else React();}}
        private void OnDisable(){Movement?.CancelInput();ResetVisual();dragged=false;}
    }
}
