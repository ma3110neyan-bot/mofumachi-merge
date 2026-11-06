using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Mofumachi.Core
{
    // Wallet, town action total and reward receipts share the existing main-save transaction.
    public sealed class MofuPointService
    {
        public const int MaximumBalance = 96;
        private readonly GameStateManager game;
        private bool rewarding;
        internal MofuPointService(GameStateManager game) { this.game = game; }

        public int Balance { get { lock (game.State.SyncRoot) return game.State.economy.mofuPoints; } }
        public long TownActionTotal { get { lock (game.State.SyncRoot) return game.State.economy.townActionTotal; } }
        public string AppliedDay { get { lock (game.State.SyncRoot) return game.State.economy.day; } }

        public bool CanSpend(int amount)
        {
            lock (game.State.SyncRoot)
            {
                int balance = game.State.economy.mofuPoints;
                return amount >= 0 && balance >= 0 && balance <= MaximumBalance && amount <= balance;
            }
        }

        public bool TryAdd(int amount, out int credited)
        {
            int actual = 0;
            bool saved = game.MutateEconomy(e =>
            {
                if (amount < 0) return false;
                actual = Credit(e, amount);
                return true;
            });
            credited = saved ? actual : 0;
            return saved;
        }

        public bool TrySpend(int amount) => game.MutateEconomy(e => TrySpendForOperationInMutation(amount, () => true));

        // The owner supplies the existing Mutate save/rollback boundary. Never Save here.
        internal bool TrySpendForOperationInMutation(int amount, Func<bool> operation)
        {
            if (!Monitor.IsEntered(game.State.SyncRoot)) throw new InvalidOperationException("Point spending requires an owned state transaction.");
            if (!CanSpend(amount) || !operation()) return false;
            game.State.economy.mofuPoints -= amount;
            return true;
        }

        // Reject overflow. Cumulative progress is never clamped to the wallet cap.
        public bool TryAddTownActionTotal(long amount) => game.MutateEconomy(e =>
        {
            if (amount <= 0 || amount > long.MaxValue - e.townActionTotal) return false;
            e.townActionTotal += amount;
            return true;
        });

        public bool TryApplyDay(string day) => game.MutateEconomy(e =>
        {
            if (!ValidDay(day) || string.CompareOrdinal(day, e.day) <= 0) return false;
            ApplyDay(e, day);
            return true;
        });

        public bool TryRewardTalk(string day, string characterId) => Reward(day, 4,
            (e, next) => CharacterCatalog.IsKnown(characterId) &&
                (next || (e.talkRewardCharacterIds.Count < 3 && !e.talkRewardCharacterIds.Contains(characterId))),
            e => e.talkRewardCharacterIds.Add(characterId));

        public bool TryRewardNormalWalk(string day, string startedDay, string walkId) => Reward(day, 6,
            (e, next) => startedDay == day && !string.IsNullOrWhiteSpace(walkId) && walkId.Length <= 128 &&
                (next || (e.normalWalkRewards < 2 && e.walkRewardReceipts.Count < 5 &&
                    !e.walkRewardReceipts.Any(r => r.walkId == walkId))),
            e => { e.normalWalkRewards++; e.walkRewardReceipts.Add(new WalkRewardReceipt(day, walkId, WalkRewardKind.Normal)); });

        public bool TryRewardFlower(string day) => Reward(day, 8,
            (e, next) => string.CompareOrdinal(e.lastFlowerRewardDay, day) < 0,
            e => e.lastFlowerRewardDay = day);

        private bool Reward(string day, int value, Func<EconomyState, bool, bool> eligible, Action<EconomyState> record)
        {
            lock (game.State.SyncRoot)
            {
                if (rewarding) return false;
                rewarding = true;
                try
                {
                    return game.MutateEconomy(e =>
                    {
                        // Mutate(false) does not roll back: decide everything before advancing the day.
                        if (!ValidDay(day) || string.CompareOrdinal(day, e.day) < 0 ||
                            string.CompareOrdinal(day, e.rewardsStartDay) < 0 ||
                            e.townActionTotal > long.MaxValue - value || !eligible(e, day != e.day)) return false;
                        if (day != e.day) ApplyDay(e, day);
                        if (e.rewardsStartDay.Length == 0) e.rewardsStartDay = day;
                        Credit(e, value);
                        e.townActionTotal += value;
                        record(e);
                        return true;
                    });
                }
                finally { rewarding = false; }
            }
        }
        private static int Credit(EconomyState e, int amount)
        {
            int actual = Math.Min(amount, MaximumBalance - e.mofuPoints);
            e.mofuPoints += actual;
            return actual;
        }
        private static void ApplyDay(EconomyState e, string day)
        {
            e.day = day;
            e.talkRewardCharacterIds.Clear();
            e.normalWalkRewards = e.additionalWalkRewards = 0;
            e.helpWalkClaimed = false;
            e.walkRewardReceipts.Clear();
        }

        public bool TryRecordDailyRecovery(string day) => game.MutateEconomy(e =>
        {
            if (!CurrentDay(e, day) || string.CompareOrdinal(e.lastRecoveryDay, day) >= 0) return false;
            e.lastRecoveryDay = day;
            return true;
        });

        public bool TryRecordFlowerDay(string day) => game.MutateEconomy(e =>
        {
            if (!CurrentDay(e, day) || string.CompareOrdinal(e.lastFlowerRewardDay, day) >= 0) return false;
            e.lastFlowerRewardDay = day;
            return true;
        });

        public bool TryRecordTalk(string day, string characterId) => game.MutateEconomy(e =>
        {
            if (!CurrentDay(e, day) || !CharacterCatalog.IsKnown(characterId) ||
                e.talkRewardCharacterIds.Count >= 3 || e.talkRewardCharacterIds.Contains(characterId)) return false;
            e.talkRewardCharacterIds.Add(characterId);
            return true;
        });

        // Count and receipt form one saved transaction, as required by v4 validation.
        public bool TryRecordWalk(string day, string walkId, WalkRewardKind kind) => game.MutateEconomy(e =>
        {
            if (!CurrentDay(e, day) || string.IsNullOrWhiteSpace(walkId) || walkId.Length > 128 ||
                !Enum.IsDefined(typeof(WalkRewardKind), kind) || e.walkRewardReceipts.Count >= 5 ||
                e.walkRewardReceipts.Any(r => string.Equals(r.walkId, walkId, StringComparison.Ordinal))) return false;
            if ((kind == WalkRewardKind.Normal && e.normalWalkRewards >= 2) ||
                (kind == WalkRewardKind.Additional && e.additionalWalkRewards >= 2) ||
                (kind == WalkRewardKind.Help && e.helpWalkClaimed)) return false;
            e.walkRewardReceipts.Add(new WalkRewardReceipt(day, walkId, kind));
            if (kind == WalkRewardKind.Normal) e.normalWalkRewards++;
            else if (kind == WalkRewardKind.Additional) e.additionalWalkRewards++;
            else e.helpWalkClaimed = true;
            return true;
        });

        private static bool CurrentDay(EconomyState e, string day) => ValidDay(day) && day == e.day;
        private static bool ValidDay(string day) => day != null && day.Length == 10 &&
            DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
