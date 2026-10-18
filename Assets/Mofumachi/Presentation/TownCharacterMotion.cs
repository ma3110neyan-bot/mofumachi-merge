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
            return new StreetCharacterPose(Math.Abs(step)*3.9f*walking,
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
            float distance=target-current,step=Math.Min(seconds,.1f)*6;
            return Math.Abs(distance)<=step?target:current+Math.Sign(distance)*step;
        }
        public static StreetCharacterOffset VertexOffset(float u,float v,float stepping,float greeting,float facing)
        {
            u=Clamp(u,0,1);v=Clamp(v,0,1);stepping=Clamp(stepping,-1,1);greeting=Clamp(greeting,-1,1);facing=Clamp(facing,-1,1);
            float side=u<.5f?-1:1;
            float lift=v<.26f?side*stepping*2.5f*(1-v/.26f):0;
            float hand=v>=.26f&&v<=.52f?(greeting+stepping*.32f)*side*(float)Math.Sin((v-.26f)/.26f*Math.PI)*Math.Abs(u-.5f)*7:0;
            float head=v>=.52f?1:Clamp((v-.40f)/.12f,0,1);
            // Translate the head as one piece; retain the atlas UVs and asymmetry.
            float turn=facing*.9f*head*head*(3-2*head);
            return new StreetCharacterOffset(hand+turn,lift);
        }
        private static float Clamp(float value,float min,float max)=>float.IsFinite(value)?Math.Max(min,Math.Min(max,value)):0;
        public static bool Blink(float elapsed,int resident)
        {if(!float.IsFinite(elapsed)||elapsed<0)return false;return (elapsed+resident*.617f)%4.1f<.11f;}
    }
}
