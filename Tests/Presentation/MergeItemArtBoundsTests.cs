using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MergeItemArtBoundsTests
    {
        [Test] public void ArtUsesTheAvailableCellBeforeAddingMoreInterfaceChrome()
        {
            // Original sprites are the focus. This guard rejects shrinking them
            // again to make room for a thicker dish or an oversized level band.
            for(int index=0;index<30;index++)
            {
                var art=MergePresentation.ItemArt(index);
                Assert.That(art.Width,Is.GreaterThan(.93f));
                Assert.That(art.Height,Is.GreaterThan(.81f));
            }
        }
        // The minimum 48-unit cell, then the 360-wide native fixtures at heights 592, 640 and 808.
        [TestCase(256,308)] [TestCase(324,364)] [TestCase(324,412)] [TestCase(324,432)]
        public void SelectionKeepsArtInsideEachCellHorizontally(float boardWidth,float boardHeight)
        {
            for(int index=0;index<30;index++)
            foreach(float elapsed in new[]{.12f,0f,.03f,.06f,.09f,.18f,.25f})
            {
                var cell=MergePerspective.Cell(index,boardWidth,boardHeight);
                var art=MergePresentation.ItemArt(index);
                float center=(art.X+art.Width/2)*cell.Width;
                float half=art.Width*cell.Width*MergeVisualTiming.SelectionScale(elapsed)/2;
                string context=$"board={boardWidth}x{boardHeight}, cell={index}, side={cell.Width}, selection={elapsed}";
                Assert.That(center-half,Is.GreaterThanOrEqualTo(0),context+" left");
                Assert.That(center+half,Is.LessThanOrEqualTo(cell.Width),context+" right");
            }
        }
        [TestCase(256,308)] [TestCase(324,364)] [TestCase(324,412)] [TestCase(324,432)]
        public void SelectionKeepsLiftedArtInsideEachCellVertically(float boardWidth,float boardHeight)
        {
            for(int index=0;index<30;index++)
            foreach(float elapsed in new[]{.12f,0f,.03f,.06f,.09f,.18f,.25f})
            {
                var cell=MergePerspective.Cell(index,boardWidth,boardHeight);
                var art=MergePresentation.ItemArt(index);
                float center=(art.Y+art.Height/2)*cell.Height+MergeVisualTiming.SelectionLift(elapsed);
                float half=art.Height*cell.Height*MergeVisualTiming.SelectionScale(elapsed)/2;
                string context=$"board={boardWidth}x{boardHeight}, cell={index}, side={cell.Height}, selection={elapsed}";
                Assert.That(center-half,Is.GreaterThanOrEqualTo(0),context+" bottom");
                Assert.That(center+half,Is.LessThanOrEqualTo(cell.Height),context+" top");
            }
        }
        [Test] public void ArtRetainsMostOfTheCellWithoutShrinkingTheInputTarget()
        {
            for(int index=0;index<30;index++)
            {
                var art=MergePresentation.ItemArt(index);
                Assert.That(art.Width,Is.GreaterThanOrEqualTo(.90f));
                Assert.That(art.Height,Is.GreaterThanOrEqualTo(.80f));
                var cell=MergePerspective.Cell(index,256,308);
                Assert.That(cell.Width,Is.GreaterThanOrEqualTo(48));
                Assert.That(cell.Height,Is.GreaterThanOrEqualTo(48));
            }
        }
    }
}
