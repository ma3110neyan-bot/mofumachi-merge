using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class TownMotionPresentationTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator StationaryDragAndLifecycleStopGait()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            hero.Movement.TryBeginDrag(1);var label=hero.GetComponentInChildren<Text>().rectTransform.localPosition;
            for(int i=0;i<30;i++)life.AdvanceFrame(1f/60);
            Assert.That(hero.VisualRoot.anchoredPosition.y,Is.EqualTo(0).Within(.01f));
            Assert.That(hero.GetComponentInChildren<Text>().rectTransform.localPosition,Is.EqualTo(label));
            hero.Movement.SetTarget(1,TownLifeView.RequestEntrance);for(int i=0;i<30;i++)life.AdvanceFrame(1f/60);
            Assert.That(hero.Movement.TryBeginDrag(2),Is.False);
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=1});
            var stopped=hero.Position;life.AdvanceFrame(10);life.AdvanceFrame(1f/60);
            Assert.That(hero.Position.X,Is.EqualTo(stopped.X));Assert.That(hero.Position.Y,Is.EqualTo(stopped.Y));
            Flow.OpenShopPreview();Assert.That(hero.Movement.OwnedPointerId,Is.Null);Flow.CloseShopPreview();
            hero.Movement.TryBeginDrag(1);Flow.ShowSettings();Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Flow.GoBack();Flow.SendMessage("OnApplicationPause",true);
            Assert.That(Flow.GetComponentsInChildren<TownLifeView>().All(v=>!v.enabled),Is.True);
            Flow.SendMessage("OnApplicationPause",false);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RenderedCornerAndArrivalUseTheSameStreet()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            hero.Movement.TryBeginDrag(1);hero.Movement.SetTarget(1,TownLifeView.RequestEntrance);
            for(int i=0;i<300;i++)life.AdvanceFrame(1f/30);
            Assert.That(life.NearbyFacility,Is.Null);hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=1});
            Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            Assert.That(hero.RenderPosition.X,Is.EqualTo(hero.Position.X).Within(.00001f));
            Assert.That(Vector2.Distance(((RectTransform)hero.transform).anchoredPosition,life.Local(hero.Position)),Is.LessThan(.01f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WaterFallsAndRipplesAnimateWithoutRaycast()
        {
            yield return EnterHome();var water=Flow.GetComponentInChildren<TownAmbientGraphic>();
            Assert.That(water.raycastTarget,Is.False);Canvas.ForceUpdateCanvases();
            var before=water.canvasRenderer.GetMesh();var vertices=before.vertices;
            water.AdvanceWater(.2f);Canvas.ForceUpdateCanvases();
            var after=water.canvasRenderer.GetMesh();
            Assert.That(after.vertexCount,Is.GreaterThan(0).And.LessThanOrEqualTo(1500));
            Assert.That(after.vertices.SequenceEqual(vertices),Is.False);
            Assert.That(Flow.GetComponentsInChildren<Text>().Count(t=>t.name=="Town bubble"),Is.LessThanOrEqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
