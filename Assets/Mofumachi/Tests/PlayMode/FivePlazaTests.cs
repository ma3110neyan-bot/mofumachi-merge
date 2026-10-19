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
            int walks=Flow.Daily.Data.Walks;
            foreach(var district in new[]{TownDistrictId.North,TownDistrictId.South,TownDistrictId.West,TownDistrictId.East})
            {
                life.World.FastTravel(TownDistrictId.Central);var start=life.World.Model.Hero.Position;
                Assert.That(life.World.GuideDistrict(district),Is.True);Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(walks));Assert.That(life.World.Model.Hero.Position,Is.EqualTo(start));
                var previous=start;var camera=life.World.Camera.Center;
                for(int frame=0;frame<5000&&life.World.Model.Hero.IsWalking;frame++)
                {
                    life.AdvanceFrame(1f/30);var p=life.World.Model.Hero.Position;
                    Assert.That(TownWalkableArea.Distance(p,previous),Is.LessThanOrEqualTo(.0041f));Assert.That(TownWorldLayout.Distance(camera,life.World.Camera.Center),Is.LessThan(.05f));
                    Assert.That(life.Regions.IsHeroTransitioning,Is.False);previous=p;camera=life.World.Camera.Center;
                }
                Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(life.World.Model.Hero.District,Is.EqualTo(district));Assert.That(Flow.Daily.Data.Walks,Is.EqualTo(++walks));
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
