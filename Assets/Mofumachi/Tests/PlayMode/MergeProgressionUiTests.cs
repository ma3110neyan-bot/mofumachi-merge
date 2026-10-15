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
using UnityEngine.InputSystem.UI;
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
            Assert.That(Flow.ProductionItemId,Is.EqualTo("bread"));Assert.That(Text("Generate frame "+Flow.ProductionItemId+" label").text,Does.Contain("パン"));
            yield return Click("Generate frame "+Flow.ProductionItemId);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(30));
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("整理"));
            AssertFeedbackFits();
            yield return Discard(0,true);Assert.That(Flow.Game.Board.At(0),Is.Not.Null);
            // The cancelled confirmation keeps this cell selected; tap once to clear it.
            yield return Click("Cell 0");Assert.That(Flow.SelectedCell,Is.EqualTo(-1));
            yield return Discard(0);yield return Discard(1);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(28));
            yield return Click("Cell 2");yield return Click("Generate frame "+Flow.ProductionItemId);Assert.That(Flow.SelectedCell,Is.EqualTo(-1));
            yield return Click("Generate frame "+Flow.ProductionItemId);yield return Click("Cell 0");yield return Click("Cell 1");
            Assert.That(Flow.Game.Board.At(1).itemId,Is.EqualTo("bread"));Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));
            Assert.That(Button("Deliver").interactable,Is.False);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Text("Requirement count bread").text,Does.Contain("1 / 1"));yield return Click("Deliver");
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
            yield return Click("Generate frame "+Flow.ProductionItemId);yield return Click("Generate frame "+Flow.ProductionItemId);yield return Click("Cell 2");yield return Click("Cell 3");
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
            Store.Fail=true;yield return Click("Generate frame "+Flow.ProductionItemId);Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
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
            var settings=InputSystem.settings;var previousBackground=settings.backgroundBehavior;
#if UNITY_EDITOR
            var previousEditorInput=settings.editorInputBehaviorInPlayMode;
#endif
            Touchscreen touch=null;
            try
            {
                // Batch runs do not focus a Game view. Isolate synthetic input from
                // editor focus without changing the project's settings or device policy.
                // Keep the same settings object: replacing it can destroy Unity's
                // temporary default settings. Restore both fields in finally.
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                touch=InputSystem.AddDevice<Touchscreen>();
                yield return null;Canvas.ForceUpdateCanvases();
                Assert.That(touch.enabled,Is.True,"The synthetic touchscreen must accept queued input");
                Assert.That(EventSystem.current.currentInputModule,Is.InstanceOf<InputSystemUIInputModule>());
                var from=Position(Button("Cell 28"));var to=Position(Button("Cell 29"));
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=from},hits);
                Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].gameObject.GetComponentInParent<MergeBoardView>(),Is.SameAs(Button("Cell 28").GetComponent<MergeBoardView>()),"The bread cell must receive the touch raycast");
                yield return Touch(touch,from,UnityEngine.InputSystem.TouchPhase.Began);
                Assert.That(touch.primaryTouch.press.isPressed,Is.True,"The queued touch must reach Input System before dragging");
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
            finally
            {
                try {if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);}
                finally
                {
                    settings.backgroundBehavior=previousBackground;
#if UNITY_EDITOR
                    settings.editorInputBehaviorInPlayMode=previousEditorInput;
#endif
                }
            }
            Assert.That(InputSystem.settings,Is.SameAs(settings));Assert.That(settings.backgroundBehavior,Is.EqualTo(previousBackground));
#if UNITY_EDITOR
            Assert.That(settings.editorInputBehaviorInPlayMode,Is.EqualTo(previousEditorInput));
#endif
        }

        private void AcceptedBread()
        {
            var s=GameState.CreateInitial();s.purchaseNoticeAcknowledged=true;s.favoriteCharacterId="momo";s.completedQuestIds.Add("tea-01");s.claimedRewardIds.Add("quest:tea-01");s.coins=30;s.townGrowthLevel=1;s.activeQuestId="bread-01";s.questState=QuestState.Accepted;s.mergeBoard.Clear();s.mergeBoard.Add(new BoardItem("bread",1,0));s.mergeBoard.Add(new BoardItem("bread",1,1));Store.Save(s);Flow.Initialize(Store);Flow.ShowMerge();
        }
        [UnityTest] public IEnumerator RequiredArtCountsAndShortageAgreeWithInventory()
        {
            yield return EnterHome();AcceptedBread();Assert.That(Text("Requirement count bread").text,Does.Contain("0 / 1"));Assert.That(Flow.GetComponentsInChildren<ItemGraphic>().Single(a=>a.name=="Requirement art bread").ItemId,Is.EqualTo("bread"));
            Assert.That(Flow.DropItem(0,1),Is.True);Assert.That(Text("Requirement count bread").text,Does.Contain("0 / 1"));yield return new WaitForSecondsRealtime(.3f);Assert.That(Text("Requirement count bread").text,Does.Contain("1 / 1"));Assert.That(Button("Deliver").interactable,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RequirementGuidesBreadWithoutGeneratingOrSaving()
        {
            yield return EnterHome();AcceptedBread();int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);yield return Click("Cell 0");yield return Click("Requirement bread");Assert.That(Flow.ProductionItemId,Is.EqualTo("bread"));Assert.That(Flow.SelectedCell,Is.EqualTo(-1));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PackedFramesAndUnacceptedQuestHaveExplicitNextActions()
        {
            yield return EnterHome();Flow.ShowMerge();yield return Click("Requirement tea");Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));AcceptedBread();Assert.That(Flow.Game.PackProductionFrames(new[]{"tea","bread","flower"}),Is.True);Flow.ShowMerge();yield return Click("Requirement bread");Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Button("Produce bread").interactable,Is.True);Flow.CancelMergeActions();Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ShortPortraitKeepsOneProductionRowAndReadableControls()
        {
            yield return EnterHome();AcceptedBread();var state=Flow.Game.State;
            state.completedQuestIds.AddRange(new[]{"bread-01","flower-01"});state.activeQuestId="tea-02";
            state.mergeBoard.Clear();state.mergeBoard.Add(new BoardItem("tea",3,0));state.mergeBoard.Add(new BoardItem("bread",2,1));
            var canvas=new GameObject("Safe Merge fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var canvasRect=(RectTransform)canvas.transform;var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
            try
            {
                foreach(int h in new[]{592,640,808})
                {
                    canvasRect.sizeDelta=new Vector2(360,h);var responsive=canvas.AddComponent<ResponsiveUILayout>();responsive.Initialize(canvas.GetComponent<Canvas>());responsive.ApplyViewport(new Vector2(360,h),new Rect(0,24,360,h-56));
                    var page=responsive.CreatePage("Safe Merge page");QuestMergeScreens.BuildMerge(page,new ScreenContext(Flow,widgets));Canvas.ForceUpdateCanvases();page.GetComponent<ProductionFrameLayout>().ResizeNow();page.GetComponentInChildren<MergeGridLayout>().ResizeNow();Canvas.ForceUpdateCanvases();
                    Assert.That(page.rect.height,Is.EqualTo(h-56).Within(.01f),"Fixture must resize the actual canvas and apply the insets");
                    var buttons=page.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Generate frame ")||b.name=="Merge actions"||b.name=="Deliver"||b.name.StartsWith("Nav ")).ToArray();
                    Assert.That(buttons.Count(b=>b.name.StartsWith("Generate frame ")),Is.EqualTo(3));
                    foreach(var button in buttons){var r=((RectTransform)button.transform).rect;Assert.That(r.width,Is.GreaterThanOrEqualTo(48),button.name);Assert.That(r.height,Is.GreaterThanOrEqualTo(48),button.name);var parent=(RectTransform)button.transform.parent;var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,button.transform);Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(parent.rect.xMin-.01f),button.name);Assert.That(bounds.max.x,Is.LessThanOrEqualTo(parent.rect.xMax+.01f),button.name);}
                    foreach(var cell in page.GetComponentsInChildren<MergeBoardView>())Assert.That(((RectTransform)cell.transform).rect.width,Is.GreaterThanOrEqualTo(48),"Safe Area cell");
                    foreach(var count in page.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Requirement count ")))Assert.That(count.preferredHeight,Is.LessThanOrEqualTo(count.rectTransform.rect.height+.5f),count.text);
                    var feedback=page.Find("Feedback").GetComponent<Text>();
                    foreach(string message in new[]{UIStrings.BoardFull,UIStrings.DifferentKind+"\nパン Lv.1を選びました。"})
                    {
                        page.GetComponent<ProductionFrameLayout>().SetMessage(message);Canvas.ForceUpdateCanvases();Assert.That(feedback.preferredHeight,Is.LessThanOrEqualTo(feedback.rectTransform.rect.height+.5f),message);
                    }
                    UnityEngine.Object.DestroyImmediate(responsive.FullScreenRoot.gameObject);UnityEngine.Object.DestroyImmediate(responsive.SafeRoot.gameObject);UnityEngine.Object.DestroyImmediate(responsive);
                }
            }finally{UnityEngine.Object.DestroyImmediate(canvas);}LogAssert.NoUnexpectedReceived();
        }
    }
public sealed class ProductionFrameUiTests : SliceUiTestFixture
{
 private Button Button(string name)=>Flow.GetComponentsInChildren<Button>().Single(b=>b.name==name);
 private IEnumerator Click(string name)
 {
  yield return null;Canvas.ForceUpdateCanvases();var button=Button(name);Assert.That(button.IsInteractable(),Is.True,name);
  var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position),button=PointerEventData.InputButton.Left};
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits,Is.Not.Empty,name);
  Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),name);
  ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
 }
 private IEnumerator SelectFrames(params string[] ids)
 {
  yield return Click("Merge actions");yield return Click("Begin frame cleanup");
  Assert.That(Button("Pack selected frames").interactable,Is.False);
  foreach(var id in ids)yield return Click("Produce "+id);
 }
 [UnityTest]public IEnumerator MultipleFramesPackOnceWithoutDeletingBoardItems()
 {
  yield return EnterHome();Flow.ShowMerge();int writes=Store.Writes;
  yield return SelectFrames("tea","bread");Assert.That(Flow.SelectedFrameCount,Is.EqualTo(2));Assert.That(Store.Writes,Is.EqualTo(writes));
  Assert.That(Flow.GetComponentsInChildren<Text>().Count(t=>t.name=="Frame selected badge"),Is.EqualTo(2));
  yield return Click("Pack selected frames");Assert.That(Flow.Game.State.productionFrameIds.Count,Is.EqualTo(3));
  yield return Click("Confirm pack frames");Assert.That(Store.Writes,Is.EqualTo(writes+1));
  Assert.That(Flow.Game.State.productionFrameIds,Is.EqualTo(new[]{"flower"}));Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));
  Assert.That(Flow.Game.State.coins,Is.Zero);Assert.That(Flow.IsModalOpen,Is.False);Assert.That(Flow.ConfirmPackFrames(),Is.False);
  LogAssert.NoUnexpectedReceived();
 }
 [UnityTest]public IEnumerator FailedPackingRestoresEveryFrameAndCancelKeepsSavedState()
 {
  yield return EnterHome();Flow.ShowMerge();yield return SelectFrames("bread","flower");yield return Click("Pack selected frames");
  string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;Store.Fail=true;yield return Click("Confirm pack frames");
  Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Flow.IsModalOpen,Is.True);
  Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Pack feedback").text,Does.Contain("セーブ"));
  Store.Fail=false;yield return Click("Cancel pack frames");Assert.That(Flow.SelectedFrameCount,Is.EqualTo(2));
  yield return Click("Cancel merge menu");Assert.That(Flow.SelectedFrameCount,Is.Zero);Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
  LogAssert.NoUnexpectedReceived();
 }
 [UnityTest]public IEnumerator AllFramesStayPackedAfterReloadAndReplacementResumesProduction()
 {
  yield return EnterHome();Flow.ShowMerge();yield return SelectFrames("tea","bread","flower");yield return Click("Pack selected frames");yield return Click("Confirm pack frames");
  Flow.Initialize(Store);Flow.ShowMerge();Assert.That(Flow.Game.State.productionFrameIds,Is.Empty);Flow.GenerateItem("bread");Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));
  yield return Click("Generate frame "+Flow.ProductionItemId);Store.Fail=true;yield return Click("Produce bread");
  Assert.That(Flow.Game.State.productionFrameIds,Is.Empty);Assert.That(Flow.IsModalOpen,Is.True);
  Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge menu help").text,Does.Contain("セーブ"));
  Store.Fail=false;yield return Click("Produce bread");Assert.That(Flow.Game.State.productionFrameIds,Is.EqualTo(new[]{"bread"}));
  yield return Click("Generate frame "+Flow.ProductionItemId);Assert.That(Flow.Game.Board.At(2).itemId,Is.EqualTo("bread"));Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo("momo"));
  Assert.That(Flow.Game.State.saveVersion,Is.EqualTo(3));LogAssert.NoUnexpectedReceived();
 }
 [UnityTest]public IEnumerator PauseClearsOnlyTransientFrameSelection()
 {
  yield return EnterHome();Flow.ShowMerge();yield return SelectFrames("tea");Assert.That(Flow.SelectedFrameCount,Is.EqualTo(1));
  Flow.SendMessage("OnApplicationPause",true);Assert.That(Flow.SelectedFrameCount,Is.Zero);Assert.That(Flow.IsModalOpen,Is.False);
  Flow.SendMessage("OnApplicationPause",false);Assert.That(Flow.Game.State.productionFrameIds.Count,Is.EqualTo(3));Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));
  LogAssert.NoUnexpectedReceived();
 }
 [UnityTest]public IEnumerator MergeLabelAndAdaptiveFramesKeepTheLockedBoardLayout()
 {
  yield return EnterHome();Flow.ShowMerge();Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.text=="Merge"),Is.True);
  Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("マージ")),Is.False);
  var canvas=new GameObject("Frame layout fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
  try
  {
   var root=(RectTransform)canvas.transform;var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
   foreach(float height in new[]{592f,640f,808f})
   {
    root.sizeDelta=new Vector2(360,height);var page=UIWidgets.Node("Frame page",root,new Rect(0,0,1,1));QuestMergeScreens.BuildMerge(page,new ScreenContext(Flow,widgets));
    Canvas.ForceUpdateCanvases();page.GetComponentInChildren<MergeGridLayout>().ResizeNow();Canvas.ForceUpdateCanvases();
    var cells=page.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
    foreach(var cell in cells){var r=((RectTransform)cell.transform).rect;Assert.That(r.width,Is.EqualTo(r.height).Within(.01));Assert.That(r.width,Is.GreaterThanOrEqualTo(48));}
    Assert.That(page.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Generate frame ")),Is.EqualTo(3));
    if(height==808)
    {
     root.sizeDelta=new Vector2(360,592);Canvas.ForceUpdateCanvases();page.GetComponent<ProductionFrameLayout>().ResizeNow();
     page.GetComponentInChildren<MergeGridLayout>().ResizeNow();Canvas.ForceUpdateCanvases();
     Assert.That(page.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Generate frame ")),Is.EqualTo(3));
     foreach(var cell in cells)Assert.That(((RectTransform)cell.transform).rect.width,Is.GreaterThanOrEqualTo(48));
    }
    UnityEngine.Object.DestroyImmediate(page.gameObject);
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(canvas);}
  LogAssert.NoUnexpectedReceived();
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
