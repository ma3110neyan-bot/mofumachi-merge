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
        [UnityTest] public IEnumerator HomeUsesOneContinuousFieldAndTheMenuHasFiveSecondaryFastTravelButtons()
        {
            yield return EnterHome();var world=Flow.GetComponentInChildren<TownContinuousWorldView>();Assert.That(world,Is.Not.Null);Assert.That(world.Model.Residents.Count,Is.EqualTo(6));
            Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Street exit ")),Is.Zero);
            Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Region walk ")),Is.False);
            TownMenuButton("Region walk East");var fast=Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Region walk ")).ToArray();Assert.That(fast.Length,Is.EqualTo(5));
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Fast travel help").text,Does.Contain("ファストトラベル"));
            Assert.That(fast.Select(b=>b.name),Is.EquivalentTo(TownDistrictLayout.All.Select(d=>"Region walk "+d)));
            var surface=Resources.Load<Texture2D>(TownWorldSurface.AtlasResource);Assert.That(surface,Is.Not.Null);
            Assert.That(Flow.GetComponentsInChildren<TownGroundGraphic>().Single().mainTexture,Is.SameAs(surface));
            Assert.That(Flow.GetComponentsInChildren<RawImage>().Any(i=>i.name=="Seamless town map"),Is.False);
            Assert.That(surface.width,Is.LessThanOrEqualTo(2048));Assert.That(surface.height,Is.LessThanOrEqualTo(2048));Assert.That(surface.isReadable,Is.False);
            fast.Single(b=>b.name=="Region walk East").onClick.Invoke();Assert.That(Flow.IsModalOpen,Is.False);Assert.That(world.Model.Hero.Region,Is.EqualTo(TownRegionId.East));Assert.That(Flow.Daily.Data.Walks,Is.Zero,"Fast travel is not a completed daily walk");LogAssert.NoUnexpectedReceived();
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
            yield return EnterHome();var life=FrameTownPortrait();Assert.That(life.GuideToPlace("west.flowers"),Is.True);
            for(int i=0;i<90;i++)life.AdvanceFrame(1f/30);
            var camera=life.World.Camera.Center;Assert.That(camera.X,Is.LessThan(TownWorldMap.Place("square").X),"Exercise a westward-moving continuous camera");
            Assert.That(life.World.Model.Hero.IsWalking,Is.True);var before=life.World.Model.Residents.ToDictionary(a=>a.Id,a=>a.Position);bool follow=life.World.FollowingHero;
            Flow.ShowSettings();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Settings));Assert.That(Flow.HomeWorldSnapshot,Is.Not.Null);Assert.That(Flow.HomeRegionSnapshot,Is.Null,"Continuous cameras must not be encoded as a bounded region snapshot");
            Flow.GoBack();var after=Flow.GetComponentInChildren<TownContinuousWorldView>();
            foreach(var actor in after.Model.Residents)Assert.That(TownWorldLayout.Distance(before[actor.Id],actor.Position),Is.LessThan(.0001f));
            Assert.That(TownWorldLayout.Distance(camera,after.Camera.Center),Is.LessThan(.0001f));Assert.That(after.FollowingHero,Is.EqualTo(follow));Assert.That(after.Model.Hero.IsWalking,Is.False);
            Assert.That(Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled).Movement.OwnedPointerId,Is.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TodayClaimAndConversationNeverWriteOrRewardTheMainSave()
        {
            yield return EnterHome();Flow.Game.State.activeQuestId="";Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));Flow.Game.State.townGrowthLevel=6;Flow.ShowHome();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Home quest").onClick.Invoke();Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Claim today flower").interactable,Is.False);Flow.CloseToday();
            for(int i=0;i<3;i++)Flow.Daily.RecordMerge();Flow.TalkToCharacter("lulu");Flow.Daily.RecordWalk();Flow.ShowToday();
            Canvas.ForceUpdateCanvases();var greeting=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Today greeting");Assert.That(greeting.preferredHeight,Is.LessThanOrEqualTo(greeting.rectTransform.rect.height+1),"All three greeting lines must fit the friend column");
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
        [UnityTest] public IEnumerator TodayVisitAndBookVisitStartARealWalkWithoutCreatingAConversation()
        {
            yield return EnterHome();Flow.ShowToday();string featured=Flow.Daily.CharacterOfDay;
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Today friend destination").text,Is.EqualTo(Flow.TownCharacterDestination(featured)));
            var visit=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Visit today friend");Assert.That(((RectTransform)visit.transform).rect.height,Is.GreaterThanOrEqualTo(48));visit.onClick.Invoke();
            Assert.That(Flow.IsModalOpen,Is.False);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Daily.Data.Talks,Is.Zero);
            Assert.That(Flow.Daily.Data.Characters.Sum(c=>c.Meetings),Is.Zero);
            Flow.OpenCharacterBook("noah");string favorite=Flow.Game.State.favoriteCharacterId;
            var bookVisit=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Talk to character");bookVisit.onClick.Invoke();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo(favorite));
            Assert.That(Flow.Daily.Data.Talks,Is.Zero);Assert.That(Flow.Daily.Data.Characters.Sum(c=>c.Meetings),Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EveryTodayRowIsActionableAndItsPaperScrollsOnAShortScreen()
        {
            yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(320,592);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(320,592),new Rect(0,24,320,536));Canvas.ForceUpdateCanvases();Flow.ShowToday();Canvas.ForceUpdateCanvases();
            var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
            foreach(var button in Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Today goal ")))
                Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(48));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Today goal 0").onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Merge));Assert.That(Flow.Daily.Data.Merges,Is.Zero);
            Flow.ShowHome();Flow.ShowToday();Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Today goal 2").onClick.Invoke();
            Assert.That(Flow.IsModalOpen,Is.False);Assert.That(Flow.GetComponentInChildren<TownLifeView>().World.Model.Hero.IsWalking,Is.True);Assert.That(Flow.Daily.Data.Walks,Is.Zero,"Only completed cross-district walking counts");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ANewWalkDismissesTheOldFacilityEntryAndItsCallback()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.GuideToFacility("quest");for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            var oldEntry=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility");Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(life.GuideToFacility("shop"),Is.True);
            Assert.That(life.NearbyFacility,Is.Null);oldEntry.onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(life.Regions.HasHeroJourney,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AWorldDragKeepsOneRealPointerOwnerUntilRelease()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);var camera=life.GetComponent<TownViewportView>();
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=81});var target=life.transform.TransformPoint(life.World.Local(TownWorldMap.Place("quest")));
            hero.OnDrag(new PointerEventData(EventSystem.current){pointerId=81,position=RectTransformUtility.WorldToScreenPoint(null,target)});
            Assert.That(life.IsManuallyDragging,Is.True);var destination=life.World.Model.Hero.Target;var point=hero.WorldPosition;
            camera.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=82});hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=82});life.World.FastTravel(TownRegionId.East);
            Assert.That(camera.OwnedPointerId,Is.Null);Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(81));Assert.That(life.World.Model.Hero.Target,Is.EqualTo(destination));Assert.That(hero.WorldPosition,Is.EqualTo(point));
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=81});Assert.That(life.IsManuallyDragging,Is.False);life.CancelInteraction();LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RejectedNpcTapsDoNotCountAConversationThatWasNeverShown()
        {
            yield return EnterHome();var life=FrameTownPortrait();Assert.That(life.World.GuideDistrict(TownDistrictId.West),Is.True);
            for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);Assert.That(life.Regions.HasHeroJourney,Is.False);
            var hero=life.Actors.Single(a=>a.Controlled);var npc=life.Actors.First(a=>!a.Controlled&&a.RegionVisible&&TownDistrictLayout.At(a.WorldPosition)==TownDistrictId.West);
            Assert.That(hero.RegionVisible,Is.True);hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=91});Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(91));
            npc.React();Assert.That(Flow.Daily.Data.Talks,Is.Zero);Assert.That(Flow.Daily.Data.Characters.Sum(c=>c.Meetings),Is.Zero);
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=91});Assert.That(life.GuideToFacility("quest"),Is.True);for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            // Frame the same naturally moving NPC without clearing the pending
            // entrance. An offscreen tap would not exercise the entrance guard.
            life.World.Camera.Jump(npc.WorldPosition);life.World.Refresh();Assert.That(npc.RegionVisible,Is.True);npc.React();
            Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Flow.Daily.Data.Talks,Is.Zero);Assert.That(Flow.Daily.Data.Characters.Sum(c=>c.Meetings),Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FlatCellsHaveStableIndexesAndRealNonOverlappingHitRects()
        {
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();var grid=Flow.GetComponentInChildren<MergeGridLayout>();grid.ResizeNow();
            var cells=grid.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));var back=(RectTransform)grid.transform.Find("Cell 0");var front=(RectTransform)grid.transform.Find("Cell 25");
            Assert.That(back.rect.width,Is.EqualTo(front.rect.width).Within(.001f));foreach(var cell in cells){var r=((RectTransform)cell.transform).rect;Assert.That(r.width,Is.GreaterThanOrEqualTo(48));Assert.That(r.height,Is.EqualTo(r.width).Within(.001f));}var area=((RectTransform)grid.transform).rect;for(int i=0;i<30;i++){var actual=(RectTransform)grid.transform.Find("Cell "+i);var projected=MergePerspective.Cell(i,area.width,area.height);Assert.That(actual.anchoredPosition.x,Is.EqualTo(projected.X).Within(.001f));Assert.That(actual.anchoredPosition.y,Is.EqualTo(projected.Y).Within(.001f));Assert.That(actual.rect.width,Is.EqualTo(projected.Width).Within(.001f));}
            Assert.That(Flow.GetComponentInChildren<PerspectiveBoardGraphic>(),Is.Not.Null);LogAssert.NoUnexpectedReceived();
        }
    }
}
