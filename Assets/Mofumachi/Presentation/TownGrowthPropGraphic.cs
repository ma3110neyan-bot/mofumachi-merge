using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TownGrowthPropGraphic : MaskableGraphic
    {
        private static Texture2D atlas;
        public TownGrowthProp Prop {get;private set;}
        public TownRegionId Region {get;private set;}
        public override Texture mainTexture=>atlas!=null?atlas:atlas=Resources.Load<Texture2D>("Mofumachi/TownGrowthPropsAtlas");
        // Measured bounds in the source atlas (1536×1024); normalized UVs also
        // work with the Android 1024 import. No runtime slicing or texture copies.
        private static readonly Rect[] Bounds={new Rect(116,17,620,327),new Rect(1090,3,161,351),new Rect(132,343,572,311),new Rect(835,354,630,325),new Rect(96,661,655,343),new Rect(808,688,652,290)};
        public static float Aspect(TownGrowthProp prop)=>Bounds[(int)prop].width/Bounds[(int)prop].height;
        public void Bind(TownGrowthProp prop,TownRegionId region){Prop=prop;Region=region;raycastTarget=false;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture==null)return;var r=GetPixelAdjustedRect();var b=Bounds[(int)Prop];var uv=new Rect(b.x/1536f,1-b.yMax/1024f,b.width/1536f,b.height/1024f);
            vh.AddVert(new Vector3(r.xMin,r.yMin),color,uv.min);vh.AddVert(new Vector3(r.xMin,r.yMax),color,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector3(r.xMax,r.yMax),color,uv.max);vh.AddVert(new Vector3(r.xMax,r.yMin),color,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
