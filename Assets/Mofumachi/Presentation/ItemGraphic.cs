using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ItemGraphic : MaskableGraphic
    {
        private static Texture2D atlas;
        private static Texture2D Atlas => atlas!=null?atlas:atlas=Resources.Load<Texture2D>("Mofumachi/ItemAtlas");
        public override Texture mainTexture => Atlas;
        public string ItemId { get; private set; }
        public int Level { get; private set; }
        public Rect UvRect { get; private set; }
        public bool Polished { get; private set; }
        private Rect pixels;
        // Measured art bounds; sprites retain their silhouettes when Unity downsizes the shared atlas.
        private static readonly Rect[] Bounds={
            new Rect(16,121,351,254),new Rect(404,54,422,322),new Rect(828,42,423,344),
            new Rect(38,493,304,250),new Rect(388,453,412,302),new Rect(803,429,439,341),
            new Rect(62,828,242,363),new Rect(382,787,392,418),new Rect(772,787,476,438)};
        public void Bind(string itemId,int level)
        {
            ItemId=itemId;Level=level;raycastTarget=false;
            int row=itemId=="tea"?0:itemId=="bread"?1:itemId=="flower"?2:-1;
            pixels=row>=0&&level>=1&&level<=3?Bounds[row*3+level-1]:Rect.zero;
            UvRect=pixels.width>0?new Rect(pixels.x/1254f,1-pixels.yMax/1254f,pixels.width/1254f,pixels.height/1254f):Rect.zero;
            SetAllDirty();
        }
        public void SetPolished(bool value){Polished=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(Atlas==null||pixels.width<=0)return;
            Rect target=GetPixelAdjustedRect();float scale=Mathf.Min(target.width/pixels.width,target.height/pixels.height);
            Vector2 size=pixels.size*scale,min=target.center-size/2,max=min+size;
            if(Polished)Quad(vh,min+new Vector2(.8f,-2.25f),max+new Vector2(.8f,-2.25f),new Color(.29f,.18f,.24f,.17f*color.a));
            Color tint=new Color(1,1,1,color.a);
            Quad(vh,min,max,tint);
        }
        private void Quad(VertexHelper vh,Vector2 min,Vector2 max,Color tint)
        {
            int start=vh.currentVertCount;
            vh.AddVert(new Vector3(min.x,min.y),tint,UvRect.min);vh.AddVert(new Vector3(min.x,max.y),tint,new Vector2(UvRect.xMin,UvRect.yMax));
            vh.AddVert(new Vector3(max.x,max.y),tint,UvRect.max);vh.AddVert(new Vector3(max.x,min.y),tint,new Vector2(UvRect.xMax,UvRect.yMin));
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
