using System;
namespace Mofumachi.Core
{
    public sealed class TownGrowthService
    {
        public void EvaluateTownGrowth(GameState state) => state.townGrowthLevel = Math.Max(state.townGrowthLevel, state.completedQuestIds.Count);
    }
}
