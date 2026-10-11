using System;
using Mofumachi.Core;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownMovementTests
    {
        private static TownNavigation Paths()=>new TownNavigation(new[]{new TownPoint(.1f,.1f),new TownPoint(.1f,.9f),new TownPoint(.9f,.9f),new TownPoint(.9f,.1f),new TownPoint(.5f,.02f),new TownPoint(.6f,.02f)},new[]{new TownEdge(0,1),new TownEdge(1,2),new TownEdge(2,3),new TownEdge(3,0),new TownEdge(4,5)},.025f);
        [Test] public void FarPointerFollowsConnectedPathWithoutTeleport()
        {
            var m=new TownMovement(Paths(),new TownPoint(.1f,.1f));Assert.That(m.TryBeginDrag(-1),Is.True);m.SetTarget(-1,new TownPoint(.9f,.9f));
            for(int i=0;i<30;i++)m.Tick(1f/30);
            Assert.That(Distance(m.Position,new TownPoint(.1f,.1f)),Is.LessThanOrEqualTo(.12001f));
            var previous=m.Position;m.SetTarget(-1,new TownPoint(.55f,.02f));m.Tick(1f/30);Assert.That(Distance(previous,m.Position),Is.EqualTo(0));
        }
        [Test] public void FountainAndBuildingsCannotBeCrossed()
        {
            var nav=Paths();var p=new TownPoint(.1f,.5f);
            for(int i=0;i<100;i++){p=nav.Move(p,new TownPoint(.9f,.5f),.025f);Assert.That(p.X<=.10001f||p.X>=.89999f||p.Y<=.10001f||p.Y>=.89999f,Is.True,"Must walk around the central fountain");}
            Assert.That(Distance(p,new TownPoint(.9f,.5f)),Is.LessThan(.001f));
        }
        [Test] public void SecondFingerCannotRetargetOrEndOwnerDrag()
        {
            var m=new TownMovement(Paths(),new TownPoint(.1f,.1f));Assert.That(m.TryBeginDrag(-1),Is.True);Assert.That(m.TryBeginDrag(2),Is.False);
            m.SetTarget(-1,new TownPoint(.1f,.9f));m.SetTarget(2,new TownPoint(.9f,.1f));m.ReleasePointer(2);Assert.That(m.OwnedPointerId,Is.EqualTo(-1));m.Tick(1f/30);
            Assert.That(m.Position.X,Is.EqualTo(.1f).Within(.00001f));Assert.That(m.Position.Y,Is.GreaterThan(.1f));
        }
        [Test] public void ReleaseAndPauseStopAtCurrentPosition()
        {
            var m=new TownMovement(Paths(),new TownPoint(.1f,.1f));m.TryBeginDrag(7);m.SetTarget(7,new TownPoint(.1f,.9f));m.Tick(1f/30);m.ReleasePointer(7);
            var p=m.Position;m.Tick(1f/30);Assert.That(Distance(p,m.Position),Is.EqualTo(0));m.TryBeginDrag(8);m.SetTarget(8,new TownPoint(.9f,.1f));m.CancelInput();m.Tick(1f/30);Assert.That(Distance(p,m.Position),Is.EqualTo(0));Assert.That(m.OwnedPointerId,Is.Null);
        }
        [Test] public void InvalidTimeAndUnreachableTargetDoNotBreakPosition()
        {
            var m=new TownMovement(Paths(),new TownPoint(.1f,.1f));m.TryBeginDrag(1);m.SetTarget(1,new TownPoint(.1f,.9f));
            foreach(float dt in new[]{-1f,float.NaN,float.PositiveInfinity,0})m.Tick(dt);Assert.That(m.Position.Y,Is.EqualTo(.1f));
            m.Tick(10);Assert.That(Distance(m.Position,new TownPoint(.1f,.1f)),Is.LessThanOrEqualTo(.00401f));
            var p=m.Position;m.SetTarget(1,new TownPoint(float.NaN,1));m.Tick(1f/30);Assert.That(float.IsNaN(m.Position.X),Is.False);
            Assert.That(()=>new TownNavigation(new[]{new TownPoint(0,0)},new[]{new TownEdge(0,2)},.1f),Throws.ArgumentException);
        }
        private static double Distance(TownPoint a,TownPoint b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
    }
}
