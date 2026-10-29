using System;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Visual.Tests
{
    public sealed class NoahFinalRigTests
    {
        private static NoahFinalRigPose Pose(float look=0,float forward=0,float step=0,float head=99,float hand=0,float lean=0)
            =>TownNoahFinalRig.Pose(new Noah25DTrialPose(0,lean,step,0,hand,look,step),head==99?look:head,forward);
        private static NoahDepthVertex V(float u,float v,NoahFinalRigPose pose)=>TownNoahFinalRig.Vertex(u,v,62,78,pose);
        [Test] public void OnlyNoahCanOptInAndNormalProductsRemainOff()
        {Assert.That(TownNoahFinalRig.IsEnabled("noah",true),Is.True);foreach(var id in new[]{"momo","fio","lulu","miel","pom",null})Assert.That(TownNoahFinalRig.IsEnabled(id,true),Is.False);Assert.That(TownNoahFinalRig.IsEnabled("noah",false),Is.False);}
        [Test] public void FaceMouthAndChinUseOneAffineBone()
        {
            foreach(float look in new[]{-1f,0,1})foreach(float forward in new[]{-1f,0,1})
            {
                var p=Pose(look,forward);var a=V(.30f,.60f,p);var b=V(.60f,.60f,p);var c=V(.45f,.60f,p);
                Assert.That(c.X,Is.EqualTo((a.X+b.X)/2).Within(.00002));Assert.That(c.Y,Is.EqualTo((a.Y+b.Y)/2).Within(.00002));
                var mouth=TownNoahFinalRig.HeadWeight(.40f,.49f);var chin=TownNoahFinalRig.HeadWeight(.40f,.46f);
                Assert.That(mouth,Is.EqualTo(1));Assert.That(chin,Is.EqualTo(1));
            }
        }
        [Test] public void StaticDepthIsProjectedIntoXYEvenWithoutCanvasPerspective()
        {
            var p=Pose();var head=V(.36f,.67f,p);var body=V(.40f,.32f,p);
            float sourceDelta=62*(.36f-.40f);
            Assert.That(Math.Abs((head.X-body.X)-sourceDelta),Is.GreaterThan(2),"Geometry difference only; this is not aesthetic approval.");
            Assert.That(head.Z,Is.LessThan(body.Z-4));
            var left=V(.36f,.67f,Pose(-1));var right=V(.36f,.67f,Pose(1));Assert.That(Math.Abs(left.X-right.X),Is.GreaterThan(3));
        }
        [Test] public void SupportStaysOnGroundAcrossTurnsAndStepPhase()
        {
            foreach(float look in new[]{-1f,0,1})foreach(float forward in new[]{-1f,0,1})for(int i=0;i<25;i++)
            {var p=Pose(look,forward,(float)Math.Sin(i*.3));var t=TownNoahFinalRig.Support(62,78,p);var a=V(.303f,0,p);var b=V(.535f,0,p);Assert.That(Math.Min(a.Y,b.Y)+t.Y,Is.EqualTo(0).Within(.00002));}
        }
        [Test] public void ContactShadowAlsoRetainsTheBroadEllipseBesideTheFeet()
        {
            var g=TownNoahFinalRig.Grounding;
            float dx=(14-(g.X+g.Width*.5f-.5f)*62)/(g.Width*31),dy=(3-(g.Y+g.Height*.5f)*78)/(g.Height*39);
            float visible=TownNoahFinalRig.GroundOpacity((float)Math.Sqrt(dx*dx+dy*dy))*g.Alpha;
            Assert.That(visible,Is.GreaterThan(TownNoahDepthFocus.GroundAlpha(14,3,62,78,true)*1.15f),"A darker centre must not erase the broad grounding ellipse. Numeric coverage, not visual approval.");
            Assert.That(TownNoahFinalRig.GroundOpacity(1),Is.Zero);Assert.That(TownNoahFinalRig.GroundOpacity(0),Is.EqualTo(1));
        }
        [Test] public void GridNeverFoldsAndInputsRemainFinite()
        {
            float[] xs={0,.06f,.16f,.18f,.30f,.303f,.37f,.42f,.47f,.53f,.535f,.625f,.65f,.70f,.84f,1};
            float[] ys={0,.06f,.14f,.22f,.28f,.36f,.44f,.49f,.53f,.60f,.67f,.80f,1};
            foreach(float look in new[]{-1f,0,1})foreach(float forward in new[]{-1f,0,1})foreach(float step in new[]{-1f,0,1})foreach(float head in new[]{-1f,0,1})foreach(float hand in new[]{-1f,0,1})
            for(int y=0;y<ys.Length-1;y++)for(int x=0;x<xs.Length-1;x++)
            {var p=Pose(look,forward,step,head,hand,hand*4.2f);var a=V(xs[x],ys[y],p);var b=V(xs[x],ys[y+1],p);var c=V(xs[x+1],ys[y+1],p);var d=V(xs[x+1],ys[y],p);string at=$"look={look},head={head},forward={forward},step={step},hand={hand},u={xs[x]},v={ys[y]}";Assert.That(Cross(a,b,c),Is.LessThan(0),at);Assert.That(Cross(a,c,d),Is.LessThan(0),at);Assert.That(a.Light,Is.InRange(.72f,1));}
            foreach(float n in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){var q=TownNoahFinalRig.Vertex(n,n,n,n,default);Assert.That(float.IsFinite(q.X)&&float.IsFinite(q.Y)&&float.IsFinite(q.Z),Is.True);}
        }
        private static float Cross(NoahDepthVertex a,NoahDepthVertex b,NoahDepthVertex c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
    }
}
