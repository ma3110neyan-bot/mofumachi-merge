using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // The rounded square is decorative; the complete cell remains the input target.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MergeCellSurfaceGraphic : MaskableGraphic
    {
        private Color paper;
        public void Bind(Color tint){paper=tint;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            float unit=Mathf.Min(r.width,r.height),inset=unit*.018f,radius=unit*.11f;
            var seat=new Rect(r.x+inset,r.y+inset,r.width-inset*2,r.height-inset*2);
            Fill(vh,seat,radius,new Color(.64f,.36f,.42f,.18f),new Color(.64f,.36f,.42f,.07f));
            var rim=new Rect(seat.x,seat.y+1,seat.width,seat.height-1);
            Fill(vh,rim,radius,Color.Lerp(paper,new Color(.69f,.49f,.38f),.35f),Color.Lerp(paper,Color.white,.72f));
            var face=new Rect(rim.x+1.25f,rim.y+1.25f,rim.width-2.5f,rim.height-2.5f);
            Fill(vh,face,radius-1,paper,Color.Lerp(paper,Color.white,.20f));
            Fill(vh,new Rect(face.x+radius,face.yMax-1,face.width-radius*2,.7f),.35f,new Color(1,1,1,.6f),new Color(1,1,1,.6f));
        }
        internal static void Fill(VertexHelper vh,Rect r,float radius,Color bottom,Color top)
        {
            if(r.width<=0||r.height<=0)return;
            radius=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)/2);
            int start=vh.currentVertCount;vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            const int steps=8;int count=0;
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(int step=0;step<=steps;step++)
                {
                    float angle=(corner*90+step*90f/steps)*Mathf.Deg2Rad;
                    var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);count++;
                }
            }
            for(int i=1;i<=count;i++)vh.AddTriangle(start,start+i,start+(i==count?1:i+1));
        }
    }
}
