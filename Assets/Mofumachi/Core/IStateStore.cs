namespace Mofumachi.Core
{
    public interface IStateStore
    {
        void Save(GameState state);
        LoadResult Load();
        void ResetSave();
    }
    public enum LoadStatus { Loaded, NewGame, Recovered, LoadBlocked, MigrationBlocked }
    public sealed class LoadResult
    {
        public GameState State { get; }
        public bool Recovered { get; }
        public string Reason { get; }
        public LoadStatus Status { get; }
        public bool IsBlocked => Status == LoadStatus.LoadBlocked || Status == LoadStatus.MigrationBlocked;
        public LoadResult(GameState state, bool recovered = false, string reason = "")
            : this(state, recovered ? LoadStatus.Recovered : LoadStatus.Loaded, reason) { }
        public LoadResult(GameState state, LoadStatus status, string reason = "")
        { State = state; Status = status; Recovered = status == LoadStatus.Recovered; Reason = reason; }
    }
}
