using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class FinalFieldRecoveryTests
    {
        [TestCase(1)] [TestCase(3)] [TestCase(6)]
        public void EveryFloorPerimeterAndObstacleApronCanBeEnteredAndWalkedBack(int level)
        {
            var town=new TownWorldWalkModel(level,"miel");var home=TownWorldMap.Place("square");
            var targets=town.Area.FloorPolygons.SelectMany(f=>f.SelectMany((a,i)=>new[]{a,new TownPoint((a.X+f[(i+1)%f.Length].X)/2,(a.Y+f[(i+1)%f.Length].Y)/2)})).ToList();
            targets.AddRange(TownVillageDressing.All.Where(p=>p.Footprint!=null).SelectMany(p=>new[]{new TownPoint(p.Feet.X-p.Width/2-.04f,p.Feet.Y-.04f),new TownPoint(p.Feet.X+p.Width/2+.04f,p.Feet.Y-.04f)}));
            foreach(var proposed in targets)
            {
                if(!town.Area.TryClamp(proposed,out var target,.12f))continue;
                foreach(var destination in new[]{target,home})
                {
                    Assert.That(town.TryGuidePoint(destination),Is.True,$"Route to {destination.X},{destination.Y}");
                    for(int i=0;i<6000&&town.Hero.IsWalking;i++)town.Step(1f/30);
                    Assert.That(town.Hero.IsWalking,Is.False,$"Stalled near {town.Hero.Position.X},{town.Hero.Position.Y} toward {destination.X},{destination.Y}");
                    Assert.That(TownWalkableArea.Distance(town.Hero.Position,destination),Is.LessThan(.002f));
                }
            }
        }
        [Test] public void ThreeCoincidentResidentsCanSeparateAndReturnInEveryDistrict()
        {
            foreach(var district in TownDistrictLayout.All)
            {
                var town=new TownWorldWalkModel(6,"miel");var origin=TownWorldMap.Place(TownDistrictLayout.EntranceId(district));
                foreach(var actor in town.Residents.Take(3))town.Restore(actor.Id,origin);
                town.Restore(town.Hero.Id,origin);var home=TownWorldMap.Place("square");
                Assert.That(town.TryGuidePoint(home),Is.True);
                for(int i=0;i<6000&&town.Hero.IsWalking;i++)town.Step(1f/30);
                Assert.That(town.Hero.IsWalking,Is.False,district+" overlapping-resident escape");
                Assert.That(TownWalkableArea.Distance(town.Hero.Position,home),Is.LessThan(.002f));
            }
        }
    }
}
