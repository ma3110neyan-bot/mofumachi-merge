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
            state.completedQuestIds.Clear();state.completedQuestIds.AddRange(Mofumachi.Core.QuestCatalog.All.Select(q=>q.Id));
            state.activeQuestId=Mofumachi.Core.QuestCatalog.All.Last().Id;state.townGrowthLevel=6;
            Flow.Daily.RecordMerge();Flow.Daily.RecordMerge();int writes=Store.Writes;string before=Mofumachi.Core.StateCodec.Encode(state);
            Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge daily progress").text,Is.EqualTo("今日の合成　2 / 3"));
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge daily next action").text,Does.Contain("あと1回"));
            Assert.That(Flow.GetComponentsInChildren<Text>().All(t=>!t.text.Contains("すべての依頼が完了")),Is.True);
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().Length,Is.EqualTo(30));
            Assert.That(Flow.GetComponentsInChildren<ItemGraphic>().Count(a=>a.name=="Item art"),Is.EqualTo(state.mergeBoard.Count));
            Assert.That(Mofumachi.Core.StateCodec.Encode(state),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.Daily.RecordMerge();Flow.ShowMerge();yield return null;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge daily next action").text,Does.Contain("話しかけ"));
            var next=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");Assert.That(next.interactable,Is.True);next.onClick.Invoke();yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Mofumachi.Core.StateCodec.Encode(state),Is.EqualTo(before));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PortraitCompositionKeepsItemDetailsAndAllActionsReadable()
        {
            yield return EnterHome();Flow.ShowMerge();Flow.TapCell(0);
            var canvas=new GameObject("Merge composition fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            try
            {
                var root=(RectTransform)canvas.transform;var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
                foreach(int height in new[]{536,592,740,808})
                {
                    root.sizeDelta=new Vector2(360,height);var page=UIWidgets.Node("Merge composition page",root,new Rect(0,0,1,1));
                    QuestMergeScreens.BuildMerge(page,new ScreenContext(Flow,widgets));Canvas.ForceUpdateCanvases();page.GetComponent<ProductionFrameLayout>().ResizeNow();page.GetComponentInChildren<MergeGridLayout>().ResizeNow();Canvas.ForceUpdateCanvases();
                    foreach(var text in page.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Requirement ")||t.name.StartsWith("Merge selected ")||t.name.StartsWith("Generate frame ")||t.name=="Merge actions label"||t.name=="Deliver label"))
                        Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.5f),text.name+" "+height+" "+text.text);
                    var frames=page.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Generate frame ")).ToArray();
                    if(height>=740)foreach(var frame in frames)Assert.That(((RectTransform)frame.transform).rect.width,Is.GreaterThanOrEqualTo(96));
                    var board=page.GetComponentsInChildren<MergeBoardView>();Assert.That(board.Length,Is.EqualTo(30));
                    foreach(var cell in board){var r=((RectTransform)cell.transform).rect;Assert.That(r.width,Is.GreaterThanOrEqualTo(48));Assert.That(r.height,Is.GreaterThanOrEqualTo(48));}
                    UnityEngine.Object.DestroyImmediate(page.gameObject);
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
