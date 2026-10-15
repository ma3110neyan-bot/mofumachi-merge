using UnityEngine;
using UnityEngine.UI;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // One bounded, noninteractive mesh. Only the viewed region is drawn.
    public sealed class TownBloomGraphic : MaskableGraphic
    {
        private RawImage background;private int stage;private float phase,refresh;
        private TownGrowthPlacement[] lamps=System.Array.Empty<TownGrowthPlacement>();
        public int PetalCount=>stage<2?0:Mathf.Min(12,stage*2);
        public void Bind(RawImage image){background=image;raycastTarget=false;}
        public void Apply(TownRegionId id,int level){stage=Mathf.Clamp(level,0,6);lamps=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(TownGrowthPresentation.For(id,stage),p=>p.Prop==TownGrowthProp.Lamp));SetVerticesDirty();}
        private void Update()
        {
            if(stage<2||background==null)return;phase+=Mathf.Min(Time.unscaledDeltaTime,.1f);refresh+=Time.unscaledDeltaTime;
            if(refresh>=1f/30){refresh=0;SetVerticesDirty();}
        }
        private Vector2 Local(Vector2 point)
        {
            var uv=background.uvRect;var r=background.rectTransform.rect;
            return transform.InverseTransformPoint(background.rectTransform.TransformPoint(r.min+Vector2.Scale((point-uv.min)/uv.size,r.size)));
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(background==null)return;
            for(int i=0;i<PetalCount;i++)
            {
                float t=Mathf.Repeat(phase*.055f+i*.173f,1),x=.16f+(i*.139f% .68f)+.025f*Mathf.Sin(phase+i);
                var p=Local(new Vector2(x,.72f-t*.39f));float size=1.5f+(i%3)*.35f;
                var tint=i%2==0?new Color(1,.70f,.80f,Mathf.Sin(t*Mathf.PI)*.62f):new Color(1,.95f,.69f,Mathf.Sin(t*Mathf.PI)*.60f);
                AddDiamond(vh,p,size,tint);
            }
            if(stage>=3)
            foreach(var prop in lamps)
                {
                    var p=Local(new Vector2(prop.Position.X,prop.Position.Y+.045f));
                    AddDiamond(vh,p,2+Mathf.Sin(phase*1.8f)*.6f,new Color(1,.95f,.64f,.45f));
                }
        }
        private static void AddDiamond(VertexHelper vh,Vector2 p,float size,Color tint)
        {
            int n=vh.currentVertCount;vh.AddVert(p+Vector2.left*size,tint,Vector2.zero);vh.AddVert(p+Vector2.up*size*1.6f,tint,Vector2.zero);
            vh.AddVert(p+Vector2.right*size,tint,Vector2.zero);vh.AddVert(p+Vector2.down*size*1.6f,tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
