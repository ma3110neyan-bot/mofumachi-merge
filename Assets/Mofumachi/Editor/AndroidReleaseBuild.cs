using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mofumachi.Editor
{
    // Batch entry points only. Never call the Development/QA configuration.
    public static class AndroidReleaseBuild
    {
        private const string Root = @"E:\Mofumachi-Validation\";
        private const string Package = "com.mofumachi.merge";
        private const string ProductRevision = "46f65b24d58531d5934035fbe4cdb4d5b00104a6";

        public static void BuildReleaseAab() => Build(true);
        public static void BuildReleaseApk() => Build(false);

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                throw new BuildFailedException("Missing release argument: " + name);
            return args[index + 1];
        }

        private static string UnderE(string path, string relativeRoot)
        {
            string full = Path.GetFullPath(path);
            string boundary = Path.GetFullPath(Path.Combine(Root, relativeRoot)).TrimEnd('\\') + "\\";
            if (!full.StartsWith(boundary, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Release project/output must remain under the designated E-drive root.");
            string ancestor = full;
            while (!string.IsNullOrEmpty(ancestor))
            {
                if ((File.Exists(ancestor) || Directory.Exists(ancestor)) &&
                    (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                    throw new BuildFailedException("Linked release paths are not allowed.");
                ancestor = Path.GetDirectoryName(ancestor);
            }
            return full;
        }

        private sealed class TemporaryGraphicsSettings
        {
            public GraphicsDeviceType[] InitialApis { get; } = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            public bool InitialAutomatic { get; } = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            public bool AlreadyDesired => !InitialAutomatic && InitialApis != null &&
                InitialApis.Length == 1 && InitialApis[0] == GraphicsDeviceType.OpenGLES3;
            public bool Changed { get; private set; }
            public bool Restored { get; private set; }

            public void Apply()
            {
                if (AlreadyDesired) return;
                // Retain the restore obligation even if a setter fails after a partial change.
                Changed = true;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            }

            public void Restore()
            {
                if (!Changed) return;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, InitialApis);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, InitialAutomatic);
                Restored = true;
            }
        }

        [Serializable]
        private sealed class BuildInfo
        {
            public string source_commit, product_commit = ProductRevision, unity_version = "6000.6.4f1";
            public string package = Package, version, format, output_path, signing_certificate_sha256;
            public string backend = "IL2CPP", architecture = "ARM64", orientation = "Portrait", graphics_api = "OpenGLES3";
            public int version_code, minimum_api, target_api, build_errors;
            public string result = "Failed";
            public bool development, script_debugging, deep_profiling, autoconnect_profiler;
            public bool build_app_bundle, custom_signing, temporary_settings_restored;
            public string[] initial_graphics_apis;
            public bool initial_automatic, graphics_settings_already_desired, graphics_settings_changed, graphics_settings_restored;
            public string build_options = "None";
        }

        private static void Build(bool aab)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor || Application.unityVersion != "6000.6.4f1")
                throw new BuildFailedException("Release builds require Windows Unity 6000.6.4f1.");
            UnderE(Path.GetDirectoryName(Application.dataPath), "projects");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
                !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Android Build Support and the Android target are required.");
            string revision = Argument("-mofumachiReleaseRevision");
            if (!Regex.IsMatch(revision, "^[a-f0-9]{40}$"))
                throw new BuildFailedException("Release tooling revision must be a full commit SHA.");
            string output = UnderE(Argument("-mofumachiReleaseOutput"), "release-candidate");
            string infoPath = UnderE(Argument("-mofumachiReleaseInfo"), "Diagnostics");
            if (!output.EndsWith(aab ? ".aab" : ".apk", StringComparison.OrdinalIgnoreCase) ||
                File.Exists(output) || Directory.Exists(output) || File.Exists(infoPath))
                throw new BuildFailedException("Release output must have the requested format and must not already exist.");

            string[] names = { "MOFUMACHI_KEYSTORE_PATH", "MOFUMACHI_KEYSTORE_PASSWORD", "MOFUMACHI_KEY_ALIAS", "MOFUMACHI_KEY_ALIAS_PASSWORD" };
            string[] signing = names.Select(Environment.GetEnvironmentVariable).ToArray();
            // Native child tools must not inherit the signing secrets as environment dumps.
            foreach (string name in names) Environment.SetEnvironmentVariable(name, null);
            if (signing.Any(string.IsNullOrWhiteSpace) || !File.Exists(signing[0]) ||
                (File.GetAttributes(signing[0]) & FileAttributes.ReparsePoint) != 0)
                throw new BuildFailedException("Release signing inputs are missing or invalid; values are never printed.");
            string fingerprint = Environment.GetEnvironmentVariable("MOFUMACHI_SIGNING_CERT_SHA256");
            if (fingerprint == null || !Regex.IsMatch(fingerprint, "^[a-f0-9]{64}$"))
                throw new BuildFailedException("A verified signing certificate SHA256 from release preflight is required.");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != Package)
                throw new BuildFailedException("Release package differs from com.mofumachi.merge.");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string[] expectedScenes = { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" };
            if (!scenes.SequenceEqual(expectedScenes) || scenes.Any(s => !File.Exists(s)))
                throw new BuildFailedException("Release scene list differs from the pinned game.");

            // Restore only settings this entry temporarily assigns, including private signing bindings.
            // This never performs a Git restore or hides unrelated Unity-generated content changes.
            bool development = EditorUserBuildSettings.development, debugging = EditorUserBuildSettings.allowDebugging;
            bool deep = EditorUserBuildSettings.buildWithDeepProfilingSupport, profiler = EditorUserBuildSettings.connectProfiler;
            bool bundle = EditorUserBuildSettings.buildAppBundle, export = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool custom = PlayerSettings.Android.useCustomKeystore;
            string keyPath = PlayerSettings.Android.keystoreName, storePassword = PlayerSettings.Android.keystorePass;
            string alias = PlayerSettings.Android.keyaliasName, keyPassword = PlayerSettings.Android.keyaliasPass;
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            var architecture = PlayerSettings.Android.targetArchitectures;
            var orientation = PlayerSettings.defaultInterfaceOrientation;
            var graphicsSettings = new TemporaryGraphicsSettings();
            var info = new BuildInfo { source_commit = revision, format = aab ? "AAB" : "APK", output_path = output,
                signing_certificate_sha256 = fingerprint, version = PlayerSettings.bundleVersion,
                version_code = PlayerSettings.Android.bundleVersionCode, minimum_api = (int)PlayerSettings.Android.minSdkVersion,
                target_api = (int)PlayerSettings.Android.targetSdkVersion,
                initial_graphics_apis = graphicsSettings.InitialApis?.Select(api => api.ToString()).ToArray(),
                initial_automatic = graphicsSettings.InitialAutomatic, graphics_settings_already_desired = graphicsSettings.AlreadyDesired };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Directory.CreateDirectory(Path.GetDirectoryName(infoPath));
            string temporary = Path.Combine(Path.GetDirectoryName(output), "building-" + Guid.NewGuid().ToString("N") + (aab ? ".aab" : ".apk"));
            try
            {
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
                EditorUserBuildSettings.connectProfiler = false;
                EditorUserBuildSettings.buildAppBundle = aab;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                graphicsSettings.Apply();
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = signing[0]; PlayerSettings.Android.keystorePass = signing[1];
                PlayerSettings.Android.keyaliasName = signing[2]; PlayerSettings.Android.keyaliasPass = signing[3];
                if (EditorUserBuildSettings.development || EditorUserBuildSettings.allowDebugging ||
                    EditorUserBuildSettings.buildWithDeepProfilingSupport || EditorUserBuildSettings.connectProfiler ||
                    !PlayerSettings.Android.useCustomKeystore || EditorUserBuildSettings.buildAppBundle != aab ||
                    PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP ||
                    PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64 ||
                    PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait ||
                    PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel26 ||
                    PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevel36)
                    throw new BuildFailedException("Effective release settings failed their pre-build gate.");
                info.development = EditorUserBuildSettings.development; info.script_debugging = EditorUserBuildSettings.allowDebugging;
                info.deep_profiling = EditorUserBuildSettings.buildWithDeepProfilingSupport; info.autoconnect_profiler = EditorUserBuildSettings.connectProfiler;
                info.build_app_bundle = aab; info.custom_signing = PlayerSettings.Android.useCustomKeystore;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes, locationPathName = temporary, target = BuildTarget.Android, options = BuildOptions.None
                });
                info.build_errors = report.summary.totalErrors;
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(temporary) || new FileInfo(temporary).Length == 0)
                    throw new BuildFailedException("Release build did not succeed; inspect the first Android/Gradle/IL2CPP error in the sanitized build log.");
                File.Move(temporary, output); // Move without overwrite, even if another file appeared after preflight.
                info.result = "Passed";
            }
            finally
            {
                PlayerSettings.Android.keystoreName = keyPath; PlayerSettings.Android.keystorePass = storePassword;
                PlayerSettings.Android.keyaliasName = alias; PlayerSettings.Android.keyaliasPass = keyPassword;
                PlayerSettings.Android.useCustomKeystore = custom;
                EditorUserBuildSettings.development = development; EditorUserBuildSettings.allowDebugging = debugging;
                EditorUserBuildSettings.buildWithDeepProfilingSupport = deep; EditorUserBuildSettings.connectProfiler = profiler;
                EditorUserBuildSettings.buildAppBundle = bundle; EditorUserBuildSettings.exportAsGoogleAndroidProject = export;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
                PlayerSettings.Android.targetArchitectures = architecture; PlayerSettings.defaultInterfaceOrientation = orientation;
                info.graphics_settings_changed = graphicsSettings.Changed;
                graphicsSettings.Restore();
                info.graphics_settings_restored = graphicsSettings.Restored;
                Array.Clear(signing, 0, signing.Length);
                AssetDatabase.SaveAssets();
                info.temporary_settings_restored = true;
                using (var stream = new FileStream(infoPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(JsonUtility.ToJson(info, true));
            }
            Debug.Log("MOFUMACHI_RELEASE_SUCCESS");
        }
    }
}
