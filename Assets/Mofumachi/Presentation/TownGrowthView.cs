using System.Collections;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownGrowthView:MonoBehaviour
    {
        public int VisibleStage { get; private set; }
        public TownRegionId Region {get;private set;}=TownRegionId.Central;
        public void SetRegion(TownRegionId region){Region=region;Apply(VisibleStage);}
        public bool IsHighlighting=>highlight!=null;
        private TownBloomGraphic bloom;private GameObject[] layers;private TownLifeView life;private RawImage background;
        private readonly RectTransform[] icons=new RectTransform[12];private readonly TownPoint[] positions=new TownPoint[12];
        private sealed class RegionalProp {internal RectTransform Rect;internal TownGrowthPlacement Placement;internal TownRegionId Region;}
        private readonly System.Collections.Generic.List<RegionalProp> regional=new System.Collections.Generic.List<RegionalProp>();
        private bool dirty;private Coroutine highlight;private Rect previousUv;
        public void Bind(ScreenContext c,TownLifeView life){this.life=life;this.background=life.Background;Build(c);}
        public void Bind(ScreenContext c,RawImage background){this.background=background;Build(c);}
        private void Build(ScreenContext c)
        {
            layers=new GameObject[6];var root=(RectTransform)transform;
            for(int i=0;i<6;i++)
            {
                var layer=UIWidgets.Node("Town growth layer "+(i+1),root,new Rect(0,0,1,1));layers[i]=layer.gameObject;
                if(i>=3)
                {
                    var kind=(TownDecorationKind)(i-3);var icon=UIWidgets.Node("Chapter town decoration",layer,new Rect(.5f,.5f,0,0));
                    icon.gameObject.AddComponent<TownDecorationGraphic>().Bind(kind);float width=i==3?64:i==4?52:140;
                    if(background!=null)width*=Mathf.Min(1,background.rectTransform.rect.width/360f);
                    icon.sizeDelta=new Vector2(width,width/TownDecorationGraphic.Aspect(kind));icons[i+6]=icon;
                    positions[i+6]=i==3?new TownPoint(.58f,.69f):i==4?new TownPoint(.20f,.665f):new TownPoint(.42f,.77f);
                }
            }
            foreach(TownRegionId region in System.Enum.GetValues(typeof(TownRegionId)))
            foreach(var prop in TownGrowthPresentation.For(region,6))
            {
                var r=UIWidgets.Node("Town grown "+region+" "+prop.Level,layers[prop.Level-1].transform as RectTransform,new Rect(.5f,.5f,0,0));
                r.pivot=new Vector2(.5f,0);r.gameObject.AddComponent<TownGrowthPropGraphic>().Bind(prop.Prop,region);
                regional.Add(new RegionalProp{Rect=r,Placement=prop,Region=region});
            }
            bloom=UIWidgets.Node("Town growing flower light",root,new Rect(0,0,1,1)).gameObject.AddComponent<TownBloomGraphic>();bloom.Bind(background);
            Reposition();
        }
        private Vector2 Local(TownPoint p)
        {
            if(life!=null)return ((RectTransform)transform).InverseTransformPoint(life.transform.TransformPoint(life.Local(p)));
            if(background==null)return Vector2.zero;
            var r=background.rectTransform.rect;var uv=background.uvRect;
            var local=r.min+Vector2.Scale((new Vector2(p.X,p.Y)-uv.min)/uv.size,r.size);
            return transform.InverseTransformPoint(background.rectTransform.TransformPoint(local));
        }
        private void Reposition()
        {
            for(int i=0;i<icons.Length;i++)if(icons[i]!=null)icons[i].anchoredPosition=life?.World?.IsReady==true?life.World.Local(TownWorldLayout.ToWorld(TownRegionId.Central,positions[i]),(RectTransform)transform):Local(positions[i]);
            foreach(var entry in regional)
            {
                var p=entry.Placement;entry.Rect.anchoredPosition=life?.World?.IsReady==true?life.World.Local(TownWorldLayout.ToWorld(entry.Region,p.Position),(RectTransform)transform):Local(p.Position);
                float width=Vector2.Distance(Local(p.Position),Local(new TownPoint(p.Position.X+p.Width,p.Position.Y)));
                entry.Rect.sizeDelta=new Vector2(width,width/TownGrowthPropGraphic.Aspect(p.Prop));
                if(life?.World?.IsReady==true){var bounds=((RectTransform)transform).rect;var at=entry.Rect.anchoredPosition;entry.Rect.gameObject.SetActive(at.y+entry.Rect.rect.height>bounds.yMin&&at.y<bounds.yMax&&at.x+width/2>bounds.xMin&&at.x-width/2<bounds.xMax);}
            }
            dirty=false;if(background!=null)previousUv=background.uvRect;
        }
        public void NotifyWorldCamera(){dirty=true;if(bloom!=null){bloom.World=life?.World;bloom.RegionForWorld=Region;}Reposition();}
        private void OnRectTransformDimensionsChange()=>dirty=true;
        private void LateUpdate(){if(dirty||(background!=null&&previousUv!=background.uvRect))Reposition();}
        public void Apply(int level)
        {
            StopHighlight();VisibleStage=Mathf.Clamp(level,0,6);bloom?.Apply(Region,VisibleStage);
            if(layers!=null)for(int i=0;i<layers.Length;i++)layers[i].SetActive(i<VisibleStage);
            foreach(var icon in icons)if(icon!=null)icon.gameObject.SetActive(life?.World?.IsReady==true||Region==TownRegionId.Central);
            foreach(var entry in regional)entry.Rect.gameObject.SetActive(life?.World?.IsReady==true||entry.Region==Region);
            dirty=true;if(life?.World?.IsReady==true)Reposition();
        }
        public void HighlightNewStage(int previous,int current)
        {StopHighlight();if(isActiveAndEnabled&&layers!=null&&Mathf.Clamp(current,0,6)>Mathf.Clamp(previous,0,6))highlight=StartCoroutine(Highlight(Mathf.Clamp(previous,0,6),Mathf.Clamp(current,0,6)));}
        private IEnumerator Highlight(int from,int to)
        {
            float elapsed=0;while(elapsed<1){elapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);float scale=1+.1f*Mathf.Sin(Mathf.Clamp01(elapsed)*Mathf.PI);
                for(int i=from;i<to;i++)foreach(Transform child in layers[i].transform)child.localScale=Vector3.one*scale;yield return null;}
            ResetScales();highlight=null;
        }
        private void ResetScales(){if(layers!=null)foreach(var layer in layers)foreach(Transform child in layer.transform)child.localScale=Vector3.one;}
        private void StopHighlight(){if(highlight!=null)StopCoroutine(highlight);highlight=null;ResetScales();}
        private void OnDisable()=>StopHighlight();
    }
}
