using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class SeamlessTownTests
    {
        [Test] public void RoadLabelsYieldToAResidentsBodyInsteadOfPrintingAcrossItsFace()
        {
            var label=new TownViewportRect(-71,24,142,30);
            Assert.That(TownWorldLayout.LabelOverlapsResident(label,new TownPoint(0,0),62,78),Is.True);
            Assert.That(TownWorldLayout.LabelOverlapsResident(label,new TownPoint(80,20),62,78),Is.True);
            Assert.That(TownWorldLayout.LabelOverlapsResident(label,new TownPoint(110,20),62,78),Is.False);
            Assert.That(TownWorldLayout.LabelOverlapsResident(label,new TownPoint(0,60),62,78),Is.False);
            Assert.That(TownWorldLayout.LabelOverlapsResident(label,new TownPoint(0,-80),62,78),Is.False);
        }
        [TestCase(TownRegionId.West)] [TestCase(TownRegionId.Central)] [TestCase(TownRegionId.East)]
        public void FirstFlowersAndLevelThreeLampsAreVisibleFromTheRegionalPhoneView(TownRegionId region)
        {
            foreach(float height in new[]{592,640,808})
            {
                float scale=TownWorldLayout.ScaleForViewport(360,height);
                var camera=new TownWorldCamera();camera.Fit(360/scale,height/(scale*TownWorldLayout.ImageAspect));
                var arrival=TownWorldMap.Place(region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square");
                // The background fills the screen. The actual field mask lies inside
                // the safe area (24 bottom / 32 top), then 62 bottom / 80 top controls.
                const float safeBottom=24,safeTop=32,fieldBottom=62,fieldTop=80;
                float maskBottom=-height/2+safeBottom+fieldBottom,maskTop=height/2-safeTop-fieldTop;
                foreach(float cameraLift in new[]{0,.04f}) // regional view / following an arrival
                {
                    camera.Jump(new TownPoint(arrival.X,arrival.Y+cameraLift));
                    foreach(var kind in new[]{TownGrowthProp.Flowers,TownGrowthProp.Lamp})
                    {
                        var prop=TownGrowthPresentation.For(region,kind==TownGrowthProp.Flowers?1:3).First(p=>p.Prop==kind);
                        var point=TownWorldMap.Growth(region,prop);
                        // Width is a world-space size; height follows the actual atlas crop.
                        float aspect=kind==TownGrowthProp.Flowers?620f/327:161f/351;
                        float width=prop.Width*scale,propHeight=width/aspect;
                        float x=(point.X-camera.Center.X)*scale,y=(point.Y-camera.Center.Y)*scale*TownWorldLayout.ImageAspect;
                        string context=region+" "+kind+" at height "+height+" camera lift "+cameraLift;
                        Assert.That(x-width/2,Is.GreaterThanOrEqualTo(-180+8),context);
                        Assert.That(x+width/2,Is.LessThanOrEqualTo(180-8),context);
                        Assert.That(y,Is.GreaterThanOrEqualTo(maskBottom+8),context);
                        Assert.That(y+propHeight,Is.LessThanOrEqualTo(maskTop-8),context);
                    }
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
                camera.Jump(new TownPoint(TownWorldMap.Right,TownWorldMap.Top));Assert.That(camera.Center.Y+height/(2*scale),Is.LessThanOrEqualTo(TownWorldMap.Top+.001f));
            }
        }
        [Test] public void CafeAndQuestGardenFencesAreObstaclesInsteadOfShortcuts()
        {
            var floor=new TownWalkableArea(0);
            Assert.That(floor.Contains(new TownPoint(-1.2f,.4f)),Is.False,"The western cafe building is not paving");
            Assert.That(floor.Contains(new TownPoint(0,2.4f)),Is.False,"The quest cottage and its fenced garden are not paving");
            var street=new TownPoint(0,2.80f);var door=TownWorldMap.Place("quest");
            Assert.That(floor.Contains(street)&&floor.Contains(door),Is.True);
            Assert.That(floor.CanTravel(street,door),Is.False,"Use the garden floor around the cottage instead of crossing the building");
            Assert.That(new TownAreaNavigation(floor).FindRoute(street,door),Is.Not.Empty,"The blocked shortcut must still have a route around the buildings");
        }
        [TestCase(0)] [TestCase(6)] public void EveryStableDestinationAndBothPhysicalEntrancesHaveAFloorSafeRoute(int level)
        {
            var area=new TownWalkableArea(level);var nav=new TownAreaNavigation(area);
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))foreach(var place in TownRegionCatalog.ForLevel(region,level).Map.Places)
            {
                var p=TownWorldMap.Place(place.Id);Assert.That(area.Contains(p),Is.True,place.Id);
                Assert.That(nav.FindRoute(TownWorldMap.Place("square"),p),Is.Not.Empty,place.Id);
            }
            Assert.That(TownWorldLayout.RegionAt(TownWorldMap.Place("quest")),Is.EqualTo(TownRegionId.Central));
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
            foreach(var p in new[]{new TownPoint(-.30f,.30f),new TownPoint(.30f,.30f),new TownPoint(0,.60f),new TownPoint(0,.02f),TownWorldMap.Place("west.cafe-front"),TownWorldMap.Place("shop"),TownWorldMap.Place("quest"),new TownPoint(-.40f,-2.40f),new TownPoint(.40f,-2.40f),new TownPoint(0,-2.70f)})
                Assert.That(area.Contains(p),Is.True,"New artwork paving at "+p.X+","+p.Y);
            foreach(var p in new[]{new TownPoint(0,.30f),new TownPoint(0,2.4f),new TownPoint(-1.2f,.4f),new TownPoint(1.2f,.4f),new TownPoint(-.22f,-.65f),new TownPoint(1.00f,-.70f),new TownPoint(-.20f,-2.65f),new TownPoint(0,-1.65f)})
                Assert.That(area.Contains(p),Is.False,"Fountain, buildings and garden islands are visible blockers at "+p.X+","+p.Y);
        }
        [Test] public void BothFountainRoutesAndTheEntireTownAreConnectedWithoutCuttingAcrossWater()
        {
            var area=new TownWalkableArea(0);var nav=new TownAreaNavigation(area);
            var points=new[]{new TownPoint(-.30f,.30f),new TownPoint(.30f,.30f),new TownPoint(0,.60f),new TownPoint(0,.02f),TownWorldMap.Place("west.cafe-front"),TownWorldMap.Place("shop"),TownWorldMap.Place("quest"),new TownPoint(-.40f,-2.40f),new TownPoint(.40f,-2.40f),new TownPoint(0,-2.70f)};
            foreach(var target in points)
            {
                var current=points[0];var route=nav.FindRoute(current,target);Assert.That(route,Is.Not.Empty,"Paving at "+target.X+","+target.Y);
                foreach(var next in route){Assert.That(area.CanTravel(current,next),Is.True);current=next;}
                Assert.That(TownWalkableArea.Distance(current,target),Is.LessThan(.001f));
            }
            Assert.That(area.CanTravel(points[0],points[1]),Is.False,"Route around the fountain, not through it");
            foreach(var via in new[]{points[2],points[3]})
            foreach(var ends in new[]{new[]{points[0],via},new[]{via,points[1]}})
            {
                var previous=ends[0];var route=nav.FindRoute(previous,ends[1]);Assert.That(route,Is.Not.Empty,"The paving above and below the fountain must both connect");
                foreach(var next in route){Assert.That(area.CanTravel(previous,next),Is.True);previous=next;}
                Assert.That(TownWalkableArea.Distance(previous,ends[1]),Is.LessThan(.001f));
            }
        }
    }
}
