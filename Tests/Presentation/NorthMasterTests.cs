using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class NorthMasterTests
    {
        private static Type Master()
        {var t=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownNorthMasterLayout");Assert.That(t,Is.Not.Null,"Compose the existing quest cottage as a finished destination");return t;}
        private static IReadOnlyList<TownSceneryPlacement> Pieces()=>(IReadOnlyList<TownSceneryPlacement>)Master().GetProperty("Dressing").GetValue(null);
        private static bool Visible(TownSceneryPlacement p,int level)=>(bool)Master().GetMethod("VisibleAtLevel").Invoke(null,new object[]{p,level});
        [Test] public void SideFurnitureKeepsTheCottageAndBothWalkingApproachesReachable()
        {
            var pieces=Pieces();var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);var entry=TownWorldMap.Place("district.north.entry");
            Assert.That(pieces.All(p=>p.District==TownDistrictId.North),Is.True);
            foreach(var p in pieces.Where(p=>p.Footprint!=null))
            {Assert.That(p.Level,Is.Zero);Assert.That(floor.ExcludedPolygons.Any(f=>ReferenceEquals(f,p.Footprint)),Is.True);Assert.That(floor.Contains(p.Foundation),Is.False);}
            foreach(var stop in new[]{TownWorldMap.Place("quest"),TownWorldMap.Place("bench-left"),TownWorldMap.Place("home-front"),new TownPoint(-.50f,1.78f),new TownPoint(.50f,1.78f),new TownPoint(-.42f,2.60f),new TownPoint(.42f,2.60f),TownWorldMap.Place("square")})
                Assert.That(nav.FindRoute(entry,stop),Is.Not.Empty,"Keep both sides, the physical door, resting places and central open");
        }
        [Test] public void FinishedFlowerbedsReplaceAtEachStageWithoutNewInvisibleBases()
        {
            foreach(float side in new[]{-1f,1f})
            {
                var stages=Pieces().Where(p=>Math.Abs(p.Feet.X-side*.43f)<.001f&&Math.Abs(p.Feet.Y-1.72f)<.001f).ToArray();
                Assert.That(stages.Select(p=>p.Level),Is.EquivalentTo(new[]{0,3,6}));float width=0;
                foreach(int level in new[]{1,3,6}){var shown=stages.Single(p=>Visible(p,level));Assert.That(shown.Width,Is.GreaterThan(width));width=shown.Width;}
                Assert.That(stages.Single(p=>p.Footprint!=null).Level,Is.Zero);
            }
        }
        [Test] public void BoardAndFrontSeatStayExposedWithoutDeletingTheInitialGarden()
        {
            var boardOverlay=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(new TownPoint(-.45f,2.28f)));
            var hedge=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.FlowerHedge&&p.Feet.Equals(new TownPoint(.48f,1.24f)));
            Assert.That(Visible(boardOverlay,6),Is.False,"A flower upgrade must not hide the request board");
            foreach(int level in new[]{1,3,6})Assert.That(Visible(hedge,level),Is.False);
            var bank=Pieces().Single(p=>p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(hedge.Feet));var seat=Pieces().Single(p=>p.Kind==TownSceneryKind.ParkBench);
            Assert.That(bank.Width,Is.GreaterThanOrEqualTo(.30f));Assert.That(bank.Feet.Y+bank.Height,Is.LessThan(seat.Feet.Y));
        }
        [Test] public void AddedForegroundLeavesRestingResidentNamesReadable()
        {
            foreach(int level in new[]{1,3,6})foreach(string id in new[]{"bench-left","home-front"})
            {
                var feet=TownWorldMap.Place(id);var plate=new TownViewportRect(feet.X-.13f,feet.Y-.075f,.26f,.075f);
                foreach(var p in Pieces().Where(p=>Visible(p,level)&&p.Feet.Y<feet.Y))
                    Assert.That(p.Feet.X+p.Width/2>plate.X&&p.Feet.X-p.Width/2<plate.X+plate.Width&&p.Feet.Y+p.Height>plate.Y&&p.Feet.Y<plate.Y+plate.Height,Is.False,"Do not repeat the East name-card regression: "+p.Kind);
            }
        }
        [Test] public void GroundSharesTheAcceptedResourceAndRemainsWithinNorth()
        {
            var t=Master();Assert.That(t.GetField("Resource").GetRawConstantValue(),Is.EqualTo(TownWestMasterLayout.Resource));
            var uv=(TownPoint)t.GetMethod("TexturePoint").Invoke(null,new object[]{TownWorldMap.Place("quest")});Assert.That(uv.X,Is.EqualTo(.5f).Within(.0001));
            Assert.That((float)t.GetField("Left").GetRawConstantValue(),Is.EqualTo(-.6f));Assert.That((float)t.GetField("Right").GetRawConstantValue(),Is.EqualTo(.6f));Assert.That((float)t.GetField("Bottom").GetRawConstantValue(),Is.EqualTo(1f));
        }
    }
}
