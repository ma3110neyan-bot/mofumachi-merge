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
        private RectTransform dailyFlower;
        private ScreenContext context;
        private TownGardenPathGraphic gardenPath;private TownGrowthView growth;private TownAmbientGraphic ambient;
        public event Action<string,string,bool> Arrived;
        public void Bind(ScreenContext c,TownLifeView town)
        {
            context=c;life=town;original=town.Background;Model=new TownWorldWalkModel(town.TownLevel,c.Game.State.favoriteCharacterId);
            scenery=UIWidgets.Node("Continuous town scenery",(RectTransform)original.transform.parent,new Rect(0,0,1,1));scenery.SetSiblingIndex(original.transform.GetSiblingIndex()+1);
            var garden=scenery.gameObject.AddComponent<RawImage>();garden.texture=Resources.Load<Texture2D>("Mofumachi/TownBackground");garden.uvRect=new Rect(.015f,.05f,.13f,.24f);garden.raycastTarget=false;
            original.enabled=false;var cover=original.GetComponent<BackgroundCover>();if(cover!=null)cover.enabled=false;
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
            {
                var r=UIWidgets.Node("Continuous background "+region,scenery,new Rect(.5f,.5f,0,0));
                var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(TownRegionCatalog.ForLevel(region,0).BackgroundResource);image.uvRect=new Rect(0,0,1,TownWorldLayout.ArtHeight);image.raycastTarget=false;tiles.Add(image);
            }
            var path=UIWidgets.Node("Continuous connecting garden paths",scenery,new Rect(0,0,1,1)).gameObject.AddComponent<TownGardenPathGraphic>();path.Bind(this);gardenPath=path;
            foreach(var actor in life.Actors)actor.BindWorld(Model.Residents.Single(r=>r.Id==actor.CharacterId));
            Model.Arrived+=OnArrival;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f));Refresh();
        }
        public void Step(float seconds){if(Model==null||!life.CanSimulate)return;Model.Step(seconds);foreach(var actor in life.Actors)actor.ApplyWorldStep(seconds);}
        public void AdvanceCamera(float seconds)
        {if(!IsReady||!life.CanSimulate)return;if(FollowingHero)Camera.Follow(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f),seconds);Refresh();}
        private Vector2 Project(TownPoint p)
        {var rect=original.rectTransform.rect;float scale=rect.width/1.06f;return rect.center+new Vector2((p.X-Camera.Center.X)*scale,(p.Y-Camera.Center.Y)*scale*TownWorldLayout.ImageAspect);}
        public Vector2 Local(TownPoint world,RectTransform target=null)
        {return (target??(RectTransform)life.transform).InverseTransformPoint(original.rectTransform.TransformPoint(Project(world)));}
        public TownPoint Source(Vector2 screen,UnityEngine.Camera camera)
        {if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(original.rectTransform,screen,camera,out var p))return Model.Hero.Position;var r=original.rectTransform.rect;float scale=r.width/1.06f;return new TownPoint(Camera.Center.X+(p.x-r.center.x)/scale,Camera.Center.Y+(p.y-r.center.y)/(scale*TownWorldLayout.ImageAspect));}
        public bool GuidePoint(TownPoint point)
        {
            if(!life.CanInteract||!TownWorldLayout.Finite(point))return false;
            if(!Model.TryGuidePoint(point))return false;FollowingHero=true;return true;
        }
        public bool GuidePlace(string id)
        {if(!life.CanInteract||life.IsManuallyDragging||!Model.TryGuide(Model.Hero.Id,id))return false;FollowingHero=true;return true;}
        public void FastTravel(TownRegionId region)
        {if(!life.CanInteract||life.IsManuallyDragging)return;life.StartInteraction();Model.FastTravel(region);FollowingHero=true;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f));SyncActors();Refresh();}
        public bool LookAt(TownRegionId region)
        {if(!life.CanInteract||!Enum.IsDefined(typeof(TownRegionId),region))return false;FollowingHero=false;Camera.Jump(TownWorldLayout.ToWorld(region,new TownPoint(.5f,.42f)));Refresh();return true;}
        public void ReturnToHero(){FollowingHero=true;Camera.Jump(new TownPoint(Model.Hero.Position.X,Model.Hero.Position.Y+.04f));Refresh();}
        public void Pan(Vector2 delta){FollowingHero=false;float scale=original.rectTransform.rect.width/1.06f;Camera.Pan(-delta.x/scale,-delta.y/(scale*TownWorldLayout.ImageAspect));Refresh();}
        public void CancelHero(){Model?.CancelHero();if(Model!=null)SyncActors();}
        public void Freeze(){Model?.CancelHero();foreach(var actor in life.Actors)actor.ApplyWorldStep(0);}
        private void OnArrival(string id,string placeId,bool walked)
        {
            var actor=life.Actors.Single(a=>a.CharacterId==id);actor.ApplyWorldStep(0);
            if(TownRegionCatalog.TryFindPlace(placeId,life.TownLevel,out _,out var place))
            {actor.SetWorldPlace(place);if(actor.Controlled){life.SetGuidedDestination(place);life.ActorReleased(actor);}}
            actor.Surprise();if(actor.Controlled&&walked)context.Flow.RecordWorldWalk();Arrived?.Invoke(id,placeId,walked);
        }
        private void SyncActors(){foreach(var actor in life.Actors)actor.ApplyWorldStep(0);}
        public void Refresh()
        {
            if(!IsReady||original==null||scenery==null)return;
            var bounds=original.rectTransform.rect;float scale=bounds.width/1.06f;int i=0;
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
            {
                var tile=tiles[i++];var r=tile.rectTransform;r.pivot=new Vector2(.5f,0);r.anchoredPosition=Project(TownWorldLayout.ToWorld(region,new TownPoint(.5f,0)));
                r.sizeDelta=new Vector2(scale,scale*TownWorldLayout.ImageAspect*TownWorldLayout.ArtHeight);
                tile.gameObject.SetActive(r.anchoredPosition.y<bounds.yMax&&r.anchoredPosition.y+r.rect.height>bounds.yMin);
            }
            foreach(var actor in life.Actors)
            {var p=Local(actor.WorldPosition);var r=((RectTransform)life.transform).rect;actor.SetRegionVisible(p.x>r.xMin-64&&p.x<r.xMax+64&&p.y>r.yMin-90&&p.y<r.yMax+90);}
            var viewing=TownWorldLayout.RegionAt(Camera.Center);life.Regions.NotifyWorldRegion(viewing);
            gardenPath?.SetVerticesDirty();
            if(context.Flow.Daily.Data.DecorationDays.Count>0){if(dailyFlower==null){dailyFlower=UIWidgets.Node("Daily collected flower",(RectTransform)life.transform,new Rect(.5f,.5f,0,0));dailyFlower.sizeDelta=new Vector2(32,40);context.Widgets.Icon("Collected flower art",UIIcon.Flower,dailyFlower,new Rect(0,0,1,1));dailyFlower.SetAsFirstSibling();}dailyFlower.anchoredPosition=Local(TownWorldLayout.ToWorld(TownRegionId.Central,new TownPoint(.62f,.34f)));}
            life.RefreshViewport();if(growth==null)growth=life.GetComponentInChildren<TownGrowthView>();growth?.NotifyWorldCamera();
            if(ambient==null)ambient=life.transform.parent.GetComponentInChildren<TownAmbientGraphic>();if(ambient!=null){ambient.World=this;ambient.NotifyViewportChanged();}
        }
        public TownWorldSnapshot Capture()=>Model==null?null:new TownWorldSnapshot(Model.Residents.Select(r=>r.Id).ToArray(),Model.Residents.Select(r=>r.Position).ToArray(),Model.Residents.Select(r=>r.PlaceId).ToArray(),Camera.Center,FollowingHero,Model.Hero.Id);
        public void Restore(TownWorldSnapshot snapshot)
        {if(snapshot==null||snapshot.FavoriteId!=Model.Hero.Id)return;for(int i=0;i<snapshot.Ids.Length;i++)Model.Restore(snapshot.Ids[i],snapshot.Positions[i],snapshot.Places[i]);FollowingHero=snapshot.Follow;Camera.Jump(snapshot.Center);SyncActors();Refresh();}
        private void OnDisable()
        {
            if(Model!=null)Model.Arrived-=OnArrival;
            if(scenery!=null){scenery.gameObject.SetActive(false);Destroy(scenery.gameObject);}
            if(original!=null){original.enabled=true;var cover=original.GetComponent<BackgroundCover>();if(cover!=null){cover.enabled=true;cover.FitNow();}}
        }
    }
    public sealed class TownWorldSnapshot
    {
        public string[] Ids {get;} public TownPoint[] Positions {get;} public string[] Places {get;} public TownPoint Center {get;} public bool Follow {get;} public string FavoriteId {get;}
        public TownWorldSnapshot(string[] ids,TownPoint[] positions,string[] places,TownPoint center,bool follow,string favorite){FavoriteId=favorite;Ids=ids;Positions=positions;Places=places;Center=center;Follow=follow;}
    }
    // Reuses a stone patch from the formal painting; no destructive image edits or extra texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownGardenPathGraphic : MaskableGraphic
    {
        private TownContinuousWorldView world;private Texture2D texture;
        public override Texture mainTexture=>texture;
        public void Bind(TownContinuousWorldView view){world=view;texture=Resources.Load<Texture2D>("Mofumachi/TownBackground");raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(world==null||!world.IsReady)return;
            foreach(var path in new[]{TownWorldLayout.NorthConnection,TownWorldLayout.SouthConnection})
            for(int i=1;i<path.Length;i++)
            {
                var a=world.Local(path[i-1],rectTransform);var b=world.Local(path[i],rectTransform);var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x)*rectTransform.rect.width*.073f;
                int n=vh.currentVertCount;var color=new Color(1,1,1,.98f);
                vh.AddVert(a-normal,color,new Vector2(.36f,.07f));vh.AddVert(a+normal,color,new Vector2(.62f,.07f));vh.AddVert(b+normal,color,new Vector2(.62f,.24f));vh.AddVert(b-normal,color,new Vector2(.36f,.24f));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
