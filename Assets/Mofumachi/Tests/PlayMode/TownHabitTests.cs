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
    public sealed class TownHabitTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator NpcsUseDifferentStopsAndResumeAfterWaiting()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var npcs=Flow.GetComponentsInChildren<TownActor>().Where(a=>!a.Controlled).ToArray();
            Assert.That(npcs.Select(a=>a.DestinationId).Distinct().Count(),Is.EqualTo(npcs.Length));var original=npcs.Select(a=>a.Position).ToArray();
            for(int i=0;i<240;i++)life.AdvanceFrame(1f/30);
            Assert.That(npcs.Where((a,i)=>!a.Position.Equals(original[i])).Any(),Is.True);Assert.That(npcs.Select(a=>a.DestinationId).Distinct().Count(),Is.EqualTo(npcs.Length));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NearbyConversationBelongsToItsActorsAndDoesNotSave()
        {
            yield return EnterHome();
            // Encounter visibility must use the Pixel 3a portrait viewport, not the Editor's Game view size.
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,740);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,740),new Rect(0,24,360,684));Canvas.ForceUpdateCanvases();
            var life=Flow.GetComponentInChildren<TownLifeView>();Assert.That(life.Regions.LookAtRegion(TownRegionId.West),Is.True);Canvas.ForceUpdateCanvases();var actors=Flow.GetComponentsInChildren<TownActor>();int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);
            Assert.That(life.TryBeginConversation(actors[0],actors[0]),Is.False);Assert.That(life.TryBeginConversation(null,actors[0]),Is.False);
            // Wait for an actual encounter; never relocate actors off their streets for a test.
            for(int i=0;i<3600&&!Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Contains("こんにちは"));i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Contains("こんにちは")),Is.True,"An actual street encounter must occur");
            Assert.That(Flow.GetComponentsInChildren<Text>().Count(t=>t.name=="Town bubble"),Is.LessThanOrEqualTo(1));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GrowthRestoresExpandedStopsFromSavedLevel()
        {
            yield return EnterHome();var state=Flow.Game.State.Clone();state.townGrowthLevel=6;Store.Save(state);Flow.Initialize(Store);Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(life.StreetMap.Places.Any(p=>p.Id=="celebration"),Is.True);Assert.That(life.GuideToPlace("celebration"),Is.True);Assert.That(life.GuideToPlace("unknown"),Is.False);Flow.Initialize(Store);Flow.ShowHome();Assert.That(Flow.GetComponentInChildren<TownLifeView>().StreetMap.Places.Any(p=>p.Id=="celebration"),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CompletedChapterCanBeReopenedFromHomeAfterReload()
        {
            yield return EnterHome();var state=Flow.Game.State.Clone();state.completedQuestIds.AddRange(new[]{"tea-01","bread-01"});state.claimedRewardIds.AddRange(new[]{"quest:tea-01","quest:bread-01"});state.coins=70;state.townGrowthLevel=2;Store.Save(state);Flow.Initialize(Store);Flow.ShowHome();
            var before=Flow.Game.State.Clone();int writes=Store.Writes;var replay=TownMenuButton("Review chapter");Assert.That(((RectTransform)replay.transform).rect.height,Is.GreaterThanOrEqualTo(48));Assert.That(replay.interactable,Is.True);replay.onClick.Invoke();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.ChapterSummary));Assert.That(Flow.LastChapter.ChapterId,Is.EqualTo("chapter-01"));Assert.That(Store.Writes,Is.EqualTo(writes+1),"Preserve the existing navigation save");
            // A successful save updates its metadata; every gameplay field must remain unchanged.
            var after=Flow.Game.State.Clone();Assert.That(System.DateTimeOffset.TryParse(after.lastSaveTime,out _),Is.True,"Save time remains valid metadata");
            Assert.That(StateCodec.Encode(Store.Load().State),Is.EqualTo(StateCodec.Encode(after)),"The recorded disk snapshot must match the live state");
            after.lastSaveTime=before.lastSaveTime;Assert.That(StateCodec.Encode(after),Is.EqualTo(StateCodec.Encode(before)),"Chapter replay must not change progress or rewards");
            Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NamesBubblesAndFacilitiesStayInsideSafeArea()
        {
            yield return EnterHome();foreach(int height in new[]{592,640,808})
            {
                var npc=FrameTownNpc(height);var life=Flow.GetComponentInChildren<TownLifeView>();npc.React();Canvas.ForceUpdateCanvases();
                var bubble=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble");Assert.That(bubble.preferredHeight,Is.LessThanOrEqualTo(bubble.rectTransform.rect.height+1));Assert.That(bubble.fontSize,Is.GreaterThanOrEqualTo(14));
                var doors=life.GetComponentsInChildren<Button>(true).Where(b=>b.name.StartsWith("Facility entrance ")).ToArray();Assert.That(doors.Length,Is.EqualTo(2));
                foreach(var button in doors)Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(48));
            }LogAssert.NoUnexpectedReceived();
        }
    }
}
