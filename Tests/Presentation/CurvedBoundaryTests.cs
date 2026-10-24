using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CurvedBoundaryTests
    {
        private static (TownPoint World,TownPoint UV,float Alpha) Vertex(int patch,float along,int row)
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownBoundaryCurveLayout");
            Assert.That(type,Is.Not.Null,"Continue the actual painted edge into a curved verge instead of leaving a rectangular cut");
            return ((TownPoint,TownPoint,float))type.GetMethod("GetVertex").Invoke(null,new object[]{patch,along,row});
        }
        [Test] public void OldPaintingContactUsesItsExactTexelsAndOpaqueCoverage()
        {
            for(int patch=0;patch<4;patch++)foreach(float t in new[]{.15f,.5f,.85f})
            {
                var v=Vertex(patch,t,1);
                var edge=patch<2?new TownPoint(patch==0?-1.8f+.88f*t:.92f+.88f*t,-1):new TownPoint(patch==2?-.6f:.6f,1.23f+2*t);
                var uv=patch==0?TownWestMasterLayout.TexturePoint(edge):patch==1?TownEastMasterLayout.TexturePoint(edge):TownNorthMasterLayout.TexturePoint(edge);
                Assert.That(TownWalkableArea.Distance(v.World,edge),Is.LessThan(.00001f));
                Assert.That(TownWalkableArea.Distance(v.UV,uv),Is.LessThan(.00001f));Assert.That(v.Alpha,Is.EqualTo(1));
            }
        }
        [Test] public void OuterEdgeCurvesAndFadesWithinANarrowVerge()
        {
            for(int patch=0;patch<4;patch++)
            {
                var depths=new[]{.1f,.25f,.5f,.75f,.9f}.Select(t=>
                {
                    var edge=Vertex(patch,t,1);var outer=Vertex(patch,t,3);
                    Assert.That(outer.Alpha,Is.Zero);return TownWalkableArea.Distance(edge.World,outer.World);
                }).ToArray();
                Assert.That(depths.All(d=>d>=.06f&&d<=.16f),Is.True);Assert.That(depths.Max()-depths.Min(),Is.GreaterThan(.025f),"An equally deep rectangle is not a curved boundary");
            }
        }
        [Test] public void CurveLeavesSouthPaintingAndWalkingRoomsIntact()
        {
            var floors=new TownWalkableArea(0).FloorPolygons;
            for(int patch=0;patch<4;patch++)for(int col=0;col<17;col++)for(int row=0;row<4;row++)
            {
                var p=Vertex(patch,col/16f,row).World;
                if(patch<2)Assert.That(Math.Abs(p.X),Is.GreaterThanOrEqualTo(.91999f),"Do not paint over the accepted South garden");
                else Assert.That(p.Y,Is.GreaterThanOrEqualTo(1.23f));
                foreach(var floor in floors)
                    Assert.That(p.X>floor.Min(v=>v.X)+.0061f&&p.X<floor.Max(v=>v.X)-.0061f&&p.Y>floor.Min(v=>v.Y)+.0061f&&p.Y<floor.Max(v=>v.Y)-.0061f,Is.False,"Only the matching .006world inner overlap may enter a room");
            }
        }
        [Test] public void SharedTextureCoordinatesAndAlphaRemainFiniteAndBounded()
        {
            for(int patch=0;patch<4;patch++)for(int col=0;col<17;col++)for(int row=0;row<4;row++)
            {
                var v=Vertex(patch,col/16f,row);
                Assert.That(float.IsFinite(v.World.X)&&float.IsFinite(v.World.Y),Is.True);
                Assert.That(v.UV.X,Is.InRange(0,1));Assert.That(v.UV.Y,Is.InRange(0,1));Assert.That(v.Alpha,Is.InRange(0,1));
            }
        }
    }
}
