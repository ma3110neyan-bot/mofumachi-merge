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
