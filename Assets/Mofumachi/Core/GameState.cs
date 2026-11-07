using System;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class BoardItem : IExtensibleDataObject
    {
        public ExtensionDataObject ExtensionData { get; set; }
        [DataMember(IsRequired = true)] public string itemId;
        [DataMember(IsRequired = true)] public int level;
        [DataMember(IsRequired = true)] public int cellIndex;
        public BoardItem(string id, int itemLevel, int cell) { itemId = id; level = itemLevel; cellIndex = cell; }
    }
    [Serializable, DataContract]
    public sealed class InventoryEntry : IExtensibleDataObject
    {
        public ExtensionDataObject ExtensionData { get; set; }
        [DataMember(IsRequired = true)] public string itemId;
        [DataMember(IsRequired = true)] public int level;
        [DataMember(IsRequired = true)] public int count;
        public InventoryEntry(string id, int itemLevel, int quantity) { itemId = id; level = itemLevel; count = quantity; }
    }
    [Serializable, DataContract]
    public sealed class GameState : IExtensibleDataObject
    {
        public ExtensionDataObject ExtensionData { get; set; }
        private object syncRoot;
        internal object SyncRoot => LazyInitializer.EnsureInitialized(ref syncRoot);
        private HashSet<int> mergeLocks;
        internal HashSet<int> MergeLocks => LazyInitializer.EnsureInitialized(ref mergeLocks);
        public const int Columns = 5, Rows = 6, MaxLevel = 3;
        public const int CurrentSaveVersion = 4;
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
        private float savedBgmVolume = .75f, savedSeVolume = .65f;
        private bool hasBgmVolume, hasSeVolume, hasFavorite;
        private string savedFavorite = "";
        [DataMember] public float bgmVolume { get => savedBgmVolume; set { savedBgmVolume = value; hasBgmVolume = true; } }
        [DataMember] public float seVolume { get => savedSeVolume; set { savedSeVolume = value; hasSeVolume = true; } }
        [DataMember] public bool purchaseNoticeAcknowledged;
        [DataMember] public string favoriteCharacterId { get => savedFavorite; set { savedFavorite = value; hasFavorite = true; } }
        [DataMember] public List<string> productionFrameIds = new List<string> { "tea", "bread", "flower" };
        [DataMember(IsRequired = true)] public string lastSaveTime = "";
        // Optional only for decoding old schemas; a valid v4 always has economy.
        [DataMember] public EconomyState economy = new EconomyState();
        [DataMember] public string preV4SourceSha256 = "";

        [OnDeserializing]
        private void BeforeDeserialize(StreamingContext context)
        {
            // DataContract does not run field initializers. Missing v2 volumes must be rejected.
            savedBgmVolume = savedSeVolume = float.NaN;
            savedFavorite = null;
            hasBgmVolume = hasSeVolume = hasFavorite = false;
            economy = null;
            preV4SourceSha256 = "";
            // Optional in v3: older saves have all three frames, while an explicit [] stays empty.
            productionFrameIds = new List<string> { "tea", "bread", "flower" };
        }
        [OnDeserialized]
        private void AfterDeserialize(StreamingContext context)
        {
            int originalVersion = saveVersion;
            if (originalVersion == 1)
            {
                if (!hasBgmVolume) savedBgmVolume = .75f;
                if (!hasSeVolume) savedSeVolume = .65f;
            }
            if ((originalVersion == 1 || originalVersion == 2) && !hasFavorite)
                savedFavorite = "";
            if (originalVersion >= 1 && originalVersion <= 3)
            {
                // Never infer missing initialization from the spendable balance.
                economy = new EconomyState { questRoute = QuestProgressionRoute.LegacyPlayer };
                saveVersion = CurrentSaveVersion;
            }
        }

        public static GameState CreateInitial()
        {
            var state = new GameState();
            state.economy.questRoute = QuestProgressionRoute.ExpandedNewPlayer;
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
            favoriteCharacterId = c.favoriteCharacterId;
            productionFrameIds = c.productionFrameIds;
            economy = c.economy;
            preV4SourceSha256 = c.preV4SourceSha256;
            ExtensionData = c.ExtensionData;
        }
    }

    // Keep the two historical v4 values; only new games opt into the expanded route.
    public enum QuestProgressionRoute { NewPlayer = 0, LegacyPlayer = 1, ExpandedNewPlayer = 2 }
    public enum WalkRewardKind { Normal, Additional, Help }

    [Serializable, DataContract]
    public sealed class WalkRewardReceipt
    {
        [DataMember(IsRequired = true)] public string day;
        [DataMember(IsRequired = true)] public string walkId;
        [DataMember(IsRequired = true)] public WalkRewardKind kind;
        public WalkRewardReceipt(string day, string id, WalkRewardKind kind)
        { this.day = day; walkId = id; this.kind = kind; }
    }

    // Storage only. This stage grants no daily/action rewards and charges no P.
    [Serializable, DataContract]
    public sealed class EconomyState
    {
        [DataMember(IsRequired = true)] public int mofuPoints = 24;
        [DataMember(IsRequired = true)] public long townActionTotal;
        [DataMember(IsRequired = true)] public bool initialized = true;
        [DataMember(IsRequired = true)] public string rewardsStartDay = "";
        [DataMember(IsRequired = true)] public string day = "";
        [DataMember(IsRequired = true)] public string lastRecoveryDay = "";
        [DataMember(IsRequired = true)] public List<string> talkRewardCharacterIds = new List<string>();
        [DataMember(IsRequired = true)] public int normalWalkRewards;
        [DataMember(IsRequired = true)] public int additionalWalkRewards;
        [DataMember(IsRequired = true)] public bool helpWalkClaimed;
        [DataMember(IsRequired = true)] public string lastFlowerRewardDay = "";
        [DataMember(IsRequired = true)] public List<WalkRewardReceipt> walkRewardReceipts = new List<WalkRewardReceipt>();
        [DataMember(IsRequired = true)] public QuestProgressionRoute questRoute;
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
                s.playerLevel < 1 || s.townGrowthLevel < 0 || s.townGrowthLevel > 6 || s.questProgress < 0 ||
                s.activeQuestId == null || s.lastSaveTime == null || !Enum.IsDefined(typeof(QuestState), s.questState) ||
                s.inventory == null || s.mergeBoard == null || s.completedQuestIds == null || s.claimedRewardIds == null ||
                !ValidVolume(s.bgmVolume) || !ValidVolume(s.seVolume) || s.favoriteCharacterId == null ||
                (s.favoriteCharacterId.Length > 0 && !CharacterCatalog.IsKnown(s.favoriteCharacterId)))
                throw new InvalidDataException("Invalid or unsupported save state.");
            if (s.productionFrameIds == null || s.productionFrameIds.Any(id => ItemCatalog.Find(id) == null) ||
                s.productionFrameIds.Distinct(StringComparer.Ordinal).Count() != s.productionFrameIds.Count)
                throw new InvalidDataException("Invalid production frame placement.");
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
            if (s.preV4SourceSha256 == null || (s.preV4SourceSha256.Length != 0 &&
                (s.preV4SourceSha256.Length != 64 || s.preV4SourceSha256.Any(c => !Uri.IsHexDigit(c)))))
                throw new InvalidDataException("Invalid pre-v4 protection digest.");
            ValidateEconomy(s.economy);
        }
        private static bool Day(string value, bool empty = true) => value != null &&
            ((empty && value.Length == 0) || DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
        private static void ValidateEconomy(EconomyState e)
        {
            if (e == null || !e.initialized || e.mofuPoints < 0 || e.mofuPoints > 96 || e.townActionTotal < 0 ||
                !Day(e.day) || !Day(e.rewardsStartDay) || !Day(e.lastRecoveryDay) || !Day(e.lastFlowerRewardDay) ||
                !Enum.IsDefined(typeof(QuestProgressionRoute), e.questRoute) || e.talkRewardCharacterIds == null ||
                e.talkRewardCharacterIds.Count > 3 || e.talkRewardCharacterIds.Any(id => !CharacterCatalog.IsKnown(id)) ||
                e.talkRewardCharacterIds.Distinct(StringComparer.Ordinal).Count() != e.talkRewardCharacterIds.Count ||
                e.normalWalkRewards < 0 || e.normalWalkRewards > 2 || e.additionalWalkRewards < 0 || e.additionalWalkRewards > 2 ||
                e.walkRewardReceipts == null || e.walkRewardReceipts.Count > 5)
                throw new InvalidDataException("Invalid or missing v4 economy state.");
            foreach (string date in new[] { e.rewardsStartDay, e.lastRecoveryDay, e.lastFlowerRewardDay })
                if (string.CompareOrdinal(date, e.day) > 0)
                    throw new InvalidDataException("Economy receipt date exceeds the applied day.");
            if (e.day.Length == 0 && (e.talkRewardCharacterIds.Count > 0 || e.walkRewardReceipts.Count > 0 || e.helpWalkClaimed))
                throw new InvalidDataException("Economy receipts require an applied day.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var receipt in e.walkRewardReceipts)
                if (receipt == null || !Day(receipt.day, false) || receipt.day != e.day ||
                    string.IsNullOrWhiteSpace(receipt.walkId) || receipt.walkId.Length > 128 || !ids.Add(receipt.walkId) ||
                    !Enum.IsDefined(typeof(WalkRewardKind), receipt.kind))
                    throw new InvalidDataException("Invalid or duplicate walk reward receipt.");
            if (e.walkRewardReceipts.Count(r => r.kind == WalkRewardKind.Normal) != e.normalWalkRewards ||
                e.walkRewardReceipts.Count(r => r.kind == WalkRewardKind.Additional) != e.additionalWalkRewards ||
                e.walkRewardReceipts.Count(r => r.kind == WalkRewardKind.Help) != (e.helpWalkClaimed ? 1 : 0))
                throw new InvalidDataException("Walk reward counters do not match saved receipts.");
        }
        internal static bool ValidVolume(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
    }
}
