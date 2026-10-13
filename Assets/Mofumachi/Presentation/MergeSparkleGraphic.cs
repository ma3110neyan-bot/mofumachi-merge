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
        private float elapsed;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void BindSelection(){selection=true;match=false;SetVerticesDirty();}
        public void BindMatch(){selection=false;match=true;SetVerticesDirty();}
        public void SetBurstFrame(float seconds){selection=false;match=false;elapsed=seconds;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0||r.height<=0)return;
            if(match)
            {
                Glow(vh,r.center,Mathf.Min(r.width,r.height)*.47f,.24f);
                Star(vh,r.center+new Vector2(-r.width*.31f,r.height*.22f),2.2f,new Color(1,.93f,.56f,.85f));
                Star(vh,r.center+new Vector2(r.width*.3f,r.height*.19f),1.6f,new Color(1,1,.88f,.7f));return;
            }
            if(selection)
            {
                Star(vh,r.center+new Vector2(-r.width*.22f,r.height*.23f),2.4f,new Color(1,1,.92f,.6f));
                Star(vh,r.center+new Vector2(r.width*.21f,r.height*.05f),1.5f,new Color(1,1,1,.38f));return;
            }
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
