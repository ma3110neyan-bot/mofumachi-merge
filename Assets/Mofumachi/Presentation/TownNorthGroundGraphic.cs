using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // A ground-only north world piece, below independently sorted official
    // buildings, seats, flowers, fountain and residents. Never a screen backdrop.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownNorthGroundGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;
        private static Texture2D painting;
        private TownNorthCourtGraphic court;
        public override Texture mainTexture=>painting!=null?painting:painting=Resources.Load<Texture2D>(TownNorthMasterLayout.Resource);
        public void Bind(TownContinuousWorldView source)
        {
            world=source;raycastTarget=false;SetAllDirty();
            court=UIWidgets.Node("North delivery court stone inlay",rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<TownNorthCourtGraphic>();court.Bind(source);
        }
        public void BindPreview(TownPoint source,float sourceWidth)
        {
            world=null;center=source;width=sourceWidth;raycastTarget=false;SetAllDirty();
            if(court==null)court=UIWidgets.Node("North delivery court stone inlay",rectTransform,new Rect(0,0,1,1)).gameObject.AddComponent<TownNorthCourtGraphic>();
            court.BindPreview(source,sourceWidth);
        }
        public void SetPreview(TownPoint source,float sourceWidth){center=source;width=sourceWidth;SetVerticesDirty();court?.BindPreview(source,sourceWidth);}
        public void Refresh(){SetVerticesDirty();court?.SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;
            Vector2 Local(TownPoint p)=>world!=null?world.Local(p,rectTransform):rectTransform.rect.center+new Vector2((p.X-center.X)*rectTransform.rect.width/width,(p.Y-center.Y)*rectTransform.rect.width/width);
            void Vertex(float x,float y){var p=new TownPoint(x,y);var uv=TownNorthMasterLayout.TexturePoint(p);vh.AddVert(Local(p),color,new Vector2(uv.X,uv.Y));}
            Vertex(TownNorthMasterLayout.Left,TownNorthMasterLayout.Bottom);Vertex(TownNorthMasterLayout.Left,TownNorthMasterLayout.Top);
            Vertex(TownNorthMasterLayout.Right,TownNorthMasterLayout.Top);Vertex(TownNorthMasterLayout.Right,TownNorthMasterLayout.Bottom);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
    // Flush painted stonework, below actors and scenery. The native review
    // rejected the former faint line drawing; use finished stone tesserae rather
    // than drawing what resembles a navigation/selection guide over the court.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownNorthCourtGraphic:MaskableGraphic
    {
        public const string Resource="Mofumachi/TownNorthCourtMosaic";
        private static Texture2D painting;
        public override Texture mainTexture=>painting!=null?painting:painting=Resources.Load<Texture2D>(Resource);
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;private bool bound;
        public void Bind(TownContinuousWorldView source){world=source;bound=true;raycastTarget=false;SetAllDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;center=source;width=sourceWidth;bound=true;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!bound||mainTexture==null)return;float scale=world!=null?world.RenderScale:rectTransform.rect.width/width;
            Vector2 Local(float x,float y)=>world!=null?world.Local(new TownPoint(x,y),rectTransform):rectTransform.rect.center+new Vector2((x-center.X)*scale,(y-center.Y)*scale);
            // Keep the native2:1 art ratio and leave the left planted ring and
            // cottage waiting apron clear. This changes no collision or route.
            const float left=-.21f,right=.21f,bottom=1.765f,top=1.975f;
            vh.AddVert(Local(left,bottom),color,new Vector2(0,0));
            vh.AddVert(Local(left,top),color,new Vector2(0,1));
            vh.AddVert(Local(right,top),color,new Vector2(1,1));
            vh.AddVert(Local(right,bottom),color,new Vector2(1,0));
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
