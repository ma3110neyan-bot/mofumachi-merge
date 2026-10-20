using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class VillageCompositionTests
    {
        private static bool Inside(TownPoint p,TownPoint[] shape)
        {
            bool inside=false;
            for(int i=0,j=shape.Length-1;i<shape.Length;j=i++)
            {var a=shape[i];var b=shape[j];if((a.Y>p.Y)!=(b.Y>p.Y)&&p.X<(b.X-a.X)*(p.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;}
            return inside;
        }
        [Test] public void ForecourtsKeepLargeMeadowCornersBesideTheBranchedRoutes()
        {
            foreach(var p in new[]{new TownPoint(-.27f,-.50f),new TownPoint(.27f,-.50f),new TownPoint(-1.42f,-.46f),new TownPoint(1.42f,-.46f)})
                Assert.That(TownWorldSurface.PavingPolygons.Any(s=>Inside(p,s)),Is.False,"Court should leave meadow beside the route at "+p.X+","+p.Y);
        }
        [Test] public void VillageDressingHasAnExplicitSourceForRenderingAndCollision()
        {
            Assert.That(typeof(TownSceneryLayout).Assembly.GetType("Mofumachi.Presentation.TownVillageDressing"),Is.Not.Null,"Village compositions need shared declarative placements, not independent view-only scatter");
        }
        [Test] public void EverySolidVillageFoundationIsVisibleAtTheInitialStageAndSharedWithNavigation()
        {
            var floor=new TownWalkableArea(0);
            foreach(var p in TownVillageDressing.All.Where(p=>p.Footprint!=null))
            {
                Assert.That(p.Level,Is.Zero,"Do not reserve an invisible future building");
                Assert.That(floor.ExcludedPolygons.Any(s=>ReferenceEquals(s,p.Footprint)),Is.True,p.Kind.ToString());
                var midpoint=new TownPoint(p.Footprint.Average(v=>v.X),p.Footprint.Average(v=>v.Y));
                Assert.That(floor.Contains(midpoint),Is.False,p.Kind+" solid base");
                foreach(var edge in p.Footprint)Assert.That(floor.Contains(edge),Is.False,p.Kind+" footprint corner");
            }
        }
        [Test] public void EveryDistrictGainsALocalDestinationGroupAndFinalFlowerBanks()
        {
            foreach(var district in TownDistrictLayout.All)
            {
                var local=TownVillageDressing.All.Where(p=>p.District==district).ToArray();
                Assert.That(local.Any(p=>p.Level==0&&p.Kind==TownSceneryKind.CottagePair),Is.True,district+" village backdrop");
                Assert.That(local.Any(p=>p.Level==3&&p.Width>=.27f),Is.True,district+" middle-stage group");
                Assert.That(local.Any(p=>p.Level==6&&p.Width>=.30f),Is.True,district+" final flower bank");
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(3)] [TestCase(6)] public void EveryResidentStartsOnClearGroundForEveryFavorite(int level)
        {
            foreach(var favorite in CharacterCatalog.All)
            {
                var model=new TownWorldWalkModel(level,favorite.Id);
                foreach(var resident in model.Residents)
                    Assert.That(model.Area.Contains(resident.Position),Is.True,favorite.Id+" favorite: "+resident.Id);
            }
        }
        [Test] public void AllFiveDestinationEntriesStayConnectedAndBothFountainRoutesRemainOpen()
        {
            var floor=new TownWalkableArea(6);var nav=new TownAreaNavigation(floor);var start=TownWorldMap.Place("square");
            foreach(var district in TownDistrictLayout.All)
            {var point=TownWorldMap.Place(TownDistrictLayout.EntranceId(district));Assert.That(floor.Contains(point),Is.True);Assert.That(nav.FindRoute(start,point),Is.Not.Empty,district.ToString());}
            foreach(float x in new[]{-.30f,.30f})Assert.That(nav.FindRoute(start,new TownPoint(x,.52f)),Is.Not.Empty,"Fountain side "+x);
        }
    }
}
