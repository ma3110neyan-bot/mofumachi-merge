using System;

namespace Mofumachi.Core
{
    public sealed class TownFrameClock
    {
        public const float StepSeconds=1f/30;
        public const int MaxStepsPerFrame=3;
        private const double Step=1d/30;
        private double remainder;
        public float Alpha=>(float)Math.Min(.999999,Math.Max(0,remainder/Step));
        public int AddElapsed(float elapsedSeconds)
        {
            if(!float.IsFinite(elapsedSeconds)||elapsedSeconds<=0)return 0;
            remainder+=elapsedSeconds;
            double count=Math.Floor((remainder+1e-8)/Step);
            remainder=Math.Max(0,remainder-count*Step);
            return (int)Math.Min(MaxStepsPerFrame,count);
        }
        public void Reset()=>remainder=0;
    }
}
