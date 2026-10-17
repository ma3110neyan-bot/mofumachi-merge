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
        public void NorthResidentsNaturallyComeWithinTalkingRangeAtStartingAndGrownLevels(int level)
        {
            var town=new TownWorldWalkModel(level,"momo");bool canMeet=false;float nearest=float.PositiveInfinity;
            for(int i=0;i<3600&&!canMeet;i++)
            {
                town.Step(1f/30);var north=town.Residents.Where(a=>!a.Controlled&&a.Region==TownRegionId.West).ToArray();
                for(int a=0;a<north.Length;a++)for(int b=a+1;b<north.Length;b++)
                {
                    nearest=Math.Min(nearest,TownWorldLayout.Distance(north[a].Position,north[b].Position));
                    if(TownWorldLayout.WithinConversationRange(north[a].Position,north[b].Position,360,62))canMeet=true;
                }
            }
            Assert.That(canMeet,Is.True,"The initial north residents must be able to greet on their real routes; nearest world distance="+nearest);
        }
        [Test] public void ConversationRangeUsesVisibleBodySpacingAndTheSameVerticalProjectionAsTheTown()
        {
            var p=new TownPoint(.5f,1.3f);
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(.74f,1.3f),360,62),Is.True);
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(.74f,1.3f),720,62),Is.False,"Do not converse across more than the bodies' visual spacing");
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(.5f,1.44f),360,62),Is.False,"Vertical world units project taller than horizontal units");
            Assert.That(TownWorldLayout.WithinConversationRange(p,new TownPoint(float.NaN,1.3f),360,62),Is.False);
            Assert.That(TownWorldLayout.WithinConversationRange(p,p,0,62),Is.False);
        }
    }
}
