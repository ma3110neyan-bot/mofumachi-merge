using System;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CharacterAuthoredGroundingTests
    {
        private static NoahGroundingProfile Profile(string id,bool refined=true)
        {
            var type=typeof(TownNoahDepthFocus).Assembly.GetType("Mofumachi.Presentation.CharacterAuthoredGrounding");
            Assert.That(type,Is.Not.Null,"Per-character contact-shadow configuration is absent.");
            return (NoahGroundingProfile)type.GetMethod("For").Invoke(null,new object[]{id,refined});
        }
        [TestCase("momo")][TestCase("miel")]
        public void ContactCentreSitsAtFeetAndStaysInsideTheActorFootprint(string id)
        {
            var p=Profile(id);var before=TownNoahDepthFocus.Grounding(true);
            Assert.That((p.Y+p.Height/2)*78,Is.InRange(-1f,0f));
            Assert.That(p.Width,Is.InRange(.8f,before.Width));
            Assert.That(p.Alpha,Is.GreaterThan(before.Alpha).And.LessThanOrEqualTo(.45f));
            Assert.That(p.X+p.Width/2,Is.EqualTo(.5f).Within(.00001));
        }
        [TestCase("noah")][TestCase("lulu")][TestCase("pom")][TestCase("fio")][TestCase(null)][TestCase("unknown")]
        public void OtherCharactersKeepTheExactAcceptedProfile(string id)=>Same(Profile(id),TownNoahDepthFocus.Grounding(true));
        [TestCase("momo")][TestCase("miel")]
        public void RollbackRestoresTheExactPrevious25DShadow(string id)=>Same(Profile(id,false),TownNoahDepthFocus.Grounding(true));
        private static void Same(NoahGroundingProfile a,NoahGroundingProfile b)
        {Assert.That(new[]{a.X,a.Y,a.Width,a.Height,a.Alpha},Is.EqualTo(new[]{b.X,b.Y,b.Width,b.Height,b.Alpha}));}
    }
}
