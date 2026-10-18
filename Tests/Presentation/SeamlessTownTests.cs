using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class SeamlessTownTests
    {
        private static TownPoint Map(float u,float v)=>new TownPoint(-.3f+1.6f*u,1.65f-2.4f*v);
        [TestCase(TownRegionId.West)] [TestCase(TownRegionId.Central)] [TestCase(TownRegionId.East)]
        public void FirstFlowersAndLevelThreeLampsAreVisibleFromTheRegionalPhoneView(TownRegionId region)
        {
            foreach(float height in new[]{592,640,808})
            {
                float scale=TownWorldLayout.ScaleForViewport(360,height);
                var camera=new TownWorldCamera();camera.Fit(360/scale,height/scale);
                camera.Jump(TownWorldMap.Place(region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square"));
                foreach(var kind in new[]{TownGrowthProp.Flowers,TownGrowthProp.Lamp})
                {
                    var prop=TownGrowthPresentation.For(region,kind==TownGrowthProp.Flowers?1:3).First(p=>p.Prop==kind);
                    var point=TownWorldMap.Growth(region,prop);
                    // Match the native 24/32-unit safe margins, 142-unit HUD,
                    // 178-unit footer, and the resulting field-center offset.
                    float x=(point.X-camera.Center.X)*scale,y=(point.Y-camera.Center.Y)*scale-14;
                    Assert.That(Math.Abs(x),Is.LessThan(168),region+" "+kind+" at height "+height);
                    Assert.That(y,Is.InRange(-(height-376)/2+8,(height-376)/2-48),region+" "+kind+" at height "+height);
                }
            }
        }
        [Test] public void CaptionsStayInsideTheActualTownMaskWithoutMovingThePaintedDoor()
        {
            foreach(float x in new[]{-300f,0,300f})
            {
                float center=TownWorldLayout.LabelCenter(x,-180,180,142);
                Assert.That(center-71,Is.GreaterThanOrEqualTo(-180));Assert.That(center+71,Is.LessThanOrEqualTo(180));
            }
            float doorTop=75.86f,maskTop=108,height=44;
            float offset=TownWorldLayout.CaptionDrop(doorTop,height,maskTop);
            Assert.That(offset,Is.LessThan(0));Assert.That(doorTop+offset+height,Is.LessThanOrEqualTo(maskTop));
            Assert.That(TownWorldLayout.CaptionDrop(0,28,108),Is.Zero,"Leave an already fitting sign attached to its doorway");
        }
        [Test] public void MapProjectionCoversShortTallAndUnusuallyTallViewportsWithoutStretching()
        {
            foreach(float height in new[]{592,808,1280,1800})
            {
                float scale=TownWorldLayout.ScaleForViewport(360,height);
                Assert.That(TownWorldMap.Width*scale,Is.GreaterThanOrEqualTo(360));
                Assert.That(TownWorldMap.Height*TownWorldLayout.ImageAspect*scale,Is.GreaterThanOrEqualTo(height-.001f));
                var camera=new TownWorldCamera();camera.Fit(360/scale,height/(scale*TownWorldLayout.ImageAspect));
                camera.Jump(Map(1,0));Assert.That(camera.Center.Y+height/(2*scale),Is.LessThanOrEqualTo(TownWorldMap.Top+.001f));
            }
        }
        [Test] public void CafeAndQuestGardenFencesAreObstaclesInsteadOfShortcuts()
        {
            var floor=new TownWalkableArea(0);
            Assert.That(floor.Contains(Map(.32f,.265f)),Is.False,"Cafe fenced flower bed");
            Assert.That(floor.Contains(Map(.68f,.275f)),Is.False,"Quest cottage fenced bed");
            Assert.That(floor.CanTravel(Map(.58f,.28f),TownWorldMap.Place("quest")),Is.False,"Use the gate around the flower bed");
        }
        [TestCase(0)] [TestCase(6)] public void EveryStableDestinationAndBothPhysicalEntrancesHaveAFloorSafeRoute(int level)
        {
            var area=new TownWalkableArea(level);var nav=new TownAreaNavigation(area);
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))foreach(var place in TownRegionCatalog.ForLevel(region,level).Map.Places)
            {
                var p=TownWorldMap.Place(place.Id);Assert.That(area.Contains(p),Is.True,place.Id);
                Assert.That(nav.FindRoute(TownWorldMap.Place("square"),p),Is.Not.Empty,place.Id);
            }
            Assert.That(TownWorldLayout.RegionAt(TownWorldMap.Place("quest")),Is.EqualTo(TownRegionId.West));
            Assert.That(TownWorldLayout.RegionAt(TownWorldMap.Place("shop")),Is.EqualTo(TownRegionId.East));
        }
        [Test] public void CameraCannotExposeAnyMapEdgeAtShortAndTallPhoneAspects()
        {
            foreach(float height in new[]{1.2f,1.6f,1.9f})
            {
                var c=new TownWorldCamera();c.Fit(.84f,height);
                foreach(var target in new[]{new TownPoint(-5,-5),new TownPoint(5,5)})
                {c.Jump(target);Assert.That(c.Center.X-.42f,Is.GreaterThanOrEqualTo(TownWorldMap.Left-.0001f));Assert.That(c.Center.X+.42f,Is.LessThanOrEqualTo(TownWorldMap.Right+.0001f));Assert.That(c.Center.Y-height/2,Is.GreaterThanOrEqualTo(TownWorldMap.Bottom-.0001f));Assert.That(c.Center.Y+height/2,Is.LessThanOrEqualTo(TownWorldMap.Top+.0001f));}
            }
        }
        [Test] public void NewMapHasBroadPavingOnBothSidesOfTheFountainAndAtBothCabins()
        {
            var area=new TownWalkableArea(0);
            foreach(var p in new[]{Map(.22f,.44f),Map(.78f,.44f),Map(.5f,.57f),Map(.79f,.25f),Map(.80f,.82f),Map(.5f,.92f)})
                Assert.That(area.Contains(p),Is.True,"New artwork paving at "+p.X+","+p.Y);
            foreach(var p in new[]{Map(.50f,.46f),Map(.19f,.13f),Map(.82f,.15f),Map(.80f,.73f)})
                Assert.That(area.Contains(p),Is.False,"Fountain and buildings are visible blockers");
        }
        [Test] public void BothFountainRoutesAndTheEntireTownAreConnectedWithoutCuttingAcrossWater()
        {
            var area=new TownWalkableArea(0);var nav=new TownAreaNavigation(area);
            var points=new[]{Map(.22f,.44f),Map(.78f,.44f),Map(.5f,.57f),Map(.79f,.25f),Map(.80f,.82f),Map(.5f,.92f)};
            foreach(var target in points)
            {
                var current=points[0];var route=nav.FindRoute(current,target);Assert.That(route,Is.Not.Empty);
                foreach(var next in route){Assert.That(area.CanTravel(current,next),Is.True);current=next;}
                Assert.That(TownWalkableArea.Distance(current,target),Is.LessThan(.001f));
            }
            Assert.That(area.CanTravel(points[0],points[1]),Is.False,"Route around the fountain, not through it");
        }
    }
}
