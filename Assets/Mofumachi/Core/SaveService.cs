using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Mofumachi.Core
{
    public sealed class SaveService : IStateStore
    {
        private readonly string directory;
        private readonly object gate = new object();
        private LoadResult blocked;
        private bool recoveredPrimary;
        public string PrimaryPath => Path.Combine(directory, "save.json");
        public string BackupPath => Path.Combine(directory, "save.backup.json");
        public string PreV4Path => Path.Combine(directory, "save.pre-v4.json");
        private string TemporaryPath => PrimaryPath + ".tmp";
        public SaveService(string saveDirectory) { directory = saveDirectory ?? throw new ArgumentNullException(nameof(saveDirectory)); }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            lock (state.SyncRoot)
            lock (gate)
            {
                ThrowIfBlocked();
                try { StateCodec.Validate(state); }
                catch (Exception ex) when (ReadFailure(ex)) { Block(ex, false); throw; }
                try
                {
                    RequireNoTemporary();
                    VerifyProtection(state);
                    if (Exists(PrimaryPath) && !recoveredPrimary)
                    {
                        int version;
                        var current = ReadState(File.ReadAllBytes(PrimaryPath), out version);
                        if (version != 4 || current.preV4SourceSha256 != state.preV4SourceSha256)
                            throw new InvalidDataException("Load and migrate the existing save before writing over it.");
                        VerifyProtection(current);
                    }
                }
                catch (Exception ex) when (ReadFailure(ex))
                {
                    Block(ex, false); ThrowIfBlocked();
                }
                var snapshot = state.Clone();
                snapshot.lastSaveTime = DateTimeOffset.UtcNow.ToString("O");
                WriteSnapshot(snapshot, false);
                state.lastSaveTime = snapshot.lastSaveTime;
                recoveredPrimary = false;
            }
        }

        public LoadResult Load()
        {
            lock (gate)
            {
                if (blocked != null) return blocked;
                bool migrating = false;
                try
                {
                    RequireNoTemporary();
                    if (!Exists(PrimaryPath))
                    {
                        if (Exists(BackupPath)) return RecoverV4();
                        if (Exists(PreV4Path)) throw new InvalidDataException("Pre-v4 protection exists without a committed main save. Explicit recovery is required.");
                        return new LoadResult(GameState.CreateInitial(), LoadStatus.NewGame);
                    }
                    byte[] original = File.ReadAllBytes(PrimaryPath);
                    string json = Text(original);
                    int version;
                    try { version = Version(json); }
                    catch (XmlException) { return RecoverV4(); }
                    if (version < 1 || version > 4) throw new InvalidDataException("Unsupported save version: " + version);
                    migrating = version < 4;
                    // Parsed v4 with invalid required data must never fall back to an older snapshot.
                    var state = StateCodec.Decode(json); StateCodec.Validate(state);
                    if (!migrating)
                    {
                        VerifyProtection(state);
                        return new LoadResult(state);
                    }
                    if (Exists(PreV4Path)) throw new InvalidDataException("Pre-v4 protection already exists. Migration will not be retried or overwritten.");
                    using (var stream = new FileStream(PreV4Path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { stream.Write(original, 0, original.Length); stream.Flush(true); }
                    byte[] protectedBytes = File.ReadAllBytes(PreV4Path);
                    string digest = Digest(original);
                    if (protectedBytes.Length != original.Length || Digest(protectedBytes) != digest || Digest(File.ReadAllBytes(PrimaryPath)) != digest)
                        throw new InvalidDataException("Pre-v4 raw protection does not match the unchanged source save.");
                    state.preV4SourceSha256 = digest;
                    VerifyProtection(state);
                    // Keep the old lastSaveTime during migration; normal saves update it later.
                    WriteSnapshot(state, true);
                    return new LoadResult(state);
                }
                catch (Exception ex) when (ReadFailure(ex)) { return Block(ex, migrating); }
            }
        }

        private LoadResult RecoverV4()
        {
            if (!Exists(BackupPath)) throw new InvalidDataException("Existing save cannot be read and no valid v4 backup is available. New-game initialization is forbidden.");
            int version;
            var state = ReadState(File.ReadAllBytes(BackupPath), out version);
            if (version != 4) throw new InvalidDataException("Only an existing v4 backup may be recovered automatically. An old backup needs explicit recovery.");
            VerifyProtection(state);
            recoveredPrimary = true;
            return new LoadResult(state, true, "主セーブを読み込めなかったため、前回の有効なバックアップから復旧しました。");
        }

        private void WriteSnapshot(GameState snapshot, bool migration)
        {
            StateCodec.Validate(snapshot);
            Directory.CreateDirectory(directory);
            bool created = false;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(StateCodec.Encode(snapshot));
                using (var stream = new FileStream(TemporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { created = true; stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                int version;
                var checkedState = ReadState(File.ReadAllBytes(TemporaryPath), out version);
                if (version != 4 || StateCodec.Encode(checkedState) != StateCodec.Encode(snapshot))
                    throw new InvalidDataException("Saved v4 candidate does not round-trip without changing data.");
                VerifyProtection(checkedState);
                if (Exists(PrimaryPath)) File.Replace(TemporaryPath, PrimaryPath, recoveredPrimary ? null : BackupPath);
                else File.Move(TemporaryPath, PrimaryPath);
            }
            finally
            {
                // A failed migration retains its candidate/marker for diagnosis, never auto-retries.
                if (!migration && created && File.Exists(TemporaryPath)) File.Delete(TemporaryPath);
            }
        }

        private void VerifyProtection(GameState state)
        {
            bool present = Exists(PreV4Path);
            if (state.preV4SourceSha256.Length == 0)
            {
                if (present) throw new InvalidDataException("Unbound pre-v4 protection found. Explicit recovery is required.");
                return;
            }
            if (!present) throw new InvalidDataException("The protected pre-v4 source is missing.");
            byte[] bytes = File.ReadAllBytes(PreV4Path);
            if (!string.Equals(Digest(bytes), state.preV4SourceSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Protected pre-v4 source SHA256 mismatch.");
            int version;
            ReadState(bytes, out version);
            if (version < 1 || version > 3) throw new InvalidDataException("Protected source is not a valid v1/v2/v3 save.");
        }

        public void ResetSave()
        {
            lock (gate)
            {
                ThrowIfBlocked();
                // Do not turn a protected/interrupted migration into an implicit new game.
                if (Exists(PreV4Path)) throw new IOException("Pre-v4 source protection is retained. Reset requires an explicit recovery decision.");
                foreach (var path in new[] { PrimaryPath, BackupPath, TemporaryPath })
                    if (File.Exists(path)) File.Delete(path);
                recoveredPrimary = false;
            }
        }
        private void RequireNoTemporary()
        {
            if (Exists(TemporaryPath)) throw new InvalidDataException("Uncommitted save.json.tmp exists. It will not be removed or overwritten.");
        }
        private LoadResult Block(Exception error, bool migration)
        {
            if (blocked == null)
                blocked = new LoadResult(null, migration ? LoadStatus.MigrationBlocked : LoadStatus.LoadBlocked,
                    error.GetType().Name + ": " + error.Message);
            return blocked;
        }
        private void ThrowIfBlocked()
        { if (blocked != null) throw new IOException("Save writes are blocked: " + blocked.Reason); }
        private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
        private static bool ReadFailure(Exception ex) => ex is IOException || ex is InvalidDataException ||
            ex is UnauthorizedAccessException || ex is SerializationException || ex is ArgumentException ||
            ex is XmlException || ex is System.Security.SecurityException || ex is FormatException;
        private static string Text(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), true)) return reader.ReadToEnd();
        }
        private static GameState ReadState(byte[] bytes, out int version)
        {
            string json = Text(bytes); version = Version(json);
            if (version < 1 || version > 4) throw new InvalidDataException("Unsupported save version: " + version);
            var state = StateCodec.Decode(json); StateCodec.Validate(state); return state;
        }
        private static int Version(string json)
        {
            // Parse syntax separately so malformed JSON can use a v4 backup, while a
            // valid JSON document with unsupported/missing version or bad v4 cannot.
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), XmlDictionaryReaderQuotas.Max))
            {
                var document = new XmlDocument { XmlResolver = null }; document.Load(reader);
                var root = document.DocumentElement;
                var nodes = root?.SelectNodes("saveVersion");
                int version;
                if (root == null || root.GetAttribute("type") != "object" || nodes == null || nodes.Count != 1 ||
                    ((XmlElement)nodes[0]).GetAttribute("type") != "number" ||
                    !int.TryParse(nodes[0].InnerText, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out version))
                    throw new InvalidDataException("Missing, duplicate or invalid save version.");
                return version;
            }
        }
        private static string Digest(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
