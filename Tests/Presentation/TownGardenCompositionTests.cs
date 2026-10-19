using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class TownGardenCompositionTests
    {
        private static bool Inside(TownPoint point,TownPoint[] polygon)
        {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                var a=polygon[i];var b=polygon[j];
                if((a.Y>point.Y)!=(b.Y>point.Y)&&point.X<(b.X-a.X)*(point.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
            }
            return inside;
        }
        private static bool Paved(TownPoint p)=>TownWorldSurface.PavingPolygons.Any(polygon=>Inside(p,polygon));
        [TestCase(592)] [TestCase(740)] [TestCase(808)]
        public void EveryPhoneFieldShowsLawnAndPavingWhileTheLawnRemainsWalkable(int height)
        {
            var floor=new TownWalkableArea(0);float fieldHeight=height-56-142;
            float scale=TownWorldLayout.ScaleForViewport(360,fieldHeight),halfWidth=360/scale/2,halfHeight=fieldHeight/scale/2;
            foreach(var district in TownDistrictLayout.All)
            {
                var arrival=TownWorldMap.Place(TownDistrictLayout.EntranceId(district));
                var camera=new TownWorldCamera();camera.Fit(halfWidth*2,halfHeight*2);camera.Jump(new TownPoint(arrival.X,arrival.Y+TownSceneryLayout.CameraLift));
                int walkable=0,lawn=0;
                for(int x=0;x<50;x++)for(int y=0;y<60;y++)
                {
                    var p=new TownPoint(camera.Center.X-halfWidth+(x+.5f)*halfWidth*2/50,camera.Center.Y-halfHeight+(y+.5f)*halfHeight*2/60);
                    if(!floor.Contains(p))continue;
                    walkable++;if(!Paved(p))lawn++;
                }
                float share=(float)lawn/walkable;string context=district+" at "+height;
                Assert.That(share,Is.InRange(.22f,.70f),context+" needs substantial lawn as well as a clear plaza/path");
                Assert.That(walkable,Is.GreaterThan(1200),context+" must retain a broad walking surface");
            }
        }
        [Test] public void AllFourConnectionsAndEveryEntranceRemainPaved()
        {
            foreach(var district in TownDistrictLayout.All)
                Assert.That(Paved(TownWorldMap.Place(TownDistrictLayout.EntranceId(district))),Is.True,district.ToString());
            foreach(float direction in new[]{-1f,1f})foreach(float step in new[]{-.08f,-.04f,0f,.04f,.08f})
            {
                Assert.That(Paved(new TownPoint((.60f+step)*direction,-.20f)),Is.True,"East/west road stays continuous");
                Assert.That(Paved(new TownPoint(0,(1f+step)*direction)),Is.True,"North/south road stays continuous");
            }
        }
        [Test] public void PavingNeverExtendsBeyondTheFiveExistingWalkingRooms()
        {
            var floors=new TownWalkableArea(0).FloorPolygons;
            foreach(var polygon in TownWorldSurface.PavingPolygons)
            {
                float left=polygon.Min(p=>p.X),right=polygon.Max(p=>p.X),bottom=polygon.Min(p=>p.Y),top=polygon.Max(p=>p.Y);
                for(int x=0;x<40;x++)for(int y=0;y<40;y++)
                {
                    var p=new TownPoint(left+(x+.5f)*(right-left)/40,bottom+(y+.5f)*(top-bottom)/40);
                    if(Inside(p,polygon))Assert.That(floors.Any(f=>Inside(p,f)),Is.True,"Paving cannot advertise an unreachable extension");
                }
            }
        }
        [Test] public void TreesHaveVisibleCrownsWithoutMovingTheirNonWalkableFoundations()
        {
            var trees=TownSceneryLayout.Nature.Where(p=>p.Kind==TownSceneryKind.Tree||p.Kind==TownSceneryKind.Blossom).ToArray();
            Assert.That(trees.Length,Is.GreaterThan(10));
            foreach(var tree in trees)
            {
                Assert.That(new TownWalkableArea(0).Contains(tree.Foundation),Is.False);
                Assert.That(tree.Width,Is.InRange(.60f,.72f),"The old .40 canopy stayed outside the phone field");
                float sourceAspect=tree.Kind==TownSceneryKind.Tree?690f/600:682f/622;
                Assert.That(tree.Width/tree.Height,Is.EqualTo(sourceAspect).Within(.001f),"Keep the original tree artwork proportions");
            }
        }
    }
}
