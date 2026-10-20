using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownVillageDetailGraphic:MaskableGraphic
    {
        private static Texture2D atlas;
        private TownSceneryKind kind;
        public override Texture mainTexture=>atlas!=null?atlas:atlas=Resources.Load<Texture2D>(TownVillageDressing.AtlasResource);
        public static Rect Bound(TownSceneryKind source)
        {var b=TownVillageDressing.SpriteBounds(source);return new Rect(b.X,b.Y,b.Width,b.Height);}
        public void Bind(TownSceneryKind source){kind=source;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;var r=GetPixelAdjustedRect();var b=Bound(kind);var uv=new Rect(b.x/1536f,1-b.yMax/1024f,b.width/1536f,b.height/1024f);
            vh.AddVert(new Vector2(r.xMin,r.yMin),color,uv.min);vh.AddVert(new Vector2(r.xMin,r.yMax),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector2(r.xMax,r.yMax),color,uv.max);vh.AddVert(new Vector2(r.xMax,r.yMin),color,new Vector2(uv.xMax,uv.yMin));vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
