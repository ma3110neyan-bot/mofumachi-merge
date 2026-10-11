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
            Assert.That(hero.GetComponent<Graphic>().depth,Is.GreaterThanOrEqualTo(0),"The hero must be rendered before pointer input.");
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
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Flow.Game.State.coins,Is.EqualTo(0));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PauseSettingsAndAspectChangeReleaseInput()
        {
            yield return EnterHome();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);hero.Movement.TryBeginDrag(1);
            Flow.SendMessage("OnApplicationPause",true);Assert.That(hero.Movement.OwnedPointerId,Is.Null);Assert.That(Flow.GetComponentsInChildren<TownLifeView>().All(v=>!v.enabled),Is.True);
            Flow.SendMessage("OnApplicationPause",false);hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);hero.Movement.TryBeginDrag(1);Flow.ShowSettings();Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Flow.GoBack();var life=Flow.GetComponentInChildren<TownLifeView>();life.CancelInteraction();Assert.That(life.NearbyFacility,Is.Null);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GrowthLayersFollowSavedLevelWithoutChangingPaths()
        {
            yield return EnterHome();foreach(int level in new[]{0,1,2,3,6})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var growth=Flow.GetComponentInChildren<TownGrowthView>();Assert.That(growth.VisibleStage,Is.EqualTo(Mathf.Min(level,3)));Assert.That(Flow.GetComponentsInChildren<TownActor>().Length,Is.EqualTo(6));
            }
            Flow.OpenShopPreview();Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Flow.Game.State.coins,Is.EqualTo(0));Flow.CloseShopPreview();LogAssert.NoUnexpectedReceived();
        }
    }
}
