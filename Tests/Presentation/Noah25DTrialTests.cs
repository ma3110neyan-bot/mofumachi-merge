using System;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Visual.Tests
{
    public sealed class Noah25DTrialTests
    {
        [Test] public void PresentationTrialCanOnlyEnableTheOneFormalNoah()
        {
            foreach(string id in new[]{"momo","lulu","pom","fio","miel","noah",null,"","Noah"})
            foreach(bool requested in new[]{false,true})
                Assert.That(TownNoah25DTrial.IsEnabled(id,requested),Is.EqualTo(id=="noah"&&requested),id??"null");
        }

        [Test] public void WalkingPlantsTheActualPawsAfterLeanBreathAndDepthTransform()
        {
            foreach(float phase in new[]{.2f,.8f,1.5f,2.8f,3.4f,4.5f,5.8f})
            foreach(float walk in new[]{.2f,.6f,1f})
            foreach(float depth in new[]{.94f,1f,1.06f})
            {
                var pose=TownNoah25DTrial.Sample(phase,walk,1,0,2,0,false,true);
                Assert.That(pose.Hop,Is.GreaterThan(0),"The trial must articulate a real walking step.");
                float sx=depth*(1-pose.Breath*.35f),sy=depth*(1+pose.Breath);
                var root=TownNoah25DTrial.SupportTranslation(61.33746f,pose,sx,sy);
                var feet=new float[2];int i=0;
                foreach(float u in new[]{.303f,.535f})
                {
                    var offset=TownNoah25DTrial.VertexOffset(u,0,pose);
                    double radians=pose.Lean*Math.PI/180;
                    double x=(61.33746f*(u-.419f)+offset.X)*sx,y=offset.Y*sy;
                    feet[i++]=(float)(x*Math.Sin(radians)+y*Math.Cos(radians))+root.Y;
                }
                Assert.That(Math.Min(feet[0],feet[1]),Is.EqualTo(0).Within(.00001));
                Assert.That(Math.Max(feet[0],feet[1]),Is.GreaterThan(.01f).And.LessThan(4.1f));
            }
        }

        [Test] public void RigidFaceAndTailRetainTheirShapeWhileTheOriginalPawsMove()
        {
            var pose=TownNoah25DTrial.Sample(1,1,1,0,2,.325f,false,true);
            var first=TownNoah25DTrial.VertexOffset(.1f,.49f,pose);
            foreach(float u in new[]{0f,.2f,.5f,.8f,1f})
            foreach(float v in new[]{.49f,.67f,1f})
            {
                var face=TownNoah25DTrial.VertexOffset(u,v,pose);
                Assert.That(face.X,Is.EqualTo(first.X));Assert.That(face.Y,Is.Zero);
            }
            Assert.That(first.X,Is.GreaterThan(0).And.LessThanOrEqualTo(.6f));
            foreach(float u in new[]{.7f,.85f,1f})
            foreach(float v in new[]{0f,.06f,.14f,.2f,.36f,.44f})
            {
                var tail=TownNoah25DTrial.VertexOffset(u,v,pose);
                Assert.That(tail.X,Is.Zero);Assert.That(tail.Y,Is.Zero);
            }
            Assert.That(TownNoah25DTrial.VertexOffset(.18f,.36f,pose).X,Is.Not.Zero);
        }

        [Test] public void TalkAndStartStopHaveSmallDistinctPosesWithoutChangingTheOriginalArt()
        {
            var idle=TownNoah25DTrial.Sample(0,0,1,0,1,0,false,false);
            var talk=TownNoah25DTrial.Sample(0,0,1,0,1,.325f,false,false);
            Assert.That(talk.Hop-idle.Hop,Is.GreaterThan(1.5f).And.LessThanOrEqualTo(3.3f));
            Assert.That(Math.Abs(talk.Hand-idle.Hand),Is.GreaterThan(.1f));
            var start=TownNoah25DTrial.Sample(0,.5f,1,0,1,0,false,true);
            var stop=TownNoah25DTrial.Sample(0,.5f,1,0,1,0,false,false);
            Assert.That(start.Lean,Is.EqualTo(stop.Lean).Within(.00001),"Changing the moving flag cannot snap a blended pose.");
            Assert.That(start.Breath,Is.EqualTo(stop.Breath).Within(.00001));
            float walking=.5f;
            var previous=start;
            for(int frame=0;frame<40;frame++)
            {
                walking=TownCharacterMotion.BlendMovement(walking,frame<20?0:1,1f/60);
                var next=TownNoah25DTrial.Sample(0,walking,1,0,1,0,false,frame>=20);
                Assert.That(Math.Abs(next.Lean-previous.Lean),Is.LessThan(.15f));
                Assert.That(Math.Abs(next.Breath-previous.Breath),Is.LessThan(.0003f));previous=next;
            }
            var settled=TownNoah25DTrial.Sample(0,0,1,0,1,0,false,false);
            Assert.That(settled.Hop,Is.Zero);Assert.That(settled.Foot,Is.Zero);
            Assert.That(TownNoah25DTrial.Sample(0,0,1,0,1,.65f,false,false).Hop,Is.Zero);
        }

        [Test] public void BlinkSamplesOnlyTheTwoRegisteredEyesFromTheFormalSmile()
        {
            var left=TownNoah25DTrial.BlinkUvOffset(.26f,.60f);
            var right=TownNoah25DTrial.BlinkUvOffset(.56f,.59f);
            Assert.That(left.X,Is.Zero);Assert.That(left.Y,Is.EqualTo(-336f/1024f).Within(.000001));
            Assert.That(right.X,Is.EqualTo(-2f/1536f).Within(.000001));
            Assert.That(right.Y,Is.EqualTo(-339f/1024f).Within(.000001));
            foreach(var point in new[]{new[]{.42f,.6f},new[]{.3f,.8f},new[]{.3f,.4f},new[]{.8f,.6f}})
            {
                var uv=TownNoah25DTrial.BlinkUvOffset(point[0],point[1]);
                Assert.That(uv.X,Is.Zero);Assert.That(uv.Y,Is.Zero);
            }
        }

        [Test] public void InvalidAndLongRunningInputsStayFiniteAndInsideSubtleMotionBounds()
        {
            foreach(float value in new[]{float.NaN,float.NegativeInfinity,-float.MaxValue,-1f,0f,1f,float.MaxValue,float.PositiveInfinity})
            {
                var pose=TownNoah25DTrial.Sample(value,value,value,value,value,value,false,true);
                foreach(float part in new[]{pose.Hop,pose.Lean,pose.Foot,pose.Breath,pose.Hand,pose.Look,pose.Travel})
                    Assert.That(float.IsFinite(part),Is.True);
                Assert.That(pose.Hop,Is.InRange(0f,5f));Assert.That(Math.Abs(pose.Lean),Is.LessThanOrEqualTo(2));
                Assert.That(Math.Abs(pose.Breath),Is.LessThanOrEqualTo(.005f));Assert.That(Math.Abs(pose.Hand),Is.LessThanOrEqualTo(1));
                var offset=TownNoah25DTrial.VertexOffset(value,value,pose);
                Assert.That(float.IsFinite(offset.X)&&float.IsFinite(offset.Y),Is.True);
                var root=TownNoah25DTrial.SupportTranslation(value,pose,value,value);
                Assert.That(float.IsFinite(root.X)&&float.IsFinite(root.Y),Is.True);
            }
        }
    }
}
