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
    public sealed class TownViewportPresentationTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator AllFiveEdgesKeepReturnInputAndWalkBackWithoutTeleporting()
        {
            yield return EnterHome();var life=FrameTownPortrait();var hero=life.Actors.Single(a=>a.Controlled);
            var field=(RectTransform)life.transform;var home=TownWorldMap.Place("square");
            Vector2 Screen(TownPoint p)=>RectTransformUtility.WorldToScreenPoint(null,field.TransformPoint(life.World.Local(p)));
            foreach(var edge in new[]{new TownPoint(1.8f,.68f),new TownPoint(-1.8f,.68f),new TownPoint(.43f,2.98f),new TownPoint(.43f,-2.98f),new TownPoint(0,.97f)})
            {
                Assert.That(life.World.GuidePoint(edge),Is.True);
                for(int i=0;i<6000&&life.World.Model.Hero.IsWalking;i++)life.AdvanceFrame(1f/30);
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(hero.WorldPosition,edge),Is.LessThan(.002f));
                yield return null;Flow.GetComponentInChildren<TownStreetHud>().Refresh();Canvas.ForceUpdateCanvases();
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(field,hero.VisualRoot);
                Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(field.rect.xMin-.5f));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(field.rect.xMax+.5f));
                Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(field.rect.yMin-.5f));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(field.rect.yMax+.5f),"A top-edge body must remain on-screen for the player to grab");
                var body=hero.GetComponentInChildren<CharacterBodyGraphic>();
                var grab=RectTransformUtility.WorldToScreenPoint(null,body.transform.TransformPoint(body.rectTransform.rect.center));
                Assert.That(body.raycastTarget&&body.Raycast(grab,null),Is.True,"Clipping must not remove the hero's return hit target");
                var entry=TownWorldMap.Place(TownDistrictLayout.EntranceId(TownDistrictLayout.At(edge)));float distance=TownWalkableArea.Distance(edge,entry);
                var request=new TownPoint(edge.X+(entry.X-edge.X)*.22f/distance,edge.Y+(entry.Y-edge.Y)*.22f/distance);
                Assert.That(life.World.Model.Area.TryClamp(request,out var inward,.09f),Is.True);
                var drag=new PointerEventData(EventSystem.current){pointerId=551,position=grab};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(drag,hits);
                Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].gameObject.GetComponentInParent<TownActor>(),Is.SameAs(hero),"The topmost real hit must be the hero, not Menu, Merge or the next-goal card");
                drag.pointerPressRaycast=hits[0];ExecuteEvents.Execute(hero.gameObject,drag,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(hero.gameObject,drag,ExecuteEvents.beginDragHandler);
                Assert.That(life.IsManuallyDragging,Is.True);
                for(int i=0;i<240&&TownWalkableArea.Distance(hero.WorldPosition,inward)>.012f;i++)
                {drag.position=Screen(inward);hero.OnDrag(drag);life.AdvanceFrame(1f/30);}
                hero.OnPointerUp(drag);for(int i=0;i<60&&life.World.Model.Hero.IsWalking;i++)life.AdvanceFrame(1f/30);
                Assert.That(life.IsManuallyDragging,Is.False);Assert.That(TownWalkableArea.Distance(hero.WorldPosition,edge),Is.GreaterThan(.13f),"Leave the edge through real drag/release handlers");
                Assert.That(life.World.GuidePoint(home),Is.True);
                for(int i=0;i<6000&&life.World.Model.Hero.IsWalking;i++)life.AdvanceFrame(1f/30);
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(hero.WorldPosition,home),Is.LessThan(.002f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CameraKeepsWaterActorsMarkersAndGrowthAligned()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var camera=life.GetComponent<TownViewportView>();
            int writes=Store.Writes;string saved=StateCodec.Encode(Flow.Game.State);life.World.Pan(new Vector2(24,16));camera.Advance(1f/30);life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();
            foreach(var actor in Flow.GetComponentsInChildren<TownActor>().Where(a=>a.RegionVisible))Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(actor.WorldPosition)),Is.LessThan(.02f));
            var entrance=TownWorldMap.Place("quest");var point=life.World.Local(entrance);var rect=(RectTransform)life.transform;var screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(point));var source=life.World.Source(screen,null);
            Assert.That(source.X,Is.EqualTo(entrance.X).Within(.0001f));Assert.That(source.Y,Is.EqualTo(entrance.Y).Within(.0001f));
            float scale=TownWorldLayout.ScaleForViewport(rect.rect.width,rect.rect.height);var square=TownWorldMap.Place("square");
            var expected=rect.rect.center+new Vector2((square.X-life.World.Camera.Center.X)*scale,(square.Y-life.World.Camera.Center.Y)*scale*TownWorldLayout.ImageAspect);
            Assert.That(Vector2.Distance(life.World.Local(square),expected),Is.LessThan(.001f),"Actor projection must use the actual playable field, including its safe-area offsets");
            // The device PDF exposed empty map corners on both north side pans.
            // Exercise the live field projection, not only the pure camera.
            Assert.That(life.World.LookAt(TownDistrictId.North),Is.True);
            foreach(float delta in new[]{-10000f,20000f})
            {
                life.World.Pan(new Vector2(delta,0));Canvas.ForceUpdateCanvases();
                float halfWidth=rect.rect.width/scale/2;
                Assert.That(life.World.Camera.Center.X-halfWidth,Is.GreaterThanOrEqualTo(-.60001f));
                Assert.That(life.World.Camera.Center.X+halfWidth,Is.LessThanOrEqualTo(.60001f));
            }
            Assert.That(life.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("Painted road direction ")),Is.Empty,"Normal field never builds floating direction captions, including while walking");
            Assert.That(Flow.GetComponentInChildren<TownAmbientGraphic>().raycastTarget,Is.False);Assert.That(Flow.GetComponentInChildren<TownGrowthView>().VisibleStage,Is.EqualTo(Flow.Game.State.townGrowthLevel));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EntranceMarkerRemainsFullyVisibleAfterCameraGuidance()
        {
            yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,592);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,592),new Rect(0,24,360,536));Canvas.ForceUpdateCanvases();
            var life=Flow.GetComponentInChildren<TownLifeView>();var camera=life.GetComponent<TownViewportView>();var field=(RectTransform)life.transform;
            Vector2 Screen(float y)=>RectTransformUtility.WorldToScreenPoint(null,field.TransformPoint(new Vector2(field.rect.center.x,field.rect.yMin+y*field.rect.height)));
            camera.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=5,position=Screen(.1f)});camera.OnDrag(new PointerEventData(EventSystem.current){pointerId=5,position=Screen(.9f)});camera.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=5});camera.Advance(1f/30);
            life.World.FastTravel(TownDistrictId.North);Assert.That(life.GuideToFacility("quest"),Is.True);
            for(int i=0;i<1800&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            Canvas.ForceUpdateCanvases();var root=(RectTransform)life.transform;var marker=life.GetComponentsInChildren<Button>().Single(b=>b.name=="Facility entrance quest");
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(root,marker.transform);
            Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(root.rect.xMin));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(root.rect.xMax));Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(root.rect.yMin));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(root.rect.yMax));Assert.That(((RectTransform)marker.transform).rect.height,Is.GreaterThanOrEqualTo(48));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActorAndCameraGesturesHaveOneOwner()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var camera=life.GetComponent<TownViewportView>();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            var e=new PointerEventData(EventSystem.current){pointerId=11,position=new Vector2(180,300)};camera.OnPointerDown(e);Assert.That(camera.OwnedPointerId,Is.EqualTo(11));camera.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=12});Assert.That(camera.OwnedPointerId,Is.EqualTo(11));
            var oldUv=life.World.Camera.Center;camera.OnDrag(new PointerEventData(EventSystem.current){pointerId=11,position=new Vector2(180,340)});camera.OnPointerUp(e);camera.Advance(1f/30);Assert.That(life.World.Camera.Center,Is.Not.EqualTo(oldUv),"Release must not discard the last drag delta");camera.OnPointerDown(e);
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=13});Assert.That(camera.OwnedPointerId,Is.Null);Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(13));camera.OnPointerDown(e);Assert.That(camera.OwnedPointerId,Is.Null);hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=13});LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseModalAndResizeReleaseCameraInput()
        {
            yield return EnterHome();var life=FrameTownPortrait();var camera=life.GetComponent<TownViewportView>();var e=new PointerEventData(EventSystem.current){pointerId=7};
            camera.OnPointerDown(e);Assert.That(camera.OwnedPointerId,Is.EqualTo(7));Flow.OpenTownMenu();Assert.That(Flow.IsModalOpen,Is.True);camera.Advance(1f/30);Assert.That(camera.OwnedPointerId,Is.Null);Flow.CloseTownMenu();Assert.That(Flow.IsModalOpen,Is.False);
            camera.OnPointerDown(e);Assert.That(camera.OwnedPointerId,Is.EqualTo(7));Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,592),new Rect(0,24,360,536));Canvas.ForceUpdateCanvases();Assert.That(camera.OwnedPointerId,Is.Null);
            camera.OnPointerDown(e);Assert.That(camera.OwnedPointerId,Is.EqualTo(7));Flow.SendMessage("OnApplicationPause",true);Assert.That(Flow.GetComponentsInChildren<TownViewportView>().All(v=>v.OwnedPointerId==null),Is.True);Flow.SendMessage("OnApplicationPause",false);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LeavingHomeRestoresBackgroundAndDoesNotSaveCamera()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.World.Pan(new Vector2(24,16));life.GetComponent<TownViewportView>().Advance(1f/30);int writes=Store.Writes;
            Flow.ShowMerge();Assert.That(Store.Writes,Is.EqualTo(writes+1));Assert.That(Flow.TownBackground.GetComponent<BackgroundCover>().enabled,Is.True);
            var size=Flow.TownBackground.rectTransform.rect;float aspect=size.width/size.height,source=(float)Flow.TownBackground.texture.width/Flow.TownBackground.texture.height;
            Assert.That(Flow.TownBackground.uvRect.width,Is.EqualTo(Mathf.Min(1,aspect/source)).Within(.0001f));Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo("momo"));LogAssert.NoUnexpectedReceived();
        }
    }
}
