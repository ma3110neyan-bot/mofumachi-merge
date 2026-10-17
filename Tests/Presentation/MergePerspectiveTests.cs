using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MergePerspectiveTests
    {
        [TestCase(300,322)] [TestCase(328,390)] [TestCase(680,640)]
        public void ThirtyStableCellsFitWithReadableHitTargets(float width,float height)
        {
            var cells=Enumerable.Range(0,30).Select(i=>MergePerspective.Cell(i,width,height)).ToArray();
            foreach(var c in cells){Assert.That(c.Width,Is.GreaterThanOrEqualTo(48));Assert.That(c.Height,Is.GreaterThanOrEqualTo(48));Assert.That(c.X,Is.GreaterThanOrEqualTo(0));Assert.That(c.Y,Is.GreaterThanOrEqualTo(0));Assert.That(c.X+c.Width,Is.LessThanOrEqualTo(width+.01f));Assert.That(c.Y+c.Height,Is.LessThanOrEqualTo(height+.01f));}
            Assert.That(cells[0].Width,Is.LessThan(cells[25].Width));Assert.That(MergePerspective.ArtScale(0),Is.LessThan(MergePerspective.ArtScale(25)));
            for(int i=0;i<30;i++)for(int j=i+1;j<30;j++)Assert.That(cells[i].Overlaps(cells[j]),Is.False,$"{i}/{j}");
        }
        [Test] public void InvalidIndexDoesNotAliasAnotherBoardCell()
        {Assert.Throws<System.ArgumentOutOfRangeException>(()=>MergePerspective.Cell(-1,300,322));Assert.Throws<System.ArgumentOutOfRangeException>(()=>MergePerspective.Cell(30,300,322));}
        [Test] public void RearRowsAreVisiblyNarrowerAndShallowerWhileCompactTargetsStayUsable()
        {
            var rear=MergePerspective.Cell(0,328,390);var front=MergePerspective.Cell(25,328,390);
            Assert.That(rear.Width/front.Width,Is.LessThanOrEqualTo(.81f),"The old 14% width change remained visually frontal");
            Assert.That(rear.Height,Is.LessThan(front.Height));Assert.That(MergePerspective.ArtScale(0),Is.LessThanOrEqualTo(.84f));
            foreach(int i in Enumerable.Range(0,30)){var r=MergePerspective.Cell(i,256,308);Assert.That(r.Width,Is.GreaterThanOrEqualTo(48));Assert.That(r.Height,Is.GreaterThanOrEqualTo(48));}
        }
    }
}
