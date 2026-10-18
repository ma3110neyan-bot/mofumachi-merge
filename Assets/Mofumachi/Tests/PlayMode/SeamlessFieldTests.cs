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
    public sealed class SeamlessFieldTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator OneOpaqueAuthoredMapReplacesAllThreeFadedPaintings()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            var map=Flow.GetComponentsInChildren<RawImage>().Single(i=>i.name=="Seamless town map");
            Assert.That(map.texture,Is.SameAs(Resources.Load<Texture2D>(TownWorldMap.Resource)));Assert.That(map.color.a,Is.EqualTo(1));Assert.That(map.raycastTarget,Is.False);
            Assert.That(Flow.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Continuous background")),Is.False);
            foreach(var region in new[]{TownRegionId.West,TownRegionId.Central,TownRegionId.East})
            {life.World.FastTravel(region);life.AdvanceFrame(1f/30);Assert.That(life.World.VisibleBackgroundCount,Is.EqualTo(1));Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);}
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
            foreach(int height in new[]{592,808,1280})
            {
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(360,height);Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));Canvas.ForceUpdateCanvases();life.World.Refresh();Canvas.ForceUpdateCanvases();
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(Flow.TownBackground.rectTransform,map.transform);var viewport=Flow.TownBackground.rectTransform.rect;
                Assert.That(bounds.min.x,Is.LessThanOrEqualTo(viewport.xMin+.01f));Assert.That(bounds.max.x,Is.GreaterThanOrEqualTo(viewport.xMax-.01f));Assert.That(bounds.min.y,Is.LessThanOrEqualTo(viewport.yMin+.01f));Assert.That(bounds.max.y,Is.GreaterThanOrEqualTo(viewport.yMax-.01f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuestDoorWalksThenOpensAndReturnsToTheSameTownPosition()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();
            var before=Flow.Game.State.Clone();int writes=Store.Writes;var initial=life.World.Model.Hero.Position;
            Assert.That(facility.RequestEnter("quest"),Is.True);Assert.That(life.World.Model.Hero.Position,Is.EqualTo(initial));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            for(int i=0;i<2400&&facility.Entry.Phase==TownEntryPhase.Walking;i++)life.AdvanceFrame(1f/30);
            Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));Assert.That(Store.Writes,Is.EqualTo(writes));
            var position=life.World.Model.Hero.Position;Assert.That(TownWorldLayout.Distance(position,TownWorldMap.Place("quest")),Is.LessThan(.032f));
            for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));Assert.That(Store.Writes,Is.EqualTo(writes+1),"Preserve the existing navigation save");
            var after=Flow.Game.State.Clone();Assert.That(System.DateTimeOffset.TryParse(after.lastSaveTime,out _),Is.True);
            Assert.That(StateCodec.Encode(Store.Load().State),Is.EqualTo(StateCodec.Encode(after)),"Disk and live state must agree after entry");
            after.lastSaveTime=before.lastSaveTime;Assert.That(StateCodec.Encode(after),Is.EqualTo(StateCodec.Encode(before)),"Entering the cabin must not change progress or rewards");
            Flow.GoBack();Assert.That(TownWorldLayout.Distance(Flow.GetComponentInChildren<TownLifeView>().World.Model.Hero.Position,position),Is.LessThan(.001f));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SouthShopIsEnteredThroughItsDoorWithoutPurchaseOrSaveMutation()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            Assert.That(facility.RequestEnter("shop"),Is.True);
            for(int i=0;i<3000&&!Flow.IsModalOpen;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.IsModalOpen,Is.True);Assert.That(life.World.Model.Hero.Region,Is.EqualTo(TownRegionId.East));
            Assert.That(TownWorldLayout.Distance(life.World.Model.Hero.Position,TownWorldMap.Place("shop")),Is.LessThan(.032f));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Flow.CloseShopPreview();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RetargetPauseAndFastTravelCancelPendingFacilityEntry()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();
            facility.RequestEnter("quest");life.World.GuidePoint(TownWorldMap.Place("square"));Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
            facility.RequestEnter("shop");life.World.FastTravel(TownRegionId.Central);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
            facility.RequestEnter("quest");life.World.Pan(new Vector2(80,0));Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Idle));
            life.World.Model.Restore(life.World.Model.Hero.Id,TownWorldMap.Place("quest"));life.World.ReturnToHero();facility.RequestEnter("quest");life.AdvanceFrame(1f/30);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
            life.World.Pan(new Vector2(-80,0));Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Idle));for(int i=0;i<30;i++)life.AdvanceFrame(1f/30);Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            facility.RequestEnter("quest");Flow.SendMessage("OnApplicationPause",true);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Idle));Flow.SendMessage("OnApplicationPause",false);
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.IsModalOpen,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HomeQuestNavigationIsARealNorthernDestination()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();int writes=Store.Writes;
            Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Nav Quest").onClick.Invoke();
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(life.World.Model.Hero.PlaceId,Is.EqualTo("quest"));
            for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            Assert.That(life.World.Model.Hero.Region,Is.EqualTo(TownRegionId.West));Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CandidateCardsExposeEightSecondPreviewAndRealSceneListening()
        {
            yield return EnterHome();Flow.ShowSettings();int writes=Store.Writes;
            foreach(var song in MusicCatalog.Candidates)
            {
                Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Preview music "+song.Id).GetComponentInChildren<Text>().text,Does.Contain("8秒"));
                Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Listen in scene "+song.Id),Is.Not.Null);
                Assert.That(Resources.Load<AudioClip>("Mofumachi/Audio/"+song.Resource).length,Is.GreaterThan(30));
            }
            Flow.PreviewMusic("title-garden");Assert.That(AudioManager.Instance.PreviewMusicId,Is.EqualTo("title-garden"));
            yield return new WaitForSecondsRealtime(8.2f);Assert.That(AudioManager.Instance.PreviewMusicId,Is.Null);Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.GoBack();Assert.That(AudioManager.Instance.CurrentRole,Is.EqualTo(MusicRole.Town));Flow.ShowMerge();Assert.That(AudioManager.Instance.CurrentRole,Is.EqualTo(MusicRole.Merge));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HomeAndQuestStoryTextFitWithoutDroppingTheLastLine()
        {
            yield return EnterHome();Canvas.ForceUpdateCanvases();var hint=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town growth hint");
            Assert.That(hint.preferredHeight,Is.LessThanOrEqualTo(hint.rectTransform.rect.height+1));Flow.ShowQuest();Canvas.ForceUpdateCanvases();
            var story=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Quest instruction");Assert.That(story.text,Is.EqualTo(QuestStoryPresentation.For("tea-01").Before));
            Assert.That(story.preferredHeight,Is.LessThanOrEqualTo(story.rectTransform.rect.height+1));LogAssert.NoUnexpectedReceived();
        }
    }
}
