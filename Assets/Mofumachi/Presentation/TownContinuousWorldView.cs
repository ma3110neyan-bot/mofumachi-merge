using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // One camera transform for background tiles, all six residents and the connecting paths.
    public sealed class TownContinuousWorldView : MonoBehaviour
    {
        public TownWorldWalkModel Model {get;private set;}
        public TownWorldCamera Camera {get;}=new TownWorldCamera();
        public bool IsReady=>Model!=null;
        public bool FollowingHero {get;private set;}=true;
        public int VisibleBackgroundCount=>tiles.Count(t=>t.gameObject.activeSelf);
        private TownLifeView life;private RawImage original;private RectTransform scenery;
        private readonly List<RawImage> tiles=new List<RawImage>();
        private RectTransform dailyFlower;private int displayedFlowers;
        private readonly List<Text> roadLabels=new List<Text>();
        private static readonly TownPoint[] SignPoints={TownWorldMap.Point(.44f,.33f),TownWorldMap.Point(.52f,.65f)};
        private ScreenContext context;
        private TownGrowthView growth;private TownAmbientGraphic ambient;
        private TownPoint surfaceCenter;private Vector2 surfaceSize;private bool surfaceReady;
        private TownRegionId? lookRegion;
        private bool trackingFinger;private Vector2 fingerScreen;private UnityEngine.Camera fingerCamera;
        internal float RenderScale=>TownWorldLayout.ScaleForViewport(original.rectTransform.rect.width,original.rectTransform.rect.height);
        public event Action<string,string,bool> Arrived;
        public void Bind(ScreenContext c,TownLifeView town)
        {
            context=c;life=town;original=town.Background;Model=new TownWorldWalkModel(town.TownLevel,c.Game.State.favoriteCharacterId);
            scenery=UIWidgets.Node("Continuous town scenery",(RectTransform)original.transform.parent,new Rect(0,0,1,1));scenery.SetSiblingIndex(original.transform.GetSiblingIndex()+1);

            original.enabled=false;var cover=original.GetComponent<BackgroundCover>();if(cover!=null)cover.enabled=false;
            var mapRect=UIWidgets.Node("Seamless town map",scenery,new Rect(.5f,.5f,0,0));
            var map=mapRect.gameObject.AddComponent<RawImage>();map.texture=Resources.Load<Texture2D>(TownWorldMap.Resource);map.raycastTarget=false;tiles.Add(map);
            if(map.texture==null)throw new InvalidOperationException("The authored seamless town map is missing.");
            foreach(var actor in life.Actors)actor.BindWorld(Model.Residents.Single(r=>r.Id==actor.CharacterId));
            foreach(var text in new[]{"北の依頼小屋へ ↑","南のショップへ ↓"})
            {var label=c.Widgets.NarrativeLabel("Painted road direction "+roadLabels.Count,text,(RectTransform)life.transform,new Rect(.5f,.5f,0,0),13);label.rectTransform.sizeDelta=new Vector2(142,30);label.color=new Color(.34f,.22f,.24f);roadLabels.Add(label);}
            life.gameObject.AddComponent<TownFacilityView>().Bind(c,life);
            Model.Arrived+=OnArrival;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f));Refresh();
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
        {if(!IsReady||!life.CanSimulate)return;if(FollowingHero)Camera.Follow(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f),seconds);Refresh();}
        private Vector2 Project(TownPoint p)
        {var rect=original.rectTransform.rect;float scale=RenderScale;return rect.center+new Vector2((p.X-Camera.Center.X)*scale,(p.Y-Camera.Center.Y)*scale*TownWorldLayout.ImageAspect);}
        public Vector2 Local(TownPoint world,RectTransform target=null)
        {return (target??(RectTransform)life.transform).InverseTransformPoint(original.rectTransform.TransformPoint(Project(world)));}
        public TownPoint Source(Vector2 screen,UnityEngine.Camera camera)
        {if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(original.rectTransform,screen,camera,out var p))return Model.Hero.Position;var r=original.rectTransform.rect;float scale=RenderScale;return new TownPoint(Camera.Center.X+(p.x-r.center.x)/scale,Camera.Center.Y+(p.y-r.center.y)/(scale*TownWorldLayout.ImageAspect));}
        public bool GuidePoint(TownPoint point)
        {
            if(!life.CanInteract||!TownWorldLayout.Finite(point))return false;
            if(!Model.TryGuidePoint(point))return false;life.StartInteraction(true);FollowingHero=true;SyncActors();return true;
        }
        public bool GuidePlace(string id)
        {if(!life.CanInteract||life.IsManuallyDragging||!Model.TryGuide(Model.Hero.Id,id))return false;life.StartInteraction(true);FollowingHero=true;SyncActors();return true;}
        public void FastTravel(TownRegionId region)
        {if(!life.CanInteract||life.IsManuallyDragging)return;life.StartInteraction();Model.FastTravel(region);FollowingHero=true;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f));SyncActors();Refresh();}
        public bool LookAt(TownRegionId region)
        {if(!life.CanInteract||!Enum.IsDefined(typeof(TownRegionId),region))return false;FollowingHero=false;lookRegion=region;Camera.Jump(TownWorldMap.Place(region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square"));Refresh();return true;}
        public void ReturnToHero(){FollowingHero=true;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f));Refresh();}
        public void Pan(Vector2 delta){life.GetComponent<TownFacilityView>()?.Cancel();FollowingHero=false;lookRegion=null;float scale=RenderScale;Camera.Pan(-delta.x/scale,-delta.y/(scale*TownWorldLayout.ImageAspect));Refresh();}
        public void CancelHero(){trackingFinger=false;Model?.CancelHero();if(Model!=null)SyncActors();}
        public void Freeze(){trackingFinger=false;Model?.CancelHero();foreach(var actor in life.Actors)actor.ApplyWorldStep(0);}
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
            var bounds=original.rectTransform.rect;float scale=RenderScale;
            Camera.Fit(bounds.width/scale,bounds.height/(scale*TownWorldLayout.ImageAspect));
            var tile=tiles[0];var mapRect=tile.rectTransform;mapRect.pivot=new Vector2(0,0);
            mapRect.anchoredPosition=Project(new TownPoint(TownWorldMap.Left,TownWorldMap.Bottom));
            mapRect.sizeDelta=new Vector2(TownWorldMap.Width*scale,TownWorldMap.Height*TownWorldLayout.ImageAspect*scale);
            foreach(var actor in life.Actors)
            {var p=Local(actor.WorldPosition);var r=((RectTransform)life.transform).rect;actor.SetRegionVisible(p.x>r.xMin-64&&p.x<r.xMax+64&&p.y>r.yMin-90&&p.y<r.yMax+90);}
            var viewing=FollowingHero?Model.Hero.Region:lookRegion??TownWorldLayout.RegionAt(Camera.Center);life.Regions.NotifyWorldRegion(viewing);
            if(!surfaceReady||!surfaceCenter.Equals(Camera.Center)||surfaceSize!=bounds.size)
            {surfaceCenter=Camera.Center;surfaceSize=bounds.size;surfaceReady=true;}
            var actorBounds=((RectTransform)life.transform).rect;
            for(int sign=0;sign<roadLabels.Count;sign++){var at=Local(SignPoints[sign]);roadLabels[sign].rectTransform.anchoredPosition=new Vector2(TownWorldLayout.LabelCenter(at.x,actorBounds.xMin,actorBounds.xMax,roadLabels[sign].rectTransform.rect.width),at.y+24);roadLabels[sign].gameObject.SetActive(at.y>actorBounds.yMin+18&&at.y<actorBounds.yMax-44);}
            int count=Math.Min(6,context.Flow.Daily.Data.DecorationDays.Count);
            if(count>0)
            {
                if(dailyFlower==null){dailyFlower=UIWidgets.Node("Daily collected flower garden",(RectTransform)life.transform,new Rect(.5f,.5f,0,0));dailyFlower.sizeDelta=new Vector2(100,48);dailyFlower.SetAsFirstSibling();}
                while(displayedFlowers<count){int slot=displayedFlowers++;var icon=context.Widgets.Icon("Collected flower art "+slot,UIIcon.Flower,dailyFlower,new Rect(.03f+slot%3*.31f,slot/3*.4f,.29f,.58f));icon.color=Color.Lerp(new Color(1,.70f,.80f),new Color(1,.92f,.55f),slot/5f);}
                dailyFlower.anchoredPosition=Local(TownWorldMap.Point(.64f,.555f));
            }
            life.GetComponent<TownFacilityView>()?.Refresh();
            life.RefreshViewport();if(growth==null)growth=life.GetComponentInChildren<TownGrowthView>();if(growth!=null){if(growth.Region!=viewing)growth.SetRegion(viewing);growth.NotifyWorldCamera();}
            if(ambient==null)ambient=life.transform.parent.GetComponentInChildren<TownAmbientGraphic>();if(ambient!=null){ambient.World=this;ambient.NotifyViewportChanged();}
        }
        public TownWorldSnapshot Capture()=>Model==null?null:new TownWorldSnapshot(Model.Residents.Select(r=>r.Id).ToArray(),Model.Residents.Select(r=>r.Position).ToArray(),Model.Residents.Select(r=>r.PlaceId).ToArray(),Camera.Center,FollowingHero,Model.Hero.Id,lookRegion);
        public void Restore(TownWorldSnapshot snapshot)
        {if(snapshot==null||snapshot.FavoriteId!=Model.Hero.Id)return;for(int i=0;i<snapshot.Ids.Length;i++)Model.Restore(snapshot.Ids[i],snapshot.Positions[i],snapshot.Places[i]);FollowingHero=snapshot.Follow;lookRegion=snapshot.ViewedRegion;Camera.Jump(snapshot.Center);SyncActors();Refresh();}
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
