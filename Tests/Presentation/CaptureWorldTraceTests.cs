using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class CaptureWorldTraceTests
    {
        private string directory;
        [SetUp] public void CreateDirectory()=>directory=Path.Combine(Path.GetTempPath(),"mofu-capture-trace-"+Guid.NewGuid());
        [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private static void Append(string output,string trace)
        {
            var type=typeof(CaptureWorldTraceTests).Assembly.GetType("Mofumachi.Tests.CaptureWorldTrace");
            Assert.That(type,Is.Not.Null,"Capture diagnostics must go to the review artifact rather than Unity's strict unexpected-log queue");
            type.GetMethod("Append",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{output,trace});
        }
        [Test] public void EveryCaptureTraceIsRetainedBesideThePicturesWithoutReplacingThem()
        {
            Directory.CreateDirectory(directory);var picture=Path.Combine(directory,"04-town-lv1-North.png");
            var bytes=new byte[]{1,2,3,4};File.WriteAllBytes(picture,bytes);
            var first="MOFUMACHI_CAPTURE_WORLD 04-town-lv1-North hero=momo モモ";
            var second="MOFUMACHI_CAPTURE_WORLD north-connection hero=momo camera=(0.0000,1.0000)";
            Append(directory,first);Append(directory,second);
            Assert.That(File.ReadAllLines(Path.Combine(directory,"capture-world.txt")),Is.EqualTo(new[]{first,second}));
            Assert.That(File.ReadAllBytes(picture),Is.EqualTo(bytes));
        }
        [Test] public void DistrictReviewFoldersKeepTheirOwnCoordinateEvidence()
        {
            var central=Path.Combine(directory,"central");var north=Path.Combine(directory,"north");
            Append(central,"central positions");Append(north,"north positions");
            Assert.That(File.ReadAllLines(Path.Combine(central,"capture-world.txt")),Is.EqualTo(new[]{"central positions"}));
            Assert.That(File.ReadAllLines(Path.Combine(north,"capture-world.txt")),Is.EqualTo(new[]{"north positions"}));
        }
    }
}
