using System;
namespace Mofumachi.Presentation
{
    public readonly struct StreetCharacterOffset
    {
        public float X {get;} public float Y {get;}
        public StreetCharacterOffset(float x,float y){X=x;Y=y;}
    }
    public readonly struct StreetCharacterPose
    {
        public float Hop {get;} public float Lean {get;} public float Foot {get;}
        public float Breath {get;} public float Hand {get;} public float Look {get;}
        public StreetCharacterPose(float hop,float lean,float foot):this(hop,lean,foot,0,0,0){}
        public StreetCharacterPose(float hop,float lean,float foot,float breath,float hand,float look)
        {Hop=hop;Lean=lean;Foot=foot;Breath=breath;Hand=hand;Look=look;}
    }
    public static class TownCharacterMotion
    {
        public static StreetCharacterPose Sample(float stride,float walking,float facing,float resting)
            =>Sample(stride,walking,facing,resting,Math.Max(0,stride),0);
        public static StreetCharacterPose Sample(float stride,float walking,float facing,float resting,float elapsed,int resident)
        {
            if(!float.IsFinite(stride)||!float.IsFinite(walking))return default;
            walking=Clamp(walking,0,1);facing=Clamp(facing,-1,1);resting=Clamp(resting,0,1);
            // Double intermediates keep even a finite float.MaxValue time finite.
            double time=float.IsFinite(elapsed)&&elapsed>=0?elapsed:0;
            double phase=((resident%6+6)%6)*.617;
            float step=(float)Math.Sin(stride),idle=1-walking;
            float sway=(float)Math.Sin(time*.9+phase)*(.45f+.2f*resting)*idle;
            return new StreetCharacterPose(Math.Abs(step)*2.2f*walking,
                (step*2.2f+facing*.8f)*walking+sway,step*walking,
                (float)Math.Sin(time*1.9+phase)*(.002f+.003f*idle),
                (float)Math.Sin(time*1.3+phase)*.12f*idle,
                Clamp(facing+(float)Math.Sin(time*.55+phase)*.22f*idle,-1,1));
        }
        // Map depth is measured from the painting's top edge, never the viewport.
        public static float DepthScale(float mapDepth)=>.94f+.12f*(float.IsFinite(mapDepth)?Clamp(mapDepth,0,1):.5f);
        public static float Turn(float current,float target,float seconds)
        {
            current=Clamp(current,-1,1);target=Clamp(target,-1,1);
            if(!float.IsFinite(seconds)||seconds<=0)return current;
            float distance=target-current,dt=Math.Min(seconds,.1f);
            if(Math.Abs(distance)<.0001f)return target;
            float eased=distance*(1-(float)Math.Exp(-dt*12));
            return current+Clamp(eased,-dt*6,dt*6);
        }
        public static float BlendMovement(float current,float target,float seconds)
        {
            current=Clamp(current,0,1);target=Clamp(target,0,1);
            if(!float.IsFinite(seconds)||seconds<=0)return current;
            float distance=target-current;
            if(Math.Abs(distance)<.0001f)return target;
            return current+distance*(1-(float)Math.Exp(-Math.Min(seconds,.1f)*(distance>0?10:14)));
        }
        public static StreetCharacterPose Reaction(float remaining,float duration,bool arrival)
        {
            if(!float.IsFinite(remaining)||!float.IsFinite(duration)||duration<=0||remaining<=0||remaining>=duration)return default;
            float progress=1-remaining/duration,envelope=(float)Math.Sin(progress*Math.PI);
            return new StreetCharacterPose(envelope*(arrival?4.6f:8),
                (float)Math.Sin(progress*Math.PI*2)*envelope*(arrival?1.2f:2.8f),0,
                (float)Math.Sin(progress*Math.PI*2)*envelope*.012f,
                (float)Math.Sin(progress*Math.PI*3)*envelope*(arrival?.35f:.85f),0);
        }
        public static StreetCharacterOffset VertexOffset(float u,float v,float stepping,float greeting,float facing)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);stepping=Clamp(stepping,-1,1);greeting=Clamp(greeting,-1,1);facing=Clamp(facing,-1,1);
            float side=Clamp((u-.5f)/.18f,-1,1);
            float footBand=v<.26f?1-v/.26f:0;
            // Cancel the body's rise at the supporting foot. Only the other
            // original foot lifts; neither shoe sinks through the contact shadow.
            float lift=(Math.Max(0,side*stepping)*4.7f-Math.Abs(stepping)*2.2f)*footBand;
            float outer=Clamp((Math.Abs(u-.5f)-.125f)/.25f,0,1);
            float hand=v>=.26f&&v<=.52f?(greeting+stepping*.24f)*side*(float)Math.Sin((v-.26f)/.26f*Math.PI)*outer*1.8f:0;
            float head=v>=.52f?1:Clamp((v-.40f)/.12f,0,1);
            // Translate the head as one piece; retain the atlas UVs and asymmetry.
            float turn=facing*.9f*head*head*(3-2*head);
            return new StreetCharacterOffset(hand+turn+stepping*1.1f*footBand,lift);
        }
        private static float Clamp(float value,float min,float max)=>float.IsFinite(value)?Math.Max(min,Math.Min(max,value)):0;
        public static bool Blink(float elapsed,int resident)
        {
            if(!float.IsFinite(elapsed)||elapsed<0)return false;
            int index=(resident%6+6)%6;
            double period=4.1+index*.17,time=elapsed,cycle=(time+index*.617)%period;
            bool doubleBlink=(Math.Floor(time/period)+index)%3==2;
            return cycle<.1||doubleBlink&&cycle>.22&&cycle<.28;
        }
    }
}
