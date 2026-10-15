using System;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class StreetExperienceTests
    {
        [Test] public void RoadPointIsProjectedToConnectedStreetAndBuildingTapIsRejected()
        {
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.Central,0,new TownPoint(.53f,.49f),out var choice),Is.True);
            Assert.That(choice.Kind,Is.EqualTo(StreetTargetKind.Road));
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.Central,0,new TownPoint(.95f,.95f),out _),Is.False);
        }
        [Test] public void ExitAndRestStopTapsHaveTheirOwnMeaning()
        {
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.Central,0,new TownPoint(.15f,.52f),out var west),Is.True);
            Assert.That(west.Kind,Is.EqualTo(StreetTargetKind.Exit));Assert.That(west.Region,Is.EqualTo(TownRegionId.West));
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.East,0,new TownPoint(.09f,.50f),out var back),Is.True);
            Assert.That(back.Region,Is.EqualTo(TownRegionId.Central));
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.West,0,new TownPoint(.39f,.36f),out var bench),Is.True);
            Assert.That(bench.PlaceId,Is.EqualTo("west.bench-front"));
        }
        [Test] public void InvalidAndLockedStreetTapsDoNotMakeTargets()
        {
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.Central,0,new TownPoint(float.NaN,0),out _),Is.False);
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.Central,0,new TownPoint(.50f,.275f),out _),Is.False);
            Assert.That(TownStreetSelection.TryResolve(TownRegionId.Central,6,new TownPoint(.50f,.275f),out var open),Is.True);
            Assert.That(open.PlaceId,Is.EqualTo("celebration"));
        }
        [Test] public void CharacterMotionStaysBoundedAndSettlesAtRest()
        {
            for(int i=0;i<400;i++)
            {
                var pose=TownCharacterMotion.Sample(i*.03f,1,1,0);
                Assert.That(pose.Hop,Is.InRange(0f,4f));Assert.That(Math.Abs(pose.Lean),Is.LessThanOrEqualTo(3));
                Assert.That(float.IsFinite(pose.Hop),Is.True);
            }
            var idle=TownCharacterMotion.Sample(0,0,0,0);Assert.That(idle.Hop,Is.Zero);Assert.That(idle.Lean,Is.Zero);
            var invalid=TownCharacterMotion.Sample(float.NaN,float.NaN,0,0);Assert.That(invalid.Hop,Is.Zero);
        }
        [Test] public void BlinkWindowsAreBriefAndDifferentForDifferentResidents()
        {
            int blink=0;
            for(int i=0;i<400;i++)if(TownCharacterMotion.Blink(i*.01f,0))blink++;
            Assert.That(blink,Is.InRange(5,20));
            Assert.That(TownCharacterMotion.Blink(.04f,0),Is.Not.EqualTo(TownCharacterMotion.Blink(.04f,3)));
        }
    }
}
