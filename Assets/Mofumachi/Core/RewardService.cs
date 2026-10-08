namespace Mofumachi.Core
{
    public sealed class RewardService
    {
        public bool GrantReward(GameState state, string rewardId, int coins)
        {
            lock (state.SyncRoot)
            {
                if (string.IsNullOrWhiteSpace(rewardId) || coins < 0 || state.claimedRewardIds.Contains(rewardId) || (long)state.coins + coins > int.MaxValue) return false;
                state.coins += coins; state.claimedRewardIds.Add(rewardId); return true;
            }
        }
    }
}
