using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownVisiblePassingTests
    {
        [Test] public void HeadOnResidentsUseTheOpenSurfaceBeforeTheirBodiesStack()
        {
            var town=new TownWorldWalkModel(6,"momo");
            town.Restore("momo",new TownPoint(0,-.75f));
            town.Restore("lulu",new TownPoint(-.25f,-.24f));town.Restore("pom",new TownPoint(.25f,-.24f));
            Assert.That(town.TryGuide("lulu","east.entry"),Is.True);Assert.That(town.TryGuide("pom","west.entry"),Is.True);
            var left=town.Residents.Single(a=>a.Id=="lulu");var right=town.Residents.Single(a=>a.Id=="pom");
            bool passed=false;float nearestAtCrossing=float.PositiveInfinity;
            for(int frame=0;frame<1800&&!passed;frame++)
            {
                foreach(var resident in town.Residents.Where(r=>r!=left&&r!=right))town.Pause(resident.Id,.2f);
                town.Step(1f/30);
                if(Math.Abs(left.Position.X-right.Position.X)<.04f)nearestAtCrossing=Math.Min(nearestAtCrossing,Math.Abs(left.Position.Y-right.Position.Y));
                passed=left.Position.X>right.Position.X+.10f;
            }
            Assert.That(passed,Is.True,"Use an alternate line rather than stop head-on");
            Assert.That(nearestAtCrossing,Is.GreaterThanOrEqualTo(.14f),"Keep a visible passing gap, not just separated foot points");
        }
    }
}
