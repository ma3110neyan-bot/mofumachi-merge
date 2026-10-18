using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class TownRegionFlowTests : SliceUiTestFixture
    {
        private Component Session()
        {
            var type=typeof(TownLifeView).Assembly.GetType("Mofumachi.Presentation.TownRegionSession");
            Assert.That(type,Is.Not.Null,"Home has no region session");var session=Flow.GetComponentInChildren(type);
            Assert.That(session,Is.Not.Null);return session;
        }
        private static T Read<T>(object owner,string property)=>(T)owner.GetType().GetProperty(property).GetValue(owner);
        private static bool Guide(Component session,TownRegionId region)=>(bool)session.GetType().GetMethod("GuideToRegion").Invoke(session,new object[]{region});
        private static TownRegionId Region(TownActor actor)=>Read<TownRegionId>(actor,"RegionId");
        private static void StepUntil(TownLifeView life,Func<bool> ready,int frames=1800)
        {for(int i=0;i<frames&&!ready();i++)life.AdvanceFrame(1f/30);Assert.That(ready(),Is.True,"Regional walk did not finish within its bounded frame budget");}

        [UnityTest] public IEnumerator SixOfficialCharactersHaveOneControllerAndThreeRegions()
        {
            yield return EnterHome();var session=Session();var actors=Flow.GetComponentsInChildren<TownActor>();
            Assert.That(actors.Length,Is.EqualTo(6));Assert.That(actors.Count(a=>a.Controlled),Is.EqualTo(1));
            Assert.That(actors.Select(a=>a.CharacterId),Is.EquivalentTo(CharacterCatalog.All.Select(c=>c.Id)));
            Assert.That(actors.Count(a=>Region(a)==TownRegionId.West),Is.EqualTo(2));
            Assert.That(actors.Count(a=>Region(a)==TownRegionId.Central),Is.EqualTo(2));
            Assert.That(actors.Count(a=>Region(a)==TownRegionId.East),Is.EqualTo(2));
            Assert.That(actors.Where(a=>!a.Controlled).All(a=>a.Movement==null),Is.True);
            Assert.That(Read<TownRegionId>(session,"HeroRegion"),Is.EqualTo(TownRegionId.Central));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HeroWalksContinuouslyAcrossTheNorthConnection()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var world=life.World;int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);
            Assert.That(life.Regions.GuideToRegion(TownRegionId.West),Is.True);var previous=world.Model.Hero.Position;bool crossed=false;
            for(int i=0;i<1800&&world.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);var current=world.Model.Hero.Position;
                Assert.That(TownWorldLayout.Distance(previous,current),Is.LessThanOrEqualTo(.00401f));previous=current;crossed|=world.Model.Hero.Region==TownRegionId.West;
                Assert.That(life.Regions.IsHeroTransitioning,Is.False);Assert.That(life.GetComponent<TownRegionView>().TransitionOpacity,Is.Zero);}
            Assert.That(crossed,Is.True);Assert.That(world.Model.Hero.IsWalking,Is.False);Assert.That(world.Model.Hero.PlaceId,Is.EqualTo("west.flowers"));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WestToEastKeepsCentralHopOrder()
        {
            yield return EnterHome();var session=Session();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            Assert.That(Guide(session,TownRegionId.West),Is.True);StepUntil(life,()=>!Read<bool>(session,"HasHeroJourney"));
            Assert.That(Guide(session,TownRegionId.East),Is.True);var seen=new System.Collections.Generic.List<TownRegionId>{Region(hero)};
            for(int i=0;i<1800&&Read<bool>(session,"HasHeroJourney");i++)
            {life.AdvanceFrame(1f/30);if(seen.Last()!=Region(hero))seen.Add(Region(hero));}
            Assert.That(seen,Is.EqualTo(new[]{TownRegionId.West,TownRegionId.Central,TownRegionId.East}));
            Assert.That(Read<bool>(session,"HasHeroJourney"),Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SideToQuestReturnsToOriginalEntranceAndRequiresEnter()
        {
            yield return EnterHome();var session=Session();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            Assert.That(Guide(session,TownRegionId.West),Is.True);StepUntil(life,()=>!Read<bool>(session,"HasHeroJourney"));
            int writes=Store.Writes;Assert.That(life.GuideToFacility("quest"),Is.True);StepUntil(life,()=>!Read<bool>(session,"HasHeroJourney"));
            Assert.That(Region(hero),Is.EqualTo(TownRegionId.West));Assert.That(life.NearbyFacility,Is.EqualTo("quest"));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Store.Writes,Is.EqualTo(writes));
            Assert.That(TownWorldLayout.Distance(hero.WorldPosition,TownWorldMap.Place("quest")),Is.LessThan(.001f));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility").onClick.Invoke();
            for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ViewingOnlyDoesNotMoveHeroOrWriteSave()
        {
            yield return EnterHome();var session=Session();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);var point=hero.Position;
            Assert.That((bool)session.GetType().GetMethod("LookAtRegion").Invoke(session,new object[]{TownRegionId.East}),Is.True);
            Assert.That(Read<TownRegionId>(session,"ViewingRegion"),Is.EqualTo(TownRegionId.East));Assert.That(Region(hero),Is.EqualTo(TownRegionId.Central));
            Assert.That(hero.Position.Equals(point),Is.True);Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
            session.GetType().GetMethod("ReturnToHero").Invoke(session,null);Assert.That(Read<TownRegionId>(session,"ViewingRegion"),Is.EqualTo(Region(hero)));LogAssert.NoUnexpectedReceived();
        }
        private Component RegionView()
        {
            var type=typeof(TownLifeView).Assembly.GetType("Mofumachi.Presentation.TownRegionView");Assert.That(type,Is.Not.Null,"Regional painting view is missing");
            var view=Flow.GetComponentInChildren(type);Assert.That(view,Is.Not.Null);return view;
        }
        [UnityTest] public IEnumerator ContinuousTravelCullsDistantBackgroundsAndNeverInterceptsHud()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.GuideToRegion(TownRegionId.West);
            for(int i=0;i<1400&&life.Regions.HasHeroJourney;i++){life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();Assert.That(life.World.VisibleBackgroundCount,Is.InRange(1,2));}
            Assert.That(life.Regions.HasHeroJourney,Is.False);foreach(var image in Flow.GetComponentsInChildren<RawImage>().Where(g=>g.name.StartsWith("Continuous background")))Assert.That(image.raycastTarget,Is.False);
            Assert.That(Flow.GetComponentsInChildren<TownActor>().Length,Is.EqualTo(6));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HeroRegionAndViewingRegionRemainSeparate()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.World.Model.Hero;var before=hero.Position;
            Assert.That(life.Regions.LookAtRegion(TownRegionId.East),Is.True);Assert.That(hero.Region,Is.EqualTo(TownRegionId.Central));Assert.That(life.Regions.ViewingRegion,Is.EqualTo(TownRegionId.East));
            Assert.That(hero.Position.Equals(before),Is.True);Assert.That(life.World.FollowingHero,Is.False);
            life.Regions.ReturnToHero();Assert.That(life.World.FollowingHero,Is.True);Assert.That(life.Regions.ViewingRegion,Is.EqualTo(TownRegionId.Central));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WaterAndGrowthShareTheContinuousCameraProjection()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var water=Flow.GetComponentInChildren<TownAmbientGraphic>();
            Assert.That(life.Regions.LookAtRegion(TownRegionId.West),Is.True);Canvas.ForceUpdateCanvases();
            Assert.That(water.World,Is.SameAs(life.World));Assert.That(water.raycastTarget,Is.False);
            var north=life.World.Local(TownWorldLayout.ToWorld(TownRegionId.West,new TownPoint(.5f,.42f)));var center=life.World.Local(TownWorldLayout.ToWorld(TownRegionId.Central,new TownPoint(.5f,.42f)));
            Assert.That(center.y,Is.LessThan(north.y));Assert.That(Flow.GetComponentInChildren<TownGrowthView>().Region,Is.EqualTo(TownRegionId.West));
            life.Regions.ReturnToHero();Assert.That(life.Regions.ViewingRegion,Is.EqualTo(TownRegionId.Central));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LeavingHomeRestoresCentralTextureAndCover()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.LookAtRegion(TownRegionId.West);
            Assert.That(Flow.TownBackground.enabled,Is.False);int writes=Store.Writes;Flow.ShowMerge();
            Assert.That(Store.Writes,Is.EqualTo(writes+1));Assert.That(Flow.TownBackground.enabled,Is.True);Assert.That(Flow.TownBackground.GetComponent<BackgroundCover>().enabled,Is.True);
            Assert.That(Flow.TownBackground.texture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/TownBackground")));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseSettingsBackAndResizeReleaseAllRegionalInput()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.GuideToRegion(TownRegionId.West);for(int i=0;i<100;i++)life.AdvanceFrame(1f/30);
            var position=life.World.Model.Hero.Position;Flow.ShowSettings();Flow.GoBack();life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(life.Regions.HasHeroJourney,Is.False);Assert.That(TownWorldLayout.Distance(position,life.World.Model.Hero.Position),Is.LessThan(.0001f));
            life.Regions.GuideToRegion(TownRegionId.East);Flow.SendMessage("OnApplicationPause",true);Assert.That(life.Regions.HasHeroJourney,Is.False);Flow.SendMessage("OnApplicationPause",false);
            life=Flow.GetComponentInChildren<TownLifeView>();Assert.That(life.Actors.Single(a=>a.Controlled).Movement.OwnedPointerId,Is.Null);Assert.That(life.GetComponent<TownRegionView>().TransitionOpacity,Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RebuildPreservesWorldPositionAndCameraWithoutSavingARegionField()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.GuideToRegion(TownRegionId.West);StepUntil(life,()=>!life.Regions.HasHeroJourney);
            var point=life.World.Model.Hero.Position;life.World.Pan(new Vector2(0,80));var center=life.World.Camera.Center;int writes=Store.Writes;
            Flow.ShowSettings();Flow.GoBack();life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(TownWorldLayout.Distance(point,life.World.Model.Hero.Position),Is.LessThan(.0001f));Assert.That(TownWorldLayout.Distance(center,life.World.Camera.Center),Is.LessThan(.0001f));
            Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(life.World.FollowingHero,Is.False);Assert.That(Store.Writes,Is.EqualTo(writes+2));
            Flow.Initialize(Store);Flow.ShowHome();Assert.That(Flow.GetComponentInChildren<TownLifeView>().Regions.HeroRegion,Is.EqualTo(TownRegionId.Central));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator InterruptedContinuousWalkLeavesNoInputOrCallbacks()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var session=life.Regions;int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);
            for(int i=0;i<3;i++){Assert.That(session.GuideToRegion(TownRegionId.West),Is.True);StepUntil(life,()=>!session.HasHeroJourney);Assert.That(session.GuideToRegion(TownRegionId.Central),Is.True);StepUntil(life,()=>!session.HasHeroJourney);}
            session.GuideToRegion(TownRegionId.East);for(int i=0;i<100;i++)life.AdvanceFrame(1f/30);var point=life.World.Model.Hero.Position;life.CancelInteraction();
            Assert.That(session.HasHeroJourney,Is.False);for(int i=0;i<30;i++)life.AdvanceFrame(1f/30);
            Assert.That(TownWorldLayout.Distance(point,life.World.Model.Hero.Position),Is.LessThan(.0001f));Assert.That(life.Actors.Single(a=>a.Controlled).Movement.OwnedPointerId,Is.Null);
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RegionVisitThenBreadDeliveryAndReloadPreservesProgress()
        {
            yield return EnterHome();var state=GameState.CreateInitial();state.purchaseNoticeAcknowledged=true;state.favoriteCharacterId="momo";
            state.completedQuestIds.Add("tea-01");state.claimedRewardIds.Add("quest:tea-01");state.coins=30;state.townGrowthLevel=1;state.activeQuestId="bread-01";state.questState=QuestState.Producing;
            Store.Save(state);Flow.Initialize(Store);Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(life.Regions.GuideToRegion(TownRegionId.West),Is.True);StepUntil(life,()=>!life.Regions.HasHeroJourney);
            string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;Store.Fail=true;Flow.ShowMerge();Store.Fail=false;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.GetComponentInChildren<TownRegionSession>().HeroRegion,Is.EqualTo(TownRegionId.West));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Flow.ShowMerge();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Merge));
            Flow.OpenMergeActions();Flow.BeginFrameCleanup();foreach(string id in Flow.Game.State.productionFrameIds.ToArray())Flow.ToggleFrameSelection(id);
            Flow.OpenPackFramesConfirmation();Assert.That(Flow.ConfirmPackFrames(),Is.True);Assert.That(Flow.Game.State.productionFrameIds,Is.Empty);
            Flow.OpenMergeActions();Flow.ChooseProductionItem("bread");Assert.That(Flow.Game.State.productionFrameIds,Is.EqualTo(new[]{"bread"}));
            Flow.GenerateFromFrame("bread");Flow.GenerateFromFrame("bread");
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Cell 2").onClick.Invoke();Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Cell 3").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.4f);Assert.That(Flow.Game.Board.At(3).itemId,Is.EqualTo("bread"));Assert.That(Flow.Game.Board.At(3).level,Is.EqualTo(2));
            Assert.That(Flow.Game.Quests.CheckDelivery(),Is.True);Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver").onClick.Invoke();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Result));Assert.That(Flow.Game.State.coins,Is.EqualTo(70));Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(2));Assert.That(Flow.Game.Board.At(3),Is.Null);
            Assert.That(Flow.Deliver().Success,Is.False);yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();Flow.ContinueAfterGrowth();Flow.GoBack();
            var saved=Flow.Game.State.Clone();Flow.Initialize(Store);Flow.ShowHome();
            Assert.That(Flow.Game.State.coins,Is.EqualTo(saved.coins));Assert.That(Flow.Game.State.claimedRewardIds,Is.EqualTo(saved.claimedRewardIds));Assert.That(Flow.Game.State.productionFrameIds,Is.EqualTo(new[]{"bread"}));
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));Assert.That(QuestCatalog.NextFor(Flow.Game.State).Id,Is.EqualTo("flower-01"));Assert.That(Flow.GetComponentInChildren<TownRegionSession>().HeroRegion,Is.EqualTo(TownRegionId.Central));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator StaleFacilityButtonCannotEnterAfterRegionChanges()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();Assert.That(life.GuideToFacility("quest"),Is.True);StepUntil(life,()=>!life.Regions.HasHeroJourney);
            var button=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility");int writes=Store.Writes;
            Assert.That(life.Regions.LookAtRegion(TownRegionId.East),Is.True);button.onClick.Invoke();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.IsModalOpen,Is.False);Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RegionalControlsAndTextFitShortAndTallSafeAreas()
        {
            yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
            foreach(int height in new[]{592,640,808})
            {
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(360,height);Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));Canvas.ForceUpdateCanvases();
                var life=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var region in new[]{TownRegionId.West,TownRegionId.Central,TownRegionId.East})
                {
                    Assert.That(life.Regions.LookAtRegion(region),Is.True);Canvas.ForceUpdateCanvases();
                    foreach(var button in Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Region walk")))
                    {
                        Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(48));var label=button.GetComponentInChildren<Text>();
                        Assert.That(label.fontSize,Is.GreaterThanOrEqualTo(14));Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+1));
                    }
                    foreach(var sign in life.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Painted road direction")))
                    {var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,sign.transform);var area=((RectTransform)life.transform).rect;Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(area.xMin));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(area.xMax));Assert.That(sign.preferredHeight,Is.LessThanOrEqualTo(sign.rectTransform.rect.height+1));Assert.That(sign.raycastTarget,Is.False);}
                    foreach(var name in new[]{"Town region name","Town growth hint"})
                    {var text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name==name);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),name);}
                    Assert.That(Flow.TownBackground.uvRect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(Flow.TownBackground.uvRect.xMax,Is.LessThanOrEqualTo(1));
                }
            }LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HitchKeepsContinuousMovementBoundedWithoutAFade()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.GuideToRegion(TownRegionId.West);
            var old=life.World.Model.Hero.Position;for(int i=0;i<40;i++){life.AdvanceFrame(.1f);Assert.That(TownWorldLayout.Distance(old,life.World.Model.Hero.Position),Is.LessThanOrEqualTo(.01201f));old=life.World.Model.Hero.Position;Assert.That(life.GetComponent<TownRegionView>().TransitionOpacity,Is.Zero);}
            Assert.That(life.Regions.HasHeroJourney,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PreviewDuringTravelDoesNotFadeUnrelatedRegion()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var session=life.Regions;
            session.GuideToRegion(TownRegionId.West);session.LookAtRegion(TownRegionId.East);var center=life.World.Camera.Center;
            StepUntil(life,()=>!session.HasHeroJourney);Assert.That(session.HeroRegion,Is.EqualTo(TownRegionId.West));Assert.That(session.ViewingRegion,Is.EqualTo(TownRegionId.East));
            Assert.That(life.World.Camera.Center.Equals(center),Is.True);Assert.That(life.GetComponent<TownRegionView>().TransitionOpacity,Is.Zero);session.ReturnToHero();Assert.That(session.ViewingRegion,Is.EqualTo(TownRegionId.West));LogAssert.NoUnexpectedReceived();
        }
    }
}
