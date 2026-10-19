using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownWorldRegressionTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(6)]
        public void WestResidentsNaturallyComeWithinTalkingRangeAtStartingAndGrownLevels(int level)
        {
            var town=new TownWorldWalkModel(level,"momo");bool canMeet=false;float nearest=float.PositiveInfinity;
            for(int i=0;i<3600&&!canMeet;i++)
            {
                town.Step(1f/30);var west=town.Residents.Where(a=>!a.Controlled&&a.Region==TownRegionId.West).ToArray();
                for(int a=0;a<west.Length;a++)for(int b=a+1;b<west.Length;b++)
                {
                    nearest=Math.Min(nearest,TownWorldLayout.Distance(west[a].Position,west[b].Position));
                    if(TownWorldLayout.WithinConversationRange(west[a].Position,west[b].Position,360,62))canMeet=true;
                }
            }
            Assert.That(canMeet,Is.True,"The initial west residents must be able to greet on their real routes; nearest world distance="+nearest);
        }
        [Test] public void ConversationRangeUsesVisibleBodySpacingAndTheSameProjectionAsTheTown()
        {
            var p=TownWorldMap.Place("square");
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(p.X+.20f,p.Y),360,62),Is.True);
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(p.X+.20f,p.Y),720,62),Is.False,"Do not converse across more than the bodies' visual spacing");
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(p.X,p.Y+.30f),360,62),Is.False,"Conversation distance uses the map's actual projection");
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(float.NaN,1.3f),360,62),Is.False);
            Assert.That(TownWorldLayout.WithinConversationRange(p,p,0,62),Is.False);
        }
    }
}
