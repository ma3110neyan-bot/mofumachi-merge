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
        private Vector3 lifePoint(TownActor hero,TownPoint point){var life=Flow.GetComponentInChildren<TownLifeView>();return life.transform.TransformPoint(life.World.Local(point));}

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
            hero.OnDrag(new PointerEventData(EventSystem.current){pointerId=-1,position=RectTransformUtility.WorldToScreenPoint(null,lifePoint(hero,TownWorldMap.Place("quest")))});yield return new WaitForSecondsRealtime(.3f);Assert.That(hero.WorldPosition.Y,Is.GreaterThan(TownWorldMap.Place("square").Y));
            ExecuteEvents.Execute(hero.gameObject,pointer,ExecuteEvents.pointerUpHandler);Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Assert.That(actors.Where(a=>!a.Controlled).All(a=>a.Movement==null),Is.True);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ArrivalNeedsReleaseAndExplicitEnter()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=1});
            life.World.Model.Restore(hero.CharacterId,TownWorldMap.Place("quest"),"quest");life.World.ReturnToHero();life.AdvanceFrame(1f/30);life.ActorReleased(hero);
            Assert.That(life.NearbyFacility,Is.Null);hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=1});Assert.That(life.NearbyFacility,Is.EqualTo("quest"));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility").onClick.Invoke();for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NpcBubbleDoesNotCoverActionsOrTakeDrag()
        {
            yield return EnterHome();var actors=Flow.GetComponentsInChildren<TownActor>();int writes=Store.Writes;actors.First(a=>!a.Controlled&&a.RegionVisible).React();
            Assert.That(Flow.GetComponentsInChildren<Text>().Count(t=>t.name=="Town bubble"),Is.LessThanOrEqualTo(1));
            Assert.That(Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Town bubble").All(t=>!t.raycastTarget),Is.True);
            Canvas.ForceUpdateCanvases();
            var guide=(RectTransform)Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Nav Home").transform;
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
            Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled).React();
            Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Guide to ")),Is.EqualTo(2));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Guide to quest").onClick.Invoke();
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);Assert.That(hero.IsGuided,Is.True);
            for(int i=0;i<1800&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);Assert.That(life.NearbyFacility,Is.EqualTo("quest"));
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Enter facility").onClick.Invoke();for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ManualDragAndPauseCanInterruptFacilityGuidance()
        {
            yield return EnterHome();var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            Assert.That(hero.GuideTo(new TownPoint(float.NaN,0)),Is.False);Assert.That(hero.GuideTo(TownWorldLayout.ToLocal(hero.RegionId,TownWorldMap.Place("shop"))),Is.True);
            hero.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=17});Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.EqualTo(17));
            hero.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=17});Assert.That(hero.GuideTo(TownWorldLayout.ToLocal(hero.RegionId,TownWorldMap.Place("shop"))),Is.True);
            Flow.SendMessage("OnApplicationPause",true);Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);
            Flow.SendMessage("OnApplicationPause",false);Assert.That(Flow.Game.State.favoriteCharacterId,Is.EqualTo("momo"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NpcsVisitSeveralStreetStopsWithoutChangingProgress()
        {
            yield return EnterHome();
            // Keep real actor visibility independent of the Editor's current Game view.
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,740);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,740),new Rect(0,24,360,684));Canvas.ForceUpdateCanvases();
            var life=Flow.GetComponentInChildren<TownLifeView>();life.World.Refresh();var npc=life.Actors.First(a=>!a.Controlled&&a.RegionVisible);
            int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);var visited=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<1200;i++)
            {
                life.AdvanceFrame(.1f);
                var stops=TownRegionCatalog.ForLevel(npc.RegionId,life.TownLevel).Map.Places;
                foreach(var stop in stops)if(TownWorldLayout.Distance(npc.WorldPosition,TownWorldMap.Place(stop.Id))<.004f)visited.Add(stop.Id);
            }
            Assert.That(visited.Count,Is.GreaterThanOrEqualTo(3));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Assert.That(Store.Writes,Is.EqualTo(writes));
            Assert.That(life.Regions.LookAtRegion(npc.RegionId),Is.True);
            // A resident can still be on a connecting road, outside the region-center
            // camera. Frame its actual position without relocating or stopping it.
            var npcPosition=npc.WorldPosition;life.World.Camera.Jump(npcPosition);life.World.Refresh();
            Assert.That(TownWorldLayout.Distance(npcPosition,npc.WorldPosition),Is.LessThan(.0001f),"Framing must not move the resident");
            life.AdvanceFrame(1f/30); // Render at the new camera before attempting the tap.
            Assert.That(npc.RegionVisible,Is.True,"Tap only a resident actually visible to the camera");
            var bounds=((RectTransform)life.transform).rect;var corners=new Vector3[4];npc.GetComponentInChildren<CharacterBodyGraphic>().rectTransform.GetWorldCorners(corners);
            Assert.That(corners.All(p=>bounds.Contains(life.transform.InverseTransformPoint(p))),Is.True,"The rendered body must fit inside the town viewport");
            Assert.That(Vector2.Distance(((RectTransform)npc.transform).anchoredPosition,life.World.Local(npc.WorldPosition)),Is.LessThan(.001f));
            int meetings=Flow.Daily.Data.Characters.Single(c=>c.Id==npc.CharacterId).Meetings;
            npc.React();
            Assert.That(Flow.Daily.Data.Characters.Single(c=>c.Id==npc.CharacterId).Meetings,Is.EqualTo(meetings+1));
            Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Speaker name"&&t.text==CharacterCatalog.Find(npc.CharacterId).Name),Is.True,"The speaker's own name plate must identify the tapped resident");
            Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"&&t.text.Length>0),Is.True,"The tapped resident must actually speak");
            life.AdvanceFrame(.1f);Assert.That(npc.GetComponentInChildren<CharacterBodyGraphic>().Expression,Is.EqualTo(CharacterExpression.Smile));
            Assert.That(npc.VisualRoot.anchoredPosition.y,Is.GreaterThan(3));
            var hero=Flow.GetComponentsInChildren<TownActor>().Single(a=>a.Controlled);
            life.Regions.ReturnToHero();
            var destinations=new[]{
                new {Button="Visit bench",Place="bench-right"},
                new {Button="Visit central fountain",Place="fountain"},
                new {Button="Visit east flowers",Place="east.flowers"}};
            string[] expectedChoices={"Visit west cafe","Visit central fountain","Visit east flowers","Visit bench","Guide to quest","Guide to shop"};
            foreach(var destination in destinations)
            {
                hero.React();var choices=Flow.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Visit ")||b.name.StartsWith("Guide to ")).ToArray();
                Assert.That(choices.Select(b=>b.name),Is.EquivalentTo(expectedChoices),"The character menu must offer the six Quality Pass destinations");
                Canvas.ForceUpdateCanvases();foreach(var visit in choices)
                    Assert.That(((RectTransform)visit.transform).rect.height,Is.GreaterThanOrEqualTo(48),visit.name);
                choices.Single(b=>b.name==destination.Button).onClick.Invoke();Assert.That(life.Regions.HasHeroJourney,Is.True);
                // IsGuided pauses at a regional gate/fade. Wait for the whole
                // journey, and observe its greeting on the actual arrival frame.
                for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
                Assert.That(life.Regions.HasHeroJourney,Is.False,"The outing must finish within its bounded journey budget");
                Assert.That(hero.IsGuided,Is.False);Assert.That(hero.Movement.OwnedPointerId,Is.Null);Assert.That(life.NearbyFacility,Is.Null,"A rest stop does not enter a facility.");
                Assert.That(TownRegionCatalog.TryFindPlace(destination.Place,life.TownLevel,out var target,out _),Is.True);
                Assert.That(hero.RegionId,Is.EqualTo(target.Region),destination.Place);
                Assert.That(Vector2.Distance(new Vector2(hero.WorldPosition.X,hero.WorldPosition.Y),new Vector2(TownWorldMap.Place(destination.Place).X,TownWorldMap.Place(destination.Place).Y)),Is.LessThan(.001f),destination.Place);
                Assert.That(Flow.GetComponentsInChildren<Text>().Any(t=>t.name=="Town bubble"),Is.True,"Observe the greeting immediately when arrival completes");
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
