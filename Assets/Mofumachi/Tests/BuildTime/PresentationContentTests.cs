using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mofumachi.Tests
{
    public sealed class PresentationContentTests
    {
        [Test] public void ContentGateRejectsMissingAssetsAndUnsupportedGlyphs()
        {
            foreach(var key in new[]{"TownBackground","UIFont","bgm-town-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.RequireContent(null,key));
            var font=Resources.Load<Font>("Mofumachi/UIFont");Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,"\uffff"));
            Mofumachi.Editor.AndroidBuild.ValidateContent();
        }
        [Test] public void RecordedAudioAssetsExistAndHaveExpectedImportSettings()
        {
            foreach(var key in new[]{"bgm-town-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})
            {
                var clip=Resources.Load<AudioClip>("Mofumachi/Audio/"+key); Assert.That(clip,Is.Not.Null,key);Assert.That(clip.length,Is.GreaterThan(.1f));
                var importer=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(clip));
                Assert.That(importer.defaultSampleSettings.loadType,Is.EqualTo(key.StartsWith("bgm")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad));
            }
        }
        [Test] public void RequiredJapaneseTextHasGlyphs()
        {
            var font = Resources.Load<Font>("Mofumachi/UIFont");
            Assert.That(font, Is.Not.Null);
            foreach (char c in string.Join("", UIStrings.All).Concat("可年必方相者許課談護量金").Distinct())
                if (!char.IsWhiteSpace(c)) Assert.That(font.HasCharacter(c), Is.True, "Missing glyph: " + c);
        }
    }
}
