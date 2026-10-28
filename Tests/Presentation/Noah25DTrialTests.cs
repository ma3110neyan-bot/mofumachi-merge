using System;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Visual.Tests
{
    public sealed class Noah25DTrialTests
    {
        [Test] public void GreetingPawTraversesBothSidesOfItsRestPoseInsteadOfOnlyTrembling()
        {
            float min=100,max=-100;
            var idle=TownNoah25DTrial.Sample(0,0,0,0,0,0,false,false);
            var rest=TownNoah25DTrial.VertexOffset(.16f,.43f,idle);
            for(int i=1;i<60;i++)
            {
                var pose=TownNoah25DTrial.Sample(0,0,0,0,0,i/60f,false,false);
                float x=TownNoah25DTrial.VertexOffset(.16f,.43f,pose).X-rest.X;
                min=Math.Min(min,x);max=Math.Max(max,x);
            }
            Assert.That(min,Is.LessThan(-2.5f));Assert.That(max,Is.GreaterThan(2.5f));
            Assert.That(max-min,Is.GreaterThan(6.5f),"The raised original paw must make a readable return arc at town scale.");
        }

        [Test] public void ReliefHasAFaceAndBodyVolumeButNeverDisplacesTheFeetOrOuterTail()
        {
            Assert.That(TownNoah25DTrial.SurfaceDepth(.40f,.32f),Is.LessThan(-5));
            Assert.That(TownNoah25DTrial.SurfaceDepth(.36f,.64f),Is.LessThan(-3));
            foreach(float u in new[]{0f,.303f,.535f,1f})Assert.That(TownNoah25DTrial.SurfaceDepth(u,0),Is.Zero);
            foreach(float v in new[]{0f,.3f,.6f,.9f,1f})Assert.That(TownNoah25DTrial.SurfaceDepth(.85f,v),Is.Zero);
            for(int y=0;y<=20;y++)for(int x=0;x<=20;x++)
                Assert.That(TownNoah25DTrial.SurfaceDepth(x/20f,y/20f),Is.InRange(-9f,0f));
        }

        [Test] public void NeutralLightRevealsRoundnessWithoutBlackeningOriginalArt()
        {
            var pose=TownNoah25DTrial.Sample(0,0,0,0,2,0,false,false);
            float center=TownNoah25DTrial.SurfaceLight(.34f,.66f,pose);
            Assert.That(center-TownNoah25DTrial.SurfaceLight(.59f,.66f,pose),Is.GreaterThan(.07f));
            Assert.That(TownNoah25DTrial.SurfaceLight(.85f,.65f,pose),Is.EqualTo(1),"Outer tail keeps its baked original light.");
            for(int y=0;y<=20;y++)for(int x=0;x<=20;x++)
                Assert.That(TownNoah25DTrial.SurfaceLight(x/20f,y/20f,pose),Is.InRange(.78f,1f));
        }

        [Test] public void ReliefAndYawKeepEveryProjectedMeshTriangleFacingForward()
        {
            float[] xs={0,.06f,.16f,.18f,.30f,.303f,.37f,.42f,.47f,.53f,.535f,.625f,.65f,.70f,.84f,1};
            float[] ys={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
            foreach(float direction in new[]{-1f,0f,1f})foreach(float walk in new[]{0f,.5f,1f})
            for(int i=0;i<=30;i++)
            {
                var pose=TownNoah25DTrial.Sample(i*.21f,walk,direction,0,i*.1f,i/30f,false,walk>0);
                StreetCharacterOffset Project(float u,float v)
                {
                    var d=TownNoah25DTrial.VertexOffset(u,v,pose);double yaw=-pose.Look*18*Math.PI/180;
                    return new StreetCharacterOffset((float)((61.33746f*u+d.X)*Math.Cos(yaw)+TownNoah25DTrial.SurfaceDepth(u,v)*Math.Sin(yaw)),78*v+d.Y);
                }
                float Area(StreetCharacterOffset a,StreetCharacterOffset b,StreetCharacterOffset c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
                for(int y=0;y<ys.Length-1;y++)for(int x=0;x<xs.Length-1;x++)
                {
                    var a=Project(xs[x],ys[y]);var b=Project(xs[x+1],ys[y]);var c=Project(xs[x+1],ys[y+1]);var d=Project(xs[x],ys[y+1]);
                    Assert.That(Area(a,b,c),Is.GreaterThan(.02f));Assert.That(Area(a,c,d),Is.GreaterThan(.02f));
                }
            }
        }
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
                    double x=(61.33746f*(u-.419f)+offset.X)*sx*Math.Cos(pose.Look*18*Math.PI/180),y=offset.Y*sy;
                    feet[i++]=(float)(x*Math.Sin(radians)+y*Math.Cos(radians))+root.Y;
                }
                Assert.That(Math.Min(feet[0],feet[1]),Is.EqualTo(0).Within(.00001));
                Assert.That(Math.Max(feet[0],feet[1]),Is.GreaterThan(.01f).And.LessThan(6.5f));
            }
        }

        [Test] public void RigidFaceAndTailRetainTheirShapeWhileTheOriginalPawsMove()
        {
            var pose=TownNoah25DTrial.Sample(1,1,1,0,2,.325f,false,true);
            var first=TownNoah25DTrial.VertexOffset(.1f,.49f,pose);
            foreach(float u in new[]{0f,.2f,.5f,.65f})
            foreach(float v in new[]{.49f,.67f,1f})
            {
                var face=TownNoah25DTrial.VertexOffset(u,v,pose);
                Assert.That(face.X,Is.EqualTo(first.X));Assert.That(face.Y,Is.Zero);
            }
            Assert.That(first.X,Is.Zero);
            foreach(float u in new[]{.7f,.85f,1f})
            foreach(float v in new[]{0f,.06f,.14f,.2f,.36f,.44f})
            {
                var tail=TownNoah25DTrial.VertexOffset(u,v,pose);
                Assert.That(tail.X,Is.Zero);Assert.That(tail.Y,Is.Zero);
            }
            foreach(float u in new[]{.85f,.95f,1f})
            foreach(float v in new[]{.45f,.49f,.53f,.60f})
            {
                var tail=TownNoah25DTrial.VertexOffset(u,v,pose);
                Assert.That(tail.X,Is.Zero);Assert.That(tail.Y,Is.Zero);
            }
            // Opaque upper curl in the formal atlas, including every corner
            // of the actual .70/.84/1 by .80/1 mesh cells, not alpha gutter.
            foreach(float u in new[]{.70f,.75f,.78f,.82f,.84f,.85f,.90f,.95f,1f})
            foreach(float v in new[]{.80f,.85f,.90f,.92f,.95f,1f})
            {var curl=TownNoah25DTrial.VertexOffset(u,v,pose);Assert.That(curl.X,Is.Zero);Assert.That(curl.Y,Is.Zero);}
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
            Assert.That(TownNoah25DTrial.Sample(0,0,1,0,1,1f,false,false).Hop,Is.Zero);
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

        [Test] public void StepsAndRaisedPawAreReadableAtTheExistingTownArtSize()
        {
            var walk=TownNoah25DTrial.Sample((float)Math.PI/2,1,1,0,2,0,false,true);
            var left=TownNoah25DTrial.VertexOffset(.303f,0,walk);
            var right=TownNoah25DTrial.VertexOffset(.535f,0,walk);
            Assert.That(Math.Abs(right.Y-left.Y),Is.InRange(4.8f,6.2f),"Alternating paws need a visible stride, without moving navigation feet.");
            var idle=TownNoah25DTrial.Sample(0,0,1,0,2,0,false,false);
            float maxHandTravel=0;
            for(int i=1;i<30;i++)
            {
                var greet=TownNoah25DTrial.Sample(0,0,1,0,2,i/30f,false,false);
                var a=TownNoah25DTrial.VertexOffset(.16f,.43f,idle);
                var b=TownNoah25DTrial.VertexOffset(.16f,.43f,greet);
                maxHandTravel=Math.Max(maxHandTravel,(float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y)));
            }
            Assert.That(maxHandTravel,Is.InRange(2.5f,7f),"A greeting must move the original raised paw, not just wobble the entire sprite.");
        }

        [Test] public void FacingBothWaysMovesTheRigidHeadWithoutMirroringItsOriginalFeatures()
        {
            var left=TownNoah25DTrial.Sample(0,0,-1,0,1,0,false,false);
            var right=TownNoah25DTrial.Sample(0,0,1,0,1,0,false,false);
            float HeadX(Noah25DTrialPose pose)
            {
                float sx=(1-pose.Breath*.35f)*(1-.035f*Math.Abs(pose.Look)),sy=1+pose.Breath;
                var root=TownNoah25DTrial.SupportTranslation(61.33746f,pose,sx,sy);
                double angle=pose.Lean*Math.PI/180;
                double x=(61.33746f*(.30f-.419f))*sx*Math.Cos(pose.Look*18*Math.PI/180)-TownNoah25DTrial.SurfaceDepth(.30f,.80f)*Math.Sin(pose.Look*18*Math.PI/180);
                return root.X+(float)(x*Math.Cos(angle)-78*.80f*sy*Math.Sin(angle));
            }
            Assert.That(HeadX(right)-HeadX(left),Is.InRange(6f,10f),"Yaw/parallax plus planted lean must be readable without mirroring the original face.");
            foreach(float u in new[]{.10f,.30f,.50f,.65f,.80f})
            {
                var face=TownNoah25DTrial.VertexOffset(u,.80f,right);
                Assert.That(face.X,Is.Zero);Assert.That(face.Y,Is.Zero);
            }
        }

        [Test] public void DirectionAndMidStrideStopsAreContinuousAndCompletelySettle()
        {
            float blend=1,facing=1;
            var previous=TownNoah25DTrial.Sample(1.2f,blend,facing,0,2,0,false,true);
            for(int i=0;i<120;i++)
            {
                blend=TownCharacterMotion.BlendMovement(blend,0,1f/60);
                facing=TownCharacterMotion.Turn(facing,-1,1f/60);
                var next=TownNoah25DTrial.Sample(1.2f,blend,facing,0,2,0,false,false);
                var a=TownNoah25DTrial.VertexOffset(.535f,0,previous);
                var b=TownNoah25DTrial.VertexOffset(.535f,0,next);
                Assert.That(Math.Abs(b.Y-a.Y),Is.LessThan(.65f));
                Assert.That(Math.Abs(next.Look-previous.Look),Is.LessThan(.11f));previous=next;
            }
            Assert.That(Math.Abs(previous.Foot),Is.LessThan(.0001f));
            Assert.That(previous.Look,Is.EqualTo(-1).Within(.001));
        }

        [Test] public void GreetingHasOneBriefFormalSmileAndReturnsToOpenEyes()
        {
            Assert.That(TownNoah25DTrial.SmilingEyes(0),Is.False);
            Assert.That(TownNoah25DTrial.SmilingEyes(1),Is.False);
            Assert.That(TownNoah25DTrial.SmilingEyes(.50f),Is.True);
            Assert.That(TownNoah25DTrial.SmilingEyes(.10f),Is.False);
            foreach(float input in new[]{float.NaN,float.PositiveInfinity,-.5f})
                Assert.That(TownNoah25DTrial.SmilingEyes(input),Is.False);
        }

        [Test] public void RegisteredHandAndFootMeshNeverFoldsDuringAFullGaitOrGreeting()
        {
            float[] xs={0,.06f,.16f,.18f,.30f,.37f,.42f,.47f,.53f,.625f,.65f,.70f,.84f,1};
            float[] ys={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
            for(int sample=0;sample<=60;sample++)
            foreach(float blend in new[]{0f,.5f,1f})
            foreach(float direction in new[]{-1f,1f})
            foreach(bool reacting in new[]{false,true})
            {
                var pose=TownNoah25DTrial.Sample(sample*.11f,blend,direction,0,2,reacting?sample/60f:0,false,blend>0);
                StreetCharacterOffset Point(float x,float y)
                {var o=TownNoah25DTrial.VertexOffset(x,y,pose);return new StreetCharacterOffset(61.33746f*x+o.X,78*y+o.Y);}
                float Area(StreetCharacterOffset a,StreetCharacterOffset b,StreetCharacterOffset c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
                for(int y=0;y<ys.Length-1;y++)for(int x=0;x<xs.Length-1;x++)
                {
                    var a=Point(xs[x],ys[y]);var b=Point(xs[x+1],ys[y]);var c=Point(xs[x+1],ys[y+1]);var d=Point(xs[x],ys[y+1]);
                    Assert.That(Area(a,b,c),Is.GreaterThan(.02f),$"Front triangle folded at sample{sample}, cell{x}/{y}");
                    Assert.That(Area(a,c,d),Is.GreaterThan(.02f),$"Back triangle folded at sample{sample}, cell{x}/{y}");
                }
            }
        }

        [Test] public void InvalidAndLongRunningInputsStayFiniteAndInsideSubtleMotionBounds()
        {
            foreach(float value in new[]{float.NaN,float.NegativeInfinity,-float.MaxValue,-1f,0f,1f,float.MaxValue,float.PositiveInfinity})
            {
                var pose=TownNoah25DTrial.Sample(value,value,value,value,value,value,false,true);
                foreach(float part in new[]{pose.Hop,pose.Lean,pose.Foot,pose.Breath,pose.Hand,pose.Look,pose.Travel})
                    Assert.That(float.IsFinite(part),Is.True);
                Assert.That(pose.Hop,Is.InRange(0f,5f));Assert.That(Math.Abs(pose.Lean),Is.LessThanOrEqualTo(4.2f));
                Assert.That(Math.Abs(pose.Breath),Is.LessThanOrEqualTo(.01f));Assert.That(Math.Abs(pose.Hand),Is.LessThanOrEqualTo(1));
                var offset=TownNoah25DTrial.VertexOffset(value,value,pose);
                Assert.That(float.IsFinite(offset.X)&&float.IsFinite(offset.Y),Is.True);
                var root=TownNoah25DTrial.SupportTranslation(value,pose,value,value);
                Assert.That(float.IsFinite(root.X)&&float.IsFinite(root.Y),Is.True);
            }
        }
    }
}
