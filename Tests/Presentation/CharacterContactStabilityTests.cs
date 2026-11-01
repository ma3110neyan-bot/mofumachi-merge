using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CharacterContactStabilityTests
    {
        [TestCase("noah")][TestCase("momo")][TestCase("lulu")][TestCase("pom")][TestCase("fio")][TestCase("miel")]
        public void PassingAStationaryNeighbourDoesNotAlternateSidesteps(string id)
        {
            var model=Arrange(id,new TownPoint(-.20f,-.22f),new TownPoint(0,-.30f),out var peer);
            Assert.That(model.TryGuidePoint(new TownPoint(.16f,-.22f)),Is.True);
            CheckTrip(model,model.Hero,peer,holdPeer:true);
        }

        [Test] public void OccupiedGoalNearAnObstacleWaitsAndRecoversWithoutRapidReversals()
        {
            var model=Arrange("momo",new TownPoint(.30f,.10f),new TownPoint(.30f,.30f),out var peer);
            // A neighbour can occupy the goal after a route was accepted.
            Assert.That(Guide(model,model.Hero,new TownPoint(.30f,.34f)),Is.True);
            CheckTrip(model,model.Hero,peer,holdPeer:false);
        }

        [Test] public void NpcPassingAStationaryNeighbourDoesNotAlternateSidesteps()
        {
            var model=Arrange("noah",new TownPoint(-.20f,-.22f),new TownPoint(0,-.30f),out var peer);
            model.Restore("noah",new TownPoint(-1.8f,-.4f));
            model.Restore("momo",new TownPoint(-.20f,-.22f));var actor=model.Residents.Single(r=>r.Id=="momo");
            Assert.That(Guide(model,actor,new TownPoint(.16f,-.22f)),Is.True);
            CheckTrip(model,actor,peer,holdPeer:true);
        }

        [Test] public void NewDestinationAndRestoreAllowAnImmediateIntentionalReverse()
        {
            var model=Arrange("momo",new TownPoint(-.20f,-.30f),new TownPoint(.30f,-.50f),out var peer);
            Assert.That(model.TryGuidePoint(new TownPoint(.20f,-.30f)),Is.True);
            for(int i=0;i<12;i++){HoldOthers(model,model.Hero,peer,true);model.Step(1f/30);}
            Assert.That(model.Hero.Position.X,Is.GreaterThan(model.Hero.Previous.X));
            Assert.That(model.TryGuidePoint(new TownPoint(-.30f,-.30f)),Is.True);
            model.Step(1f/30);Assert.That(model.Hero.Position.X,Is.LessThan(model.Hero.Previous.X));
            model.CancelHero();model.Restore("momo",new TownPoint(.15f,-.30f));
            Assert.That(model.TryGuidePoint(new TownPoint(.25f,-.30f)),Is.True);
            model.Step(1f/30);Assert.That(model.Hero.Position.X,Is.GreaterThan(model.Hero.Previous.X));
        }

        private static TownWorldWalkModel Arrange(string id,TownPoint start,TownPoint neighbour,out TownWorldResident peer)
        {
            var model=new TownWorldWalkModel(1,id);string other=id=="lulu"?"momo":"lulu";
            foreach(var resident in model.Residents)model.Restore(resident.Id,new TownPoint(-1.8f,-.4f));
            model.Restore(id,start);model.Restore(other,neighbour);peer=model.Residents.Single(r=>r.Id==other);return model;
        }
        private static bool Guide(TownWorldWalkModel model,TownWorldResident actor,TownPoint target)
            =>(bool)typeof(TownWorldWalkModel).GetMethod("Guide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(model,new object[]{actor,target,null,true,false});
        private static void HoldOthers(TownWorldWalkModel model,TownWorldResident actor,TownWorldResident peer,bool holdPeer)
        {foreach(var r in model.Residents.Where(r=>r!=actor&&(r!=peer||holdPeer||!r.IsWalking)))model.Pause(r.Id,4);}
        private static void CheckTrip(TownWorldWalkModel model,TownWorldResident actor,TownWorldResident peer,bool holdPeer)
        {
            float px=0,py=0;var reversals=new Queue<int>();var previousTarget=actor.Target;
            for(int tick=0;tick<900&&actor.IsWalking;tick++)
            {
                HoldOthers(model,actor,peer,holdPeer);model.Step(1f/30);
                Assert.That(model.Area.Contains(actor.Position),Is.True);
                Assert.That(TownWalkableArea.Distance(actor.Position,peer.Position),Is.GreaterThanOrEqualTo(.0669f));
                float x=actor.Position.X-actor.Previous.X,y=actor.Position.Y-actor.Previous.Y;
                float distance=(float)Math.Sqrt(x*x+y*y),before=(float)Math.Sqrt(px*px+py*py);
                Assert.That(distance,Is.LessThanOrEqualTo((actor.Controlled?.12f:.047f)/30+.00001f));
                if(TownWalkableArea.Distance(previousTarget,actor.Target)>.001f){reversals.Clear();px=py=0;}
                else if(distance>.00001f&&before>.00001f&&(x*px+y*py)/(distance*before)<-.1f)reversals.Enqueue(tick);
                while(reversals.Count>0&&tick-reversals.Peek()>15)reversals.Dequeue();
                Assert.That(reversals.Count,Is.LessThanOrEqualTo(1),"Actual world movement reversed repeatedly within half a second, tick="+tick);
                if(distance>.00001f){px=x;py=y;}previousTarget=actor.Target;
            }
            Assert.That(actor.IsWalking,Is.False,"The correction must not replace jitter with indefinite waiting.");
            Assert.That(TownWalkableArea.Distance(actor.Position,actor.Target),Is.LessThan(.001f),"Arrive at the destination, not just abandon it.");
        }
    }
}
