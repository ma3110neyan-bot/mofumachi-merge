using System;
namespace Mofumachi.Presentation
{
    // Presentation-only timing; no save, reward or board references.
    public static class MergeVisualTiming
    {
        public const float LockSeconds=.22f,GatherSeconds=.08f,LifetimeSeconds=.5f,SelectionSeconds=.12f;
        private static bool Valid(float elapsed)=>float.IsFinite(elapsed)&&elapsed>=0;
        private static float Smooth(float t){t=Math.Min(1,Math.Max(0,t));return t*t*(3-2*t);}
        public static float SelectionScale(float elapsed)=>Valid(elapsed)?1+.06f*Smooth(elapsed/SelectionSeconds):1;
        public static float GatherProgress(float elapsed)=>Valid(elapsed)?Smooth(elapsed/GatherSeconds):0;
        public static float GatherLift(float elapsed)
        {
            if(!Valid(elapsed)||elapsed<=0||elapsed>=GatherSeconds)return 0;
            return .12f*(float)Math.Sin(GatherProgress(elapsed)*Math.PI);
        }
        public static float ResultLift(float elapsed)
        {
            if(!Valid(elapsed)||elapsed<=GatherSeconds||elapsed>=LockSeconds)return 0;
            return .055f*(float)Math.Sin((elapsed-GatherSeconds)/(LockSeconds-GatherSeconds)*Math.PI);
        }
        public static float ResultScale(float elapsed)
        {
            if(!Valid(elapsed)||elapsed<=GatherSeconds||elapsed>=LockSeconds)return 1;
            return 1+.1f*(float)Math.Sin((elapsed-GatherSeconds)/(LockSeconds-GatherSeconds)*Math.PI);
        }
        public static float SparkleAlpha(float elapsed)
        {
            if(!IsActive(elapsed)||elapsed<=GatherSeconds)return 0;
            return .65f*(float)Math.Sin((elapsed-GatherSeconds)/(LifetimeSeconds-GatherSeconds)*Math.PI);
        }
        public static bool IsActive(float elapsed)=>Valid(elapsed)&&elapsed<LifetimeSeconds;
    }
}
