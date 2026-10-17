using System;
using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownLifeView : MonoBehaviour
    {
        public static readonly TownPoint RequestEntrance=TownPlaceCatalog.RequestEntrance,ShopEntrance=TownPlaceCatalog.ShopEntrance;
        public static System.Collections.Generic.IReadOnlyList<TownPoint> WalkStops=>TownPlaceCatalog.ForLevel(0).Nodes;
        public TownStreetMap StreetMap {get;private set;}
        public TownRegionSession Regions {get;private set;}
        private TownContinuousWorldView continuousWorld;
        public TownContinuousWorldView World=>continuousWorld!=null?continuousWorld:continuousWorld=GetComponent<TownContinuousWorldView>();
        public System.Collections.Generic.IReadOnlyList<TownActor> Actors {get;private set;}
        public int TownLevel=>context?.Game.State.townGrowthLevel??0;
        public RawImage Background=>background;
        public string NearbyFacility {get;private set;}
        internal bool CanSimulate=>isActiveAndEnabled&&context!=null&&!context.Flow.IsModalOpen&&!context.Flow.IsAppPaused&&context.Flow.CurrentScreen==ScreenId.Home;
        public bool CanInteract=>CanSimulate&&!(Regions?.IsHeroTransitioning??false);
        public bool IsManuallyDragging=>actors!=null&&actors.Any(a=>a.Controlled&&a.Movement.OwnedPointerId.HasValue&&(World?.IsReady==true||!a.IsGuided));
        private RawImage background;private ScreenContext context;private TownActor[] actors;private RectTransform actorLayer;
        private GameObject bubble,entrance,outings;private TownActor speaker;private TownPlace destination;private float now,nextTalk,bubbleUntil,nextSocial;private int choiceIndex;
        private readonly TownFrameClock clock=new TownFrameClock();
        public void Bind(ScreenContext c,RawImage image)
        {
            context=c;background=image;actorLayer=(RectTransform)transform;actorLayer.gameObject.AddComponent<RectMask2D>();
            StreetMap=TownPlaceCatalog.ForLevel(c.Game.State.townGrowthLevel);var nav=new TownNavigation(StreetMap.Nodes,StreetMap.Edges,.028f);actors=new TownActor[6];
            string[] initial={"bench-left","home-front","fountain","bench-right","flowers"};int npc=0;
            for(int i=0;i<6;i++)
            {
                var character=CharacterCatalog.All[i];bool controlled=character.Id==c.Game.State.favoriteCharacterId;TownPlace place=null;
                if(!controlled)TownPlaceCatalog.TryFind(StreetMap,initial[npc++],out place);
                var point=controlled?StreetMap.Nodes[0]:place.Position;
                var rect=UIWidgets.Node("Town actor "+character.Id,actorLayer,new Rect(.5f,.5f,0,0));rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(62,78);
                var actor=rect.gameObject.AddComponent<TownActor>();actor.Bind(character.Id,controlled,new TownMovement(nav,point),nav);actor.Connect(this,point,point,1.6f+i*.4f);if(!controlled)actor.SetDestination(place,1.6f+i*.4f);actors[i]=actor;
                var ground=c.Widgets.Panel("Town actor ground shadow",rect,new Rect(.17f,-.035f,.66f,.095f),new Color(.24f,.19f,.26f,.18f));ground.radius=100;ground.decorated=false;ground.transform.SetAsFirstSibling();
                var panel=c.Widgets.Panel("Town actor name panel",rect,new Rect(-.13f,-.26f,1.26f,.26f)).rectTransform;
                c.Widgets.NarrativeLabel("Town actor name",character.Name,panel,new Rect(.04f,.03f,.92f,.94f),12);
                actor.Render(0,0);
            }
            AddEntrance("依頼部屋",RequestEntrance,"quest",UIWidgets.BluePaper);AddEntrance("ショップ",ShopEntrance,"shop",UIWidgets.BlushPaper);
            Actors=Array.AsReadOnly(actors);Regions=gameObject.AddComponent<TownRegionSession>();Regions.Bind(c,this,Actors);
            nextTalk=4.5f;nextSocial=3;
        }
        private void AddEntrance(string name,TownPoint location,string id,Color tint)
        {
            var marker=context.Widgets.Button("Facility marker "+name,name,actorLayer,new Rect(.5f,.5f,0,0),()=>GuideToFacility(id),true);
            var rect=(RectTransform)marker.transform;rect.sizeDelta=new Vector2(92,48);rect.anchoredPosition=MarkerLocal(location);
            marker.GetComponent<RoundedPanelGraphic>().color=tint;marker.GetComponentInChildren<Text>().fontSize=14;
        }
        public bool GuideToFacility(string id)=>id=="quest"||id=="shop"?GuideToPlace(id):false;
        public void OpenOutings(){if(CanInteract)ShowOutings();}
        public bool GuideToStreetPoint(TownPoint point)
        {
            if(World?.IsReady==true)return World.GuidePoint(TownWorldLayout.ToWorld(Regions.HeroRegion,point));
            if(!CanInteract||Regions.ViewingRegion!=Regions.HeroRegion)return false;
            if(!TownStreetSelection.TryProjectRoad(Regions.HeroRegion,TownLevel,point,out var target))return false;
            var hero=actors.Single(a=>a.Controlled);return hero.GuideTo(target);
        }
        public bool GuideToRestStop(string id)
        {
            var region=Regions?.HeroRegion??TownRegionId.Central;
            string prefix=region==TownRegionId.West?"west.":region==TownRegionId.East?"east.":"";
            return id=="bench"?GuideToPlace(prefix.Length==0?"bench-right":prefix+"bench-front"):
                id=="flowers"?GuideToPlace(prefix+"flowers"):id=="fountain"?GuideToPlace("fountain"):false;
        }
        public bool GuideToPlace(string id)
        {
            if(World?.IsReady==true)return World.GuidePlace(id);
            if(Regions!=null)return Regions.GuideToPlace(id);
            if(!CanInteract||!TownPlaceCatalog.TryFind(StreetMap,id,out var place))return false;
            foreach(var a in actors)if(a.Controlled&&a.GuideTo(place.Position)){destination=place;return true;}return false;
        }
        internal void ChooseNextStop(TownActor actor)
        {
            if(Regions?.OwnsJourney(actor)==true)return;
            if(Regions?.TryStartVisitAfterRest(actor)==true)return;
            var candidates=TownRegionCatalog.ForLevel(actor.RegionId,TownLevel).Map.Places.Where(p=>p.Activity!=TownActivity.Entrance&&p.Id!="square").ToArray();
            for(int k=0;k<candidates.Length;k++)
            {
                var p=candidates[(choiceIndex+k)%candidates.Length];
                if(p.Id==actor.DestinationId||Regions?.IsStopReserved(p.Id,actor)==true||actors.Any(a=>!a.Controlled&&a!=actor&&a.RegionId==actor.RegionId&&a.DestinationId==p.Id))continue;
                choiceIndex=(choiceIndex+k+1)%candidates.Length;actor.SetDestination(p,0);return;
            }
        }
        internal float MovementScale(TownActor actor)
        {
            foreach(var other in actors)
            {
                if(other==actor||other.RegionId!=actor.RegionId||Distance(actor.Position,other.Position)>.035f)continue;
                if(other.Controlled||CharacterCatalog.Find(other.CharacterId).PortraitIndex<CharacterCatalog.Find(actor.CharacterId).PortraitIndex)return .45f;
            }return 1;
        }
        public bool TryBeginConversation(TownActor first,TownActor second)
        {
            if(!CanInteract||first==null||second==null||first==second||first.RegionId!=second.RegionId||!first.RegionVisible||!second.RegionVisible||first.IsRegionFading||second.IsRegionFading||now<nextSocial||IsManuallyDragging||GetComponent<TownViewportView>()?.IsDragging==true||outings!=null||entrance!=null||!WithinConversationRange(first,second)||!Visible(first)||!Visible(second))return false;
            first.FaceTowards(second.Position);second.FaceTowards(first.Position);first.Greet();second.Greet();nextSocial=now+8;
            ShowBubble(CharacterCatalog.Find(first.CharacterId).Name+"：こんにちは、"+CharacterCatalog.Find(second.CharacterId).Name+"♪",first);return true;
        }
        private bool WithinConversationRange(TownActor first,TownActor second)
        {
            if(World?.IsReady!=true)return Distance(first.Position,second.Position)<=.09f;
            float width=Mathf.Min(((RectTransform)first.transform).rect.width,((RectTransform)second.transform).rect.width);
            return TownWorldLayout.WithinConversationRange(first.WorldPosition,second.WorldPosition,background.rectTransform.rect.width,width);
        }
        private void ShowOutings()
        {
            Regions?.CancelHeroJourney();GetComponent<TownStreetWalkView>()?.CancelSelection();GetComponent<TownViewportView>()?.CancelGesture();
            foreach(var actor in actors)if(actor.Controlled){actor.Movement?.CancelInput();actor.ResetVisual();}destination=null;
            HideOutings();HideBubble();HideEntrance();
            outings=TownOutingMenu.Open(context,this,(RectTransform)transform.parent);nextTalk=now+9;
        }
        public bool DismissOutings(){if(outings==null)return false;HideOutings();return true;}
        private void Update()=>AdvanceFrame(Time.unscaledDeltaTime);
        public void AdvanceFrame(float seconds)
        {
            if(context==null||actors==null)return;if(!CanSimulate){CancelInteraction();return;}
            if(!float.IsFinite(seconds)||seconds<=0)return;now+=Mathf.Min(seconds,.1f);
            Regions?.BeginPresentationFrame();
            int steps=clock.AddElapsed(seconds);for(int step=0;step<steps;step++){if(World?.IsReady==true)World.Step(TownFrameClock.StepSeconds);else{foreach(var a in actors)a.Advance(TownFrameClock.StepSeconds);Regions?.Step(TownFrameClock.StepSeconds);}}
            var camera=GetComponent<TownViewportView>();if(camera!=null){foreach(var a in actors)if(a.Controlled&&a.RegionVisible&&(a.IsGuided||!IsManuallyDragging&&!camera.IsDragging))camera.Focus(a.Position);camera.Advance(seconds);}
            foreach(var a in actors)a.Render(clock.Alpha,seconds);
            Array.Sort(actors,(a,b)=>(World?.IsReady==true?b.WorldPosition.Y.CompareTo(a.WorldPosition.Y):b.RenderPosition.Y.CompareTo(a.RenderPosition.Y)));foreach(var a in actors)a.transform.SetAsLastSibling();
            if(bubble!=null&&now>=bubbleUntil)HideBubble();
            if(IsManuallyDragging){HideBubble();return;}
            if(now>=nextSocial)for(int i=0;i<actors.Length;i++)for(int j=i+1;j<actors.Length;j++)if(TryBeginConversation(actors[i],actors[j]))break;
            if(bubble==null&&outings==null&&entrance==null&&now>=nextTalk)
            {
                var actor=actors.FirstOrDefault(a=>!a.Controlled&&a.IsResting&&Visible(a));if(actor!=null)ShowBubble(CharacterCatalog.Find(actor.CharacterId).Name+"："+StreetLine(actor),actor);nextTalk=now+6;
            }
            foreach(var a in actors)
            {
                var label=a.transform.Find("Town actor name panel");if(label!=null)label.gameObject.SetActive(a.RegionVisible&&(a.Controlled||a==speaker||a.IsResting&&Visible(a)));
            }
            PositionBubble();
        }
        private bool Visible(TownActor a)
        {if(!a.RegionVisible)return false;var p=World?.IsReady==true?World.Local(a.WorldPosition):Local(a.Position);var r=actorLayer.rect;return p.x>r.xMin+38&&p.x<r.xMax-38&&p.y>r.yMin+24&&p.y<r.yMax-86;}
        private static float Distance(TownPoint a,TownPoint b)=>Vector2.Distance(new Vector2(a.X,a.Y),new Vector2(b.X,b.Y));
        public Vector2 Local(TownPoint point)
        {if(World?.IsReady==true)return World.Local(TownWorldLayout.ToWorld(Regions.ViewingRegion,point));if(background==null)return Vector2.zero;var uv=background.uvRect;var r=background.rectTransform.rect;var p=r.min+Vector2.Scale((new Vector2(point.X,point.Y)-uv.min)/uv.size,r.size);return actorLayer.InverseTransformPoint(background.rectTransform.TransformPoint(p));}
        public TownPoint Source(Vector2 screen,Camera camera)
        {if(World?.IsReady==true)return TownWorldLayout.ToLocal(Regions.ViewingRegion,World.Source(screen,camera));if(background==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform,screen,camera,out var p))return StreetMap.Nodes[0];var r=background.rectTransform.rect;var uv=background.uvRect;var n=(p-r.min)/r.size;return new TownPoint(uv.x+n.x*uv.width,uv.y+n.y*uv.height);}
        public void StartInteraction(bool preserveJourney=false){if(!preserveJourney)Regions?.CancelHeroJourney();GetComponent<TownStreetWalkView>()?.CancelSelection();GetComponent<TownViewportView>()?.CancelGesture();destination=null;HideOutings();HideBubble();HideEntrance();nextTalk=now+6;}
        internal void SetGuidedDestination(TownPlace place)=>destination=place;
        internal void ApplyViewingRegion(TownRegionId region)
        {
            if(context==null||actors==null)return;
            StreetMap=TownRegionCatalog.ForLevel(region,TownLevel).Map;HideBubble();HideOutings();HideEntrance();
            if(World?.IsReady!=true)foreach(var a in actors)a.SetRegionVisible(a.RegionId==region);
            foreach(string name in new[]{"依頼部屋","ショップ"}){var marker=actorLayer.Find("Facility marker "+name);if(marker!=null)marker.gameObject.SetActive(region==TownRegionId.Central);}
            RefreshViewport();
        }
        public void ActorReleased(TownActor actor)
        {
            if(!CanInteract||!actor.Controlled||actor.Movement.OwnedPointerId.HasValue||World?.Model?.Hero.IsWalking==true)return;actor.FreezeMovement();HideEntrance();
            if(Regions?.OwnsJourney(actor)==true||!actor.RegionVisible)return;
            string facility=actor.RegionId!=TownRegionId.Central?null:Distance(actor.Position,RequestEntrance)<.034f?"quest":Distance(actor.Position,ShopEntrance)<.034f?"shop":null;
            if(facility==null){if(destination!=null&&Distance(actor.Position,destination.Position)<.034f){actor.Surprise();ShowBubble(destination.Dialogue,actor);destination=null;}return;}
            NearbyFacility=facility;HideBubble();actor.Surprise();var root=(RectTransform)transform.parent;
            var b=context.Widgets.Button("Enter facility",facility=="quest"?"依頼部屋に入る":"ショップへ",root,new Rect(0,0,1,1),()=>
            {
                if(!CanInteract||NearbyFacility!=facility||actor.RegionId!=TownRegionId.Central||!actor.RegionVisible)return;
                if(facility=="quest")context.Flow.ShowQuest();else context.Flow.OpenShopPreview();
            });UIWidgets.Bottom((RectTransform)b.transform,48,178,48,48);entrance=b.gameObject;
        }
        internal void React(TownActor a)
        {if(!CanInteract||IsManuallyDragging||entrance!=null)return;context.Flow.PlayCue(AudioCue.Character);if(a.Controlled)ShowOutings();else{HideOutings();bool recorded=context.Flow.TalkToCharacter(a.CharacterId);ShowBubble(recorded?CharacterCatalog.Find(a.CharacterId).Name+"："+StreetLine(a):context.Flow.Daily.LastError,a);}}
        private string StreetLine(TownActor actor)
        {string reunion=QuestStoryPresentation.ReunionFor(actor.CharacterId,context.Game.State.completedQuestIds);var visits=context.Flow.Daily.Data.Characters.First(c=>c.Id==actor.CharacterId).Meetings;return reunion.Length>0&&visits%3==1?reunion:TownCharacterDialogue.For(actor.CharacterId,visits);}
        private void ShowBubble(string text,TownActor a)
        {
            HideBubble();speaker=a;var root=(RectTransform)transform.parent;var p=context.Widgets.StoryPanel("Town bubble panel",root,new Rect(.5f,.5f,0,0),true).rectTransform;p.sizeDelta=new Vector2(Mathf.Min(288,root.rect.width-28),116);
            string who=CharacterCatalog.Find(a.CharacterId).Name;if(text.StartsWith(who+"：",StringComparison.Ordinal))text=text.Substring(who.Length+1);
            var name=context.Widgets.Panel("Speaker name plate",p,new Rect(.05f,.70f,.36f,.24f),UIWidgets.RosePaper);name.radius=8;name.gloss=.05f;
            context.Widgets.StoryHeading("Speaker name",who,name.rectTransform,new Rect(.04f,0,.92f,1),14);
            context.Widgets.NarrativeLabel("Town bubble",text,p,new Rect(.07f,.09f,.86f,.58f),15);bubble=p.gameObject;bubbleUntil=now+4;PositionBubble();
        }
        private void PositionBubble()
        {
            if(bubble==null||speaker==null)return;var root=(RectTransform)transform.parent;var p=(RectTransform)bubble.transform;var world=actorLayer.TransformPoint(World?.IsReady==true?World.Local(speaker.WorldPosition):Local(speaker.RenderPosition));var target=(Vector2)root.InverseTransformPoint(world)+Vector2.up*122;
            var r=root.rect;float half=p.rect.width/2;target.x=Mathf.Clamp(target.x,r.xMin+half+14,r.xMax-half-14);target.y=Mathf.Clamp(target.y,r.yMin+178+p.rect.height/2,r.yMax-142-p.rect.height/2);p.anchoredPosition=target;
        }
        public void SetViewScale(float scale){if(actors!=null)foreach(var a in actors)a.SetViewScale(scale);}
        public void RefreshViewport()
        {if(actorLayer==null)return;foreach(var item in new[]{("依頼部屋",RequestEntrance),("ショップ",ShopEntrance)}){var r=actorLayer.Find("Facility marker "+item.Item1) as RectTransform;if(r!=null)r.anchoredPosition=MarkerLocal(item.Item2);}PositionBubble();}
        private Vector2 MarkerLocal(TownPoint location)
        {
            var point=Local(location)+Vector2.up*104;var bounds=actorLayer.rect;
            // A short safe area cannot always pan enough to fit a sign above a
            // painted doorway. Keep its 48-unit control visible inside the town;
            // navigation still targets the unchanged original entrance point.
            point.x=Mathf.Clamp(point.x,bounds.xMin+50,bounds.xMax-50);
            point.y=Mathf.Clamp(point.y,bounds.yMin+28,bounds.yMax-28);return point;
        }
        private void HideBubble(){if(bubble!=null){bubble.SetActive(false);Destroy(bubble);}bubble=null;speaker=null;}
        private void HideEntrance(){NearbyFacility=null;if(entrance!=null){entrance.SetActive(false);Destroy(entrance);}entrance=null;}
        private void HideOutings(){if(outings!=null){outings.SetActive(false);Destroy(outings);}outings=null;}
        public void CancelInteraction(){Regions?.CancelJourneys();GetComponent<TownStreetWalkView>()?.CancelSelection();GetComponent<TownViewportView>()?.CancelGesture();clock.Reset();destination=null;HideOutings();if(actors!=null)foreach(var a in actors){a?.Movement?.CancelInput();a?.ResetVisual();}HideBubble();HideEntrance();nextTalk=now+6;}
        private void OnRectTransformDimensionsChange(){CancelInteraction();RefreshViewport();}
        private void OnDisable()=>CancelInteraction();
    }
}
