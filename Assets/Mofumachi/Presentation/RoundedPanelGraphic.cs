using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Finely edged surfaces, standard UGUI shader, no extra texture allocation.
    public sealed class RoundedPanelGraphic : Image
    {
        public float radius = 20;
        public float borderWidth = 1.25f;
        public float gloss = .22f;
        public bool decorated = true;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            if(r.width<=0 || r.height<=0)return;
            if(!decorated){Fill(vh,r,radius,color,color);return;}
            var shadow=r;shadow.y-=2;
            var shade=new Color(.37f,.22f,.29f,.12f*color.a);Fill(vh,shadow,radius,shade,shade);
            Fill(vh,r,radius,new Color(.94f,.78f,.51f,color.a),new Color(1,.96f,.82f,color.a));
            float inset=Mathf.Min(borderWidth,Mathf.Min(r.width,r.height)/2);
            r=new Rect(r.x+inset,r.y+inset,r.width-2*inset,r.height-2*inset);
            var light=Color.Lerp(color,Color.white,gloss);light.a=color.a;
            Fill(vh,r,radius-inset,color,light);
            var rim=new Rect(r.x+radius*.45f,r.yMax-3,Mathf.Max(0,r.width-radius*.9f),1);
            Fill(vh,rim,.5f,new Color(1,1,1,.46f*color.a),new Color(1,1,1,.46f*color.a));
        }
        private static void Fill(VertexHelper vh,Rect r,float requestedRadius,Color bottom,Color top)
        {
            if(r.width<=0 || r.height<=0)return;
            float corner=Mathf.Clamp(requestedRadius,0,Mathf.Min(r.width,r.height)/2);
            int start=vh.currentVertCount;vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            const int steps = 8; int n = 0;
            for (int c = 0; c < 4; c++)
            {
                var center = new Vector2(c == 0 || c == 3 ? r.xMax-corner : r.xMin+corner, c < 2 ? r.yMax-corner : r.yMin+corner);
                for (int k = 0; k <= steps; k++)
                {
                    float angle = (c*90 + k*90f/steps) * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))*corner;
                    Color tint = Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,point.y));
                    vh.AddVert(point, tint, Vector2.zero); n++;
                }
            }
            for (int i=1;i<=n;i++) vh.AddTriangle(start,start+i,start+(i==n?1:i+1));
        }
    }
}
