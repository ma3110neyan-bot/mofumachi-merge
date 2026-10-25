using System.Collections;
using System.Collections.Generic;
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
    public sealed class MajorPolishTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator FingerFollowsTheFloorAndReleaseDoesNotContinueToTheOldDestination()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var actor=life.Actors.Single(a=>a.Controlled);string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            var e=new PointerEventData(EventSystem.current){pointerId=711};actor.OnPointerDown(e);actor.OnBeginDrag(e);
            var point=life.transform.TransformPoint(life.World.Local(new TownPoint(.28f,-.05f)));e.position=RectTransformUtility.WorldToScreenPoint(null,point);actor.OnDrag(e);
            Assert.That(life.World.Model.IsFollowingFinger,Is.True);for(int i=0;i<70;i++)life.AdvanceFrame(1f/30);
            Assert.That(life.World.Model.Area.Contains(actor.WorldPosition),Is.True);var before=actor.WorldPosition;
            actor.OnPointerUp(e);Assert.That(actor.Movement.OwnedPointerId,Is.Null);for(int i=0;i<45;i++)life.AdvanceFrame(1f/30);
            Assert.That(life.World.Model.IsFollowingFinger,Is.False);Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWorldLayout.Distance(before,actor.WorldPosition),Is.LessThan(.025f));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseCancelsTheFingerAndANewWorldPointDoesNotOwnAnotherPointer()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var actor=life.Actors.Single(a=>a.Controlled);
            var e=new PointerEventData(EventSystem.current){pointerId=712,position=RectTransformUtility.WorldToScreenPoint(null,life.transform.TransformPoint(life.World.Local(new TownPoint(.28f,-.05f))))};
            actor.OnPointerDown(e);actor.OnBeginDrag(e);actor.OnDrag(e);var target=life.World.Model.Hero.Target;
            actor.OnDrag(new PointerEventData(EventSystem.current){pointerId=713,position=Vector2.zero});Assert.That(life.World.Model.Hero.Target,Is.EqualTo(target));
            Flow.SendMessage("OnApplicationPause",true);var stopped=actor.WorldPosition;for(int i=0;i<10;i++)life.AdvanceFrame(1f/30);
            Assert.That(actor.Movement.OwnedPointerId,Is.Null);Assert.That(life.World.Model.IsFollowingFinger,Is.False);Assert.That(actor.WorldPosition,Is.EqualTo(stopped));Flow.SendMessage("OnApplicationPause",false);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ReleasingAMovingFingerAtEitherDoorOffersEntryAfterCoasting()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var actor=life.Actors.Single(a=>a.Controlled);
            foreach(var door in new[]{("quest",TownWorldMap.Place("quest")),("shop",TownWorldMap.Place("shop"))})
            {
                life.CancelInteraction();var start=new TownPoint(door.Item2.X-.015f,door.Item2.Y);life.World.Model.Restore(actor.CharacterId,start);life.World.ReturnToHero();life.AdvanceFrame(1f/30);
                var e=new PointerEventData(EventSystem.current){pointerId=714};actor.OnPointerDown(e);actor.OnBeginDrag(e);
                var target=life.transform.TransformPoint(life.World.Local(new TownPoint(door.Item2.X+.03f,door.Item2.Y)));e.position=RectTransformUtility.WorldToScreenPoint(null,target);actor.OnDrag(e);
                life.AdvanceFrame(1f/30);Assert.That(life.World.Model.Hero.IsWalking,Is.True);Assert.That(life.World.Model.Hero.Speed,Is.GreaterThan(0));actor.OnPointerUp(e);
                for(int i=0;i<60;i++)life.AdvanceFrame(1f/30);
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(life.NearbyFacility,Is.EqualTo(door.Item1));
                Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name=="Enter facility"&&b.IsInteractable()),Is.True);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Entry stays an explicit player action");
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DiscoveryNoticeOpensTheDiscoveredCharacterAndKeepsFavoriteConfirmationSeparate()
        {
            yield return EnterHome();string favorite=Flow.Game.State.favoriteCharacterId;Assert.That(Flow.TalkToCharacter("noah"),Is.True);
            var notice=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="New book discovery");Assert.That(notice.GetComponentInChildren<Text>().text,Does.Contain("ノア"));
            Flow.TalkToCharacter("noah");Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="New book discovery"),Is.SameAs(notice),"The same tier must not repeatedly create a popup");
            notice.onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.CharacterSelection));Assert.That(Flow.PreviewFavoriteId,Is.EqualTo("noah"));Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo(favorite));
            Assert.That(TownCharacterBook.For("noah",Flow.Game.State,Flow.Daily.Data).IsNew,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator BookNextDiscoveryAndStoryPaperFitShortAndPixelHeightScreens()
        {
            yield return EnterHome();Flow.ShowCharacterSelection();
            using(var viewport=new FixedViewport(Flow))
            foreach(int height in new[]{592,640,740,808})
            {
                viewport.Apply(height);
                foreach(var c in CharacterCatalog.All)
                {
                    Flow.PreviewFavorite(c.Id);Canvas.ForceUpdateCanvases();
                    foreach(string name in new[]{"Book next discovery","Partner description","Book collection counts"})
                    {var text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name==name);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),name+" "+c.Id+" "+height);}
                    Assert.That(Flow.GetComponentsInChildren<StoryPaperGraphic>().All(p=>!p.raycastTarget),Is.True);
                    var next=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Book next discovery");Assert.That(next.GetComponentInParent<ScrollRect>(),Is.Null,"The next discovery stays visible above the scrollable pages");
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FlatBoardKeepsAlignedEdgesAndAllThirtyCellsActuallyReceiveRaycasts()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var grid=Flow.GetComponentInChildren<MergeGridLayout>();grid.ResizeNow();Canvas.ForceUpdateCanvases();
            var board=Flow.GetComponentInChildren<PerspectiveBoardGraphic>();var mesh=board.canvasRenderer.GetMesh();float top=board.rectTransform.rect.yMax;
            var back=mesh.vertices.Where(v=>v.y>=top-.01f).ToArray();Assert.That(back.Max(v=>v.x)-back.Min(v=>v.x),Is.GreaterThan(board.rectTransform.rect.width*.88f));
            Assert.That(board.GetComponentInParent<RoundedPanelGraphic>().color.a,Is.Zero,"Only the thin ivory panel draws the board surface");
            using(var viewport=new FixedViewport(Flow))
            foreach(int height in new[]{592,640,740,808})
            {
                viewport.Apply(height);Flow.GetComponentInChildren<ProductionFrameLayout>().ResizeNow();Canvas.ForceUpdateCanvases();grid.ResizeNow();Canvas.ForceUpdateCanvases();
                foreach(var cell in grid.GetComponentsInChildren<MergeBoardView>())
                {
                    var r=(RectTransform)cell.transform;Assert.That(r.rect.width,Is.GreaterThanOrEqualTo(48));Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(48));
                    foreach(var p in new[]{r.rect.center,new Vector2(r.rect.xMin+1,r.rect.yMin+1),new Vector2(r.rect.xMin+1,r.rect.yMax-1),new Vector2(r.rect.xMax-1,r.rect.yMin+1),new Vector2(r.rect.xMax-1,r.rect.yMax-1)})
                    {
                        var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(viewport.Camera,r.TransformPoint(p))};
                        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Assert.That(hits,Is.Not.Empty,cell.name+" "+height);
                        Assert.That(hits[0].gameObject.GetComponentInParent<MergeBoardView>(),Is.SameAs(cell),cell.name+" must be the actual foremost target at "+height+" "+p);
                    }
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
        // A real resized canvas and camera viewport, not only different safe-area
        // anchors on an unchanged runtime canvas. Every case includes OS insets.
        internal sealed class FixedViewport:System.IDisposable
        {
            private readonly Canvas canvas;private readonly ResponsiveUILayout layout;private readonly RectTransform rect;private readonly GameObject cameraObject;
            private RenderTexture target;public Camera Camera {get;}
            public FixedViewport(UIFlowController flow)
            {
                canvas=flow.GetComponentInChildren<Canvas>();layout=flow.GetComponentInChildren<ResponsiveUILayout>();rect=(RectTransform)canvas.transform;
                canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;rect.localScale=Vector3.one;rect.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                cameraObject=new GameObject("Actual Polish viewport camera");cameraObject.transform.SetParent(flow.transform,false);Camera=cameraObject.AddComponent<Camera>();Camera.orthographic=true;Camera.transform.position=new Vector3(0,0,-10);Camera.nearClipPlane=.1f;Camera.farClipPlane=100;canvas.worldCamera=Camera;
            }
            public void Apply(int height)
            {
                if(target!=null){Camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);}
                target=new RenderTexture(360,height,24);target.Create();Camera.targetTexture=target;Camera.orthographicSize=height/2f;
                rect.sizeDelta=new Vector2(360,height);layout.ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));Canvas.ForceUpdateCanvases();
                Assert.That(rect.rect.height,Is.EqualTo(height).Within(.01f));Assert.That(layout.ContentRoot.rect.height,Is.EqualTo(height-56).Within(.01f));
            }
            public void Dispose(){Camera.targetTexture=null;if(target!=null){target.Release();Object.DestroyImmediate(target);}Object.DestroyImmediate(cameraObject);}
        }
    }
}
