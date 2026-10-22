using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
namespace Mofumachi.Tests
{
    public sealed class EastMasterReviewTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator FloristDoorStaysReadableDuringTravelAndWaitsForTheSecondTap()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);
                // ShowHome preserves the previous visit's feet. Each level
                // must start in central, rather than at the previous shop doorway.
                life.World.FastTravel(TownDistrictId.Central);
                Assert.That(life.World.Model.Hero.District,Is.EqualTo(TownDistrictId.Central),"Each level starts a fresh shop journey from central");
                life.World.LookAt(TownDistrictId.East);Canvas.ForceUpdateCanvases();
                var facility=life.GetComponent<TownFacilityView>();
                var hit=life.transform.Find("Facility entrance shop");
                var sign=hit.Find("Facility sign shop");
                Assert.That(hit.gameObject.activeInHierarchy,Is.True,"The florist door remains a real destination even when its caption withdraws");
                Assert.That(sign.gameObject.activeSelf,Is.False,"A distant shop facade stays visible");
                void TapDoor()=>ExecuteEvents.Execute(hit.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                TapDoor();Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Walking));
                for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)
                {life.AdvanceFrame(1f/30);Assert.That(life.IsSpeaking,Is.False,"Unsolicited dialogue must not hide the journey or its arrival");}
                Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                for(int i=0;i<360;i++){life.AdvanceFrame(1f/30);Assert.That(life.IsSpeaking,Is.False);}
                Assert.That(sign.gameObject.activeInHierarchy,Is.True);Assert.That(Flow.IsModalOpen,Is.False);
                Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
                TapDoor();Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
                for(int i=0;i<30&&!Flow.IsModalOpen;i++)life.AdvanceFrame(1f/30);
                Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Flow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Shop preview"),Is.True);
                Flow.GoBack();Assert.That(Flow.IsModalOpen,Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EastGroundFollowsTheWorldAndBothFloristSideWalksRemainUsable()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.FastTravel(TownDistrictId.East);Canvas.ForceUpdateCanvases();
            var ground=Flow.GetComponentInChildren<TownEastGroundGraphic>();Assert.That(ground,Is.Not.Null);Assert.That(ground.raycastTarget,Is.False);
            var texture=Resources.Load<Texture2D>(TownEastMasterLayout.Resource);Assert.That(texture,Is.Not.Null);Assert.That(ground.mainTexture,Is.SameAs(texture));
            Assert.That(texture,Is.SameAs(Resources.Load<Texture2D>(TownWestMasterLayout.Resource)),"The shared ground must not allocate a second texture");Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.width,Is.LessThanOrEqualTo(1024));Assert.That(texture.height,Is.LessThanOrEqualTo(2048));
            void AssertProjection()
            {
                Canvas.ForceUpdateCanvases();var mesh=ground.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(4));
                Assert.That(Vector2.Distance(mesh.vertices[0],life.World.Local(new TownPoint(TownEastMasterLayout.Left,TownEastMasterLayout.Bottom),ground.rectTransform)),Is.LessThan(.01f));
            }
            AssertProjection();life.World.Pan(new Vector2(36,-20));AssertProjection();
            foreach(var stop in new[]{new TownPoint(1.70f,-.22f),new TownPoint(1.70f,.50f),new TownPoint(.84f,.50f),new TownPoint(.80f,.03f),TownWorldMap.Place("shop"),TownWorldMap.Place("square")})
            {
                Assert.That(life.World.GuidePoint(stop),Is.True);
                for(int i=0;i<3000&&life.World.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);}
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,stop),Is.LessThan(.002f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        // Actual rendered bounds and depth order, not a text/layout mock.
        internal static void AssertNameClearOfBenches(TownLifeView life,TownActor actor)
        {
            var plate=actor.transform.Find("Town actor name panel");
            Assert.That(plate.gameObject.activeInHierarchy,Is.True,"The resting resident must keep a readable name");
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,plate);
            var benches=life.GetComponent<TownSceneryView>().Pieces
                .Where(p=>p.Rect.gameObject.activeInHierarchy&&p.Placement.Kind==TownSceneryKind.ParkBench)
                .Select(p=>p.Rect).Concat(life.GetComponentsInChildren<TownGrowthPropGraphic>()
                .Where(p=>p.Prop==TownGrowthProp.Bench).Select(p=>p.rectTransform));
            foreach(var bench in benches.Where(b=>b.GetSiblingIndex()>actor.transform.GetSiblingIndex()))
                Assert.That(bounds.Intersects(RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,bench)),Is.False,"A foreground bench must not draw over "+actor.CharacterId+"'s name");
        }
        [UnityTest] public IEnumerator FlowerCartReplacesWithEarnedDisplayWithoutChangingProgress()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.LookAt(TownDistrictId.East);Canvas.ForceUpdateCanvases();
            var scene=life.GetComponent<TownSceneryView>();var growth=Flow.GetComponentInChildren<TownGrowthView>();
            var initial=scene.Pieces.Single(p=>TownEastMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Kind==TownSceneryKind.FloristCart);
            var initialBench=scene.Pieces.Single(p=>TownEastMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Kind==TownSceneryKind.ParkBench);
            string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            foreach(int level in new[]{1,2,3,6})
            {
                scene.SetTownLevel(level);growth.Apply(level);Canvas.ForceUpdateCanvases();
                Assert.That(initial.Rect.gameObject.activeSelf,Is.EqualTo(level<2));
                Assert.That(initialBench.Rect.gameObject.activeSelf,Is.EqualTo(level<3));
                var earnedBench=Flow.GetComponentsInChildren<TownGrowthPropGraphic>(true).Single(p=>p.Region==TownRegionId.East&&p.Prop==TownGrowthProp.Bench);
                Assert.That(earnedBench.gameObject.activeSelf,Is.EqualTo(level>=3));
                if(level>=3)Assert.That(Vector2.Distance(initialBench.Rect.anchoredPosition,earnedBench.rectTransform.anchoredPosition),Is.LessThan(.01f),"Upgrade the same finished seat, without leaving a second bench over the name card");
                var earned=Flow.GetComponentsInChildren<TownGrowthPropGraphic>(true).Where(p=>p.Region==TownRegionId.East&&p.Prop==TownGrowthProp.FlowerDisplay&&p.name=="Town grown East 2").ToArray();
                Assert.That(earned.Length,Is.EqualTo(1));Assert.That(earned[0].gameObject.activeSelf,Is.EqualTo(level>=2));
                foreach(var site in new[]{new TownPoint(1.70f,-.67f),new TownPoint(.70f,-.58f)})
                    Assert.That(scene.Pieces.Count(p=>p.Rect.gameObject.activeSelf&&TownEastMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Feet.Equals(site)),Is.EqualTo(1));
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
    }
}
