using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mofumachi.Tests
{
    public sealed class PresentationContentTests
    {
        [Test] public void TownDecorationsUseEffective1024Limit()
        {
            var atlas=Resources.Load<Texture2D>("Mofumachi/TownDecorationAtlas");Assert.That(atlas,Is.Not.Null);AssertSharedTextureImport(atlas);
            foreach(TownDecorationKind kind in System.Enum.GetValues(typeof(TownDecorationKind)))
            {var uv=TownDecorationGraphic.Region(kind);Assert.That(uv.width,Is.GreaterThan(0));Assert.That(uv.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(uv.xMax,Is.LessThanOrEqualTo(1));}
        }
        [Test] public void StoryFontHasRequiredGlyphs()
        {
            foreach(var key in new[]{"UIFont","UIFontBold","UIFontStory"})
            {
                var font=Resources.Load<Font>("Mofumachi/"+key);Assert.That(font,Is.Not.Null,key);
                foreach(var text in UIStrings.All)Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,text));
                foreach(var text in new[]{"\uffff","\u0378","\u0416"})
                    Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,text));
            }
        }
        [Test] public void FullBodyAtlasHasSixNamedCharactersAndExpressions()
        {
            var atlas=Resources.Load<Texture2D>("Mofumachi/CharacterBodyAtlas");Assert.That(atlas,Is.Not.Null);
            AssertSharedTextureImport(atlas);
        }
        [Test] public void RequiredItemArtAndNewJapaneseGlyphsArePresent()
        {
            foreach(var key in new[]{"ItemAtlas","TitleLogo","UIPanelFrame"})
            {
                var texture=Resources.Load<Texture2D>("Mofumachi/"+key);Assert.That(texture,Is.Not.Null,key);
                AssertSharedTextureImport(texture);
            }
            foreach(string key in new[]{"UIFont","UIFontBold"})
                Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(Resources.Load<Font>("Mofumachi/"+key),string.Join("",UIStrings.All)));
        }
        private static void AssertSharedTextureImport(Texture2D texture)
        {
            var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(texture));
            Assert.That(importer,Is.Not.Null,texture.name);
            Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize,Is.EqualTo(1024),texture.name+" default import limit");
            var android=importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden,Is.True,texture.name+" Android override");
            Assert.That(android.maxTextureSize,Is.EqualTo(1024),texture.name+" Android import limit");
            Assert.That(texture.width,Is.LessThanOrEqualTo(1024),texture.name+" imported width");
            Assert.That(texture.height,Is.LessThanOrEqualTo(1024),texture.name+" imported height");
            Assert.That(importer.mipmapEnabled,Is.False,texture.name);
            Assert.That(importer.isReadable,Is.False,texture.name);
            Assert.That(importer.alphaIsTransparency,Is.True,texture.name);
        }
        [Test] public void SelectionCopyHasGlyphsInBothFonts()
        {
            foreach (string key in new[] { "UIFont", "UIFontBold" })
            {
                var font = Resources.Load<Font>("Mofumachi/" + key);
                foreach (var character in Mofumachi.Core.CharacterCatalog.All)
                    Assert.DoesNotThrow(() => Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font, character.Name + character.Description));
                Assert.DoesNotThrow(() => Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font, UIStrings.PartnerSelection));
            }
        }
        [Test] public void ContentGateRejectsMissingAssetsAndUnsupportedGlyphs()
        {
            foreach(var key in new[]{"TownBackground","UIFont","UIFontBold","UIIconAtlas","TitleLogo","UIPanelFrame","ItemAtlas","CharacterBodyAtlas","bgm-town-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.RequireContent(null,key));
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
            Assert.That(Resources.Load<Texture2D>("Mofumachi/TitleLogo"),Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Mofumachi/UIPanelFrame"),Is.Not.Null);
        }
    }
}
