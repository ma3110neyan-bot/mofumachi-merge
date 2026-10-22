using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class EastMasterTests
    {
        private static Type Master()
        {
            var t=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownEastMasterLayout");
            Assert.That(t,Is.Not.Null,"East must compose the existing florist as a finished destination");return t;
        }
        private static IReadOnlyList<TownSceneryPlacement> Pieces()=>(IReadOnlyList<TownSceneryPlacement>)Master().GetProperty("Dressing").GetValue(null);
        private static bool Visible(TownSceneryPlacement p,int level)=>(bool)Master().GetMethod("VisibleAtLevel").Invoke(null,new object[]{p,level});
        [Test] public void InitialFlowerCartReplacesWithEarnedDisplayWithoutStacking()
        {
            var cart=Pieces().Single(p=>p.Kind==TownSceneryKind.FloristCart&&p.Feet.Equals(new TownPoint(1.50f,-.14f)));
            Assert.That(Visible(cart,1),Is.True);Assert.That(Visible(cart,2),Is.False);Assert.That(Visible(cart,6),Is.False);
            Assert.That(TownGrowthPresentation.For(TownRegionId.East,2).Count(p=>p.Prop==TownGrowthProp.FlowerDisplay&&TownWalkableArea.Distance(p.Position,cart.Feet)<.02f),Is.EqualTo(1));
            foreach(var site in new[]{new TownPoint(1.70f,-.67f),new TownPoint(.70f,-.58f)})
            foreach(int level in new[]{1,3,6})Assert.That(Pieces().Count(p=>p.Feet.Equals(site)&&Visible(p,level)),Is.EqualTo(1));
        }
        [Test] public void InitialFurnitureHasVisibleSolidBasesAndPreservesBothSideWalksAndShop()
        {
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);var start=TownWorldMap.Place("east.entry");
            foreach(var piece in Pieces().Where(p=>p.Footprint!=null))
            {Assert.That(piece.Level,Is.Zero);Assert.That(floor.ExcludedPolygons.Any(p=>ReferenceEquals(p,piece.Footprint)),Is.True);Assert.That(floor.Contains(piece.Foundation),Is.False);}
            foreach(var stop in new[]{TownWorldMap.Place("shop"),TownWorldMap.Place("east.flowers"),new TownPoint(1.70f,-.22f),new TownPoint(1.70f,.50f),new TownPoint(.84f,.50f),new TownPoint(.80f,.03f),TownWorldMap.Place("square")})
                Assert.That(nav.FindRoute(start,stop),Is.Not.Empty,"Keep both side walks, flower stop, real door and central open");
            Assert.That(Pieces().All(p=>p.District==TownDistrictId.East),Is.True);
        }
        [Test] public void LowFrontGardenKeepsTheNewBenchAndFlowerDisplayExposed()
        {
            var bed=TownSceneryLayout.Nature.Single(p=>p.Kind==TownSceneryKind.Flowerbed&&p.Foundation.Equals(new TownPoint(1.48f,-.08f)));
            var hedge=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.FlowerHedge&&p.Feet.Equals(new TownPoint(1.52f,-.84f)));
            var overlay=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(new TownPoint(1.49f,-.12f)));
            Assert.That(Visible(overlay,6),Is.False,"The earned flower display must not disappear under a generic flower sprite");
            var tree=TownSceneryLayout.Nature.Single(p=>p.Kind==TownSceneryKind.Tree&&p.Feet.Equals(new TownPoint(1.86f,-.72f)));
            foreach(int level in new[]{1,3,6}){Assert.That(Visible(bed,level),Is.False);Assert.That(Visible(hedge,level),Is.False);Assert.That(Visible(tree,level),Is.False);}
            Assert.That(Pieces().Any(p=>p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(hedge.Feet)&&p.Width>=.28f),Is.True);
            var bench=Pieces().Single(p=>p.Kind==TownSceneryKind.ParkBench);
            foreach(var p in Pieces().Where(p=>p.Kind==TownSceneryKind.FlowerBorder&&Math.Abs(p.Feet.X-bench.Feet.X)<.15f&&p.Feet.Y<bench.Feet.Y))
                Assert.That(p.Feet.Y+p.Height,Is.LessThan(bench.Feet.Y));
        }
        [Test] public void GroundSharesTheAcceptedPaintingButProjectsOnlyIntoEast()
        {
            var t=Master();Assert.That(t.GetField("Resource").GetRawConstantValue(),Is.EqualTo(TownWestMasterLayout.Resource));
            var uv=t.GetMethod("TexturePoint");var door=(TownPoint)uv.Invoke(null,new object[]{TownWorldMap.Place("shop")});Assert.That(door.X,Is.EqualTo(.5f).Within(.0001));
            var westJoin=TownWestMasterLayout.TexturePoint(new TownPoint(-.6f,-.24f));var eastJoin=(TownPoint)uv.Invoke(null,new object[]{new TownPoint(.6f,-.24f)});
            Assert.That(eastJoin.X,Is.EqualTo(westJoin.X).Within(.0001));Assert.That(eastJoin.Y,Is.EqualTo(westJoin.Y).Within(.0001));
            Assert.That((float)t.GetField("Left").GetRawConstantValue(),Is.EqualTo(.6f));Assert.That((float)t.GetField("Right").GetRawConstantValue(),Is.EqualTo(1.8f));
        }
    }
}
