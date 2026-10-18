using System;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Visual.Tests
{
    public sealed class ResidentPresenceTests
    {
        [Test] public void RestingResidentHasSmallSwayWithoutMovingItsFeet()
        {
            var pose=TownCharacterMotion.Sample(.9f,0,0,1);
            Assert.That(Math.Abs(pose.Lean),Is.InRange(.01f,.75f));
            Assert.That(pose.Hop,Is.Zero);Assert.That(pose.Foot,Is.Zero);
        }

        [Test] public void EveryFiniteStrideKeepsMotionFiniteAndWithinTheExistingGaitBounds()
        {
            foreach(float phase in new[]{-float.MaxValue,-1000f,0f,.9f,1000f,float.MaxValue})
            foreach(float walk in new[]{-100f,0f,.25f,1f,100f})
            foreach(float face in new[]{float.NaN,-100f,0f,100f,float.PositiveInfinity})
            {
                var pose=TownCharacterMotion.Sample(phase,walk,face,1);
                Assert.That(float.IsFinite(pose.Hop)&&float.IsFinite(pose.Lean)&&float.IsFinite(pose.Foot),Is.True);
                Assert.That(pose.Hop,Is.InRange(0f,4f));Assert.That(Math.Abs(pose.Lean),Is.LessThanOrEqualTo(3));
                Assert.That(Math.Abs(pose.Foot),Is.LessThanOrEqualTo(1));
            }
        }

        [Test] public void IdleBreathingHandsAndLookingAreDeterministicAndSubtleForAllSixResidents()
        {
            for(int resident=0;resident<6;resident++)for(int frame=0;frame<600;frame++)
            {
                var pose=TownCharacterMotion.Sample(0,0,0,1,frame/30f,resident);
                var same=TownCharacterMotion.Sample(0,0,0,1,frame/30f,resident);
                Assert.That(pose.Hop,Is.Zero);Assert.That(pose.Foot,Is.Zero);
                Assert.That(Math.Abs(pose.Breath),Is.LessThanOrEqualTo(.006f));
                Assert.That(Math.Abs(pose.Hand),Is.LessThanOrEqualTo(.15f));
                Assert.That(Math.Abs(pose.Look),Is.LessThanOrEqualTo(.25f));
                Assert.That(Math.Abs(pose.Lean),Is.LessThanOrEqualTo(.75f));
                Assert.That(pose.Breath,Is.EqualTo(same.Breath));Assert.That(pose.Hand,Is.EqualTo(same.Hand));
            }
            var first=TownCharacterMotion.Sample(0,0,0,1,1,0);
            var other=TownCharacterMotion.Sample(0,0,0,1,1,1);
            Assert.That(first.Breath,Is.Not.Zero.And.Not.EqualTo(other.Breath));
            Assert.That(first.Hand,Is.Not.Zero);Assert.That(first.Look,Is.Not.Zero);
        }

        [Test] public void InvalidIdleInputsCannotPoisonThePose()
        {
            foreach(float time in new[]{float.NaN,float.NegativeInfinity,float.PositiveInfinity,-1f,float.MaxValue})
            {
                var pose=TownCharacterMotion.Sample(.5f,.4f,float.NaN,float.NaN,time,int.MinValue);
                Assert.That(float.IsFinite(pose.Hop)&&float.IsFinite(pose.Lean)&&float.IsFinite(pose.Breath)&&float.IsFinite(pose.Hand)&&float.IsFinite(pose.Look),Is.True);
            }
        }

        [Test] public void DepthOnlyMakesForegroundResidentsModestlyLarger()
        {
            Assert.That(TownCharacterMotion.DepthScale(1),Is.GreaterThan(TownCharacterMotion.DepthScale(0)));
            Assert.That(TownCharacterMotion.DepthScale(.5f),Is.EqualTo(1).Within(.00001));
            foreach(float depth in new[]{-100f,0f,.5f,1f,100f,float.NaN,float.PositiveInfinity})
                Assert.That(TownCharacterMotion.DepthScale(depth),Is.InRange(.94f,1.06f));
        }

        [Test] public void TurnCrossesFacingSmoothlyAndIgnoresInvalidElapsedTime()
        {
            float turn=1;
            for(int frame=0;frame<20;frame++)
            {
                float next=TownCharacterMotion.Turn(turn,-1,1f/60);
                Assert.That(Math.Abs(next-turn),Is.LessThanOrEqualTo(.10001f));
                Assert.That(next,Is.InRange(-1f,1f));turn=next;
            }
            Assert.That(turn,Is.EqualTo(-1).Within(.00001f));
            Assert.That(TownCharacterMotion.Turn(.5f,-1,float.NaN),Is.EqualTo(.5f));
            Assert.That(TownCharacterMotion.Turn(float.NaN,float.PositiveInfinity,.03f),Is.Zero);
        }

        [Test] public void AtlasVertexOffsetsStayFiniteForHostilePoseInputs()
        {
            foreach(float value in new[]{float.NaN,float.NegativeInfinity,-100f,0f,100f,float.PositiveInfinity})
            foreach(float u in new[]{0f,.25f,.5f,.75f,1f})
            foreach(float v in new[]{0f,.12f,.26f,.4f,.52f,.65f,.8f,1f})
            {
                var offset=TownCharacterMotion.VertexOffset(u,v,value,value,value);
                Assert.That(float.IsFinite(offset.X)&&float.IsFinite(offset.Y),Is.True);
                Assert.That(Math.Abs(offset.X),Is.LessThanOrEqualTo(5));
                Assert.That(Math.Abs(offset.Y),Is.LessThanOrEqualTo(2.5f));
            }
        }

        [Test] public void TurningTranslatesTheApprovedHeadWithoutMirroringOrBendingItsFace()
        {
            var left=TownCharacterMotion.VertexOffset(0,.52f,0,0,1);
            var right=TownCharacterMotion.VertexOffset(1,1,0,0,1);
            Assert.That(left.X,Is.GreaterThan(0).And.EqualTo(right.X));
            Assert.That(left.Y,Is.Zero);Assert.That(right.Y,Is.Zero);
            Assert.That(TownCharacterMotion.VertexOffset(.5f,0,0,1,1).X,Is.Zero);
            Assert.That(TownCharacterMotion.VertexOffset(1,.4f,0,1,0).X,Is.GreaterThan(0));
        }
    }
}
