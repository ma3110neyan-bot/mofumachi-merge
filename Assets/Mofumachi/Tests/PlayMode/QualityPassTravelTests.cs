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
    public sealed class QualityPassTravelTests : SliceUiTestFixture
    {
        private Button Button(string name)=>Flow.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        private static void Walk(TownLifeView life)
        {for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);Assert.That(life.Regions.HasHeroJourney,Is.False);}
        [UnityTest] public IEnumerator CharacterOffersSixDestinationsAndBackClosesWithoutLeavingTown()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();int writes=Store.Writes;
            life.Actors.Single(a=>a.Controlled).React();Canvas.ForceUpdateCanvases();
            foreach(string name in new[]{"Visit west cafe","Visit central fountain","Visit east flowers","Visit bench","Guide to quest","Guide to shop"})
            {var b=Button(name);Assert.That(((RectTransform)b.transform).rect.height,Is.GreaterThanOrEqualTo(48));}
            Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            Assert.That(Flow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Town outing choices"),Is.False);
            Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator OneShortcutTapWalksAndRepeatedTapDoesNotRestartTheJourney()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);int writes=Store.Writes;
            var button=Button("Region walk West");button.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
            button.GetComponent<ButtonPressFeedback>().OnPointerDown(new PointerEventData(EventSystem.current));
            Assert.That(button.GetComponentInChildren<Text>().transform.localScale.x,Is.LessThan(1));
            Assert.That(button.transform.localScale,Is.EqualTo(Vector3.one),"Press feedback must not shrink the 48-unit pointer target");button.onClick.Invoke();
            Assert.That(life.Regions.HasHeroJourney,Is.True);Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.Central));
            for(int i=0;i<8;i++)life.AdvanceFrame(1f/30);var point=hero.Position;
            button.onClick.Invoke();Assert.That(hero.Position.Equals(point),Is.True);Assert.That(hero.IsGuided,Is.True);
            Walk(life);Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.West));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CharacterDestinationCrossesCentralAndReachesQuestEntrance()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();int writes=Store.Writes;
            life.Actors.Single(a=>a.Controlled).React();Button("Visit east flowers").onClick.Invoke();Walk(life);
            Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.East));
            life.Actors.Single(a=>a.Controlled).React();Button("Guide to quest").onClick.Invoke();Walk(life);
            Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.Central));Assert.That(life.NearbyFacility,Is.EqualTo("quest"));
            Assert.That(Store.Writes,Is.EqualTo(writes));Button("Enter facility").onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RetargetAndSettingsReleaseJourneyWithoutSaveMutation()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            Button("Region walk West").onClick.Invoke();for(int i=0;i<9;i++)life.AdvanceFrame(1f/30);
            Button("Region walk East").onClick.Invoke();Walk(life);Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.East));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Button("Region walk West").onClick.Invoke();Flow.ShowSettings();Flow.GoBack();life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(life.Regions.HasHeroJourney,Is.False);Assert.That(life.Actors.Single(a=>a.Controlled).Movement.OwnedPointerId,Is.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ShortcutsReachAdvertisedPlacesIncludingTheCurrentRegion()
        {
            yield return EnterHome();Flow.Game.State.townGrowthLevel=2;Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            var hero=life.Actors.Single(a=>a.Controlled);int writes=Store.Writes;
            foreach(var region in new[]{TownRegionId.Central,TownRegionId.West,TownRegionId.West,TownRegionId.Central,TownRegionId.East})
            {
                Button("Region walk "+region).onClick.Invoke();Walk(life);
                string id=TownOutingDestinations.ForRegion(region,2);Assert.That(TownRegionCatalog.TryFindPlace(id,2,out var target,out _),Is.True);
                Assert.That(hero.RegionId,Is.EqualTo(target.Region));Assert.That(hero.Position.X,Is.EqualTo(target.Local.X).Within(.001));Assert.That(hero.Position.Y,Is.EqualTo(target.Local.Y).Within(.001));
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
    }
}
