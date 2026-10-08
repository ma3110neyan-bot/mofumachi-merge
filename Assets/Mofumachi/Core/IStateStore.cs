namespace Mofumachi.Core
{
    public interface IStateStore
    {
        void Save(GameState state);
        LoadResult Load();
        void ResetSave();
    }
    public sealed class LoadResult
    {
        public GameState State { get; }
        public bool Recovered { get; }
        public string Reason { get; }
        public LoadResult(GameState state, bool recovered = false, string reason = "")
        { State = state; Recovered = recovered; Reason = reason; }
    }
}
