using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class SouthMasterReviewTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator GardenGroundUsesTheAcceptedPaintingAndFollowsPanAndRestore()
        {
            yield return EnterHome();var life=FrameTownPortrait(640);life.World.LookAt(TownDistrictId.South);Canvas.ForceUpdateCanvases();
            var ground=Flow.GetComponentInChildren<TownSouthGroundGraphic>();Assert.That(ground,Is.Not.Null);Assert.That(ground.raycastTarget,Is.False);
            var art=Resources.Load<Texture2D>(TownSouthMasterLayout.Resource);Assert.That(art,Is.Not.Null);Assert.That(ground.mainTexture,Is.SameAs(art));Assert.That(art.isReadable,Is.False);
            var entry=ground.GetComponentInChildren<TownSouthEntryBlendGraphic>();Assert.That(entry,Is.Not.Null);Assert.That(entry.raycastTarget,Is.False);
            Assert.That(entry.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownCentralMasterLayout.Resource)),"Reuse the accepted Central edge without a duplicate painting");
            void Check()
            {
                Canvas.ForceUpdateCanvases();var mesh=ground.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(16));Assert.That(mesh.colors32[0].a,Is.Zero);Assert.That(mesh.colors32[5].a,Is.EqualTo(255));Assert.That(Vector2.Distance(mesh.vertices[0],life.World.Local(new TownPoint(TownSouthMasterLayout.Left,TownSouthMasterLayout.Bottom),ground.rectTransform)),Is.LessThan(.01f));
                var join=entry.canvasRenderer.GetMesh();Assert.That(join.vertexCount,Is.EqualTo(16));
                for(int col=0;col<4;col++)
                {
                    Assert.That(join.colors32[col].a,Is.Zero);Assert.That(join.colors32[12+col].a,Is.EqualTo(255),"The cut itself must be fully textured, not expose the flat underlying lawn");
                    float x=new[]{-.60f,-.55f,.55f,.60f}[col];var top=new TownPoint(x,-1);var uv=TownCentralMasterLayout.TexturePoint(top);
                    Assert.That(Vector2.Distance(join.vertices[12+col],life.World.Local(top,entry.rectTransform)),Is.LessThan(.01f));
                    Assert.That(Vector2.Distance(join.uv[12+col],new Vector2(uv.X,uv.Y)),Is.LessThan(.00001f));
                }
            }
            string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;var positions=life.World.Model.Residents.Select(a=>a.Position).ToArray();
            Check();life.World.Pan(new Vector2(30,-20));Check();var snapshot=life.World.Capture();life.World.LookAt(TownDistrictId.Central);life.World.Restore(snapshot);Check();
            Assert.That(life.World.Model.Residents.Select(a=>a.Position),Is.EqualTo(positions));Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GardenVisitorsCanWalkBothSidesAndStopAtTheExistingRestPlaces()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3,6})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);life.World.FastTravel(TownDistrictId.Central);
                foreach(var stop in new[]{new TownPoint(-.38f,-1.50f),new TownPoint(-.38f,-1.96f),new TownPoint(0,-2.04f),new TownPoint(.38f,-1.96f),new TownPoint(.38f,-1.50f),new TownPoint(.20f,-2.20f),new TownPoint(-.20f,-2.40f),TownWorldMap.Place("front-promenade"),TownWorldMap.Place("celebration")})
                {
                    Assert.That(life.World.GuidePoint(stop),Is.True);
                    for(int i=0;i<3000&&life.World.Model.Hero.IsWalking;i++){life.AdvanceFrame(1f/30);Assert.That(life.World.Model.Area.Contains(life.World.Model.Hero.Position),Is.True);}
                    Assert.That(life.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,stop),Is.LessThan(.002f));
                }
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GardenStagesKeepOneRestSeatAndReadableResidentNamesWithoutSaving()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3,6})
            {
                // Rebuild the actual stage, including world depth, rather than
                // changing only the scenery while leaving old actor ordering.
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);
                life.World.LookAt(TownDistrictId.South);Canvas.ForceUpdateCanvases();
                var scene=life.GetComponent<TownSceneryView>();string saved=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
                Assert.That(scene.Pieces.Count(p=>p.Rect.gameObject.activeSelf&&p.Placement.District==TownDistrictId.South&&p.Placement.Kind==TownSceneryKind.GardenPergola),Is.EqualTo(1));
                Assert.That(scene.Pieces.Any(p=>p.Rect.gameObject.activeSelf&&p.Placement.District==TownDistrictId.South&&p.Placement.Kind==TownSceneryKind.FlowerArch),Is.EqualTo(level>=6));
                foreach(var resident in life.Actors.Where(a=>a.RegionVisible&&a.transform.Find("Town actor name panel").gameObject.activeInHierarchy))EastMasterReviewTests.AssertNameClearOfBenches(life,resident);
                // This fixture position is used only for bounds verification,
                // never for the native candidate screenshots or route export.
                life.World.Model.Restore(life.World.Model.Hero.Id,TownWorldMap.Place("celebration"));
                life.World.ReturnToHero();life.RefreshWorldPresentation();Canvas.ForceUpdateCanvases();
                var hero=life.Actors.Single(a=>a.Controlled);var plate=hero.transform.Find("Town actor name panel");
                Assert.That(plate.gameObject.activeInHierarchy,Is.True,"A visitor at the southern rest stop keeps a readable name");
                var nameBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,plate);
                foreach(var bank in scene.Pieces.Where(p=>p.Rect.gameObject.activeInHierarchy&&p.Placement.District==TownDistrictId.South&&p.Placement.Kind==TownSceneryKind.FlowerBorder&&p.Rect.GetSiblingIndex()>hero.transform.GetSiblingIndex()))
                    Assert.That(nameBounds.Intersects(RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,bank.Rect)),Is.False,"Foreground flowers must not hide the celebration visitor name at Lv"+level);
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(saved));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
