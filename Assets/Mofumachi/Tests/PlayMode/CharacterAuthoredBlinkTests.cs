using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class CharacterAuthoredBlinkTests
    {
        [Test]
        public void RearBlinkRetainsHeadWhenUnitySerializesMissingClosedHeadAsEmptyObject()
        {
            var head=new CharacterAuthoredPart{id="head"};
            var view=new CharacterAuthoredView{direction="back",closedHead=new CharacterAuthoredPart{id=""}};
            var visible=view.PartFor(head,true);
            Assert.That(visible,Is.SameAs(head));
            Assert.DoesNotThrow(()=>default(NoahAuthoredPose).Part(visible.id));
        }

        [Test]
        public void RearBlinkNeverAddsEyesEvenWhenAnAlternateHeadIsPresent()
        {
            var head=new CharacterAuthoredPart{id="head"};
            var view=new CharacterAuthoredView{direction="back",closedHead=new CharacterAuthoredPart{id="head"}};
            Assert.That(view.PartFor(head,true),Is.SameAs(head));
        }

        [TestCase("left"),TestCase("right")]
        public void SideBlinkUsesRegisteredClosedHeadAndReopensOriginal(string direction)
        {
            var open=new CharacterAuthoredPart{id="head"};var closed=new CharacterAuthoredPart{id="head"};
            var view=new CharacterAuthoredView{direction=direction,closedHead=closed};
            Assert.That(view.PartFor(open,true),Is.SameAs(closed));
            Assert.That(view.PartFor(open,false),Is.SameAs(open));
        }

        [Test]
        public void BlinkLeavesLimbsUnchanged()
        {
            var arm=new CharacterAuthoredPart{id="nearArm"};
            var view=new CharacterAuthoredView{direction="left",closedHead=new CharacterAuthoredPart{id="head"}};
            Assert.That(view.PartFor(arm,true),Is.SameAs(arm));
        }

        [Test]
        public void MissingOptionalRearHeadRetainsOriginal()
        {
            var head=new CharacterAuthoredPart{id="head"};
            Assert.That(new CharacterAuthoredView{direction="back"}.PartFor(head,true),Is.SameAs(head));
        }
    }
}
