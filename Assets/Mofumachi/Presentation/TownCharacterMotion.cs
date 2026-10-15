using System;
namespace Mofumachi.Presentation
{
    public readonly struct StreetCharacterPose
    {
        public float Hop {get;} public float Lean {get;} public float Foot {get;}
        public StreetCharacterPose(float hop,float lean,float foot){Hop=hop;Lean=lean;Foot=foot;}
    }
    public static class TownCharacterMotion
    {
        public static StreetCharacterPose Sample(float stride,float walking,float facing,float resting)
        {
            if(!float.IsFinite(stride)||!float.IsFinite(walking))return default;
            walking=Math.Max(0,Math.Min(1,walking));facing=float.IsFinite(facing)?Math.Max(-1,Math.Min(1,facing)):0;
            return new StreetCharacterPose(Math.Abs((float)Math.Sin(stride))*3.4f*walking,
                ((float)Math.Sin(stride)*1.6f+facing*.8f)*walking,(float)Math.Sin(stride)*walking);
        }
        public static bool Blink(float elapsed,int resident)
        {if(!float.IsFinite(elapsed)||elapsed<0)return false;return (elapsed+resident*.617f)%4.1f<.11f;}
    }
}
