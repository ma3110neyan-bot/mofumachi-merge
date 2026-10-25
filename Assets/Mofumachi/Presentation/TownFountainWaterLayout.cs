using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Coordinates follow the frozen fountain's 1610x977 alpha-cut artwork.
    // Value tuples and cached anchors: no particles, textures or frame arrays.
    public static class TownFountainWaterLayout
    {
        public const int QuadCount=80;
        public const float LoopSeconds=10,FrameInterval=1f/30;
        private static readonly TownPoint[] starts={new TownPoint(.443f,.786f),new TownPoint(.564f,.784f),new TownPoint(.398f,.676f),new TownPoint(.446f,.660f),new TownPoint(.565f,.662f),new TownPoint(.602f,.676f)};
        private static readonly TownPoint[] ends={new TownPoint(.442f,.720f),new TownPoint(.565f,.720f),new TownPoint(.386f,.488f),new TownPoint(.432f,.443f),new TownPoint(.573f,.447f),new TownPoint(.618f,.488f)};
        private static readonly TownPoint[] glints={new TownPoint(.58f,.452f),new TownPoint(.42f,.40f),new TownPoint(.56f,.365f),new TownPoint(.50f,.802f)};
        private static float Repeat(float value)=>value-(float)Math.Floor(value);
        private static TownPoint Lerp(TownPoint a,TownPoint b,float t)=>new TownPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
        private static (int,TownPoint,TownPoint,TownPoint,TownPoint,float) Diamond(int kind,TownPoint p,float x,float y,float alpha)
            =>(kind,new TownPoint(p.X-x,p.Y),new TownPoint(p.X,p.Y+y),new TownPoint(p.X+x,p.Y),new TownPoint(p.X,p.Y-y),alpha);
        public static (int Kind,TownPoint A,TownPoint B,TownPoint C,TownPoint D,float Alpha) Quad(int index,float phase)
        {
            if(index<0||index>=QuadCount||!float.IsFinite(phase)||phase<0)throw new ArgumentOutOfRangeException();
            phase%=LoopSeconds;
            if(index<12)
            {
                int stream=index/2;float t=Repeat(phase*1.2f+index%2*.5f+stream*.117f);
                var a=Lerp(starts[stream],ends[stream],t);var b=Lerp(starts[stream],ends[stream],Math.Min(1,t+.15f));
                float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                float nx=length>0?-dy/length*.0014f:0,ny=length>0?dx/length*.0014f:0;
                return (0,new TownPoint(a.X+nx,a.Y+ny),new TownPoint(b.X+nx,b.Y+ny),new TownPoint(b.X-nx,b.Y-ny),new TownPoint(a.X-nx,a.Y-ny),.28f*(float)Math.Sin(t*Math.PI));
            }
            if(index<24)
            {
                int slot=index-12,stream=slot/2;float t=Repeat(phase*.9f+slot%2*.5f+stream*.17f),side=slot%2==0?-1:1;
                var p=new TownPoint(ends[stream].X+side*(.004f+.007f*t),ends[stream].Y+.018f*4*t*(1-t)-.004f*t);
                return Diamond(1,p,.0017f*(1-.25f*t),.0035f,.30f*(1-t));
            }
            if(index<72)
            {
                int part=index-24,ring=part/16,segment=part%16;float t=Repeat(phase*.5f+ring/3f),x=.035f+.105f*t,y=.008f+.024f*t;
                float a=segment*(float)Math.PI/8,b=(segment+1)*(float)Math.PI/8;
                TownPoint Point(float angle,float size)=>new TownPoint(.5f+(float)Math.Cos(angle)*x*size,.39f+(float)Math.Sin(angle)*y*size);
                return (2,Point(a,1),Point(a,.96f),Point(b,.96f),Point(b,1),.32f*(1-t));
            }
            int sparkle=(index-72)/2;float glow=.15f+.85f*Math.Max(0,(float)Math.Sin(phase*2*Math.PI/5+sparkle*1.7f));
            bool horizontal=index%2==0;
            return Diamond(3,glints[sparkle],(horizontal?.009f:.0015f)*glow,(horizontal?.0016f:.012f)*glow,.30f*glow);
        }
    }
}
