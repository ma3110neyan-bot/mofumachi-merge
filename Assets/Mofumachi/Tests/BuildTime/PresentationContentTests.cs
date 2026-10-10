using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mofumachi.Tests
{
    public sealed class PresentationContentTests
    {
        [Test] public void ContentGateRejectsMissingAssetsAndUnsupportedGlyphs()
        {
            foreach(var key in new[]{"TownBackground","UIFont","UIFontBold","UIIconAtlas","bgm-town-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.RequireContent(null,key));
            var font=Resources.Load<Font>("Mofumachi/UIFont");
            Assert.That(font,Is.Not.Null);
            // None of these characters exists in the bundled subset. Include a
            // valid letter that Windows can substitute from an installed font.
            foreach(var text in new[]{"\uffff","\u0378","\u0416"})
                Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,text),"Must reject missing source glyph U+"+((int)text[0]).ToString("X4"));
            Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(null,"課金"));
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
            Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,string.Join("",UIStrings.All)+"可年必方相者許課談護量金\n\t "));
            var bold=Resources.Load<Font>("Mofumachi/UIFontBold");Assert.That(bold,Is.Not.Null);
            Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(bold,string.Join("",UIStrings.All)+UIStrings.PopLabels));
            Assert.That(Resources.Load<Texture2D>("Mofumachi/UIIconAtlas"),Is.Not.Null);
        }
    }
}
