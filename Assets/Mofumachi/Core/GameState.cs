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
        [DataMember(IsRequired = true)] public string itemId;
        [DataMember(IsRequired = true)] public int level;
        [DataMember(IsRequired = true)] public int cellIndex;
        public BoardItem(string id, int itemLevel, int cell) { itemId = id; level = itemLevel; cellIndex = cell; }
    }
    [Serializable, DataContract]
    public sealed class InventoryEntry
    {
        [DataMember(IsRequired = true)] public string itemId;
        [DataMember(IsRequired = true)] public int level;
        [DataMember(IsRequired = true)] public int count;
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
        public const int CurrentSaveVersion = 2;
        [DataMember(IsRequired = true)] public int saveVersion = CurrentSaveVersion;
        [DataMember(IsRequired = true)] public int coins;
        [DataMember(IsRequired = true)] public int gems;
        [DataMember(IsRequired = true)] public int stamina = 30;
        [DataMember(IsRequired = true)] public int playerLevel = 1;
        [DataMember(IsRequired = true)] public int townGrowthLevel;
        [DataMember(IsRequired = true)] public string activeQuestId = "";
        [DataMember(IsRequired = true)] public QuestState questState;
        [DataMember(IsRequired = true)] public int questProgress;
        [DataMember(IsRequired = true)] public List<InventoryEntry> inventory = new List<InventoryEntry>();
        [DataMember(IsRequired = true)] public List<BoardItem> mergeBoard = new List<BoardItem>();
        [DataMember(IsRequired = true)] public List<string> completedQuestIds = new List<string>();
        [DataMember(IsRequired = true)] public List<string> claimedRewardIds = new List<string>();
        [DataMember(IsRequired = true)] public bool bgmEnabled = true;
        [DataMember(IsRequired = true)] public bool seEnabled = true;
        [DataMember] public float bgmVolume = .75f;
        [DataMember] public float seVolume = .65f;
        [DataMember] public bool purchaseNoticeAcknowledged;
        [DataMember(IsRequired = true)] public string lastSaveTime = "";

        [OnDeserializing]
        private void BeforeDeserialize(StreamingContext context)
        {
            // DataContract does not run field initializers. Missing v2 volumes must be rejected.
            bgmVolume = seVolume = float.NaN;
        }
        [OnDeserialized]
        private void AfterDeserialize(StreamingContext context)
        {
            if (saveVersion != 1) return;
            bgmVolume = .75f; seVolume = .65f; purchaseNoticeAcknowledged = false;
            saveVersion = CurrentSaveVersion;
        }

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
            bgmVolume = c.bgmVolume; seVolume = c.seVolume;
            purchaseNoticeAcknowledged = c.purchaseNoticeAcknowledged;
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
            if (s == null || s.saveVersion != GameState.CurrentSaveVersion || s.coins < 0 || s.gems < 0 || s.stamina < 0 ||
                s.playerLevel < 1 || s.townGrowthLevel < 0 || s.questProgress < 0 ||
                s.activeQuestId == null || s.lastSaveTime == null || !Enum.IsDefined(typeof(QuestState), s.questState) ||
                s.inventory == null || s.mergeBoard == null || s.completedQuestIds == null || s.claimedRewardIds == null ||
                !ValidVolume(s.bgmVolume) || !ValidVolume(s.seVolume))
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
        internal static bool ValidVolume(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
    }
}
