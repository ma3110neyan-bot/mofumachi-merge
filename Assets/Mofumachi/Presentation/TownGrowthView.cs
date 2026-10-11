using UnityEngine;
namespace Mofumachi.Presentation
{
    public sealed class TownGrowthView : MonoBehaviour
    {
        public int VisibleStage { get; private set; }
        private GameObject[] layers;private TownLifeView life;private readonly RectTransform[] icons=new RectTransform[9];private readonly Mofumachi.Core.TownPoint[] positions=new Mofumachi.Core.TownPoint[9];private bool dirty;
        public void Bind(ScreenContext c,TownLifeView life)
        {
            this.life=life;layers=new GameObject[3];var root=(RectTransform)transform;
            for(int i=0;i<3;i++)
            {
                var layer=UIWidgets.Node("Town growth layer "+(i+1),root,new Rect(0,0,1,1));layers[i]=layer.gameObject;
                for(int j=0;j<3;j++)
                {
                    var icon=c.Widgets.Icon("Saved town decoration",i==2?UIIcon.Star:UIIcon.Flower,layer,new Rect(.5f,.5f,0,0));
                    icon.rectTransform.sizeDelta=new Vector2(i==2?12:22,i==2?12:22);
                    var p=new Mofumachi.Core.TownPoint(i==2?.37f+j*.025f:.1f+j*.33f,i==2?.595f:.505f+i*.135f);
                    icons[i*3+j]=icon.rectTransform;positions[i*3+j]=p;icon.rectTransform.anchoredPosition=life.Local(p);icon.raycastTarget=false;
                }
            }
        }
        private void OnRectTransformDimensionsChange()=>dirty=true;
        private void LateUpdate(){if(!dirty||life==null)return;dirty=false;for(int i=0;i<icons.Length;i++)if(icons[i]!=null)icons[i].anchoredPosition=life.Local(positions[i]);}
        public void Apply(int townLevel){VisibleStage=Mathf.Clamp(townLevel,0,3);if(layers!=null)for(int i=0;i<layers.Length;i++)layers[i].SetActive(i<VisibleStage);}
    }
}
