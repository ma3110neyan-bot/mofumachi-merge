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
        [UnityTest] public IEnumerator OneSharedGroundAndFiveLandmarksReplaceTheOpaquePainting()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            var ground=Flow.GetComponentsInChildren<TownGroundGraphic>().Single();
            Assert.That(ground.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownWorldSurface.AtlasResource)));Assert.That(ground.color.a,Is.EqualTo(1));Assert.That(ground.raycastTarget,Is.False);
            var boundary=Flow.GetComponentsInChildren<TownBoundaryGardenGraphic>().Single();
            Assert.That(boundary.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownSceneryLayout.NatureResource)));Assert.That(boundary.raycastTarget,Is.False);
            var curve=Flow.GetComponentsInChildren<TownBoundaryCurveGraphic>().Single();
            Assert.That(curve.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownWestMasterLayout.Resource)));Assert.That(curve.raycastTarget,Is.False);
            Canvas.ForceUpdateCanvases();Assert.That(curve.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(272),"One bounded shared-texture curve batch, without per-flower objects");
            var scenery=life.GetComponent<TownSceneryView>();var landmarks=scenery.Pieces.Where(p=>p.Placement.Kind<=TownSceneryKind.Gazebo).ToArray();
            var villageAtlas=Resources.Load<Texture2D>(TownVillageDressing.AtlasResource);
            Assert.That(villageAtlas,Is.Not.Null,"The composed village artwork must be imported as a runtime resource");
            var details=life.GetComponentsInChildren<TownVillageDetailGraphic>(true);Assert.That(details.Length,Is.GreaterThan(0));
            int checkedDetails=0;
            foreach(var piece in scenery.Pieces)
            {
                var detail=piece.Rect.GetComponent<TownVillageDetailGraphic>();if(detail==null)continue;
                // Approved benches, lamps, flowers and parcels share their
                // respective original atlases, rather than one village sheet.
                var source=TownVillageDressing.SpriteSource(piece.Placement.Kind);
                var atlas=Resources.Load<Texture2D>(source.Resource);Assert.That(atlas,Is.Not.Null,source.Resource);
                Assert.That(detail.mainTexture,Is.SameAs(atlas),piece.Placement.Kind.ToString());Assert.That(detail.raycastTarget,Is.False);checkedDetails++;
            }
            Assert.That(checkedDetails,Is.EqualTo(details.Length),"Every composed detail must retain its source-bound shared atlas");
            Assert.That(landmarks.Select(p=>p.Placement.Kind),Is.EquivalentTo(new[]{TownSceneryKind.Cafe,TownSceneryKind.Florist,TownSceneryKind.QuestCottage,TownSceneryKind.Gazebo}));
            foreach(var landmark in landmarks)
            {var graphic=landmark.Rect.GetComponent<TownLandmarkGraphic>();Assert.That(graphic.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownSceneryLayout.LandmarkResource)));Assert.That(graphic.raycastTarget,Is.False);}
            var fountain=life.GetComponentsInChildren<TownFountainGraphic>().Single();Assert.That(fountain.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownFountainGraphic.Resource)));Assert.That(fountain.raycastTarget,Is.False);
            Assert.That(Flow.GetComponentsInChildren<RawImage>(true).Any(i=>i.name=="Seamless town map"||i.texture==Resources.Load<Texture2D>(TownWorldMap.Resource)),Is.False,"The old map supplies only the small fountain silhouette");
            Assert.That(Flow.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Continuous background")),Is.False);
            foreach(var district in TownDistrictLayout.All)
            {
                life.World.FastTravel(district);life.AdvanceFrame(1f/30);Assert.That(life.World.VisibleBackgroundCount,Is.EqualTo(1));Assert.That(life.World.Model.Hero.District,Is.EqualTo(district));Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);
                if(district!=TownDistrictId.Central)Assert.That(landmarks.Single(p=>p.Placement.District==district).Rect.gameObject.activeInHierarchy,Is.True,"The destination landmark must actually be drawn");
            }
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
            foreach(int height in new[]{592,808,1280})
            {
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(360,height);Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));Canvas.ForceUpdateCanvases();life.World.Refresh();Canvas.ForceUpdateCanvases();
                var mesh=ground.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.GreaterThan(0));
                var vertices=mesh.vertices.Select(v=>life.transform.InverseTransformPoint(ground.transform.TransformPoint(v))).ToArray();var viewport=((RectTransform)life.transform).rect;
                Assert.That(vertices.Min(v=>v.x),Is.LessThanOrEqualTo(viewport.xMin+.01f));Assert.That(vertices.Max(v=>v.x),Is.GreaterThanOrEqualTo(viewport.xMax-.01f));Assert.That(vertices.Min(v=>v.y),Is.LessThanOrEqualTo(viewport.yMin+.01f));Assert.That(vertices.Max(v=>v.y),Is.GreaterThanOrEqualTo(viewport.yMax-.01f));
                var fieldCenter=RectTransformUtility.WorldToScreenPoint(null,life.transform.TransformPoint(viewport.center));var source=life.World.Source(fieldCenter,null);
                Assert.That(source.X,Is.EqualTo(life.World.Camera.Center.X).Within(.0001f));Assert.That(source.Y,Is.EqualTo(life.World.Camera.Center.Y).Within(.0001f),"Projection must use the playable field center after safe-area resizing");
            }
            var guideSpot=TownWorldMap.Place("west.meeting");life.World.Model.Restore(life.World.Model.Hero.Id,guideSpot);life.World.ReturnToHero();
            Assert.That(TownWorldLayout.Distance(life.World.Model.Hero.Position,guideSpot),Is.LessThan(.001f));
            Assert.That(life.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("Painted road direction")),Is.Empty,"The removed floating captions must not be recreated, even as inactive objects");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator QuestDoorWaitsForATapAfterArrivalAndReturnsToTheSameTownPosition()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();
            var before=Flow.Game.State.Clone();int writes=Store.Writes;var initial=life.World.Model.Hero.Position;
            Assert.That(facility.RequestEnter("quest"),Is.True);Assert.That(life.World.Model.Hero.Position,Is.EqualTo(initial));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            for(int i=0;i<2400&&facility.Entry.Phase==TownEntryPhase.Walking;i++)life.AdvanceFrame(1f/30);
            Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Assert.That(Store.Writes,Is.EqualTo(writes));
            for(int i=0;i<90;i++)life.AdvanceFrame(1f/30);
            Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            Assert.That(facility.RequestEnter("quest"),Is.True);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
            Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name=="Enter facility"),Is.False,"The doorway itself stays the deliberate entry action");
            var position=life.World.Model.Hero.Position;Assert.That(TownWorldLayout.Distance(position,TownWorldMap.Place("quest")),Is.LessThan(.032f));
            for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));Assert.That(Store.Writes,Is.EqualTo(writes+1),"Preserve the existing navigation save");
            var after=Flow.Game.State.Clone();Assert.That(System.DateTimeOffset.TryParse(after.lastSaveTime,out _),Is.True);
            Assert.That(StateCodec.Encode(Store.Load().State),Is.EqualTo(StateCodec.Encode(after)),"Disk and live state must agree after entry");
            after.lastSaveTime=before.lastSaveTime;Assert.That(StateCodec.Encode(after),Is.EqualTo(StateCodec.Encode(before)),"Entering the cabin must not change progress or rewards");
            Flow.GoBack();Assert.That(TownWorldLayout.Distance(Flow.GetComponentInChildren<TownLifeView>().World.Model.Hero.Position,position),Is.LessThan(.001f));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EastShopIsEnteredThroughItsDoorWithoutPurchaseOrSaveMutation()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            Assert.That(facility.RequestEnter("shop"),Is.True);
            for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)life.AdvanceFrame(1f/30);
            Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
            for(int i=0;i<90;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.IsModalOpen,Is.False);Assert.That(facility.Entry.OpenAmount,Is.Zero);
            Assert.That(facility.RequestEnter("shop"),Is.True);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
            Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name=="Enter facility"),Is.False,"The doorway itself stays the deliberate entry action");
            for(int i=0;i<30&&!Flow.IsModalOpen;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.IsModalOpen,Is.True);Assert.That(life.World.Model.Hero.Region,Is.EqualTo(TownRegionId.East));
            Assert.That(TownWorldLayout.Distance(life.World.Model.Hero.Position,TownWorldMap.Place("shop")),Is.LessThan(.032f));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Flow.CloseShopPreview();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CafeIsARealWalkingDestinationWithATappedDoorAndAMeetingInside()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var facility=life.GetComponent<TownFacilityView>();
            string before=StateCodec.Encode(Flow.Game.State);int talks=Flow.Daily.Data.Talks;
            Assert.That(facility.RequestEnter("cafe"),Is.True);
            for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)life.AdvanceFrame(1f/30);
            Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Assert.That(Flow.IsModalOpen,Is.False);
            Assert.That(life.World.Model.Hero.District,Is.EqualTo(TownDistrictId.West));Assert.That(Flow.Daily.Data.Talks,Is.EqualTo(talks));
            Assert.That(facility.RequestEnter("cafe"),Is.True);
            for(int i=0;i<30&&!Flow.IsModalOpen;i++)life.AdvanceFrame(1f/30);
            Assert.That(Flow.IsModalOpen,Is.True);Assert.That(Flow.Daily.Data.Talks,Is.EqualTo(talks),"Opening a cafe cannot count as talking");
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            LogAssert.NoUnexpectedReceived();
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
        [UnityTest] public IEnumerator HomeMenuQuestNavigationIsARealNorthernDestination()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();int writes=Store.Writes;
            Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name=="Nav Quest"),Is.False);
            TownMenuButton("Nav Quest").onClick.Invoke();Assert.That(Flow.IsModalOpen,Is.False);
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(life.World.Model.Hero.PlaceId,Is.EqualTo("quest"));
            for(int i=0;i<2400&&life.Regions.HasHeroJourney;i++)life.AdvanceFrame(1f/30);
            Assert.That(life.World.Model.Hero.Region,Is.EqualTo(TownRegionId.Central));Assert.That(life.World.Model.Hero.District,Is.EqualTo(TownDistrictId.North));Assert.That(life.NearbyFacility,Is.EqualTo("quest"));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
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
