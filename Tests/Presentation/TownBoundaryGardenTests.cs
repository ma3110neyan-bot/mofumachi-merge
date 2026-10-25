using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownBoundaryGardenTests
    {
        [Test] public void OriginalTwoHundredSpritesKeepTheirMirrorOrientationWhenOuterPlantingIsAdded()
        {
            var method=typeof(TownBoundaryGardenLayout).GetMethod("IsMirrored");
            Assert.That(method,Is.Not.Null,"Sorted ordinal must not flip existing art when new cap sprites are inserted ahead of it");
            var originals=Plants().Where(p=>!(p.Feet.Y>=3.15f&&Math.Abs(p.Feet.X)<.84f)&&Math.Abs(Math.Abs(p.Feet.X)-2.035f)>.00001f).ToArray();
            Assert.That(originals.Length,Is.EqualTo(200),"All original boundary placements must remain");
            for(int i=0;i<originals.Length;i++)
                Assert.That((bool)method.Invoke(null,new object[]{originals[i]}),Is.EqualTo((i&1)!=0),"Original UV orientation at "+originals[i].Feet.X+","+originals[i].Feet.Y);
        }
        [Test] public void TrackedEdgeCameraDoesNotRevealAnUnplantedNorthCapOrOuterSideStrip()
        {
            var plants=Plants();
            foreach(var point in new[]{new TownPoint(0,3.30f),new TownPoint(.40f,3.60f),new TownPoint(-.40f,3.80f),new TownPoint(-1.88f,-.12f),new TownPoint(1.88f,-.12f)})
                Assert.That(plants.Any(p=>point.X>=p.Feet.X-p.Width/2&&point.X<=p.Feet.X+p.Width/2&&point.Y>=p.Feet.Y&&point.Y<=p.Feet.Y+p.Height),Is.True,"The actual tracked-edge camera exposes bare ground here: "+point.X+","+point.Y);
        }
        private static IReadOnlyList<TownSceneryPlacement> Plants()
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownBoundaryGardenLayout");
            Assert.That(type,Is.Not.Null,"Finish the empty outside banks with original plants, without shrinking the walking floor");
            return (IReadOnlyList<TownSceneryPlacement>)type.GetProperty("Dressing").GetValue(null);
        }
        [Test] public void AddedPlantRectanglesNeverCoverAnyOfTheFiveWalkingRooms()
        {
            var floors=new TownWalkableArea(0).FloorPolygons;
            foreach(var p in Plants())foreach(var floor in floors)
            {
                bool separated=p.Feet.X+p.Width/2<=floor.Min(v=>v.X)||p.Feet.X-p.Width/2>=floor.Max(v=>v.X)||p.Feet.Y+p.Height<=floor.Min(v=>v.Y)||p.Feet.Y>=floor.Max(v=>v.Y);
                Assert.That(separated,Is.True,"The complete sprite, including its crown, must stay outside the floor: "+p.Feet.X+","+p.Feet.Y);
                Assert.That(p.Footprint,Is.Null,"A painted bank must not add a hidden collision shape");
            }
        }
        [Test] public void AllFourExposedCornerBanksHaveContinuousPlanting()
        {
            var plants=Plants();
            foreach(float side in new[]{-1f,1f})foreach(var point in new[]{new TownPoint(side*.9f,1.4f),new TownPoint(side*1.25f,1.9f),new TownPoint(side*1.1f,2.6f),new TownPoint(side*1.1f,-1.3f),new TownPoint(side*1.4f,-1.9f),new TownPoint(side*1.1f,-2.7f)})
                Assert.That(plants.Any(p=>point.X>=p.Feet.X-p.Width/2&&point.X<=p.Feet.X+p.Width/2&&point.Y>=p.Feet.Y&&point.Y<=p.Feet.Y+p.Height),Is.True,"Exposed bank remains bare at "+point.X+","+point.Y);
        }
        [Test] public void BorderIsCompleteAtLevelOneWithOneBoundedStaticBatch()
        {
            var plants=Plants();Assert.That(plants.Count,Is.InRange(80,256));
            Assert.That(plants.All(p=>p.Level==0&&p.Width>0&&p.Height>0),Is.True);
            Assert.That(plants.Any(p=>p.Kind==TownSceneryKind.Tree),Is.True);Assert.That(plants.Any(p=>p.Kind==TownSceneryKind.Blossom),Is.True);
            Assert.That(plants.Any(p=>p.Kind==TownSceneryKind.Flowerbed),Is.True);Assert.That(plants.Any(p=>p.Kind==TownSceneryKind.Fence),Is.True);
            Assert.That(plants.Count*4,Is.LessThanOrEqualTo(1024));
        }
    }
}
