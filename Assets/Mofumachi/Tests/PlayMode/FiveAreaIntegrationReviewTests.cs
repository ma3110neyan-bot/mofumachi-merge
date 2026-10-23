using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class FiveAreaIntegrationReviewTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator AllFiveRolesConnectByWalkingAtEveryReviewedLevelWithoutChangingProgress()
        {
            yield return EnterHome();
            foreach(int level in new[]{1,3,6})
            {
                Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=FrameTownPortrait(640);
                string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
                foreach(var district in new[]{TownDistrictId.West,TownDistrictId.Central,TownDistrictId.East,TownDistrictId.Central,TownDistrictId.North,TownDistrictId.Central,TownDistrictId.South})
                {
                    var goal=district==TownDistrictId.North?new TownPoint(0,1.90f):district==TownDistrictId.South?new TownPoint(-.20f,-2.40f):district==TownDistrictId.West?TownWorldMap.Place("west.cafe-front"):district==TownDistrictId.East?TownWorldMap.Place("shop"):TownWorldMap.Place("square");
                    Assert.That(life.World.GuidePoint(goal),Is.True);
                    for(int frame=0;frame<3600&&life.World.Model.Hero.IsWalking;frame++)
                    {
                        life.AdvanceFrame(1f/30);
                        foreach(var actor in life.World.Model.Residents)Assert.That(life.World.Model.Area.Contains(actor.Position),Is.True);
                        var actors=life.World.Model.Residents;
                        for(int a=0;a<actors.Count;a++)for(int b=a+1;b<actors.Count;b++)Assert.That(TownWalkableArea.Distance(actors[a].Position,actors[b].Position),Is.GreaterThan(.066f));
                    }
                    Assert.That(life.World.Model.Hero.IsWalking,Is.False,"A continuous journey must arrive without fast travel: "+district);
                    Assert.That(TownWalkableArea.Distance(life.World.Model.Hero.Position,goal),Is.LessThan(.002f));
                    Assert.That(TownDistrictLayout.At(life.World.Model.Hero.Position),Is.EqualTo(district));
                    life.RefreshWorldPresentation();Canvas.ForceUpdateCanvases();
                    foreach(var actor in life.Actors.Where(a=>a.RegionVisible))Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(actor.WorldPosition)),Is.LessThan(.01f));
                }
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
