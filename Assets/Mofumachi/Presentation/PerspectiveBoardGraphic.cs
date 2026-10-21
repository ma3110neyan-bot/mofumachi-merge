using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Retain the existing component name for screen/capture compatibility.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PerspectiveBoardGraphic : MaskableGraphic
    {
        private static Texture2D frame;
        public override Texture mainTexture=>frame!=null?frame:frame=Resources.Load<Texture2D>("Mofumachi/MergeBoardFrame");
        protected override void Awake(){base.Awake();raycastTarget=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            if(mainTexture==null)return;
            // Nine slices preserve the authored roses, ribbon and gold rim
            // instead of stretching the decorations with the phone's aspect.
            // This layer stays behind the original full rectangular targets.
            float edge=Mathf.Min(12,Mathf.Min(r.width,r.height)*.045f);
            float[] x={r.xMin,r.xMin+edge,r.xMax-edge,r.xMax};
            float[] y={r.yMin,r.yMin+edge*1.22f,r.yMax-edge*1.5f,r.yMax};
            float[] u={0,.16f,.84f,1},v={0,.13f,.84f,1};
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                var uv=new Rect(u[col],v[row],u[col+1]-u[col],v[row+1]-v[row]);
                // Stretch only a plain gold/ribbon strip along the top. The
                // authored bow is a separate proportional detail, not a stripe.
                if(row==2&&col==1)uv=new Rect(.25f,.90f,.07f,.06f);
                Quad(vh,new Rect(x[col],y[row],x[col+1]-x[col],y[row+1]-y[row]),uv);
            }
            Quad(vh,new Rect(r.center.x-edge*.87f,r.yMax-edge,edge*1.74f,edge),new Rect(.35f,.88f,.31f,.12f));
        }
        private void Quad(VertexHelper vh,Rect rect,Rect uv)
        {
            int start=vh.currentVertCount;
            vh.AddVert(new Vector2(rect.xMin,rect.yMin),color,uv.min);
            vh.AddVert(new Vector2(rect.xMin,rect.yMax),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector2(rect.xMax,rect.yMax),color,uv.max);
            vh.AddVert(new Vector2(rect.xMax,rect.yMin),color,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
