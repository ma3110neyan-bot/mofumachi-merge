using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class VisualLockTownTests
    {
        [Test] public void InitialFountainViewHasSeatsOnBothSidesWithoutWaitingForGrowth()
        {
            // Pin real foreground destinations, not an arbitrary total sprite
            // count: clearing the park again must fail this regression.
            foreach(int side in new[]{-1,1})
                Assert.That(TownVillageDressing.All.Any(p=>p.Kind.ToString()=="ParkBench"&&p.Level==0&&p.Feet.X*side>.30f&&p.Feet.X*side<.55f&&p.Feet.Y>-.5f&&p.Feet.Y<.5f),Is.True,"Initial fountain seating on side "+side);
        }
        [Test] public void BranchWalksEndBesideAnInitialSeatOrStoreDisplay()
        {
            string[] purposes={"ParkBench","CafeTerrace","FloristCart","NoticeBoard","GardenPergola"};
            foreach(var walk in TownWorldSurface.LocalWalks)
            {
                var end=walk.Last();
                Assert.That(TownVillageDressing.All.Any(p=>p.Level==0&&purposes.Contains(p.Kind.ToString())&&TownWalkableArea.Distance(end,p.Feet)<.27f),Is.True,"Empty end at "+end.X+","+end.Y);
            }
        }
        [Test] public void AddedSolidDressingNeverCoversAnExistingPlaceOrEntrance()
        {
            var floor=new TownWalkableArea(0);
            foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
            foreach(var place in TownRegionCatalog.ForLevel(region,6).Map.Places)
                Assert.That(floor.Contains(TownWorldMap.Place(place.Id)),Is.True,place.Id);
            foreach(var district in TownDistrictLayout.All)
                Assert.That(floor.Contains(TownWorldMap.Place(TownDistrictLayout.EntranceId(district))),Is.True,district.ToString());
        }
        [Test] public void NamedRestStopsActuallyArriveBesideTheVisibleSeats()
        {
            foreach(string id in new[]{"bench-left","bench-right"})
            {
                var stop=TownWorldMap.Place(id);
                Assert.That(TownVillageDressing.All.Any(p=>p.Level==0&&(p.Kind==TownSceneryKind.ParkBench||p.Kind==TownSceneryKind.NoticeBoard)&&TownWalkableArea.Distance(stop,p.Feet)<.16f),Is.True,id+" cannot be an empty point far away from the seat");
            }
        }
    }
}
