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
            var sample=(Func<int,float,(int,TownPoint,TownPoint,TownPoint,TownPoint,float)>)Delegate.CreateDelegate(typeof(Func<int,float,(int,TownPoint,TownPoint,TownPoint,TownPoint,float)>),type.GetMethod("Quad",new[]{typeof(int),typeof(float)}));
            return ((int)type.GetField("QuadCount").GetValue(null),sample);
        }
        [Test] public void FourWaterCategoriesStayWithinTheFountainAndLowOpacityVertexBudget()
        {
            var water=Water();Assert.That(water.Count*4,Is.InRange(4,784));var kinds=new bool[4];
            for(int frame=0;frame<100;frame++)for(int i=0;i<water.Count;i++)
            {
                var q=water.Sample(i,frame*.1f);Assert.That(q.Kind,Is.InRange(0,3));kinds[q.Kind]=true;
                Assert.That(q.Alpha,Is.InRange(0,.86f));
                foreach(var p in new[]{q.A,q.B,q.C,q.D})
                {Assert.That(float.IsFinite(p.X)&&float.IsFinite(p.Y),Is.True);Assert.That(p.X,Is.InRange(.28f,.72f));Assert.That(p.Y,Is.InRange(.22f,1.30f));if(q.Alpha>.001f)Assert.That(p.Y,Is.LessThanOrEqualTo(.84f),"Normal visible water stays below the causal show-only pixel mask");}
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
        private static Type Layout=>typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownFountainWaterLayout");
        private static Func<float,(int Stage,float Strength,float Glow)> Show()
        {
            var method=Layout.GetMethod("ShowAt",new[]{typeof(float)});Assert.That(method,Is.Not.Null,"A short timed fountain show needs explicit quiet, prelude, show and recovery states");
            return (Func<float,(int,float,float)>)Delegate.CreateDelegate(typeof(Func<float,(int,float,float)>),method);
        }
        [Test] public void DisplayGrowsFourteenPercentAroundTheSamePhysicalDepthWithoutChangingWalkableGeometry()
        {
            var method=Layout.GetMethod("Project");Assert.That(method,Is.Not.Null,"Fountain art and water must share a render-only enlargement");
            var project=(Func<TownPoint,float,TownPoint>)Delegate.CreateDelegate(typeof(Func<TownPoint,float,TownPoint>),method);
            var left=project(new TownPoint(0,0),1.14f);var right=project(new TownPoint(1,1),1.14f);
            Assert.That(right.X-left.X,Is.EqualTo(TownCentralMasterLayout.FountainWidth*1.14f).Within(.000001f));
            Assert.That(right.Y-left.Y,Is.EqualTo(TownCentralMasterLayout.FountainHeight*1.14f).Within(.000001f));
            Assert.That(left.Y,Is.EqualTo(.285f+(.10f-.285f)*1.14f).Within(.000001f));
            Assert.That(TownCentralMasterLayout.FountainRadiusX,Is.EqualTo(.255f));Assert.That(TownCentralMasterLayout.FountainRadiusY,Is.EqualTo(.155f));
        }
        [Test] public void NormalWaterHasContinuousWideLayeredStreamsRatherThanSparseSubpixelDots()
        {
            var water=Water();Assert.That(water.Count,Is.EqualTo(196));
            for(int stream=0;stream<6;stream++)for(int part=0;part<4;part++)
            {
                var outer=water.Sample(stream*8+part*2,.31f);var core=water.Sample(stream*8+part*2+1,.31f);
                Assert.That(outer.Kind,Is.Zero);Assert.That(core.Kind,Is.Zero);
                Assert.That(Math.Abs(outer.A.X-outer.D.X)+Math.Abs(outer.A.Y-outer.D.Y),Is.GreaterThanOrEqualTo(.010f));
                Assert.That(core.Alpha,Is.GreaterThanOrEqualTo(.50f));
            }
        }
        [Test] public void ThreeMinuteCycleHasTwoSecondPreludeSixSecondShowAndOneSecondRecovery()
        {
            var show=Show();Assert.That(show(170.9f).Stage,Is.Zero);Assert.That(show(171).Stage,Is.EqualTo(1));
            Assert.That(show(172).Glow,Is.GreaterThan(.3f));Assert.That(show(173).Stage,Is.EqualTo(2));Assert.That(show(174).Strength,Is.EqualTo(1));
            Assert.That(show(178.9f).Strength,Is.EqualTo(1));Assert.That(show(179).Stage,Is.EqualTo(3));
            Assert.That(show(179.9f).Strength,Is.LessThan(.04f));Assert.That(show(180).Stage,Is.Zero);Assert.That(show(354).Strength,Is.EqualTo(1));
            foreach(float at in new[]{171f,173f,179f,180f})Assert.That(Math.Abs(show(at-.0001f).Strength-show(at+.0001f).Strength),Is.LessThan(.002f),"No abrupt water pop at show boundaries");
        }
        [Test] public void ShowRaisesVisibleJetsButKeepsTheTotalWaterBelowTheExisting1500VertexBudget()
        {
            var method=Layout.GetMethod("Quad",new[]{typeof(int),typeof(float),typeof(float),typeof(float)});Assert.That(method,Is.Not.Null);
            var sample=(Func<int,float,float,float,(int Kind,TownPoint A,TownPoint B,TownPoint C,TownPoint D,float Alpha)>)Delegate.CreateDelegate(typeof(Func<int,float,float,float,(int,TownPoint,TownPoint,TownPoint,TownPoint,float)>),method);
            float peak=0;
            for(int frame=0;frame<30;frame++)for(int i=0;i<196;i++)
            {
                var q=sample(i,frame/30f,1,1);Assert.That(q.Alpha,Is.InRange(0,.86f));
                foreach(var at in new[]{q.A,q.B,q.C,q.D}){Assert.That(at.X,Is.InRange(.28f,.72f));Assert.That(at.Y,Is.InRange(.22f,1.30f));if(q.Alpha>.4f)peak=Math.Max(peak,at.Y);}
            }
            Assert.That(peak,Is.GreaterThan(1.05f));Assert.That(196*4+672,Is.LessThanOrEqualTo(1500));
        }
        [Test] public void NormalStreamHighlightsTravelDownTheWaterRatherThanOnlyChangingBrightness()
        {
            var water=Water();
            for(int stream=0;stream<6;stream++)
            {
                var a=water.Sample(stream*8+1,.10f);var b=water.Sample(stream*8+1,.20f);
                float yA=(a.A.Y+a.B.Y+a.C.Y+a.D.Y)/4,yB=(b.A.Y+b.B.Y+b.C.Y+b.D.Y)/4;
                Assert.That(yA-yB,Is.GreaterThan(.004f),"A bright packet must visibly descend even when the painting is static");
                Assert.That(Math.Min(a.Alpha,b.Alpha),Is.GreaterThanOrEqualTo(.70f));
            }
        }
        [TestCase(4)] [TestCase(8)] [TestCase(15)] [TestCase(30)] [TestCase(60)]
        public void QaShowStartsWithinSixtyRealSecondsEvenAtLowFrameRates(int fps)
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownFountainWaterClock");
            var ctor=type.GetConstructor(new[]{typeof(float)});Assert.That(ctor,Is.Not.Null,"QA needs an explicit short cycle");
            var clock=ctor.Invoke(new object[]{45f});var tick=(Func<float,bool,bool>)Delegate.CreateDelegate(typeof(Func<float,bool,bool>),clock,type.GetMethod("Advance"));
            var elapsed=(Func<float>)Delegate.CreateDelegate(typeof(Func<float>),clock,type.GetProperty("Elapsed").GetMethod);
            var method=Layout.GetMethod("ShowAt",new[]{typeof(float),typeof(float)});Assert.That(method,Is.Not.Null);
            var show=(Func<float,float,(int Stage,float Strength,float Glow)>)Delegate.CreateDelegate(typeof(Func<float,float,(int,float,float)>),method);
            float first=-1;bool recovered=false;
            for(int frame=0;frame<60*fps;frame++)
            {
                tick(1f/fps,true);int stage=show(elapsed(),45).Stage;
                if(stage==2&&first<0)first=(frame+1f)/fps;
                if(first>0&&stage==0)recovered=true;
            }
            Assert.That(first,Is.InRange(30,60));Assert.That(first,Is.EqualTo(38).Within(1f/fps+.04f));Assert.That(recovered,Is.True);
        }
        [Test] public void NormalWaterCarriesLongerBrightPacketsOverAVisibleBlueBody()
        {
            // A short upper-tier fall is only about ten phone pixels tall.
            // Sub-two-pixel packets over a faint body disappear into the art.
            var water=Water();
            for(int stream=0;stream<6;stream++)
            {
                var outer=water.Sample(stream*8,.05f);
                var core=water.Sample(stream*8+1,.05f);
                Assert.That(outer.Alpha,Is.GreaterThanOrEqualTo(.45f),"The cyan body must remain visible behind the moving white packet");
                Assert.That(Math.Sqrt(Math.Pow(core.B.X-core.A.X,2)+Math.Pow(core.B.Y-core.A.Y,2)),Is.GreaterThan(.014f),"The shortest upper fall needs a perceptible moving packet");
                Assert.That(Math.Sqrt(Math.Pow(core.D.X-core.A.X,2)+Math.Pow(core.D.Y-core.A.Y,2)),Is.GreaterThan(.012f),"The highlight must survive phone-scale rasterization");
            }
        }
        [Test] public void ShowHighlightsTravelDownTheReturningJetsRatherThanOnlyWobblingInPlace()
        {
            for(int jet=0;jet<3;jet++)
            {
                int index=136+jet*16+9;
                var a=TownFountainWaterLayout.Quad(index,.1f,1,1);
                var b=TownFountainWaterLayout.Quad(index,.2f,1,1);
                float yA=(a.A.Y+a.B.Y+a.C.Y+a.D.Y)/4,yB=(b.A.Y+b.B.Y+b.C.Y+b.D.Y)/4;
                Assert.That(yA-yB,Is.GreaterThan(.02f),"A white packet must travel toward the basin, rather than brighten one fixed wire segment");
                Assert.That(Math.Min(a.Alpha,b.Alpha),Is.GreaterThan(.65f));
            }
        }
        [Test] public void ShowClockCountsSlowActiveFramesButNeverCountsPausedTime()
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownFountainWaterClock");Assert.That(type,Is.Not.Null);
            var clock=Activator.CreateInstance(type);var tick=(Func<float,bool,bool>)Delegate.CreateDelegate(typeof(Func<float,bool,bool>),clock,type.GetMethod("Advance"));
            var elapsed=(Func<float>)Delegate.CreateDelegate(typeof(Func<float>),clock,type.GetProperty("Elapsed").GetMethod);
            tick(600,false);Assert.That(elapsed(),Is.Zero);tick(.25f,true);Assert.That(elapsed(),Is.EqualTo(7f/30).Within(.00001f));
            foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f,0f})tick(invalid,true);
            Assert.That(elapsed(),Is.EqualTo(7f/30).Within(.00001f));tick(600,false);Assert.That(elapsed(),Is.EqualTo(7f/30).Within(.00001f));
            for(int i=0;i<719;i++)tick(.25f,true);Assert.That(elapsed(),Is.EqualTo(0).Within(.00001f),"Exactly180 active real seconds completes one cycle at4FPS");
        }
    }
}
