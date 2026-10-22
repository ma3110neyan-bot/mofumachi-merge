using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // A ground-only west world piece, below independently sorted official
    // buildings, seats, flowers, fountain and residents. Never a screen backdrop.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownWestGroundGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;
        private static Texture2D painting;
        public override Texture mainTexture=>painting!=null?painting:painting=Resources.Load<Texture2D>(TownWestMasterLayout.Resource);
        public void Bind(TownContinuousWorldView source){world=source;raycastTarget=false;SetAllDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;center=source;width=sourceWidth;raycastTarget=false;SetAllDirty();}
        public void SetPreview(TownPoint source,float sourceWidth){center=source;width=sourceWidth;SetVerticesDirty();}
        public void Refresh()=>SetVerticesDirty();
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;
            Vector2 Local(TownPoint p)=>world!=null?world.Local(p,rectTransform):rectTransform.rect.center+new Vector2((p.X-center.X)*rectTransform.rect.width/width,(p.Y-center.Y)*rectTransform.rect.width/width);
            void Vertex(float x,float y){var p=new TownPoint(x,y);var uv=TownWestMasterLayout.TexturePoint(p);vh.AddVert(Local(p),color,new Vector2(uv.X,uv.Y));}
            Vertex(TownWestMasterLayout.Left,TownWestMasterLayout.Bottom);Vertex(TownWestMasterLayout.Left,TownWestMasterLayout.Top);
            Vertex(TownWestMasterLayout.Right,TownWestMasterLayout.Top);Vertex(TownWestMasterLayout.Right,TownWestMasterLayout.Bottom);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
