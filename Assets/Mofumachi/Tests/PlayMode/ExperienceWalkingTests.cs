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
    public sealed class ExperienceWalkingTests : SliceUiTestFixture
    {
        private bool HasOutings()=>Flow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Town outing choices");
        private static void Walk(TownLifeView life)
        {for(int i=0;i<2400&&(life.Regions.HasHeroJourney||life.Actors.Single(a=>a.Controlled).IsGuided);i++)life.AdvanceFrame(1f/30);}
        [UnityTest] public IEnumerator FavoriteTapSelectsTheStreetWithoutCoveringItWithMenu()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);int writes=Store.Writes;
            var prompt=Flow.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Street walking hint").gameObject;Assert.That(prompt.activeSelf,Is.False);
            hero.OnPointerClick(new PointerEventData(EventSystem.current));Assert.That(prompt.activeSelf,Is.True);
            Assert.That(life.GetComponent<TownStreetWalkView>().IsArmed,Is.True);
            Assert.That(HasOutings(),Is.False);
            Assert.That(Flow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Town outing choices"),Is.False);
            Assert.That(Store.Writes,Is.EqualTo(writes));Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(life.GetComponent<TownStreetWalkView>().IsArmed,Is.False);
        }
        [UnityTest] public IEnumerator StreetExitWalksToTheNeighborAndReturnsWithoutProgressWrites()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var walking=life.GetComponent<TownStreetWalkView>();string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            Assert.That(walking.Arm(),Is.True);Assert.That(walking.Tap(new TownPoint(.13f,.72f)),Is.True);Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.Central));Walk(life);
            Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.West));Assert.That(walking.Arm(),Is.True);Assert.That(walking.Tap(new TownPoint(.94f,1.45f)),Is.True);Walk(life);
            Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.Central));Assert.That(life.Regions.HasHeroJourney,Is.False);Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(1));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BuildingTapKeepsSelectionAndStreetTapReachesConnectedRoad()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var walking=life.GetComponent<TownStreetWalkView>();var hero=life.Actors.Single(a=>a.Controlled);
            walking.Arm();Assert.That(walking.Tap(new TownPoint(.95f,.95f)),Is.False);Assert.That(walking.IsArmed,Is.True);Assert.That(hero.IsGuided,Is.False);
            Assert.That(walking.Tap(new TownPoint(.53f,.49f)),Is.True);Walk(life);Assert.That(hero.IsGuided,Is.False);
            Assert.That(life.World.Model.Area.Contains(new TownPoint(.53f,.49f)),Is.True);
            Assert.That(Vector2.Distance(new Vector2(hero.Position.X,hero.Position.Y),new Vector2(.53f,.49f)),Is.LessThan(.001f),"A valid floor tap keeps its position rather than snapping to a line");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PanningDoesNotBecomeATapAndPauseClearsSelection()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var walking=life.GetComponent<TownStreetWalkView>();var camera=life.GetComponent<TownViewportView>();walking.Arm();
            var e=new PointerEventData(EventSystem.current){pointerId=71,position=new Vector2(180,340)};
            camera.OnPointerDown(e);camera.OnBeginDrag(e);e.position+=Vector2.right*24;camera.OnDrag(e);camera.OnPointerUp(e);camera.OnPointerClick(e);
            Assert.That(life.Actors.Single(a=>a.Controlled).IsGuided,Is.False);Assert.That(walking.IsArmed,Is.True);
            Flow.SendMessage("OnApplicationPause",true);Assert.That(walking.IsArmed,Is.False);Assert.That(camera.OwnedPointerId,Is.Null);Flow.SendMessage("OnApplicationPause",false);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BrowsingHasNoDuplicateExitControlsAndSelectionReturnsToTheHero()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.LookAtRegion(TownRegionId.West);
            Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Street exit ")),Is.False);
            var p=life.World.Model.Hero.Position;life.GetComponent<TownStreetWalkView>().Arm();Assert.That(life.World.FollowingHero,Is.True);
            Assert.That(life.World.Model.Hero.Position.Equals(p),Is.True);Assert.That(life.Regions.ViewingRegion,Is.EqualTo(TownRegionId.Central));LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator DestinationsCancelBothDirectWalkingAndArmedSelection()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var walking=life.GetComponent<TownStreetWalkView>();var hero=life.Actors.Single(a=>a.Controlled);
            walking.Arm();Assert.That(walking.Tap(new TownPoint(.53f,.49f)),Is.True);Assert.That(hero.IsGuided,Is.True);life.OpenOutings();
            Assert.That(HasOutings(),Is.True);Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);var point=hero.Position;
            for(int i=0;i<12;i++)life.AdvanceFrame(1f/30);Assert.That(hero.Position,Is.EqualTo(point));
            Flow.GoBack();Assert.That(HasOutings(),Is.False);walking.Arm();life.OpenOutings();Assert.That(walking.IsArmed,Is.False);Assert.That(HasOutings(),Is.True);Flow.GoBack();
            Assert.That(HasOutings(),Is.False);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RejectedSecondFingerCannotTurnItsClickIntoAWalk()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var walking=life.GetComponent<TownStreetWalkView>();var camera=life.GetComponent<TownViewportView>();walking.Arm();
            var local=life.Local(new TownPoint(.53f,.49f));var screen=RectTransformUtility.WorldToScreenPoint(null,life.transform.TransformPoint(local));
            var first=new PointerEventData(EventSystem.current){pointerId=51,position=screen};var second=new PointerEventData(EventSystem.current){pointerId=52,position=screen};
            camera.OnPointerDown(first);camera.OnPointerDown(second);camera.OnPointerUp(second);camera.OnPointerClick(second);
            Assert.That(camera.OwnedPointerId,Is.EqualTo(51));Assert.That(life.Actors.Single(a=>a.Controlled).IsGuided,Is.False);Assert.That(walking.IsArmed,Is.True);
            camera.OnPointerUp(first);camera.OnPointerClick(first);Assert.That(life.Actors.Single(a=>a.Controlled).IsGuided,Is.True);Assert.That(camera.OwnedPointerId,Is.Null);LogAssert.NoUnexpectedReceived();
        }

    }
}
