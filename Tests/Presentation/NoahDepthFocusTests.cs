using System;
using NUnit.Framework;
using Mofumachi.Presentation;

namespace Mofumachi.Visual.Tests
{
    public sealed class NoahDepthFocusTests
    {
        private const float Width=61.33746f,Height=78;
        private static NoahDepthFocusPose Pose(float phase=0,float walk=0,float look=0,float head=0,float forward=0,float reaction=0)
            =>TownNoahDepthFocus.Pose(TownNoah25DTrial.Sample(phase,walk,look,0,2,reaction,false,walk>0),phase,walk,head,forward,reaction);
        private static NoahDepthVertex V(float u,float v,NoahDepthFocusPose p)=>TownNoahDepthFocus.Vertex(u,v,Width,Height,p);
        private static double Length(NoahDepthVertex a,NoahDepthVertex b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y)+(a.Z-b.Z)*(a.Z-b.Z));
        // Independent application of Ry then Rx then Rz, matching Unity's
        // right-to-left quaternion convention, not the production helper.
        private static NoahDepthVertex Reference(NoahDepthVertex p,NoahDepthFocusPose pose,float sx=1,float sy=1,float sz=1)
        {
            double y=pose.Yaw*Math.PI/180,x=pose.Pitch*Math.PI/180,z=pose.Roll*Math.PI/180;
            double a=p.X*sx*Math.Cos(y)+p.Z*sz*Math.Sin(y),c=-p.X*sx*Math.Sin(y)+p.Z*sz*Math.Cos(y);
            double b=p.Y*sy*Math.Cos(x)-c*Math.Sin(x),depth=p.Y*sy*Math.Sin(x)+c*Math.Cos(x);
            return new NoahDepthVertex((float)(a*Math.Cos(z)-b*Math.Sin(z)),(float)(a*Math.Sin(z)+b*Math.Cos(z)),(float)depth,1);
        }
        [Test] public void HeadAndEyesMoveAsOneRigidPieceAndOuterTailNeverWarps()
        {
            foreach(float look in new[]{-1f,0,1})foreach(float forward in new[]{-1f,0,1})
            {
                var p=Pose(1,1,look,look,forward,.4f);
                var a=V(.20f,.60f,p);var b=V(.60f,.60f,p);var c=V(.36f,.80f,p);
                Assert.That(Length(a,b),Is.EqualTo(Width*.40).Within(.00002));
                Assert.That(Length(a,c),Is.EqualTo(Math.Sqrt(Math.Pow(Width*.16,2)+Math.Pow(Height*.20,2))).Within(.00002));
                foreach(float u in new[]{.70f,.84f,1})foreach(float v in new[]{.2f,.8f,1})
                {var t=V(u,v,p);Assert.That(t.X,Is.EqualTo(Width*(u-.419f)).Within(.00001));Assert.That(t.Y,Is.EqualTo(Height*v));Assert.That(t.Z,Is.Zero);Assert.That(t.Light,Is.EqualTo(1));}
            }
        }
        [Test] public void FaceIsInFrontOfTorsoAndTurnsWithReadableRelativeParallax()
        {
            var neutral=Pose();Assert.That(V(.36f,.67f,neutral).Z,Is.LessThan(V(.4f,.32f,neutral).Z-2));
            float Relative(float look)
            {var p=Pose(0,0,look,look);return Reference(V(.36f,.67f,p),p).X-Reference(V(.4f,.32f,p),p).X;}
            Assert.That(Math.Abs(Relative(1)-Relative(-1)),Is.GreaterThan(3.2f),"At2x town scale the face must move visibly relative to the torso, not just tilt the entire card.");
        }
        [Test] public void ArmsAndFeetHaveOppositeDepthPhaseWithoutNewReactionTiming()
        {
            var a=Pose((float)Math.PI/2,1,1,1);var b=Pose(-(float)Math.PI/2,1,1,1);
            float arm=V(.16f,.43f,a).Z-V(.16f,.43f,b).Z,foot=V(.303f,0,a).Z-V(.303f,0,b).Z;
            Assert.That(Math.Abs(arm),Is.GreaterThan(1));Assert.That(Math.Abs(foot),Is.GreaterThan(1));Assert.That(arm*foot,Is.LessThan(0));
            Assert.That(Pose(0,0,1,1,0,.4f).Nod,Is.GreaterThan(1));Assert.That(Pose().Nod,Is.Zero);
        }
        [Test] public void ActualSupportPawStaysOnFloorAfterAllThreeRotations()
        {
            foreach(float look in new[]{-1f,0,1})foreach(float forward in new[]{-1f,0,1})foreach(float scale in new[]{.94f,1.06f})
            for(int i=0;i<30;i++)
            {
                var p=Pose(i*.22f,1,look,look,forward,i/30f);float sx=scale*(1-p.Motion.Breath*.35f),sy=scale*(1+p.Motion.Breath);
                var root=TownNoahDepthFocus.Support(Width,Height,p,sx,sy,scale);
                var a=Reference(V(.303f,0,p),p,sx,sy,scale);var b=Reference(V(.535f,0,p),p,sx,sy,scale);
                Assert.That(Math.Min(a.Y,b.Y)+root.Y,Is.EqualTo(0).Within(.00002));
                Assert.That(Math.Max(a.Y,b.Y)+root.Y,Is.LessThan(8),"No jumping away from the floor to imitate volume.");
                var actual=TownNoahDepthFocus.Project(V(.36f,.67f,p),p,sx,sy,scale);var expected=Reference(V(.36f,.67f,p),p,sx,sy,scale);
                Assert.That(actual.X,Is.EqualTo(expected.X).Within(.00002));Assert.That(actual.Y,Is.EqualTo(expected.Y).Within(.00002));Assert.That(actual.Z,Is.EqualTo(expected.Z).Within(.00002));
            }
        }
        [Test] public void ProjectedRegisteredGridNeverFoldsDuringTurnsAndFrontBackGait()
        {
            float[] xs={0,.06f,.16f,.18f,.30f,.303f,.37f,.42f,.47f,.53f,.535f,.625f,.65f,.70f,.84f,1};
            float[] ys={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
            foreach(float look in new[]{-1f,0,1})foreach(float head in new[]{-1f,0,1})foreach(float forward in new[]{-1f,0,1})
            foreach(float walk in new[]{0f,.5f,1})for(int sample=0;sample<12;sample++)
            {
                var pose=Pose(sample*.55f,walk,look,head,forward,sample/12f);
                NoahDepthVertex P(float u,float v)=>Reference(V(u,v,pose),pose);
                float Area(NoahDepthVertex a,NoahDepthVertex b,NoahDepthVertex c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
                for(int y=0;y<ys.Length-1;y++)for(int x=0;x<xs.Length-1;x++)
                {
                    var a=P(xs[x],ys[y]);var b=P(xs[x+1],ys[y]);var c=P(xs[x+1],ys[y+1]);var d=P(xs[x],ys[y+1]);
                    Assert.That(Area(a,b,c),Is.GreaterThan(.015f),$"Fold front {look}/{head}/{forward}/{walk}/{sample}/{x}/{y}");
                    Assert.That(Area(a,c,d),Is.GreaterThan(.015f),$"Fold back {look}/{head}/{forward}/{walk}/{sample}/{x}/{y}");
                }
            }
        }
        [Test] public void ShadowIsReadableBesideThePawsAboveTheNameCard()
        {
            float before=TownNoahDepthFocus.GroundAlpha(14,3,62,78,false),after=TownNoahDepthFocus.GroundAlpha(14,3,62,78,true);
            Assert.That(after,Is.GreaterThan(.15f));Assert.That(after-before,Is.GreaterThan(.12f));
            Assert.That(TownNoahDepthFocus.GroundAlpha(100,3,62,78,true),Is.Zero);
        }
        [Test] public void StopReturnsDepthSwayToRestAndNeutralLightingKeepsItsBounds()
        {
            var rest=Pose(1,0,1,1,1);Assert.That(rest.Pitch,Is.Zero);Assert.That(rest.Sway,Is.Zero);
            foreach(float direction in new[]{-1f,0,1})for(int y=0;y<=16;y++)for(int x=0;x<=16;x++)
            {var v=V(x/16f,y/16f,Pose(1,1,direction,direction,direction,.4f));Assert.That(v.Light,Is.InRange(.80f,1));}
        }
        [Test] public void DefaultCachedPoseStillProjectsAnIdentityFrame()
        {
            var v=TownNoahDepthFocus.Project(new NoahDepthVertex(3,7,-2,1),default,1,1,1);
            Assert.That(v.X,Is.EqualTo(3));Assert.That(v.Y,Is.EqualTo(7));Assert.That(v.Z,Is.EqualTo(-2));
        }
        [Test] public void FrontBackMotionUsesDepthInsteadOfKeepingTheLastSidewaysYaw()
        {
            var side=Pose((float)Math.PI/2,1,1,1,0);var away=Pose((float)Math.PI/2,1,1,.35f,1);var toward=Pose((float)Math.PI/2,1,1,.35f,-1);
            Assert.That(Math.Abs(away.Yaw),Is.LessThan(Math.Abs(side.Yaw)*.5));
            Assert.That(away.Pitch,Is.LessThan(-3));Assert.That(toward.Pitch,Is.GreaterThan(3));
            foreach(var pose in new[]{side,away,toward})foreach(float u in new[]{0f,.303f,.535f,1f})foreach(float v in new[]{0f,.53f,.8f,1f})
                Assert.That(30+Reference(V(u,v,pose),pose,1.06f,1.06f,1.06f).Z,Is.InRange(.1f,100),"Real portrait depth clearance must survive vertical facing.");
        }
        [Test] public void ActualMeshKeepsMouthAndChinRigidWithTheEyes()
        {
            // Mouth atlas x1352..1377/y166..179; chin around1362/188.
            // Interpolate the real quad triangles rather than sampling a
            // smooth weight at a point the rendering grid never contains.
            float[] xs={0,.06f,.16f,.18f,.30f,.303f,.37f,.42f,.47f,.53f,.535f,.625f,.65f,.70f,.84f,1};
            float[] ys={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
            NoahDepthVertex MeshPoint(float u,float v,NoahDepthFocusPose p)
            {
                int x=0,y=0;while(x<xs.Length-2&&u>xs[x+1])x++;while(y<ys.Length-2&&v>ys[y+1])y++;
                float s=(u-xs[x])/(xs[x+1]-xs[x]),t=(v-ys[y])/(ys[y+1]-ys[y]);
                var a=V(xs[x],ys[y],p);var b=V(xs[x],ys[y+1],p);var c=V(xs[x+1],ys[y+1],p);var d=V(xs[x+1],ys[y],p);
                float A=t>=s?1-t:1-s,B=t>=s?t-s:0,C=t>=s?s:t,D=t>=s?0:s-t;
                return new NoahDepthVertex(a.X*A+b.X*B+c.X*C+d.X*D,a.Y*A+b.Y*B+c.Y*C+d.Y*D,a.Z*A+b.Z*B+c.Z*C+d.Z*D,1);
            }
            foreach(float look in new[]{-1f,0,1})foreach(float head in new[]{-1f,1f})foreach(float forward in new[]{-1f,0,1})
            foreach(float reaction in new[]{0f,.4f})
            {
                var pose=Pose(1.2f,1,look,head,forward,reaction);var eye=MeshPoint(.37f,.60f,pose);
                foreach(var point in new[]{(.37f,.49f),(.42f,.49f),(.35f,.51f),(.45f,.51f),(.39f,.46f)})
                {
                    double expected=Math.Sqrt(Math.Pow(Width*(point.Item1-.37f),2)+Math.Pow(Height*(point.Item2-.60f),2));
                    Assert.That(Length(eye,MeshPoint(point.Item1,point.Item2,pose)),Is.EqualTo(expected).Within(.00002),$"Face changed at{point}/{look}/{head}/{forward}/{reaction}");
                }
            }
        }
    }
}
