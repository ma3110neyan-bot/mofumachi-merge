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
        [UnityTest] public IEnumerator OneFastTravelTapRespondsAndRepeatedTapsDoNotQueueAJourney()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();int writes=Store.Writes;
            var button=Button("Region walk West");button.GetComponent<ButtonPressFeedback>().OnPointerDown(new PointerEventData(EventSystem.current));
            Assert.That(button.GetComponentInChildren<Text>().transform.localScale.x,Is.LessThan(1));Assert.That(button.transform.localScale,Is.EqualTo(Vector3.one));
            life.GuideToPlace("east.flowers");button.onClick.Invoke();Assert.That(life.Regions.HeroRegion,Is.EqualTo(TownRegionId.West));Assert.That(life.Regions.HasHeroJourney,Is.False);
            var point=life.World.Model.Hero.Position;for(int i=0;i<10;i++)button.onClick.Invoke();Assert.That(life.World.Model.Hero.Position.Equals(point),Is.True);
            Assert.That(Flow.Daily.Data.Walks,Is.Zero);Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
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
        [UnityTest] public IEnumerator FastTravelVisitsEachRegionWithoutCountingAsWalking()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();int writes=Store.Writes;
            foreach(var region in new[]{TownRegionId.Central,TownRegionId.West,TownRegionId.West,TownRegionId.Central,TownRegionId.East})
            {Button("Region walk "+region).onClick.Invoke();Assert.That(life.Regions.HeroRegion,Is.EqualTo(region));Assert.That(life.Regions.HasHeroJourney,Is.False);Assert.That(life.World.FollowingHero,Is.True);}
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Flow.Daily.Data.Walks,Is.Zero);LogAssert.NoUnexpectedReceived();
        }
    }
}
