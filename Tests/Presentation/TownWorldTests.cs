using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownWorldTests
    {
        [Test] public void NorthAndSouthReuseStableIdsOnOneVerticalCoordinateSystem()
        {
            var p=new TownPoint(.4f,.5f);
            var north=TownWorldLayout.ToWorld(TownRegionId.West,p);var center=TownWorldLayout.ToWorld(TownRegionId.Central,p);var south=TownWorldLayout.ToWorld(TownRegionId.East,p);
            Assert.That(north.Y,Is.GreaterThan(center.Y));Assert.That(south.Y,Is.LessThan(center.Y));
            foreach(TownRegionId r in Enum.GetValues(typeof(TownRegionId)))
            {var round=TownWorldLayout.ToLocal(r,TownWorldLayout.ToWorld(r,p));Assert.That(round.X,Is.EqualTo(p.X).Within(.00001));Assert.That(round.Y,Is.EqualTo(p.Y).Within(.00001));}
        }
        [Test] public void HeroWalksNorthToSouthContinuouslyThroughTheCentralPark()
        {
            var town=new TownWorldWalkModel(6,"momo");town.FastTravel(TownRegionId.West);
            Assert.That(town.TryGuide("momo","east.flowers"),Is.True);var previous=town.Hero.Position;bool sawCenter=false;int arrived=0;town.Arrived+=(id,place,walked)=>{if(id=="momo"&&walked)arrived++;};
            for(int i=0;i<18000&&town.Hero.IsWalking;i++)
            {town.Step(1f/30);var p=town.Hero.Position;Assert.That(Distance(previous,p),Is.LessThanOrEqualTo(.12f/30+.00001f));if(town.Hero.Region==TownRegionId.Central)sawCenter=true;previous=p;}
            Assert.That(sawCenter,Is.True);Assert.That(town.Hero.Region,Is.EqualTo(TownRegionId.East));Assert.That(town.Hero.IsWalking,Is.False);Assert.That(arrived,Is.EqualTo(1));Assert.That(town.Residents.Count,Is.EqualTo(6));
        }
        [Test] public void MidPathRetargetAndCancelNeverTeleportTheHero()
        {
            var town=new TownWorldWalkModel(0,"noah");town.TryGuide("noah","west.flowers");for(int i=0;i<300;i++)town.Step(1f/30);
            var p=town.Hero.Position;Assert.That(town.TryGuide("noah","east.flowers"),Is.True);Assert.That(Distance(p,town.Hero.Position),Is.Zero);
            town.CancelHero();Assert.That(Distance(p,town.Hero.Position),Is.Zero);Assert.That(town.Hero.IsWalking,Is.False);
            foreach(float dt in new[]{float.NaN,float.PositiveInfinity,-1,0})town.Step(dt);Assert.That(Distance(p,town.Hero.Position),Is.Zero);
        }
        [Test] public void OffRoadAndUnknownTargetsDoNotStartOrProgressAVisit()
        {var t=new TownWorldWalkModel(0,"lulu");Assert.That(t.TryGuide("lulu","missing"),Is.False);Assert.That(t.TryGuidePoint(new TownPoint(100,100)),Is.False);Assert.That(t.TryGuidePoint(new TownPoint(float.NaN,0)),Is.False);Assert.That(t.Hero.IsWalking,Is.False);}
        [Test] public void FastTravelIsDistinctFromCompletedWalkingAndCameraRemainsFinite()
        {
            var t=new TownWorldWalkModel(6,"momo");int walks=0;t.Arrived+=(id,place,walked)=>{if(walked&&id=="momo")walks++;};t.FastTravel(TownRegionId.East);Assert.That(walks,Is.Zero);Assert.That(t.Hero.Region,Is.EqualTo(TownRegionId.East));
            var c=new TownWorldCamera();c.Follow(t.Hero.Position,1f/30);Assert.That(float.IsFinite(c.Center.X)&&float.IsFinite(c.Center.Y),Is.True);c.Follow(new TownPoint(float.NaN,0),.1f);Assert.That(float.IsFinite(c.Center.Y),Is.True);
            Assert.That(TownWorldLayout.Name(TownRegionId.Central),Is.EqualTo("中央公園"));
        }
        [Test] public void SpecialDialogueIsDiscoveredOnlyAfterSixConversations()
        {
            foreach(var c in CharacterCatalog.All)
            {
                string special=TownCharacterDialogue.For(c.Id,6);
                for(int i=0;i<6;i++)Assert.That(TownCharacterDialogue.For(c.Id,i),Is.Not.EqualTo(special));
                Assert.That(TownCharacterDialogue.For(c.Id,9),Is.EqualTo(special));
            }
        }
        [Test] public void PaintedConnectingRoadsContinueToTheNeighbourInsteadOfStoppingAtTheEdge()
        {
            var town=new TownWorldWalkModel(0,"momo");Assert.That(town.TryGuidePoint(new TownPoint(.06f,.72f)),Is.True,"The painted connector needs a finger-sized touch corridor");town.CancelHero();Assert.That(town.TryGuidePoint(new TownPoint(.13f,.72f)),Is.True);
            Assert.That(town.Hero.PlaceId,Is.EqualTo("west.flowers"));town.CancelHero();
            Assert.That(town.TryGuidePoint(new TownPoint(.52f,.32f)),Is.True);Assert.That(town.Hero.PlaceId,Is.EqualTo("east.flowers"));
            town.FastTravel(TownRegionId.West);Assert.That(town.TryGuidePoint(new TownPoint(.94f,1.45f)),Is.True);Assert.That(town.Hero.PlaceId,Is.EqualTo("square"));
        }
        [Test] public void AutonomousResidentsActuallyVisitANeighbourAndCanMeetOnSharedRoads()
        {
            var t=new TownWorldWalkModel(6,"momo");var npcs=t.Residents.Where(a=>!a.Controlled).ToArray();var starts=npcs.Select(a=>a.Region).ToArray();bool crossed=false,met=false;
            for(int i=0;i<9000;i++)
            {t.Step(1f/30);for(int a=0;a<npcs.Length;a++){crossed|=npcs[a].Region!=starts[a];for(int b=a+1;b<npcs.Length;b++)if(npcs[a].Region==npcs[b].Region&&TownWorldLayout.Distance(npcs[a].Position,npcs[b].Position)<.09f)met=true;}}
            Assert.That(crossed,Is.True,"Residents never crossed: "+string.Join("; ",npcs.Select(n=>n.Id+":"+n.Region+" "+n.PlaceId+" "+n.Position.X+","+n.Position.Y)));Assert.That(met,Is.True,"Residents never met");
        }
        [TestCase(0)] [TestCase(6)] public void NpcStepsStayWithinTheirWalkingBudgetIncludingArrivals(int level)
        {
            var town=new TownWorldWalkModel(level,"momo");var npcs=town.Residents.Where(a=>!a.Controlled).ToArray();
            var previous=npcs.Select(a=>a.Position).ToArray();bool crossed=false;var initial=npcs.Select(a=>a.Region).ToArray();
            for(int frame=0;frame<9000;frame++)
            {
                town.Step(1f/30);
                for(int i=0;i<npcs.Length;i++)
                {
                    Assert.That(TownWorldLayout.Distance(previous[i],npcs[i].Position),Is.LessThanOrEqualTo(.047f/30+.00001f),npcs[i].Id+" at frame "+frame+" (including the final arrival step)");
                    Assert.That(town.Area.Contains(npcs[i].Position),Is.True);crossed|=npcs[i].Region!=initial[i];previous[i]=npcs[i].Position;
                }
            }
            Assert.That(crossed,Is.True,"NPCs must really cross a region boundary during this check");
        }
        private static float Distance(TownPoint a,TownPoint b)=>(float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        [Test] public void MeetingPausesBothResidentsThenResumesTheirOriginalWalk()
        {
            var town=new TownWorldWalkModel(6,"momo");var npc=town.Residents.First(a=>!a.Controlled);
            town.TryGuide(npc.Id,"square");var p=npc.Position;
            town.Pause(npc.Id,1.2f);for(int i=0;i<30;i++)town.Step(1f/30);
            Assert.That(Distance(p,npc.Position),Is.Zero);Assert.That(npc.IsWalking,Is.True);
            for(int i=0;i<30;i++)town.Step(1f/30);Assert.That(Distance(p,npc.Position),Is.GreaterThan(0));
        }
        [Test] public void ResidentsReserveDistinctStopsAndFastTravelUsesAnEntry()
        {
            var town=new TownWorldWalkModel(6,"momo");town.FastTravel(TownRegionId.West);
            Assert.That(town.Hero.PlaceId,Is.EqualTo("west.entry"));
            for(int i=0;i<12000;i++)
            {
                town.Step(1f/30);var stops=town.Residents.Where(a=>!a.Controlled&&a.PlaceId!=null).Select(a=>a.PlaceId).ToArray();
                Assert.That(stops.Distinct().Count(),Is.EqualTo(stops.Length));
            }
        }
    }
}
