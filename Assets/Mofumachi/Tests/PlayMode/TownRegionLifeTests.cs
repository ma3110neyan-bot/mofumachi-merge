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
        [UnityTest] public IEnumerator NpcWalksBetweenRegionsWithoutAnEntryTeleport()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var npcs=life.World.Model.Residents.Where(a=>!a.Controlled).ToArray();
            var initial=npcs.Select(a=>a.Region).ToArray();var previous=npcs.Select(a=>a.Position).ToArray();bool crossed=false;
            for(int i=0;i<9000&&!crossed;i++){life.AdvanceFrame(1f/30);for(int j=0;j<npcs.Length;j++){Assert.That(TownWorldLayout.Distance(previous[j],npcs[j].Position),Is.LessThanOrEqualTo(.00118f));crossed|=npcs[j].Region!=initial[j];previous[j]=npcs[j].Position;}}
            Assert.That(crossed,Is.True,"Residents must visit another region on the same connected paths");Assert.That(life.Actors.Count,Is.EqualTo(6));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DifferentRegionsNeverConverseOrBlockEachOther()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var west=life.Actors.First(a=>a.RegionId==TownRegionId.West);var east=life.Actors.First(a=>a.RegionId==TownRegionId.East);
            Assert.That(life.TryBeginConversation(west,east),Is.False);
            var scale=typeof(TownLifeView).GetMethod("MovementScale",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Assert.That((float)scale.Invoke(life,new object[]{west}),Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TwoResidentsMeetAndResumeInVisibleRegion()
        {
            yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,740);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,740),new Rect(0,24,360,684));Canvas.ForceUpdateCanvases();
            var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.LookAtRegion(TownRegionId.West);var actors=life.Actors.Where(a=>!a.Controlled&&a.RegionId==TownRegionId.West).ToArray();Assert.That(actors.Length,Is.EqualTo(2));bool greeted=false;
            for(int i=0;i<3600&&!greeted;i++){life.AdvanceFrame(1f/30);greeted=Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Contains("こんにちは"));}
            Assert.That(greeted,Is.True);actors=life.Actors.Where(a=>life.World.Model.Residents.Single(r=>r.Id==a.CharacterId).PauseRemaining>0).ToArray();Assert.That(actors.Length,Is.EqualTo(2),"Inspect the two actors of the actual encounter");var paused=actors.Select(a=>a.WorldPosition).ToArray();Advance(life,.5f);
            foreach(var actor in actors)Assert.That(life.World.Model.Residents.Single(r=>r.Id==actor.CharacterId).PauseRemaining,Is.GreaterThan(0));
            for(int i=0;i<actors.Length;i++)Assert.That(TownWorldLayout.Distance(paused[i],actors[i].WorldPosition),Is.LessThan(.0001f));
            Advance(life,20);Assert.That(actors.Where((a,i)=>TownWorldLayout.Distance(paused[i],a.WorldPosition)>.001f).Any(),Is.True);LogAssert.NoUnexpectedReceived();
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
        [UnityTest] public IEnumerator TappingAWalkingResidentPausesForActualDialogue()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var npc=life.Actors.First(a=>!a.Controlled);life.Regions.LookAtRegion(npc.RegionId);
            Assert.That(life.World.Model.TryGuide(npc.CharacterId,"west.meeting"),Is.True);life.AdvanceFrame(1f/30);npc.React();
            Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Length>0),Is.True);Assert.That(Flow.Daily.Data.Characters.Single(c=>c.Id==npc.CharacterId).Meetings,Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ResidentsReserveDifferentStopsThroughoutTheirContinuousVisits()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            for(int i=0;i<9000;i++){life.AdvanceFrame(1f/30);var places=life.World.Model.Residents.Where(a=>!a.Controlled&&a.PlaceId!=null).Select(a=>a.PlaceId).ToArray();Assert.That(places.Distinct().Count(),Is.EqualTo(places.Length));}
            Assert.That(life.Actors.Count,Is.EqualTo(6));LogAssert.NoUnexpectedReceived();
        }
    }
}
