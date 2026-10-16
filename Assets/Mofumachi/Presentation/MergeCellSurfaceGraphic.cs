using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // A shallow porcelain dish, rendered in one UGUI mesh. The rectangular
    // button underneath remains the complete input target, including its corners.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MergeCellSurfaceGraphic : MaskableGraphic
    {
        private Color paper;
        public void Bind(Color tint){paper=tint;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            float unit=Mathf.Min(r.width,r.height),radius=unit*.46f;
            var baseRect=new Rect(r.x+r.width*.025f,r.y+r.height*.10f,r.width*.95f,r.height*.72f);
            Fill(vh,baseRect,radius,new Color(.32f,.24f,.28f,.19f),new Color(.32f,.24f,.28f,.04f));
            var body=new Rect(baseRect.x,baseRect.y+unit*.045f,baseRect.width,baseRect.height-unit*.07f);
            Fill(vh,body,radius,new Color(.79f,.65f,.48f),new Color(.98f,.9f,.72f));
            var lip=new Rect(body.x,body.y+unit*.065f,body.width,body.height-unit*.065f);
            Fill(vh,lip,radius,Color.Lerp(paper,new Color(.9f,.79f,.63f),.36f),Color.Lerp(paper,Color.white,.8f));
            var well=new Rect(lip.x+unit*.08f,lip.y+unit*.07f,lip.width-unit*.16f,lip.height-unit*.16f);
            Fill(vh,well,radius*.76f,Color.Lerp(paper,Color.white,.4f),Color.Lerp(paper,new Color(.75f,.72f,.65f),.23f));
            var inner=new Rect(well.x+unit*.025f,well.y,well.width-unit*.05f,well.height-unit*.055f);
            Fill(vh,inner,radius*.67f,Color.Lerp(paper,new Color(.95f,.86f,.7f),.12f),Color.Lerp(paper,Color.white,.38f));
            // Fine reflected light along the upper lip; never outside the cell.
            Fill(vh,new Rect(lip.x+unit*.23f,lip.yMax-unit*.038f,lip.width-unit*.46f,unit*.018f),unit*.009f,new Color(1,1,1,.6f),new Color(1,1,1,.6f));
        }
        private static void Fill(VertexHelper vh,Rect r,float radius,Color bottom,Color top)
        {
            if(r.width<=0||r.height<=0)return;float corner=Mathf.Min(radius,Mathf.Min(r.width,r.height)/2);
            int start=vh.currentVertCount;vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            const int steps=6;int count=0;
            for(int c=0;c<4;c++)
            {
                var center=new Vector2(c==0||c==3?r.xMax-corner:r.xMin+corner,c<2?r.yMax-corner:r.yMin+corner);
                for(int k=0;k<=steps;k++)
                {float a=(c*90+k*90f/steps)*Mathf.Deg2Rad;var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*corner;vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);count++;}
            }
            for(int i=1;i<=count;i++)vh.AddTriangle(start,start+i,start+(i==count?1:i+1));
        }
    }
}
