using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class PopPresentationTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator FullBodyActorsFollowBackgroundCropOnShortAndTallScreens()
        {
            yield return EnterHome();
            var canvas=new GameObject("Town crop fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var root=(RectTransform)canvas.transform;
            var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
            try
            {
                foreach(float height in new[]{592f,640f,740f,808f})
                {
                    root.sizeDelta=new Vector2(360,height);var background=widgets.Background(Resources.Load<Texture2D>("Mofumachi/TownBackground"),root);background.GetComponent<BackgroundCover>().SendMessage("Fit");
                    var page=UIWidgets.Node("Home crop fixture",root,new Rect(0,0,1,1));TopHomeScreens.BuildHome(page,new ScreenContext(Flow,widgets),background);Canvas.ForceUpdateCanvases();yield return null;
                    var actors=page.GetComponentsInChildren<TownActor>();Assert.That(actors.Length,Is.EqualTo(6));Assert.That(actors.Count(a=>a.Controlled),Is.EqualTo(1));
                    var life=page.GetComponentInChildren<TownLifeView>();
                    foreach(var actor in actors)
                    {
                        var rect=(RectTransform)actor.transform;Assert.That(rect.rect.width,Is.EqualTo(62));Assert.That(rect.rect.height,Is.EqualTo(78));
                        var expected=life.Local(actor.Position);Assert.That(Vector2.Distance(rect.anchoredPosition,expected),Is.LessThan(3));
                        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(page,rect);var quest=RectTransformUtility.CalculateRelativeRectTransformBounds(page,page.Find("Home quest"));
                        Assert.That(bounds.min.y,Is.GreaterThan(quest.max.y));
                        var body=actor.GetComponentInChildren<CharacterBodyGraphic>();
                        Assert.That(body,Is.Not.Null,"Each town actor must have its own full-body graphic.");
                        Assert.That(body.rectTransform,Is.SameAs(actor.VisualRoot));
                        Assert.That(body.CharacterId,Is.EqualTo(actor.CharacterId));
                    }
                    Object.DestroyImmediate(page.gameObject);Object.DestroyImmediate(background.gameObject);
                }
            }
            finally{Object.DestroyImmediate(canvas);}
        }
        [UnityTest] public IEnumerator CharacterReactionDoesNotWriteOrChangeGameProgress()
        {
            yield return EnterHome();var actor=Flow.GetComponentsInChildren<TownActor>().First(a=>!a.Controlled);
            int writes=Store.Writes,coins=Flow.Game.State.coins;actor.React();yield return new WaitForSecondsRealtime(.12f);
            var body=actor.GetComponentInChildren<CharacterBodyGraphic>();
            Assert.That(body,Is.Not.Null,"The tapped actor must have a full-body graphic.");
            Assert.That(body.rectTransform,Is.SameAs(actor.VisualRoot));
            Assert.That(body.Expression,Is.EqualTo(CharacterExpression.Smile));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.SendMessage("OnApplicationPause",true);Assert.That(Flow.GetComponentsInChildren<TownLifeView>().All(r=>!r.enabled),Is.True);
            Assert.That(Flow.GetComponentsInChildren<TownAmbientGraphic>().All(r=>!r.enabled),Is.True);
            Flow.SendMessage("OnApplicationPause",false);Assert.That(Flow.GetComponentsInChildren<TownLifeView>().All(r=>r.enabled),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SuccessfulDeliveryClearsOldMergeMessageAndFailureKeepsReason()
        {
            yield return EnterHome();
            Assert.That(Flow.AcceptQuest(), Is.True);
            Assert.That(Flow.DropItem(0, 1), Is.True);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge), Is.Not.Empty);
            Store.Fail = true;
            Assert.That(Flow.Deliver().Success, Is.False);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge), Is.Not.Empty);
            Assert.That(Flow.Game.State.coins, Is.EqualTo(0));
            Store.Fail = false;
            Assert.That(Flow.Deliver().Success, Is.True);
            Flow.ShowMerge();
            Assert.That(Flow.FeedbackFor(ScreenId.Merge), Is.Empty);
            Assert.That(Flow.Game.State.coins, Is.EqualTo(30));
            Assert.That(Flow.Game.State.mergeBoard, Is.Empty);
        }
    }
}
