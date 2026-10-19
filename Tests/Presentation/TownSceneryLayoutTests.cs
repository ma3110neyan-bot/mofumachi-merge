using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownSceneryLayoutTests
    {
        [Test] public void EveryDistrictHasAnIndependentRecognizableLandmark()
        {
            Assert.That(TownSceneryLayout.Landmarks.Count,Is.EqualTo(5));
            Assert.That(TownSceneryLayout.Landmarks.Select(p=>p.District).Distinct().Count(),Is.EqualTo(5));
            var floor=new TownWalkableArea(0);
            foreach(var p in TownSceneryLayout.Landmarks)
            {
                Assert.That(floor.Contains(p.Foundation),Is.False,p.Kind.ToString());
                Assert.That(p.Width,Is.InRange(.35f,.65f));
                Assert.That(p.Height,Is.InRange(.20f,.70f));
                Assert.That(TownDistrictLayout.At(p.Foundation),Is.EqualTo(p.District));
            }
        }
        [Test] public void EachPlantedIslandIsVisibleAndSceneryKeepsWalkingGroundClear()
        {
            var floor=new TownWalkableArea(0);
            foreach(var hole in floor.ExcludedIslands.Skip(5))
                Assert.That(TownSceneryLayout.Nature.Any(p=>TownWalkableArea.Distance(p.Foundation,hole.Center)<.01f),Is.True);
            foreach(var p in TownSceneryLayout.Nature)
                Assert.That(floor.Contains(p.Foundation),Is.False,p.Kind+" at "+p.Foundation.X+","+p.Foundation.Y);
        }
        [Test] public void SourceArtworkIsOnlyASpriteSheetNeverTheWorldDimensions()
        {
            foreach(var p in TownSceneryLayout.Landmarks)
                Assert.That(p.Width,Is.LessThan(TownDistrictLayout.RoomWidth*.6f),"A landmark leaves routes on both sides");
            Assert.That(TownSceneryLayout.CameraLift,Is.EqualTo(.42f));
        }
        [Test] public void PaintedFrontPostsAndPlanterBasesCannotBeEnteredByWalking()
        {
            var floor=new TownWalkableArea(0);
            foreach(var p in new[]{new TownPoint(.1354f,-1.8843f),new TownPoint(-1.3975f,.1664f),new TownPoint(1.42f,.16f),new TownPoint(.18f,2.19f),new TownPoint(.13f,.21f)})
            {
                Assert.That(floor.Contains(p),Is.False,"Opaque front base at "+p.X+","+p.Y);
                Assert.That(floor.TryClamp(p,out var edge,.10f),Is.True,"A nearby finger target must resolve to the path edge");
                Assert.That(floor.Contains(edge),Is.True);Assert.That(TownWalkableArea.Distance(p,edge),Is.LessThanOrEqualTo(.10f));
            }
        }
        [Test] public void ArchesKeepTheirOpeningWalkableButProtectTheActualPillarFeet()
        {
            var floor=new TownWalkableArea(6);
            foreach(var p in new[]{new TownPoint(-.14913f,1.06565f),new TownPoint(.15f,1.06f),new TownPoint(-1.305f,-.77f),new TownPoint(-1.095f,-.77f),new TownPoint(1.095f,-.81f),new TownPoint(1.305f,-.81f)})
                Assert.That(floor.Contains(p),Is.False,"Arch post at "+p.X+","+p.Y);
            foreach(var p in new[]{new TownPoint(0,1.04f),new TownPoint(-1.2f,-.79f),new TownPoint(1.2f,-.83f)})
                Assert.That(floor.Contains(p),Is.True,"Walk through the middle of an arch");
        }
        [Test] public void ArrivalCameraFitsTheWholeLandmarkAndHeroInTheActualSafeField()
        {
            foreach(float width in new[]{320f,360f})foreach(float screenHeight in new[]{592f,640f,740f,808f})
            {
                // Safe area 24/32, compact controls 62/80; projection uses this
                // field, never the larger background rectangle behind the HUD.
                float fieldHeight=screenHeight-24-32-62-80,scale=TownWorldLayout.ScaleForViewport(width,fieldHeight);
                var camera=new TownWorldCamera();camera.Fit(width/scale,fieldHeight/scale);
                foreach(var landmark in TownSceneryLayout.Landmarks)
                {
                    var hero=TownWorldMap.Place(TownDistrictLayout.EntranceId(landmark.District));camera.Jump(new TownPoint(hero.X,hero.Y+TownSceneryLayout.CameraLift));
                    float top=(landmark.Feet.Y+landmark.Height-camera.Center.Y)*scale;
                    Assert.That(top,Is.LessThanOrEqualTo(fieldHeight/2+.01f),landmark.Kind+" roof at "+width+"x"+screenHeight);
                    float feet=(hero.Y-camera.Center.Y)*scale;
                    Assert.That(feet-22,Is.GreaterThanOrEqualTo(-fieldHeight/2),landmark.Kind+" name above footer");
                    Assert.That(feet+82,Is.LessThanOrEqualTo(fieldHeight/2),landmark.Kind+" head below HUD");
                }
            }
        }
    }
}
