using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // A ground-only south garden world piece, below independently sorted official
    // buildings, seats, flowers, fountain and residents. Never a screen backdrop.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownSouthGroundGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;
        private static Texture2D painting;
        private TownSouthEntryBlendGraphic entry;
        private TownBoundaryGardenGraphic boundary;
        private TownBoundaryCurveGraphic curve;
        public override Texture mainTexture=>painting!=null?painting:painting=Resources.Load<Texture2D>(TownSouthMasterLayout.Resource);
        private void EnsureEntry()
        {
            if(curve==null)curve=UIWidgets.Node("Curved painted street ends",rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<TownBoundaryCurveGraphic>();
            if(entry==null)entry=UIWidgets.Node("South garden entry blend",rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<TownSouthEntryBlendGraphic>();
            // The last master layer hosts the same outside-floor perimeter in
            // live town and growth previews, below the original sorted props.
            if(boundary==null)boundary=UIWidgets.Node("Planted town perimeter",rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<TownBoundaryGardenGraphic>();
        }
        public void Bind(TownContinuousWorldView source){world=source;raycastTarget=false;EnsureEntry();curve.Bind(source);entry.Bind(source);boundary.Bind(source);SetAllDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;center=source;width=sourceWidth;raycastTarget=false;EnsureEntry();curve.BindPreview(source,sourceWidth);entry.BindPreview(source,sourceWidth);boundary.BindPreview(source,sourceWidth);SetAllDirty();}
        public void SetPreview(TownPoint source,float sourceWidth){center=source;width=sourceWidth;curve?.SetPreview(source,sourceWidth);entry?.SetPreview(source,sourceWidth);boundary?.SetPreview(source,sourceWidth);SetVerticesDirty();}
        public void Refresh(){curve?.Refresh();entry?.Refresh();boundary?.Refresh();SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;
            Vector2 Local(TownPoint p)=>world!=null?world.Local(p,rectTransform):rectTransform.rect.center+new Vector2((p.X-center.X)*rectTransform.rect.width/width,(p.Y-center.Y)*rectTransform.rect.width/width);
            var xs=new[]{TownSouthMasterLayout.Left,-.80f,.80f,TownSouthMasterLayout.Right};
            var ys=new[]{TownSouthMasterLayout.Bottom,-3.12f,-1.12f,TownSouthMasterLayout.Top};
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                var p=new TownPoint(xs[col],ys[row]);var uv=TownSouthMasterLayout.TexturePoint(p);var tint=color;
                if(row==0||row==3||col==0||col==3)tint.a=0;
                vh.AddVert(Local(p),tint,new Vector2(uv.X,uv.Y));
            }
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {int i=row*4+col;vh.AddTriangle(i,i+4,i+5);vh.AddTriangle(i,i+5,i+1);}
        }
    }
}
