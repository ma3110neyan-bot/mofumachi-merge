using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class CharacterSelectionTests : SliceUiTestFixture
    {
        private IEnumerator FirstSelection()
        {
            yield return LoadTitle(); Flow.BeginGame();
            Assert.That(Flow.ConfirmPurchaseNotice(), Is.True);
            yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.CharacterSelection));
        }
        [UnityTest] public IEnumerator FirstStartSelectsAndSecondStartResumesFavorite()
        {
            yield return FirstSelection(); int writes = Store.Writes;
            Flow.PreviewFavorite("miel"); Flow.PreviewFavorite("not-a-character");
            Assert.That(Flow.PreviewFavoriteId, Is.EqualTo("miel"));
            Assert.That(Store.Writes, Is.EqualTo(writes)); Assert.That(Flow.Game.State.favoriteCharacterId, Is.Empty);
            Assert.That(Flow.ConfirmFavorite(), Is.True);
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Store.Load().State.favoriteCharacterId, Is.EqualTo("miel"));
            Assert.That(Flow.ReturnToTitle(), Is.True); yield return null; yield return null;
            Assert.That(Flow.GetComponentInChildren<CharacterBodyGraphic>().CharacterId, Is.EqualTo("miel"));
            var companions = Flow.GetComponentsInChildren<RawImage>().Where(i => i.name.StartsWith("Character ")).Select(i => i.name);
            Assert.That(companions, Is.EquivalentTo(new[] { "Character 0", "Character 1", "Character 2", "Character 3", "Character 5" }),
                "TOP must show the selected partner once and all five other characters.");
            Flow.Initialize(Store); Flow.BeginGame(); yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Flow.Game.State.favoriteCharacterId, Is.EqualTo("miel"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SelectionSaveFailureAndCancelKeepProgress()
        {
            yield return FirstSelection(); var before = Store.Load().State;
            Store.Fail = true; Flow.PreviewFavorite("noah");
            Assert.That(Flow.ConfirmFavorite(), Is.False);
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.CharacterSelection));
            Assert.That(Flow.Game.State.favoriteCharacterId, Is.Empty);
            Assert.That(Flow.AcceptQuest(), Is.False); Assert.That(Flow.DropItem(0, 1), Is.False);
            Flow.CreateTea(); Assert.That(Flow.Game.State.mergeBoard.Count, Is.EqualTo(before.mergeBoard.Count));
            Store.Fail = false; Flow.ShowHome();
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.CharacterSelection));
            Flow.GoBack(); yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Title));
            Assert.That(Store.Load().State.favoriteCharacterId, Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReselectionReturnsThroughSettingsToItsOrigin()
        {
            yield return EnterHome(); Flow.AcceptQuest(); Flow.DropItem(0, 1);
            yield return new WaitForSecondsRealtime(.3f); Assert.That(Flow.Deliver().Success, Is.True);
            yield return new WaitForSecondsRealtime(1.3f);
            var delivery = Flow.LastDelivery;
            foreach (var origin in new[] { ScreenId.Result, ScreenId.Growth, ScreenId.Quest })
            {
                if (origin == ScreenId.Growth) Flow.ShowGrowth();
                if (origin == ScreenId.Quest) Flow.ShowQuest();
                string favorite = Flow.Game.State.favoriteCharacterId;
                Flow.ShowSettings(); Flow.ShowCharacterSelection(); Flow.PreviewFavorite("noah");
                Flow.ShowSettings(); Flow.GoBack();
                Assert.That(Flow.CurrentScreen, Is.EqualTo(origin), "Selection's settings shortcut must retain the original return screen.");
                Assert.That(Flow.Game.State.favoriteCharacterId, Is.EqualTo(favorite), "An unconfirmed preview must not change the partner.");
                Flow.ShowSettings(); Flow.ShowCharacterSelection(); Flow.PreviewFavorite("fio");
                Assert.That(Flow.ConfirmFavorite(), Is.True);
                Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Settings)); Flow.GoBack();
                Assert.That(Flow.CurrentScreen, Is.EqualTo(origin));
                Assert.That(Flow.LastDelivery, Is.SameAs(delivery)); Assert.That(Flow.Game.State.coins, Is.EqualTo(30));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BookOpensWithTheWholeCharacterAndDescriptionAcrossPortraitWidths()
        {
            yield return EnterHome();
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
            foreach(var size in new[]{new Vector2(320,640),new Vector2(360,640),new Vector2(360,808)})
            {
                ((RectTransform)canvas.transform).sizeDelta=size;
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(size,new Rect(Vector2.zero,size));
                Flow.OpenCharacterBook("noah");Canvas.ForceUpdateCanvases();
                var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();
                Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(1).Within(.001f));
                var selectors=Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Partner ")).Select(b=>(RectTransform)b.transform).ToArray();
                Assert.That(selectors.Length,Is.EqualTo(6));
                foreach(var selector in selectors)
                {
                    Assert.That(selector.rect.width,Is.GreaterThanOrEqualTo(48),selector.name+" at "+size);
                    Assert.That(selector.rect.height,Is.GreaterThanOrEqualTo(48));
                    Assert.That(Bounds(scroll.viewport,selector).yMax,Is.EqualTo(Bounds(scroll.viewport,selectors[0]).yMax).Within(.01f));
                }
                foreach(var item in new[]{Flow.GetComponentsInChildren<CharacterBodyGraphic>().Single().rectTransform,Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Partner description").rectTransform})
                {
                    var bounds=Bounds(scroll.viewport,item);
                    Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-.5f),item.name+" must fit on the opening page at "+size);
                    Assert.That(bounds.yMax,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+.5f));
                }
                var description=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Partner description");
                Assert.That(description.preferredHeight,Is.LessThanOrEqualTo(description.rectTransform.rect.height+1));
                Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Book collection counts").text,Does.Contain("依頼"));
                Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Book unlocked reactions").text,Does.Contain("？？？"));
            }
            LogAssert.NoUnexpectedReceived();
        }
        private static Rect Bounds(RectTransform root,RectTransform item)
        {var corners=new Vector3[4];item.GetWorldCorners(corners);var min=(Vector2)root.InverseTransformPoint(corners[0]);var max=(Vector2)root.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
    }
}
