using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class QualityPassMergeTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator SelectedItemNameAndLevelRemainVisibleAboveTheBoard()
        {
            yield return EnterHome();Flow.ShowMerge();int writes=Store.Writes;string before=Mofumachi.Core.StateCodec.Encode(Flow.Game.State);
            Flow.TapCell(0);yield return null;Canvas.ForceUpdateCanvases();
            var name=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge selected item");
            Assert.That(name.text,Is.EqualTo("お茶 Lv.1"));Assert.That(name.GetComponentInParent<ScrollRect>(),Is.Null);
            Assert.That(name.preferredHeight,Is.LessThanOrEqualTo(name.rectTransform.rect.height+.5f));
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge selected hint").text,Does.Contain("同じ種類"));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Mofumachi.Core.StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Flow.TapCell(0);yield return null;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge selected item").text,Is.EqualTo("アイテムを選ぶ"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CompletedStoryKeepsSparseSavedItemsAndShowsLiveDailyPurpose()
        {
            yield return EnterHome();var state=Flow.Game.State;
            void NavigateWithoutChangingProgress(System.Action navigate)
            {
                var beforeNavigation=state.Clone();int navigationWrites=Store.Writes;
                navigate();
                Assert.That(Store.Writes,Is.EqualTo(navigationWrites+1),"Preserve exactly one existing navigation save; completed-story rendering must not save again");
                var after=Flow.Game.State.Clone();
                Assert.That(System.DateTimeOffset.TryParse(after.lastSaveTime,out _),Is.True,"Navigation keeps valid save metadata");
                Assert.That(Mofumachi.Core.StateCodec.Encode(Store.Load().State),Is.EqualTo(Mofumachi.Core.StateCodec.Encode(after)),"The saved snapshot must match the live state");
                after.lastSaveTime=beforeNavigation.lastSaveTime;
                Assert.That(Mofumachi.Core.StateCodec.Encode(after),Is.EqualTo(Mofumachi.Core.StateCodec.Encode(beforeNavigation)),"Completed-story navigation must not change progress, rewards, or the sparse board");
            }
            state.completedQuestIds.Clear();state.completedQuestIds.AddRange(Mofumachi.Core.QuestCatalog.All.Select(q=>q.Id));
            state.activeQuestId=Mofumachi.Core.QuestCatalog.All.Last().Id;state.townGrowthLevel=6;
            Flow.Daily.RecordMerge();Flow.Daily.RecordMerge();NavigateWithoutChangingProgress(Flow.ShowMerge);
            int writes=Store.Writes;string before=Mofumachi.Core.StateCodec.Encode(state);yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge daily progress").text,Is.EqualTo("今日の合成　2 / 3"));
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge daily next action").text,Does.Contain("あと1回"));
            Assert.That(Flow.GetComponentsInChildren<Text>().All(t=>!t.text.Contains("すべての依頼が完了")),Is.True);
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().Length,Is.EqualTo(30),"Completed-story rendering must retain all thirty board cells");
            Assert.That(Flow.GetComponentsInChildren<ItemGraphic>().Count(a=>a.name=="Item art"),Is.EqualTo(state.mergeBoard.Count),"Completed-story rendering must show only the saved items without filling empty cells");
            Assert.That(Mofumachi.Core.StateCodec.Encode(state),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.Daily.RecordMerge();NavigateWithoutChangingProgress(Flow.ShowMerge);
            writes=Store.Writes;before=Mofumachi.Core.StateCodec.Encode(state);yield return null;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge daily next action").text,Does.Contain("話しかけ"));
            Assert.That(Mofumachi.Core.StateCodec.Encode(state),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));
            var next=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");Assert.That(next.interactable,Is.True);NavigateWithoutChangingProgress(next.onClick.Invoke);
            before=Mofumachi.Core.StateCodec.Encode(state);yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Mofumachi.Core.StateCodec.Encode(state),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes+1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PortraitCompositionKeepsItemDetailsAndAllActionsReadable()
        {
            yield return EnterHome();Flow.ShowMerge();Flow.TapCell(0);
            var canvas=new GameObject("Merge composition fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            try
            {
                var root=(RectTransform)canvas.transform;var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
                root.sizeDelta=new Vector2(360,808);var page=UIWidgets.Node("Merge composition page",root,new Rect(0,0,1,1));
                QuestMergeScreens.BuildMerge(page,new ScreenContext(Flow,widgets));
                foreach(var size in new[]{new Vector2(360,536),new Vector2(320,536),new Vector2(360,592),new Vector2(360,740),new Vector2(360,808),new Vector2(320,536)})
                {
                    root.sizeDelta=size;float height=size.y;
                    Canvas.ForceUpdateCanvases();page.GetComponent<ProductionFrameLayout>().ResizeNow();page.GetComponentInChildren<MergeGridLayout>().ResizeNow();Canvas.ForceUpdateCanvases();
                    foreach(var text in page.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Requirement ")||t.name.StartsWith("Merge selected ")||t.name.StartsWith("Generate frame ")||t.name=="Merge actions label"||t.name=="Deliver label"))
                        Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.5f),text.name+" "+height+" "+text.text);
                    var frames=page.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Generate frame ")).ToArray();
                    if(height>=740)foreach(var frame in frames)Assert.That(((RectTransform)frame.transform).rect.width,Is.GreaterThanOrEqualTo(96));
                    var board=page.GetComponentsInChildren<MergeBoardView>();Assert.That(board.Length,Is.EqualTo(30));
                    foreach(var cell in board){var r=((RectTransform)cell.transform).rect;Assert.That(r.width,Is.GreaterThanOrEqualTo(48));Assert.That(r.height,Is.GreaterThanOrEqualTo(48));}
                    foreach(var button in page.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Nav ")||b.name.StartsWith("Generate frame ")||b.name=="Merge actions"||b.name=="Deliver"))
                    {var target=((RectTransform)button.transform).rect;Assert.That(target.width,Is.GreaterThanOrEqualTo(48),button.name+" "+size);Assert.That(target.height,Is.GreaterThanOrEqualTo(48),button.name+" "+size);}
                    var labels=page.GetComponentsInChildren<Text>().Where(t=>t.name=="Nav label").ToArray();Assert.That(labels.Length,Is.EqualTo(3));
                    foreach(var label in labels)
                    {
                        Assert.That(label.fontSize,Is.GreaterThanOrEqualTo(12));Assert.That(label.resizeTextForBestFit,Is.False);
                        Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.5f),label.text+" "+size);
                        Assert.That(label.cachedTextGenerator.vertexCount,Is.GreaterThan(4),label.text+" must generate visible glyphs at "+size);
                    }
                    var feedback=page.Find("Feedback").GetComponent<Text>();var surface=page.Find("Feedback surface").GetComponent<Graphic>();
                    Assert.That(surface.transform.GetSiblingIndex(),Is.LessThan(feedback.transform.GetSiblingIndex()),"Feedback paper must render behind its message, including an already populated feedback surface");
                    Assert.That(surface.raycastTarget,Is.False);Assert.That(feedback.raycastTarget,Is.False);
                    if(height<700)Assert.That(feedback.gameObject.activeSelf,Is.False,"Selected details already appear above the board and must not add an obscuring duplicate");
                    foreach(string message in new[]{"キラッ！ アイテムが育ちました。","移動しました。",UIStrings.BoardFull,UIStrings.DifferentKind+"\nパン Lv.1を選びました。",UIStrings.DifferentLevel,UIStrings.HighestLevel,UIStrings.MergeBusy,UIStrings.OutsideBoard,UIStrings.EmptySource,"セーブできませんでした。保存先を確認してください。"})
                    {
                        page.GetComponent<ProductionFrameLayout>().SetMessage(message);Canvas.ForceUpdateCanvases();
                        Assert.That(feedback.gameObject.activeSelf,Is.True,message);Assert.That(surface.gameObject.activeSelf,Is.True,message);
                        Assert.That(feedback.text,Is.EqualTo(message));Assert.That(feedback.fontSize,Is.GreaterThanOrEqualTo(14));Assert.That(feedback.resizeTextForBestFit,Is.False);
                        Assert.That(feedback.preferredHeight,Is.LessThanOrEqualTo(feedback.rectTransform.rect.height+.5f),message+" "+size);
                        Assert.That(feedback.cachedTextGenerator.vertexCount,Is.GreaterThan(4),message);
                        if(message=="キラッ！ アイテムが育ちました。"||message=="移動しました。")
                            Assert.That(page.Find("Merge progress/Merge goal content").gameObject.activeSelf,Is.True,"Normal merge/move notices must preserve live delivery counts at "+size);
                        var feedbackBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(page,feedback.transform);
                        var surfaceBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(page,surface.transform);
                        foreach(var cell in board)
                        {
                            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(page,cell.transform);
                            bool Overlaps(Bounds other)=>bounds.min.x<other.max.x&&bounds.max.x>other.min.x&&bounds.min.y<other.max.y&&bounds.max.y>other.min.y;
                            Assert.That(Overlaps(feedbackBounds),Is.False,cell.name+" must remain visible during "+message+" at "+size);
                            Assert.That(Overlaps(surfaceBounds),Is.False,cell.name+" must not be covered by feedback paper at "+size);
                        }
                    }
                    page.GetComponent<ProductionFrameLayout>().SetMessage("");Canvas.ForceUpdateCanvases();
                    Assert.That(page.Find("Merge progress/Merge goal content").gameObject.activeSelf,Is.True,"Clearing the message restores requirements/purpose");
                    Assert.That(page.Find("Merge progress/Merge selected item").gameObject.activeSelf,Is.True,"Clearing the message restores item details");
                    foreach(string notice in new[]{UIStrings.DeliveryHint(Flow.Game),"お茶 Lv.1を作りました。"+UIStrings.MergeSelectionHint,"パンを作れます。"+UIStrings.MergeSelectionHint})
                    {
                        page.GetComponent<ProductionFrameLayout>().SetMessage(notice);Canvas.ForceUpdateCanvases();
                        Assert.That(page.Find("Merge progress/Merge goal content").gameObject.activeSelf,Is.True,"Acceptance, deselection and generation must keep delivery counts visible at "+size);
                        if(height<700&&notice==UIStrings.DeliveryHint(Flow.Game))Assert.That(feedback.gameObject.activeSelf,Is.False,"The requirement card already shows the delivery hint");
                        if(feedback.gameObject.activeSelf)Assert.That(feedback.preferredHeight,Is.LessThanOrEqualTo(feedback.rectTransform.rect.height+.5f),notice+" "+size);
                    }
                    page.GetComponent<ProductionFrameLayout>().SetMessage("");
                }
            }finally{UnityEngine.Object.DestroyImmediate(canvas);}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ThirtyRoundedSquaresKeepRectangularInputAndReadableLevelBadges()
        {
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();
            var dishes=Flow.GetComponentsInChildren<Graphic>().Where(g=>g.name=="Cell porcelain dish").ToArray();
            Assert.That(dishes.Length,Is.EqualTo(30));
            foreach(var dish in dishes){Assert.That(dish.raycastTarget,Is.False);Assert.That(dish.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(100));}
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().Length,Is.EqualTo(30));
            foreach(var level in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Item level"))
            {Assert.That(level.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontBold")));Assert.That(level.rectTransform.rect.height,Is.GreaterThanOrEqualTo(level.preferredHeight-1));}
            LogAssert.NoUnexpectedReceived();
        }
    }
}
