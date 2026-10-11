using System;
using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class ChapterFlowTests:SliceUiTestFixture
    {
        private IEnumerator DeliverNext()
        {
            Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);
            foreach(var r in Flow.Game.Quests.ActiveQuest.Requirements)Assert.That(Flow.Game.AddItem(r.ItemId,r.Level),Is.True);
            Assert.That(Flow.Deliver().Success,Is.True);yield return new WaitForSecondsRealtime(1.25f);Flow.ShowGrowth();
        }
        [UnityTest] public IEnumerator EveryChapterHasExplicitClosureAndFinale()
        {
            yield return EnterHome();
            for(int i=0;i<6;i++)
            {
                yield return DeliverNext();Flow.ContinueAfterGrowth();
                Assert.That(Flow.CurrentScreen,Is.EqualTo(i%2==1?ScreenId.ChapterSummary:ScreenId.Home));
                if(i%2==1){Assert.That(Flow.LastChapter.IsFinale,Is.EqualTo(i==5));Flow.GoBack();}
            }
            Assert.That(Flow.ReviewLastChapter(),Is.True);Assert.That(Flow.Game.State.coins,Is.EqualTo(330));
            Assert.That(Flow.GetComponentsInChildren<CharacterBodyGraphic>().Length,Is.EqualTo(6));
            Assert.That(Flow.GetComponentsInChildren<CharacterBodyGraphic>().All(b=>b.Expression==CharacterExpression.Smile),Is.True);
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Chapter total").text,Does.Contain("6 / 6"));
            Assert.That(QuestCatalog.NextFor(Flow.Game.State),Is.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ChapterReentryAndSaveFailureNeverPayOrAdvance()
        {
            yield return EnterHome();yield return DeliverNext();Flow.ContinueAfterGrowth();Flow.ShowQuest();Flow.AcceptQuest();Flow.Game.AddItem("bread",2);
            Store.Fail=true;Assert.That(Flow.Deliver().SaveFailed,Is.True);Assert.That(ChapterProgress.For(Flow.Game.State).LastCompletedChapter,Is.Null);
            Store.Fail=false;Assert.That(Flow.Deliver().Success,Is.True);yield return new WaitForSecondsRealtime(1.25f);Flow.ShowGrowth();
            Store.Fail=true;Flow.ContinueAfterGrowth();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Growth));Store.Fail=false;
            Flow.ContinueAfterGrowth();var chapter=Flow.LastChapter;int coins=Flow.Game.State.coins;var rewards=Flow.Game.State.claimedRewardIds.ToArray();
            Flow.ContinueAfterGrowth();Flow.ShowSettings();Flow.ShowCharacterSelection();Flow.PreviewFavorite("noah");Flow.ConfirmFavorite();Flow.GoBack();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.ChapterSummary));Assert.That(Flow.LastChapter,Is.SameAs(chapter));
            Store.Fail=true;Assert.That(Flow.ReturnToTitle(),Is.False);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.ChapterSummary));Store.Fail=false;
            Flow.GoBack();Assert.That(Flow.ReviewLastChapter(),Is.True);Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));Assert.That(Flow.Game.State.claimedRewardIds,Is.EqualTo(rewards));
            Assert.That(Flow.Persist(),Is.True);yield return SceneManager.LoadSceneAsync("GameScene");yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator UnknownActiveQuestDoesNotPretendStoryComplete()
        {
            yield return EnterHome();Flow.Game.State.activeQuestId="unknown-active";Flow.ShowHome();Flow.ShowQuest();
            Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.text==UIStrings.UnknownQuest),Is.True);
            Assert.That(Flow.GetComponentsInChildren<Text>().All(t=>!t.text.Contains("すべての依頼が完了")),Is.True);
            Assert.That(Flow.AcceptQuest(),Is.False);Assert.That(Flow.Game.State.activeQuestId,Is.EqualTo("unknown-active"));
            Assert.That(Flow.ReviewLastChapter(),Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ChapterPagesFitShortPortraitAndActualValues()
        {
            yield return EnterHome();var state=Flow.Game.State;state.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));state.coins=int.MaxValue;state.townGrowthLevel=int.MaxValue;
            var canvas=new GameObject("Chapter layout",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var rect=(RectTransform)canvas.transform;var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
            try{foreach(float height in new[]{592f,640f,808f})foreach(var chapter in ChapterCatalog.All)
            {
                rect.sizeDelta=new Vector2(360,height);var page=UIWidgets.Node("Summary fixture",rect,new Rect(0,0,1,1));
                ChapterSummaryScreen.Build(page,new ScreenContext(Flow,widgets),ChapterPresentation.Create(state,chapter.Id));Canvas.ForceUpdateCanvases();
                foreach(var text in page.GetComponentsInChildren<Text>()){Assert.That(text.resizeTextForBestFit,Is.False);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);}
                if(chapter.Id=="chapter-03")
                {
                    var regions=page.GetComponentsInChildren<RectTransform>().Where(r=>r.name.StartsWith("Celebrating ")).OrderBy(r=>Bounds(page,r).min.x).ToArray();
                    Assert.That(regions.Length,Is.EqualTo(6));
                    for(int i=1;i<regions.Length;i++)Assert.That(Bounds(page,regions[i-1]).max.x,Is.LessThanOrEqualTo(Bounds(page,regions[i]).min.x),"Finale columns overlap");
                }
                Assert.That(page.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Chapter ")).Count(),Is.EqualTo(2));UnityEngine.Object.DestroyImmediate(page.gameObject);
            }
                foreach(float height in new[]{592f,640f,808f})
                {
                    rect.sizeDelta=new Vector2(360,height);state.completedQuestIds.Clear();state.activeQuestId="tea-01";
                    var page=UIWidgets.Node("Quest fixture",rect,new Rect(0,0,1,1));QuestMergeScreens.BuildQuest(page,new ScreenContext(Flow,widgets));Canvas.ForceUpdateCanvases();
                    var body=page.GetComponentsInChildren<CharacterBodyGraphic>().Single();var label=page.GetComponentsInChildren<Text>().Single(t=>t.name=="Quest chapter");
                    Assert.That(Bounds(page,body.rectTransform).max.y,Is.LessThanOrEqualTo(Bounds(page,label.rectTransform).min.y),"Chapter caption overlaps requester");
                    Assert.That(label.fontSize,Is.GreaterThanOrEqualTo(14));Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+1));
                    UnityEngine.Object.DestroyImmediate(page.gameObject);
                }
            }finally{UnityEngine.Object.DestroyImmediate(canvas);}LogAssert.NoUnexpectedReceived();
        }
        private static Bounds Bounds(RectTransform root,RectTransform child)=>RectTransformUtility.CalculateRelativeRectTransformBounds(root,child);
    }
}
