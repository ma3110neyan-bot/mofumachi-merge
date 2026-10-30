using System;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class NoahAuthoredMotionTests
    {
        [Test] public void WorldNorthFacesAwayAndStoppingRetainsFacing()
        {
            Assert.That(NoahAuthoredMotion.Direction(0,1,NoahAuthoredDirection.Left),Is.EqualTo(NoahAuthoredDirection.Back));
            Assert.That(NoahAuthoredMotion.Direction(0,-1,NoahAuthoredDirection.Right),Is.EqualTo(NoahAuthoredDirection.Front));
            Assert.That(NoahAuthoredMotion.Direction(1,0,NoahAuthoredDirection.Left),Is.EqualTo(NoahAuthoredDirection.Right));
            Assert.That(NoahAuthoredMotion.Direction(0,0,NoahAuthoredDirection.Right),Is.EqualTo(NoahAuthoredDirection.Right));
            Assert.That(NoahAuthoredMotion.Direction(float.NaN,float.PositiveInfinity,NoahAuthoredDirection.Left),Is.EqualTo(NoahAuthoredDirection.Left));
        }
        [Test] public void AllThreeDirectionsKeepFacesRigidAndOneFootGrounded()
        {
            foreach(var d in new[]{NoahAuthoredDirection.Left,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back})
            foreach(NoahAuthoredMode mode in Enum.GetValues(typeof(NoahAuthoredMode)))
            for(int i=0;i<70;i++)
            {
                var p=NoahAuthoredMotion.Sample(i/30f,i*.14f,1,mode,d);
                p.Head.Point(0,0,out float ax,out float ay);p.Head.Point(150,0,out float bx,out float by);
                Assert.That(Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay)),Is.EqualTo(150).Within(.001));
                Assert.That(Math.Max(p.NearLeg.Y,p.FarLeg.Y),Is.Zero);
                Assert.That(p.NearLeg.Y<=0&&p.FarLeg.Y<=0,Is.True);
            }
        }
        [Test] public void PouchStaysOnAnatomicalRightAndRearGreetingActuallyMovesAnArm()
        {
            var right=NoahAuthoredMotion.Sample(0,0,0,NoahAuthoredMode.Idle,NoahAuthoredDirection.Right);
            var back=NoahAuthoredMotion.Sample(0,0,0,NoahAuthoredMode.Idle,NoahAuthoredDirection.Back);
            Assert.That(right.Pouch.X,Is.LessThan(0));Assert.That(back.Pouch.X,Is.GreaterThan(0));
            var a=NoahAuthoredMotion.Sample(.1f,0,0,NoahAuthoredMode.Greet,NoahAuthoredDirection.Back);
            var b=NoahAuthoredMotion.Sample(.4f,0,0,NoahAuthoredMode.Greet,NoahAuthoredDirection.Back);
            Assert.That(Math.Abs(a.NearArm.Rotation-b.NearArm.Rotation),Is.GreaterThan(.05));
        }
        [Test] public void InvalidInputCannotMakeANonfinitePoseAndStoppingSettlesTheFeet()
        {
            var p=NoahAuthoredMotion.Sample(float.NaN,float.PositiveInfinity,float.NaN,NoahAuthoredMode.Walk,NoahAuthoredDirection.Left);
            foreach(string id in new[]{"head","body","farArm","nearArm","tail","pouch","farLeg","nearLeg"})
            {var t=p.Part(id);Assert.That(float.IsFinite(t.X)&&float.IsFinite(t.Y)&&float.IsFinite(t.Rotation),Is.True);}
            p=NoahAuthoredMotion.Sample(2,1,0,NoahAuthoredMode.Walk,NoahAuthoredDirection.Right);
            Assert.That(p.NearLeg.Y,Is.Zero);Assert.That(p.FarLeg.Y,Is.Zero);
        }
    }
}
