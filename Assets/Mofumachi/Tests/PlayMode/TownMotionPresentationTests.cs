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
        [UnityTest] public IEnumerator GrowthHighlightStopsAndDecorationNeverTakesInput()
        {
            yield return EnterHome();var growth=Flow.GetComponentInChildren<TownGrowthView>();growth.Apply(6);growth.HighlightNewStage(3,6);
            Assert.That(growth.IsHighlighting,Is.True);var props=Flow.GetComponentInChildren<TownLifeView>().GetComponentsInChildren<TownGrowthPropGraphic>(true);
            Assert.That(props.Length,Is.EqualTo(System.Enum.GetValues(typeof(TownRegionId)).Cast<TownRegionId>().Sum(r=>TownGrowthPresentation.For(r,6).Count)));
            Assert.That(props.Length,Is.GreaterThan(0));Assert.That(props.All(g=>!g.raycastTarget),Is.True);
            growth.enabled=false;Assert.That(growth.IsHighlighting,Is.False);Assert.That(growth.VisibleStage,Is.EqualTo(6));
            foreach(var decoration in props)Assert.That(decoration.transform.localScale,Is.EqualTo(Vector3.one));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator StationaryDragAndLifecycleStopGait()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);
            hero.Movement.TryBeginDrag(1);var label=hero.GetComponentInChildren<Text>().rectTransform.localPosition;for(int i=0;i<30;i++)life.AdvanceFrame(1f/60);
            Assert.That(hero.VisualRoot.anchoredPosition.y,Is.EqualTo(0).Within(.01f));Assert.That(hero.GetComponentInChildren<Text>().rectTransform.localPosition,Is.EqualTo(label));
            Assert.That(hero.Movement.TryBeginDrag(2),Is.False);hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=1});life.GuideToFacility("quest");for(int i=0;i<12;i++)life.AdvanceFrame(1f/60);
            Assert.That(hero.VisualRoot.anchoredPosition.y,Is.GreaterThan(0));life.CancelInteraction();var stopped=hero.WorldPosition;life.AdvanceFrame(10);Assert.That(TownWorldLayout.Distance(hero.WorldPosition,stopped),Is.LessThan(.0001f));
            Assert.That(hero.Movement.TryBeginDrag(3),Is.True);Flow.OpenTownMenu();Assert.That(Flow.IsModalOpen,Is.True);Assert.That(hero.Movement.OwnedPointerId,Is.Null);Flow.CloseTownMenu();Flow.ShowSettings();Flow.GoBack();Flow.SendMessage("OnApplicationPause",true);
            Assert.That(Flow.GetComponentsInChildren<TownLifeView>().All(v=>!v.enabled),Is.True);Flow.SendMessage("OnApplicationPause",false);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RenderedCornerAndArrivalUseTheSameStreet()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);life.GuideToFacility("quest");
            for(int i=0;i<1800&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            Assert.That(life.Regions.HasHeroJourney,Is.False);Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            Assert.That(hero.RenderPosition.X,Is.EqualTo(hero.Position.X).Within(.00001f));Assert.That(Vector2.Distance(((RectTransform)hero.transform).anchoredPosition,life.World.Local(hero.WorldPosition)),Is.LessThan(.01f));LogAssert.NoUnexpectedReceived();
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
            var addition=Flow.GetComponentInChildren<TownFountainWaterGraphic>();var life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(addition.transform.parent,Is.SameAs(Flow.GetComponentInChildren<TownFountainGraphic>().transform));Assert.That(addition.raycastTarget,Is.False);
            float phase=addition.Phase;addition.AdvanceWater(.2f);Canvas.ForceUpdateCanvases();
            Assert.That(addition.Phase,Is.EqualTo(Mathf.Repeat(phase+.1f,10)).Within(.00001f),"Resume dt is bounded");
            Assert.That(addition.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(784));
            Assert.That(addition.canvasRenderer.GetMesh().vertexCount+after.vertexCount,Is.LessThanOrEqualTo(1500),"Keep the old and added water within the existing total mesh budget");
            phase=addition.Phase;addition.enabled=false;addition.AdvanceWater(.1f);Assert.That(addition.Phase,Is.EqualTo(phase));addition.enabled=true;
            life.World.LookAt(TownDistrictId.North);life.World.Camera.Jump(new TownPoint(0,2.9f));life.World.Refresh();Canvas.ForceUpdateCanvases();
            phase=addition.Phase;addition.AdvanceWater(.1f);Assert.That(addition.Phase,Is.EqualTo(phase));Assert.That(addition.canvasRenderer.GetMesh().vertexCount,Is.Zero,"Offscreen water must not keep emitting geometry");
            life.World.ReturnToHero();Canvas.ForceUpdateCanvases();Assert.That(addition.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(784));
            float sessionElapsed=addition.ShowElapsed;Flow.ShowHome();addition=Flow.GetComponentInChildren<TownFountainWaterGraphic>();
            Assert.That(addition.ShowElapsed,Is.EqualTo(sessionElapsed),"Home rebuilds must not restart the three-minute show countdown");
            for(int tick=0;tick<1760;tick++)addition.AdvanceWater(.1f);Canvas.ForceUpdateCanvases();
            Assert.That(addition.ShowStage,Is.EqualTo(2),"Normal active-visible time must reach the short fountain show");
            Flow.SendMessage("OnApplicationPause",true);var pausedWater=Flow.GetComponentInChildren<TownFountainWaterGraphic>();
            phase=pausedWater.Phase;pausedWater.AdvanceWater(.1f);Assert.That(pausedWater.Phase,Is.EqualTo(phase),"The rebuilt paused Home must not animate the new water");
            Flow.SendMessage("OnApplicationPause",false);
            Assert.That(Flow.GetComponentsInChildren<Text>().Count(t=>t.name=="Town bubble"),Is.LessThanOrEqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
