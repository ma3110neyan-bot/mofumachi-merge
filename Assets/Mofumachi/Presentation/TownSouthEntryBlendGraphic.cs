using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownSouthEntryBlendGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;
        private static Texture2D edge;
        public override Texture mainTexture=>edge!=null?edge:edge=Resources.Load<Texture2D>(TownCentralMasterLayout.Resource);
        public void Bind(TownContinuousWorldView source){world=source;raycastTarget=false;SetAllDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;center=source;width=sourceWidth;raycastTarget=false;SetAllDirty();}
        public void SetPreview(TownPoint source,float sourceWidth){center=source;width=sourceWidth;SetVerticesDirty();}
        public void Refresh()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;
            Vector2 Local(TownPoint p)=>world!=null?world.Local(p,rectTransform):rectTransform.rect.center+new Vector2((p.X-center.X)*rectTransform.rect.width/width,(p.Y-center.Y)*rectTransform.rect.width/width);
            var xs=new[]{TownSouthEntryBlendLayout.Left,-.55f,.55f,TownSouthEntryBlendLayout.Right};
            foreach(float y in TownSouthEntryBlendLayout.Rows)foreach(float x in xs)
            {
                var p=new TownPoint(x,y);var uv=TownSouthEntryBlendLayout.TexturePoint(p);var tint=color;tint.a*=TownSouthEntryBlendLayout.VertexOpacity(p);
                vh.AddVert(Local(p),tint,new Vector2(uv.X,uv.Y));
            }
            for(int row=0;row<TownSouthEntryBlendLayout.Rows.Count-1;row++)for(int col=0;col<3;col++)
            {int i=row*4+col;vh.AddTriangle(i,i+4,i+5);vh.AddTriangle(i,i+5,i+1);}
            foreach(float side in new[]{-1f,1f})
            {
                int start=vh.currentVertCount;
                var joinXs=side<0?new[]{-.66f,-.63f,-.60f}:new[]{.60f,.63f,.66f};
                foreach(float y in new[]{-.44f,-.38f,-.02f,.04f})foreach(float x in joinXs)
                {
                    var p=new TownPoint(x,y);var uv=TownSouthEntryBlendLayout.LateralTexturePoint(p);var tint=color;tint.a*=TownSouthEntryBlendLayout.LateralOpacity(p);
                    vh.AddVert(Local(p),tint,new Vector2(uv.X,uv.Y));
                }
                for(int row=0;row<3;row++)for(int col=0;col<2;col++)
                {int i=start+row*3+col;vh.AddTriangle(i,i+3,i+4);vh.AddTriangle(i,i+4,i+1);}
            }
        }
    }
}
