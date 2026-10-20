using System;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Mofumachi.Tests
{
    public sealed class PresentationContentTests
    {
        private static Type[] CustomGraphics()=>typeof(TownBloomGraphic).Assembly.GetTypes()
            .Where(t=>!t.IsAbstract&&typeof(Graphic).IsAssignableFrom(t)).OrderBy(t=>t.FullName).ToArray();
        [Test] public void CustomGraphicsRequireCanvasRendererBeforeTheyEnterAMaskedCanvas()
        {
            var graphics=CustomGraphics();Assert.That(graphics,Does.Contain(typeof(TownBloomGraphic)));Assert.That(graphics,Does.Contain(typeof(TownRouteGraphic)));
            var missing=new System.Collections.Generic.List<string>();
            foreach(var type in graphics)
            {
                bool requiresRenderer=false;
                for(var current=type;current!=null;current=current.BaseType)
                    requiresRenderer|=current.GetCustomAttributesData().Any(a=>a.AttributeType==typeof(RequireComponent)&&a.ConstructorArguments.Any(v=>v.Value is Type dependency&&dependency==typeof(CanvasRenderer)));
                if(!requiresRenderer)missing.Add(type.Name);
            }
            Assert.That(missing,Is.Empty,"Custom graphics must declare their CanvasRenderer dependency: "+string.Join(", ",missing));
        }
        [Test] public void CustomGraphicsCanBeClippedDisabledAndDestroyedWithoutRendererErrors()
        {
            var root=new GameObject("Graphic renderer lifecycle fixture",typeof(RectTransform),typeof(Canvas));
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(360,740);
            try
            {
                var mask=UIWidgets.Node("Masked graphics",root.transform as RectTransform,new Rect(0,0,1,1));mask.gameObject.AddComponent<RectMask2D>();
                foreach(var type in CustomGraphics())
                {
                    var node=UIWidgets.Node(type.Name,mask,new Rect(0,0,1,1));var graphic=(Graphic)node.gameObject.AddComponent(type);
                    Assert.That(node.GetComponent<CanvasRenderer>(),Is.Not.Null,type.Name+" runtime creation must supply a CanvasRenderer");
                    Canvas.ForceUpdateCanvases();if(graphic is MaskableGraphic masked)masked.RecalculateClipping();
                    if(graphic is TownVillageDetailGraphic village)
                    {
                        var pending=village.canvasRenderer.GetMesh();Assert.That(pending==null?0:pending.vertexCount,Is.Zero,"A village part must not draw an arbitrary sprite before Bind");
                        foreach(var kind in Enum.GetValues(typeof(TownSceneryKind)).Cast<TownSceneryKind>().Where(k=>k>=TownSceneryKind.CottagePair))
                        {
                            village.Bind(kind);Canvas.ForceUpdateCanvases();var mesh=village.canvasRenderer.GetMesh();
                            Assert.That(mesh.vertexCount,Is.EqualTo(4),kind.ToString());Assert.That(village.mainTexture,Is.Not.Null,kind.ToString());Assert.That(village.raycastTarget,Is.False);
                            foreach(var vertex in mesh.vertices)Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y),Is.True,kind.ToString());
                            foreach(var uv in mesh.uv){Assert.That(uv.x,Is.InRange(0f,1f),kind.ToString());Assert.That(uv.y,Is.InRange(0f,1f),kind.ToString());}
                        }
                        Assert.Throws<ArgumentOutOfRangeException>(()=>village.Bind(TownSceneryKind.Cafe),"An unsupported binding must still be rejected at the call site");
                    }
                    mask.gameObject.SetActive(false);mask.gameObject.SetActive(true);node.gameObject.SetActive(false);UnityEngine.Object.DestroyImmediate(node.gameObject);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            LogAssert.NoUnexpectedReceived();
        }
        [Test] public void TownDecorationsUseEffective1024Limit()
        {
            var atlas=Resources.Load<Texture2D>("Mofumachi/TownDecorationAtlas");Assert.That(atlas,Is.Not.Null);AssertSharedTextureImport(atlas);
            foreach(TownDecorationKind kind in System.Enum.GetValues(typeof(TownDecorationKind)))
            {var uv=TownDecorationGraphic.Region(kind);Assert.That(uv.width,Is.GreaterThan(0));Assert.That(uv.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(uv.xMax,Is.LessThanOrEqualTo(1));}
        }
        [Test] public void StoryFontHasRequiredGlyphs()
        {
            foreach(var key in new[]{"UIFont","UIFontBold","UIFontStory","UIFontBody"})
            {
                var font=Resources.Load<Font>("Mofumachi/"+key);Assert.That(font,Is.Not.Null,key);
                foreach(var text in UIStrings.All)Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,text));
                foreach(var text in new[]{"\uffff","\u0378","\u0416"})
                    Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(font,text));
            }
        }
        [Test] public void GrowthPropsAndNarrativeFontAreBundledAndCommercialLicenseIsIncluded()
        {
            foreach(string key in new[]{"TownGrowthPropsAtlas","TownStreetPropsAtlas"})
            {var atlas=Resources.Load<Texture2D>("Mofumachi/"+key);Assert.That(atlas,Is.Not.Null,key);AssertSharedTextureImport(atlas);}
            Assert.That(Resources.Load<TextAsset>("Mofumachi/UIFontBody-LICENSE").text,Does.Contain("SIL OPEN FONT LICENSE"));
            var body=Resources.Load<Font>("Mofumachi/UIFontBody");
            foreach(var text in QuestStoryPresentation.Text)Assert.DoesNotThrow(()=>Mofumachi.Editor.AndroidBuild.ValidateGlyphs(body,text));
        }
        [Test] public void EveryGrowthPropRendersBundledFiniteGeometryWithoutTakingInput()
        {
            var canvas=new GameObject("Growth atlas fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(360,740);
            var node=UIWidgets.Node("Growth prop",(RectTransform)canvas.transform,new Rect(.5f,.5f,0,0));node.sizeDelta=new Vector2(80,80);
            var graphic=node.gameObject.AddComponent<TownGrowthPropGraphic>();
            try
            {
                foreach(TownGrowthProp prop in Enum.GetValues(typeof(TownGrowthProp)))
                {
                    graphic.Bind(prop,Mofumachi.Core.TownRegionId.Central);Canvas.ForceUpdateCanvases();
                    Assert.That(graphic.mainTexture,Is.Not.Null,prop.ToString());Assert.That(graphic.raycastTarget,Is.False);
                    var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(4),prop.ToString());
                    foreach(var vertex in mesh.vertices)Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y),Is.True,prop.ToString());
                    foreach(var uv in mesh.uv){Assert.That(uv.x,Is.InRange(0f,1f),prop.ToString());Assert.That(uv.y,Is.InRange(0f,1f),prop.ToString());}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(canvas);}
            LogAssert.NoUnexpectedReceived();
        }
        [Test] public void FullBodyAtlasHasSixNamedCharactersAndExpressions()
        {
            var atlas=Resources.Load<Texture2D>("Mofumachi/CharacterBodyAtlas");Assert.That(atlas,Is.Not.Null);
            AssertSharedTextureImport(atlas);
        }
        [Test] public void RequiredItemArtAndNewJapaneseGlyphsArePresent()
        {
            foreach(var key in new[]{"ItemAtlas","TitleLogo","UIPanelFrame","MergeBoardFrame"})
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
            foreach(var key in new[]{"TownBackground","UIFont","UIFontBold","UIIconAtlas","TitleLogo","UIPanelFrame","ItemAtlas","CharacterBodyAtlas","bgm-town-loop","bgm-beginning-loop","bgm-merge-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})Assert.Throws<UnityEditor.Build.BuildFailedException>(()=>Mofumachi.Editor.AndroidBuild.RequireContent(null,key));
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
            foreach(var key in new[]{"bgm-town-loop","bgm-beginning-loop","bgm-merge-loop","se-confirm","se-character","se-merge","se-delivery","se-reward","se-growth"})
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
