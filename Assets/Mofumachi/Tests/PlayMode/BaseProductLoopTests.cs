using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class BaseProductLoopTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator VisitFromBookWalksToTheRealFriendWithoutInventingConversation()
        {
            yield return EnterHome();var life=FrameTownPortrait();
            var target=life.World.Model.Residents.First(r=>!r.Controlled);
            int talks=Flow.Daily.Data.Characters.Single(c=>c.Id==target.Id).Meetings;
            long coins=Flow.Game.State.coins;
            Flow.OpenCharacterBook(target.Id);yield return null;
            Assert.That(Flow.VisitTownCharacter(target.Id),Is.True);
            life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            for(int i=0;i<900&&life.World.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);life.GetComponent<TownMeetingJourney>().Advance(1f/30);}
            var friend=life.World.Model.Residents.Single(r=>r.Id==target.Id);
            Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,friend.Position),Is.LessThan(.16f));
            Assert.That(Flow.Daily.Data.Characters.Single(c=>c.Id==target.Id).Meetings,Is.EqualTo(talks));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));
        }
        [UnityTest] public IEnumerator FacilityInteriorsCannotBeOpenedFromTheOtherSideOfTown()
        {
            yield return EnterHome();FrameTownPortrait();
            Flow.OpenCafe();Assert.That(Flow.IsModalOpen,Is.False);
            Flow.OpenShopPreview();Assert.That(Flow.IsModalOpen,Is.False);
            Assert.That(Flow.VisitTownCharacter("unknown"),Is.False);
            Assert.That(Flow.Daily.Data.Talks,Is.Zero);
        }
        [UnityTest] public IEnumerator ANewFacilityDestinationCancelsTheOldMeetingAndKeepsArrivalAtTheDoor()
        {
            yield return EnterHome();var life=FrameTownPortrait();
            Assert.That(Flow.VisitTownCharacter("lulu"),Is.True);
            var visit=life.GetComponent<TownMeetingJourney>();Assert.That(visit.IsActive,Is.True);
            Assert.That(life.GetComponent<TownFacilityView>().RequestEnter("quest"),Is.True);
            Assert.That(visit.IsActive,Is.False);
            for(int i=0;i<3000&&life.World.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);visit.Advance(1f/30);}
            life.AdvanceFrame(1f/30);visit.Advance(1f/30);
            Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,TownWorldMap.Place("quest")),Is.LessThan(.033f));
            Assert.That(life.GetComponent<TownFacilityView>().Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
        }
        [UnityTest] public IEnumerator PointDistrictAndCameraCommandsNeverResumeAnOldVisit()
        {
            yield return EnterHome();var life=FrameTownPortrait();
            Assert.That(Flow.VisitTownCharacter("lulu"),Is.True);Assert.That(life.World.GuidePoint(TownWorldMap.Place("square")),Is.True);
            Assert.That(life.GetComponent<TownMeetingJourney>().IsActive,Is.False);
            Assert.That(Flow.VisitTownCharacter("lulu"),Is.True);Assert.That(life.World.GuideDistrict(TownDistrictId.South),Is.True);
            Assert.That(life.GetComponent<TownMeetingJourney>().IsActive,Is.False);
            Assert.That(Flow.VisitTownCharacter("lulu"),Is.True);life.World.Pan(Vector2.right*10);
            Assert.That(life.GetComponent<TownMeetingJourney>().IsActive,Is.False);
        }
        [UnityTest] public IEnumerator ReturningFromSettingsDoesNotTurnAnNpcsDestinationIntoItsCurrentLocation()
        {
            yield return EnterHome();var life=FrameTownPortrait();
            life.World.Model.Restore("lulu",new TownPoint(-1.15f,-.30f),"west.bench-front");
            Assert.That(Flow.TownCharacterDestination("lulu"),Is.EqualTo(TownCharacterBook.PlaceLabel(null,TownDistrictId.West)));
            Flow.ShowSettings();Flow.GoBack();
            Assert.That(Flow.TownCharacterDestination("lulu"),Is.EqualTo(TownCharacterBook.PlaceLabel(null,TownDistrictId.West)));
        }
    }
}
