using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Quiet paper rules and stitched margins share the standard UGUI material.
    // Pure decoration: never captures a tap or allocates a texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StoryPaperGraphic:MaskableGraphic
    {
        public bool Speech;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<24||r.height<24)return;
            var gold=new Color(.70f,.53f,.38f,.25f);var fibre=new Color(.68f,.46f,.34f,.045f);
            for(float y=r.yMin+12;y<r.yMax-10;y+=14)Quad(vh,new Rect(r.xMin+10,y,r.width-20,.45f),fibre);
            for(float x=r.xMin+15;x<r.xMax-13;x+=9)
            {Quad(vh,new Rect(x,r.yMin+6,4,.65f),gold);Quad(vh,new Rect(x,r.yMax-7,4,.65f),gold);}
            Quad(vh,new Rect(r.xMin+6,r.yMin+12,.65f,r.height-24),gold);Quad(vh,new Rect(r.xMax-7,r.yMin+12,.65f,r.height-24),gold);
            if(Speech)
            {int n=vh.currentVertCount;vh.AddVert(new Vector2(r.center.x-7,r.yMin+1),UIWidgets.StoryPaper,Vector2.zero);vh.AddVert(new Vector2(r.center.x,r.yMin-9),UIWidgets.StoryPaper,Vector2.zero);vh.AddVert(new Vector2(r.center.x+7,r.yMin+1),UIWidgets.StoryPaper,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        }
        private static void Quad(VertexHelper vh,Rect r,Color color)
        {int n=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
