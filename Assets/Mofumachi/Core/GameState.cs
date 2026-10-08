using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace Mofumachi.Core
{
    public enum QuestState { Unaccepted, Accepted, Producing, Deliverable, RewardClaimed, TownGrown }

    [Serializable, DataContract]
    public sealed class BoardItem
    {
        [DataMember] public string itemId;
        [DataMember] public int level;
        [DataMember] public int cellIndex;
        public BoardItem(string id, int itemLevel, int cell) { itemId = id; level = itemLevel; cellIndex = cell; }
    }
    [Serializable, DataContract]
    public sealed class InventoryEntry
    {
        [DataMember] public string itemId;
        [DataMember] public int level;
        [DataMember] public int count;
        public InventoryEntry(string id, int itemLevel, int quantity) { itemId = id; level = itemLevel; count = quantity; }
    }
    [Serializable, DataContract]
    public sealed class GameState
    {
        private object syncRoot;
        internal object SyncRoot => LazyInitializer.EnsureInitialized(ref syncRoot);
        private HashSet<int> mergeLocks;
        internal HashSet<int> MergeLocks => LazyInitializer.EnsureInitialized(ref mergeLocks);
        public const int Columns = 5, Rows = 6, MaxLevel = 3;
        [DataMember] public int saveVersion = 1;
        [DataMember] public int coins;
        [DataMember] public int gems;
        [DataMember] public int stamina = 30;
        [DataMember] public int playerLevel = 1;
        [DataMember] public int townGrowthLevel;
        [DataMember] public string activeQuestId = "";
        [DataMember] public QuestState questState;
        [DataMember] public int questProgress;
        [DataMember] public List<InventoryEntry> inventory = new List<InventoryEntry>();
        [DataMember] public List<BoardItem> mergeBoard = new List<BoardItem>();
        [DataMember] public List<string> completedQuestIds = new List<string>();
        [DataMember] public List<string> claimedRewardIds = new List<string>();
        [DataMember] public bool bgmEnabled = true;
        [DataMember] public bool seEnabled = true;
        [DataMember] public string lastSaveTime = "";

        public static GameState CreateInitial()
        {
            var state = new GameState();
            state.mergeBoard.Add(new BoardItem("tea", 1, 0)); state.mergeBoard.Add(new BoardItem("tea", 1, 1));
            return state;
        }
        public GameState Clone() => StateCodec.Decode(StateCodec.Encode(this));
        public void CopyFrom(GameState other)
        {
            var c = other.Clone();
            saveVersion = c.saveVersion; coins = c.coins; gems = c.gems; stamina = c.stamina;
            playerLevel = c.playerLevel; townGrowthLevel = c.townGrowthLevel;
            activeQuestId = c.activeQuestId; questState = c.questState; questProgress = c.questProgress;
            inventory = c.inventory; mergeBoard = c.mergeBoard;
            completedQuestIds = c.completedQuestIds; claimedRewardIds = c.claimedRewardIds;
            bgmEnabled = c.bgmEnabled; seEnabled = c.seEnabled; lastSaveTime = c.lastSaveTime;
        }
    }

    public static class StateCodec
    {
        public static string Encode(GameState state)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(GameState)).WriteObject(stream, state);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static GameState Decode(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (GameState)new DataContractJsonSerializer(typeof(GameState)).ReadObject(stream);
        }
        public static void Validate(GameState s)
        {
            if (s == null || s.saveVersion != 1 || s.coins < 0 || s.gems < 0 || s.stamina < 0 ||
                s.playerLevel < 1 || s.townGrowthLevel < 0 || s.questProgress < 0 ||
                s.activeQuestId == null || s.lastSaveTime == null || !Enum.IsDefined(typeof(QuestState), s.questState) ||
                s.inventory == null || s.mergeBoard == null || s.completedQuestIds == null || s.claimedRewardIds == null)
                throw new InvalidDataException("Invalid or unsupported save state.");
            var cells = new HashSet<int>();
            foreach (var item in s.mergeBoard)
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.level < 1 || item.level > GameState.MaxLevel ||
                    item.cellIndex < 0 || item.cellIndex >= GameState.Columns * GameState.Rows || !cells.Add(item.cellIndex))
                    throw new InvalidDataException("Invalid merge board.");
            foreach (var item in s.inventory)
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.level < 1 || item.level > GameState.MaxLevel || item.count <= 0)
                    throw new InvalidDataException("Invalid inventory.");
            foreach (var ids in new[] { s.completedQuestIds, s.claimedRewardIds })
                if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
                    throw new InvalidDataException("Invalid completion identifiers.");
            if (s.lastSaveTime.Length > 0 && !DateTimeOffset.TryParse(s.lastSaveTime, out _))
                throw new InvalidDataException("Invalid save timestamp.");
        }
    }
}
