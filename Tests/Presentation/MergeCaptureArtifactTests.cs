using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Mofumachi.Tests;
namespace Mofumachi.CaptureTests
{
    // Tests only the capture output contract; no game/Unity/device tests run here.
    public sealed class MergeCaptureArtifactTests
    {
        private string root;
        [SetUp] public void Setup()=>root=Path.Combine(Path.GetTempPath(),"merge-capture-files-"+Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup(){if(Directory.Exists(root))Directory.Delete(root,true);}
        [Test] public void MissingOutputArgumentCannotPassInMemory()
            =>Assert.Throws<InvalidOperationException>(()=>MergeCaptureArtifacts.RequireOutput(Array.Empty<string>()));
        [TestCase("")][TestCase(" ")][TestCase("screenshots")][TestCase("-testResults")]
        public void OutputMustBeAnExplicitAbsoluteDirectory(string value)
            =>Assert.Throws<InvalidOperationException>(()=>MergeCaptureArtifacts.RequireOutput(new[]{"-mofumachiMergeReward",value}));
        [Test] public void DuplicateOutputArgumentsFailInsteadOfChoosingAnotherRun()
            =>Assert.Throws<InvalidOperationException>(()=>MergeCaptureArtifacts.RequireOutput(new[]{"-mofumachiMergeReward",root,"-mofumachiMergeReward",root+"-other"}));
        [Test] public void FreshOutputIsCreatedAtExactlyThePassedPath()
        {
            string actual=MergeCaptureArtifacts.RequireOutput(new[]{"-mofumachiMergeReward",root});
            Assert.That(actual,Is.EqualTo(Path.GetFullPath(root)));Assert.That(Directory.Exists(actual),Is.True);
        }
        [Test] public void FilteredCaptureCannotSilentlyFallBackWhenItsOutputArgumentIsMissing()
            =>Assert.Throws<InvalidOperationException>(()=>MergeCaptureArtifacts.RequireOutput(new[]{"-runTests","-testPlatform","PlayMode","-testResults",Path.Combine(root,"capture.xml"),"-testFilter","Mofumachi.Tests.MergeRewardCaptureTests.MergeRewardCandidateScreens"}));
        [Test] public void ExistingEvidenceIsPreservedRatherThanReusedAsNewSuccess()
        {
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"old.json"),"old");
            Assert.Throws<IOException>(()=>MergeCaptureArtifacts.RequireOutput(new[]{"-mofumachiMergeReward",root}));
            Assert.That(File.ReadAllText(Path.Combine(root,"old.json")),Is.EqualTo("old"));
        }
        private Dictionary<string,string> Fixture()
        {
            Directory.CreateDirectory(root);var hashes=new Dictionary<string,string>();
            foreach(var name in MergeCaptureArtifacts.ImageNames)
            {byte[] bytes=System.Text.Encoding.UTF8.GetBytes("synthetic output contract: "+name);File.WriteAllBytes(Path.Combine(root,name),bytes);hashes[name]=MergeCaptureArtifacts.Hash(bytes);}
            File.WriteAllText(Path.Combine(root,"merge-reward-native.json"),"{\"synthetic\":true}");return hashes;
        }
        [Test] public void ElevenDiskFilesAndJsonAreRequired()
        {var hashes=Fixture();Assert.DoesNotThrow(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));}
        [Test] public void EmptyDirectoryCannotPassFromElevenDeclaredImages()
        {
            var hashes=Fixture();foreach(var file in Directory.GetFiles(root))File.Delete(file);
            Assert.Throws<IOException>(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));
        }
        [Test] public void TenImagesFailEvenWithElevenManifestEntries()
        {var hashes=Fixture();File.Delete(Path.Combine(root,MergeCaptureArtifacts.ImageNames[0]));Assert.Throws<IOException>(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));}
        [Test] public void MissingJsonFailsDespiteAllImagesExisting()
        {var hashes=Fixture();File.Delete(Path.Combine(root,"merge-reward-native.json"));Assert.Throws<IOException>(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));}
        [Test] public void EmptyJsonFailsDespiteAllImagesExisting()
        {var hashes=Fixture();File.WriteAllText(Path.Combine(root,"merge-reward-native.json"),"");Assert.Throws<IOException>(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));}
        [Test] public void ChangedOrZeroLengthImageFails()
        {var hashes=Fixture();File.WriteAllBytes(Path.Combine(root,MergeCaptureArtifacts.ImageNames[0]),Array.Empty<byte>());Assert.Throws<IOException>(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));}
        [Test] public void ExtraImageDoesNotBelongToTheElevenShotCapture()
        {var hashes=Fixture();File.WriteAllText(Path.Combine(root,"other.jpg"),"other");Assert.Throws<IOException>(()=>MergeCaptureArtifacts.VerifyWritten(root,hashes));}
    }
}
