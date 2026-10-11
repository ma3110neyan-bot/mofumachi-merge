using System;
using Mofumachi.Core;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class TownFrameClockTests
    {
        [Test] public void FramePartitionsKeepTheSameTravelBudget()
        {
            foreach(var slices in new[]{30,60,120})
            {
                var clock=new TownFrameClock();int steps=0;
                for(int i=0;i<slices;i++)steps+=clock.AddElapsed(1f/slices);
                Assert.That(steps,Is.EqualTo(30),"rate "+slices);
            }
            var irregular=new TownFrameClock();int total=0;
            for(int i=0;i<20;i++){total+=irregular.AddElapsed(.012f);total+=irregular.AddElapsed(.038f);}
            Assert.That(total,Is.EqualTo(30));
            var nav=new TownNavigation(new[]{new TownPoint(0,0),new TownPoint(1,0)},new[]{new TownEdge(0,1)},.028f);
            var movement=new TownMovement(nav,new TownPoint(0,0));movement.TryBeginDrag(1);movement.SetTarget(1,new TownPoint(1,0));
            for(int i=0;i<total;i++)movement.Tick(TownFrameClock.StepSeconds);
            Assert.That(movement.Position.X,Is.EqualTo(.12f).Within(.0001f));
        }
        [Test] public void ClockDropsStallDebtAndResetClearsIt()
        {
            var clock=new TownFrameClock();Assert.That(clock.AddElapsed(10),Is.EqualTo(3));
            Assert.That(clock.Alpha,Is.InRange(0f,.999999f));
            Assert.That(clock.AddElapsed(.001f),Is.EqualTo(0));
            foreach(var invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
                Assert.That(clock.AddElapsed(invalid),Is.EqualTo(0));
            clock.Reset();Assert.That(clock.Alpha,Is.EqualTo(0));
            Assert.That(clock.AddElapsed(TownFrameClock.StepSeconds),Is.EqualTo(1));
        }
        [Test] public void InterpolationFollowsTheCorner()
        {
            var nav=new TownNavigation(new[]{new TownPoint(0,0),new TownPoint(1,0),new TownPoint(1,1)},new[]{new TownEdge(0,1),new TownEdge(1,2)},.028f);
            var a=new TownPoint(.9f,0);var b=new TownPoint(1,.1f);
            Assert.That(nav.DistanceAlongPath(a,b),Is.EqualTo(.2f).Within(.0001f));
            var midpoint=nav.InterpolateAlongPath(a,b,.5f);
            Assert.That(midpoint.X,Is.EqualTo(1).Within(.0001f));Assert.That(midpoint.Y,Is.EqualTo(0).Within(.0001f));
            Assert.That(nav.InterpolateAlongPath(a,b,-1).X,Is.EqualTo(a.X));
            Assert.That(nav.InterpolateAlongPath(a,b,2).Y,Is.EqualTo(b.Y).Within(.0001f));
            Assert.That(nav.InterpolateAlongPath(a,b,float.NaN).X,Is.EqualTo(a.X));
        }
        [Test] public void DisconnectedInterpolationDoesNotTeleport()
        {
            var nav=new TownNavigation(new[]{new TownPoint(0,0),new TownPoint(1,0),new TownPoint(0,1),new TownPoint(1,1)},new[]{new TownEdge(0,1),new TownEdge(2,3)},.028f);
            var a=new TownPoint(.5f,0);var b=new TownPoint(.5f,1);
            Assert.That(nav.DistanceAlongPath(a,b),Is.EqualTo(0));
            Assert.That(nav.InterpolateAlongPath(a,b,.5f).Y,Is.EqualTo(0));
        }
    }
}
