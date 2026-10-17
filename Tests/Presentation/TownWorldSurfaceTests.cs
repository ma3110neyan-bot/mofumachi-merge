using System;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownWorldSurfaceTests
    {
        [Test] public void PaintingEdgesBlendIntoTheGardenButTheInteriorStaysOpaque()
        {
            Assert.That(TownWorldSurface.PaintingOpacity(.5f,.5f),Is.EqualTo(1));
            foreach(var p in new[]{(0f,.5f),(1f,.5f),(.5f,0f),(.5f,1f)})Assert.That(TownWorldSurface.PaintingOpacity(p.Item1,p.Item2),Is.Zero);
            Assert.That(TownWorldSurface.PaintingOpacity(.03f,.5f),Is.InRange(.01f,.99f));
            Assert.That(TownWorldSurface.PaintingOpacity(.5f,.04f),Is.InRange(.01f,.99f));
        }
        [Test] public void GardenPathsShareOneCrossSectionAtEveryBendAndKeepTexturePhaseContinuous()
        {
            foreach(var path in new[]{TownWorldLayout.NorthConnection,TownWorldLayout.SouthConnection})
            {
                var original=path.ToArray();var ribbon=TownWorldSurface.BuildPath(path);
                Assert.That(ribbon.Count,Is.GreaterThan(path.Length));
                foreach(var p in path)Assert.That(ribbon.Count(s=>TownWorldLayout.Distance(s.Center,p)<.00001f),Is.EqualTo(1),"A bend must have one shared section rather than overlapping segment ends");
                for(int i=0;i<ribbon.Count;i++)
                {
                    var s=ribbon[i];Assert.That(TownWorldLayout.Finite(s.Point(.1f)),Is.True);Assert.That(s.TextureV,Is.InRange(0f,1f));
                    float length=(float)Math.Sqrt(s.Normal.X*s.Normal.X+s.Normal.Y*s.Normal.Y*TownWorldLayout.ImageAspect*TownWorldLayout.ImageAspect);Assert.That(length,Is.InRange(.99f,1.51f));
                    if(i>0){var a=ribbon[i-1];Assert.That(s.Distance,Is.GreaterThan(a.Distance));Assert.That(Math.Abs(s.TextureV-a.TextureV),Is.LessThanOrEqualTo((s.Distance-a.Distance)/TownWorldSurface.PathTileLength+.0001f),"Do not reset texture UVs at path nodes");}
                }
                Assert.That(path,Is.EqualTo(original),"Rendering cannot change the walking route");
            }
        }
        [Test] public void PathOverlayCoversTheConnectorsAndEndsBeforeThePaintedSquareAndCafe()
        {
            Assert.That(TownWorldSurface.PathOpacity(.82f,true),Is.EqualTo(1));Assert.That(TownWorldSurface.PathOpacity(-.08f,false),Is.EqualTo(1));
            Assert.That(TownWorldSurface.PathOpacity(.47f,true),Is.Zero);Assert.That(TownWorldSurface.PathOpacity(1.4f,true),Is.Zero);
            Assert.That(TownWorldSurface.PathOpacity(.47f,false),Is.Zero);Assert.That(TownWorldSurface.PathOpacity(-.4f,false),Is.Zero);
        }
        [Test] public void ConnectorStonesHaveTheSameScaleAcrossAndAlongTheRibbon()
        {
            // One atlas half is twice as tall as it is wide. Projected world distance
            // is already aspect-correct, so a stone must keep its shape at a bend.
            float across=2*TownWorldSurface.PathHalfWidth/(.5f-.004f);
            float along=TownWorldSurface.PathTileLength/(1-.004f);
            Assert.That(across/along,Is.InRange(.95f,1.05f),"The narrow source crop made connector stones much larger across the road");
            Assert.That(TownWorldSurface.PathTileLength,Is.GreaterThan(.5f),"A few large painted stones must not repeat every short step");
        }
        [Test] public void LawnUsesTheWholeMaterialAndRemainsContinuousAcrossNegativeWorldTiles()
        {
            var a=TownWorldSurface.LawnUV(.12f,.21f);var b=TownWorldSurface.LawnUV(.22f,.235f);
            Assert.That(TownWorldLayout.Distance(a,b),Is.GreaterThan(.04f),"The old .10 by .025 world crop repeated the same grass in a visible grid");
            foreach(float edge in new[]{-2f,-1f,0f,1f,2f})
            {
                var left=TownWorldSurface.LawnUV(edge-.0001f,.31f);var right=TownWorldSurface.LawnUV(edge+.0001f,.31f);
                Assert.That(TownWorldLayout.Distance(left,right),Is.LessThan(.001f),"Mirrored lawn tiles must meet at the same texels");
                var below=TownWorldSurface.LawnUV(.31f,edge-.0001f);var above=TownWorldSurface.LawnUV(.31f,edge+.0001f);
                Assert.That(TownWorldLayout.Distance(below,above),Is.LessThan(.001f));
            }
        }
        [Test] public void LawnAndPavingStayInsideTheirOwnAtlasHalves()
        {
            foreach(float x in new[]{-2.7f,-1f,-.2f,0f,.5f,1f,2.7f})foreach(float y in new[]{-2.7f,-.2f,0f,.5f,1f,2.7f})
            {
                var lawn=TownWorldSurface.LawnUV(x,y);Assert.That(lawn.X,Is.InRange(.0019f,.4981f));Assert.That(lawn.Y,Is.InRange(.0019f,.9981f));
            }
            var left=TownWorldSurface.PavingUV(-1,0);var right=TownWorldSurface.PavingUV(1,1);
            Assert.That(left.X,Is.InRange(.5019f,.5021f));Assert.That(right.X,Is.InRange(.9979f,.9981f));
            Assert.That(right.Y-left.Y,Is.GreaterThan(.99f),"Use the complete paving panel instead of magnifying a small crop");
        }
        [Test] public void DailyGreetingHasShortLinesAndNoStrandedQuestionMarkForAnyFriend()
        {
            var daily=new TownDailyService(new MemoryTownDailyStore(),new DateTime(2026,10,17));
            for(int i=0;i<6;i++)
            {
                daily.EnsureDay(new DateTime(2026,10,17).AddDays(i));var lines=daily.Greeting.Split('\n');
                Assert.That(lines.All(line=>line.Length<=13),Is.True,"The friend column must not wrap its question mark onto a new line");
                Assert.That(lines.All(line=>!line.StartsWith("？")),Is.True);
            }
        }
    }
}
