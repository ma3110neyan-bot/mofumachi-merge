using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PerspectiveBoardGraphic : MaskableGraphic
    {
        private Texture2D texture;
        public override Texture mainTexture=>texture!=null?texture:texture=Resources.Load<Texture2D>("Mofumachi/MergeBoardFrame");
        protected override void Awake(){base.Awake();raycastTarget=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            // A thick front edge and shadow establish the board's angle before the dishes are drawn.
            Quad(vh,new Vector2(r.xMin+3,r.yMin-7),new Vector2(r.xMin+r.width*.1f+3,r.yMax-7),new Vector2(r.xMax-r.width*.1f+3,r.yMax-7),new Vector2(r.xMax+3,r.yMin-7),new Color(.36f,.22f,.16f,.34f),Vector2.zero,Vector2.zero);
            Quad(vh,new Vector2(r.xMin,r.yMin-5),new Vector2(r.xMin,r.yMin+12),new Vector2(r.xMax,r.yMin+12),new Vector2(r.xMax,r.yMin-5),new Color(.78f,.54f,.28f),new Vector2(.04f,.02f),new Vector2(.96f,.05f));
            const int strips=8;
            for(int i=0;i<strips;i++)
            {
                float v=i/(float)strips,u=(i+1f)/strips;float low=r.width*.10f*v,high=r.width*.10f*u;
                Quad(vh,new Vector2(r.xMin+low,r.yMin+r.height*v),new Vector2(r.xMin+high,r.yMin+r.height*u),new Vector2(r.xMax-high,r.yMin+r.height*u),new Vector2(r.xMax-low,r.yMin+r.height*v),Color.white,new Vector2(0,v),new Vector2(1,u));
            }
        }
        private static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint,Vector2 low,Vector2 high)
        {int n=vh.currentVertCount;vh.AddVert(a,tint,low);vh.AddVert(b,tint,new Vector2(low.x,high.y));vh.AddVert(c,tint,high);vh.AddVert(d,tint,new Vector2(high.x,low.y));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
