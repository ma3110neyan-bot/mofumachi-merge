using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class CharacterStabilityTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator SixResidentsPassAStationaryNeighbourWithoutJitterOrSpeech()
        {
            yield return EnterHome();var life=FrameTownPortrait();var model=life.World.Model;
            string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            // AdvanceFrame sorts life.Actors' backing array for depth. Keep a
            // stable identity list so sorting cannot repeat/skip a resident.
            var actors=life.Actors.ToArray();var checkedIds=new HashSet<string>();
            foreach(var actor in actors)
            {
                string peerId=actor.CharacterId=="lulu"?"momo":"lulu";
                foreach(var r in model.Residents)model.Restore(r.Id,new TownPoint(-1.8f,-.4f));
                model.Restore(actor.CharacterId,new TownPoint(-.20f,-.22f));model.Restore(peerId,new TownPoint(0,-.30f));
                var moving=model.Residents.Single(r=>r.Id==actor.CharacterId);var peer=model.Residents.Single(r=>r.Id==peerId);
                Assert.That((bool)typeof(TownWorldWalkModel).GetMethod("Guide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(model,new object[]{moving,new TownPoint(.16f,-.22f),null,true,false}),Is.True);
                float px=0,py=0;var reversals=new Queue<int>();
                for(int tick=0;tick<900&&moving.IsWalking;tick++)
                {
                    foreach(var r in model.Residents.Where(r=>r!=moving))model.Pause(r.Id,4);
                    life.AdvanceFrame(1f/30);
                    Assert.That(life.IsSpeaking,Is.False,"Proximity must not trigger speech.");
                    Assert.That(TownWalkableArea.Distance(moving.Position,peer.Position),Is.GreaterThanOrEqualTo(.0669f));
                    float x=moving.Position.X-moving.Previous.X,y=moving.Position.Y-moving.Previous.Y;
                    float distance=(float)Math.Sqrt(x*x+y*y),previous=(float)Math.Sqrt(px*px+py*py);
                    if(distance>.00001f&&previous>.00001f&&(x*px+y*py)/(distance*previous)<-.1f)reversals.Enqueue(tick);
                    while(reversals.Count>0&&tick-reversals.Peek()>15)reversals.Dequeue();
                    Assert.That(reversals.Count,Is.LessThanOrEqualTo(1),actor.CharacterId+": world position cannot alternate rapidly.");
                    if(distance>.00001f){px=x;py=y;}
                }
                Assert.That(moving.IsWalking,Is.False);
                Assert.That(TownWalkableArea.Distance(moving.Position,new TownPoint(.16f,-.22f)),Is.LessThan(.001f));
                Assert.That(checkedIds.Add(actor.CharacterId),Is.True,"Each identity must be checked once.");
            }
            Assert.That(checkedIds,Is.EquivalentTo(CharacterCatalog.All.Select(c=>c.Id)));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
    }
}
