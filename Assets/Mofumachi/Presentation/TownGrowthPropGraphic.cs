using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownGrowthPropGraphic : MaskableGraphic
    {
        private static Texture2D atlas,streetAtlas;
        public TownGrowthProp Prop {get;private set;}
        public TownRegionId Region {get;private set;}
        public override Texture mainTexture=>(int)Prop>=6?(streetAtlas!=null?streetAtlas:streetAtlas=Resources.Load<Texture2D>("Mofumachi/TownStreetPropsAtlas")):(atlas!=null?atlas:atlas=Resources.Load<Texture2D>("Mofumachi/TownGrowthPropsAtlas"));
        // Measured bounds in the source atlas (1536×1024); normalized UVs also
        // work with the Android 1024 import. No runtime slicing or texture copies.
        private static readonly Rect[] Bounds={new Rect(116,17,620,327),new Rect(1090,3,161,351),new Rect(132,343,572,311),new Rect(835,354,630,325),new Rect(96,661,655,343),new Rect(808,688,652,290)};
        private static readonly Rect[] StreetBounds={new Rect(52,92,617,492),new Rect(710,65,485,520),new Rect(38,648,557,535),new Rect(645,665,585,512)};
        private static Rect Bound(TownGrowthProp prop)=>(int)prop>=6?StreetBounds[(int)prop-6]:Bounds[(int)prop];
        public static float Aspect(TownGrowthProp prop){var r=Bound(prop);return r.width/r.height;}
        public void Bind(TownGrowthProp prop,TownRegionId region){Prop=prop;Region=region;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;var r=GetPixelAdjustedRect();var b=Bound(Prop);float tw=(int)Prop>=6?1254:1536,th=(int)Prop>=6?1254:1024;var uv=new Rect(b.x/tw,1-b.yMax/th,b.width/tw,b.height/th);
            vh.AddVert(new Vector3(r.xMin,r.yMin),color,uv.min);vh.AddVert(new Vector3(r.xMin,r.yMax),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector3(r.xMax,r.yMax),color,uv.max);vh.AddVert(new Vector3(r.xMax,r.yMin),color,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
