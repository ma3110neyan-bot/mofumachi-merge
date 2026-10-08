using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Mofumachi.Core
{
    public sealed class SaveService : IStateStore
    {
        private readonly string directory;
        private readonly object gate = new object();
        public string PrimaryPath => Path.Combine(directory, "save.json");
        public string BackupPath => Path.Combine(directory, "save.backup.json");
        public SaveService(string saveDirectory) { directory = saveDirectory ?? throw new ArgumentNullException(nameof(saveDirectory)); }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            // Transactions acquire the state lock first; use the same ordering here.
            lock (state.SyncRoot)
            lock (gate)
            {
                StateCodec.Validate(state);
                var snapshot = state.Clone(); snapshot.lastSaveTime = DateTimeOffset.UtcNow.ToString("O");
                Directory.CreateDirectory(directory);
                var temporary = PrimaryPath + ".tmp";
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(StateCodec.Encode(snapshot));
                    using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    StateCodec.Validate(StateCodec.Decode(File.ReadAllText(temporary)));
                    if (File.Exists(PrimaryPath))
                        File.Replace(temporary, PrimaryPath, TryRead(PrimaryPath) != null ? BackupPath : null);
                    else File.Move(temporary, PrimaryPath);
                    state.lastSaveTime = snapshot.lastSaveTime;
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
        public LoadResult Load()
        {
            lock (gate)
            {
                var main = TryRead(PrimaryPath);
                if (main != null) return new LoadResult(main);
                var backup = TryRead(BackupPath);
                if (backup != null) return new LoadResult(backup, true, "主セーブを読み込めなかったため、前回の有効なバックアップから復旧しました。");
                bool existed = File.Exists(PrimaryPath) || File.Exists(BackupPath);
                return new LoadResult(GameState.CreateInitial(), existed, existed ? "セーブを検証できなかったため、安全な初期状態で開始しました。" : "");
            }
        }
        public void ResetSave()
        {
            lock (gate)
                foreach (var path in new[] { PrimaryPath, BackupPath, PrimaryPath + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
        }
        private static GameState TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var state = StateCodec.Decode(File.ReadAllText(path)); StateCodec.Validate(state); return state;
            }
            catch (IOException) { return null; }
            catch (InvalidDataException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (SerializationException) { return null; }
            catch (ArgumentException) { return null; }
        }
    }
}
