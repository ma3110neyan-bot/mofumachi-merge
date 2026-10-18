using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
 public sealed class EastWestTownTests
 {
  [Test] public void DestinationsOpenWestAndEastFromTheCentralPlaza()
  {
   var center=TownWorldMap.Place("square");var cafe=TownWorldMap.Place("west.cafe-front");var flower=TownWorldMap.Place("shop");
   Assert.That(cafe.X,Is.LessThan(center.X-.5f));Assert.That(flower.X,Is.GreaterThan(center.X+.5f));
   Assert.That(Math.Abs(cafe.Y-flower.Y),Is.LessThan(.18f));
   Assert.That(TownWorldMap.Width/TownWorldMap.Height,Is.EqualTo(1.5f).Within(.001));
   Assert.That(TownWorldLayout.RegionAt(cafe),Is.EqualTo(TownRegionId.West));Assert.That(TownWorldLayout.RegionAt(flower),Is.EqualTo(TownRegionId.East));
  }
  [TestCase(0)] [TestCase(6)] public void EveryDestinationIsOnTheAuthoredFloorAndReachableAtEveryLevel(int level)
  {
   var floor=new TownWalkableArea(level);var nav=new TownAreaNavigation(floor);var start=TownWorldMap.Place("square");
   foreach(TownRegionId r in Enum.GetValues(typeof(TownRegionId)))foreach(var place in TownRegionCatalog.ForLevel(r,level).Map.Places)
   {var end=TownWorldMap.Place(place.Id);Assert.That(floor.Contains(end),Is.True,place.Id);var path=nav.FindRoute(start,end);Assert.That(path,Is.Not.Empty,place.Id);var p=start;foreach(var q in path){Assert.That(floor.CanTravel(p,q),Is.True,place.Id);p=q;}}
  }
  [Test] public void FountainAndStorefrontBuildingsAreNotWalkableShortcuts()
  {
   var f=new TownWalkableArea(6);
   foreach(var p in new[]{TownWorldMap.Point(.52f,.49f),TownWorldMap.Point(.27f,.4f),TownWorldMap.Point(.82f,.35f),TownWorldMap.Point(.35f,.63f),TownWorldMap.Point(.70f,.64f)})Assert.That(f.Contains(p),Is.False,p.X+","+p.Y);
   Assert.That(f.CanTravel(TownWorldMap.Point(.36f,.50f),TownWorldMap.Point(.70f,.50f)),Is.False);
  }
  [Test] public void WalkingCrossesBothHorizontalBoundariesWithoutAnInstantMove()
  {
   var t=new TownWorldWalkModel(6,"momo");t.FastTravel(TownRegionId.West);Assert.That(t.TryGuide("momo","east.meeting"),Is.True);bool central=false;var last=t.Hero.Position;
   for(int i=0;i<24000&&t.Hero.IsWalking;i++){t.Step(1f/30);Assert.That(TownWalkableArea.Distance(last,t.Hero.Position),Is.LessThanOrEqualTo(.00401f));central|=t.Hero.Region==TownRegionId.Central;last=t.Hero.Position;}
   Assert.That(t.Hero.IsWalking,Is.False);Assert.That(central,Is.True);Assert.That(t.Hero.Region,Is.EqualTo(TownRegionId.East));
  }
  [Test] public void LevelsAddUsefulStreetFurnitureAndStorefrontDressingWithoutChangingTheRules()
  {
   foreach(TownRegionId r in Enum.GetValues(typeof(TownRegionId)))
   {var first=TownGrowthPresentation.For(r,1);var middle=TownGrowthPresentation.For(r,3);var grown=TownGrowthPresentation.For(r,6);Assert.That(first.Count,Is.LessThan(middle.Count));Assert.That(middle.Count,Is.LessThan(grown.Count));Assert.That(grown.Any(p=>p.Prop.ToString()=="Bench"||p.Prop.ToString()=="FlowerDisplay"||p.Prop.ToString()=="CafeSign"),Is.True,r.ToString());}
  }
  [Test] public void EveryAuthoredStopKeepsTheWholeResidentAboveTheCompactFooter()
  {
   foreach(float height in new[]{592f,640f,740f,808f,1280f})
   {float scale=TownWorldLayout.ScaleForViewport(360,height);var camera=new TownWorldCamera();camera.Fit(360/scale,height/scale);
    foreach(TownRegionId r in Enum.GetValues(typeof(TownRegionId)))foreach(var stop in TownRegionCatalog.ForLevel(r,6).Map.Places)
    {var p=TownWorldMap.Place(stop.Id);camera.Jump(new TownPoint(p.X,p.Y+.04f));float feet=(p.Y-camera.Center.Y)*scale;
     Assert.That(feet-20,Is.GreaterThanOrEqualTo(-height/2+24+62),stop.Id+" below footer at "+height);
     Assert.That(feet+82,Is.LessThanOrEqualTo(height/2-32-80),stop.Id+" behind HUD at "+height);}
   }
  }
 }
}
