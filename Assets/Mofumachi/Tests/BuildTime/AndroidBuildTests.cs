using Mofumachi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mofumachi.Tests
{
    public sealed class AndroidBuildTests
    {
        [Test]
        public void PixelBuildConfigurationIsPortraitArm64AndApk()
        {
            AndroidBuild.ConfigurePixel3a();
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo("com.mofumachi.merge"));
            Assert.That(PlayerSettings.Android.requestedVisibleInsets,Is.EqualTo(AndroidWindowInsetsType.None));
            Assert.That(PlayerSettings.Android.renderOutsideSafeArea,Is.True);
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android), Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.Android.minSdkVersion, Is.EqualTo(AndroidSdkVersions.AndroidApiLevel26));
            Assert.That((int)PlayerSettings.Android.targetSdkVersion, Is.EqualTo(36));
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(new[] { GraphicsDeviceType.OpenGLES3 }));
            Assert.That(EditorUserBuildSettings.buildAppBundle, Is.False);
            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(2));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo("Assets/Scenes/TitleScene.unity"));
            AndroidBuild.ValidateContent();
        }
    }
}
