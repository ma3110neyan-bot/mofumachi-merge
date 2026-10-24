using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class NorthCameraBoundaryTests
    {
        // Reproduces the supplied north side-pan views: the 1.10-wide phone
        // must stay within the 1.20-wide painted north room, not the empty corners.
        [TestCase(592)] [TestCase(740)] [TestCase(808)]
        public void NorthSidePanAndFollowKeepBothPaintedBanksInThePhone(int height)
        {
            float scale=TownWorldLayout.ScaleForViewport(360,height),halfWidth=180/scale;
            var camera=new TownWorldCamera();camera.Fit(halfWidth*2,height/scale);
            void Painted()
            {
                Assert.That(camera.Center.X-halfWidth,Is.GreaterThanOrEqualTo(-.60001f),"Left side exposes the unpainted northwest corner");
                Assert.That(camera.Center.X+halfWidth,Is.LessThanOrEqualTo(.60001f),"Right side exposes the unpainted northeast corner");
            }
            foreach(int side in new[]{-1,1})
            {
                camera.Jump(new TownPoint(side*1.2f,2));Painted();
                for(int i=0;i<60;i++){camera.Pan(side*.12f,0);Painted();}
                for(int i=0;i<120;i++){camera.Follow(new TownPoint(side*.42f,2.6f),1f/30);Painted();}
                Assert.That(side*.42f-camera.Center.X,Is.InRange(-halfWidth+.06f,halfWidth-.06f),"The lateral waiting place and resident remain visible");
            }
        }
        [Test] public void NorthApproachDoesNotSnapOrChangeTheEastWestFraming()
        {
            var camera=new TownWorldCamera();camera.Fit(1.10f,2.1f);
            foreach(float side in new[]{-1f,1f})
            {
                camera.Jump(new TownPoint(side*1.2f,.12f));
                Assert.That(camera.Center.X,Is.EqualTo(side*1.2f).Within(.00001f));
                Assert.That(camera.Center.Y,Is.EqualTo(.12f).Within(.00001f));
                camera.Jump(new TownPoint(side*1.2f,.60f));var previous=camera.Center;
                for(int i=1;i<=160;i++)
                {
                    camera.Jump(new TownPoint(side*1.2f,.60f+i*.005f));
                    Assert.That(TownWorldLayout.Distance(camera.Center,previous),Is.LessThan(.014f),"No abrupt lateral snap at the north entry");previous=camera.Center;
                }
            }
        }
    }
}
