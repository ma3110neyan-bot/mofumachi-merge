using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
namespace Mofumachi.Tests
{
    public sealed class NorthMasterReviewTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator QuestCottageDoorStaysReadableDuringTravelAndWaitsForTheSecondTap()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);
                // ShowHome preserves the previous visit's feet. Each level
                // must start in central, rather than at the previous quest doorway.
                life.World.FastTravel(TownDistrictId.Central);
                Assert.That(life.World.Model.Hero.District,Is.EqualTo(TownDistrictId.Central),"Each level starts a fresh quest journey from central");
                life.World.LookAt(TownDistrictId.North);Canvas.ForceUpdateCanvases();
                var facility=life.GetComponent<TownFacilityView>();
                var hit=life.transform.Find("Facility entrance quest");
                var sign=hit.Find("Facility sign quest");
                Assert.That(hit.gameObject.activeInHierarchy,Is.True,"The cottage door remains a real destination even when its caption withdraws");
                Assert.That(sign.gameObject.activeSelf,Is.False,"A distant quest facade stays visible");
                void TapDoor()=>ExecuteEvents.Execute(hit.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                TapDoor();Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Walking));
                for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)
                {life.AdvanceFrame(1f/30);Assert.That(life.IsSpeaking,Is.False,"Unsolicited dialogue must not hide the journey or its arrival");}
                Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                for(int i=0;i<360;i++){life.AdvanceFrame(1f/30);Assert.That(life.IsSpeaking,Is.False);}
                Assert.That(sign.gameObject.activeInHierarchy,Is.True);Assert.That(Flow.IsModalOpen,Is.False);
                Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
                TapDoor();Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
                for(int i=0;i<30&&Flow.CurrentScreen==ScreenId.Home;i++)life.AdvanceFrame(1f/30);
                Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest),"The physical cottage opens the existing request screen");
                Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NorthGroundFollowsTheWorldAndBothCottageSideWalksRemainUsable()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.FastTravel(TownDistrictId.North);Canvas.ForceUpdateCanvases();
            var ground=Flow.GetComponentInChildren<TownNorthGroundGraphic>();Assert.That(ground,Is.Not.Null);Assert.That(ground.raycastTarget,Is.False);
            var court=ground.GetComponentInChildren<TownNorthCourtGraphic>();Assert.That(court,Is.Not.Null);Assert.That(court.raycastTarget,Is.False);
            var mosaic=Resources.Load<Texture2D>(TownNorthCourtGraphic.Resource);Assert.That(mosaic,Is.Not.Null);Assert.That(court.mainTexture,Is.SameAs(mosaic));
            Assert.That(mosaic.width,Is.LessThanOrEqualTo(512));Assert.That(mosaic.height,Is.LessThanOrEqualTo(256));Assert.That(mosaic.isReadable,Is.False);
            var texture=Resources.Load<Texture2D>(TownNorthMasterLayout.Resource);Assert.That(texture,Is.Not.Null);Assert.That(ground.mainTexture,Is.SameAs(texture));
            Assert.That(texture,Is.SameAs(Resources.Load<Texture2D>(TownWestMasterLayout.Resource)),"The shared ground must not allocate a second texture");Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.width,Is.LessThanOrEqualTo(1024));Assert.That(texture.height,Is.LessThanOrEqualTo(2048));
            void AssertProjection()
            {
                Canvas.ForceUpdateCanvases();var mesh=ground.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(4));
                Assert.That(Vector2.Distance(mesh.vertices[0],life.World.Local(new TownPoint(TownNorthMasterLayout.Left,TownNorthMasterLayout.Bottom),ground.rectTransform)),Is.LessThan(.01f));
                var courtMesh=court.canvasRenderer.GetMesh();Assert.That(courtMesh.vertexCount,Is.EqualTo(4));
                Assert.That(Vector2.Distance(courtMesh.vertices[0],life.World.Local(new TownPoint(-.21f,1.765f),court.rectTransform)),Is.LessThan(.01f),"The mosaic must remain on its world paving after Pan/Restore, never float with the HUD");
                foreach(var actor in life.Actors)
                    Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(actor.WorldPosition)),Is.LessThan(.01f),"Camera/restore refresh must project every actor immediately, before another simulation frame: "+actor.CharacterId);
            }
            AssertProjection();var positions=life.World.Model.Residents.Select(r=>r.Position).ToArray();
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            life.World.Pan(new Vector2(36,-20));AssertProjection();life.RefreshWorldPresentation();
            Assert.That(life.World.Model.Residents.Select(r=>r.Position),Is.EqualTo(positions),"Presentation refresh must not move residents to improve a screenshot");
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));
            var snapshot=life.World.Capture();life.World.FastTravel(TownDistrictId.Central);life.World.Restore(snapshot);AssertProjection();
            foreach(var stop in new[]{new TownPoint(-.50f,1.78f),new TownPoint(.50f,1.78f),new TownPoint(-.42f,2.60f),new TownPoint(.42f,2.60f),TownWorldMap.Place("bench-left"),TownWorldMap.Place("home-front"),TownWorldMap.Place("quest"),TownWorldMap.Place("square")})
            {
                Assert.That(life.World.GuidePoint(stop),Is.True);
                for(int i=0;i<3000&&life.World.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);}
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,stop),Is.LessThan(.002f));
            }
            // Exercise crossing traffic and a genuinely occupied arrival point,
            // not just isolated routes or a staged photograph of separated NPCs.
            var model=life.World.Model;model.Restore(model.Hero.Id,new TownPoint(0,1.80f));
            var friends=model.Residents.Where(a=>!a.Controlled).Take(3).ToArray();
            model.Restore(friends[0].Id,new TownPoint(-.22f,2.02f));model.Restore(friends[1].Id,new TownPoint(.22f,1.95f));model.Restore(friends[2].Id,TownWorldMap.Place("quest"));
            Assert.That(model.TryGuide(friends[0].Id,"home-front"),Is.True);Assert.That(model.TryGuide(friends[1].Id,"bench-left"),Is.True);Assert.That(model.TryGuide(model.Hero.Id,"quest"),Is.True);
            for(int i=0;i<3000&&model.Hero.IsWalking;i++)
            {
                life.AdvanceFrame(1f/30);
                for(int a=0;a<model.Residents.Count;a++)for(int b=a+1;b<model.Residents.Count;b++)
                    Assert.That(TownWalkableArea.Distance(model.Residents[a].Position,model.Residents[b].Position),Is.GreaterThan(.066f));
            }
            Assert.That(model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(model.Hero.Position,TownWorldMap.Place("quest")),Is.LessThan(.002f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NorthGardensReplaceTheirStageAndLeaveTheBoardAndSeatClearWithoutSaving()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.LookAt(TownDistrictId.North);Canvas.ForceUpdateCanvases();
            var scene=life.GetComponent<TownSceneryView>();var growth=Flow.GetComponentInChildren<TownGrowthView>();
            string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            foreach(int level in new[]{1,3,6})
            {
                scene.SetTownLevel(level);growth.Apply(level);Canvas.ForceUpdateCanvases();
                foreach(float side in new[]{-1f,1f})
                {
                    var stages=scene.Pieces.Where(p=>TownNorthMasterLayout.Dressing.Contains(p.Placement)&&Mathf.Abs(p.Placement.Feet.X-side*.43f)<.001f&&Mathf.Abs(p.Placement.Feet.Y-1.72f)<.001f).ToArray();
                    Assert.That(stages.Count(p=>p.Rect.gameObject.activeSelf),Is.EqualTo(1));
                    Assert.That(stages.Single(p=>p.Rect.gameObject.activeSelf).Placement.Level,Is.EqualTo(level==1?0:level));
                }
                Assert.That(scene.Pieces.Any(p=>p.Rect.gameObject.activeSelf&&p.Placement.Kind==TownSceneryKind.NoticeBoard&&p.Placement.District==TownDistrictId.North),Is.True);
                var parcels=scene.Pieces.Where(p=>p.Rect.gameObject.activeSelf&&p.Placement.Kind==TownSceneryKind.DeliveryParcels).ToArray();
                Assert.That(parcels.Length,Is.EqualTo(1));Assert.That(parcels[0].Placement.Level,Is.EqualTo(level>=3?3:0));
                var parcelArt=parcels[0].Rect.GetComponent<TownVillageDetailGraphic>().mainTexture as Texture2D;
                Assert.That(parcelArt,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/TownDeliveryParcels")));Assert.That(parcelArt.width,Is.LessThanOrEqualTo(256));Assert.That(parcelArt.isReadable,Is.False);
                Assert.That(scene.Pieces.Any(p=>p.Rect.gameObject.activeSelf&&p.Placement.Kind==TownSceneryKind.DeliveryBunting),Is.EqualTo(level>=3));
                Assert.That(scene.Pieces.Any(p=>p.Rect.gameObject.activeSelf&&p.Placement.Kind==TownSceneryKind.FlowerArch),Is.EqualTo(level>=6));
                var plaque=scene.Pieces.Single(p=>p.Placement.Kind==TownSceneryKind.QuestCottage).Rect.Find("Cottage request plaque").GetComponent<UnityEngine.UI.Text>();
                Assert.That(plaque.font,Is.Not.Null);Assert.That(plaque.text,Is.EqualTo("おねがい"));Assert.That(plaque.raycastTarget,Is.False);
                Assert.That(scene.Pieces.Any(p=>p.Rect.gameObject.activeSelf&&p.Placement.Kind==TownSceneryKind.FlowerBorder&&p.Placement.Feet.Equals(new TownPoint(-.45f,2.28f))),Is.False,"The board must not acquire an opaque flower overlay");
                var seat=scene.Pieces.Single(p=>TownNorthMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Kind==TownSceneryKind.ParkBench).Rect;
                var bank=scene.Pieces.Single(p=>TownNorthMasterLayout.Dressing.Contains(p.Placement)&&p.Placement.Feet.Equals(new TownPoint(.48f,1.24f))).Rect;
                Assert.That(bank.anchoredPosition.y+bank.rect.height,Is.LessThan(seat.anchoredPosition.y));
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
    }
}
