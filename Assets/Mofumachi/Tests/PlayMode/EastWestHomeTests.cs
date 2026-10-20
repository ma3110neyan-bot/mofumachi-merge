using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
 public sealed class EastWestHomeTests:SliceUiTestFixture
 {
  [UnityTest] public IEnumerator StreetHasACompactHudAndMenuActionsAreOnlyExpandedOnRequest()
  {
   yield return EnterHome();var life=FrameTownPortrait();var field=(RectTransform)life.transform;
   Assert.That(field.offsetMin,Is.EqualTo(Vector2.zero));Assert.That(field.offsetMax,Is.EqualTo(Vector2.zero),"The street now fills the safe view behind floating controls");
   foreach(var name in new[]{"Today town","Town menu","Nav Merge"})
   {var button=Flow.GetComponentsInChildren<Button>().Single(b=>b.name==name);var hit=((RectTransform)button.transform).rect;Assert.That(hit.width,Is.GreaterThanOrEqualTo(48),name);Assert.That(hit.height,Is.GreaterThanOrEqualTo(48),name);Assert.That(button.GetComponent<ButtonPressFeedback>().PreserveHitbox,Is.True,name);}
   Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name=="Region walk West"),Is.False);
   Flow.OpenTownMenu();Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
   foreach(var name in new[]{"Region walk West","Region walk Central","Region walk East","Region walk North","Region walk South","Nav Quest","Walk to cafe","Walk to florist","Town book","Town today"})Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name==name),Is.Not.Null);
   Flow.GoBack();Assert.That(Flow.IsModalOpen,Is.False);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
  }
  [UnityTest] public IEnumerator ExpandedMenuCancelsMovementAndGuideWalksWithoutChangingProgress()
  {
   yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.World.Model.Hero;
   // At the initial level the named cafe activity is locked; the menu still
   // lets the player walk to its physical doorstep without changing unlocks.
   TownMenuButton("Walk to cafe").onClick.Invoke();Assert.That(Flow.IsModalOpen,Is.False);Assert.That(hero.IsWalking,Is.True);
   Assert.That(TownWalkableArea.Distance(hero.Target,TownWorldMap.Place("west.cafe-front")),Is.LessThan(.001f));
   life.AdvanceFrame(1f/30);var p=hero.Position;string save=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
   Flow.OpenTownMenu();for(int i=0;i<30;i++)life.AdvanceFrame(1f/30);Assert.That(hero.Position,Is.EqualTo(p));Assert.That(hero.IsWalking,Is.False);
   Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Walk to florist").onClick.Invoke();Assert.That(Flow.IsModalOpen,Is.False);Assert.That(hero.IsWalking,Is.True);Assert.That(hero.PlaceId,Is.EqualTo("shop"));
   Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(save));LogAssert.NoUnexpectedReceived();
  }
  [UnityTest] public IEnumerator ResidentsPassBehindAndInFrontOfTheSameFountainPixels()
  {
   yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var hero=life.Actors.Single(a=>a.Controlled);var foreground=life.GetComponentInChildren<TownFountainGraphic>();
   Assert.That(life.World.Model.Area.Contains(new TownPoint(-.21f,.30f)),Is.True);Assert.That(life.World.Model.Area.Contains(new TownPoint(0,.02f)),Is.True);
   life.World.Model.Restore(hero.CharacterId,new TownPoint(-.21f,.30f));life.World.ReturnToHero();life.AdvanceFrame(1f/30);
   Assert.That(hero.transform.GetSiblingIndex(),Is.LessThan(foreground.transform.GetSiblingIndex()));
   life.World.Model.Restore(hero.CharacterId,new TownPoint(0,.02f));life.World.ReturnToHero();life.AdvanceFrame(1f/30);
   Assert.That(hero.transform.GetSiblingIndex(),Is.GreaterThan(foreground.transform.GetSiblingIndex()));Assert.That(foreground.raycastTarget,Is.False);LogAssert.NoUnexpectedReceived();
  }
  [UnityTest] public IEnumerator CompactNextGoalFitsForEveryLevelAndDailyStateOnNarrowPhones()
  {
   yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
   ((RectTransform)canvas.transform).sizeDelta=new Vector2(320,592);Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(320,592),new Rect(0,24,320,536));
   for(int stage=0;stage<7;stage++)
   {
    Flow.Game.State.townGrowthLevel=stage;Flow.ShowHome();Canvas.ForceUpdateCanvases();
    var title=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal title");Assert.That(title.preferredWidth,Is.LessThanOrEqualTo(title.rectTransform.rect.width+1));Assert.That(title.preferredHeight,Is.LessThanOrEqualTo(title.rectTransform.rect.height+1));
    var hint=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town growth hint");Assert.That(hint.preferredWidth,Is.LessThanOrEqualTo(hint.rectTransform.rect.width+1));Assert.That(hint.preferredHeight,Is.LessThanOrEqualTo(hint.rectTransform.rect.height+1));
   }
   Flow.Game.State.completedQuestIds.Clear();Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));Flow.Game.State.activeQuestId="";
   foreach(bool claimed in new[]{false,true})foreach(int talks in new[]{0,1})foreach(int walks in new[]{0,1})foreach(int merges in new[]{0,3})
   {
    var d=Flow.Daily.Data;d.Claimed=claimed;d.Talks=talks;d.Walks=walks;d.Merges=merges;Flow.ShowHome();Canvas.ForceUpdateCanvases();
    var title=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Next goal title");Assert.That(title.preferredWidth,Is.LessThanOrEqualTo(title.rectTransform.rect.width+1));Assert.That(title.preferredHeight,Is.LessThanOrEqualTo(title.rectTransform.rect.height+1));
    var hint=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town growth hint");Assert.That(hint.preferredWidth,Is.LessThanOrEqualTo(hint.rectTransform.rect.width+1));Assert.That(hint.preferredHeight,Is.LessThanOrEqualTo(hint.rectTransform.rect.height+1));
   }
   LogAssert.NoUnexpectedReceived();
  }
 }
}
