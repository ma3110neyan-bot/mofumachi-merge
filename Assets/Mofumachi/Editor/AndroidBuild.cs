using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mofumachi.Editor
{
    public static class AndroidBuild
    {
        public const string ApplicationId = "com.mofumachi.merge";
        public static readonly string[] Scenes = { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" };

        [MenuItem("Mofumachi/Android/Configure Pixel 3a")]
        public static void ConfigurePixel3a()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.productName = "もふまちメルジュ";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 1080; PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // AndroidX Core 1.15 requires compileSdk >= 35. Use Unity 6.6's API 36
            // platform for the generated Gradle project; minimum device API stays 26.
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = false;
            EditorBuildSettings.scenes = Scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }

        public static void ValidateContent()
        {
            foreach (var scene in Scenes)
            {
                if (!File.Exists(scene)) throw new BuildFailedException("Missing scene: " + scene);
                var contents = File.ReadAllText(scene);
                string bootstrap = AssetDatabase.AssetPathToGUID("Assets/Mofumachi/Presentation/UIFlowController.cs");
                if (bootstrap.Length == 0 || !contents.Contains("guid: " + bootstrap))
                    throw new BuildFailedException("Scene has no Mofumachi bootstrap: " + scene);
            }
            if (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mofumachi/Resources/Mofumachi/CharacterMaster.png") == null ||
                AssetDatabase.LoadAssetAtPath<Font>("Assets/Mofumachi/Resources/Mofumachi/UIFont.otf") == null)
                throw new BuildFailedException("Character master or Japanese UI font is missing or failed to import.");
        }

        [MenuItem("Mofumachi/Android/Build Development APK")]
        public static void BuildDevelopmentApk()
        {
            if (Application.unityVersion != "6000.6.4f1")
                throw new BuildFailedException("Use the project-pinned Unity 6000.6.4f1.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, Android SDK & NDK Tools and OpenJDK in Unity Hub.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Activate the Android Build Profile first. Batch builds require -buildTarget Android.");
            ConfigurePixel3a(); ValidateContent(); AssetDatabase.SaveAssets();
            string output = ResolveOutput();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // Publish this attempt only after success; an older APK is preserved on failure.
            string temporary = Path.Combine(Path.GetDirectoryName(output), "vertical-slice-" + Guid.NewGuid().ToString("N") + ".apk");
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes, locationPathName = temporary, target = BuildTarget.Android,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(temporary) || new FileInfo(temporary).Length == 0)
                    throw new BuildFailedException("APK build failed: " + report.summary.result + "; errors: " + report.summary.totalErrors);
                if (File.Exists(output)) File.Delete(output);
                File.Move(temporary, output);
                Debug.Log("MOFUMACHI_APK_SUCCESS " + output + " (" + new FileInfo(output).Length + " bytes)");
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static string ResolveOutput()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-mofumachiApkPath");
            if (index >= 0)
            {
                if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new BuildFailedException("-mofumachiApkPath requires an APK file path.");
                if (!args[index + 1].EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException("Output path must end in .apk.");
                return Path.GetFullPath(args[index + 1]);
            }
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Android/vertical-slice.apk"));
        }
    }
}
