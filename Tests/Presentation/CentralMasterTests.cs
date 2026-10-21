using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CentralMasterTests
    {
        private static Type Master()
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownCentralMasterLayout");
            Assert.That(type,Is.Not.Null,"The central master must be one authored composition, not the old repeated ground");return type;
        }
        private static IReadOnlyList<TownSceneryPlacement> Dressing()=>
            (IReadOnlyList<TownSceneryPlacement>)Master().GetProperty("Dressing").GetValue(null);
        [Test] public void FountainIsThePlazaFocusAndItsLargerBaseStillLeavesBothWalkingRoutes()
        {
            var fountain=TownSceneryLayout.Landmarks.Single(p=>p.Kind==TownSceneryKind.Fountain);
            Assert.That(fountain.Width/TownWorldLayout.ViewWidth,Is.InRange(.48f,.56f),"The approved fountain is a focal landmark, not a miniature inside an empty stone circle");
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);
            foreach(float side in new[]{-1f,1f})
            {
                Assert.That(floor.Contains(new TownPoint(side*.21f,.30f)),Is.False,"The enlarged flower rim cannot be walked through");
                var around=new TownPoint(side*.30f,.30f);
                Assert.That(floor.Contains(around),Is.True,"Keep independent routes on both sides");
                Assert.That(nav.FindRoute(TownWorldMap.Place("square"),around),Is.Not.Empty);
                Assert.That(nav.FindRoute(around,new TownPoint(0,.60f)),Is.Not.Empty);
            }
        }
        [Test] public void EachGrowthStageChangesGardensAboveTheActualPortraitFooter()
        {
            var all=Dressing();var visible=Master().GetMethod("VisibleAtLevel");
            foreach(float side in new[]{-1f,1f})
            {
                var bank=all.Where(p=>Math.Abs(p.Feet.X-side*.43f)<.001f&&Math.Abs(p.Feet.Y+.52f)<.001f).ToArray();
                Assert.That(bank.Select(p=>p.Level),Is.EquivalentTo(new[]{0,3,6}));
                float previous=0;
                foreach(int level in new[]{1,3,6})
                {
                    var shown=bank.Single(p=>(bool)visible.Invoke(null,new object[]{p,level}));
                    float feet=640+(.17f-shown.Feet.Y)*720/TownWorldLayout.ViewWidth;
                    Assert.That(feet,Is.LessThan(1160),"Replacement must be above the old footer at the same 720x1280 framing");
                    Assert.That(shown.Width,Is.GreaterThan(previous));previous=shown.Width;
                }
                Assert.That(bank.Where(p=>p.Footprint!=null).All(p=>p.Level==0),Is.True);
            }
        }
        [Test] public void CentralHasAFriendAtArrivalWhileEveryOtherDistrictStaysPopulated()
        {
            foreach(var character in CharacterCatalog.All)
            {
                var town=new TownWorldWalkModel(0,character.Id);
                Assert.That(town.Residents.Count(p=>p.District==TownDistrictId.Central),Is.GreaterThanOrEqualTo(2),"The central gathering place cannot start empty of friends");
                Assert.That(town.Residents.Select(p=>p.District).Distinct().Count(),Is.EqualTo(5));
                Assert.That(town.Residents.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(6));
                Assert.That(town.Residents.All(p=>town.Area.Contains(p.Position)),Is.True);
            }
        }
        [Test] public void FountainSideLampsReadAtCharacterScale()
        {
            foreach(float side in new[]{-1f,1f})
            {
                var lamp=TownVillageDressing.All.Single(p=>p.District==TownDistrictId.Central&&p.Kind==TownSceneryKind.StreetLamp&&Math.Abs(p.Feet.X-side*.47f)<.001f&&Math.Abs(p.Feet.Y-.35f)<.001f);
                Assert.That(lamp.Height*720/TownWorldLayout.ViewWidth,Is.GreaterThan(115),"Lamp silhouette should be visible beside residents at the reviewed framing");
            }
        }
        [Test] public void InitialGardenBanksHaveVisibleFoundationsRatherThanWalkThroughFlowers()
        {
            var floor=new TownWalkableArea(0);
            foreach(float side in new[]{-1f,1f})
            {
                var center=new TownPoint(side*.50f,-.87f);
                Assert.That(floor.Contains(center),Is.False,"The finished initial garden bank is a real boundary");
                Assert.That(floor.TryClamp(center,out var outside,.10f),Is.True);
                Assert.That(floor.Contains(outside),Is.True);
            }
        }
        [Test] public void GrowthUpgradesAnAlreadyFinishedCentralGardenAndNeverRemodelsAnotherDistrict()
        {
            var all=Dressing();Assert.That(all.All(p=>p.District==TownDistrictId.Central),Is.True);
            var visible=Master().GetMethod("VisibleAtLevel");
            foreach(float side in new[]{-1f,1f})
            {
                var garden=all.Where(p=>Math.Abs(p.Feet.X-side*.50f)<.001f&&Math.Abs(p.Feet.Y+.89f)<.001f).ToArray();
                Assert.That(garden.Select(p=>p.Level),Is.EquivalentTo(new[]{0,3,6}));
                foreach(int level in new[]{1,3,6})
                {
                    var shown=garden.Where(p=>(bool)visible.Invoke(null,new object[]{p,level})).ToArray();
                    Assert.That(shown.Length,Is.EqualTo(1),"Upgrade the garden instead of piling all three versions on it");
                    Assert.That(shown[0].Level,Is.EqualTo(level==1?0:level));
                }
                Assert.That(garden.Where(p=>p.Footprint!=null).All(p=>p.Level==0),Is.True,"No invisible future blocker");
            }
        }
        [Test] public void EveryNewCentralSolidIsSharedByRenderingAndNavigationFromTheFirstLevel()
        {
            var floor=new TownWalkableArea(0);
            foreach(var piece in Dressing().Where(p=>p.Footprint!=null))
            {
                Assert.That(piece.Level,Is.Zero);
                Assert.That(floor.ExcludedPolygons.Any(p=>ReferenceEquals(p,piece.Footprint)),Is.True);
                Assert.That(floor.Contains(piece.Foundation),Is.False);
            }
            var nav=new TownAreaNavigation(floor);var start=TownWorldMap.Place("square");
            foreach(var district in TownDistrictLayout.All)
                Assert.That(nav.FindRoute(start,TownWorldMap.Place(TownDistrictLayout.EntranceId(district))),Is.Not.Empty);
            foreach(float side in new[]{-.30f,.30f})Assert.That(nav.FindRoute(start,new TownPoint(side,.52f)),Is.Not.Empty);
        }
        [Test] public void PaintedStoneAnchorsUseFixedWorldCoordinatesAcrossCameraAndLevelChanges()
        {
            var uv=Master().GetMethod("TexturePoint");
            var fountain=(TownPoint)uv.Invoke(null,new object[]{new TownPoint(0,.30f)});
            Assert.That(fountain.X,Is.EqualTo(.5f).Within(.0001));
            Assert.That(1-fountain.Y,Is.InRange(.41f,.44f),"Measured center of the new painted fountain socket");
            foreach(float side in new[]{-.6f,.6f})
            {
                var exit=(TownPoint)uv.Invoke(null,new object[]{new TownPoint(side,-.24f)});
                Assert.That(1-exit.Y,Is.InRange(.64f,.69f),"The painted cross street must match the actual east/west approach");
            }
        }
    }
}
