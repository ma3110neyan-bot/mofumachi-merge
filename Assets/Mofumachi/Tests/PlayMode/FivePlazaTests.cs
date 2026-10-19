using System;
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
    public sealed class FivePlazaTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator FiveDistrictsRenderIndependentLandmarksFromOneSharedFloor()
        {
            yield return EnterHome();var life=FrameTownPortrait();var scene=life.GetComponent<TownSceneryView>();
            Assert.That(scene,Is.Not.Null);Assert.That(Flow.GetComponentsInChildren<TownGroundGraphic>().Count(),Is.EqualTo(1));
            Assert.That(Flow.GetComponentsInChildren<RawImage>().Any(i=>i.name=="Seamless town map"),Is.False,"The rejected giant painting cannot be the world");
            foreach(var district in TownDistrictLayout.All)
            {
                life.World.FastTravel(district);life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();
                Assert.That(life.World.Model.Hero.District,Is.EqualTo(district));
                var landmark=TownSceneryLayout.Landmarks.Single(p=>p.District==district);
                var feet=life.World.Local(landmark.Feet);var field=((RectTransform)life.transform).rect;float scale=field.width/TownWorldLayout.ViewWidth;
                Assert.That(feet.x-landmark.Width*scale/2,Is.GreaterThanOrEqualTo(field.xMin-.1f),district.ToString());
                Assert.That(feet.x+landmark.Width*scale/2,Is.LessThanOrEqualTo(field.xMax+.1f),district.ToString());
                Assert.That(feet.y+landmark.Height*scale,Is.LessThanOrEqualTo(field.yMax+.1f),district+" roof must fit in the real field");
                if(district!=TownDistrictId.Central)Assert.That(scene.Pieces.Single(p=>p.Placement.Kind==landmark.Kind).Rect.gameObject.activeInHierarchy,Is.True);
            }
            foreach(var key in new[]{TownSceneryLayout.LandmarkResource,TownSceneryLayout.NatureResource,TownWorldSurface.AtlasResource})
            {var art=Resources.Load<Texture2D>(key);Assert.That(art,Is.Not.Null,key);Assert.That(art.isReadable,Is.False,key);Assert.That(art.width,Is.LessThanOrEqualTo(1024),key);}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator PointerProjectionUsesTheFieldAfterSafeAreaResizeAndFiveWayTravel()
        {
            yield return EnterHome();var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
            foreach(int height in new[]{592,740,808})
            {
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(360,height);Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));Canvas.ForceUpdateCanvases();
                var life=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var district in TownDistrictLayout.All)
                {
                    life.World.FastTravel(district);life.World.Refresh();var p=life.World.Model.Hero.Position;
                    var screen=RectTransformUtility.WorldToScreenPoint(null,life.transform.TransformPoint(life.World.Local(p)));
                    Assert.That(TownWorldLayout.Distance(p,life.World.Source(screen,null)),Is.LessThan(.0001f),district+" at "+height);
                    var landmark=TownSceneryLayout.Landmarks.Single(l=>l.District==district);var top=life.World.Local(new TownPoint(landmark.Feet.X,landmark.Feet.Y+landmark.Height));
                    Assert.That(top.y,Is.LessThanOrEqualTo(((RectTransform)life.transform).rect.yMax+.1f),district+" roof at "+height);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator AllFourExitsWalkContinuouslyAndCompleteOnlyOnArrival()
        {
            yield return EnterHome();var life=FrameTownPortrait();string save=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            var hero=life.World.Model.Hero;int arrivals=0,walkedArrivals=0;string arrivedPlace=null;bool arrivedWhileWalking=false;
            life.World.Arrived+=(id,place,walked)=>
            {
                if(id!=hero.Id)return;
                arrivals++;if(walked)walkedArrivals++;arrivedPlace=place;arrivedWhileWalking|=hero.IsWalking;
            };
            Assert.That(Flow.Daily.Data.Walks,Is.Zero,"The first trip must exercise an incomplete daily walk goal");
            foreach(var district in new[]{TownDistrictId.North,TownDistrictId.South,TownDistrictId.West,TownDistrictId.East})
            {
                int walks=Flow.Daily.Data.Walks;
                life.World.FastTravel(TownDistrictId.Central);var start=life.World.Model.Hero.Position;
                int arrivalsBefore=arrivals,walkedBefore=walkedArrivals;
                Assert.That(life.World.GuideDistrict(district),Is.True);Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(walks));Assert.That(life.World.Model.Hero.Position,Is.EqualTo(start));
                Assert.That(hero.IsWalking,Is.True);Assert.That(arrivals,Is.EqualTo(arrivalsBefore),district+" must not arrive when guidance starts");
                var previous=start;var camera=life.World.Camera.Center;
                for(int frame=0;frame<5000&&life.World.Model.Hero.IsWalking;frame++)
                {
                    life.AdvanceFrame(1f/30);var p=life.World.Model.Hero.Position;
                    Assert.That(TownWalkableArea.Distance(p,previous),Is.LessThanOrEqualTo(.0041f));Assert.That(TownWorldLayout.Distance(camera,life.World.Camera.Center),Is.LessThan(.05f));
                    Assert.That(life.Regions.IsHeroTransitioning,Is.False);previous=p;camera=life.World.Camera.Center;
                    if(hero.IsWalking)
                    {
                        Assert.That(arrivals,Is.EqualTo(arrivalsBefore),district+" must not announce arrival while still walking");
                        Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(walks),district+" must not advance the daily goal before arrival");
                    }
                }
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(life.World.Model.Hero.District,Is.EqualTo(district));Assert.That(hero.Position,Is.EqualTo(hero.Target));
                Assert.That(arrivals,Is.EqualTo(arrivalsBefore+1),district+" must emit exactly one hero arrival");
                Assert.That(walkedArrivals,Is.EqualTo(walkedBefore+1),district+" must count exactly one physical crossing");
                Assert.That(arrivedPlace,Is.EqualTo(TownDistrictLayout.EntranceId(district)));Assert.That(arrivedWhileWalking,Is.False);
                Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(1),district+" completes the once-per-day goal without counting extra trips");
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(save));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator NorthAndSouthSettingsReturnPreservesNegativeAndLargeCoordinates()
        {
            yield return EnterHome();
            foreach(var district in new[]{TownDistrictId.North,TownDistrictId.South})
            {
                var life=FrameTownPortrait();life.World.FastTravel(district);var point=life.World.Model.Hero.Position;var camera=life.World.Camera.Center;
                Flow.ShowSettings();Flow.GoBack();life=Flow.GetComponentInChildren<TownLifeView>();
                Assert.That(TownWorldLayout.Distance(life.World.Model.Hero.Position,point),Is.LessThan(.0001f));Assert.That(life.World.Model.Hero.District,Is.EqualTo(district));
                Assert.That(TownWorldLayout.Distance(life.World.Camera.Center,camera),Is.LessThan(.0001f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LandmarkOcclusionUsesFeetAndDoesNotCoverResidentsWithTheWholePainting()
        {
            yield return EnterHome();var life=FrameTownPortrait();var hero=life.Actors.Single(a=>a.Controlled);var fountain=life.GetComponentInChildren<TownFountainGraphic>();
            life.World.Model.Restore(hero.CharacterId,new TownPoint(0,.53f));life.World.ReturnToHero();life.AdvanceFrame(1f/30);
            Assert.That(hero.transform.GetSiblingIndex(),Is.LessThan(fountain.transform.GetSiblingIndex()));
            life.World.Model.Restore(hero.CharacterId,new TownPoint(0,.06f));life.World.ReturnToHero();life.AdvanceFrame(1f/30);
            Assert.That(hero.transform.GetSiblingIndex(),Is.GreaterThan(fountain.transform.GetSiblingIndex()));LogAssert.NoUnexpectedReceived();
        }
    }
}
