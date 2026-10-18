using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownWalkableAreaTests
    {
        [Test] public void ParkHasAWalkableSurfaceOnBothSidesOfTheFountain()
        {
            var area=new TownWalkableArea(0);
            foreach(var p in new[]{TownWorldMap.Point(.19f,.45f),TownWorldMap.Point(.80f,.45f),TownWorldMap.Place("square"),TownWorldMap.Place("fountain")})
                Assert.That(area.Contains(p),Is.True,p.ToString());
            Assert.That(area.Contains(TownWorldMap.Point(.50f,.455f)),Is.False,"The fountain is an obstacle, not floor");
            Assert.That(area.Contains(TownWorldMap.Point(.80f,.15f)),Is.False,"Painted buildings cannot be crossed");
        }
        [Test] public void AFreeFloorTapKeepsItsPositionRatherThanSnappingToTheOldCenterline()
        {
            var town=new TownWorldWalkModel(0,"momo");var target=new TownPoint(.79f,.55f);
            Assert.That(town.TryGuidePoint(target),Is.True);
            Assert.That(TownWorldLayout.Distance(town.Hero.Target,target),Is.LessThan(.0001f));
        }
        [Test] public void RoutesGoAroundTheFountainAndStayInsideTheAuthoredFloor()
        {
            var area=new TownWalkableArea(0);var nav=new TownAreaNavigation(area);
            var start=TownWorldMap.Point(.19f,.45f);var end=TownWorldMap.Point(.80f,.45f);var route=nav.FindRoute(start,end);
            Assert.That(route.Count,Is.GreaterThan(1),"A straight segment would cross the fountain");
            var previous=start;foreach(var point in route){Assert.That(area.CanTravel(previous,point),Is.True);previous=point;}
            Assert.That(TownWorldLayout.Distance(previous,end),Is.LessThan(.0001f));
        }
        [Test] public void AThinGapBetweenPaintedFloorAndConnectorCannotBeSkippedBySampling()
        {
            var area=new TownWalkableArea(6);var center=TownWorldMap.Point(.5f,.455f);var a=new TownPoint(center.X-.15f,center.Y+.1799f);var b=new TownPoint(center.X+.15f,center.Y+.1799f);
            Assert.That(area.Contains(a)&&area.Contains(b),Is.True);
            Assert.That(area.CanTravel(a,b),Is.False,"There is a narrow off-floor interval, even though both endpoints are on floor");
        }
        [Test] public void NeighbouringFloorTargetsHaveDifferentRoutesAndAllThreeAreasConnect()
        {
            var area=new TownWalkableArea(6);var nav=new TownAreaNavigation(area);
            var a=nav.FindRoute(TownWorldMap.Place("square"),TownWorldMap.Point(.74f,.47f));
            var b=nav.FindRoute(TownWorldMap.Place("square"),TownWorldMap.Point(.78f,.48f));
            Assert.That(a.Last(),Is.Not.EqualTo(b.Last()));
            foreach(var r in new[]{TownRegionId.West,TownRegionId.East})
            {
                var p=TownWorldMap.Place(TownRegionCatalog.ForLevel(r,6).Map.Places.First(x=>x.Activity==TownActivity.Flowers).Id);
                var path=nav.FindRoute(TownWorldMap.Place("square"),p);Assert.That(path,Is.Not.Empty,r.ToString());
                var previous=TownWorldMap.Place("square");foreach(var next in path){Assert.That(area.CanTravel(previous,next),Is.True);previous=next;}
                Assert.That(TownWorldLayout.Distance(previous,p),Is.LessThan(.0001f));
            }
        }
        [Test] public void FingerInputClampsBuildingsAndReleaseStopsWithoutWalkingToTheOldTarget()
        {
            var town=new TownWorldWalkModel(6,"momo");var area=town.Area;
            Assert.That(town.FollowFinger(new TownPoint(.80f,.55f)),Is.True);
            for(int i=0;i<90;i++)town.Step(1f/30);
            Assert.That(town.FollowFinger(new TownPoint(.95f,.68f)),Is.True);
            Assert.That(area.Contains(town.Hero.Target),Is.True);
            var at=town.Hero.Position;town.ReleaseFinger();for(int i=0;i<60;i++)town.Step(1f/30);
            Assert.That(TownWorldLayout.Distance(at,town.Hero.Position),Is.LessThan(.025f));
            Assert.That(town.Hero.IsWalking,Is.False);Assert.That(town.IsFollowingFinger,Is.False);
            Assert.That(town.FollowFinger(new TownPoint(float.NaN,0)),Is.False);
        }
        [Test] public void OpposingResidentsPassWithoutPushingAStationaryResidentOrGettingStuck()
        {
            var town=new TownWorldWalkModel(6,"momo");var a=new TownPoint(.30f,.46f);var b=new TownPoint(.68f,.46f);
            town.Restore("momo",a);town.Restore("lulu",b);town.Restore("pom",new TownPoint(.47f,.46f));town.Pause("pom",4);var idle=town.Residents.Single(x=>x.Id=="pom").Position;
            Assert.That(town.TryGuidePoint(b),Is.True);Assert.That(town.TryGuide("lulu","bench-left"),Is.True);
            bool separated=false;
            for(int i=0;i<1800&&town.Hero.IsWalking;i++)
            {
                town.Step(1f/30);Assert.That(town.Area.Contains(town.Hero.Position),Is.True);
                if(i<100)Assert.That(town.Residents.Single(x=>x.Id=="pom").Position,Is.EqualTo(idle),"Passing cannot push a stationary character");
                var lulu=town.Residents.Single(x=>x.Id=="lulu");
                if(Math.Abs(lulu.Position.X-town.Hero.Position.X)<.08f&&Math.Abs(lulu.Position.Y-town.Hero.Position.Y)>.012f)separated=true;
            }
            Assert.That(town.Hero.IsWalking,Is.False,"No deadlock at the crossing");Assert.That(separated,Is.True,"Residents need room to pass side by side");
        }
        [TestCase(0)] [TestCase(6)] public void EveryUnlockedPlaceHasAnObstacleSafeRouteFromThePark(int level)
        {
            var area=new TownWalkableArea(level);var nav=new TownAreaNavigation(area);var start=TownWorldMap.Place("square");
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))foreach(var place in TownRegionCatalog.ForLevel(region,level).Map.Places)
            {var target=TownWorldMap.Place(place.Id);Assert.That(area.Contains(target),Is.True,place.Id);var route=nav.FindRoute(start,target);Assert.That(route,Is.Not.Empty,place.Id);var previous=start;foreach(var next in route){Assert.That(area.CanTravel(previous,next),Is.True,place.Id);previous=next;}}
        }
        [TestCase(.02f,.57f,.90f,-.33f)]
        [TestCase(.50f,1.11f,.50f,-.45f)]
        [TestCase(.90f,-.33f,.02f,.57f)]
        public void AvoidanceDoesNotLoseTheRouteAndLoopAroundAnObstacle(float x,float y,float tx,float ty)
        {
            var town=new TownWorldWalkModel(6,"momo");town.Restore("momo",new TownPoint(x,y));
            Assert.That(town.TryGuidePoint(new TownPoint(tx,ty)),Is.True);var target=town.Hero.Target;
            for(int i=0;i<6000&&town.Hero.IsWalking;i++)town.Step(1f/30);
            Assert.That(town.Hero.IsWalking,Is.False,"A valid journey must finish even after residents steer around each other");
            Assert.That(TownWorldLayout.Distance(town.Hero.Position,target),Is.LessThan(.002f));
        }
    }
}
