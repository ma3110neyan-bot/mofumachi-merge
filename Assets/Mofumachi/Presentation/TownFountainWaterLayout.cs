using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Coordinates follow the frozen fountain's 1610x977 alpha-cut artwork.
    // Value tuples and cached anchors: no particles, textures or frame arrays.
    public static class TownFountainWaterLayout
    {
        public const int QuadCount=196;
        public const float LoopSeconds=10,FrameInterval=1f/30,DisplayScale=1.14f,ShowInterval=180,QaShowInterval=45;
        private static readonly TownPoint[] starts={new TownPoint(.443f,.786f),new TownPoint(.564f,.784f),new TownPoint(.398f,.676f),new TownPoint(.446f,.660f),new TownPoint(.565f,.662f),new TownPoint(.602f,.676f)};
        private static readonly TownPoint[] ends={new TownPoint(.442f,.720f),new TownPoint(.565f,.720f),new TownPoint(.386f,.488f),new TownPoint(.432f,.443f),new TownPoint(.573f,.447f),new TownPoint(.618f,.488f)};
        private static readonly TownPoint[] glints={new TownPoint(.58f,.452f),new TownPoint(.42f,.40f),new TownPoint(.56f,.365f),new TownPoint(.50f,.802f),new TownPoint(.39f,.37f),new TownPoint(.61f,.39f),new TownPoint(.47f,.33f),new TownPoint(.52f,.45f)};
        public static TownPoint Project(TownPoint uv,float scale)=>new TownPoint((uv.X-.5f)*TownCentralMasterLayout.FountainWidth*scale,TownCentralMasterLayout.FountainCenterY+(TownCentralMasterLayout.FountainFeetY+uv.Y*TownSceneryLayout.FountainHeight-TownCentralMasterLayout.FountainCenterY)*scale);
        private static float Smooth(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        public static (int Stage,float Strength,float Glow) ShowAt(float elapsed)=>ShowAt(elapsed,ShowInterval);
        public static (int Stage,float Strength,float Glow) ShowAt(float elapsed,float cycleSeconds)
        {
            if(!float.IsFinite(elapsed)||elapsed<0)throw new ArgumentOutOfRangeException(nameof(elapsed));
            if(!float.IsFinite(cycleSeconds)||cycleSeconds<10||cycleSeconds>600)throw new ArgumentOutOfRangeException(nameof(cycleSeconds));
            elapsed%=cycleSeconds;float prelude=cycleSeconds-9,show=cycleSeconds-7,recovery=cycleSeconds-1;
            if(elapsed<prelude)return (0,0,0);
            if(elapsed<show)return (1,0,Smooth((elapsed-prelude)/2));
            if(elapsed<recovery)return (2,Smooth((elapsed-show)/.75f),1);
            float strength=1-Smooth(elapsed-recovery);return (3,strength,strength);
        }
        private static float Repeat(float value)=>value-(float)Math.Floor(value);
        private static TownPoint Lerp(TownPoint a,TownPoint b,float t)=>new TownPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
        private static (int,TownPoint,TownPoint,TownPoint,TownPoint,float) Diamond(int kind,TownPoint p,float x,float y,float alpha)
            =>(kind,new TownPoint(p.X-x,p.Y),new TownPoint(p.X,p.Y+y),new TownPoint(p.X+x,p.Y),new TownPoint(p.X,p.Y-y),alpha);
        public static (int Kind,TownPoint A,TownPoint B,TownPoint C,TownPoint D,float Alpha) Quad(int index,float phase)=>Quad(index,phase,0,0);
        public static (int Kind,TownPoint A,TownPoint B,TownPoint C,TownPoint D,float Alpha) Quad(int index,float phase,float showStrength,float anticipation)
        {
            if(index<0||index>=QuadCount||!float.IsFinite(phase)||phase<0||!float.IsFinite(showStrength)||showStrength<0||showStrength>1||!float.IsFinite(anticipation)||anticipation<0||anticipation>1)throw new ArgumentOutOfRangeException();
            phase%=LoopSeconds;
            if(index<48)
            {
                int stream=index/8,part=index%8/2;bool core=index%2==1;
                // The blue body remains continuous; white packets actually fall.
                // Fixed strips with alpha pulsing were indistinguishable from art.
                float t=core?Repeat(phase*1.6f+part*.25f+stream*.11f):part/4f;
                float next=core?Math.Min(1,t+.23f):t+.25f;
                TownPoint Flow(float along)
                {
                    var at=Lerp(starts[stream],ends[stream],along);
                    return new TownPoint(at.X+.0025f*(float)Math.Sin(along*6+phase*4+stream)*along,at.Y);
                }
                var a=Flow(t);var b=Flow(next);
                float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                float width=(core?.0062f:.0115f)*(1+.35f*showStrength),nx=length>0?-dy/length*width:width,ny=length>0?dx/length*width:0;
                float pulse=.5f+.5f*(float)Math.Sin(phase*2*(float)Math.PI-part*1.7f-stream*.8f);
                return (0,new TownPoint(a.X+nx,a.Y+ny),new TownPoint(b.X+nx,b.Y+ny),new TownPoint(b.X-nx,b.Y-ny),new TownPoint(a.X-nx,a.Y-ny),(core?.72f+.12f*pulse:.46f+.10f*pulse)+.02f*showStrength);
            }
            if(index<72)
            {
                int slot=index-48,stream=slot/4;float t=Repeat(phase*1.2f+slot%4*.25f+stream*.17f),side=slot%2==0?-1:1;
                var p=new TownPoint(ends[stream].X+side*(.006f+(.025f+.012f*showStrength)*t),ends[stream].Y+(.045f+.025f*showStrength)*4*t*(1-t)-.012f*t);
                return Diamond(1,p,.0085f*(1-.25f*t),.014f,.80f*(1-t));
            }
            if(index<120)
            {
                int part=index-72,ring=part/16,segment=part%16;float t=Repeat(phase*.6f+ring/3f),x=.035f+.105f*t,y=.008f+.024f*t;
                float a=segment*(float)Math.PI/8,b=(segment+1)*(float)Math.PI/8;
                TownPoint Point(float angle,float size)=>new TownPoint(.5f+(float)Math.Cos(angle)*x*size,.39f+(float)Math.Sin(angle)*y*size);
                return (2,Point(a,1),Point(a,.74f),Point(b,.74f),Point(b,1),(.65f+.15f*anticipation)*(1-t));
            }
            if(index<136)
            {
                int sparkle=(index-120)/2;float glow=.25f+.75f*Math.Max(0,(float)Math.Sin(phase*2*Math.PI/2+sparkle*1.7f));bool horizontal=index%2==0;
                return Diamond(3,glints[sparkle],(horizontal?.024f:.004f)*glow,(horizontal?.004f:.032f)*glow,(.74f+.10f*anticipation)*glow);
            }
            if(index<184)
            {
                int slot=index-136,jet=slot/16,part=slot%16/2;bool core=slot%2==1;
                // Blue water keeps the arc continuous. White packets travel
                // along it, so a show reads as moving water rather than wires.
                float a=core?Repeat(phase*.7f+part/8f+jet*.13f):part/8f,b=core?Math.Min(1,a+.08f):(part+1)/8f;
                TownPoint Curve(float t)
                {
                    var start=new TownPoint(.5f+(jet-1)*.025f,.76f);
                    // A curved centre jet lands in the basin, rather than a
                    // stationary vertical bar that doubles back on itself.
                    var end=new TownPoint(.5f+(jet==1?.055f:(jet-1)*.135f),.43f);
                    var control=new TownPoint(.5f+(jet==1?-.035f:(jet-1)*.09f)+.010f*(float)Math.Sin(phase*2+jet),.82f+(.86f+.035f*(float)Math.Sin(phase*3+jet))*showStrength);
                    return new TownPoint((1-t)*(1-t)*start.X+2*(1-t)*t*control.X+t*t*end.X,(1-t)*(1-t)*start.Y+2*(1-t)*t*control.Y+t*t*end.Y);
                }
                var from=Curve(a);var to=Curve(b);float dx=to.X-from.X,dy=to.Y-from.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                float width=core?.004f:.009f,nx=length>.000001f?-dy/length*width:width,ny=length>.000001f?dx/length*width:0;
                float alpha=showStrength*(core?.68f+.08f*(.5f+.5f*(float)Math.Sin(phase*7-part)):.34f);
                return (0,new TownPoint(from.X+nx,from.Y+ny),new TownPoint(to.X+nx,to.Y+ny),new TownPoint(to.X-nx,to.Y-ny),new TownPoint(from.X-nx,from.Y-ny),alpha);
            }
            int drop=index-184;float flight=Repeat(phase*.8f+drop/12f),direction=drop%2==0?-1:1;
            var top=new TownPoint(.5f+direction*(.012f+.07f*flight),.79f+showStrength*(.40f-.20f*flight)+.045f*4*flight*(1-flight));
            return Diamond(1,top,.006f,.014f,.80f*showStrength*(1-flight));
        }
    }
    // Presentation-only session time. No wall clock or SaveData fields.
    public sealed class TownFountainWaterClock
    {
        private int ticks,animationTicks;private double pending;private readonly int cycleTicks;
        public TownFountainWaterClock():this(TownFountainWaterLayout.ShowInterval){}
        public TownFountainWaterClock(float cycleSeconds)
        {
            if(!float.IsFinite(cycleSeconds)||cycleSeconds<10||cycleSeconds>600)throw new ArgumentOutOfRangeException(nameof(cycleSeconds));
            cycleTicks=(int)Math.Round(cycleSeconds*30);
        }
        public float CycleSeconds=>cycleTicks/30f;
        public float Phase=>animationTicks/30f;
        public float Elapsed=>ticks/30f;
        public bool Advance(float seconds,bool active)
        {
            if(!active||!float.IsFinite(seconds)||seconds<=0)return false;
            pending+=seconds;double steps=Math.Floor((pending+1e-8)*30);
            if(steps==0)return false;
            pending=Math.Max(0,pending-steps/30d);ticks=(ticks+(int)(steps%cycleTicks))%cycleTicks;animationTicks=(animationTicks+(int)(steps%300))%300;return true;
        }
    }
}
