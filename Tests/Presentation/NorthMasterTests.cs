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
        [Test] public void DeliveryDisplayGrowsBesideTheCottageWithoutOccupyingItsWaitingApron()
        {
            Assert.That(Enum.TryParse<TownSceneryKind>("DeliveryParcels",out var parcels),Is.True,"North needs a visible delivery destination, not only larger flowers");
            Assert.That(Enum.TryParse<TownSceneryKind>("DeliveryBunting",out var bunting),Is.True);
            Assert.That(Enum.TryParse<TownSceneryKind>("FlowerArch",out var arch),Is.True);
            Assert.That(TownVillageDressing.SpriteSource(bunting).Resource,Is.EqualTo("Mofumachi/TownDecorationAtlas"),"Use hanging flags, not new traversable planter-posts in the side walk");
            var pieces=Pieces();
            foreach(int level in new[]{1,3,6})
            {
                var display=pieces.Where(p=>p.Kind==parcels&&Visible(p,level)).ToArray();
                Assert.That(display.Length,Is.EqualTo(1),"Replace the parcel display rather than stacking duplicate sprites");
                Assert.That(display[0].Feet.Y,Is.GreaterThan(TownWorldMap.Place("quest").Y));
                Assert.That(Math.Abs(display[0].Feet.X),Is.LessThan(.3f),"Keep deliveries beside the cottage, not at the district edge");
                Assert.That(display[0].Feet.X-display[0].Width/2,Is.GreaterThanOrEqualTo(.085f),"Leave the physical door opening clear");
                Assert.That(display[0].Feet.X+display[0].Width/2,Is.LessThanOrEqualTo(.24001f),"Parcel boxes must remain on the existing cottage foundation");
                Assert.That(pieces.Any(p=>p.Kind==bunting&&Visible(p,level)),Is.EqualTo(level>=3));
                Assert.That(pieces.Any(p=>p.Kind==arch&&Visible(p,level)),Is.EqualTo(level>=6));
            }
            Assert.That(pieces.Single(p=>p.Kind==parcels&&Visible(p,3)).Width,Is.GreaterThan(pieces.Single(p=>p.Kind==parcels&&Visible(p,1)).Width*1.4f));
            // The forecourt remains a surface, with two independent waiting bays.
            var floor=new TownWalkableArea(1);
            foreach(float y in new[]{1.80f,1.90f,2.00f,2.06f})foreach(float x in new[]{-.24f,-.12f,0,.12f,.24f})
                Assert.That(floor.Contains(new TownPoint(x,y)),Is.True,"No new display may consume the usable apron");
            Assert.That(floor.CanTravel(new TownPoint(-.24f,1.8f),new TownPoint(-.24f,2.06f)),Is.True);
            Assert.That(floor.CanTravel(new TownPoint(.24f,1.8f),new TownPoint(.24f,2.06f)),Is.True);
        }
        [TestCase(1)] [TestCase(3)] [TestCase(6)]
        public void SeveralResidentsCanUseTheNorthCourtWithoutBlockingTheDoor(int level)
        {
            var town=new TownWorldWalkModel(level,"momo");
            town.Restore("momo",new TownPoint(0,1.80f));
            town.Restore("lulu",new TownPoint(-.22f,2.02f));
            town.Restore("pom",new TownPoint(.22f,1.95f));
            town.Restore("fio",new TownPoint(0,2.08f));
            Assert.That(town.TryGuide("lulu","home-front"),Is.True);
            Assert.That(town.TryGuide("pom","bench-left"),Is.True);
            Assert.That(town.TryGuide("momo","quest"),Is.True);
            for(int frame=0;frame<3000&&town.Hero.IsWalking;frame++)
            {
                town.Step(1f/30);
                foreach(var actor in town.Residents)Assert.That(town.Area.Contains(actor.Position),Is.True);
                for(int a=0;a<town.Residents.Count;a++)for(int b=a+1;b<town.Residents.Count;b++)
                    Assert.That(TownWalkableArea.Distance(town.Residents[a].Position,town.Residents[b].Position),Is.GreaterThan(.066f),"Residents must pass, yield or wait instead of overlapping feet");
            }
            Assert.That(town.Hero.IsWalking,Is.False,"The occupied doorway must be released for the visitor");
            Assert.That(TownWalkableArea.Distance(town.Hero.Position,TownWorldMap.Place("quest")),Is.LessThan(.002f));
        }
        [Test] public void FloralDoorwayUsesTheExistingFoundationAndLeavesTheVisitorInFront()
        {
            var depth=Master().GetMethod("DepthY");Assert.That(depth,Is.Not.Null,"Attached doorway flowers need a foreground layer independent of their solid base");
            var arch=Pieces().Single(p=>p.Kind.ToString()=="FlowerArch");var floor=new TownWalkableArea(1);
            foreach(float side in new[]{-1f,1f})Assert.That(floor.Contains(new TownPoint(arch.Feet.X+side*arch.Width*.42f,arch.Feet.Y)),Is.False,"Arch feet must sit on the existing cottage foundation");
            var cottage=TownSceneryLayout.Landmarks.Single(p=>p.Kind==TownSceneryKind.QuestCottage);
            Assert.That((float)depth.Invoke(null,new object[]{arch}),Is.InRange(TownWorldMap.Place("quest").Y+.005f,cottage.Feet.Y-.005f),"The flowers frame the cottage but never paint over the arriving visitor");
            foreach(var p in Pieces().Where(p=>Math.Abs(p.Feet.X)<.25f&&(p.Kind.ToString()=="DeliveryParcels"||p.Kind.ToString()=="FlowerBasket")))
                Assert.That((float)depth.Invoke(null,new object[]{p}),Is.InRange(TownWorldMap.Place("quest").Y+.005f,cottage.Feet.Y-.005f),"Cottage-attached displays must not disappear behind the facade");
            foreach(var p in TownSceneryLayout.Landmarks.Where(p=>p.District!=TownDistrictId.North))Assert.That(depth.Invoke(null,new object[]{p}),Is.EqualTo(p.Feet.Y));
        }
        [Test] public void RequestedNorthLinksUseDifferentOriginsAndBypassTheDistrictEntry()
        {
            var property=Master().GetProperty("Circulation");Assert.That(property,Is.Not.Null,"Export actual pair-to-pair routes, not five arrows from one entry");
            var links=(IReadOnlyList<TownPoint[]>)property.GetValue(null);Assert.That(links.Count,Is.GreaterThanOrEqualTo(5));
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);var starts=links.Select(p=>p[0]).Distinct().ToArray();
            Assert.That(starts.Length,Is.GreaterThanOrEqualTo(3));
            foreach(var link in links)for(int direction=0;direction<2;direction++)
            {
                var points=direction==0?link:link.Reverse().ToArray();var from=points[0];
                foreach(var to in points.Skip(1))
                {
                    var route=nav.FindRoute(from,to);Assert.That(route,Is.Not.Empty);
                    foreach(var p in route){Assert.That(p.Y,Is.GreaterThan(1.72f),"Side-to-side visits must not funnel back to the entry");Assert.That(floor.CanTravel(from,p),Is.True);from=p;}
                }
            }
        }
        [Test] public void LevelThreeAddsBoardDecorationWithoutChangingAnyInitialOrLevelSixLandmark()
        {
            var board=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.NoticeBoard&&p.District==TownDistrictId.North);
            var additions=Pieces().Where(p=>p.Level==3&&Math.Abs(p.Feet.X-board.Feet.X)<.18f&&Math.Abs(p.Feet.Y-board.Feet.Y)<.20f).ToArray();
            Assert.That(additions.Any(p=>p.Kind==TownSceneryKind.StreetLamp),Is.True,"A board-side lantern makes the intermediate stage visible");
            Assert.That(additions.Any(p=>p.Kind==TownSceneryKind.FlowerBasket),Is.True);
            Assert.That(additions.All(p=>p.Footprint==null),Is.True,"Attach decorations to the existing planted board foundation");
            foreach(var p in additions){Assert.That(Visible(p,1),Is.False);Assert.That(Visible(p,3),Is.True);Assert.That(Visible(p,6),Is.True);}
        }
        [TestCase(1)] [TestCase(3)] [TestCase(6)]
        public void ThreeDifferentNorthOriginsCompleteVisitsWithoutUsingTheEntry(int level)
        {
            var town=new TownWorldWalkModel(level,"momo");var arrived=new HashSet<string>();
            var expected=new Dictionary<string,string>{{"momo","home-front"},{"lulu","quest"},{"pom","bench-left"}};
            town.Restore("momo",TownWorldMap.Place("quest"));town.Restore("lulu",new TownPoint(-.42f,2.60f));town.Restore("pom",new TownPoint(.42f,2.60f));
            town.Arrived+=(id,place,crossed)=>{if(expected.TryGetValue(id,out var target)&&place==target)arrived.Add(id);};
            foreach(var visit in expected)Assert.That(town.TryGuide(visit.Key,visit.Value),Is.True);
            for(int i=0;i<3000&&arrived.Count<3;i++)
            {
                town.Step(1f/30);
                foreach(var actor in town.Residents.Where(a=>expected.ContainsKey(a.Id)&&!arrived.Contains(a.Id)))Assert.That(actor.Position.Y,Is.GreaterThan(1.72f));
                for(int a=0;a<town.Residents.Count;a++)for(int b=a+1;b<town.Residents.Count;b++)Assert.That(TownWalkableArea.Distance(town.Residents[a].Position,town.Residents[b].Position),Is.GreaterThan(.066f));
            }
            Assert.That(arrived,Is.EquivalentTo(expected.Keys),"All three visits must finish, not only the controlled resident");
        }
    }
}
