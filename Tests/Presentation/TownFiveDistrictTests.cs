using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownFiveDistrictTests
    {
        [Test] public void FiveScreenSizedRoomsHaveDistinctCardinalLandmarks()
        {
            Assert.That(TownWorldMap.Left,Is.EqualTo(-1.8f));Assert.That(TownWorldMap.Right,Is.EqualTo(1.8f).Within(.0001));
            Assert.That(TownWorldMap.Bottom,Is.EqualTo(-3));Assert.That(TownWorldMap.Top,Is.EqualTo(3));
            Assert.That(TownWorldLayout.ViewWidth,Is.EqualTo(1.10f));
            Assert.That(TownWorldMap.Place("quest").Y,Is.GreaterThan(2));
            Assert.That(TownWorldMap.Place("celebration").Y,Is.LessThan(-2));
            Assert.That(TownWorldMap.Place("west.cafe-front").X,Is.EqualTo(-1.2f));
            Assert.That(TownWorldMap.Place("shop").X,Is.EqualTo(1.2f));
        }
        [Test] public void EachRoomHasBroadFloorAndAllFourSeamsHavePassingWidth()
        {
            var floor=new TownWalkableArea(0);
            foreach(var c in new[]{new TownPoint(0,0),new TownPoint(-1.2f,0),new TownPoint(1.2f,0),new TownPoint(0,2),new TownPoint(0,-2)})
            foreach(float x in new[]{-.42f,.42f})foreach(float y in new[]{-.65f,.65f})
                Assert.That(floor.Contains(new TownPoint(c.X+x,c.Y+y)),Is.True,"Broad floor at "+c.X+","+c.Y);
            foreach(float offset in new[]{-.3f,0,.3f})
            {
                Assert.That(floor.CanTravel(new TownPoint(-.65f,offset),new TownPoint(-.55f,offset)),Is.True);
                Assert.That(floor.CanTravel(new TownPoint(.55f,offset),new TownPoint(.65f,offset)),Is.True);
                Assert.That(floor.CanTravel(new TownPoint(offset,.95f),new TownPoint(offset,1.05f)),Is.True);
                Assert.That(floor.CanTravel(new TownPoint(offset,-1.05f),new TownPoint(offset,-.95f)),Is.True);
            }
            Assert.That(floor.Contains(new TownPoint(1.2f,2)),Is.False,"The corners outside the cross are landscaped");
        }
        [Test] public void AllFiveLandmarksAreHolesAndHaveFloorLoopsOnBothSides()
        {
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);
            foreach(var hole in new[]{(new TownPoint(0,.30f),.17f,.12f),(new TownPoint(-1.2f,.4f),.29f,.31f),(new TownPoint(1.2f,.4f),.275f,.31f),(new TownPoint(0,2.4f),.24f,.26f),(new TownPoint(0,-1.65f),.25f,.225f)})
            {
                Assert.That(floor.Contains(hole.Item1),Is.False);
                var left=new TownPoint(hole.Item1.X-hole.Item2-.08f,hole.Item1.Y);var right=new TownPoint(hole.Item1.X+hole.Item2+.08f,hole.Item1.Y);
                Assert.That(floor.CanTravel(left,right),Is.False,"Landmarks cannot be crossed");
                foreach(float sign in new[]{-1f,1f})
                {
                    var via=new TownPoint(hole.Item1.X,hole.Item1.Y+sign*(hole.Item3+.10f));
                    AssertRoute(floor,nav,left,via);AssertRoute(floor,nav,via,right);
                }
            }
        }
        [Test] public void DistrictsKeepStableOrderingAndLegacyCategoriesWithoutShrinkingTheRooms()
        {
            Assert.That(TownDistrictLayout.All,Is.EqualTo(new[]{TownDistrictId.West,TownDistrictId.Central,TownDistrictId.East,TownDistrictId.North,TownDistrictId.South}));
            foreach(var id in TownDistrictLayout.All)
            {
                Assert.That(TownDistrictLayout.At(TownDistrictLayout.Center(id)),Is.EqualTo(id));
                Assert.That(TownDistrictLayout.At(TownWorldMap.Place(TownDistrictLayout.EntranceId(id))),Is.EqualTo(id));
                Assert.That(TownDistrictLayout.Name(id),Is.Not.Empty);
            }
            Assert.That(TownDistrictLayout.LegacyRegion(TownDistrictId.North),Is.EqualTo(TownRegionId.Central));
            Assert.That(TownDistrictLayout.LegacyRegion(TownDistrictId.South),Is.EqualTo(TownRegionId.Central));
        }
        [Test] public void RenderersReceiveTheSameCachedFloorAndHoleGeometryAtEveryLevel()
        {
            var first=new TownWalkableArea(0);var grown=new TownWalkableArea(6);
            Assert.That(first.FloorPolygons.Count,Is.EqualTo(5));Assert.That(first.ExcludedIslands.Count,Is.GreaterThanOrEqualTo(5));
            Assert.That(first.FloorPolygons,Is.SameAs(grown.FloorPolygons));Assert.That(first.ExcludedIslands,Is.SameAs(grown.ExcludedIslands));
        }
        [TestCase(TownDistrictId.West)] [TestCase(TownDistrictId.East)] [TestCase(TownDistrictId.North)] [TestCase(TownDistrictId.South)]
        public void EveryPhysicalDistrictWalkCountsOnceAtArrivalEvenAtLevelZero(TownDistrictId district)
        {
            var town=new TownWorldWalkModel(0,"momo");int arrived=0,walks=0;
            town.Arrived+=(id,place,walked)=>{if(id==town.Hero.Id){arrived++;if(walked)walks++;}};
            Assert.That(town.TryGuideDistrict(district),Is.True);
            Assert.That(arrived,Is.Zero,"A request cannot award walking progress");
            for(int i=0;i<12000&&town.Hero.IsWalking;i++)town.Step(1f/30);
            Assert.That(town.Hero.IsWalking,Is.False);Assert.That(town.Hero.District,Is.EqualTo(district));
            Assert.That(arrived,Is.EqualTo(1));Assert.That(walks,Is.EqualTo(1));
            for(int i=0;i<60;i++)town.Step(1f/30);Assert.That(arrived,Is.EqualTo(1));
        }
        [Test] public void DistrictFastTravelDoesNotCountAsAWalkAndNormalPlaceLookupKeepsCoreGates()
        {
            var town=new TownWorldWalkModel(0,"momo");
            Assert.That(town.Residents.Select(a=>TownDistrictLayout.At(a.Position)).Distinct().Count(),Is.EqualTo(5),"Initial residents populate all five rooms");
            int walks=0;town.Arrived+=(id,place,walked)=>{if(id==town.Hero.Id&&walked)walks++;};
            foreach(var district in TownDistrictLayout.All){town.FastTravel(district);Assert.That(town.Hero.District,Is.EqualTo(district));}
            Assert.That(walks,Is.Zero);Assert.That(town.TryGuide("momo","west.cafe-front"),Is.False);Assert.That(town.TryGuide("momo","east.workshop-front"),Is.False);
        }
        [Test] public void EveryOrderedPairOfDistrictEntrancesHasAFloorSafeRoute()
        {
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);
            foreach(var from in TownDistrictLayout.All)foreach(var to in TownDistrictLayout.All)
                AssertRoute(floor,nav,TownWorldMap.Place(TownDistrictLayout.EntranceId(from)),TownWorldMap.Place(TownDistrictLayout.EntranceId(to)));
        }
        [Test] public void FingerCrossingBetweenNorthAndSouthCountsOnlyWhenReleased()
        {
            var town=new TownWorldWalkModel(0,"momo");town.FastTravel(TownDistrictId.North);int arrivals=0,walks=0;
            town.Arrived+=(id,place,walked)=>{if(id==town.Hero.Id){arrivals++;if(walked)walks++;}};
            Assert.That(town.FollowFinger(TownWorldMap.Place(TownDistrictLayout.EntranceId(TownDistrictId.South))),Is.True);
            for(int i=0;i<12000&&town.Hero.IsWalking;i++)town.Step(1f/30);
            Assert.That(town.Hero.IsWalking,Is.False);Assert.That(town.Hero.Region,Is.EqualTo(TownRegionId.Central));Assert.That(town.Hero.District,Is.EqualTo(TownDistrictId.South));
            Assert.That(arrivals,Is.Zero,"Holding a finger cannot finish the visit");
            town.ReleaseFinger();town.ReleaseFinger();for(int i=0;i<60;i++)town.Step(1f/30);
            Assert.That(arrivals,Is.EqualTo(1));Assert.That(walks,Is.EqualTo(1));
        }
        [Test] public void LegacyGraphExposesTheSameConnectedSafeFloorIncludingNewDistrictEntrances()
        {
            TownWorldLayout.BuildGraph(0,out var nodes,out var edges);var floor=new TownWalkableArea(0);
            foreach(var node in nodes)Assert.That(floor.Contains(node),Is.True);
            foreach(var edge in edges)Assert.That(floor.CanTravel(nodes[edge.From],nodes[edge.To]),Is.True);
            var reached=new System.Collections.Generic.HashSet<int>{0};bool changed;
            do{changed=false;foreach(var edge in edges)if(reached.Contains(edge.From)||reached.Contains(edge.To)){changed|=reached.Add(edge.From);changed|=reached.Add(edge.To);}}while(changed);
            Assert.That(reached.Count,Is.EqualTo(nodes.Length));
            foreach(var district in TownDistrictLayout.All)
                Assert.That(nodes.Any(p=>TownWorldLayout.Distance(p,TownWorldMap.Place(TownDistrictLayout.EntranceId(district)))<.0001f),Is.True,district.ToString());
        }
        private static void AssertRoute(TownWalkableArea floor,TownAreaNavigation nav,TownPoint a,TownPoint b)
        {var route=nav.FindRoute(a,b);Assert.That(route,Is.Not.Empty);foreach(var next in route){Assert.That(floor.CanTravel(a,next),Is.True);a=next;}Assert.That(TownWorldLayout.Distance(a,b),Is.LessThan(.0001f));}
    }
}
