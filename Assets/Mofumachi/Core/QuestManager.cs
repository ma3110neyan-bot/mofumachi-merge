using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Mofumachi.Core
{
    public sealed class DeliveryResult
    {
        public bool Success { get; }
        public bool SaveFailed { get; }
        public string Message { get; }
        public DeliveryResult(bool success, string message = "", bool saveFailed = false) { Success = success; Message = message; SaveFailed = saveFailed; }
    }
    public sealed class QuestManager
    {
        private readonly GameState state;
        private readonly IStateStore store;
        private readonly RewardService reward;
        private readonly TownGrowthService town;
        private readonly Dictionary<string, QuestDefinition> catalog;
        private QuestDefinition active;
        private bool delivering;
        public string LastError { get; private set; } = "";
        public QuestManager(GameState state, IStateStore store, RewardService reward, TownGrowthService town, IEnumerable<QuestDefinition> questCatalog = null)
        {
            this.state = state; this.store = store; this.reward = reward; this.town = town;
            catalog = (questCatalog ?? new[] { QuestDefinition.First }).ToDictionary(q => q.Id, StringComparer.Ordinal);
            if (state.activeQuestId.Length > 0 && !catalog.TryGetValue(state.activeQuestId, out active))
                LastError = "保存中の依頼が現在の依頼カタログにありません。対応するカタログを読み込んでください。";
        }
        public bool AcceptQuest(QuestDefinition quest)
        {
            lock (state.SyncRoot)
            {
                if (quest == null || delivering || state.activeQuestId.Length > 0 || state.completedQuestIds.Contains(quest.Id)) return false;
                if (!catalog.TryGetValue(quest.Id, out var authored)) { LastError = "未登録の依頼は受注できません。"; return false; }
                quest = authored;
                var before = state.Clone(); var previous = active;
                state.activeQuestId = quest.Id; state.questState = QuestState.Accepted; state.questProgress = 0; active = quest;
                try { store.Save(state); LastError = ""; return true; }
                catch (IOException) { state.CopyFrom(before); active = previous; LastError = "受注を保存できませんでした。再試行してください。"; return false; }
                catch (UnauthorizedAccessException) { state.CopyFrom(before); active = previous; LastError = "保存先へ書き込めません。"; return false; }
            }
        }
        private long Available(ItemRequirement r) => state.inventory.Where(i => i.itemId == r.ItemId && i.level == r.Level).Sum(i => (long)i.count) + state.mergeBoard.LongCount(i => i.itemId == r.ItemId && i.level == r.Level && !state.MergeLocks.Contains(i.cellIndex));
        public bool CheckDelivery()
        {
            lock (state.SyncRoot)
                return active != null && state.activeQuestId == active.Id && !state.completedQuestIds.Contains(active.Id) &&
                    !state.claimedRewardIds.Contains("quest:" + active.Id) && active.Requirements.All(r => Available(r) >= r.Count);
        }
        public void RefreshProgress()
        {
            if (active == null || state.completedQuestIds.Contains(active.Id)) return;
            state.questProgress = (int)Math.Min(int.MaxValue, active.Requirements.Sum(r => Math.Min(Available(r), r.Count)));
            state.questState = CheckDelivery() ? QuestState.Deliverable : QuestState.Producing;
        }
        public DeliveryResult TryDeliver()
        {
            lock (state.SyncRoot)
            {
                if (delivering || !CheckDelivery()) return new DeliveryResult(false, "納品条件を満たしていません。完了済み依頼は再納品できません。");
                if ((long)state.coins + active.Coins > int.MaxValue) return new DeliveryResult(false, "所持コインの上限を超えます。");
                var before = state.Clone(); delivering = true;
                try
                {
                    foreach (var requirement in active.Requirements)
                    {
                        int remaining = requirement.Count;
                        foreach (var item in state.inventory.Where(i => i.itemId == requirement.ItemId && i.level == requirement.Level).ToArray())
                        {
                            int take = Math.Min(item.count, remaining); item.count -= take; remaining -= take;
                            if (item.count == 0) state.inventory.Remove(item);
                            if (remaining == 0) break;
                        }
                        foreach (var item in state.mergeBoard.Where(i => i.itemId == requirement.ItemId && i.level == requirement.Level && !state.MergeLocks.Contains(i.cellIndex)).OrderBy(i => i.cellIndex).Take(remaining).ToArray())
                            state.mergeBoard.Remove(item);
                    }
                    state.questProgress = (int)Math.Min(int.MaxValue, active.Requirements.Sum(r => (long)r.Count));
                    state.completedQuestIds.Add(active.Id);
                    reward.GrantReward(state, "quest:" + active.Id, active.Coins); state.questState = QuestState.RewardClaimed;
                    town.EvaluateTownGrowth(state); state.questState = QuestState.TownGrown;
                    store.Save(state); LastError = "";
                    return new DeliveryResult(true, "納品完了。街が成長しました。");
                }
                catch (IOException) { state.CopyFrom(before); LastError = "保存に失敗したため、納品前の状態へ戻しました。"; return new DeliveryResult(false, LastError, true); }
                catch (UnauthorizedAccessException) { state.CopyFrom(before); LastError = "保存先へ書き込めません。納品前の状態へ戻しました。"; return new DeliveryResult(false, LastError, true); }
                finally { delivering = false; }
            }
        }
    }
}
