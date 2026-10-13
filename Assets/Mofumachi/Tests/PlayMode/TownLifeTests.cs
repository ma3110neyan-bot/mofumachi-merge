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
    public sealed class TownLifeTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator FavoriteDragAndNpcWanderStayOnVisiblePaths()
        {
            yield return EnterHome();
            // Graphic depth is established by rendering, not by a layout rebuild alone.
            yield return null;Canvas.ForceUpdateCanvases();var actors=Flow.GetComponentsInChildren<TownActor>();Assert.That(actors.Length,Is.EqualTo(6));
            var hero=actors.Single(a=>a.Controlled);Assert.That(hero.CharacterId,Is.EqualTo("momo"));
            Assert.That(hero.GetComponentInChildren<CharacterBodyGraphic>().depth,Is.GreaterThanOrEqualTo(0),"The hero must be rendered before pointer input.");
            var pointer=new PointerEventData(EventSystem.current){pointerId=-1,position=RectTransformUtility.WorldToScreenPoint(null,hero.transform.position)};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Any(h=>h.gameObject.GetComponentInParent<TownActor>()==hero),Is.True);
            ExecuteEvents.Execute(hero.gameObject,pointer,ExecuteEvents.pointerDownHandler);Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(-1));
            hero.Movement.SetTarget(-1,TownLifeView.RequestEntrance);yield return new WaitForSecondsRealtime(.3f);Assert.That(hero.Position.Y,Is.GreaterThan(.47f));
            ExecuteEvents.Execute(hero.gameObject,pointer,ExecuteEvents.pointerUpHandler);Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Assert.That(actors.Where(a=>!a.Controlled).All(a=>a.Movement==null),Is.True);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ArrivalNeedsReleaseAndExplicitEnter()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            hero.Movement.TryBeginDrag(1);hero.Movement.SetTarget(1,TownLifeView.RequestEntrance);
            for(int i=0;i<300;i++)hero.Movement.Tick(1f/30);life.ActorReleased(hero);
            Assert.That(life.NearbyFacility,Is.Null,"Owned pointer must be released first");hero.Movement.ReleasePointer(1);life.ActorReleased(hero);
            Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility").onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NpcBubbleDoesNotCoverActionsOrTakeDrag()
        {
            yield return EnterHome();var actors=Flow.GetComponentsInChildren<TownActor>();int writes=Store.Writes;actors.First(a=>!a.Controlled).React();
            Assert.That(Flow.GetComponentsInChildren<Text>().Count(t=>t.name=="Town bubble"),Is.LessThanOrEqualTo(1));
            Assert.That(Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Town bubble").All(t=>!t.raycastTarget),Is.True);
            Canvas.ForceUpdateCanvases();
            var guide=(RectTransform)Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Guide to quest").transform;
            var panel=(RectTransform)Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble").transform.parent;
            var a=new Vector3[4];var b=new Vector3[4];guide.GetWorldCorners(a);panel.GetWorldCorners(b);
            Assert.That(new Rect((Vector2)a[0],(Vector2)(a[2]-a[0])).Overlaps(new Rect((Vector2)b[0],(Vector2)(b[2]-b[0]))),Is.False,"Dialogue must leave facility guidance readable.");
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Flow.Game.State.coins,Is.EqualTo(0));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseSettingsAndAspectChangeReleaseInput()
        {
            yield return EnterHome();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);hero.Movement.TryBeginDrag(1);
            Flow.SendMessage("OnApplicationPause",true);Assert.That(hero.Movement.OwnedPointerId,Is.Null);Assert.That(Flow.GetComponentsInChildren<TownLifeView>().All(v=>!v.enabled),Is.True);
            Flow.SendMessage("OnApplicationPause",false);hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);hero.Movement.TryBeginDrag(1);Flow.ShowSettings();Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Flow.GoBack();var life=Flow.GetComponentInChildren<TownLifeView>();life.CancelInteraction();Assert.That(life.NearbyFacility,Is.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FacilityGuideArrivesThenRequiresExplicitEntry()
        {
            yield return EnterHome();int writes=Store.Writes;var life=Flow.GetComponentInChildren<TownLifeView>();
            Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Guide to ")),Is.EqualTo(2));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Guide to quest").onClick.Invoke();
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);Assert.That(hero.IsGuided,Is.True);
            for(int i=0;i<300;i++)life.AdvanceFrame(1f/30);
            Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);Assert.That(life.NearbyFacility,Is.EqualTo("quest"));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility").onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ManualDragAndPauseCanInterruptFacilityGuidance()
        {
            yield return EnterHome();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            Assert.That(hero.GuideTo(new TownPoint(float.NaN,0)),Is.False);Assert.That(hero.GuideTo(TownLifeView.ShopEntrance),Is.True);
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=17});Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(17));
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=17});Assert.That(hero.GuideTo(TownLifeView.ShopEntrance),Is.True);
            Flow.SendMessage("OnApplicationPause",true);Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Flow.SendMessage("OnApplicationPause",false);Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo("momo"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NpcsVisitSeveralStreetStopsWithoutChangingProgress()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var npc=Flow.GetComponentsInChildren<TownActor>().First(a=>!a.Controlled);
            int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);var visited=new System.Collections.Generic.HashSet<int>();
            var stops=TownLifeView.WalkStops;
            for(int i=0;i<1200;i++)
            {
                life.AdvanceFrame(.1f);
                for(int j=0;j<stops.Count;j++)if(Vector2.Distance(new Vector2(npc.Position.X,npc.Position.Y),new Vector2(stops[j].X,stops[j].Y))<.004f)visited.Add(j);
            }
            Assert.That(visited.Count,Is.GreaterThanOrEqualTo(3));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));
            npc.React();life.AdvanceFrame(.1f);Assert.That(npc.GetComponentInChildren<CharacterBodyGraphic>().Expression,Is.EqualTo(CharacterExpression.Smile));
            Assert.That(npc.VisualRoot.anchoredPosition.y,Is.GreaterThan(3));
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            foreach(string place in new[]{"bench","flowers","fountain"})
            {
                hero.React();Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Visit ")),Is.EqualTo(3));
                Canvas.ForceUpdateCanvases();foreach(var visit in Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Visit ")))
                    Assert.That(((RectTransform)visit.transform).rect.height,Is.GreaterThanOrEqualTo(48),visit.name);
                Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Visit "+place).onClick.Invoke();Assert.That(hero.IsGuided,Is.True);
                for(int i=0;i<300;i++)life.AdvanceFrame(1f/30);
                Assert.That(hero.IsGuided,Is.False);Assert.That(life.NearbyFacility,Is.Null,"A rest stop does not enter a facility.");
                Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"),Is.True);
            }
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));
            hero.React();Flow.SendMessage("OnApplicationPause",true);Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Visit ")),Is.False);
            Flow.SendMessage("OnApplicationPause",false);Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo("momo"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GrowthLayersFollowSavedLevelWithoutChangingPaths()
        {
            yield return EnterHome();foreach(int level in new[]{0,1,2,3,4,5,6,int.MaxValue})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var growth=Flow.GetComponentInChildren<TownGrowthView>();Assert.That(growth.VisibleStage,Is.EqualTo(Mathf.Min(level,6)));Assert.That(Flow.GetComponentsInChildren<TownActor>().Length,Is.EqualTo(6));
            }
            Flow.OpenShopPreview();Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Flow.Game.State.coins,Is.EqualTo(0));Flow.CloseShopPreview();LogAssert.NoUnexpectedReceived();
        }
    }
}
