using System;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class NorthEntryBlendTests
    {
        private static Type Layout()
        {
            var type=typeof(TownWorldMap).Assembly.GetType("Mofumachi.Presentation.TownNorthEntryBlendLayout");
            Assert.That(type,Is.Not.Null,"The opaque North road flare needs a bounded Central contact transition");return type;
        }
        private static float Alpha(TownPoint p)=>(float)Layout().GetMethod("VertexOpacity").Invoke(null,new object[]{p});
        private static TownPoint Sample(TownPoint p)=>(TownPoint)Layout().GetMethod("TexturePoint").Invoke(null,new object[]{p});
        private static float MeshAlpha(TownPoint point)
        {
            var vertices=(IReadOnlyList<(TownPoint World,TownPoint UV,float Alpha)>)Layout().GetProperty("Vertices").GetValue(null);
            int columns=(int)Layout().GetField("Columns").GetRawConstantValue(),rows=(int)Layout().GetField("Rows").GetRawConstantValue();
            float At(int ia,int ib,int ic)
            {
                var a=vertices[ia];var b=vertices[ib];var c=vertices[ic];
                float d=(b.World.Y-c.World.Y)*(a.World.X-c.World.X)+(c.World.X-b.World.X)*(a.World.Y-c.World.Y);
                if(Math.Abs(d)<.00000001f)return 0;
                float wa=((b.World.Y-c.World.Y)*(point.X-c.World.X)+(c.World.X-b.World.X)*(point.Y-c.World.Y))/d;
                float wb=((c.World.Y-a.World.Y)*(point.X-c.World.X)+(a.World.X-c.World.X)*(point.Y-c.World.Y))/d,wc=1-wa-wb;
                return wa<-.00001f||wb<-.00001f||wc<-.00001f?0:wa*a.Alpha+wb*b.Alpha+wc*c.Alpha;
            }
            float alpha=0;
            for(int row=0;row<rows-1;row++)for(int col=0;col<columns-1;col++)
            {int i=row*columns+col;alpha=Math.Max(alpha,Math.Max(At(i,i+columns,i+columns+1),At(i,i+columns+1,i+1)));}
            return alpha;
        }
        [Test] public void NorthContactContinuesTheExactCentralEdgeWithoutMovingTheAcceptedGround()
        {
            foreach(float x in new[]{-.6f,-.45f,-.30f,-.12f,0,.12f,.30f,.45f,.6f})
            {
                var point=new TownPoint(x,1);var expected=TownCentralMasterLayout.TexturePoint(point);
                Assert.That(TownWalkableArea.Distance(Sample(point),expected),Is.LessThan(.00001f));
                Assert.That(Alpha(point),Is.EqualTo(1));
            }
            Assert.That(TownCentralMasterLayout.Top,Is.EqualTo(1.23f));
            Assert.That(TownNorthMasterLayout.Bottom,Is.EqualTo(1f));
        }
        [Test] public void OnlyTheLowerNorthRoadCanReceiveTheTransition()
        {
            foreach(var point in new[]{new TownPoint(0,.999f),new TownPoint(0,1.321f),new TownPoint(-.601f,1.1f),new TownPoint(.601f,1.1f),new TownPoint(float.NaN,1),new TownPoint(0,float.PositiveInfinity)})
                Assert.That(Alpha(point),Is.Zero,"The candidate cannot paint other districts or the North court");
            Assert.That(Alpha(new TownPoint(0,1.20f)),Is.EqualTo(1));
            Assert.That(Alpha(new TownPoint(0,1.28f)),Is.EqualTo(1));
            Assert.That(Alpha(new TownPoint(0,1.30f)),Is.InRange(.4f,.6f));
            Assert.That(Alpha(new TownPoint(0,1.32f)),Is.Zero.Within(.00001f));
        }
        [Test] public void ExistingBroadSideCrossingsRetainTheirNorthPavingAboveTheContactFeather()
        {
            var floor=new TownWalkableArea(0);
            foreach(float side in new[]{-1f,1f})
            {
                Assert.That(floor.CanTravel(new TownPoint(side*.30f,.95f),new TownPoint(side*.30f,1.05f)),Is.True);
                foreach(float x in new[]{.27f,.30f,.33f})foreach(float y in new[]{1.012f,1.025f,1.05f,1.07f,1.10f})
                {
                    Assert.That(Alpha(new TownPoint(side*x,y)),Is.Zero.Within(.00001f),"Keep the existing low North paving in the free side crossing");
                    Assert.That(MeshAlpha(new TownPoint(side*x,y)),Is.Zero.Within(.00001f),"Triangle interpolation must also leave the broad side crossing clear");
                }
            }
        }
        [Test] public void NorthPlantedBanksKeepTheirOriginalPixelsOutsideTheRoadMask()
        {
            var width=Layout().GetMethod("RoadHalfWidth");Assert.That(width,Is.Not.Null);
            float last=.5f;
            foreach(float y in new[]{1f,1.05f,1.10f,1.15f,1.23f,1.32f})
            {
                float half=(float)width.Invoke(null,new object[]{y});
                Assert.That(half,Is.InRange(.199f,.481f));Assert.That(half,Is.LessThanOrEqualTo(last));last=half;
                if(y<1.07f)continue;
                foreach(float side in new[]{-1f,1f})foreach(float x in new[]{half+.001f,.50f,.60f})
                {
                    Assert.That(Alpha(new TownPoint(side*x,y)),Is.Zero.Within(.00001f));
                    Assert.That(MeshAlpha(new TownPoint(side*x,y)),Is.Zero.Within(.00001f));
                }
            }
            Assert.That(Alpha(new TownPoint(.5f,1.025f)),Is.InRange(.05f,.95f),"Garden contact has a short feather rather than another rectangular pasted edge");
        }
        [Test] public void ContinuedCurbsWidenTowardTheExistingNorthNeckWithoutAnotherTexture()
        {
            var contact=Sample(new TownPoint(0,1));var neck=Sample(new TownPoint(0,1.32f));
            Assert.That(contact.X,Is.EqualTo(.5f));Assert.That(neck.X,Is.EqualTo(.5f));
            Assert.That(neck.Y,Is.EqualTo(TownCentralMasterLayout.TexturePoint(new TownPoint(0,.76f)).Y).Within(.00001f));
            float previous=contact.Y;
            for(int i=1;i<=32;i++)
            {
                float y=1+i*.01f;var uv=Sample(new TownPoint(.1f,y));
                Assert.That(uv.X,Is.EqualTo(TownCentralMasterLayout.TexturePoint(new TownPoint(.1f,1)).X).Within(.00001f));
                Assert.That(uv.Y,Is.LessThan(previous));previous=uv.Y;
            }
        }
        [Test] public void StaticTransitionMeshIsFiniteBoundedAndLeavesOtherPresentationDataUntouched()
        {
            var floors=new TownWalkableArea(0).FloorPolygons;var excluded=new TownWalkableArea(0).ExcludedPolygons;
            var north=TownNorthMasterLayout.Dressing.ToArray();var central=TownCentralMasterLayout.Dressing.ToArray();
            var vertices=(IReadOnlyList<(TownPoint World,TownPoint UV,float Alpha)>)Layout().GetProperty("Vertices").GetValue(null);
            int columns=(int)Layout().GetField("Columns").GetRawConstantValue(),rows=(int)Layout().GetField("Rows").GetRawConstantValue();
            Assert.That(vertices.Count,Is.EqualTo(columns*rows).And.LessThanOrEqualTo(640));
            foreach(var vertex in vertices)
            {
                Assert.That(TownWorldLayout.Finite(vertex.World)&&TownWorldLayout.Finite(vertex.UV),Is.True);
                Assert.That(vertex.World.X,Is.InRange(-.6f,.6f));Assert.That(vertex.World.Y,Is.InRange(1f,1.32f));
                Assert.That(vertex.UV.X,Is.InRange(0,1));Assert.That(vertex.UV.Y,Is.InRange(0,1));Assert.That(vertex.Alpha,Is.InRange(0,1));
            }
            Assert.That(Layout().GetProperty("Vertices").GetValue(null),Is.SameAs(vertices),"Camera refresh reprojects a cached mesh");
            Assert.That(new TownWalkableArea(6).FloorPolygons,Is.SameAs(floors));Assert.That(new TownWalkableArea(6).ExcludedPolygons,Is.SameAs(excluded));
            Assert.That(TownNorthMasterLayout.Dressing,Is.EqualTo(north));Assert.That(TownCentralMasterLayout.Dressing,Is.EqualTo(central));
        }
    }
}
