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
        [UnityTest] public IEnumerator CameraKeepsWaterActorsMarkersAndGrowthAligned()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var camera=life.GetComponent<TownViewportView>();
            int writes=Store.Writes;string saved=StateCodec.Encode(Flow.Game.State);life.World.Pan(new Vector2(24,16));camera.Advance(1f/30);life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();
            foreach(var actor in Flow.GetComponentsInChildren<TownActor>().Where(a=>a.RegionVisible))Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(actor.WorldPosition)),Is.LessThan(.02f));
            var entrance=TownWorldMap.Place("quest");var point=life.World.Local(entrance);var rect=(RectTransform)life.transform;var screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(point));var source=life.World.Source(screen,null);
            Assert.That(source.X,Is.EqualTo(entrance.X).Within(.0001f));Assert.That(source.Y,Is.EqualTo(entrance.Y).Within(.0001f));
            Assert.That(Flow.GetComponentInChildren<TownAmbientGraphic>().raycastTarget,Is.False);Assert.That(Flow.GetComponentInChildren<TownGrowthView>().VisibleStage,Is.EqualTo(Flow.Game.State.townGrowthLevel));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EntranceMarkerRemainsFullyVisibleAfterCameraGuidance()
        {
            yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,592);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,592),new Rect(0,24,360,536));Canvas.ForceUpdateCanvases();
            var life=Flow.GetComponentInChildren<TownLifeView>();var camera=life.GetComponent<TownViewportView>();var background=Flow.TownBackground;
            Vector2 Screen(float y)=>RectTransformUtility.WorldToScreenPoint(null,background.rectTransform.TransformPoint(new Vector2(0,background.rectTransform.rect.yMin+y*background.rectTransform.rect.height)));
            camera.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=5,position=Screen(.1f)});camera.OnDrag(new PointerEventData(EventSystem.current){pointerId=5,position=Screen(.9f)});camera.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=5});camera.Advance(1f/30);
            life.World.FastTravel(TownRegionId.West);Assert.That(life.GuideToFacility("quest"),Is.True);
            for(int i=0;i<1800&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            Canvas.ForceUpdateCanvases();var root=(RectTransform)life.transform;var marker=life.GetComponentsInChildren<Button>().Single(b=>b.name=="Facility entrance quest");
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(root,marker.transform);
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(root.rect.yMin));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(root.rect.yMax));Assert.That(((RectTransform)marker.transform).rect.height,Is.GreaterThanOrEqualTo(48));LogAssert.NoUnexpectedReceived();
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
            yield return EnterHome();var camera=Flow.GetComponentInChildren<TownViewportView>();var e=new PointerEventData(EventSystem.current){pointerId=7};camera.OnPointerDown(e);Flow.OpenShopPreview();camera.Advance(1f/30);Assert.That(camera.OwnedPointerId,Is.Null);Flow.CloseShopPreview();
            camera.OnPointerDown(e);Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,592),new Rect(0,24,360,536));Canvas.ForceUpdateCanvases();Assert.That(camera.OwnedPointerId,Is.Null);
            camera.OnPointerDown(e);Flow.SendMessage("OnApplicationPause",true);Assert.That(Flow.GetComponentsInChildren<TownViewportView>().All(v=>v.OwnedPointerId==null),Is.True);Flow.SendMessage("OnApplicationPause",false);LogAssert.NoUnexpectedReceived();
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
