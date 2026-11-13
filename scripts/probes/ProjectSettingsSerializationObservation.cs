// Test-only byte observer. No Unity dependency; never writes ProjectSettings.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mofumachi.Probe
{
    [DataContract]
    public sealed class StageRecord
    {
        [DataMember] public string stage, observed_utc, file_modified_utc, sha256, bom;
        [DataMember] public long byte_size;
        [DataMember] public int crlf_count, bare_lf_count, bare_cr_count, api_byte_offset = -1;
        [DataMember] public bool metadata_stable = true, comparison_available, changed, has_suffix_i, has_empty_inproject_binding, api_redacted;
        [DataMember] public string m_APIs, api_raw_hex, api_context_hex, automatic, android_keystore_name, android_keyalias_name;
        [DataMember] public string keystore_line_sha256, alias_line_sha256, android_use_custom_keystore;
        [DataMember] public string architecture, orientation, android_backend, minimum_api, target_api;
    }

    [DataContract]
    public sealed class ProbeReport
    {
        [DataMember] public string source_head, run_id, result = "Running", error_type, error_stage;
        [DataMember] public StageRecord baseline;
        [DataMember] public List<StageRecord> stages = new List<StageRecord>();
        [DataMember] public string classification = "Incomplete", first_i_stage, first_empty_inproject_stage, first_keystore_change_stage;
        [DataMember] public bool i_and_empty_binding_same_stage, i_and_keystore_change_same_stage;
        [DataMember] public int graphics_setters, graphics_restore_setters, non_graphics_setters, signing_setting_setters, signing_restore_setters, save_assets_calls;
        [DataMember] public int unity_invocations, automatic_retries, build_player_calls, aab_count, apk_count, pixel_operations, signing_verifications;
        [DataMember] public bool completed_stages, original_signing_restored, stage8_complete;
        [DataMember] public string[] initial_graphics_apis;
        [DataMember] public bool initial_automatic, initial_custom_keystore;
        [DataMember] public string initial_backend, initial_architecture, initial_orientation;
        [DataMember] public string attribution_limit = "First observed disk change; not exclusive writer proof. Earlier operations may dirty state for a later flush. P0 follows Unity import/initialization; E0 follows process exit.";
    }

    public static class SettingsObservation
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static string Hash(byte[] bytes)
        {
            using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static string Hex(byte[] bytes, int offset, int count)
        {
            return BitConverter.ToString(bytes, offset, count).Replace("-", " ").ToLowerInvariant();
        }

        private static bool Sensitive(string text, string[] secrets)
        {
            if (secrets != null) foreach (string s in secrets)
                if (!string.IsNullOrEmpty(s) && text.IndexOf(s, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        private static string Field(string text, string name)
        {
            Match m = Regex.Match(text, @"(?m)^  " + Regex.Escape(name) + @":[ \t]*([^\r\n]*)");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Number(string text, string name)
        {
            string value = Field(text, name);
            return value == null ? "<absent>" : Regex.IsMatch(value, @"^-?[0-9]+$") ? value : "<unrecognized>";
        }

        private static string PrivateField(string value, bool allowEmptyBinding)
        {
            if (value == null) return "<absent>";
            if (value.Length == 0 || value == "''" || value == "\"\"") return "<empty>";
            if (allowEmptyBinding && value == "'{inproject}: '") return value;
            return "<redacted>";
        }

        public static StageRecord FromBytes(string stage, byte[] bytes, string unusedPath, string mtime, string[] secrets)
        {
            if (!Regex.IsMatch(stage, @"^(B0|E0|P[0-6][a-e]?|R[0-9]+)$")) throw new InvalidDataException("Invalid stage id.");
            int bom = bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191 ? 3 : 0;
            // A new encoding is a finding, not permission to transcode the source.
            if (bytes.Length >= 2 && ((bytes[0] == 255 && bytes[1] == 254) || (bytes[0] == 254 && bytes[1] == 255)))
                throw new InvalidDataException("Unexpected settings encoding.");
            string text = Utf8.GetString(bytes, bom, bytes.Length - bom);
            var r = new StageRecord { stage = stage, observed_utc = DateTime.UtcNow.ToString("o"), file_modified_utc = mtime,
                sha256 = Hash(bytes), byte_size = bytes.Length, bom = bom == 3 ? "UTF-8" : "None" };
            for (int n = 0; n < bytes.Length; n++)
            {
                if (bytes[n] == 13) { if (n + 1 < bytes.Length && bytes[n + 1] == 10) { r.crlf_count++; n++; } else r.bare_cr_count++; }
                else if (bytes[n] == 10) r.bare_lf_count++;
            }
            // Confine raw context to the graphics block; never include nearby private settings.
            Match block = Regex.Match(text, @"(?m)^  m_BuildTargetGraphicsAPIs:[^\r\n]*\r?\n(?:(?:  - |    )[^\r\n]*(?:\r?\n|$))+");
            Match api = block.Success ? Regex.Match(block.Value, @"(?m)^  - m_BuildTarget: Android\r?\n    m_APIs: ([^\r\n]*)\r?\n    m_Automatic: ([01])(?:\r?\n|$)") : Match.Empty;
            if (!api.Success) throw new InvalidDataException("Expected unique Android graphics block missing.");
            if (Regex.Matches(block.Value, @"(?m)^  - m_BuildTarget: Android\r?$").Count != 1)
                throw new InvalidDataException("Duplicate Android graphics block.");
            string raw = api.Groups[1].Value;
            r.api_redacted = Sensitive(block.Value, secrets) || !Regex.IsMatch(raw, @"^[0-9a-fA-F]{8}(?:i)?$");
            r.m_APIs = r.api_redacted ? "<redacted>" : raw;
            r.has_suffix_i = !r.api_redacted && raw == "0b000000i";
            r.automatic = api.Groups[2].Value;
            // Group.Index is relative to block.Value, including the match's position.
            r.api_byte_offset = bom + Utf8.GetByteCount(text.Substring(0, block.Index + api.Groups[1].Index));
            if (!r.api_redacted)
            {
                int count = Utf8.GetByteCount(raw);
                r.api_raw_hex = Hex(bytes, r.api_byte_offset, count);
                int lower = bom + Utf8.GetByteCount(text.Substring(0, block.Index));
                int upper = lower + Utf8.GetByteCount(block.Value);
                int start = Math.Max(lower, r.api_byte_offset - 12);
                r.api_context_hex = Hex(bytes, start, Math.Min(upper - start, count + 24));
            }
            string key = Field(text, "AndroidKeystoreName"), alias = Field(text, "AndroidKeyaliasName");
            r.android_keystore_name = PrivateField(key, true); r.android_keyalias_name = PrivateField(alias, false);
            r.has_empty_inproject_binding = key == "'{inproject}: '";
            r.keystore_line_sha256 = key == null ? null : Hash(Utf8.GetBytes("AndroidKeystoreName:" + key));
            r.alias_line_sha256 = alias == null ? null : Hash(Utf8.GetBytes("AndroidKeyaliasName:" + alias));
            r.android_use_custom_keystore = Number(text, "AndroidUseCustomKeystore");
            r.architecture = Number(text, "AndroidTargetArchitectures"); r.orientation = Number(text, "defaultScreenOrientation");
            r.minimum_api = Number(text, "AndroidMinSdkVersion"); r.target_api = Number(text, "AndroidTargetSdkVersion");
            Match backend = Regex.Match(text, @"(?m)^  scriptingBackend:\r?\n(?:(?:    )[^\r\n]*\r?\n)*?    Android: ([0-9]+)\r?$");
            r.android_backend = backend.Success ? backend.Groups[1].Value : "<absent>";
            return r;
        }

        public static StageRecord Read(string stage, string path, string[] secrets)
        {
            var before = new FileInfo(path); long length = before.Length; DateTime time = before.LastWriteTimeUtc;
            byte[] bytes = File.ReadAllBytes(path);
            var r = FromBytes(stage, bytes, "ProjectSettings/ProjectSettings.asset", time.ToString("o"), secrets);
            var after = new FileInfo(path);
            r.metadata_stable = length == after.Length && time == after.LastWriteTimeUtc && length == bytes.Length;
            Array.Clear(bytes, 0, bytes.Length);
            if (!r.metadata_stable) throw new IOException("Settings changed during byte read; no retry.");
            return r;
        }

        public static string Json<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return Utf8.GetString(stream.ToArray());
            }
        }
        public static string StageJson(StageRecord value) { return Json(value); }

        public static string RelevantDiff(StageRecord before, StageRecord after)
        {
            var lines = new StringBuilder("Safe field projection only; excludes private raw settings and password fields.\n");
            lines.Append("from ").Append(before.stage).Append(" to ").Append(after.stage).Append('\n');
            string[] names = { "sha256", "byte_size", "bom", "crlf_count", "bare_lf_count", "bare_cr_count", "m_APIs", "api_raw_hex", "automatic", "android_keystore_name", "keystore_line_sha256", "android_keyalias_name", "alias_line_sha256", "android_use_custom_keystore", "architecture", "orientation", "android_backend", "minimum_api", "target_api" };
            foreach (string name in names)
            {
                var field = typeof(StageRecord).GetField(name);
                string a = Convert.ToString(field.GetValue(before)), b = Convert.ToString(field.GetValue(after));
                if (a != b) lines.Append(name).Append(": ").Append(a).Append(" -> ").Append(b).Append('\n');
            }
            return lines.ToString();
        }

        public static void WriteStage(string directory, StageRecord value, StageRecord previous)
        {
            string stages = Path.Combine(directory, "stages"); Directory.CreateDirectory(stages);
            value.comparison_available = previous != null;
            if (previous != null) value.changed = previous.sha256 != value.sha256;
            WriteNew(Path.Combine(stages, value.stage + ".json"), StageJson(value));
            if (previous != null) WriteNew(Path.Combine(stages, value.stage + ".diff"), RelevantDiff(previous, value));
        }

        public static void WriteNew(string path, string text)
        {
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(file, Utf8)) writer.Write(text);
        }

        public static ProbeReport LoadReport(string path)
        {
            using (var file = File.OpenRead(path)) return (ProbeReport)new DataContractJsonSerializer(typeof(ProbeReport)).ReadObject(file);
        }

        public static void WriteReport(string directory, ProbeReport report)
        {
            Classify(report);
            // Only this new run's progress JSON is updated. Never target an asset path.
            File.WriteAllText(Path.Combine(directory, "probe-report.json"), Json(report), Utf8);
        }

        public static void Classify(ProbeReport r)
        {
            r.first_i_stage = r.first_empty_inproject_stage = r.first_keystore_change_stage = null;
            bool final = false, exited = false;
            foreach (StageRecord s in r.stages)
            {
                if (r.first_i_stage == null && s.has_suffix_i) r.first_i_stage = s.stage;
                if (r.first_empty_inproject_stage == null && s.has_empty_inproject_binding) r.first_empty_inproject_stage = s.stage;
                if (r.first_keystore_change_stage == null && r.baseline != null && s.keystore_line_sha256 != r.baseline.keystore_line_sha256)
                    r.first_keystore_change_stage = s.stage;
                final |= s.stage == "P6"; exited |= s.stage == "E0";
            }
            string first = r.first_i_stage;
            r.classification = first == null ? (final && exited ? "S7" : "Incomplete") : first == "P0" ? "S1" :
                first == "P1" ? "GetterWindow" : first == "P2" ? "S2" : first.StartsWith("P3", StringComparison.Ordinal) ? "S3" :
                first.StartsWith("P4", StringComparison.Ordinal) ? "S4" : first.StartsWith("P5", StringComparison.Ordinal) ? "S5" :
                first == "P6" ? "S6" : first == "E0" ? "ExitOnly" : "EmergencyRestore";
            r.i_and_empty_binding_same_stage = first != null && first == r.first_empty_inproject_stage;
            r.i_and_keystore_change_same_stage = first != null && first == r.first_keystore_change_stage;
        }
    }
}
