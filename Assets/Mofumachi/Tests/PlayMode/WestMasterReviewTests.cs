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
    public sealed class WestMasterReviewTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator CafeDoorStaysReadableDuringTravelAndWaitsForTheSecondTap()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);
                life.World.LookAt(TownDistrictId.West);Canvas.ForceUpdateCanvases();
                var facility=life.GetComponent<TownFacilityView>();
                var hit=life.transform.Find("Facility entrance cafe");
                var sign=hit.Find("Facility sign cafe");
                Assert.That(hit.gameObject.activeInHierarchy,Is.True,"The door remains a real destination even when its caption withdraws");
                Assert.That(sign.gameObject.activeSelf,Is.False,"A distant cafe facade stays visible");
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
                Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Flow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Cafe visit"),Is.True);
                Flow.GoBack();Assert.That(Flow.IsModalOpen,Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WestGroundFollowsTheWorldAndBothCafeSideWalksRemainUsable()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.FastTravel(TownDistrictId.West);Canvas.ForceUpdateCanvases();
            var ground=Flow.GetComponentInChildren<TownWestGroundGraphic>();Assert.That(ground,Is.Not.Null);Assert.That(ground.raycastTarget,Is.False);
            var texture=Resources.Load<Texture2D>(TownWestMasterLayout.Resource);Assert.That(texture,Is.Not.Null);Assert.That(ground.mainTexture,Is.SameAs(texture));Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.width,Is.LessThanOrEqualTo(1024));Assert.That(texture.height,Is.LessThanOrEqualTo(2048));
            void AssertProjection()
            {
                Canvas.ForceUpdateCanvases();var mesh=ground.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(4));
                Assert.That(Vector2.Distance(mesh.vertices[0],life.World.Local(new TownPoint(TownWestMasterLayout.Left,TownWestMasterLayout.Bottom),ground.rectTransform)),Is.LessThan(.01f));
            }
            AssertProjection();life.World.Pan(new Vector2(36,-20));AssertProjection();
            foreach(var stop in new[]{new TownPoint(-1.70f,-.22f),new TownPoint(-1.70f,.50f),new TownPoint(-.84f,.50f),new TownPoint(-.80f,.03f),TownWorldMap.Place("west.cafe-front"),TownWorldMap.Place("square")})
            {
                Assert.That(life.World.GuidePoint(stop),Is.True);
                for(int i=0;i<3000&&life.World.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);}
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,stop),Is.LessThan(.002f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator WestPatioReplacesItsInitialStageWithoutChangingProgress()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.LookAt(TownDistrictId.West);Canvas.ForceUpdateCanvases();
            var scene=life.GetComponent<TownSceneryView>();var growth=Flow.GetComponentInChildren<TownGrowthView>();
            var initial=scene.Pieces.Single(p=>TownWestMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Kind==TownSceneryKind.CafeTerrace);
            string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            foreach(int level in new[]{1,2,3,6})
            {
                scene.SetTownLevel(level);growth.Apply(level);Canvas.ForceUpdateCanvases();
                Assert.That(initial.Rect.gameObject.activeSelf,Is.EqualTo(level<2));
                var earned=Flow.GetComponentsInChildren<TownGrowthPropGraphic>(true).Where(p=>p.Region==TownRegionId.West&&p.Prop==TownGrowthProp.CafeTable&&p.name=="Town grown West 2").ToArray();
                Assert.That(earned.Length,Is.EqualTo(1));Assert.That(earned[0].gameObject.activeSelf,Is.EqualTo(level>=2));
                foreach(var site in new[]{new TownPoint(-1.70f,-.67f),new TownPoint(-.70f,-.58f)})
                    Assert.That(scene.Pieces.Count(p=>p.Rect.gameObject.activeSelf&&TownWestMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Feet.Equals(site)),Is.EqualTo(1));
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
    }
}
