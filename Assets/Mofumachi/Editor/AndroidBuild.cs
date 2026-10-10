using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;

namespace Mofumachi.Editor
{
    public static class AndroidBuild
    {
        public const string QaApplicationId = "com.mofumachi.merge.qa";
        public const string ApplicationId = "com.mofumachi.merge";
        public static readonly string[] Scenes = { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" };

        [MenuItem("Mofumachi/Android/Configure Pixel 3a")]
        public static void ConfigurePixel3a()
        {
            ConfigureDeviceSettings();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.productName = "もふまちメルジュ";
        }
        private static void ConfigureDeviceSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
            PlayerSettings.Android.requestedVisibleInsets=AndroidWindowInsetsType.None;PlayerSettings.Android.renderOutsideSafeArea=true;
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

        public static void RequireContent(UnityEngine.Object asset,string label)
        { if(asset==null)throw new BuildFailedException("Missing or failed to import: "+label); }
        public static void ValidateGlyphs(Font font,string text)
        {
            RequireContent(font,"UIFont");
            var error=FontEngine.InitializeFontEngine();
            if(error!=FontEngineError.Success)throw new BuildFailedException("Cannot initialize UI font validation: "+error);
            error=FontEngine.LoadFontFace(font,16);
            if(error!=FontEngineError.Success)throw new BuildFailedException("Cannot load bundled UI font data: "+error);
            // Font.HasCharacter can accept a substituted/missing glyph in the
            // dynamic renderer. Check the bundled face's Unicode mapping instead.
            foreach(char ch in text)
                if(!char.IsWhiteSpace(ch)&&(!FontEngine.TryGetGlyphIndex(ch,out uint glyphIndex)||glyphIndex==0))
                    throw new BuildFailedException("UI font missing glyph: U+"+((int)ch).ToString("X4"));
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
            RequireContent(Resources.Load<Texture2D>("Mofumachi/CharacterMaster"),"CharacterMaster");
            RequireContent(Resources.Load<Texture2D>("Mofumachi/TownBackground"),"TownBackground");
            var font=Resources.Load<Font>("Mofumachi/UIFont");RequireContent(font,"UIFont");
            foreach(var text in Mofumachi.Presentation.UIStrings.All)ValidateGlyphs(font,text);
            foreach(var key in new[]{"bgm-town-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})RequireContent(Resources.Load<AudioClip>("Mofumachi/Audio/"+key),key);

        }

        public static void WithQaIdentity(Action action)
        {
            string id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),name=PlayerSettings.productName;
            try { PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,QaApplicationId);PlayerSettings.productName="もふまちメルジュ QA";action(); }
            finally {PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,id);PlayerSettings.productName=name;AssetDatabase.SaveAssets();}
        }
        [MenuItem("Mofumachi/Android/Build QA APK (separate save)")]
        public static void BuildQaApk() => WithQaIdentity(()=>{ConfigureDeviceSettings();BuildConfiguredApk(true);});
        [MenuItem("Mofumachi/Android/Build Development APK")]
        public static void BuildDevelopmentApk()
        { ConfigurePixel3a();BuildConfiguredApk(false); }
        private static void BuildConfiguredApk(bool qa)
        {
            if (Application.unityVersion != "6000.6.4f1")
                throw new BuildFailedException("Use the project-pinned Unity 6000.6.4f1.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, Android SDK & NDK Tools and OpenJDK in Unity Hub.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new BuildFailedException("Activate the Android Build Profile first. Batch builds require -buildTarget Android.");
            ValidateContent(); AssetDatabase.SaveAssets();
            string output = ResolveOutput(qa);
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
                if (File.Exists(output)) File.Replace(temporary,output,null);
                else File.Move(temporary, output);
                Debug.Log("MOFUMACHI_APK_SUCCESS " + output + " (" + new FileInfo(output).Length + " bytes)");
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static string ResolveOutput(bool qa)
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
            return Path.GetFullPath(Path.Combine(Application.dataPath, qa?"../Builds/Android/vertical-slice-qa.apk":"../Builds/Android/vertical-slice.apk"));
        }
    }
}
