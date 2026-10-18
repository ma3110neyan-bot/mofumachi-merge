using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MergePerspectiveTests
    {
        [TestCase(256,308)] [TestCase(300,322)] [TestCase(328,390)] [TestCase(680,640)]
        public void ThirtyStableCellsFitWithReadableHitTargets(float width,float height)
        {
            var cells=Enumerable.Range(0,30).Select(i=>MergePerspective.Cell(i,width,height)).ToArray();
            foreach(var c in cells){Assert.That(c.Width,Is.GreaterThanOrEqualTo(48));Assert.That(c.Height,Is.GreaterThanOrEqualTo(48));Assert.That(c.X,Is.GreaterThanOrEqualTo(0));Assert.That(c.Y,Is.GreaterThanOrEqualTo(0));Assert.That(c.X+c.Width,Is.LessThanOrEqualTo(width+.01f));Assert.That(c.Y+c.Height,Is.LessThanOrEqualTo(height+.01f));}
            for(int i=0;i<30;i++)for(int j=i+1;j<30;j++)Assert.That(cells[i].Overlaps(cells[j]),Is.False,$"{i}/{j}");
        }
        [Test] public void InvalidIndexDoesNotAliasAnotherBoardCell()
        {Assert.Throws<System.ArgumentOutOfRangeException>(()=>MergePerspective.Cell(-1,300,322));Assert.Throws<System.ArgumentOutOfRangeException>(()=>MergePerspective.Cell(30,300,322));}
        [TestCase(256,308)] [TestCase(300,322)] [TestCase(328,390)] [TestCase(680,640)]
        public void AllSixRowsUseAlignedEqualSquaresAndConstantItemScale(float width,float height)
        {
            var first=MergePerspective.Cell(0,width,height);
            foreach(int i in Enumerable.Range(0,30))
            {
                var cell=MergePerspective.Cell(i,width,height);
                Assert.That(cell.Width,Is.EqualTo(first.Width).Within(.001f),"Every row has the same item space");
                Assert.That(cell.Height,Is.EqualTo(cell.Width).Within(.001f),"Cells must remain square on tall and wide screens");
                Assert.That(cell.X,Is.EqualTo(first.X+(i%5)*(first.Width+4)).Within(.001f));
                Assert.That(cell.Y,Is.EqualTo(first.Y-(i/5)*(first.Height+4)).Within(.001f),"Indexes run left to right, then downward");
                Assert.That(MergePerspective.ArtScale(i),Is.EqualTo(1),"Items retain their size in every row");
            }
            var last=MergePerspective.Cell(29,width,height);
            Assert.That(first.X,Is.EqualTo(width-last.X-last.Width).Within(.001f));
            Assert.That(last.Y,Is.EqualTo(height-first.Y-first.Height).Within(.001f));
        }
        [TestCase(256,308)] [TestCase(328,390)] [TestCase(680,640)]
        public void EveryCellCenterAndInsetCornerMapsOnlyToItsStableIndex(float width,float height)
        {
            var cells=Enumerable.Range(0,30).Select(i=>MergePerspective.Cell(i,width,height)).ToArray();
            for(int i=0;i<30;i++)
            foreach(var x in new[]{cells[i].X+1,cells[i].X+cells[i].Width/2,cells[i].X+cells[i].Width-1})
            foreach(var y in new[]{cells[i].Y+1,cells[i].Y+cells[i].Height/2,cells[i].Y+cells[i].Height-1})
            {
                var hits=Enumerable.Range(0,30).Where(j=>x>=cells[j].X&&x<=cells[j].X+cells[j].Width&&y>=cells[j].Y&&y<=cells[j].Y+cells[j].Height).ToArray();
                Assert.That(hits,Is.EqualTo(new[]{i}),"A point in a cell must not hit its neighbour");
            }
        }
    }
}
