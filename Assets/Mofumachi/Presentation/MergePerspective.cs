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
            const float gap=4;int row=index/5,column=index%5;
            float side=Math.Min((width-gap*4)/5,(height-gap*5)/6);
            float left=(width-(side*5+gap*4))/2,bottom=(height-(side*6+gap*5))/2;
            return new MergeCellRect(left+column*(side+gap),bottom+(5-row)*(side+gap),side,side);
        }
        public static float ArtScale(int index)
        {if(index<0||index>=30)throw new ArgumentOutOfRangeException(nameof(index));return 1;}
    }
}
