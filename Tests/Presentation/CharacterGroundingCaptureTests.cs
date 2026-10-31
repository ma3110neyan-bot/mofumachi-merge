using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CharacterGroundingCaptureTests
    {
        private static bool Sample(int frame,bool walking,NoahAuthoredDirection actual,NoahAuthoredDirection intended,bool captured)
        {
            var method=typeof(CharacterAuthoredCaptureRoute).GetMethod("ShouldCaptureGrounding",BindingFlags.Public|BindingFlags.Static);
            Assert.That(method,Is.Not.Null,"Direction-aware native grounding sample policy is absent.");
            return (bool)method.Invoke(null,new object[]{frame,walking,actual,intended,captured});
        }
        [TestCase("momo")][TestCase("miel")]
        public void RealRoutesCaptureWalkingInAllFourDirections(string id)
        {
            var model=new TownWorldWalkModel(0,id);
            model.Restore(id,CharacterAuthoredCaptureRoute.MomoStart);model.Restore("noah",CharacterAuthoredCaptureRoute.PartnerStart);
            var direction=NoahAuthoredDirection.Left;var captured=new List<NoahAuthoredDirection>();
            var fixedFrame=new HashSet<NoahAuthoredDirection>();var walked=new HashSet<NoahAuthoredDirection>();
            foreach(var target in CharacterAuthoredCaptureRoute.Targets)
            {
                var intended=NoahAuthoredMotion.Direction(target.X-model.Hero.Position.X,target.Y-model.Hero.Position.Y,direction);
                Assert.That(model.TryGuidePoint(target),Is.True);bool sampled=false;
                for(int frame=0;frame<180;frame++)
                {
                    foreach(var npc in model.Residents.Where(n=>!n.Controlled))model.Pause(npc.Id,4);
                    model.Step(1f/30);
                    if(model.Hero.IsWalking){direction=NoahAuthoredMotion.Direction(model.Hero.Position.X-model.Hero.Previous.X,model.Hero.Position.Y-model.Hero.Previous.Y,direction);walked.Add(direction);}
                    if(frame==12)fixedFrame.Add(direction);
                    if(Sample(frame,model.Hero.IsWalking,direction,intended,sampled)){captured.Add(direction);sampled=true;}
                    if(frame>12&&!model.Hero.IsWalking)break;
                }
                Assert.That(sampled,Is.True,"A real walking sample is required before ending each route.");
                Assert.That(model.Hero.IsWalking,Is.False);model.CancelHero();
            }
            Assert.That(walked,Does.Contain(NoahAuthoredDirection.Back));
            Assert.That(fixedFrame,Does.Not.Contain(NoahAuthoredDirection.Back),"Regression fixture must reproduce the missing rear sample at fixed frame13.");
            Assert.That(captured,Is.EquivalentTo(new[]{NoahAuthoredDirection.Left,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back,NoahAuthoredDirection.Front}));
        }
        [Test] public void SamplingRequiresWarmupActualWalkingIntendedFacingAndNoPreviousSample()
        {
            Assert.That(Sample(11,true,NoahAuthoredDirection.Back,NoahAuthoredDirection.Back,false),Is.False);
            Assert.That(Sample(20,false,NoahAuthoredDirection.Back,NoahAuthoredDirection.Back,false),Is.False);
            Assert.That(Sample(20,true,NoahAuthoredDirection.Right,NoahAuthoredDirection.Back,false),Is.False);
            Assert.That(Sample(20,true,NoahAuthoredDirection.Back,NoahAuthoredDirection.Back,true),Is.False);
            Assert.That(Sample(20,true,NoahAuthoredDirection.Back,NoahAuthoredDirection.Back,false),Is.True);
        }
    }
}
