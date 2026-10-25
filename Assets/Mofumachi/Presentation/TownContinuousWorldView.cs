using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // One field projection for five plazas, paths, landmarks, doors and all six residents.
    public sealed class TownContinuousWorldView : MonoBehaviour
    {
        public TownWorldWalkModel Model {get;private set;}
        public TownWorldCamera Camera {get;}=new TownWorldCamera();
        public bool IsReady=>Model!=null;
        internal bool CanAnimateWater=>life!=null&&life.CanSimulate;
        public bool FollowingHero {get;private set;}=true;
        public int VisibleBackgroundCount=>ground!=null&&ground.isActiveAndEnabled?1:0;
        private TownLifeView life;private RawImage original;private RectTransform scenery;
        private TownGroundGraphic ground;private TownCentralGroundGraphic centralGround;private TownWestGroundGraphic westGround;private TownEastGroundGraphic eastGround;private TownNorthGroundGraphic northGround;private TownSouthGroundGraphic southGround;private TownSceneryView props;
        private readonly List<(Transform Transform,float Feet)> depth=new List<(Transform,float)>();
        private readonly List<TownDiscoveryNotice> discoveryNotices=new List<TownDiscoveryNotice>(2);
        private RectTransform Field=>(RectTransform)life.transform;
        private RectTransform dailyFlower;private int displayedFlowers;
        private ScreenContext context;private TownFountainGraphic fountain;private TownFountainWaterGraphic fountainWater;
        private TownGrowthView growth;private TownAmbientGraphic ambient;
        private TownPoint renderedCenter;private Vector2 renderedSize;private bool sceneReady;
        private RectTransform topHud,locationRibbon,navigationBar;

        private TownRegionId? lookRegion;
        private bool trackingFinger;private Vector2 fingerScreen;private UnityEngine.Camera fingerCamera;
        internal float RenderScale=>TownWorldLayout.ScaleForViewport(Field.rect.width,Field.rect.height);
        public event Action<string,string,bool> Arrived;
        public void Bind(ScreenContext c,TownLifeView town)
        {
            context=c;life=town;original=town.Background;Model=new TownWorldWalkModel(town.TownLevel,c.Game.State.favoriteCharacterId);
            scenery=UIWidgets.Node("Continuous town scenery",(RectTransform)original.transform.parent,new Rect(0,0,1,1));scenery.SetSiblingIndex(original.transform.GetSiblingIndex()+1);

            original.enabled=false;var cover=original.GetComponent<BackgroundCover>();if(cover!=null)cover.enabled=false;
            ground=UIWidgets.Node("Continuous shared paving",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownGroundGraphic>();ground.Bind(this);
            centralGround=UIWidgets.Node("Central master ground",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownCentralGroundGraphic>();centralGround.Bind(this);
            westGround=UIWidgets.Node("West cafe master ground",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownWestGroundGraphic>();westGround.Bind(this);
            eastGround=UIWidgets.Node("East florist master ground",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownEastGroundGraphic>();eastGround.Bind(this);
            northGround=UIWidgets.Node("North quest master ground",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownNorthGroundGraphic>();northGround.Bind(this);
            southGround=UIWidgets.Node("South garden master ground",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownSouthGroundGraphic>();southGround.Bind(this);
            props=life.gameObject.AddComponent<TownSceneryView>();props.Bind(this);props.SetTownLevel(town.TownLevel);
            foreach(var actor in life.Actors)actor.BindWorld(Model.Residents.Single(r=>r.Id==actor.CharacterId));
            fountain=UIWidgets.Node("Town fountain foreground",(RectTransform)life.transform,new Rect(0,0,1,1)).gameObject.AddComponent<TownFountainGraphic>();fountain.Bind(this);
            fountainWater=UIWidgets.Node("Fountain flowing water",fountain.rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<TownFountainWaterGraphic>();fountainWater.Bind(this);
            life.gameObject.AddComponent<TownFacilityView>().Bind(c,life);
            Model.Arrived+=OnArrival;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+TownSceneryLayout.CameraLift),Model.Hero.Position);Refresh();
        }
        public void Step(float seconds)
        {
            if(Model==null||!life.CanSimulate)return;
            if(trackingFinger&&life.IsManuallyDragging&&life.CanInteract)Model.FollowFinger(Source(fingerScreen,fingerCamera));
            Model.Step(seconds);foreach(var actor in life.Actors)actor.ApplyWorldStep(seconds);
        }
        public bool FollowPointer(Vector2 screen,UnityEngine.Camera camera)
        {if(!life.CanInteract||!life.IsManuallyDragging)return false;fingerScreen=screen;fingerCamera=camera;trackingFinger=true;FollowingHero=true;return Model.FollowFinger(Source(screen,camera));}
        public void ReleasePointer(){trackingFinger=false;Model.ReleaseFinger();SyncActors();}
        public void AdvanceCamera(float seconds)
        {if(!IsReady||!life.CanSimulate)return;if(FollowingHero)Camera.Follow(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+TownSceneryLayout.CameraLift),seconds,Model.Hero.Position);Refresh();}
        private Vector2 Project(TownPoint p)
        {var rect=Field.rect;float scale=RenderScale;return rect.center+new Vector2((p.X-Camera.Center.X)*scale,(p.Y-Camera.Center.Y)*scale*TownWorldLayout.ImageAspect);}
        public Vector2 Local(TownPoint world,RectTransform target=null)
        {return (target??(RectTransform)life.transform).InverseTransformPoint(Field.TransformPoint(Project(world)));}
        public TownPoint Source(Vector2 screen,UnityEngine.Camera camera)
        {if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Field,screen,camera,out var p))return Model.Hero.Position;var r=Field.rect;float scale=RenderScale;return new TownPoint(Camera.Center.X+(p.x-r.center.x)/scale,Camera.Center.Y+(p.y-r.center.y)/(scale*TownWorldLayout.ImageAspect));}
        public bool GuidePoint(TownPoint point,bool preserveMeeting=false)
        {
            if(!life.CanInteract||!TownWorldLayout.Finite(point))return false;
            if(!Model.TryGuidePoint(point))return false;if(!preserveMeeting)life.GetComponent<TownMeetingJourney>()?.Cancel();life.StartInteraction(true);FollowingHero=true;SyncActors();return true;
        }
        public bool GuidePlace(string id)
        {if(!life.CanInteract||life.IsManuallyDragging||!Model.TryGuide(Model.Hero.Id,id))return false;life.GetComponent<TownMeetingJourney>()?.Cancel();life.StartInteraction(true);FollowingHero=true;SyncActors();return true;}
        public void FastTravel(TownRegionId region)=>FastTravel((TownDistrictId)region);
        public void FastTravel(TownDistrictId district)
        {if(!life.CanInteract||life.IsManuallyDragging)return;life.StartInteraction();Model.FastTravel(district);FollowingHero=true;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+TownSceneryLayout.CameraLift),Model.Hero.Position);SyncActors();Refresh();}
        public bool GuideDistrict(TownDistrictId district)
        {if(!life.CanInteract||life.IsManuallyDragging||!Model.TryGuideDistrict(district))return false;life.GetComponent<TownMeetingJourney>()?.Cancel();life.StartInteraction(true);FollowingHero=true;SyncActors();return true;}
        public bool LookAt(TownRegionId region)=>Enum.IsDefined(typeof(TownRegionId),region)&&LookAt((TownDistrictId)region);
        public bool LookAt(TownDistrictId district)
        {if(!life.CanInteract||!Enum.IsDefined(typeof(TownDistrictId),district))return false;FollowingHero=false;lookRegion=TownDistrictLayout.LegacyRegion(district);var p=TownWorldMap.Place(TownDistrictLayout.EntranceId(district));Camera.Jump(new TownPoint(p.X,p.Y+TownSceneryLayout.CameraLift));Refresh();return true;}
        public void ReturnToHero(){FollowingHero=true;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+TownSceneryLayout.CameraLift),Model.Hero.Position);Refresh();}
        public void Pan(Vector2 delta){life.GetComponent<TownMeetingJourney>()?.Cancel();life.GetComponent<TownFacilityView>()?.Cancel();FollowingHero=false;lookRegion=null;float scale=RenderScale;Camera.Pan(-delta.x/scale,-delta.y/(scale*TownWorldLayout.ImageAspect));Refresh();}
        public void CancelHero(){life.GetComponent<TownMeetingJourney>()?.Cancel();trackingFinger=false;Model?.CancelHero();if(Model!=null)SyncActors();}
        public void Freeze(){life.GetComponent<TownMeetingJourney>()?.Cancel();trackingFinger=false;Model?.CancelHero();foreach(var actor in life.Actors)actor.ApplyWorldStep(0);}
        private void OnArrival(string id,string placeId,bool walked)
        {
            var actor=life.Actors.Single(a=>a.CharacterId==id);actor.ApplyWorldStep(0);
            if(TownRegionCatalog.TryFindPlace(placeId,life.TownLevel,out _,out var place))
            {actor.SetWorldPlace(place);if(actor.Controlled)life.SetGuidedDestination(place);}
            if(actor.Controlled)life.ActorReleased(actor);
            actor.Surprise();if(actor.Controlled){if(walked)context.Flow.RecordWorldWalk();if(Model.Hero.DistanceWalked>=.14f)context.Flow.RecordCharacterOuting(id);}Arrived?.Invoke(id,placeId,walked);
        }
        private void SyncActors(){foreach(var actor in life.Actors)actor.ApplyWorldStep(0);}
        public void Refresh()
        {
            if(!IsReady||original==null||scenery==null)return;
            props?.SetTownLevel(life.TownLevel);
            var bounds=Field.rect;float scale=RenderScale;
            if(topHud==null)topHud=Field.parent.Find("HUD") as RectTransform;
            if(locationRibbon==null)locationRibbon=Field.parent.Find("Town location ribbon") as RectTransform;
            if(navigationBar==null)navigationBar=Field.parent.Find("Navigation") as RectTransform;
            float TopInset(RectTransform overlay)=>overlay==null?0:Mathf.Max(0,bounds.yMax-Field.InverseTransformPoint(overlay.TransformPoint(new Vector2(0,overlay.rect.yMin))).y+4)/scale;
            float bottomInset=navigationBar==null?0:Mathf.Max(0,Field.InverseTransformPoint(navigationBar.TransformPoint(new Vector2(0,navigationBar.rect.yMax))).y-bounds.yMin+4)/scale;
            // Existing HUD/ribbon/actions float over this full-field rectangle.
            // Visible alone is insufficient if a Menu/Merge button takes the grab.
            float topInset=Mathf.Max(TopInset(topHud),TopInset(locationRibbon));
            // Discovery cards are real buttons below the location ribbon. Keep
            // them working without letting a new discovery capture the hero's
            // return drag at the North edge. Ignore inactive/replaced notices.
            // Reuse storage: no per-frame array or object allocation.
            Field.parent.GetComponentsInChildren(false,discoveryNotices);
            foreach(var notice in discoveryNotices)topInset=Mathf.Max(topInset,TopInset((RectTransform)notice.transform));
            Camera.Fit(bounds.width/scale,bounds.height/(scale*TownWorldLayout.ImageAspect),topInset,bottomInset);
            bool sceneChanged=!sceneReady||!renderedCenter.Equals(Camera.Center)||renderedSize!=bounds.size;
            if(sceneChanged){ground.Refresh();centralGround.Refresh();westGround.Refresh();eastGround.Refresh();northGround.Refresh();southGround.Refresh();props.Refresh();fountain?.Refresh();fountainWater?.NotifyViewportChanged();renderedCenter=Camera.Center;renderedSize=bounds.size;sceneReady=true;}
            foreach(var actor in life.Actors)
            {
                var p=Local(actor.WorldPosition);var r=((RectTransform)life.transform).rect;
                // Restore/pan can refresh the scenery before the next actor
                // animation frame. Reuse the culling projection now, without
                // moving the model or advancing the character pose.
                var rect=(RectTransform)actor.transform;if(rect.anchoredPosition!=p)rect.anchoredPosition=p;
                actor.SetRegionVisible(p.x>r.xMin-64&&p.x<r.xMax+64&&p.y>r.yMin-90&&p.y<r.yMax+90);
            }
            var viewing=FollowingHero?Model.Hero.Region:lookRegion??TownWorldLayout.RegionAt(Camera.Center);life.Regions.NotifyWorldRegion(viewing);
            int count=Math.Min(6,context.Flow.Daily.Data.DecorationDays.Count);
            if(count>0)
            {
                if(dailyFlower==null){dailyFlower=UIWidgets.Node("Daily collected flower garden",(RectTransform)life.transform,new Rect(.5f,.5f,0,0));dailyFlower.sizeDelta=new Vector2(70,35);dailyFlower.SetAsFirstSibling();}
                while(displayedFlowers<count){int slot=displayedFlowers++;var icon=context.Widgets.Icon("Collected flower art "+slot,UIIcon.Flower,dailyFlower,new Rect(.03f+slot%3*.31f,slot/3*.4f,.29f,.58f));icon.color=Color.Lerp(new Color(1,.70f,.80f),new Color(1,.92f,.55f),slot/5f);}
                dailyFlower.anchoredPosition=Local(new TownPoint(-.20f,-2.72f));
            }
            life.GetComponent<TownFacilityView>()?.Refresh();
            life.RefreshViewport();if(growth==null)growth=life.GetComponentInChildren<TownGrowthView>();if(growth!=null){if(growth.Region!=viewing)growth.SetRegion(viewing);growth.NotifyWorldCamera();}
            if(ambient==null)ambient=life.transform.parent.GetComponentInChildren<TownAmbientGraphic>();if(ambient!=null)
            {
                if(ambient.transform.parent!=fountain.transform)
                {var r=ambient.rectTransform;r.SetParent(fountain.transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
                ambient.World=this;ambient.NotifyViewportChanged();
            }
        }
        internal void SortResidents(TownActor[] ordered)
        {
            depth.Clear();
            if(props!=null)foreach(var piece in props.Pieces)if(piece.Rect.gameObject.activeSelf)depth.Add((piece.Rect,TownSouthMasterLayout.DepthY(piece.Placement)));
            // Texture bottom includes the front basin/flower silhouette. Depth
            // belongs to the physical footprint center, so front-side visitors
            // are not covered while the far-side path stays behind the fountain.
            if(fountain!=null)depth.Add((fountain.transform,TownCentralMasterLayout.FountainCenterY));
            if(growth!=null)growth.AddDepthEntries(depth);
            life.GetComponent<TownFacilityView>()?.AddDepthEntries(depth);
            foreach(var actor in ordered)if(actor.RegionVisible)depth.Add((actor.transform,actor.WorldPosition.Y));
            depth.Sort((a,b)=>{int order=b.Feet.CompareTo(a.Feet);return order!=0?order:a.Transform.GetSiblingIndex().CompareTo(b.Transform.GetSiblingIndex());});
            bool orderedAlready=true;int previous=-1;
            foreach(var entry in depth){int index=entry.Transform.GetSiblingIndex();if(index<=previous){orderedAlready=false;break;}previous=index;}
            if(!orderedAlready)foreach(var entry in depth)entry.Transform.SetAsLastSibling();
            life.GetComponent<TownFacilityView>()?.RaiseControls();
        }
        public TownWorldSnapshot Capture()=>Model==null?null:new TownWorldSnapshot(Model.Residents.Select(r=>r.Id).ToArray(),Model.Residents.Select(r=>r.Position).ToArray(),Model.Residents.Select(r=>r.PlaceId).ToArray(),Camera.Center,FollowingHero,Model.Hero.Id,lookRegion);
        public void Restore(TownWorldSnapshot snapshot)
        {if(snapshot==null||snapshot.FavoriteId!=Model.Hero.Id)return;for(int i=0;i<snapshot.Ids.Length;i++)Model.Restore(snapshot.Ids[i],snapshot.Positions[i],snapshot.Places[i]);FollowingHero=snapshot.Follow;lookRegion=snapshot.ViewedRegion;Camera.Jump(snapshot.Center,FollowingHero?(TownPoint?)Model.Hero.Position:null);SyncActors();Refresh();}
        private void OnDisable()
        {
            if(Model!=null)Model.Arrived-=OnArrival;
            if(scenery!=null){scenery.gameObject.SetActive(false);Destroy(scenery.gameObject);}
            if(original!=null){original.enabled=true;var cover=original.GetComponent<BackgroundCover>();if(cover!=null){cover.enabled=true;cover.FitNow();}}
        }
    }
    public sealed class TownWorldSnapshot
    {
        public string[] Ids {get;} public TownPoint[] Positions {get;} public string[] Places {get;} public TownPoint Center {get;} public bool Follow {get;} public string FavoriteId {get;} public TownRegionId? ViewedRegion {get;}
        public TownWorldSnapshot(string[] ids,TownPoint[] positions,string[] places,TownPoint center,bool follow,string favorite,TownRegionId? viewedRegion=null){ViewedRegion=viewedRegion;FavoriteId=favorite;Ids=ids;Positions=positions;Places=places;Center=center;Follow=follow;}
    }
}
