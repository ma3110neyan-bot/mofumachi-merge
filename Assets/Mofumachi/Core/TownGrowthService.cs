using System;
using System.IO;
namespace Mofumachi.Core
{
    public sealed class TownGrowthService
    {
        private static readonly int[] Requests={0,1,2,4,6,9,12};
        private static readonly long[] Actions={0,4,8,40,96,160,224};
        public int CalculateTownLevel(GameState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            int completed=QuestCatalog.CompletedCount(state);
            for(int level=6;level>=1;level--)
                if(completed>=Requests[level]&&state.economy.townActionTotal>=Actions[level])return level;
            return 0;
        }
        public void EvaluateTownGrowth(GameState state)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(state.townGrowthLevel<0||state.townGrowthLevel>6)throw new InvalidDataException("Saved town level is outside the supported range.");
            state.townGrowthLevel=Math.Max(state.townGrowthLevel,CalculateTownLevel(state));
        }
    }
}
