using System;
namespace Mofumachi.Presentation
{
    public readonly struct MergeCellRect
    {
        public float X {get;} public float Y {get;} public float Width {get;} public float Height {get;}
        public MergeCellRect(float x,float y,float w,float h){X=x;Y=y;Width=w;Height=h;}
        public bool Overlaps(MergeCellRect other)=>X<other.X+other.Width-.001f&&X+Width>other.X+.001f&&Y<other.Y+other.Height-.001f&&Y+Height>other.Y+.001f;
    }
    public static class MergePerspective
    {
        public static MergeCellRect Cell(int index,float width,float height)
        {
            if(index<0||index>=30)throw new ArgumentOutOfRangeException(nameof(index));
            if(!float.IsFinite(width)||!float.IsFinite(height)||width<256||height<308)throw new ArgumentOutOfRangeException(nameof(width),"A 5x6 board requires 48-unit input targets");
            const float gap=4;int row=index/5,column=index%5;float bottom=(width-gap*4)/5;
            float back=Math.Max(.76f,48/bottom);float scale=back+(1-back)*row/5;
            float average=(height-gap*5)/6,depth=Math.Max(0,Math.Min(.18f,1-48/average));
            float h=average*(1+depth*(row-2.5f)/2.5f),used=0;
            for(int r=0;r<=row;r++)used+=average*(1+depth*(r-2.5f)/2.5f);
            float w=bottom*scale,rowWidth=w*5+gap*4;
            return new MergeCellRect((width-rowWidth)/2+column*(w+gap),Math.Max(0,height-used-row*gap),w,h);
        }
        public static float ArtScale(int index)
        {if(index<0||index>=30)throw new ArgumentOutOfRangeException(nameof(index));return .82f+(index/5)*.036f;}
    }
}
