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
            float back=Math.Max(.86f,48/bottom);float scale=back+(1-back)*row/5;
            float w=bottom*scale,h=(height-gap*5)/6,rowWidth=w*5+gap*4;
            return new MergeCellRect((width-rowWidth)/2+column*(w+gap),height-(row+1)*h-row*gap,w,h);
        }
        public static float ArtScale(int index)
        {if(index<0||index>=30)throw new ArgumentOutOfRangeException(nameof(index));return .9f+(index/5)*.02f;}
    }
}
