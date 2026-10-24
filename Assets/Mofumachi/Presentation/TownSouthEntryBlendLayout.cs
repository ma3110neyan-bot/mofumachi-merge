using System;
using System.Collections.Generic;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // South owns this narrow transition. Central's accepted painting and mesh
    // remain untouched; its bottom texels continue into the garden at the join.
    public static class TownSouthEntryBlendLayout
    {
        public const float Left=TownCentralMasterLayout.Left,Right=TownCentralMasterLayout.Right;
        public const float Top=-1f,Bottom=-1.28f;
        public static IReadOnlyList<float> Rows {get;}=Array.AsReadOnly(new[]{Bottom,-1.14f,-1.10f,Top});
        public static TownPoint TexturePoint(TownPoint point)=>TownCentralMasterLayout.TexturePoint(new TownPoint(point.X,2*Top-point.Y));
        public static float Opacity(float y)
        {float t=Math.Max(0,Math.Min(1,(y+1.14f)/.04f));return t*t*(3-2*t);}
        public static TownPoint LateralTexturePoint(TownPoint p)=>TownCentralMasterLayout.TexturePoint(new TownPoint((p.X<0?2*Left:2*Right)-p.X,p.Y));
        public static float LateralOpacity(TownPoint p)
        {
            float distance=Math.Abs(p.X)-Right;if(distance<0||distance>.06f)return 0;
            float depth=Math.Max(0,Math.Min(1,1-distance/.06f));
            float side=Math.Max(0,Math.Min(1,(.24f-Math.Abs(p.Y+.20f))/.06f));
            return depth*side;
        }
        public static float VertexOpacity(TownPoint p)
        {
            float side=Math.Max(0,Math.Min(1,Math.Min(p.X-Left,Right-p.X)/.05f));
            float depth=Math.Max(0,Math.Min(1,(Top-p.Y)/.10f));
            return Opacity(p.Y)*(1-depth*(1-side));
        }
    }
}
