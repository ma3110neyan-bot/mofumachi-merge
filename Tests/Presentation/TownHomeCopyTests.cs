using System;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
 public sealed class TownHomeCopyTests
 {
  [Test] public void CompactHintRemainsOneReadableLineForEveryGrowthAndDailyState()
  {
   var daily=new TownDailyService(new MemoryTownDailyStore(),new DateTime(2026,10,18));var state=GameState.CreateInitial();
   for(int level=0;level<=6;level++){state.townGrowthLevel=level;Assert.That(TownNextGoal.CompactDetail(state,daily).Length,Is.InRange(4,16));}
   state.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));state.activeQuestId="";
   foreach(int talks in new[]{0,1})foreach(int walks in new[]{0,1})foreach(int merges in new[]{0,3})foreach(bool claimed in new[]{false,true})
   {daily.Data.Talks=talks;daily.Data.Walks=walks;daily.Data.Merges=merges;daily.Data.Claimed=claimed;Assert.That(TownNextGoal.CompactDetail(state,daily).Length,Is.InRange(4,16));}
  }
 }
}
