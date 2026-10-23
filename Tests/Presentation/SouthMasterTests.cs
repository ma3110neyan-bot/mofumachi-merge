using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class SouthMasterTests
    {
        private static Type Master(){var t=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownSouthMasterLayout");Assert.That(t,Is.Not.Null,"Finish the garden using the accepted town quality");return t;}
        private static IReadOnlyList<TownSceneryPlacement> Pieces()=>(IReadOnlyList<TownSceneryPlacement>)Master().GetProperty("Dressing").GetValue(null);
        private static bool Visible(TownSceneryPlacement p,int level)=>(bool)Master().GetMethod("VisibleAtLevel").Invoke(null,new object[]{p,level});
        [Test] public void InitialGardenHasOriginalGazeboRestSeatFlowersAndLanterns()
        {
            var pieces=Pieces();Assert.That(pieces.All(p=>p.District==TownDistrictId.South),Is.True);
            Assert.That(TownSceneryLayout.Landmarks.Single(p=>p.District==TownDistrictId.South).Kind,Is.EqualTo(TownSceneryKind.Gazebo));
            foreach(var kind in new[]{TownSceneryKind.GardenPergola,TownSceneryKind.FlowerBorder,TownSceneryKind.StreetLamp,TownSceneryKind.FlowerBasket})Assert.That(pieces.Any(p=>p.Kind==kind&&Visible(p,1)),Is.True,"Lv1 is already a finished garden: "+kind);
            Assert.That(TownVillageDressing.All.Any(p=>p.Kind==TownSceneryKind.ParkBench&&p.District==TownDistrictId.South),Is.True);
            Assert.That(Master().GetField("Resource").GetRawConstantValue(),Is.EqualTo(TownWestMasterLayout.Resource),"Share the accepted stone/flower palette without a new large texture");
        }
        [Test] public void StagedGardenReplacesItsPlantingWithoutNewFloorBlockers()
        {
            var pieces=Pieces();var floor=new TownWalkableArea(0);
            foreach(var p in pieces.Where(p=>p.Footprint!=null)){Assert.That(p.Level,Is.Zero);Assert.That(floor.Contains(p.Foundation),Is.False);}
            var site=pieces.Where(p=>p.Kind==TownSceneryKind.FlowerBorder&&p.Feet.Equals(new TownPoint(-.21f,-2.98f))).ToArray();Assert.That(site.Length,Is.EqualTo(3));
            float previous=0;foreach(int level in new[]{1,3,6}){var shown=site.Single(p=>Visible(p,level));Assert.That(shown.Width,Is.GreaterThan(previous));previous=shown.Width;}
            Assert.That(pieces.Any(p=>p.Kind==TownSceneryKind.FlowerArch&&Visible(p,1)),Is.False);
            Assert.That(pieces.Any(p=>p.Kind==TownSceneryKind.FlowerArch&&Visible(p,3)),Is.False);
            Assert.That(pieces.Any(p=>p.Kind==TownSceneryKind.FlowerArch&&Visible(p,6)),Is.True);
            var oldPergola=TownVillageDressing.All.Single(p=>p.Kind==TownSceneryKind.GardenPergola&&p.District==TownDistrictId.South);
            foreach(int level in new[]{1,3,6})Assert.That(pieces.Concat(new[]{oldPergola}).Count(p=>p.Kind==TownSceneryKind.GardenPergola&&Visible(p,level)),Is.EqualTo(1));
        }
        [Test] public void GardenCircuitAndRestPlacesRemainReachableInBothDirections()
        {
            var links=(IReadOnlyList<TownPoint[]>)Master().GetProperty("Circulation").GetValue(null);var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);
            Assert.That(links.Count,Is.GreaterThanOrEqualTo(5));
            foreach(var link in links)foreach(var points in new[]{link,link.Reverse().ToArray()})
            {var from=points[0];foreach(var to in points.Skip(1)){var route=nav.FindRoute(from,to);Assert.That(route,Is.Not.Empty);foreach(var p in route){Assert.That(floor.CanTravel(from,p),Is.True);from=p;}}}
        }
        [Test] public void LowGardenBanksLeaveResidentAndRestingVisitorNamesClear()
        {
            foreach(int level in new[]{1,3,6})foreach(var feet in new[]{TownWorldMap.Place("front-promenade"),TownWorldMap.Place("celebration"),new TownPoint(.20f,-2.20f),new TownPoint(0,-2.04f)})
            {
                var plate=new TownViewportRect(feet.X-.13f,feet.Y-.075f,.26f,.075f);
                foreach(var p in Pieces().Where(p=>Visible(p,level)&&p.Feet.Y<feet.Y))
                    Assert.That(p.Feet.X+p.Width/2>plate.X&&p.Feet.X-p.Width/2<plate.X+plate.Width&&p.Feet.Y+p.Height>plate.Y&&p.Feet.Y<plate.Y+plate.Height,Is.False,"Foreground garden must not cover the name: "+p.Kind);
            }
        }
        [TestCase(1)] [TestCase(3)] [TestCase(6)]
        public void ThreeVisitorsCanPassThroughTheGardenWithoutCongestion(int level)
        {
            var model=new TownWorldWalkModel(level,"momo");var arrived=new HashSet<string>();
            var targets=new Dictionary<string,TownPoint>{{"momo",new TownPoint(.20f,-2.20f)},{"lulu",TownWorldMap.Place("flowers")},{"noah",TownWorldMap.Place("square")}};
            model.Restore("momo",new TownPoint(0,-1.12f));model.Restore("lulu",new TownPoint(.38f,-1.50f));model.Restore("noah",TownWorldMap.Place("front-promenade"));
            model.Arrived+=(id,place,crossed)=>{if(targets.TryGetValue(id,out var target)&&TownWalkableArea.Distance(model.Residents.Single(a=>a.Id==id).Position,target)<.002f)arrived.Add(id);};
            Assert.That(model.TryGuidePoint(targets["momo"]),Is.True);Assert.That(model.TryGuide("lulu","flowers"),Is.True,"Use an unlocked NPC destination; the front-promenade activity unlock stays at Lv4");Assert.That(model.TryGuide("noah","square"),Is.True,"Use an unlocked destination; celebration stays level-gated");
            for(int i=0;i<3000&&arrived.Count<3;i++)
            {
                model.Step(1f/30);foreach(var actor in model.Residents)Assert.That(model.Area.Contains(actor.Position),Is.True);
                for(int a=0;a<model.Residents.Count;a++)for(int b=a+1;b<model.Residents.Count;b++)Assert.That(TownWalkableArea.Distance(model.Residents[a].Position,model.Residents[b].Position),Is.GreaterThan(.066f));
            }
            Assert.That(arrived,Is.EquivalentTo(targets.Keys));
        }
    }
}
