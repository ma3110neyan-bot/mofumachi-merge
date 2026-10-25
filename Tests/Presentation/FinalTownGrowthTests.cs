using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class FinalTownGrowthTests
    {
        [Test] public void EveryDistrictCanDescribeItsActualDeliveryChangeWithoutInventingUnlocks()
        {
            var method=typeof(TownGrowthPresentation).GetMethod("ChangeSummary",BindingFlags.Public|BindingFlags.Static);
            Assert.That(method,Is.Not.Null,"The delivery comparison must describe North/South too, including levels with no new district prop");
            foreach(var district in TownDistrictLayout.All)foreach(var pair in new[]{(1,2),(2,3),(5,6)})
            {
                var text=(string)method.Invoke(null,new object[]{district,pair.Item1,pair.Item2});
                Assert.That(text,Is.Not.Empty,district+" "+pair);
                if(pair.Item2==2&&(district==TownDistrictId.North||district==TownDistrictId.South))Assert.That(text,Does.Contain("Lv.3"),"Do not alter the authored growth pacing just to manufacture a Lv2 sample");
                if(pair.Item2==6&&(district==TownDistrictId.North||district==TownDistrictId.South))Assert.That(text,Does.Contain("花アーチ"));
            }
        }
        [Test] public void EachDistrictComparisonFocusBelongsToThatDistrict()
        {
            var method=typeof(TownGrowthPresentation).GetMethod("FocusForDistrict",BindingFlags.Public|BindingFlags.Static);
            Assert.That(method,Is.Not.Null,"North/South comparison must never silently focus the central fountain");
            foreach(var district in TownDistrictLayout.All)foreach(int level in new[]{2,3,6})
            {
                var focus=(TownPoint)method.Invoke(null,new object[]{district,level});
                Assert.That(TownDistrictLayout.At(focus),Is.EqualTo(district));
            }
        }
        [Test] public void SixRealDeliveriesPersistLevelAndRewardsThroughFreshManagers()
        {
            string directory=Path.Combine(Path.GetTempPath(),"mofu-growth-"+Guid.NewGuid());
            try
            {
                var store=new SaveService(directory);var game=new GameStateManager(store.Load().State,store);int coins=0;
                foreach(var quest in QuestCatalog.All)
                {
                    int before=game.State.townGrowthLevel;
                    Assert.That(game.Quests.AcceptQuest(quest),Is.True);
                    foreach(var requirement in quest.Requirements)
                    {
                        while(game.State.mergeBoard.Count(i=>i.itemId==requirement.ItemId&&i.level==requirement.Level)<requirement.Count)
                        {
                            var pair=game.State.mergeBoard.Where(i=>i.itemId==requirement.ItemId&&i.level<requirement.Level).GroupBy(i=>i.level).FirstOrDefault(g=>g.Count()>=2);
                            if(pair==null)Assert.That(game.AddItem(requirement.ItemId,1),Is.True);
                            else{var items=pair.Take(2).ToArray();Assert.That(game.TryMerge(items[0].cellIndex,items[1].cellIndex),Is.True);game.Board.ReleaseLocks();}
                        }
                    }
                    Assert.That(game.Quests.TryDeliver().Success,Is.True);coins+=quest.Coins;
                    Assert.That(game.State.townGrowthLevel,Is.EqualTo(before+1));
                    var restored=store.Load().State;StateCodec.Validate(restored);
                    Assert.That(restored.townGrowthLevel,Is.EqualTo(before+1));Assert.That(restored.coins,Is.EqualTo(coins));
                    Assert.That(restored.completedQuestIds.Count,Is.EqualTo(before+1));Assert.That(restored.claimedRewardIds.Count,Is.EqualTo(before+1));
                    game=new GameStateManager(restored,new SaveService(directory));
                    Assert.That(game.Quests.TryDeliver().Success,Is.False,"Reload must never pay the previous delivery twice");
                }
                Assert.That(game.State.townGrowthLevel,Is.EqualTo(6));Assert.That(game.State.coins,Is.EqualTo(330));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
