using System;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class MergeVisualTimingTests
    {
        [Test] public void SelectionSettlesWithoutOvershoot()
        {
            Assert.That(MergeVisualTiming.SelectionScale(0),Is.EqualTo(1));
            Assert.That(MergeVisualTiming.SelectionScale(.06f),Is.InRange(1.01f,1.05f));
            Assert.That(MergeVisualTiming.SelectionScale(.12f),Is.EqualTo(1.06f).Within(.00001));
            for(int i=0;i<120;i++)Assert.That(MergeVisualTiming.SelectionScale(i/60f),Is.InRange(1f,1.06001f));
        }
        [Test] public void GatheringReachesDestinationBeforeUnlock()
        {
            Assert.That(MergeVisualTiming.GatherProgress(0),Is.Zero);
            Assert.That(MergeVisualTiming.GatherProgress(.04f),Is.InRange(.1f,.9f));
            Assert.That(MergeVisualTiming.GatherProgress(.08f),Is.EqualTo(1));
            Assert.That(MergeVisualTiming.GatherProgress(.22f),Is.EqualTo(1));
        }
        [Test] public void SelectionHasVisibleLiftButNeverChangesTheUnlockWindow()
        {
            var method=typeof(MergeVisualTiming).GetMethod("SelectionLift");
            Assert.That(method,Is.Not.Null,"Selection needs a real bounded lift above its dish.");
            var lift=(Func<float,float>)Delegate.CreateDelegate(typeof(Func<float,float>),method);
            Assert.That(lift(0),Is.Zero);Assert.That(lift(.12f),Is.EqualTo(3).Within(.0001));
            foreach(float t in new[]{-1f,float.NaN,float.PositiveInfinity})Assert.That(lift(t),Is.Zero);
            Assert.That(MergeVisualTiming.LockSeconds,Is.EqualTo(.22f));
        }
        [Test] public void ResultTravelsFromMeetingPointToDestinationBeforeUnlock()
        {
            var method=typeof(MergeVisualTiming).GetMethod("LandingProgress");
            Assert.That(method,Is.Not.Null,"Both items meet halfway, then the result lands in the destination cell.");
            var progress=(Func<float,float>)Delegate.CreateDelegate(typeof(Func<float,float>),method);
            Assert.That(progress(.08f),Is.Zero);Assert.That(progress(.15f),Is.InRange(.3f,.7f));
            Assert.That(progress(.22f),Is.EqualTo(1));Assert.That(progress(1),Is.EqualTo(1));
        }
        [Test] public void ResultBouncesAndReturnsToNormalAtExistingUnlockTime()
        {
            Assert.That(MergeVisualTiming.ResultScale(.08f),Is.EqualTo(1));
            Assert.That(MergeVisualTiming.ResultScale(.15f),Is.GreaterThan(1.05f));
            Assert.That(MergeVisualTiming.ResultScale(.22f),Is.EqualTo(1).Within(.00001));
            Assert.That(MergeVisualTiming.ResultScale(.5f),Is.EqualTo(1));
            for(int i=0;i<120;i++)Assert.That(MergeVisualTiming.ResultScale(i/120f),Is.InRange(.99999f,1.10001f));
        }
        [Test] public void LightTailDoesNotExtendTheMergeLockOrRunForever()
        {
            Assert.That(MergeVisualTiming.SparkleAlpha(.04f),Is.Zero);
            Assert.That(MergeVisualTiming.SparkleAlpha(.22f),Is.GreaterThan(0));
            Assert.That(MergeVisualTiming.IsActive(.49f),Is.True);
            Assert.That(MergeVisualTiming.IsActive(.5f),Is.False);
            Assert.That(MergeVisualTiming.SparkleAlpha(.5f),Is.Zero);
            Assert.That(MergeVisualTiming.SparkleAlpha(5),Is.Zero);
        }
        [Test] public void JoiningHasAShortLightFlashWithoutExtendingInputLock()
        {
            var method=typeof(MergeVisualTiming).GetMethod("ImpactAlpha");Assert.That(method,Is.Not.Null);
            var flash=(Func<float,float>)Delegate.CreateDelegate(typeof(Func<float,float>),method);
            Assert.That(flash(.04f),Is.Zero);Assert.That(flash(.08f),Is.GreaterThan(.4f));
            Assert.That(flash(.22f),Is.Zero);Assert.That(flash(float.NaN),Is.Zero);Assert.That(flash(1),Is.Zero);
        }
        [Test] public void GatheringFloatsAboveTheBoardAndLandsBeforeUnlock()
        {
            var method=typeof(MergeVisualTiming).GetMethod("GatherLift");
            Assert.That(method,Is.Not.Null,"The gathered item needs a bounded floating arc.");
            var lift=(Func<float,float>)Delegate.CreateDelegate(typeof(Func<float,float>),method);
            Assert.That(lift(0),Is.Zero);
            Assert.That(lift(MergeVisualTiming.GatherSeconds/2),Is.InRange(.08f,.16f));
            Assert.That(lift(MergeVisualTiming.GatherSeconds),Is.Zero.Within(.00001));
            Assert.That(lift(MergeVisualTiming.LockSeconds),Is.Zero.Within(.00001));
            foreach(float time in new[]{-1f,float.NaN,float.PositiveInfinity,1f})Assert.That(lift(time),Is.Zero);
        }
        [Test] public void ResultSettlesAtGroundBeforeTheSuccessLightEnds()
        {
            var method=typeof(MergeVisualTiming).GetMethod("ResultLift");
            Assert.That(method,Is.Not.Null,"The result must land before normal input resumes.");
            var lift=(Func<float,float>)Delegate.CreateDelegate(typeof(Func<float,float>),method);
            Assert.That(lift(MergeVisualTiming.GatherSeconds),Is.Zero);
            Assert.That(lift((MergeVisualTiming.GatherSeconds+MergeVisualTiming.LockSeconds)/2),Is.InRange(.035f,.075f));
            Assert.That(lift(MergeVisualTiming.LockSeconds),Is.Zero.Within(.00001));
            Assert.That(lift(MergeVisualTiming.LifetimeSeconds),Is.Zero);
            foreach(float time in new[]{-1f,float.NaN,float.NegativeInfinity})Assert.That(lift(time),Is.Zero);
        }
        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void InvalidElapsedTimeCannotLeakScaleOrLight(float elapsed)
        {
            Assert.That(MergeVisualTiming.SelectionScale(elapsed),Is.EqualTo(1));
            Assert.That(MergeVisualTiming.GatherProgress(elapsed),Is.Zero);
            Assert.That(MergeVisualTiming.ResultScale(elapsed),Is.EqualTo(1));
            Assert.That(MergeVisualTiming.SparkleAlpha(elapsed),Is.Zero);
            Assert.That(MergeVisualTiming.IsActive(elapsed),Is.False);
        }
    }
}
