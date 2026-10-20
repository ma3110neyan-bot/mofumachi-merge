using System;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Visual.Tests
{
    public sealed class TownMeetingApproachTests
    {
        [TestCase(0,-.25f,.1f,.10f)]
        [TestCase(-1.2f,-.30f,-1.48f,-.25f)]
        [TestCase(1.2f,-.30f,1.48f,-.24f)]
        [TestCase(0,1.65f,-.30f,1.55f)]
        public void VisitStopsBesideTheRealFriendWithoutEnteringTheirFeetOrAPlant(float hx,float hy,float x,float y)
        {
            var floor=new TownWalkableArea(1);var hero=new TownPoint(hx,hy);var friend=new TownPoint(x,y);
            var points=TownMeetingApproach.Candidates(floor,hero,friend);
            Assert.That(points.Count,Is.GreaterThan(0));
            float previous=-1;
            foreach(var p in points)
            {
                Assert.That(floor.Contains(p),Is.True);
                Assert.That(floor.CanTravel(friend,p),Is.True);
                Assert.That(TownWalkableArea.Distance(p,friend),Is.InRange(.085f,.121f));
                var distance=TownWalkableArea.Distance(hero,p);Assert.That(distance,Is.GreaterThanOrEqualTo(previous));previous=distance;
            }
        }
        [Test] public void MissingOrNonfiniteFriendNeverProducesARemoteDestination()
        {
            Assert.That(TownMeetingApproach.Candidates(null,new TownPoint(0,0),new TownPoint(0,0)),Is.Empty);
            Assert.That(TownMeetingApproach.Candidates(new TownWalkableArea(1),new TownPoint(0,0),new TownPoint(float.NaN,0)),Is.Empty);
        }
    }
}
