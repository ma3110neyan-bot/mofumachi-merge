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
            foreach(var p in new[]{new TownPoint(-.30f,.30f),new TownPoint(.30f,.30f),TownWorldMap.Place("square"),TownWorldMap.Place("fountain")})
                Assert.That(area.Contains(p),Is.True,p.ToString());
            Assert.That(area.Contains(new TownPoint(0,.30f)),Is.False,"The fountain is an obstacle, not floor");
            Assert.That(area.Contains(new TownPoint(1.2f,.40f)),Is.False,"The painted florist building cannot be crossed");
        }
        [Test] public void AFreeFloorTapKeepsItsPositionRatherThanSnappingToTheOldCenterline()
        {
            var town=new TownWorldWalkModel(0,"momo");var target=new TownPoint(.34f,.18f);
            Assert.That(town.Area.Contains(target),Is.True);
            Assert.That(town.TryGuidePoint(target),Is.True);
            Assert.That(TownWorldLayout.Distance(town.Hero.Target,target),Is.LessThan(.0001f));
        }
        [Test] public void RoutesGoAroundTheFountainAndStayInsideTheAuthoredFloor()
        {
            var area=new TownWalkableArea(0);var nav=new TownAreaNavigation(area);
            var start=new TownPoint(-.30f,.30f);var end=new TownPoint(.30f,.30f);var route=nav.FindRoute(start,end);
            Assert.That(area.Contains(start)&&area.Contains(end),Is.True);
            Assert.That(area.CanTravel(start,end),Is.False);
            Assert.That(route.Count,Is.GreaterThan(1),"A straight segment would cross the fountain");
            var previous=start;foreach(var point in route){Assert.That(area.CanTravel(previous,point),Is.True);previous=point;}
            Assert.That(TownWorldLayout.Distance(previous,end),Is.LessThan(.0001f));
        }
        [Test] public void AThinFountainEdgeCrossingCannotBeSkippedBySampling()
        {
            // Graze the north edge of the actual fountain island.
            // The blocked interval is under .01 world units, between valid endpoints.
            var area=new TownWalkableArea(6);var island=area.ExcludedIslands[0];var center=island.Center;
            float y=center.Y+island.Y-.00002f;
            var a=new TownPoint(center.X-.05f,y);var b=new TownPoint(center.X+.07f,y);
            Assert.That(area.Contains(a)&&area.Contains(b),Is.True);
            Assert.That(area.Contains(new TownPoint(center.X,y)),Is.False);
            Assert.That(area.CanTravel(a,b),Is.False,"There is a narrow off-floor interval, even though both endpoints are on floor");
        }
        [Test] public void NeighbouringFloorTargetsHaveDifferentRoutesAndAllThreeAreasConnect()
        {
            var area=new TownWalkableArea(6);var nav=new TownAreaNavigation(area);
            var a=nav.FindRoute(TownWorldMap.Place("square"),new TownPoint(.32f,.20f));
            var b=nav.FindRoute(TownWorldMap.Place("square"),new TownPoint(.38f,.18f));
            Assert.That(a,Is.Not.Empty);Assert.That(b,Is.Not.Empty);
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
            Assert.That(town.FollowFinger(new TownPoint(.38f,.18f)),Is.True);
            for(int i=0;i<90;i++)town.Step(1f/30);
            var building=new TownPoint(1.2f,.40f);Assert.That(area.Contains(building),Is.False);
            Assert.That(town.FollowFinger(building),Is.True);
            Assert.That(area.Contains(town.Hero.Target),Is.True);
            Assert.That(TownWalkableArea.Distance(town.Hero.Target,building),Is.GreaterThan(.01f));
            var at=town.Hero.Position;town.ReleaseFinger();for(int i=0;i<60;i++)town.Step(1f/30);
            Assert.That(TownWorldLayout.Distance(at,town.Hero.Position),Is.LessThan(.025f));
            Assert.That(town.Hero.IsWalking,Is.False);Assert.That(town.IsFollowingFinger,Is.False);
            Assert.That(town.FollowFinger(new TownPoint(float.NaN,0)),Is.False);
        }
        [Test] public void OpposingResidentsPassWithoutPushingAStationaryResidentOrGettingStuck()
        {
            var town=new TownWorldWalkModel(6,"momo");var a=TownWorldMap.Place("square");var b=TownWorldMap.Place("bench-right");
            var middle=new TownPoint((a.X+b.X)/2,(a.Y+b.Y)/2);Assert.That(town.Area.Contains(middle),Is.True);Assert.That(town.Area.CanTravel(a,b),Is.True);
            town.Restore("momo",a);town.Restore("lulu",b);town.Restore("pom",middle);town.Pause("pom",4);var idle=town.Residents.Single(x=>x.Id=="pom").Position;
            Assert.That(idle,Is.EqualTo(middle),"The stationary resident starts on the actual shared paving");
            Assert.That(town.TryGuidePoint(b),Is.True);Assert.That(town.TryGuide("lulu","square"),Is.True);
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
        [TestCase(-1.2f,.04f,1.2f,.04f)]
        [TestCase(0,.65f,0,-2.4f)]
        [TestCase(1.2f,.04f,-1.2f,.04f)]
        public void AvoidanceDoesNotLoseTheRouteAndLoopAroundAnObstacle(float u,float v,float tu,float tv)
        {
            var town=new TownWorldWalkModel(6,"momo");var start=new TownPoint(u,v);var end=new TownPoint(tu,tv);
            Assert.That(town.Area.Contains(start)&&town.Area.Contains(end),Is.True);
            town.Restore("momo",start);Assert.That(town.Hero.Position,Is.EqualTo(start));
            Assert.That(town.TryGuidePoint(end),Is.True);var target=town.Hero.Target;
            for(int i=0;i<6000&&town.Hero.IsWalking;i++)town.Step(1f/30);
            Assert.That(town.Hero.IsWalking,Is.False,"A valid journey must finish even after residents steer around each other");
            Assert.That(TownWorldLayout.Distance(town.Hero.Position,target),Is.LessThan(.002f));
        }
    }
}
