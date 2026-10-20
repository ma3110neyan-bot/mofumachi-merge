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
        [Test] public void FountainApproachHasNoUnplantedGrassHoleBetweenItsTwoWalkingSides()
        {
            // The native Lv6 image showed an isolated green triangle here,
            // between the two side paths and the north-bound street.
            var floor=new TownWalkableArea(0);
            foreach(float x in new[]{-.015f,0,.015f})foreach(float y in new[]{.545f,.565f,.585f})
            {
                Assert.That(TownWorldSurface.PavingPolygons.Any(s=>Inside(new TownPoint(x,y),s)),Is.True,"Unplanted gap above the fountain at "+x+","+y);
                Assert.That(floor.Contains(new TownPoint(x,y)),Is.True,"The filled fountain approach must remain walkable");
            }
        }
        [Test] public void LocalWalksReachClearApronsWithContinuousPavingAndNavigation()
        {
            var floor=new TownWalkableArea(0);
            Assert.That(TownWorldSurface.LocalWalks.Count,Is.EqualTo(6),"Both sides of each shop and both request-plaza nooks need a clear approach");
            foreach(var walk in TownWorldSurface.LocalWalks)
            {
                Assert.That(walk.Length,Is.GreaterThanOrEqualTo(2));
                for(int i=1;i<walk.Length;i++)
                {
                    var a=walk[i-1];var b=walk[i];float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);
                    foreach(float side in new[]{-.9f,0,.9f})
                    {
                        float nx=-dy/length*TownWorldSurface.LocalWalkHalfWidth*side,ny=dx/length*TownWorldSurface.LocalWalkHalfWidth*side;
                        Assert.That(floor.CanTravel(new TownPoint(a.X+nx,a.Y+ny),new TownPoint(b.X+nx,b.Y+ny)),Is.True,"Local walking width must clear buildings and planting: "+a.X+","+a.Y+" to "+b.X+","+b.Y);
                    }
                    for(int sample=0;sample<=8;sample++)
                    {
                        float t=sample/8f;var point=new TownPoint(a.X+dx*t,a.Y+dy*t);
                        Assert.That(TownWorldSurface.PavingPolygons.Any(s=>Inside(point,s)),Is.True,"The declared walk must actually be paved");
                    }
                }
                var end=walk[walk.Length-1];
                for(int i=0;i<16;i++)
                {
                    double angle=i*Math.PI*2/16;var edge=new TownPoint(end.X+(float)Math.Cos(angle)*TownWorldSurface.LocalCourtRadius*.95f,end.Y+(float)Math.Sin(angle)*TownWorldSurface.LocalCourtRadius*.95f);
                    Assert.That(floor.CanTravel(end,edge),Is.True,"A rounded destination court must stay outside solid and planted islands");
                    Assert.That(TownWorldSurface.PavingPolygons.Any(s=>Inside(edge,s)),Is.True,"Local walks need rounded paved ends");
                }
            }
        }
        [Test] public void FirstCafeAndFloristGrowthFurnishingsStayBesideTheirActualEntrances()
        {
            foreach(var region in new[]{TownRegionId.West,TownRegionId.East})
            {
                var prop=TownGrowthPresentation.For(region,2).Single(p=>p.Prop==(region==TownRegionId.West?TownGrowthProp.CafeTable:TownGrowthProp.FlowerDisplay));
                var door=TownWorldMap.Place(region==TownRegionId.West?"west.cafe-front":"shop");
                Assert.That(Math.Abs(prop.Position.Y-door.Y),Is.LessThan(.25f),"Early growth should furnish the visible storefront, rather than the far lower edge");
                Assert.That(Math.Abs(prop.Position.X-door.X),Is.InRange(.25f,.39f),"Keep the entrance and street clear beside the furniture");
            }
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
