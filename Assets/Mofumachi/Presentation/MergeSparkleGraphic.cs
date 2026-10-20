using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Standard UGUI mesh: eight stars and one soft glow, without particle objects or textures.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MergeSparkleGraphic : MaskableGraphic
    {
        private bool selection;
        private bool match;
        private bool drop,blocked,selectedCell;
        private float elapsed;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void BindSelection(){selection=true;match=drop=selectedCell=false;SetVerticesDirty();}
        public void BindMatch(){selection=drop=selectedCell=false;match=true;SetVerticesDirty();}
        public void BindSelectedCell(){selection=match=drop=false;selectedCell=true;SetVerticesDirty();}
        public void BindDrop(bool rejected){selection=match=selectedCell=false;drop=true;blocked=rejected;SetVerticesDirty();}
        public void SetBurstFrame(float seconds){selection=match=drop=selectedCell=false;elapsed=seconds;SetVerticesDirty();}
        private void Update(){if(match||drop){elapsed=Time.unscaledTime;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            if(drop||selectedCell)
            {
                var tint=selectedCell?new Color(.94f,.37f,.58f,.95f):blocked?new Color(.88f,.40f,.39f,.90f):new Color(.24f,.71f,.55f,.98f);
                Outline(vh,r,drop?2.5f:1.8f,tint);
                if(!blocked)Star(vh,r.center+new Vector2(r.width*.34f,r.height*.33f),3.1f,new Color(1,1,.88f,.95f));
                return;
            }
            if(match)
            {
                float pulse=.78f+.15f*Mathf.Sin(elapsed*4);
                Outline(vh,r,1.6f,new Color(.36f,.76f,.58f,pulse));
                Glow(vh,r.center,Mathf.Min(r.width,r.height)*.47f,.16f);
                Star(vh,r.center+new Vector2(-r.width*.34f,r.height*.31f),2.8f,new Color(1,.93f,.56f,.95f));
                Star(vh,r.center+new Vector2(r.width*.34f,r.height*.29f),2.2f,new Color(1,1,.88f,.85f));return;
            }
            if(selection)
            {
                Star(vh,r.center+new Vector2(-r.width*.22f,r.height*.23f),2.4f,new Color(1,1,.92f,.6f));
                Star(vh,r.center+new Vector2(r.width*.21f,r.height*.05f),1.5f,new Color(1,1,1,.38f));return;
            }
            float flash=MergeVisualTiming.ImpactAlpha(elapsed);
            if(flash>0)Glow(vh,r.center,Mathf.Min(r.width,r.height)*.36f,flash);
            float alpha=MergeVisualTiming.SparkleAlpha(elapsed);if(alpha<=0)return;
            float t=Mathf.Clamp01((elapsed-MergeVisualTiming.GatherSeconds)/(MergeVisualTiming.LifetimeSeconds-MergeVisualTiming.GatherSeconds));
            Glow(vh,r.center,Mathf.Min(r.width,r.height)*(.18f+.1f*t),alpha*.28f);
            for(int i=0;i<8;i++)
            {
                float angle=(i*45+15)*Mathf.Deg2Rad;
                var p=r.center+new Vector2(Mathf.Cos(angle)*r.width,Mathf.Sin(angle)*r.height)*(.2f+.2f*t);
                Color tint=i%3==0?new Color(1,.78f,.85f,alpha):i%3==1?new Color(.8f,1,1,alpha):new Color(1,.96f,.68f,alpha);
                Star(vh,p,(i%2==0?2.7f:1.9f)*(1-.55f*t),tint);
            }
        }
        private static void Outline(VertexHelper vh,Rect bounds,float thickness,Color tint)
        {
            var r=new Rect(bounds.x+.75f,bounds.y+.75f,bounds.width-1.5f,bounds.height-1.5f);
            float radius=Mathf.Min(r.width,r.height)*.11f;int start=vh.currentVertCount,count=0;
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(int step=0;step<=8;step++)
                {
                    float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    vh.AddVert(center+direction*radius,tint,Vector2.zero);vh.AddVert(center+direction*Mathf.Max(0,radius-thickness),tint,Vector2.zero);count++;
                }
            }
            for(int i=0;i<count;i++){int a=start+i*2,b=start+((i+1)%count)*2;vh.AddTriangle(a,b,a+1);vh.AddTriangle(a+1,b,b+1);}
        }
        private static void Star(VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            int start=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<8;i++)
            {float angle=i*Mathf.PI/4;float size=i%2==0?radius:radius*.28f;vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size,tint,Vector2.zero);}
            for(int i=0;i<8;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
        }
        private static void Glow(VertexHelper vh,Vector2 center,float radius,float alpha)
        {
            int start=vh.currentVertCount;vh.AddVert(center,new Color(1,.94f,.76f,alpha),Vector2.zero);
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Color(1,.96f,.84f,0),Vector2.zero);}
            for(int i=0;i<16;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%16);
        }
    }
}
