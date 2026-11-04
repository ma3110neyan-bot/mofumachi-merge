using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MergeFontProfileTests
    {
        [TestCase("Back label")]
        [TestCase("Generate frame tea label")]
        [TestCase("Generate frame bread label")]
        [TestCase("Generate frame flower label")]
        [TestCase("Merge actions label")]
        [TestCase("Deliver label")]
        [TestCase("Nav label")]
        public void SmallMergeMenusKeepTheCuteFace(string name)
        {Assert.That(MergeFontProfile.IsMenu(name),Is.True);}
        [TestCase("Item level")]
        [TestCase("Requirement count tea")]
        [TestCase("Merge reward coins")]
        [TestCase("Reward amount")]
        [TestCase("Feedback")]
        [TestCase("Unreviewed label")]
        public void NumbersAndUnknownLabelsAreNotMovedToHachi(string name)
        {Assert.That(MergeFontProfile.IsMenu(name),Is.False);}
        [Test] public void ThreeTitleOptionsAreSpecificAndOutlineIsSmall()
        {
            Assert.That(MergeFontProfile.Font(MergeFontVariant.A),Is.EqualTo("UIFontAccent"));
            Assert.That(MergeFontProfile.Font(MergeFontVariant.B),Is.EqualTo("UIFontAccent"));
            Assert.That(MergeFontProfile.Font(MergeFontVariant.C),Is.EqualTo("UIFontStory"));
            Assert.That(MergeFontProfile.Bold(MergeFontVariant.A),Is.True);
            Assert.That(MergeFontProfile.Bold(MergeFontVariant.B),Is.True);
            Assert.That(MergeFontProfile.Bold(MergeFontVariant.C),Is.False);
            Assert.That(MergeFontProfile.Outline(MergeFontVariant.A),Is.Zero);
            Assert.That(MergeFontProfile.Outline(MergeFontVariant.B),Is.EqualTo(.35f));
            Assert.That(MergeFontProfile.Outline(MergeFontVariant.C),Is.Zero);
        }
    }
}
