using System.Linq;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // An in-world walking affordance, not another screen or a saved position.
    public sealed class TownStreetWalkView : MonoBehaviour
    {
        public bool IsArmed {get;private set;}
        private TownLifeView life;private ScreenContext context;private RectTransform panel,markerRoot;
        private Text hint;private GameObject selection;private TownRouteGraphic route;private TownRegionId shown=(TownRegionId)(-1);
        public void Bind(ScreenContext c,TownLifeView town)
        {
            context=c;life=town;markerRoot=UIWidgets.Node("Street exits",(RectTransform)transform,new Rect(0,0,1,1));
            markerRoot.SetAsFirstSibling();route=UIWidgets.Node("Walking footsteps",(RectTransform)transform,new Rect(0,0,1,1)).gameObject.AddComponent<TownRouteGraphic>();route.Bind(life);
            panel=c.Widgets.OrnatePanel("Street walking hint",(RectTransform)transform.parent,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;
            UIWidgets.Top(panel,16,142,16,48);
            hint=c.Widgets.Label("Street walking instruction","キャラをタップして、道を歩こう",panel,new Rect(.025f,.08f,.76f,.84f),12);
            var menu=c.Widgets.Button("Street destinations","行き先",panel,new Rect(.78f,0,.22f,1),()=>life.OpenOutings(),true);menu.GetComponentInChildren<Text>().fontSize=12;
            ((RectTransform)menu.transform).sizeDelta=new Vector2(0,0);panel.gameObject.SetActive(false);RefreshMarkers();
        }
        public bool Arm()
        {
            if(life==null||!life.CanInteract||life.IsManuallyDragging)return false;
            life.StartInteraction();life.Regions.ReturnToHero();IsArmed=true;panel.gameObject.SetActive(true);
            var hero=life.Actors.Single(a=>a.Controlled);
            var ring=context.Widgets.Panel("Selected walking partner",(RectTransform)hero.transform,new Rect(-.12f,-.07f,1.24f,.19f),new Color(1,.84f,.39f,.72f));ring.radius=80;ring.decorated=false;ring.transform.SetAsFirstSibling();selection=ring.gameObject;
            hint.text="道・花・ベンチ・出口をタップ♪";return true;
        }
        public void CancelSelection()
        {IsArmed=false;if(panel!=null)panel.gameObject.SetActive(false);if(selection!=null){selection.SetActive(false);Destroy(selection);}selection=null;if(hint!=null)hint.text="キャラをタップして、道を歩こう";}
        public bool Tap(TownPoint point)
        {
            if(life.World?.IsReady==true){if(!IsArmed||!life.CanInteract)return false;bool walkingStarted=life.World.GuidePoint(point);if(walkingStarted)CancelSelection();else hint.text="石畳の道を選んでね♪";return walkingStarted;}
            if(!IsArmed||!life.CanInteract||life.Regions.ViewingRegion!=life.Regions.HeroRegion)return false;
            if(!TownStreetSelection.TryResolve(life.Regions.HeroRegion,life.TownLevel,point,out var target))
            {hint.text="石畳の道や、行き先を選んでね";return false;}
            bool started=target.Kind==StreetTargetKind.Exit?life.GuideToPlace(TownOutingDestinations.ForRegion(target.Region,life.TownLevel)):
                target.Kind==StreetTargetKind.Place?life.GuideToPlace(target.PlaceId):life.GuideToStreetPoint(target.Point);
            if(started){CancelSelection();hint.text="一緒におさんぽ中♪";}return started;
        }
        private void RefreshMarkers()
        {
            if(life.World?.IsReady==true){shown=life.Regions.ViewingRegion;return;}
            foreach(Transform child in markerRoot){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            shown=life.Regions.ViewingRegion;
            foreach(var next in new[]{TownRegionId.West,TownRegionId.Central,TownRegionId.East})
            {
                if(!TownRegionCatalog.TryGetPortal(shown,next,out var gate))continue;
                var captured=gate.From.Local;var origin=shown;
                string copy=next==TownRegionId.West?"西のカフェへ":next==TownRegionId.East?"東の小道へ":"中央の広場へ";
                var b=context.Widgets.Button("Street exit "+next,copy,markerRoot,new Rect(.5f,.5f,0,0),()=>{if(life.Regions.HeroRegion!=origin){life.Regions.ReturnToHero();return;}if(!IsArmed)Arm();Tap(captured);},true);
                var r=(RectTransform)b.transform;r.sizeDelta=new Vector2(106,48);b.GetComponentInChildren<Text>().fontSize=12;
                b.gameObject.AddComponent<StreetMarkerAnchor>().Bind(life,gate.From.Local);
            }
        }
        private void LateUpdate()
        {
            if(life==null)return;if(shown!=life.Regions.ViewingRegion)RefreshMarkers();
            if(!life.CanSimulate)CancelSelection();
            if(panel!=null)panel.gameObject.SetActive(life.CanSimulate&&IsArmed);
        }
        private void OnDisable()=>CancelSelection();
    }
    public sealed class StreetMarkerAnchor : MonoBehaviour
    {
        private TownLifeView life;private TownPoint point;
        public void Bind(TownLifeView view,TownPoint p){life=view;point=p;LateUpdate();}
        private void LateUpdate()
        {if(life==null)return;var r=(RectTransform)transform;r.anchoredPosition=life.Local(point)+Vector2.up*24;}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownRouteGraphic : MaskableGraphic
    {
        private TownLifeView life;private TownStreetMap map;private TownNavigation navigation;private float lastRefresh;
        public void Bind(TownLifeView view){life=view;raycastTarget=false;}
        private void LateUpdate(){if(life!=null&&Time.unscaledTime-lastRefresh>=1f/30){lastRefresh=Time.unscaledTime;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(life==null||!life.CanSimulate)return;
            if(life.World?.IsReady==true){if(!life.World.Model.Hero.IsWalking)return;for(int k=1;k<=14;k++){var point=life.World.Local(life.World.Model.SampleHeroRoute(k/14f));int start=vh.currentVertCount;var color=new Color(1,.86f,.36f,.5f);vh.AddVert(point+Vector2.left*2,color,Vector2.zero);vh.AddVert(point+Vector2.up*3,color,Vector2.zero);vh.AddVert(point+Vector2.right*2,color,Vector2.zero);vh.AddVert(point+Vector2.down*3,color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);}return;}
            var hero=life.Actors.Single(a=>a.Controlled);
            if(!hero.RegionVisible||!hero.IsGuided)return;
            var target=life.Regions.HasHeroJourney?life.Regions.HeroLegTarget:hero.GuidedTarget;
            var current=TownRegionCatalog.ForLevel(hero.RegionId,life.TownLevel).Map;if(map!=current){map=current;navigation=new TownNavigation(map.Nodes,map.Edges,.028f);}
            for(int i=1;i<=14;i++)
            {
                var p=life.Local(navigation.InterpolateAlongPath(hero.Position,target,i/14f));p.x+=(i%2==0?2:-2);
                float alpha=(.35f+.25f*Mathf.Sin(Time.unscaledTime*3-i*.3f))*(1-i/18f);var tint=new Color(1,.89f,.49f,alpha);
                int n=vh.currentVertCount;float x=2.2f,y=3.2f;
                vh.AddVert(p+new Vector2(-x,0),tint,Vector2.zero);vh.AddVert(p+new Vector2(0,y),tint,Vector2.zero);vh.AddVert(p+new Vector2(x,0),tint,Vector2.zero);vh.AddVert(p+new Vector2(0,-y),tint,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
