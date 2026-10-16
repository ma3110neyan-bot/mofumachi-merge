using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownWorldTests
    {
        [Test] public void NorthAndSouthReuseStableIdsOnOneVerticalCoordinateSystem()
        {
            var p=new TownPoint(.4f,.5f);
            var north=TownWorldLayout.ToWorld(TownRegionId.West,p);var center=TownWorldLayout.ToWorld(TownRegionId.Central,p);var south=TownWorldLayout.ToWorld(TownRegionId.East,p);
            Assert.That(north.Y,Is.GreaterThan(center.Y));Assert.That(south.Y,Is.LessThan(center.Y));
            foreach(TownRegionId r in Enum.GetValues(typeof(TownRegionId)))
            {var round=TownWorldLayout.ToLocal(r,TownWorldLayout.ToWorld(r,p));Assert.That(round.X,Is.EqualTo(p.X).Within(.00001));Assert.That(round.Y,Is.EqualTo(p.Y).Within(.00001));}
        }
        [Test] public void HeroWalksNorthToSouthContinuouslyThroughTheCentralPark()
        {
            var town=new TownWorldWalkModel(6,"momo");town.FastTravel(TownRegionId.West);
            Assert.That(town.TryGuide("momo","east.flowers"),Is.True);var previous=town.Hero.Position;bool sawCenter=false;int arrived=0;town.Arrived+=(id,place,walked)=>{if(id=="momo"&&walked)arrived++;};
            for(int i=0;i<18000&&town.Hero.IsWalking;i++)
            {town.Step(1f/30);var p=town.Hero.Position;Assert.That(Distance(previous,p),Is.LessThanOrEqualTo(.12f/30+.00001f));if(town.Hero.Region==TownRegionId.Central)sawCenter=true;previous=p;}
            Assert.That(sawCenter,Is.True);Assert.That(town.Hero.Region,Is.EqualTo(TownRegionId.East));Assert.That(town.Hero.IsWalking,Is.False);Assert.That(arrived,Is.EqualTo(1));Assert.That(town.Residents.Count,Is.EqualTo(6));
        }
        [Test] public void MidPathRetargetAndCancelNeverTeleportTheHero()
        {
            var town=new TownWorldWalkModel(0,"noah");town.TryGuide("noah","west.flowers");for(int i=0;i<300;i++)town.Step(1f/30);
            var p=town.Hero.Position;Assert.That(town.TryGuide("noah","east.flowers"),Is.True);Assert.That(Distance(p,town.Hero.Position),Is.Zero);
            town.CancelHero();Assert.That(Distance(p,town.Hero.Position),Is.Zero);Assert.That(town.Hero.IsWalking,Is.False);
            foreach(float dt in new[]{float.NaN,float.PositiveInfinity,-1,0})town.Step(dt);Assert.That(Distance(p,town.Hero.Position),Is.Zero);
        }
        [Test] public void OffRoadAndUnknownTargetsDoNotStartOrProgressAVisit()
        {var t=new TownWorldWalkModel(0,"lulu");Assert.That(t.TryGuide("lulu","missing"),Is.False);Assert.That(t.TryGuidePoint(new TownPoint(100,100)),Is.False);Assert.That(t.TryGuidePoint(new TownPoint(float.NaN,0)),Is.False);Assert.That(t.Hero.IsWalking,Is.False);}
        [Test] public void FastTravelIsDistinctFromCompletedWalkingAndCameraRemainsFinite()
        {
            var t=new TownWorldWalkModel(6,"momo");int walks=0;t.Arrived+=(id,place,walked)=>{if(walked&&id=="momo")walks++;};t.FastTravel(TownRegionId.East);Assert.That(walks,Is.Zero);Assert.That(t.Hero.Region,Is.EqualTo(TownRegionId.East));
            var c=new TownWorldCamera();c.Follow(t.Hero.Position,1f/30);Assert.That(float.IsFinite(c.Center.X)&&float.IsFinite(c.Center.Y),Is.True);c.Follow(new TownPoint(float.NaN,0),.1f);Assert.That(float.IsFinite(c.Center.Y),Is.True);
            Assert.That(TownWorldLayout.Name(TownRegionId.Central),Is.EqualTo("中央公園"));
        }
        private static float Distance(TownPoint a,TownPoint b)=>(float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
    }
}
