using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class FieldEdgeRecoveryTests
    {
        [Test] public void NorthEdgeBodyClearsTheExistingHudAndLocationRibbon()
        {
            const float scale=360/1.1f;var camera=new TownWorldCamera();camera.Fit(1.1f,1.55f,85/scale,62/scale);
            var feet=new TownPoint(.43f,2.98f);camera.Jump(new TownPoint(feet.X,feet.Y+.42f),feet);
            float topClearance=(.775f-(feet.Y-camera.Center.Y)-.22f)*scale;
            Assert.That(topClearance,Is.GreaterThanOrEqualTo(85f),"Body must stay below the existing81px ribbon, with4px gap; being inside the field alone is insufficient");
            feet=new TownPoint(.43f,-2.98f);camera.Jump(new TownPoint(feet.X,feet.Y+.42f),feet);
            Assert.That((feet.Y-camera.Center.Y+.775f)*scale,Is.GreaterThanOrEqualTo(62f),"The bottom actions must not intercept the return grab after arrival");
        }
        [TestCase(1)] [TestCase(3)] [TestCase(6)]
        public void FiveAreaEdgesReturnByTapAndDragWithAllSixResidentsStillActive(int level)
        {
            var town=new TownWorldWalkModel(level,"miel");var camera=new TownWorldCamera();camera.Fit(1.1f,1.55f);
            var home=TownWorldMap.Place("square");
            foreach(var edge in new[]{new TownPoint(1.8f,.68f),new TownPoint(-1.8f,.68f),new TownPoint(.43f,2.98f),new TownPoint(.43f,-2.98f),new TownPoint(0,.97f)})
            {
                Assert.That(town.TryGuidePoint(edge),Is.True);
                for(int i=0;i<6000&&town.Hero.IsWalking;i++)
                {town.Step(1f/30);var p=town.Hero.Position;camera.Follow(new TownPoint(p.X,p.Y+TownSceneryLayout.CameraLift),1f/30,p);Assert.That(town.Area.Contains(p),Is.True);}
                Assert.That(town.Hero.IsWalking,Is.False,$"Edge {edge.X},{edge.Y}");
                Assert.That(TownWalkableArea.Distance(town.Hero.Position,edge),Is.LessThan(.002f));
                Assert.That(town.FollowFinger(home),Is.True);
                for(int i=0;i<6000&&town.Hero.IsWalking;i++)town.Step(1f/30);
                town.ReleaseFinger();for(int i=0;i<60&&town.Hero.IsWalking;i++)town.Step(1f/30);
                Assert.That(town.Hero.IsWalking||town.IsFollowingFinger,Is.False,"Return without Restore/FastTravel, with live NPCs");
                Assert.That(TownWalkableArea.Distance(town.Hero.Position,home),Is.LessThan(.015f));
            }
        }
        [TestCase(1.5999999f,.45000005f)] [TestCase(1.65f,.45000005f)]
        public void EastBuildingCornerCanBeWalkedOutAndBackWithoutReset(float x,float y)
        {
            var town=new TownWorldWalkModel(6,"miel");var entry=TownWorldMap.Place("east.entry");
            town.Restore("miel",entry);
            foreach(var target in new[]{new TownPoint(x,y),entry})
            {
                Assert.That(town.TryGuidePoint(target),Is.True);
                for(int i=0;i<5000&&town.Hero.IsWalking;i++)town.Step(1f/30);
                Assert.That(town.Hero.IsWalking,Is.False,$"East journey to {target.X},{target.Y} stalled at {town.Hero.Position.X},{town.Hero.Position.Y}");
                Assert.That(TownWalkableArea.Distance(town.Hero.Position,target),Is.LessThan(.002f));
            }
        }
        [TestCase(1.8f,.68f)] [TestCase(-1.8f,.68f)]
        [TestCase(.43f,2.98f)] [TestCase(.43f,-2.98f)]
        public void FollowingAnEdgeResidentKeepsTheBodyAvailableForReturnInput(float x,float y)
        {
            var feet=new TownPoint(x,y);Assert.That(new TownWalkableArea(6).Contains(feet),Is.True);
            var camera=new TownWorldCamera();camera.Fit(1.10f,1.55f);
            camera.Jump(new TownPoint(feet.X,feet.Y+TownSceneryLayout.CameraLift),feet);
            for(int i=0;i<120;i++)camera.Follow(new TownPoint(feet.X,feet.Y+TownSceneryLayout.CameraLift),1f/30,feet);
            Assert.That(feet.X-camera.Center.X,Is.InRange(-.45f,.45f),"The east/west resident must remain draggable instead of being framed into North");
            Assert.That(feet.Y-camera.Center.Y,Is.InRange(-.73501f,.53501f),"Top-edge residents need visible body/head room, not only an off-screen name/shadow");
            camera.Fit(1.1f,.9f);
            Assert.That(feet.X-camera.Center.X,Is.InRange(-.45001f,.45001f));
            Assert.That(feet.Y-camera.Center.Y,Is.InRange(-.41001f,.21001f),"A resize must keep the tracked body available");
            camera.Jump(new TownPoint(0,2));camera.Pan(100,0);
            Assert.That(camera.Center.X+.55f,Is.LessThanOrEqualTo(.60001f),"Free North pans must clear the East tracking constraint");
        }
    }
}
