// Dedicated explicit batch entry point. Copied into a fresh E-only worktree.
// No initialization callback, menu entry, graphics setter, build, or signing operation.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Mofumachi.Probe;

namespace Mofumachi.Editor.Diagnostics
{
    public static class AndroidProjectSettingsSerializationProbe
    {
        private const string Source = "ddbb93f3e6b5a45795be87561f61de5d4194f65c";
        private static readonly string[] Names = { "MOFUMACHI_KEYSTORE_PATH", "MOFUMACHI_KEYSTORE_PASSWORD", "MOFUMACHI_KEY_ALIAS", "MOFUMACHI_KEY_ALIAS_PASSWORD" };

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 == args.Length) throw new InvalidOperationException("Missing probe argument.");
            return args[i + 1];
        }

        private static string UnderE(string path, string child)
        {
            string full = Path.GetFullPath(path), root = @"E:\Mofumachi-Validation\" + child + "\\";
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("E-only probe path required.");
            for (string p = full; !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
                if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Linked probe path rejected.");
            return full;
        }

        public static void Run()
        {
            string directory = null, settings = null, stage = "P0";
            string[] input = new string[4], original = new string[4];
            ProbeReport report = null; bool originalRead = false, restoreOwed = false, originalCustom = false;
            int restoreAttempted = 0, restoreSucceeded = 0;
            Action<string> observe = null;
            bool skipP6 = Array.IndexOf(Environment.GetCommandLineArgs(), "-mofumachiProbeSkipP6") >= 0;
            try
            {
                if (Application.platform != RuntimePlatform.WindowsEditor || Application.unityVersion != "6000.6.4f1" ||
                    EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    throw new InvalidOperationException("Pinned Windows Android editor required.");
                string project = UnderE(Path.GetDirectoryName(Application.dataPath), "p");
                directory = UnderE(Argument("-mofumachiProbeDirectory"), "Diagnostics");
                if (Argument("-mofumachiProbeSource") != Source) throw new InvalidOperationException("Source pin differs.");
                settings = Path.Combine(project, "ProjectSettings/ProjectSettings.asset");
                report = SettingsObservation.LoadReport(Path.Combine(directory, "probe-report.json"));
                if (report.source_head != Source || report.stages.Count != 0 || report.baseline == null || report.baseline.m_APIs != "0b000000")
                    throw new InvalidOperationException("New pre-Unity baseline required.");
                for (int i = 0; i < Names.Length; i++)
                {
                    input[i] = Environment.GetEnvironmentVariable(Names[i]);
                    Environment.SetEnvironmentVariable(Names[i], null);
                    if (string.IsNullOrWhiteSpace(input[i])) throw new InvalidOperationException("Private process inputs missing.");
                }
                report.unity_invocations = 1;
                observe = delegate(string id)
                {
                    stage = id;
                    // No PlayerSettings getter or setter is hidden in the observer.
                    StageRecord previous = report.stages.Count == 0 ? report.baseline : report.stages[report.stages.Count - 1];
                    StageRecord current = SettingsObservation.Read(id, settings, input);
                    SettingsObservation.WriteStage(directory, current, previous);
                    report.stages.Add(current); SettingsObservation.WriteReport(directory, report);
                    Debug.Log("MOFUMACHI_SERIALIZATION_PROBE_STAGE " + id);
                    if (skipP6 && current.sha256 != report.baseline.sha256)
                        throw new InvalidOperationException("Control pre-P6 mutation observed.");
                };
                // First executeMethod observation, after startup/import callbacks but before any PlayerSettings access.
                observe("P0");
                stage = "P1";
                var graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                report.initial_graphics_apis = Array.ConvertAll(graphics, g => g.ToString());
                report.initial_automatic = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
                var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
                var architecture = PlayerSettings.Android.targetArchitectures;
                var orientation = PlayerSettings.defaultInterfaceOrientation;
                originalCustom = PlayerSettings.Android.useCustomKeystore;
                original[0] = PlayerSettings.Android.keystoreName; original[1] = PlayerSettings.Android.keystorePass;
                original[2] = PlayerSettings.Android.keyaliasName; original[3] = PlayerSettings.Android.keyaliasPass;
                originalRead = true;
                report.initial_custom_keystore = originalCustom;
                report.initial_backend = backend.ToString(); report.initial_architecture = architecture.ToString();
                report.initial_orientation = orientation.ToString();
                observe("P1");
                // Keep P3 a same-value experiment, even when an unexpected baseline is encountered.
                if (backend != ScriptingImplementation.IL2CPP || architecture != AndroidArchitecture.ARM64 || orientation != UIOrientation.Portrait)
                    throw new InvalidOperationException("Same-value release settings precondition differs.");
                stage = "P2"; report.save_assets_calls++; AssetDatabase.SaveAssets(); observe("P2");
                stage = "P3a"; report.non_graphics_setters++; PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend); observe("P3a");
                stage = "P3b"; report.non_graphics_setters++; PlayerSettings.Android.targetArchitectures = architecture; observe("P3b");
                stage = "P3c"; report.non_graphics_setters++; PlayerSettings.defaultInterfaceOrientation = orientation; observe("P3c");
                restoreOwed = true;
                stage = "P4a"; report.signing_setting_setters++; PlayerSettings.Android.useCustomKeystore = true; observe("P4a");
                stage = "P4b"; report.signing_setting_setters++; PlayerSettings.Android.keystoreName = input[0]; observe("P4b");
                stage = "P4c"; report.signing_setting_setters++; PlayerSettings.Android.keystorePass = input[1]; observe("P4c");
                stage = "P4d"; report.signing_setting_setters++; PlayerSettings.Android.keyaliasName = input[2]; observe("P4d");
                stage = "P4e"; report.signing_setting_setters++; PlayerSettings.Android.keyaliasPass = input[3]; observe("P4e");
                stage = "P5a"; restoreAttempted |= 1; report.signing_restore_setters++; PlayerSettings.Android.useCustomKeystore = originalCustom; restoreSucceeded |= 1; observe("P5a");
                stage = "P5b"; restoreAttempted |= 2; report.signing_restore_setters++; PlayerSettings.Android.keystoreName = original[0]; restoreSucceeded |= 2; observe("P5b");
                stage = "P5c"; restoreAttempted |= 4; report.signing_restore_setters++; PlayerSettings.Android.keystorePass = original[1]; restoreSucceeded |= 4; observe("P5c");
                stage = "P5d"; restoreAttempted |= 8; report.signing_restore_setters++; PlayerSettings.Android.keyaliasName = original[2]; restoreSucceeded |= 8; observe("P5d");
                stage = "P5e"; restoreAttempted |= 16; report.signing_restore_setters++; PlayerSettings.Android.keyaliasPass = original[3]; restoreSucceeded |= 16; observe("P5e");
                restoreOwed = false; report.original_signing_restored = true;
                observe("P5");
                if (skipP6)
                {
                    SettingsObservation.WriteNew(Path.Combine(directory, "p6-skipped.json"), "{\"p6_skipped\":true,\"after_stage\":\"P5\"}");
                    Debug.Log("MOFUMACHI_SERIALIZATION_PROBE_P6_SKIPPED");
                }
                else
                {
                    stage = "P6"; report.save_assets_calls++; AssetDatabase.SaveAssets(); observe("P6");
                }
                report.completed_stages = true; report.result = "StagesCompleted";
                Debug.Log("MOFUMACHI_SERIALIZATION_PROBE_COMPLETE");
            }
            catch (Exception error)
            {
                if (report != null) { report.result = "Failed"; report.error_type = error.GetType().FullName; report.error_stage = stage; }
                // Do not emit exception messages/stacks, which can contain private paths or values.
                Debug.LogError("MOFUMACHI_SERIALIZATION_PROBE_FAILED");
            }
            finally
            {
                if (restoreOwed && originalRead)
                {
                    // Safety restoration after a partial failure, without repeating the failed experiment.
                    // Continue restoration of other fields if a restore operation itself fails; no SaveAssets retry.
                    Action[] restore = {
                        () => PlayerSettings.Android.useCustomKeystore = originalCustom,
                        () => PlayerSettings.Android.keystoreName = original[0],
                        () => PlayerSettings.Android.keystorePass = original[1],
                        () => PlayerSettings.Android.keyaliasName = original[2],
                        () => PlayerSettings.Android.keyaliasPass = original[3]
                    };
                    for (int i = 0; i < restore.Length; i++)
                    {
                        // Never repeat a restore setter, including a setter that already failed.
                        int bit = 1 << i;
                        if ((restoreAttempted & bit) != 0) continue;
                        restoreAttempted |= bit;
                        try { report.signing_restore_setters++; restore[i](); restoreSucceeded |= bit; if (observe != null) observe("R" + i); }
                        catch (Exception) { /* Preserve the first failure; remaining fields still need restoration. */ }
                    }
                    report.original_signing_restored = restoreSucceeded == 31;
                }
                for (int i = 0; i < Names.Length; i++) Environment.SetEnvironmentVariable(Names[i], null);
                Array.Clear(input, 0, input.Length); Array.Clear(original, 0, original.Length);
                if (report != null && directory != null)
                {
                    try { SettingsObservation.WriteReport(directory, report); }
                    catch (Exception) { Debug.LogError("MOFUMACHI_SERIALIZATION_PROBE_REPORT_FAILED"); }
                }
            }
            if (report == null || !report.completed_stages) EditorApplication.Exit(1);
        }
    }
}
