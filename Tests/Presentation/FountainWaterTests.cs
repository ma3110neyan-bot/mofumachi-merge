using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class FountainWaterTests
    {
        private static (int Count,Func<int,float,(int Kind,TownPoint A,TownPoint B,TownPoint C,TownPoint D,float Alpha)> Sample) Water()
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownFountainWaterLayout");
            Assert.That(type,Is.Not.Null,"Fountain water needs bounded flowing streams, splashes, ripples and glints rather than static artwork alone");
            var sample=(Func<int,float,(int,TownPoint,TownPoint,TownPoint,TownPoint,float)>)Delegate.CreateDelegate(typeof(Func<int,float,(int,TownPoint,TownPoint,TownPoint,TownPoint,float)>),type.GetMethod("Quad"));
            return ((int)type.GetField("QuadCount").GetValue(null),sample);
        }
        [Test] public void FourWaterCategoriesStayWithinTheFountainAndLowOpacityVertexBudget()
        {
            var water=Water();Assert.That(water.Count*4,Is.InRange(4,320));var kinds=new bool[4];
            for(int frame=0;frame<100;frame++)for(int i=0;i<water.Count;i++)
            {
                var q=water.Sample(i,frame*.1f);Assert.That(q.Kind,Is.InRange(0,3));kinds[q.Kind]=true;
                Assert.That(q.Alpha,Is.InRange(0,.42f));
                foreach(var p in new[]{q.A,q.B,q.C,q.D})
                {Assert.That(float.IsFinite(p.X)&&float.IsFinite(p.Y),Is.True);Assert.That(p.X,Is.InRange(.34f,.66f));Assert.That(p.Y,Is.InRange(.31f,.83f));}
            }
            Assert.That(kinds.All(k=>k),Is.True,"All four requested effects must actually emit geometry");
        }
        [Test] public void FlowSplashRippleAndGlintEachChangeTheirVisibleFrame()
        {
            var water=Water();var changed=new bool[4];
            for(int i=0;i<water.Count;i++)
            {
                var a=water.Sample(i,0);var b=water.Sample(i,.31f);
                changed[a.Kind]|=!a.A.Equals(b.A)||!a.B.Equals(b.B)||Math.Abs(a.Alpha-b.Alpha)>.00001f;
            }
            Assert.That(changed.All(k=>k),Is.True,"The four effects must move or change opacity, not just add a static layer");
        }
        [Test] public void TenSecondLoopReturnsToTheSameVisibleWaterFrame()
        {
            var water=Water();
            for(int i=0;i<water.Count;i++)
            {
                var a=water.Sample(i,.31f);var b=water.Sample(i,10.31f);
                Assert.That(a.Kind,Is.EqualTo(b.Kind));Assert.That(a.Alpha,Is.EqualTo(b.Alpha).Within(.00001f));
                foreach(var pair in new[]{(a.A,b.A),(a.B,b.B),(a.C,b.C),(a.D,b.D)})
                {Assert.That(pair.Item1.X,Is.EqualTo(pair.Item2.X).Within(.00001f));Assert.That(pair.Item1.Y,Is.EqualTo(pair.Item2.Y).Within(.00001f));}
            }
        }
        [Test] public void InvalidIndicesAndNonFiniteOrNegativePhaseAreRejected()
        {
            var water=Water();Assert.Throws<ArgumentOutOfRangeException>(()=>water.Sample(-1,0));Assert.Throws<ArgumentOutOfRangeException>(()=>water.Sample(water.Count,0));
            foreach(float phase in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f})Assert.Throws<ArgumentOutOfRangeException>(()=>water.Sample(0,phase));
        }
        [Test] public void AnimatedSamplingDoesNotAllocateManagedMemoryAfterWarmup()
        {
            var water=Water();float sum=0;for(int i=0;i<water.Count;i++)sum+=water.Sample(i,.1f).Alpha;
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int frame=0;frame<500;frame++)for(int i=0;i<water.Count;i++)sum+=water.Sample(i,frame*.01f).Alpha;
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(sum,Is.GreaterThan(0));Assert.That(allocated,Is.Zero,"Water geometry must not create per-frame arrays/objects on Pixel");
        }
    }
}
