using System;
using System.IO;
using System.Linq;

namespace Mofumachi.Core
{
    public sealed class GameStateManager
    {
        private readonly IStateStore store;
        public GameState State { get; }
        public MergeBoard Board { get; }
        public QuestManager Quests { get; }
        public string LastError { get; private set; } = "";
        public GameStateManager(GameState state, IStateStore store)
        {
            State = state; this.store = store; Board = new MergeBoard(state);
            Quests = new QuestManager(state, store, new RewardService(), new TownGrowthService());
        }
        public bool TryMerge(int from, int to) => Mutate(() => Board.TryMerge(from, to));
        public bool TryMove(int from, int to) => Mutate(() => Board.TryMove(from, to));
        public bool AddItem(string id, int level) => Mutate(() => Board.AddItem(id, level));
        public bool SetAudio(bool bgm, bool se) => SetAudioPreferences(bgm, se, State.bgmVolume, State.seVolume);
        public bool SetAudioPreferences(bool bgm, bool se, float bgmVolume, float seVolume)
        {
            if (!StateCodec.ValidVolume(bgmVolume) || !StateCodec.ValidVolume(seVolume))
            { LastError = "音量は0から100%の範囲で設定してください。"; return false; }
            return Mutate(() =>
            {
                State.bgmEnabled = bgm; State.seEnabled = se;
                State.bgmVolume = bgmVolume; State.seVolume = seVolume;
                return true;
            });
        }
        public bool AcknowledgePurchaseNotice()
        {
            lock (State.SyncRoot)
            {
                if (State.purchaseNoticeAcknowledged) { LastError = ""; return true; }
                return Mutate(() => { State.purchaseNoticeAcknowledged = true; return true; });
            }
        }
        public bool Save()
        {
            lock (State.SyncRoot)
            {
                try { store.Save(State); LastError = ""; return true; }
                catch (IOException) { LastError = "セーブできませんでした。保存先を確認してください。"; return false; }
                catch (UnauthorizedAccessException) { LastError = "保存先へ書き込めません。"; return false; }
            }
        }
        private bool Mutate(Func<bool> action)
        {
            lock (State.SyncRoot)
            {
                var before = State.Clone();
                var previousLocks = State.MergeLocks.ToArray();
                if (!action()) return false;
                Quests.RefreshProgress();
                if (Save()) return true;
                State.CopyFrom(before); Board.ReleaseLocks();
                foreach (var cell in previousLocks) State.MergeLocks.Add(cell);
                return false;
            }
        }
    }
}
