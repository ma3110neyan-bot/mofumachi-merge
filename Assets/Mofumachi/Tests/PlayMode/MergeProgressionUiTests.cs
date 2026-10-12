using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class MergeProgressionUiTests : SliceUiTestFixture
    {
        private Button Button(string name)=>Flow.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        private Text Text(string name)=>Flow.GetComponentsInChildren<Text>().Single(t=>t.name==name);
        private static Vector2 Position(Component c)=>RectTransformUtility.WorldToScreenPoint(null,c.transform.position);
        private void AssertFeedbackFits()
        {
            Canvas.ForceUpdateCanvases();var label=Text("Feedback");
            Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.5f),label.text);
        }
        private IEnumerator Click(string name)
        {
            yield return null;Canvas.ForceUpdateCanvases();
            var button=Button(name);Assert.That(button.IsInteractable(),Is.True,name);
            var pointer=new PointerEventData(EventSystem.current){position=Position(button),button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits,Is.Not.Empty,name+" must be visible to raycasts");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private void FullTeaAfterFirstQuest()
        {
            var state=GameState.CreateInitial();state.purchaseNoticeAcknowledged=true;state.favoriteCharacterId="momo";
            state.activeQuestId="tea-01";state.completedQuestIds.Add("tea-01");state.claimedRewardIds.Add("quest:tea-01");
            state.coins=30;state.townGrowthLevel=1;state.mergeBoard.Clear();
            for(int i=0;i<30;i++)state.mergeBoard.Add(new BoardItem("tea",3,i));
            Store.Save(state);Flow.Initialize(Store);Flow.ShowQuest();
        }
        private IEnumerator Discard(int cell,bool cancel=false)
        {
            yield return Click("Cell "+cell);yield return Click("Merge actions");yield return Click("Discard selected");
            Assert.That(Text("Discard item level").text,Does.Contain("お茶 Lv.3"));
            yield return Click(cancel?"Cancel discard":"Confirm discard");
        }
        [UnityTest] public IEnumerator FullBoardCanBeOrganizedIntoBreadDeliveryUsingVisibleButtons()
        {
            yield return EnterHome();FullTeaAfterFirstQuest();yield return Click("Accept quest");
            Assert.That(Flow.ProductionItemId,Is.EqualTo("bread"));Assert.That(Text("Create tea label").text,Does.Contain("パン"));
            yield return Click("Create tea");Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(30));
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("整理"));
            AssertFeedbackFits();
            yield return Discard(0,true);Assert.That(Flow.Game.Board.At(0),Is.Not.Null);
            // The cancelled confirmation keeps this cell selected; tap once to clear it.
            yield return Click("Cell 0");Assert.That(Flow.SelectedCell,Is.EqualTo(-1));
            yield return Discard(0);yield return Discard(1);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(28));
            yield return Click("Cell 2");yield return Click("Create tea");Assert.That(Flow.SelectedCell,Is.EqualTo(-1));
            yield return Click("Create tea");yield return Click("Cell 0");yield return Click("Cell 1");
            Assert.That(Flow.Game.Board.At(1).itemId,Is.EqualTo("bread"));Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));
            Assert.That(Button("Deliver").interactable,Is.False);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Text("Progress").text,Does.Contain("パン Lv.2 1 / 1"));yield return Click("Deliver");
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Result));Assert.That(Flow.Game.State.coins,Is.EqualTo(70));
            Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(2));Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(28));
            Assert.That(Flow.Deliver().Success,Is.False);Assert.That(Flow.Game.State.coins,Is.EqualTo(70));
            yield return new WaitForSecondsRealtime(1.3f);yield return Click("Show growth");Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Growth));
            yield return Click("Return town");Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.ChapterSummary));
            yield return Click("Chapter home");Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.coins,Is.EqualTo(70));
            Flow.Initialize(Store);yield return SceneManager.LoadSceneAsync("GameScene");yield return null;
            Assert.That(Flow.Game.State.coins,Is.EqualTo(70));Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(28));
            Assert.That(QuestCatalog.NextFor(Flow.Game.State).Id,Is.EqualTo("flower-01"));
            Flow.ShowQuest();Assert.That(Text("Requester").text,Is.EqualTo("ミエル"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ChangingKindClearsOldSelectionAndNextTapsMergeBread()
        {
            yield return EnterHome();Flow.ShowMerge();yield return Click("Cell 0");yield return Click("Merge actions");
            yield return Click("Produce bread");Assert.That(Flow.SelectedCell,Is.EqualTo(-1));
            LogAssert.NoUnexpectedReceived(); // QA information must not pollute Editor log assertions.
            yield return Click("Create tea");yield return Click("Create tea");yield return Click("Cell 2");yield return Click("Cell 3");
            Assert.That(Flow.Game.Board.At(2),Is.Null);Assert.That(Flow.Game.Board.At(3).itemId,Is.EqualTo("bread"));
            Assert.That(Flow.Game.Board.At(3).level,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MismatchedTapReselectsAndSameCellTapClearsWithoutMutation()
        {
            yield return EnterHome();Flow.ShowMerge();Flow.GenerateItem("bread");string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            yield return Click("Cell 0");yield return Click("Cell 2");
            Assert.That(Flow.SelectedCell,Is.EqualTo(2));Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("種類"));
            AssertFeedbackFits();
            yield return Click("Cell 2");Assert.That(Flow.SelectedCell,Is.EqualTo(-1));
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator UnacceptedRequestRoutesToAcceptanceAndResumeSuggestsBread()
        {
            yield return EnterHome();FullTeaAfterFirstQuest();Flow.ShowMerge();yield return Click("Deliver");
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));yield return Click("Accept quest");
            yield return Click("Merge actions");yield return Click("Produce tea");Assert.That(Flow.ProductionItemId,Is.EqualTo("tea"));
            Flow.Initialize(Store);yield return SceneManager.LoadSceneAsync("GameScene");yield return null;Flow.ShowMerge();
            Assert.That(Flow.ProductionItemId,Is.EqualTo("bread"));Assert.That(Flow.Game.State.activeQuestId,Is.EqualTo("bread-01"));
            Assert.That(Flow.Game.State.saveVersion,Is.EqualTo(3));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FailedDiscardAndProductionKeepBoardAndCannotReportSuccess()
        {
            yield return EnterHome();Flow.ShowMerge();yield return Click("Cell 0");yield return Click("Merge actions");yield return Click("Discard selected");
            string before=StateCodec.Encode(Flow.Game.State);Store.Fail=true;yield return Click("Confirm discard");
            Assert.That(Flow.IsModalOpen,Is.True);Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Assert.That(Text("Discard feedback").text,Does.Contain("セーブ"));Store.Fail=false;yield return Click("Cancel discard");
            Store.Fail=true;yield return Click("Create tea");Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("セーブ"));Store.Fail=false;LogAssert.NoUnexpectedReceived();
        }
        private static IEnumerator Touch(Touchscreen screen,Vector2 position,UnityEngine.InputSystem.TouchPhase phase)
        {
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=71,position=position,phase=phase});
            yield return null;yield return null;
        }
        [UnityTest] public IEnumerator UnknownSavedItemCanBeDiscardedThroughConfirmation()
        {
            yield return EnterHome();Assert.That(Flow.Game.AddItem("future-item",1),Is.True);Flow.Initialize(Store);Flow.ShowMerge();
            yield return Click("Cell 2");yield return Click("Merge actions");yield return Click("Discard selected");
            Assert.That(Text("Discard item level").text,Is.EqualTo("アイテム Lv.1"));yield return Click("Confirm discard");
            Assert.That(Flow.Game.Board.At(2),Is.Null);Assert.That(Flow.IsModalOpen,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator InputSystemTouchDragAndTapReachBreadCellsThroughEventSystem()
        {
            yield return EnterHome();Flow.ShowMerge();
            // Seed a valid full board. This test exercises actual input routing, not DropItem calls.
            Flow.Game.State.mergeBoard.Clear();for(int i=0;i<28;i++)Flow.Game.AddItem("tea",3);
            Flow.Game.AddItem("bread",1);Flow.Game.AddItem("bread",1);Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var touch=InputSystem.AddDevice<Touchscreen>();
            try
            {
                var from=Position(Button("Cell 28"));var to=Position(Button("Cell 29"));
                yield return Touch(touch,from,UnityEngine.InputSystem.TouchPhase.Began);
                yield return Touch(touch,Vector2.Lerp(from,to,.5f),UnityEngine.InputSystem.TouchPhase.Moved);
                Assert.That(GameObject.Find("Dragged tea"),Is.Not.Null,"Touch drag must begin before dropping.");
                yield return Touch(touch,to,UnityEngine.InputSystem.TouchPhase.Moved);
                var ghost=GameObject.Find("Dragged tea");Assert.That(Vector2.Distance(Position(ghost.transform),to),Is.LessThan(3));
                yield return Touch(touch,to,UnityEngine.InputSystem.TouchPhase.Ended);yield return new WaitForSecondsRealtime(.3f);
                Assert.That(Flow.Game.Board.At(28),Is.Null);Assert.That(Flow.Game.Board.At(29).level,Is.EqualTo(2));
                to=Position(Button("Cell 29"));from=Position(Button("Cell 28"));
                yield return Touch(touch,to,UnityEngine.InputSystem.TouchPhase.Began);yield return Touch(touch,to,UnityEngine.InputSystem.TouchPhase.Ended);
                Assert.That(Flow.SelectedCell,Is.EqualTo(29));
                yield return Touch(touch,from,UnityEngine.InputSystem.TouchPhase.Began);yield return Touch(touch,from,UnityEngine.InputSystem.TouchPhase.Ended);
                Assert.That(Flow.Game.Board.At(29),Is.Null);Assert.That(Flow.Game.Board.At(28).level,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
            }
            finally {InputSystem.RemoveDevice(touch);}
        }
    }
public sealed class MergeGuidanceTests
{
 private string root;private GameStateManager game;
 [SetUp]public void Setup(){root=Path.Combine(Path.GetTempPath(),"merge-guidance-"+Guid.NewGuid());game=new GameStateManager(GameState.CreateInitial(),new SaveService(root));}
 [TearDown]public void Cleanup(){if(Directory.Exists(root))Directory.Delete(root,true);}

 [Test]public void DiagnosticsOnlyEnablesDevelopmentPlayers()
 {
  Assert.That(MergeInputDiagnostics.ShouldRecord(true,true),Is.False);
  Assert.That(MergeInputDiagnostics.ShouldRecord(true,false),Is.False);
  Assert.That(MergeInputDiagnostics.ShouldRecord(false,false),Is.False);
  Assert.That(MergeInputDiagnostics.ShouldRecord(false,true),Is.True);
 }

 [Test]public void BreadQuestSuggestsBreadWithoutNewSaveField(){game.State.completedQuestIds.Add("tea-01");Assert.That(UIStrings.SuggestedProduction(game),Is.EqualTo("bread"));Assert.That(game.State.saveVersion,Is.EqualTo(3));}
 [Test]public void MixedQuestSuggestsTheMissingRequirement(){game.State.activeQuestId="tea-02";game.State.mergeBoard.Clear();game.AddItem("tea",3);Assert.That(UIStrings.SuggestedProduction(game),Is.EqualTo("bread"));}
 [Test]public void UnknownQuestDoesNotInventARequiredKind(){game.State.activeQuestId="unknown";Assert.That(UIStrings.SuggestedProduction(game),Is.EqualTo("tea"));Assert.That(UIStrings.DeliveryHint(game),Does.Contain("確認できません"));}
 [Test]public void DifferentKindsExplainTheMismatch(){game.State.mergeBoard[1].itemId="bread";Assert.That(UIStrings.DropReason(game.Board,0,1),Does.Contain("種類"));}
 [Test]public void DifferentLevelsExplainTheMismatch(){game.State.mergeBoard[1].level=2;Assert.That(UIStrings.DropReason(game.Board,0,1),Does.Contain("Lv."));}
 [Test]public void HighestLevelIsExplained(){foreach(var i in game.State.mergeBoard)i.level=3;Assert.That(UIStrings.DropReason(game.Board,0,1),Does.Contain("最高"));}
 [Test]public void ValidPairHasNoRefusal(){Assert.That(UIStrings.DropReason(game.Board,0,1),Is.Empty);Assert.That(UIStrings.DropReason(game.Board,0,2),Is.Empty);}
 [Test]public void UnacceptedQuestGuidesToAcceptance(){Assert.That(UIStrings.DeliveryHint(game),Does.Contain("依頼を見る"));}
 [Test]public void RequirementSummaryShowsRealExcessCount(){game.State.completedQuestIds.Add("tea-01");game.State.mergeBoard.Clear();game.AddItem("bread",2);game.AddItem("bread",2);Assert.That(UIStrings.RequiredSummary(game,QuestCatalog.All.Single(q=>q.Id=="bread-01")),Does.Contain("2 / 1"));}
}

}
