using System;
using Mofumachi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
namespace Mofumachi.Tests
{
    public sealed class QaIdentityTests
    {
        [Test] public void GrowthCheckUsesASeparateSaveIdentityAndRestoresBuildSettings()
        {
            string id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),name=PlayerSettings.productName;
            AndroidBuild.WithGrowthQaIdentity(()=>{
                Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo("com.mofumachi.merge.qa.growthcheck"));
                Assert.That(PlayerSettings.productName,Is.EqualTo("もふまちメルジュ 成長確認"));
            });
            Assert.Throws<InvalidOperationException>(()=>AndroidBuild.WithGrowthQaIdentity(()=>{throw new InvalidOperationException("Growth build failure");}));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo(id));Assert.That(PlayerSettings.productName,Is.EqualTo(name));
        }
        [Test] public void TownQaIdentityRestoredOnSuccessAndFailure()
        {
            string id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),name=PlayerSettings.productName;
            AndroidBuild.WithTownQaIdentity(()=>{Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo("com.mofumachi.merge.qa.townlife"));Assert.That(PlayerSettings.productName,Is.EqualTo("もふまちメルジュ 街QA"));});
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo(id));Assert.That(PlayerSettings.productName,Is.EqualTo(name));
            Assert.Throws<InvalidOperationException>(()=>AndroidBuild.WithTownQaIdentity(()=>{throw new InvalidOperationException("Town build failure");}));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo(id));Assert.That(PlayerSettings.productName,Is.EqualTo(name));
        }
        [Test] public void QaIdentityIsRestoredEvenWhenBuildThrows()
        {
            string id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),name=PlayerSettings.productName;
            Assert.Throws<InvalidOperationException>(()=>AndroidBuild.WithQaIdentity(()=>{
                Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo(AndroidBuild.QaApplicationId));throw new InvalidOperationException("Simulated build failure");
            }));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),Is.EqualTo(id));Assert.That(PlayerSettings.productName,Is.EqualTo(name));
        }
    }
}
