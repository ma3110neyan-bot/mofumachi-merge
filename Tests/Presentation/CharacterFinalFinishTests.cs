using System;
using System.Reflection;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CharacterFinalFinishTests
    {
        [Test] public void RearHeadInheritsTheRotatedBodyNeckSocket()
        {
            foreach(NoahAuthoredMode mode in Enum.GetValues(typeof(NoahAuthoredMode)))for(int i=1;i<70;i++)
            {
                var p=NoahAuthoredMotion.Sample(i*.037f,i*.17f,1,mode,NoahAuthoredDirection.Back);
                p.Body.Point(0,-238,out float x,out float y);
                Assert.That(p.Head.X,Is.EqualTo(x).Within(.0001),"Rear head must remain a body child, including lean.");
                Assert.That(p.Head.Y,Is.EqualTo(y).Within(.0001));
            }
        }
        [TestCase(NoahAuthoredDirection.Left,-20)][TestCase(NoahAuthoredDirection.Right,20)][TestCase(NoahAuthoredDirection.Back,0)]
        public void RegisteredChinKeepsItsBodyLocalSocketWhileHeadTilts(NoahAuthoredDirection direction,float socketX)
        {
            var method=typeof(NoahAuthoredPose).GetMethod("AttachedHead");Assert.That(method,Is.Not.Null,"Rigid chin attachment resolver missing.");
            foreach(NoahAuthoredMode mode in Enum.GetValues(typeof(NoahAuthoredMode)))for(int i=0;i<60;i++)
            {
                var p=NoahAuthoredMotion.Sample(i*.043f,i*.19f,i%2,mode,direction);
                var head=(NoahPartTransform)method.Invoke(p,new object[]{0f,30f,0f,0f,63f});
                head.Point(0,63,out float x,out float y);p.Body.Point(socketX,-238+30+63,out float bx,out float by);
                Assert.That(x,Is.EqualTo(bx).Within(.0001));Assert.That(y,Is.EqualTo(by).Within(.0001));
                head.Point(120,63,out float ax,out float ay);
                Assert.That(Math.Sqrt((ax-x)*(ax-x)+(ay-y)*(ay-y)),Is.EqualTo(120).Within(.001),"Keep head art rigid.");
            }
        }
        private static object Tracker(string name){var t=typeof(NoahAuthoredMotion).Assembly.GetType("Mofumachi.Presentation."+name);Assert.That(t,Is.Not.Null,name+" missing");return Activator.CreateInstance(t);}
        private static NoahAuthoredDirection Face(object tracker,NoahAuthoredDirection previous,float dx,float dy,float seconds)
            =>(NoahAuthoredDirection)tracker.GetType().GetMethod("Update").Invoke(tracker,new object[]{previous,dx,dy,seconds});
        [Test] public void NorthMotionShowsBackAndDiagonalNoiseDoesNotFlipEveryFrame()
        {
            var t=Tracker("TownCharacterFacing");var face=NoahAuthoredDirection.Left;int flips=0;
            for(int i=0;i<60;i++){var next=Face(t,face,i%2==0?.0002f:-.0002f,.001f,1f/60);if(next!=face)flips++;face=next;}
            Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));Assert.That(flips,Is.EqualTo(1));
            for(int i=0;i<120;i++)face=Face(t,face,i%2==0?.0012f:.0009f,.001f,1f/60);
            Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));
            for(int i=0;i<12;i++)face=Face(t,face,.003f,.0001f,1f/60);
            Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Right));
        }
        [Test] public void StopRefreshAndOneFrameReverseDoNotLoseFacing()
        {
            var t=Tracker("TownCharacterFacing");var face=NoahAuthoredDirection.Back;
            face=Face(t,face,0,-.001f,1f/60);Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));
            face=Face(t,face,0,.001f,1f/60);Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));
            face=Face(t,face,0,0,1);Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));
            face=Face(t,face,.001f,0,0);Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));
            face=Face(t,face,float.NaN,float.PositiveInfinity,.1f);Assert.That(face,Is.EqualTo(NoahAuthoredDirection.Back));
        }
        [Test] public void TapGateIgnoresSameVisibleConversationAndShortRepeatsButAllowsAnotherCharacter()
        {
            var gate=Tracker("TownCharacterTapGate");var method=gate.GetType().GetMethod("TryOpen");
            bool Tap(string id,float time,bool visible)=>(bool)method.Invoke(gate,new object[]{id,time,visible});
            Assert.That(Tap("noah",0,false),Is.True);Assert.That(Tap("noah",.2f,false),Is.False);
            Assert.That(Tap("noah",2,true),Is.False);Assert.That(Tap("momo",2,true),Is.True);
            Assert.That(Tap("noah",2.01f,true),Is.True);Assert.That(Tap("noah",6,false),Is.True);
            Assert.That(Tap(null,7,false),Is.False);Assert.That(Tap("noah",float.NaN,false),Is.False);
        }
    }
}
