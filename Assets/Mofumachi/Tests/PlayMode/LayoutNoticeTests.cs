using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class LayoutNoticeTests : SliceUiTestFixture
    {
        [Test] public void PortraitUsesFullSafeHeightAndLandscapeUsesCenteredViewport()
        {
            var canvas = new GameObject("Layout fixture", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvas.transform;
            var layout = canvas.AddComponent<ResponsiveUILayout>(); layout.Initialize(canvas.GetComponent<Canvas>());
            try
            {
                foreach (var size in new[] { new Vector2(360, 640), new Vector2(393, 808), new Vector2(1080, 1920), new Vector2(1080, 2220), new Vector2(1440, 2960), new Vector2(2560, 1440) })
                {
                    rect.sizeDelta = size;
                    var safe = new Rect(0, 20, size.x, size.y - 48);
                    layout.ApplyViewport(size, safe); Canvas.ForceUpdateCanvases();
                    Assert.That(layout.FullScreenRoot.anchorMin, Is.EqualTo(Vector2.zero));
                    Assert.That(layout.FullScreenRoot.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(layout.SafeRoot.anchorMin.y, Is.EqualTo(20 / size.y).Within(.0001));
                    if (size.y >= size.x)
                    {
                        Assert.That(layout.ContentRoot.anchorMin, Is.EqualTo(Vector2.zero));
                        Assert.That(layout.ContentRoot.anchorMax, Is.EqualTo(Vector2.one));
                    }
                    else Assert.That(layout.ContentRoot.rect.width / layout.ContentRoot.rect.height, Is.EqualTo(9f / 16).Within(.001));
                }
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        [UnityTest] public IEnumerator NoticeBlocksUntilSaveSucceedsAndCannotBeBypassed()
        {
            yield return LoadTitle();
            var start = Flow.GetComponentsInChildren<Button>().Single(b => b.name == "Start");
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, start.transform.position) };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.First().gameObject.GetComponentInParent<Button>(), Is.SameAs(start));
            ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Flow.BeginGame(); Flow.ShowHome(); Flow.GoBack();
            Assert.That(Flow.IsPurchaseNoticeOpen, Is.True);
            Assert.That(Flow.GetComponentsInChildren<PurchaseNoticeView>().Length, Is.EqualTo(1));
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Title));
            Store.Fail = true;
            Assert.That(Flow.ConfirmPurchaseNotice(), Is.False);
            Assert.That(Flow.Game.State.purchaseNoticeAcknowledged, Is.False);
            Assert.That(Flow.IsPurchaseNoticeOpen, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("TitleScene"));
            Store.Fail = false;
            Assert.That(Flow.ConfirmPurchaseNotice(), Is.True);
            Assert.That(Flow.ConfirmPurchaseNotice(), Is.False);
            yield return null; yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            Assert.That(Store.Load().State.purchaseNoticeAcknowledged, Is.True);
            yield return LoadTitle(); Flow.BeginGame(); yield return null; yield return null;
            Assert.That(Flow.IsPurchaseNoticeOpen, Is.False);
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Home));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator UnconfirmedDirectGameSceneStartsAtTitle()
        {
            yield return SceneManager.LoadSceneAsync("GameScene"); yield return null;
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Title));
            Flow.ShowMerge();
            Assert.That(Flow.CurrentScreen, Is.EqualTo(ScreenId.Title));
            Assert.That(Flow.IsPurchaseNoticeOpen, Is.True);
        }

        [UnityTest] public IEnumerator RequiredJapaneseTextFitsAndHomeUsesActualHudValues()
        {
            yield return EnterHome();
            Flow.Game.State.coins = int.MaxValue; Flow.Game.State.townGrowthLevel = int.MaxValue;
            Flow.ShowHome(); Canvas.ForceUpdateCanvases();
            var hud = Flow.GetComponentsInChildren<Text>().Single(t => t.name == "Coins value");
            Assert.That(hud.text, Is.EqualTo(int.MaxValue.ToString("N0")));
            Assert.That(hud.resizeTextForBestFit, Is.False);
            Assert.That(hud.preferredWidth, Is.LessThanOrEqualTo(hud.rectTransform.rect.width + 1));
            foreach (var t in Flow.GetComponentsInChildren<Text>())
            {
                Assert.That(t.resizeTextForBestFit, Is.False);
                Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(t.rectTransform.rect.height + 1), t.name);
            }
        }
    }
}
