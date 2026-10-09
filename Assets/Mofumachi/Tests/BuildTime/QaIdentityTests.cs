using System;
using Mofumachi.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
namespace Mofumachi.Tests
{
    public sealed class QaIdentityTests
    {
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
