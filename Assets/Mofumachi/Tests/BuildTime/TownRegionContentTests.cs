using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mofumachi.Tests
{
    public sealed class TownRegionContentTests
    {
        [Test] public void RegionAssetsKeepApprovedSourceAndImportSettings()
        {
            string[] keys={"TownBackground","TownWestBackground","TownEastBackground"};
            string[] hashes={"971461342858e74c8db5f669f28c3e5c0c1a0f9d364e645ec55b9ea2834e7b51","a4bda540be5347d253d2decd010e2f96f4cb4b64861b4e73859338865342c328","2e45be7b35f6533279def009a0c187bba24f16f7725287e2ffb40e630b0a4e3e"};
            for(int i=0;i<keys.Length;i++)
            {
                var texture=Resources.Load<Texture2D>("Mofumachi/"+keys[i]);Assert.That(texture,Is.Not.Null,keys[i]);
                string path=AssetDatabase.GetAssetPath(texture);using(var sha=SHA256.Create())Assert.That(string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b=>b.ToString("x2"))),Is.EqualTo(hashes[i]),keys[i]+" approved painting");
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize,Is.EqualTo(2048));
                Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.isReadable,Is.False);
                Assert.That(texture.width,Is.LessThanOrEqualTo(2048));Assert.That(texture.height,Is.LessThanOrEqualTo(2048));
                if(i>0){var android=importer.GetPlatformTextureSettings("Android");Assert.That(android.overridden,Is.True);Assert.That(android.maxTextureSize,Is.EqualTo(2048));}
            }
        }
        [Test] public void RegionUiHasSourceGlyphs()
        {
            string text=UIStrings.RegionLabels+string.Concat(System.Enum.GetValues(typeof(TownRegionId)).Cast<TownRegionId>().SelectMany(r=>
                TownRegionCatalog.ForLevel(r,6).Map.Places.Select(p=>p.Name+p.Dialogue)));
            foreach(string key in new[]{"UIFont","UIFontBold","UIFontStory"})
                Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(Resources.Load<Font>("Mofumachi/"+key),text),key);
        }
    }
}
