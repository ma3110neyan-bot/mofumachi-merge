using System.Collections;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownGrowthView:MonoBehaviour
    {
        public int VisibleStage { get; private set; }
        public bool IsHighlighting=>highlight!=null;
        private GameObject[] layers;private TownLifeView life;private RawImage background;
        private readonly RectTransform[] icons=new RectTransform[12];private readonly TownPoint[] positions=new TownPoint[12];
        private bool dirty;private Coroutine highlight;private Rect previousUv;
        public void Bind(ScreenContext c,TownLifeView life){this.life=life;this.background=life.Background;Build(c);}
        public void Bind(ScreenContext c,RawImage background){this.background=background;Build(c);}
        private void Build(ScreenContext c)
        {
            layers=new GameObject[6];var root=(RectTransform)transform;
            for(int i=0;i<6;i++)
            {
                var layer=UIWidgets.Node("Town growth layer "+(i+1),root,new Rect(0,0,1,1));layers[i]=layer.gameObject;
                if(i<3)for(int j=0;j<3;j++)
                {
                    var icon=c.Widgets.Icon("Saved town decoration",i==2?UIIcon.Star:UIIcon.Flower,layer,new Rect(.5f,.5f,0,0));
                    icon.rectTransform.sizeDelta=new Vector2(i==2?12:22,i==2?12:22);icon.raycastTarget=false;
                    icons[i*3+j]=icon.rectTransform;positions[i*3+j]=new TownPoint(i==2?.37f+j*.025f:.1f+j*.33f,i==2?.595f:.505f+i*.135f);
                }
                else
                {
                    var kind=(TownDecorationKind)(i-3);var icon=UIWidgets.Node("Chapter town decoration",layer,new Rect(.5f,.5f,0,0));
                    icon.gameObject.AddComponent<TownDecorationGraphic>().Bind(kind);float width=i==3?64:i==4?52:140;
                    if(background!=null)width*=Mathf.Min(1,background.rectTransform.rect.width/360f);
                    icon.sizeDelta=new Vector2(width,width/TownDecorationGraphic.Aspect(kind));icons[i+6]=icon;
                    positions[i+6]=i==3?new TownPoint(.58f,.69f):i==4?new TownPoint(.20f,.665f):new TownPoint(.42f,.77f);
                }
            }
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
        private void Reposition(){for(int i=0;i<icons.Length;i++)if(icons[i]!=null)icons[i].anchoredPosition=Local(positions[i]);dirty=false;if(background!=null)previousUv=background.uvRect;}
        private void OnRectTransformDimensionsChange()=>dirty=true;
        private void LateUpdate(){if(dirty||(background!=null&&previousUv!=background.uvRect))Reposition();}
        public void Apply(int level){StopHighlight();VisibleStage=Mathf.Clamp(level,0,6);if(layers!=null)for(int i=0;i<layers.Length;i++)layers[i].SetActive(i<VisibleStage);}
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
