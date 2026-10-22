using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class WestMasterTests
    {
        private static Type Master()
        {
            var t=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownWestMasterLayout");
            Assert.That(t,Is.Not.Null,"West must have its own calibrated composition, not bare repeated lawn");return t;
        }
        private static IReadOnlyList<TownSceneryPlacement> Pieces()=>
            (IReadOnlyList<TownSceneryPlacement>)Master().GetProperty("Dressing").GetValue(null);
        private static bool Visible(TownSceneryPlacement p,int level)=>
            (bool)Master().GetMethod("VisibleAtLevel").Invoke(null,new object[]{p,level});
        [Test] public void InitialPatioReplacesWithEarnedPatioInsteadOfStacking()
        {
            var patio=Pieces().Single(p=>p.Kind==TownSceneryKind.CafeTerrace&&p.Feet.Equals(new TownPoint(-1.5f,-.12f)));
            Assert.That(Visible(patio,1),Is.True);Assert.That(Visible(patio,2),Is.False);Assert.That(Visible(patio,6),Is.False);
            Assert.That(TownGrowthPresentation.For(TownRegionId.West,2).Count(p=>p.Prop==TownGrowthProp.CafeTable&&p.Position.Equals(patio.Feet)),Is.EqualTo(1));
            foreach(int level in new[]{1,3,6})Assert.That(Pieces().Where(p=>Visible(p,level)).GroupBy(p=>(p.Kind,p.Feet)).All(g=>g.Count()==1),Is.True);
        }
        [Test] public void NewFurnitureHasVisibleFoundationsAndKeepsBothSideWalksAndDoorReachable()
        {
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);var start=TownWorldMap.Place("west.entry");
            foreach(var piece in Pieces().Where(p=>p.Footprint!=null))
            {
                Assert.That(piece.Level,Is.Zero);Assert.That(floor.ExcludedPolygons.Any(p=>ReferenceEquals(p,piece.Footprint)),Is.True);
                Assert.That(floor.Contains(piece.Foundation),Is.False);
            }
            foreach(var stop in new[]{TownWorldMap.Place("west.cafe-front"),new TownPoint(-1.70f,-.22f),new TownPoint(-1.70f,.50f),new TownPoint(-.80f,.03f),new TownPoint(-.84f,.50f),TownWorldMap.Place("square")})
                Assert.That(nav.FindRoute(start,stop),Is.Not.Empty,"West court, both side walks and central must remain open: "+stop.X+","+stop.Y);
            Assert.That(Pieces().All(p=>p.District==TownDistrictId.West),Is.True);
        }
        [Test] public void BenchAndLampsFrameTheForecourtWithoutHidingIt()
        {
            var pieces=Pieces();Assert.That(pieces.Any(p=>p.Kind==TownSceneryKind.ParkBench&&p.Feet.Y<-.25f&&p.Feet.Y>-.60f),Is.True);
            Assert.That(pieces.Count(p=>p.Kind==TownSceneryKind.StreetLamp&&p.Width>=.07f),Is.GreaterThanOrEqualTo(2));
            var bench=pieces.Single(p=>p.Kind==TownSceneryKind.ParkBench);
            foreach(var bed in pieces.Where(p=>p.Kind==TownSceneryKind.FlowerBorder&&Math.Abs(p.Feet.X-bench.Feet.X)<.15f&&p.Feet.Y<bench.Feet.Y))
                Assert.That(bed.Feet.Y+bed.Height,Is.LessThan(bench.Feet.Y),"Front flowers must not swallow the bench");
        }
        [Test] public void LegacyPlantingLeavesThePatioAndNewBenchExposed()
        {
            var patioBed=TownSceneryLayout.Nature.Single(p=>p.Kind==TownSceneryKind.Flowerbed&&p.Foundation.Equals(new TownPoint(-1.5f,-.08f)));
            var oldBank=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.FlowerHedge&&p.Feet.Equals(new TownPoint(-1.5f,-.72f)));
            foreach(int level in new[]{1,3,6}){Assert.That(Visible(patioBed,level),Is.False);Assert.That(Visible(oldBank,level),Is.False);}
            Assert.That(Pieces().Any(p=>p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(oldBank.Feet)&&p.Width>=.28f),Is.True,"Keep a finished low bed, not a deleted verge");
        }
        [Test] public void GroundStaysInWestAndMatchesTheSharedCrossStreet()
        {
            var t=Master();var uv=t.GetMethod("TexturePoint");
            var door=(TownPoint)uv.Invoke(null,new object[]{TownWorldMap.Place("west.cafe-front")});Assert.That(door.X,Is.EqualTo(.5f).Within(.0001f));
            foreach(float x in new[]{-1.8f,-.6f}){var point=(TownPoint)uv.Invoke(null,new object[]{new TownPoint(x,-.24f)});Assert.That(1-point.Y,Is.InRange(.64f,.69f));}
            Assert.That((float)t.GetField("Left").GetRawConstantValue(),Is.EqualTo(-1.8f));
            Assert.That((float)t.GetField("Right").GetRawConstantValue(),Is.EqualTo(-.6f));
        }
    }
}
