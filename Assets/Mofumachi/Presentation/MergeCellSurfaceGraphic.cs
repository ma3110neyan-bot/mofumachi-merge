using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Cream/mint paper and faint motifs are decorative; input keeps its full rectangle.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MergeCellSurfaceGraphic : MaskableGraphic
    {
        private Color paper;private int motif=-1;
        public void Bind(Color tint,int emptyCell=-1){paper=tint;motif=emptyCell;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            bool before=MergeRewardPresentation.CaptureBefore;
            float unit=Mathf.Min(r.width,r.height),inset=unit*.018f,radius=unit*(before?.11f:.14f);
            var seat=new Rect(r.x+inset,r.y+inset,r.width-inset*2,r.height-inset*2);
            Fill(vh,seat,radius,new Color(.64f,.36f,.42f,.18f),new Color(.64f,.36f,.42f,.07f));
            var rim=new Rect(seat.x,seat.y+1,seat.width,seat.height-1);
            Fill(vh,rim,radius,before?Color.Lerp(paper,new Color(.69f,.49f,.38f),.35f):new Color(.91f,.72f,.46f),Color.Lerp(paper,Color.white,before?.72f:.86f));
            var face=new Rect(rim.x+1.25f,rim.y+1.25f,rim.width-2.5f,rim.height-2.5f);
            Fill(vh,face,radius-1,paper,Color.Lerp(paper,Color.white,.20f));
            Fill(vh,new Rect(face.x+radius,face.yMax-1,face.width-radius*2,.7f),.35f,new Color(1,1,1,.6f),new Color(1,1,1,.6f));
            if(motif>=0&&!before)Motif(vh,r.center,unit*.18f,(motif/5+motif%5)%4,.16f);
        }
        internal static void Motif(VertexHelper vh,Vector2 c,float size,int kind,float alpha=.27f)
        {
            var green=new Color(.38f,.69f,.55f,alpha);var pink=new Color(.95f,.48f,.59f,alpha);var gold=new Color(.91f,.68f,.30f,alpha);
            if(kind==0)
            {
                Oval(vh,c+Vector2.down*size*.25f,size*.65f,size*.52f,0,green);
                for(int i=0;i<4;i++)Oval(vh,c+new Vector2((i-1.5f)*size*.43f,size*(i==0||i==3?.36f:.66f)),size*.25f,size*.33f,(i-1.5f)*-.13f,green);
            }
            else if(kind==1)
            {
                Oval(vh,c+new Vector2(-size*.32f,-size*.10f),size*.32f,size*.76f,-.75f,green);
                Oval(vh,c+new Vector2(size*.30f,size*.25f),size*.30f,size*.74f,-.75f,green);
                Fill(vh,new Rect(c.x-size*.56f,c.y-size*.66f,size*1.2f,size*.09f),size*.04f,green,green);
            }
            else if(kind==2)
            {
                for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Oval(vh,c+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*size*.43f,size*.32f,size*.48f,-a,pink);}
                Oval(vh,c,size*.23f,size*.23f,0,gold);
            }
            else
            {
                Diamond(vh,c,size*.78f,gold);Diamond(vh,c+new Vector2(size*.7f,size*.7f),size*.32f,gold);
            }
        }
        private static void Diamond(VertexHelper vh,Vector2 c,float size,Color tint)
        {int n=vh.currentVertCount;vh.AddVert(c+Vector2.up*size,tint,Vector2.zero);vh.AddVert(c+Vector2.right*size*.44f,tint,Vector2.zero);vh.AddVert(c-Vector2.up*size,tint,Vector2.zero);vh.AddVert(c-Vector2.right*size*.44f,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        private static void Oval(VertexHelper vh,Vector2 c,float rx,float ry,float angle,Color tint)
        {
            int n=vh.currentVertCount;vh.AddVert(c,tint,Vector2.zero);float co=Mathf.Cos(angle),si=Mathf.Sin(angle);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6,x=Mathf.Cos(a)*rx,y=Mathf.Sin(a)*ry;vh.AddVert(c+new Vector2(co*x-si*y,si*x+co*y),tint,Vector2.zero);}
            for(int i=0;i<12;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%12);
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
