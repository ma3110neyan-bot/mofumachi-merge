using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class FiveAreaRouteTests
    {
        [Test] public void ReviewRoutesVisitEachDistrictRoleOnTheRealFloorInBothDirections()
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Tests.TownReviewRoutes");
            Assert.That(type,Is.Not.Null,"All five areas need actual navigation annotations, not guessed arrows");
            var floor=new TownWalkableArea(0);var nav=new TownAreaNavigation(floor);
            foreach(TownDistrictId district in Enum.GetValues(typeof(TownDistrictId)))
            {
                var links=(IReadOnlyList<TownPoint[]>)type.GetMethod("For").Invoke(null,new object[]{district});
                Assert.That(links.Count,Is.EqualTo(6));
                foreach(var link in links)foreach(var ordered in new[]{link,link.Reverse().ToArray()})
                {
                    var from=ordered[0];Assert.That(TownDistrictLayout.At(from),Is.EqualTo(district));Assert.That(floor.Contains(from),Is.True);
                    foreach(var to in ordered.Skip(1))
                    {Assert.That(floor.Contains(to),Is.True,district+" "+to);var route=nav.FindRoute(from,to);Assert.That(route,Is.Not.Empty);foreach(var step in route){Assert.That(floor.CanTravel(from,step),Is.True);from=step;}}
                }
            }
        }
    }
}
