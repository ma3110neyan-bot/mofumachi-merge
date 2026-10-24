using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // One shared-atlas batch, below existing independent props/residents. No
    // per-flower GameObjects, new textures, navigation or per-frame layout work.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownBoundaryGardenGraphic:MaskableGraphic
    {
        private static Texture2D atlas;
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;
        public override Texture mainTexture=>atlas!=null?atlas:atlas=Resources.Load<Texture2D>(TownBoundaryGardenLayout.Resource);
        public void Bind(TownContinuousWorldView source){world=source;raycastTarget=false;SetAllDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;center=source;width=sourceWidth;raycastTarget=false;SetAllDirty();}
        public void SetPreview(TownPoint source,float sourceWidth){center=source;width=sourceWidth;SetVerticesDirty();}
        public void Refresh()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;var rect=rectTransform.rect;
            float scale=world!=null?world.RenderScale:rect.width/width;if(scale<=0)return;
            int ordinal=0;
            foreach(var p in TownBoundaryGardenLayout.Dressing)
            {
                var feet=world!=null?world.Local(p.Feet,rectTransform):rect.center+new Vector2((p.Feet.X-center.X)*scale,(p.Feet.Y-center.Y)*scale);
                float l=feet.x-p.Width*scale/2,r=l+p.Width*scale,b=feet.y,t=b+p.Height*scale;
                bool mirror=(ordinal++&1)!=0;
                if(r<rect.xMin||l>rect.xMax||t<rect.yMin||b>rect.yMax)continue;
                var source=TownBoundaryGardenLayout.SpriteBounds(p.Kind);
                float u0=source.X/1536f,u1=(source.X+source.Width)/1536f,v0=1-(source.Y+source.Height)/1024f,v1=1-source.Y/1024f;
                if(mirror){float swap=u0;u0=u1;u1=swap;}
                int start=vh.currentVertCount;
                vh.AddVert(new Vector2(l,b),color,new Vector2(u0,v0));vh.AddVert(new Vector2(l,t),color,new Vector2(u0,v1));
                vh.AddVert(new Vector2(r,t),color,new Vector2(u1,v1));vh.AddVert(new Vector2(r,b),color,new Vector2(u1,v0));
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
