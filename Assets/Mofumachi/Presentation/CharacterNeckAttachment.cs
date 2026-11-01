using System;
using System.Collections.Generic;
namespace Mofumachi.Presentation
{
    // Immutable source registration after Configure. There is no head timer,
    // tween, previous-frame position or independent root to catch up with.
    [Serializable] public sealed class CharacterNeckBinding
    {
        public int version;
        public float bodyX,bodyY,headX,headY,closedHeadX,closedHeadY;
        public bool hasClosedHead;
        public void Validate()
        {
            if(version!=1||!float.IsFinite(bodyX)||!float.IsFinite(bodyY)||!float.IsFinite(headX)||!float.IsFinite(headY)
                ||hasClosedHead&&(!float.IsFinite(closedHeadX)||!float.IsFinite(closedHeadY)))throw new InvalidOperationException("Invalid body-neck registration.");
        }
        public NoahPartTransform Resolve(NoahPartTransform body,float headAngle,bool closed)
        {
            float x=closed&&hasClosedHead?closedHeadX:headX,y=closed&&hasClosedHead?closedHeadY:headY;
            body.Point(bodyX,bodyY,out float bx,out float by);
            new NoahPartTransform(0,0,headAngle).Point(x,y,out float hx,out float hy);
            return new NoahPartTransform(bx-hx,by-hy,headAngle);
        }
        // Build-time only. Alpha is top-to-bottom, before atlas packing. The
        // central contour excludes ears/hats and transparent rectangular gutters.
        public static void FindJoint(byte[] alpha,int width,int height,float pivotX,float pivotY,float scale,bool chin,out float x,out float y)
        {
            if(alpha==null||width<5||height<1||alpha.Length!=width*height||!float.IsFinite(scale)||scale<=0||!float.IsFinite(pivotX)||!float.IsFinite(pivotY))throw new ArgumentException("Invalid neck source.");
            int left=(int)(width*.4f),right=(int)(width*.6f),edge=chin?-1:height;
            for(int row=0;row<height;row++)for(int col=left;col<right;col++)if(alpha[row*width+col]>=128)edge=chin?Math.Max(edge,row):Math.Min(edge,row);
            if(edge<0||edge>=height)throw new InvalidOperationException("No opaque central neck contour.");
            var columns=new List<int>();
            for(int row=Math.Max(0,edge-2);row<=Math.Min(height-1,edge+2);row++)for(int col=left;col<right;col++)if(alpha[row*width+col]>=128)columns.Add(col);
            columns.Sort();int middle=columns.Count/2;float centre=columns.Count%2==0?(columns[middle-1]+columns[middle])*.5f:columns[middle];
            // Median X and extreme Y alone may intersect transparent padding.
            // Select a real opaque pixel on that contour, then use its centre
            // (the same centre sampled by the atlas's bilinear texture lookup).
            int contact=-1;float nearest=float.PositiveInfinity;
            for(int col=left;col<right;col++)if(alpha[edge*width+col]>=128&&Math.Abs(col-centre)<nearest){contact=col;nearest=Math.Abs(col-centre);}
            if(contact<0)throw new InvalidOperationException("Opaque neck edge missing.");
            x=(contact+.5f-pivotX*width)*scale;y=(edge+.5f-pivotY*height)*scale;
        }
    }
}
