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
        [UnityTest] public IEnumerator PortraitFramesKeepSeparateSlotsOnShortAndTallScreens()
        {
            yield return EnterHome();
            var canvas=new GameObject("Portrait slot fixture",typeof(RectTransform),typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var root=(RectTransform)canvas.transform;
            var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
            try
            {
                foreach(float height in new[]{592f,640f,740f,808f})
                {
                    root.sizeDelta=new Vector2(360,height);
                    var page=UIWidgets.Node("Home slot fixture",root,new Rect(0,0,1,1));
                    TopHomeScreens.BuildHome(page,new ScreenContext(Flow,widgets));Canvas.ForceUpdateCanvases();
                    var frames=page.GetComponentsInChildren<CharacterReaction>();Assert.That(frames.Length,Is.EqualTo(6));
                    for(int i=0;i<frames.Length;i++)
                    {
                        var rect=(RectTransform)frames[i].transform;
                        Assert.That(rect.rect.width,Is.InRange(48f,64f),"Portrait must fit its own slot, not the whole town");
                        Assert.That(rect.rect.width,Is.EqualTo(rect.rect.height).Within(.01));
                        var a=RectTransformUtility.CalculateRelativeRectTransformBounds(page,rect);
                        for(int j=0;j<i;j++)
                        {
                            var b=RectTransformUtility.CalculateRelativeRectTransformBounds(page,frames[j].transform);
                            Assert.That(new Rect(a.min.x,a.min.y,a.size.x,a.size.y).Overlaps(new Rect(b.min.x,b.min.y,b.size.x,b.size.y)),Is.False);
                        }
                    }
                    frames[0].GetComponent<Button>().onClick.Invoke();frames[1].GetComponent<Button>().onClick.Invoke();
                    Assert.That(frames[0].GetComponent<RoundedPanelGraphic>().color,Is.EqualTo(UIWidgets.Cream));
                    Assert.That(frames[1].GetComponent<RoundedPanelGraphic>().color,Is.EqualTo(UIWidgets.Pink));
                    Object.DestroyImmediate(page.gameObject);
                }
            }
            finally{Object.DestroyImmediate(canvas);}
        }

        [UnityTest] public IEnumerator CharacterReactionDoesNotWriteOrChangeGameProgress()
        {
            yield return EnterHome();
            var character = Flow.GetComponentsInChildren<CharacterReaction>().First();
            int writes = Store.Writes, coins = Flow.Game.State.coins;
            var start = character.transform.localScale;
            character.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(character.transform.localScale, Is.Not.EqualTo(start));
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Flow.Game.State.coins, Is.EqualTo(coins));
            Assert.That(Store.Writes, Is.EqualTo(writes));
            Flow.SendMessage("OnApplicationPause", true);
            Assert.That(Flow.GetComponentsInChildren<CharacterReaction>().All(r => !r.enabled), Is.True);
            Assert.That(Flow.GetComponentsInChildren<TownAmbientGraphic>().All(r => !r.enabled), Is.True);
            Flow.SendMessage("OnApplicationPause", false);
            Assert.That(Flow.GetComponentsInChildren<CharacterReaction>().All(r => r.enabled), Is.True);
            LogAssert.NoUnexpectedReceived();
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
