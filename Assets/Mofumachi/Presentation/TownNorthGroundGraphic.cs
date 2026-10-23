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
    // Flush stonework, not an obstacle or floating UI. A letter seal and floral
    // compass give the open delivery forecourt a purpose without taking away
    // a single walkable point. This small colour-only mesh needs no new texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownNorthCourtGraphic:MaskableGraphic
    {
        private TownContinuousWorldView world;private TownPoint center;private float width=1.10f;private bool bound;
        public void Bind(TownContinuousWorldView source){world=source;bound=true;raycastTarget=false;SetAllDirty();}
        public void BindPreview(TownPoint source,float sourceWidth){world=null;center=source;width=sourceWidth;bound=true;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!bound)return;float scale=world!=null?world.RenderScale:rectTransform.rect.width/width;
            var gold=new Color(.62f,.36f,.14f,.50f);var light=new Color(1,.91f,.65f,.65f);var rose=new Color(.78f,.34f,.40f,.38f);
            Vector2 Local(float x,float y)=>world!=null?world.Local(new TownPoint(x,y),rectTransform):rectTransform.rect.center+new Vector2((x-center.X)*scale,(y-center.Y)*scale);
            void Line(float x,float y,float tx,float ty,float width,Color tint)
            {
                var a=Local(x,y);var b=Local(tx,ty);var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*scale*.5f;int n=vh.currentVertCount;
                vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
            void Ring(float cx,float cy,float rx,float ry,Color tint)
            {
                for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Line(cx+Mathf.Cos(a)*rx,cy+Mathf.Sin(a)*ry,cx+Mathf.Cos(b)*rx,cy+Mathf.Sin(b)*ry,.003f,tint);}
            }
            const float y=1.90f;
            Ring(0,y,.225f,.13f,gold);Ring(0,y,.211f,.119f,light);
            // An envelope stamped into the paving points towards the cottage.
            Line(-.085f,y-.042f,.085f,y-.042f,.004f,gold);Line(.085f,y-.042f,.085f,y+.042f,.004f,gold);
            Line(.085f,y+.042f,-.085f,y+.042f,.004f,gold);Line(-.085f,y+.042f,-.085f,y-.042f,.004f,gold);
            Line(-.085f,y+.042f,0,y-.012f,.004f,gold);Line(0,y-.012f,.085f,y+.042f,.004f,gold);
            Line(-.085f,y-.042f,-.028f,y-.002f,.003f,gold);Line(.085f,y-.042f,.028f,y-.002f,.003f,gold);
            foreach(float x in new[]{-.19f,.19f})
                for(int petal=0;petal<5;petal++){float a=petal*Mathf.PI*2/5;Ring(x+Mathf.Cos(a)*.012f,y+Mathf.Sin(a)*.012f,.009f,.006f,rose);}
            // Low stone flourishes outline two passing bays, leaving the centre
            // and both side approaches wholly traversable at every growth level.
            foreach(float side in new[]{-1f,1f})
            {
                Line(side*.27f,1.78f,side*.27f,2.02f,.003f,light);
                Line(side*.27f,2.02f,side*.21f,2.07f,.003f,light);
            }
        }
    }
}
