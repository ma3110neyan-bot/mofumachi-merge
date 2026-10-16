using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Mofumachi.Tests
{
    public sealed class DailyWorldTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator HomeUsesOneContinuousFieldAndOnlyThreeFastTravelButtons()
        {
            yield return EnterHome();var world=Flow.GetComponentInChildren<TownContinuousWorldView>();Assert.That(world,Is.Not.Null);Assert.That(world.Model.Residents.Count,Is.EqualTo(6));
            Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Street exit ")),Is.Zero);
            var fast=Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Region walk ")).ToArray();Assert.That(fast.Length,Is.EqualTo(3));Assert.That(fast.All(b=>b.GetComponentInChildren<Text>().text.Contains("すぐ移動")),Is.True);
            fast.Single(b=>b.name=="Region walk East").onClick.Invoke();Assert.That(world.Model.Hero.Region,Is.EqualTo(TownRegionId.East));Assert.That(Flow.Daily.Data.Walks,Is.Zero,"Fast travel is not a completed daily walk");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActualWalkingHasNoFadeAndDailyCountsOnlyItsCompletedArrival()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            Assert.That(life.GuideToPlace("west.flowers"),Is.True);Assert.That(Flow.Daily.Data.Walks,Is.Zero);var previous=life.World.Model.Hero.Position;
            for(int i=0;i<3000&&life.World.Model.Hero.IsWalking;i++)
            {life.AdvanceFrame(1f/30);var p=life.World.Model.Hero.Position;Assert.That(TownWorldLayout.Distance(previous,p),Is.LessThanOrEqualTo(.0041f));Assert.That(life.Regions.IsHeroTransitioning,Is.False);previous=p;}
            Assert.That(life.World.Model.Hero.Region,Is.EqualTo(TownRegionId.West));Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(1));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SettingsReturnKeepsTheMidWalkWorldPositionWithoutARegionJump()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.GuideToPlace("west.flowers");for(int i=0;i<170;i++)life.AdvanceFrame(1f/30);var before=life.World.Model.Hero.Position;
            Flow.ShowSettings();Flow.GoBack();var after=Flow.GetComponentInChildren<TownContinuousWorldView>().Model.Hero.Position;Assert.That(TownWorldLayout.Distance(before,after),Is.LessThan(.0001f));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TodayClaimAndConversationNeverWriteOrRewardTheMainSave()
        {
            yield return EnterHome();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            for(int i=0;i<3;i++)Flow.Daily.RecordMerge();Flow.TalkToCharacter("lulu");Flow.Daily.RecordWalk();Flow.ShowToday();
            var claim=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Claim today flower");Assert.That(claim.interactable,Is.True);claim.onClick.Invoke();Assert.That(Flow.Daily.Data.Claimed,Is.True);Assert.That(Flow.Daily.Data.DecorationDays.Count,Is.EqualTo(1));
            Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Claim today flower").interactable,Is.False);Flow.CloseToday();Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BookOpenedFromTodayReturnsToTownWithoutChangingTheFavorite()
        {
            yield return EnterHome();string favorite=Flow.Game.State.favoriteCharacterId;Flow.ShowToday();Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Open character book").onClick.Invoke();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.CharacterSelection));Flow.PreviewFavorite("noah");Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo(favorite));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SixProfilesShowBookFieldsAndKeepFavoriteConfirmationSeparate()
        {
            yield return EnterHome();Flow.ShowCharacterSelection();string favorite=Flow.Game.State.favoriteCharacterId;
            foreach(var c in CharacterCatalog.All)
            {Flow.PreviewFavorite(c.Id);Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Book favorite place"&&t.text.Contains(TownCharacterBook.For(c.Id,Flow.Game.State,Flow.Daily.Data).FavoritePlace)),Is.True);Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Partner ")),Is.EqualTo(6));Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo(favorite));}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ANewWalkDismissesTheOldFacilityEntryAndItsCallback()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.GuideToFacility("quest");for(int i=0;i<600&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            var oldEntry=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility");Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(life.GuideToFacility("shop"),Is.True);
            Assert.That(life.NearbyFacility,Is.Null);oldEntry.onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(life.Regions.HasHeroJourney,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AWorldDragKeepsOneRealPointerOwnerUntilRelease()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);var camera=life.GetComponent<TownViewportView>();
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=81});var target=life.transform.TransformPoint(life.Local(TownLifeView.RequestEntrance));
            hero.OnDrag(new PointerEventData(EventSystem.current){pointerId=81,position=RectTransformUtility.WorldToScreenPoint(null,target)});
            Assert.That(life.IsManuallyDragging,Is.True);var destination=life.World.Model.Hero.Target;var point=hero.WorldPosition;
            camera.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=82});hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=82});life.World.FastTravel(TownRegionId.East);
            Assert.That(camera.OwnedPointerId,Is.Null);Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(81));Assert.That(life.World.Model.Hero.Target,Is.EqualTo(destination));Assert.That(hero.WorldPosition,Is.EqualTo(point));
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=81});Assert.That(life.IsManuallyDragging,Is.False);life.CancelInteraction();LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RejectedNpcTapsDoNotCountAConversationThatWasNeverShown()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);var npc=life.Actors.First(a=>!a.Controlled&&a.RegionVisible);
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=91});npc.React();Assert.That(Flow.Daily.Data.Talks,Is.Zero);Assert.That(Flow.Daily.Data.Characters.Sum(c=>c.Meetings),Is.Zero);
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=91});life.GuideToFacility("quest");for(int i=0;i<600&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);npc.React();
            Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Flow.Daily.Data.Talks,Is.Zero);Assert.That(Flow.Daily.Data.Characters.Sum(c=>c.Meetings),Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PerspectiveCellsHaveStableIndexesAndRealNonOverlappingHitRects()
        {
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();var grid=Flow.GetComponentInChildren<MergeGridLayout>();grid.ResizeNow();
            var cells=grid.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));var back=(RectTransform)grid.transform.Find("Cell 0");var front=(RectTransform)grid.transform.Find("Cell 25");
            Assert.That(back.rect.width,Is.LessThan(front.rect.width));foreach(var cell in cells){var r=((RectTransform)cell.transform).rect;Assert.That(r.width,Is.GreaterThanOrEqualTo(48));Assert.That(r.height,Is.GreaterThanOrEqualTo(48));}var area=((RectTransform)grid.transform).rect;for(int i=0;i<30;i++){var actual=(RectTransform)grid.transform.Find("Cell "+i);var projected=MergePerspective.Cell(i,area.width,area.height);Assert.That(actual.anchoredPosition.x,Is.EqualTo(projected.X).Within(.001f));Assert.That(actual.anchoredPosition.y,Is.EqualTo(projected.Y).Within(.001f));Assert.That(actual.rect.width,Is.EqualTo(projected.Width).Within(.001f));}
            Assert.That(Flow.GetComponentInChildren<PerspectiveBoardGraphic>(),Is.Not.Null);LogAssert.NoUnexpectedReceived();
        }
    }
}
