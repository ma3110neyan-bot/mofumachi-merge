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
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuestDoorWalksThenOpensAndReturnsToTheSameTownPosition()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();
            string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;var initial=life.World.Model.Hero.Position;
            Assert.That(facility.RequestEnter("quest"),Is.True);Assert.That(life.World.Model.Hero.Position,Is.EqualTo(initial));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            for(int i=0;i<2400&&facility.Entry.Phase==TownEntryPhase.Walking;i++)life.AdvanceFrame(1f/30);
            Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));Assert.That(Store.Writes,Is.EqualTo(writes));
            var position=life.World.Model.Hero.Position;Assert.That(TownWorldLayout.Distance(position,TownWorldMap.Place("quest")),Is.LessThan(.032f));
            for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
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
