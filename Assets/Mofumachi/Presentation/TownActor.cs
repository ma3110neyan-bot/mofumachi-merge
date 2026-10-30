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
        private bool noahTrialRequested=true;
        public bool NoahTrialEnabled=>TownNoah25DTrial.IsEnabled(CharacterId,noahTrialRequested);
        private bool noahReprototypeRequested=true;
        public bool NoahReprototypeEnabled=>NoahTrialEnabled&&noahReprototypeRequested;
        private bool noahDepthFocusRequested=true;
        public bool NoahDepthFocusEnabled=>NoahReprototypeEnabled&&noahDepthFocusRequested;
#if MOFUMACHI_NOAH_FINAL_RIG
        private bool noahFinalRigRequested=true;
#else
        private bool noahFinalRigRequested=false;
#endif
        public bool NoahFinalRigEnabled=>NoahDepthFocusEnabled&&TownNoahFinalRig.IsEnabled(CharacterId,noahFinalRigRequested);
#if MOFUMACHI_NOAH_AUTHORED
        private bool noahAuthoredRequested=true;
#else
        private bool noahAuthoredRequested=false;
#endif
        private NoahAuthoredGraphic authored;
        private NoahAuthoredDirection authoredDirection=NoahAuthoredDirection.Left;
        public bool NoahAuthoredEnabled=>CharacterId=="noah"&&noahAuthoredRequested;
        public NoahAuthoredDirection AuthoredDirection=>authoredDirection;
        public void SetNoahAuthoredEnabled(bool enabled)
        {
            if(CharacterId!="noah")return;
            noahAuthoredRequested=enabled;
            if(NoahAuthoredEnabled)EnsureAuthoredGraphic();
            else {if(authored!=null)authored.enabled=false;if(body!=null){body.enabled=true;body.SetNoahTownTrial(NoahTrialEnabled);}}
            if(life!=null)Render(0,0);
        }
        private void EnsureAuthoredGraphic()
        {
            if(authored!=null)return;
            var node=UIWidgets.Node("Noah authored part trial",VisualRoot,new Rect(0,0,1,1));
            node.pivot=new Vector2(.5f,0);authored=node.gameObject.AddComponent<NoahAuthoredGraphic>();authored.Configure();
            if(body!=null)body.enabled=false;
        }
        public void SetNoahFinalRigEnabled(bool enabled)
        {
            bool previous=NoahFinalRigEnabled;noahFinalRigRequested=enabled;if(previous==NoahFinalRigEnabled)return;
            body?.SetNoahFinalRig(NoahFinalRigEnabled);if(life!=null)Render(0,0);
        }
        public void SetNoahDepthFocusEnabled(bool enabled)
        {
            bool previous=NoahDepthFocusEnabled;noahDepthFocusRequested=enabled;if(previous==NoahDepthFocusEnabled)return;
            body?.SetNoahDepthFocus(NoahDepthFocusEnabled);EnsureNoahShadow();if(life!=null)Render(0,0);
        }
        public void SetNoahReprototypeEnabled(bool enabled)
        {
            bool previous=NoahReprototypeEnabled;noahReprototypeRequested=enabled;
            if(previous==NoahReprototypeEnabled)return;
            body?.SetNoahReprototype(NoahReprototypeEnabled);body?.SetNoahDepthFocus(NoahDepthFocusEnabled);EnsureNoahShadow();if(life!=null)Render(0,0);
        }
        private float ReactionSeconds=>NoahTrialEnabled?TownNoah25DTrial.ReactionSeconds:.65f;
        // An evidence-capture switch, deliberately absent from product UI and
        // persistence. Re-render the same instant without resetting any clock.
        public void SetNoahTrialEnabled(bool enabled)
        {
            bool previous=NoahTrialEnabled;noahTrialRequested=enabled;
            if(previous==NoahTrialEnabled)return;
            body?.SetNoahTownTrial(NoahTrialEnabled);body?.SetNoahReprototype(NoahReprototypeEnabled);body?.SetNoahDepthFocus(NoahDepthFocusEnabled);EnsureNoahShadow();if(life!=null)Render(0,0);
        }
        public bool IsGuided { get; private set; }
        public TownPoint GuidedTarget=>guideTarget;
        public TownPlace Place {get;private set;} public string DestinationId=>Place?.Id;
        public bool IsResting=>worldResident==null?!Controlled&&waitRemaining>0:!worldResident.IsWalking&&worldResident.Rest>0;
        private const int GuidePointer=int.MinValue;
        private TownPoint guideTarget;
        private System.Collections.Generic.IReadOnlyList<TownPoint> walkRoute;
        private int routeIndex,routeDirection=1;
        private TownNavigation navigation;
        private TownPoint npcPosition,npcA,npcB,npcTarget,previous;
        private float waitRemaining,reactionRemaining,stride,amplitude,breath,socialRemaining,viewScale=1;
        private TownLifeView life;
        private CharacterBodyGraphic body;
        private RoundedPanelGraphic softShadow,contactShadow;
        private NoahGroundShadowGraphic noahShadow;
        private RectTransform rect;
        private bool dragged,surprised,moving;
        private bool regionalGuidance,transitionHeld;
        private CanvasGroup visibility;
        private float journeyFade;
        private float facing,facingTarget,speedBlend,movementIntensity,benchBlend,flowerBlend;
        private float noahHeadFacing,noahForward,noahForwardTarget;
        public void Bind(string characterId,bool controlled,TownMovement movement,TownNavigation navigation)
        {
            CharacterId=characterId;Controlled=controlled;Movement=controlled?movement:null;
            this.navigation=navigation;npcPosition=movement.Position;previous=Position;rect=(RectTransform)transform;
            VisualRoot=UIWidgets.Node("Town actor visual",rect,new Rect(0,0,1,1));VisualRoot.pivot=new Vector2(.5f,0);
            body=VisualRoot.gameObject.AddComponent<CharacterBodyGraphic>();body.Bind(characterId);body.raycastTarget=true;
            body.SetNoahTownTrial(NoahTrialEnabled);
            body.SetNoahReprototype(NoahReprototypeEnabled);
            body.SetNoahDepthFocus(NoahDepthFocusEnabled);
            body.SetNoahFinalRig(NoahFinalRigEnabled);
            if(NoahAuthoredEnabled)EnsureAuthoredGraphic();
#if UNITY_ANDROID && !UNITY_EDITOR
            // Device attribution only. Editor capture tests keep their strict
            // unexpected-log check; no logging exception is needed there.
            if(CharacterId=="noah")Debug.Log("MOFUMACHI_NOAH_TRIAL_MODE "+(NoahFinalRigEnabled?"rig":"current"));
            if(CharacterId=="noah")Debug.Log("MOFUMACHI_NOAH_AUTHORED_MODE "+(NoahAuthoredEnabled?"authored":"current"));
#endif
            softShadow=CreateShadow("Town actor soft shadow",new Rect(.12f,-.06f,.76f,.12f),.075f);
            contactShadow=CreateShadow("Town actor contact shadow",new Rect(.27f,-.032f,.46f,.064f),.19f);
            softShadow.transform.SetAsFirstSibling();contactShadow.transform.SetSiblingIndex(1);
            EnsureNoahShadow();
            visibility=gameObject.AddComponent<CanvasGroup>();
        }
        private RoundedPanelGraphic CreateShadow(string name,Rect bounds,float opacity)
        {
            var shadow=UIWidgets.Node(name,rect,bounds).gameObject.AddComponent<RoundedPanelGraphic>();
            shadow.radius=100;shadow.decorated=false;shadow.raycastTarget=false;shadow.color=new Color(.24f,.19f,.26f,opacity);return shadow;
        }
        private void EnsureNoahShadow()
        {
            if(!NoahReprototypeEnabled||rect==null||noahShadow!=null)return;
            noahShadow=UIWidgets.Node("Noah soft ground shadow",rect,new Rect(.18f,-.06f,.64f,.12f)).gameObject.AddComponent<NoahGroundShadowGraphic>();
            noahShadow.raycastTarget=false;noahShadow.color=new Color(.18f,.14f,.20f,.28f);noahShadow.transform.SetAsFirstSibling();
            noahShadow.ConfigureGrounding(NoahDepthFocusEnabled);
        }
        internal void Connect(TownLifeView view,TownPoint a,TownPoint b,float delay)
        {life=view;npcA=a;npcB=b;npcTarget=b;waitRemaining=delay;ResetVisual();}
        internal void BindWorld(TownWorldResident resident){worldResident=resident;ApplyWorldStep(0);}
        internal void ApplyWorldStep(float dt)
        {
            if(worldResident==null)return;RegionId=worldResident.Region;IsGuided=worldResident.IsWalking;moving=worldResident.IsWalking&&worldResident.PauseRemaining<=0&&TownWorldLayout.Distance(worldResident.Previous,worldResident.Position)>.000001f;
            if(dt>0&&float.IsFinite(dt))movementIntensity=moving?Mathf.Clamp01(TownWorldLayout.Distance(worldResident.Previous,worldResident.Position)/(dt*(Controlled?.12f:.047f))):0;
            else if(!moving)movementIntensity=0;
            if(CharacterId=="noah")
            {
                float dx=worldResident.Position.X-worldResident.Previous.X,dy=worldResident.Position.Y-worldResident.Previous.Y;
                float distance=Mathf.Sqrt(dx*dx+dy*dy);noahForwardTarget=moving&&distance>.000001f?Mathf.Clamp(dy/distance,-1,1):0;
            }
            facingTarget=worldResident.Facing;stride=worldResident.Stride;waitRemaining=worldResident.Rest;
            TownRegionCatalog.TryFindPlace(worldResident.PlaceId,life.TownLevel,out _,out var place);Place=place;
        }
        internal void SetWorldPlace(TownPlace place){Place=place;IsGuided=false;}
        internal void SetWalkRoute(System.Collections.Generic.IReadOnlyList<TownPoint> route,int start)
        {if(Controlled||route==null||route.Count<3)return;walkRoute=route;routeDirection=start%2==0?1:-1;routeIndex=(start+routeDirection+route.Count)%route.Count;npcTarget=route[routeIndex];}
        internal void SetDestination(TownPlace place,float wait){if(Controlled||place==null)return;regionalGuidance=transitionHeld=false;Place=place;npcTarget=place.Position;waitRemaining=wait;}
        internal void Greet(){if(worldResident!=null)life.World.Model.Pause(CharacterId,1.4f);socialRemaining=1;reactionRemaining=ReactionSeconds;surprised=false;}
        internal void FaceTowards(TownPoint other){if(worldResident!=null)life.World.Model.Face(CharacterId,TownWorldLayout.ToWorld(RegionId,other));float x=other.X-Position.X;if(Mathf.Abs(x)>.002f)facingTarget=Mathf.Sign(x);}
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
            if(authored!=null)authored.raycastTarget=interactive;
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
            if(Mathf.Abs(Position.X-previous.X)>.000001f)facingTarget=Mathf.Sign(Position.X-previous.X);
            moving=distance>.000001f;movementIntensity=moving&&dt>0?Mathf.Clamp01(distance/(dt*(Controlled?.12f:.028f))):0;stride=Mathf.Repeat(stride+distance*140,Mathf.PI*2);
        }
        internal void Render(float alpha,float elapsedSeconds)
        {
            if(life==null||!RegionVisible||!float.IsFinite(elapsedSeconds)||elapsedSeconds<0)return;
            float dt=life.CanSimulate?Mathf.Min(elapsedSeconds,.1f):0;
            alpha=float.IsFinite(alpha)?Mathf.Clamp01(alpha):0;
            RenderPosition=worldResident==null?navigation.InterpolateAlongPath(previous,Position,alpha):Position;
            rect.anchoredPosition=worldResident==null?life.Local(RenderPosition):life.World.Local(worldResident.Position);
            amplitude=TownCharacterMotion.BlendMovement(amplitude,moving?movementIntensity:0,dt);
            facing=TownCharacterMotion.Turn(facing,facingTarget,dt);
            if(CharacterId=="noah"){noahHeadFacing=TownCharacterMotion.Turn(noahHeadFacing,facingTarget*(1-Mathf.Abs(noahForwardTarget)*.65f),dt*1.5f);noahForward=TownCharacterMotion.Turn(noahForward,noahForwardTarget,dt);}
            breath+=dt;reactionRemaining=Mathf.Max(0,reactionRemaining-dt);
            int resident=CharacterCatalog.Find(CharacterId).PortraitIndex;
            bool restingSmile=IsResting&&Mathf.Sin(breath*.8f+resident*.617f)>.88f;
            var expression=reactionRemaining>0?(surprised&&reactionRemaining>.43f?CharacterExpression.Surprise:CharacterExpression.Smile):(restingSmile?CharacterExpression.Smile:CharacterExpression.Normal);
            if(body.Expression!=expression)body.Bind(CharacterId,expression);
            var reaction=TownCharacterMotion.Reaction(reactionRemaining,.65f,surprised);
            var pose=TownCharacterMotion.Sample(stride,amplitude,facing,IsResting?1:0,breath,resident);
            benchBlend=TownCharacterMotion.BlendMovement(benchBlend,IsResting&&Place?.Activity==TownActivity.Bench?1:0,dt);
            flowerBlend=TownCharacterMotion.BlendMovement(flowerBlend,IsResting&&Place?.Activity==TownActivity.Flowers?1:0,dt);
            float attention=.5f+.5f*Mathf.Sin(breath*.65f+resident*.617f);
            float rest=-2*benchBlend-1.3f*flowerBlend*attention;
            float scale=viewScale*DepthScale;
            if(NoahAuthoredEnabled)
            {
                EnsureAuthoredGraphic();
                if(moving)authoredDirection=NoahAuthoredMotion.Direction(worldResident==null?Position.X-previous.X:worldResident.Position.X-worldResident.Previous.X,worldResident==null?Position.Y-previous.Y:worldResident.Position.Y-worldResident.Previous.Y,authoredDirection);
                bool front=authoredDirection==NoahAuthoredDirection.Front;
                body.enabled=front;authored.enabled=!front;
                VisualRoot.anchoredPosition=Vector2.zero;VisualRoot.localScale=Vector3.one*scale;VisualRoot.localRotation=Quaternion.identity;
                if(front){body.SetNoahTownTrial(false);body.Pose(0,0,0,TownCharacterMotion.Blink(breath,resident));}
                else authored.SetPose(breath,stride,amplitude,reactionRemaining>0?NoahAuthoredMode.Greet:life.IsActorSpeaking(this)?NoahAuthoredMode.Talk:moving?NoahAuthoredMode.Walk:NoahAuthoredMode.Idle,authoredDirection);
                UpdateShadows(0,scale);return;
            }
            if(NoahTrialEnabled)
            {
                body.SetNoahFinalRig(NoahFinalRigEnabled);
                var trial=NoahReprototypeEnabled?TownNoah25DTrial.Sample(stride,amplitude,facing,IsResting?1:0,breath,reactionRemaining,surprised,moving):TownNoah25DTrial.PreviousSample(stride,amplitude,facing,IsResting?1:0,breath,reactionRemaining,surprised,moving);
                float sx=scale*(1-trial.Breath*.35f),sy=scale*(1+trial.Breath);
                if(NoahDepthFocusEnabled)
                {
                    if(NoahFinalRigEnabled)
                    {
                        var rig=TownNoahFinalRig.Pose(trial,noahHeadFacing,noahForward);
                        var rigGround=TownNoahFinalRig.Support(body.NoahArtSize.x,body.NoahArtSize.y,rig);
                        VisualRoot.anchoredPosition=new Vector2(rigGround.X*sx,rigGround.Y*sy);
                        VisualRoot.localScale=new Vector3(sx,sy,scale);VisualRoot.localRotation=Quaternion.identity;
                        body.PoseNoahFinalRig(rig,TownCharacterMotion.Blink(breath,resident)||TownNoah25DTrial.SmilingEyes(reactionRemaining));UpdateShadows(trial.Hop,scale);return;
                    }
                    var focus=TownNoahDepthFocus.Pose(trial,stride,amplitude,noahHeadFacing,noahForward,reactionRemaining);
                    var planted=TownNoahDepthFocus.Support(body.NoahArtSize.x,body.NoahArtSize.y,focus,sx,sy,scale);
                    VisualRoot.anchoredPosition=new Vector2(planted.X,planted.Y);VisualRoot.localScale=new Vector3(sx,sy,scale);
                    VisualRoot.localRotation=Quaternion.AngleAxis(focus.Roll,Vector3.forward)*Quaternion.AngleAxis(focus.Pitch,Vector3.right)*Quaternion.AngleAxis(focus.Yaw,Vector3.up);
                    body.PoseNoahDepthFocus(focus,TownCharacterMotion.Blink(breath,resident)||TownNoah25DTrial.SmilingEyes(reactionRemaining));UpdateShadows(trial.Hop,scale);return;
                }
                if(!NoahReprototypeEnabled)sx*=1-.035f*Mathf.Abs(trial.Look);
                var support=NoahReprototypeEnabled?TownNoah25DTrial.SupportTranslation(body.NoahArtSize.x,trial,sx,sy):TownNoah25DTrial.PreviousSupportTranslation(body.NoahArtSize.x,trial,sx,sy);
                VisualRoot.anchoredPosition=new Vector2(support.X,support.Y);
                VisualRoot.localScale=new Vector3(sx,sy,NoahReprototypeEnabled?scale:1);
                VisualRoot.localRotation=NoahReprototypeEnabled?Quaternion.AngleAxis(trial.Lean,Vector3.forward)*Quaternion.AngleAxis(-trial.Look*TownNoah25DTrial.YawDegrees,Vector3.up):Quaternion.Euler(0,0,trial.Lean);
                body.PoseNoah(trial,TownCharacterMotion.Blink(breath,resident)||TownNoah25DTrial.SmilingEyes(reactionRemaining));UpdateShadows(trial.Hop,scale);return;
            }
            VisualRoot.anchoredPosition=new Vector2(-pose.Foot*1.1f,pose.Hop+reaction.Hop+rest)*scale;
            float compression=pose.Breath+reaction.Breath-Mathf.Cos(stride*2)*amplitude*.006f;
            VisualRoot.localScale=new Vector3(scale*(1-compression*.35f),scale*(1+compression),1);
            float looking=flowerBlend*(.65f+.65f*attention)*(facing<0?-1:1);
            VisualRoot.localRotation=Quaternion.Euler(0,0,pose.Lean*(1-benchBlend*.35f)+looking+reaction.Lean);
            body.Pose(pose.Foot,pose.Hand+reaction.Hand+flowerBlend*attention*.1f,pose.Look,TownCharacterMotion.Blink(breath,resident));
            UpdateShadows(pose.Hop+reaction.Hop,scale);
        }
        private float DepthScale=>TownCharacterMotion.DepthScale((TownWorldMap.Top-WorldPosition.Y)/TownWorldMap.Height);
        private void UpdateShadows(float height,float scale)
        {
            if(softShadow==null||contactShadow==null)return;height=Mathf.Clamp(height,0,12);
            softShadow.enabled=contactShadow.enabled=!NoahReprototypeEnabled;
            if(noahShadow!=null){noahShadow.enabled=NoahReprototypeEnabled;if(NoahFinalRigEnabled)noahShadow.ConfigureFinalRig();else noahShadow.ConfigureGrounding(NoahDepthFocusEnabled);noahShadow.rectTransform.localScale=Vector3.one*scale;}
            if(NoahTrialEnabled)
            {
                // Same two grounded graphics; do not lift the shadow with the
                // rendered body, or let the asymmetric tail set its footprint.
                softShadow.rectTransform.localScale=new Vector3(scale*.68f,scale*.80f,1);
                contactShadow.rectTransform.localScale=new Vector3(scale*.67f,scale*.85f,1);
                softShadow.color=new Color(.24f,.19f,.26f,.105f);
                contactShadow.color=new Color(.24f,.19f,.26f,.24f);return;
            }
            softShadow.rectTransform.localScale=Vector3.one*(scale*(1+height*.006f));
            contactShadow.rectTransform.localScale=Vector3.one*(scale*(1-height*.012f));
            softShadow.color=new Color(.24f,.19f,.26f,.075f*(1-height*.015f));
            contactShadow.color=new Color(.24f,.19f,.26f,.19f*(1-height*.025f));
        }
        internal void FreezeMovement()
        {previous=Position;RenderPosition=Position;moving=false;movementIntensity=0;if(life!=null)rect.anchoredPosition=worldResident==null?life.Local(Position):life.World.Local(worldResident.Position);}
        internal void ResetVisual()
        {IsGuided=false;FreezeMovement();stride=amplitude=breath=reactionRemaining=speedBlend=socialRemaining=facing=facingTarget=benchBlend=flowerBlend=noahHeadFacing=noahForward=noahForwardTarget=0;surprised=false;float scale=viewScale*DepthScale;if(VisualRoot!=null){VisualRoot.anchoredPosition=Vector2.zero;VisualRoot.localScale=new Vector3(scale,scale,1);VisualRoot.localRotation=Quaternion.identity;}UpdateShadows(0,scale);if(body!=null){if(body.Expression!=CharacterExpression.Normal)body.Bind(CharacterId);body.Pose(0,0,0,false);}}
        public void React(){if(life==null||!life.CanInteract||!RegionVisible||journeyFade>0||life.IsManuallyDragging||life.NearbyFacility!=null)return;if(worldResident!=null&&!Controlled)life.World.Model.Pause(CharacterId,2);surprised=false;reactionRemaining=ReactionSeconds;life.React(this);}
        internal void SetViewScale(float scale){viewScale=float.IsFinite(scale)?Mathf.Clamp(scale,1,1.15f):1;}
        internal void Surprise(){surprised=true;reactionRemaining=ReactionSeconds;}
        public void OnPointerDown(PointerEventData e)
        {if(!Controlled||!life.CanInteract||!RegionVisible||Movement.OwnedPointerId.HasValue&&(worldResident!=null||!IsGuided))return;life.StartInteraction();if(IsGuided){Movement.CancelInput();IsGuided=false;FreezeMovement();}if(Movement.TryBeginDrag(e.pointerId))dragged=false;}
        public void OnBeginDrag(PointerEventData e)
        {if(!Controlled||!life.CanInteract)return;if(!Movement.OwnedPointerId.HasValue)OnPointerDown(e);if(Movement.OwnedPointerId==e.pointerId){dragged=true;e.eligibleForClick=false;}}
        public void OnDrag(PointerEventData e)
        {if(Controlled&&life.CanInteract&&Movement.OwnedPointerId==e.pointerId){dragged=true;if(worldResident!=null)life.World.FollowPointer(e.position,e.pressEventCamera);else Movement.SetTarget(e.pointerId,life.Source(e.position,e.pressEventCamera));}}
        public void OnEndDrag(PointerEventData e)=>Release(e);
        public void OnPointerUp(PointerEventData e)=>Release(e);
        private void Release(PointerEventData e)
        {if(!Controlled||Movement.OwnedPointerId!=e.pointerId)return;Movement.ReleasePointer(e.pointerId);if(worldResident!=null&&dragged)life.World.ReleasePointer();FreezeMovement();life.ActorReleased(this);if(dragged)e.eligibleForClick=false;}
        public void OnPointerClick(PointerEventData e){if(!dragged){if(Controlled&&life.CanInteract)life.GetComponent<TownStreetWalkView>()?.Arm();else React();}}
        private void OnDisable(){Movement?.CancelInput();ResetVisual();dragged=false;}
    }
    // One unlit, noninteractive ellipse. Smooth alpha falloff replaces the
    // Noah-only hard rounded rectangles; the legacy shadows stay available
    // unchanged for the causal OFF comparison. No texture/material allocation.
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class NoahGroundShadowGraphic : UnityEngine.UI.MaskableGraphic
    {
        private bool configured,depthFocus,finalRig;
        internal void ConfigureGrounding(bool focus)
        {
            if(configured&&depthFocus==focus&&!finalRig)return;configured=true;depthFocus=focus;finalRig=false;ApplyGrounding(TownNoahDepthFocus.Grounding(focus));
        }
        internal void ConfigureFinalRig()
        {
            if(configured&&finalRig)return;configured=true;finalRig=true;ApplyGrounding(TownNoahFinalRig.Grounding);
        }
        private void ApplyGrounding(NoahGroundingProfile p)
        {
            rectTransform.anchorMin=new Vector2(p.X,p.Y);rectTransform.anchorMax=new Vector2(p.X+p.Width,p.Y+p.Height);rectTransform.offsetMin=rectTransform.offsetMax=Vector2.zero;
            color=new Color(.18f,.14f,.20f,p.Alpha);SetVerticesDirty();
        }
        private static readonly float[] radii={.25f,.55f,.80f,1};
        private static readonly float[] opacity={.90f,.50f,.15f,0};
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            const int sectors=24;vh.AddVert(r.center,color,Vector2.zero);
            for(int ring=0;ring<radii.Length;ring++)for(int i=0;i<sectors;i++)
            {
                float angle=i*(Mathf.PI*2/sectors);var tint=color;tint.a*=finalRig?TownNoahFinalRig.GroundOpacity(radii[ring]):depthFocus?TownNoahDepthFocus.GroundOpacity(radii[ring],true):opacity[ring];
                vh.AddVert(new Vector2(r.center.x+Mathf.Cos(angle)*r.width*.5f*radii[ring],r.center.y+Mathf.Sin(angle)*r.height*.5f*radii[ring]),tint,Vector2.zero);
            }
            for(int i=0;i<sectors;i++)
            {
                int next=(i+1)%sectors;vh.AddTriangle(0,1+i,1+next);
                for(int ring=0;ring<radii.Length-1;ring++)
                {
                    int a=1+ring*sectors+i,b=1+ring*sectors+next,c=b+sectors,d=a+sectors;
                    vh.AddTriangle(a,d,c);vh.AddTriangle(a,c,b);
                }
            }
        }
    }
}
