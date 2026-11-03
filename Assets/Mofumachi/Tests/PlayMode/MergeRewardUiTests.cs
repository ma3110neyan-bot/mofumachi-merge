using System.Collections;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class MergeRewardUiTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator CurrentQuestUsesOfficialArtAndActualCoinsWithoutSaving()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);
            var page=Flow.GetComponentInChildren<MergeRewardView>();Assert.That(page,Is.Not.Null);
            var title=page.transform.Find("Header/Page title ribbon");Assert.That(title.GetComponent<RibbonGraphic>().color.r,Is.GreaterThan(.9f));
            Assert.That(title.GetComponent<RibbonGraphic>().color.g,Is.LessThan(.6f));Assert.That(title.Find("Page title").GetComponent<Text>().text,Is.EqualTo("ごほうびMerge"));
            Assert.That(page.transform.Find("Merge progress/Current reward band").GetComponent<Graphic>().raycastTarget,Is.False);
            Assert.That(page.GetComponentsInChildren<Text>().Single(t=>t.name=="Current request caption").text,Is.EqualTo("現在の依頼"));
            Assert.That(page.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Reward amount").text,Is.EqualTo(QuestDefinition.First.Coins+" Coins"));
            Assert.That(page.GetComponentsInChildren<ItemGraphic>().Single(a=>a.name=="Requirement art tea").mainTexture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/ItemAtlas")));
            Assert.That(page.GetComponentsInChildren<Button>().Single(b=>b.name=="Generate frame tea").GetComponent<RoundedPanelGraphic>().color,Is.EqualTo(MergeRewardView.Rose));
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AcceptedIncompleteRequestStillHasALargeReviewAction()
        {
            yield return EnterHome();Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);Flow.ShowMerge();yield return null;
            var review=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");
            Assert.That(review.interactable,Is.True);Assert.That(review.GetComponentInChildren<Text>().text,Is.EqualTo("依頼を見る"));
            var management=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Merge actions");
            Assert.That(review.GetComponent<RoundedPanelGraphic>().gloss,Is.GreaterThan(management.GetComponent<RoundedPanelGraphic>().gloss));
            Assert.That(review.GetComponent<RoundedPanelGraphic>().borderWidth,Is.GreaterThan(management.GetComponent<RoundedPanelGraphic>().borderWidth));
            int coins=Flow.Game.State.coins,count=Flow.Game.State.mergeBoard.Count;
            review.onClick.Invoke();yield return null;Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(count));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CompletedRequestUsesExistingDeliveryAndExistingRewardOnly()
        {
            yield return EnterHome();Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);Flow.ShowMerge();
            Assert.That(Flow.DropItem(0,1),Is.True);yield return new WaitForSecondsRealtime(.3f);
            var deliver=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");Assert.That(deliver.interactable,Is.True);
            Assert.That(deliver.GetComponentInChildren<Text>().text,Is.EqualTo("納品する"));deliver.onClick.Invoke();yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Result));Assert.That(Flow.Game.State.coins,Is.EqualTo(QuestDefinition.First.Coins));
            Assert.That(Flow.Game.State.claimedRewardIds.Count(x=>x=="quest:"+QuestDefinition.First.Id),Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EmptyMotifsNeverCoverSavedItemsOrReceiveInput()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            var motif=typeof(MergeCellSurfaceGraphic).GetField("motif",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(var cell in cells)
            {
                int index=int.Parse(cell.name.Substring(5));var paper=cell.GetComponentInChildren<MergeCellSurfaceGraphic>();
                Assert.That(paper.raycastTarget,Is.False);Assert.That((int)motif.GetValue(paper),Is.EqualTo(Flow.Game.Board.At(index)==null?index:-1));
                Assert.That(paper.canvasRenderer.GetMesh().vertexCount,Is.LessThan(400),"Bounded lightweight cell decoration");
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator MultipleRequirementsKeepSeparateArtNamesAndCountsWithoutSaving()
        {
            yield return EnterHome();Flow.Game.State.activeQuestId="tea-02";
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);
            var fixture=new GameObject("Local reward spacing fixture",typeof(RectTransform),typeof(Canvas));fixture.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
            try
            {
                foreach(var size in new[]{new Vector2(320,536),new Vector2(360,640),new Vector2(360,740)})
                {
                    ((RectTransform)fixture.transform).sizeDelta=size;var root=UIWidgets.Node("Reward spacing page",(RectTransform)fixture.transform,new Rect(0,0,1,1));
                    QuestMergeScreens.BuildMerge(root,new ScreenContext(Flow,widgets));Canvas.ForceUpdateCanvases();root.GetComponent<ProductionFrameLayout>().ResizeNow();Canvas.ForceUpdateCanvases();
                    var cards=root.Find("Merge progress/Merge goal content").GetComponentsInChildren<Button>();Assert.That(cards.Length,Is.EqualTo(2));
                    foreach(var card in cards)
                    {
                        var r=(RectTransform)card.transform;Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(48));Assert.That(r.rect.width,Is.GreaterThanOrEqualTo(48));
                        var art=card.GetComponentInChildren<ItemGraphic>().rectTransform;
                        var count=card.GetComponentsInChildren<Text>().Single(t=>t.name.StartsWith("Requirement count "));
                        var name=card.GetComponentsInChildren<Text>().Single(t=>t.name.StartsWith("Requirement name "));
                        var artBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(r,art);var countBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(r,count.rectTransform);
                        var nameBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(r,name.rectTransform);
                        Assert.That(artBounds.min.y,Is.GreaterThanOrEqualTo(countBounds.max.y),"Icon must not cross the count row at "+size);
                        Assert.That(nameBounds.min.y,Is.GreaterThanOrEqualTo(countBounds.max.y),"Name must not cross the count row at "+size);
                        Assert.That(name.fontSize,Is.GreaterThanOrEqualTo(12));Assert.That(count.fontSize,Is.GreaterThanOrEqualTo(12));
                        Assert.That(name.preferredHeight,Is.LessThanOrEqualTo(name.rectTransform.rect.height+.5f));
                        Assert.That(name.preferredWidth,Is.LessThanOrEqualTo(name.rectTransform.rect.width+.5f));
                        Assert.That(count.preferredHeight,Is.LessThanOrEqualTo(count.rectTransform.rect.height+.5f));
                    }
                    Assert.That(root.GetComponentsInChildren<MergeBoardView>().Length,Is.EqualTo(30));UnityEngine.Object.DestroyImmediate(root.gameObject);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(fixture);}
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
    }
}
