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
        [UnityTest] public IEnumerator HeroWalksToGateThenStartsAtNeighbourEntry()
        {
            yield return EnterHome();var session=Session();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            Assert.That(Guide(session,TownRegionId.West),Is.True);Assert.That(Region(hero),Is.EqualTo(TownRegionId.Central));
            StepUntil(life,()=>Read<bool>(session,"IsHeroTransitioning"));
            Assert.That(Vector2.Distance(new Vector2(hero.Position.X,hero.Position.Y),new Vector2(.15f,.52f)),Is.LessThan(.001f));
            StepUntil(life,()=>Region(hero)==TownRegionId.West);
            Assert.That(hero.Position.X,Is.EqualTo(.94f).Within(.001f));Assert.That(hero.Position.Y,Is.EqualTo(.55f).Within(.001f));
            StepUntil(life,()=>!Read<bool>(session,"HasHeroJourney"));
            Assert.That(hero.Position.X,Is.Not.EqualTo(.94f),"Walk inward after the transition");
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Store.Writes,Is.EqualTo(writes));
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
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
            Assert.That(Region(hero),Is.EqualTo(TownRegionId.Central));Assert.That(life.NearbyFacility,Is.EqualTo("quest"));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Store.Writes,Is.EqualTo(writes));
            Assert.That(hero.Position.X,Is.EqualTo(.18f).Within(.001f));Assert.That(hero.Position.Y,Is.EqualTo(.635f).Within(.001f));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility").onClick.Invoke();
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
        [UnityTest] public IEnumerator TransitionUsesAtMostTwoBackgroundsAndNeverInterceptsHud()
        {
            yield return EnterHome();var session=Session();var view=RegionView();var life=Flow.GetComponentInChildren<TownLifeView>();
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Region view West").onClick.Invoke();
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Walk to viewed region").onClick.Invoke();
            StepUntil(life,()=>Read<bool>(session,"IsHeroTransitioning"));
            for(int i=0;i<14;i++)
            {
                life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();Assert.That(Read<int>(view,"VisibleBackgroundCount"),Is.InRange(1,2));
                Assert.That(Read<Graphic>(view,"TransitionGraphic").raycastTarget,Is.False);
            }
            Assert.That(Read<int>(view,"VisibleBackgroundCount"),Is.EqualTo(1));
            Assert.That(Flow.GetComponentsInChildren<TownActor>().Length,Is.EqualTo(6));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HeroRegionAndViewingRegionRemainSeparate()
        {
            yield return EnterHome();var session=Session();var before=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled).Position;
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Region view East").onClick.Invoke();
            Assert.That(Read<TownRegionId>(session,"HeroRegion"),Is.EqualTo(TownRegionId.Central));
            Assert.That(Read<TownRegionId>(session,"ViewingRegion"),Is.EqualTo(TownRegionId.East));
            Assert.That(Flow.TownBackground.texture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/TownEastBackground")));
            Assert.That(Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled).Position.Equals(before),Is.True);
            foreach(var button in Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Region view")||b.name=="Walk to viewed region"||b.name=="Return to town hero"))
                Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(48),button.name);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CentralWaterAndGrowthDoNotAppearInSideTown()
        {
            yield return EnterHome();var session=Session();var water=Flow.GetComponentInChildren<TownAmbientGraphic>();var growth=Flow.GetComponentInChildren<TownGrowthView>();
            session.GetType().GetMethod("LookAtRegion").Invoke(session,new object[]{TownRegionId.West});Canvas.ForceUpdateCanvases();
            Assert.That(Read<TownRegionId>(water,"Region"),Is.EqualTo(TownRegionId.West));Assert.That(water.canvasRenderer.GetMesh().vertexCount,Is.Zero);
            Assert.That(Read<TownRegionId>(growth,"Region"),Is.EqualTo(TownRegionId.West));
            Assert.That(Flow.GetComponentsInChildren<TownDecorationGraphic>().Length,Is.Zero);
            session.GetType().GetMethod("ReturnToHero").Invoke(session,null);Canvas.ForceUpdateCanvases();
            Assert.That(Read<TownRegionId>(water,"Region"),Is.EqualTo(TownRegionId.Central));Assert.That(water.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(0));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LeavingHomeRestoresCentralTextureAndCover()
        {
            yield return EnterHome();var session=Session();session.GetType().GetMethod("LookAtRegion").Invoke(session,new object[]{TownRegionId.West});
            Assert.That(Flow.TownBackground.texture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/TownWestBackground")));
            int writes=Store.Writes;Flow.ShowMerge();Assert.That(Store.Writes,Is.EqualTo(writes+1));
            Assert.That(Flow.TownBackground.texture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/TownBackground")));
            Assert.That(Flow.TownBackground.GetComponent<BackgroundCover>().enabled,Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseSettingsBackAndResizeReleaseAllRegionalInput()
        {
            yield return EnterHome();var session=Session();var life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(Guide(session,TownRegionId.West),Is.True);StepUntil(life,()=>Read<bool>(session,"IsHeroTransitioning"));
            Flow.ShowSettings();Flow.GoBack();yield return null;
            session=Session();Assert.That(Read<bool>(session,"HasHeroJourney"),Is.False);Assert.That(Read<bool>(session,"IsHeroTransitioning"),Is.False);
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Assert.That(Guide(session,TownRegionId.East),Is.True);Flow.SendMessage("OnApplicationPause",true);Flow.SendMessage("OnApplicationPause",false);yield return null;
            session=Session();Assert.That(Read<bool>(session,"HasHeroJourney"),Is.False);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,592),new Rect(0,24,360,536));Canvas.ForceUpdateCanvases();
            Assert.That(Flow.GetComponentsInChildren<TownActor>().Where(a=>a.Controlled).All(a=>a.Movement.OwnedPointerId==null),Is.True);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RebuildUsesOnlySettledTransientSnapshot()
        {
            yield return EnterHome();var session=Session();var life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(Guide(session,TownRegionId.West),Is.True);StepUntil(life,()=>!Read<bool>(session,"HasHeroJourney"));
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);var point=hero.Position;int writes=Store.Writes;
            Flow.ShowSettings();Flow.GoBack();yield return null;session=Session();hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            Assert.That(Region(hero),Is.EqualTo(TownRegionId.West));Assert.That(hero.Position.X,Is.EqualTo(point.X).Within(.001f));
            Assert.That(Read<bool>(session,"HasHeroJourney"),Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Assert.That(Store.Writes,Is.EqualTo(writes+2),"Only the existing two navigation saves");
            Flow.Initialize(Store);Flow.ShowHome();session=Session();Assert.That(Read<TownRegionId>(session,"HeroRegion"),Is.EqualTo(TownRegionId.Central),"Reload starts at the center, without a region save field");LogAssert.NoUnexpectedReceived();
        }
    }
}
