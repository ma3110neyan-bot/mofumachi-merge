using System;
using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class TownRegionLifeTests : SliceUiTestFixture
    {
        private static bool Schedule(TownRegionSession session,string id,TownRegionId target)
        {
            var method=typeof(TownRegionSession).GetMethod("TryScheduleNpcVisit");Assert.That(method,Is.Not.Null,"NPC regional visiting is not implemented");
            return (bool)method.Invoke(session,new object[]{id,target});
        }
        private static void Advance(TownLifeView life,float seconds)
        {for(int i=0;i<Mathf.CeilToInt(seconds*30);i++)life.AdvanceFrame(1f/30);}
        [UnityTest] public IEnumerator OffscreenNpcsKeepLivingWithoutDuplicatedActors()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var actors=life.Actors.ToArray();var before=actors.Select(a=>a.Position).ToArray();
            Advance(life,12);
            Assert.That(actors.Where((a,i)=>!a.RegionVisible&&!a.Position.Equals(before[i])).Any(),Is.True,"Hidden residents must still walk their streets");
            foreach(var a in actors.Where(a=>!a.RegionVisible))Assert.That(a.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            Assert.That(Flow.GetComponentsInChildren<TownActor>().Length,Is.EqualTo(6));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NpcChangesRegionThroughReservedEntrances()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var session=life.Regions;var npcs=life.Actors.Where(a=>!a.Controlled).ToArray();
            Assert.That(Schedule(session,npcs.First(a=>a.RegionId==TownRegionId.West).CharacterId,TownRegionId.Central),Is.False,"At least 20 seconds before the first regional visit");
            Assert.That(Schedule(session,life.Actors.Single(a=>a.Controlled).CharacterId,TownRegionId.West),Is.False);
            bool crossed=false;var last=NpcRegions(npcs);var previous=npcs.Select(a=>a.Position).ToArray();
            for(int i=0;i<3600&&!crossed;i++)
            {
                life.AdvanceFrame(1f/30);
                for(int j=0;j<npcs.Length;j++)
                {
                    var a=npcs[j];if(a.RegionId!=last[j])
                    {
                        Assert.That(TownRegionCatalog.TryGetPortal(last[j],a.RegionId,out var portal),Is.True);
                        Assert.That(Vector2.Distance(new Vector2(previous[j].X,previous[j].Y),new Vector2(portal.From.Local.X,portal.From.Local.Y)),Is.LessThan(.001f));
                        Assert.That(Vector2.Distance(new Vector2(a.Position.X,a.Position.Y),new Vector2(portal.To.Local.X,portal.To.Local.Y)),Is.LessThan(.001f));crossed=true;
                    }
                    previous[j]=a.Position;last[j]=a.RegionId;
                }
                foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))Assert.That(npcs.Count(a=>a.RegionId==region),Is.LessThanOrEqualTo(2));
            }
            Assert.That(crossed,Is.True,"A resident must reach a neighbouring street through its gate");LogAssert.NoUnexpectedReceived();
        }
        private static TownRegionId[] NpcRegions(TownActor[] actors)=>actors.Select(a=>a.RegionId).ToArray();
        [UnityTest] public IEnumerator DifferentRegionsNeverConverseOrBlockEachOther()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var west=life.Actors.First(a=>a.RegionId==TownRegionId.West);var east=life.Actors.First(a=>a.RegionId==TownRegionId.East);
            Assert.That(life.TryBeginConversation(west,east),Is.False);
            var scale=typeof(TownLifeView).GetMethod("MovementScale",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Assert.That((float)scale.Invoke(life,new object[]{west}),Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TwoResidentsMeetAndResumeInVisibleRegion()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();Assert.That(life.Regions.LookAtRegion(TownRegionId.West),Is.True);
            var actors=life.Actors.Where(a=>a.RegionVisible).ToArray();bool greeted=false;
            for(int i=0;i<900&&!greeted;i++){life.AdvanceFrame(1f/30);greeted=Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Contains("こんにちは"));}
            Assert.That(greeted,Is.True,"Use an actual encounter of the two west residents");var points=actors.Select(a=>a.Position).ToArray();Advance(life,10);
            Assert.That(actors.Where((a,i)=>!a.Position.Equals(points[i])).Any(),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SideVisitsNeverMutateCoinsQuestsOrBoard()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            foreach(var region in new[]{TownRegionId.West,TownRegionId.East,TownRegionId.Central}){life.Regions.LookAtRegion(region);Advance(life,40);}
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LevelsAddVisitsWithoutClosingExistingEntrances()
        {
            yield return EnterHome();foreach(int level in new[]{0,2,4,6})
            {
                var state=Flow.Game.State.Clone();state.townGrowthLevel=level;Store.Save(state);Flow.Initialize(Store);Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var region in new[]{TownRegionId.West,TownRegionId.East})
                {
                    life.Regions.LookAtRegion(region);var places=life.StreetMap.Places;
                    Assert.That(places.Any(p=>p.Activity==TownActivity.Bench),Is.True);Assert.That(places.Any(p=>p.Activity==TownActivity.Flowers),Is.True);
                    Assert.That(places.Any(p=>p.Id=="west.cafe-front"),Is.EqualTo(region==TownRegionId.West&&level>=2));
                    Assert.That(places.Any(p=>p.Id=="east.workshop-front"),Is.EqualTo(region==TownRegionId.East&&level>=4));
                }
                life.Regions.ReturnToHero();Assert.That(life.GuideToFacility("quest"),Is.True);Assert.That(life.GuideToFacility("shop"),Is.True);
            }LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TappingTravellingResidentHasDialogueInsteadOfNullPlaceException()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();TownActor travelling=null;
            for(int i=0;i<2400&&travelling==null;i++)
            {life.AdvanceFrame(1f/30);travelling=life.Actors.FirstOrDefault(a=>!a.Controlled&&a.DestinationId==null);}
            Assert.That(travelling,Is.Not.Null,"Observe a real regional journey");life.Regions.LookAtRegion(travelling.RegionId);travelling.React();
            Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Length>0),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LocalStopChoiceHonorsIncomingJourneyReservation()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var session=life.Regions;
            var journeysField=typeof(TownRegionSession).GetField("journeys",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var choose=typeof(TownLifeView).GetMethod("ChooseNextStop",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            bool inspected=false;
            for(int i=0;i<7200&&!inspected;i++)
            {
                life.AdvanceFrame(1f/30);var journeys=(System.Collections.IDictionary)journeysField.GetValue(session);
                foreach(System.Collections.DictionaryEntry pair in journeys)
                {
                    var type=pair.Value.GetType();var actor=(TownActor)type.GetField("Actor",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(pair.Value);
                    var target=(TownPlace)type.GetField("Destination",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(pair.Value);
                    if(actor.Controlled||!TownRegionCatalog.TryFindPlace(target.Id,life.TownLevel,out var location,out _))continue;
                    var local=life.Actors.FirstOrDefault(a=>!a.Controlled&&a!=actor&&a.RegionId==location.Region&&a.DestinationId!=null);if(local==null)continue;
                    for(int j=0;j<12;j++){choose.Invoke(life,new object[]{local});Assert.That(local.DestinationId,Is.Not.EqualTo(target.Id),"Local resident selected an incoming visitor's reserved stop");}
                    inspected=true;break;
                }
            }
            Assert.That(inspected,Is.True,"Inspect a real regional visitor, including walking-in reservations");LogAssert.NoUnexpectedReceived();
        }
    }
}
