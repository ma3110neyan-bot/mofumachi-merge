using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    // Optional actual-Unity output from the existing isolated font test. No new test registration.
    public static class HachiFontCapture
    {
        [Serializable] private sealed class FontRow {public string name,font,resource,style,reason;public int size,nominal_size;public bool hachi_fits;}
        [Serializable] private sealed class Shot {public string file,sha256,screen,state_sha256,geometry_sha256;public int width,height;public List<FontRow> fonts=new List<FontRow>();}
        [Serializable] private sealed class Proof
        {
            public string kind="hachi-font-only-native",source_revision,product_fingerprint,packed_resource_sha256,output_directory;
            public bool native_unity=true,disk_verified,visual_approved=false,pixel_verified=false;public int image_count;
            public List<Shot> images=new List<Shot>();
        }
        public static string Argument(string key){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i<0?null:i+1<args.Length?args[i+1]:"";}
        private static string Sha(byte[] bytes)=>MergeCaptureArtifacts.Hash(bytes);
        private static string State(UIFlowController flow)=>Sha(Encoding.UTF8.GetBytes(StateCodec.Encode(flow.Game.State)));
        private static string Geometry(UIFlowController flow)
        {
            var s=new StringBuilder();foreach(var r in flow.GetComponentsInChildren<RectTransform>())s.Append(r.name).Append(r.rect.ToString("F3")).Append(r.anchoredPosition.ToString("F3")).Append(r.localScale.ToString("F3"));
            return Sha(Encoding.UTF8.GetBytes(s.ToString()));
        }
        public static void AssertTypeface(Text text)
        {
            Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
            var role=ReleaseFontPolicy.For(text.name,text.fontSize,text.text);if(role==ReleaseFontRole.Unchanged)return;
            var font=text.font;var style=text.fontStyle;bool fit;
            try{text.font=Resources.Load<Font>("Mofumachi/UIFontAccent");text.fontStyle=FontStyle.Bold;fit=ReleaseTypography.Fits(text);}
            finally{text.font=font;text.fontStyle=style;}
            Assert.That(font,Is.EqualTo(Resources.Load<Font>("Mofumachi/"+ReleaseFontPolicy.Resource(role,fit))),text.name+": Hachi first; measured overflow only");
            Assert.That(style,Is.EqualTo(font==Resources.Load<Font>("Mofumachi/UIFontAccent")?FontStyle.Bold:FontStyle.Normal),text.name);
        }
        public static void VerifyMeasuredFallback()
        {
            var node=new GameObject("Coins value",typeof(RectTransform),typeof(Text));
            try
            {
                var text=node.GetComponent<Text>();text.text="0 / 3";text.fontSize=12;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.color=new Color(.3f,.2f,.25f);text.rectTransform.anchoredPosition=new Vector2(7,9);text.rectTransform.sizeDelta=new Vector2(120,80);
                ReleaseTypography.Apply(text);Assert.That(text.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")));
                text.rectTransform.sizeDelta=new Vector2(1,1);Canvas.ForceUpdateCanvases();Assert.That(text.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontReadingBold")));
                text.rectTransform.sizeDelta=new Vector2(120,80);Canvas.ForceUpdateCanvases();Assert.That(text.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")));
                Assert.That(node.GetComponents<HachiFontFit>().Length,Is.EqualTo(1));Assert.That(text.fontSize,Is.EqualTo(12));Assert.That(text.rectTransform.anchoredPosition,Is.EqualTo(new Vector2(7,9)));Assert.That(text.color,Is.EqualTo(new Color(.3f,.2f,.25f)));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
        public static void VerifyCompactHeaders()
        {
            var root=new GameObject("Compact Merge header regression",typeof(RectTransform),typeof(MergeRewardView));
            try
            {
                foreach(string name in new[]{"Current request caption","Merge reward coins"})
                {
                    var node=new GameObject(name,typeof(RectTransform),typeof(Text));node.transform.SetParent(root.transform,false);
                    var text=node.GetComponent<Text>();text.text=name=="Current request caption"?"現在の依頼":"達成ごほうび 30 Coins";text.fontSize=11;text.color=new Color(.3f,.2f,.25f);text.rectTransform.anchoredPosition=new Vector2(7,9);text.rectTransform.sizeDelta=new Vector2(200,12);
                    ReleaseTypography.Apply(text);Canvas.ForceUpdateCanvases();
                    Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(12.5f),name+": native failure was 16 in a 12px row");
                    Assert.That(text.fontSize,Is.InRange(8,10));Assert.That(text.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")));
                    text.rectTransform.sizeDelta=new Vector2(200,24);Canvas.ForceUpdateCanvases();Assert.That(text.fontSize,Is.EqualTo(11),"Restore authored size when it fits");
                    text.rectTransform.sizeDelta=new Vector2(200,12);Canvas.ForceUpdateCanvases();ReleaseTypography.Apply(text);
                    Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(12.5f));Assert.That(text.GetComponents<MergeCompactFontFit>().Length,Is.EqualTo(1));Assert.That(text.GetComponent<MergeCompactFontFit>().NominalSize,Is.EqualTo(11));
                    Assert.That(text.rectTransform.sizeDelta,Is.EqualTo(new Vector2(200,12)));Assert.That(text.rectTransform.anchoredPosition,Is.EqualTo(new Vector2(7,9)));Assert.That(text.color,Is.EqualTo(new Color(.3f,.2f,.25f)));
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void VerifyDescriptionFit()
        {
            var node=new GameObject("Settings description",typeof(RectTransform),typeof(Text));
            try
            {
                var text=node.GetComponent<Text>();text.text="下へスクロールすると、BGMを選べます♪";text.fontSize=14;text.lineSpacing=1.28f;text.color=new Color(.3f,.2f,.25f);text.rectTransform.anchoredPosition=new Vector2(7,9);text.rectTransform.sizeDelta=new Vector2(256,40);
                ReleaseTypography.Apply(text);Canvas.ForceUpdateCanvases();
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(40.5f));Assert.That(text.fontSize,Is.InRange(12,14));
                text.rectTransform.sizeDelta=new Vector2(296,40);Canvas.ForceUpdateCanvases();Assert.That(text.fontSize,Is.EqualTo(14));
                text.rectTransform.sizeDelta=new Vector2(256,40);Canvas.ForceUpdateCanvases();ReleaseTypography.Apply(text);
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(40.5f));Assert.That(text.GetComponents<HachiFontFit>().Length,Is.EqualTo(1));Assert.That(text.GetComponent<HachiFontFit>().NominalSize,Is.EqualTo(14));
                Assert.That(text.rectTransform.sizeDelta,Is.EqualTo(new Vector2(256,40)));Assert.That(text.rectTransform.anchoredPosition,Is.EqualTo(new Vector2(7,9)));Assert.That(text.color,Is.EqualTo(new Color(.3f,.2f,.25f)));Assert.That(text.lineSpacing,Is.EqualTo(1.28f));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
        public static void VerifyNarrowBookTitle()
        {
            var node=new GameObject("Page title",typeof(RectTransform),typeof(Text));
            try
            {
                var text=node.GetComponent<Text>();text.text="キャラクター図鑑";text.fontSize=18;text.lineSpacing=1.28f;text.color=new Color(.3f,.2f,.25f);text.rectTransform.anchoredPosition=new Vector2(7,9);text.rectTransform.sizeDelta=new Vector2(142.68f,37.7568f);
                ReleaseTypography.Apply(text);Canvas.ForceUpdateCanvases();
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(38.2568f));Assert.That(text.fontSize,Is.InRange(16,17));Assert.That(text.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")));
                text.rectTransform.sizeDelta=new Vector2(161.7f,37.7568f);Canvas.ForceUpdateCanvases();Assert.That(text.fontSize,Is.EqualTo(18));
                text.rectTransform.sizeDelta=new Vector2(142.68f,37.7568f);Canvas.ForceUpdateCanvases();ReleaseTypography.Apply(text);
                Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(38.2568f));Assert.That(text.GetComponents<HachiFontFit>().Length,Is.EqualTo(1));Assert.That(text.GetComponent<HachiFontFit>().NominalSize,Is.EqualTo(18));
                Assert.That(text.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")));Assert.That(text.rectTransform.sizeDelta,Is.EqualTo(new Vector2(142.68f,37.7568f)));Assert.That(text.rectTransform.anchoredPosition,Is.EqualTo(new Vector2(7,9)));Assert.That(text.color,Is.EqualTo(new Color(.3f,.2f,.25f)));Assert.That(text.lineSpacing,Is.EqualTo(1.28f));
            }
            finally{UnityEngine.Object.DestroyImmediate(node);}
        }
        private static void Verify(string directory,Proof proof)
        {
            if(proof.images.Count!=12||Directory.GetFiles(directory,"*.png").Length!=12)throw new IOException("Twelve actual font-only PNGs required.");
            foreach(var row in proof.images){string path=Path.Combine(directory,row.file);if(!File.Exists(path)||new FileInfo(path).Length==0||Sha(File.ReadAllBytes(path))!=row.sha256)throw new IOException("Font PNG missing/changed.");}
            foreach(string name in new[]{"font-only-native.json","font-only.html"})if(!File.Exists(Path.Combine(directory,name))||new FileInfo(Path.Combine(directory,name)).Length==0)throw new IOException("Font proof/viewer missing.");
        }
        public static IEnumerator Run(UIFlowController flow,string directory,Func<int> writes)
        {
            if(string.IsNullOrWhiteSpace(directory)||!Path.IsPathRooted(directory))throw new IOException("Absolute font-only output required.");
            directory=Path.GetFullPath(directory);
            if(Directory.Exists(directory)&&(Directory.EnumerateFileSystemEntries(directory).Any()||(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0))throw new IOException("Use a fresh font-only directory.");
            Directory.CreateDirectory(directory);
            var proof=new Proof{source_revision=Argument("-mofumachiSourceRevision"),product_fingerprint=Argument("-mofumachiProductFingerprint"),packed_resource_sha256=Argument("-mofumachiPackedResourceSha"),output_directory=directory};
            if(proof.source_revision==null||proof.source_revision.Length!=40||proof.product_fingerprint==null||proof.product_fingerprint.Length!=64||proof.packed_resource_sha256==null||proof.packed_resource_sha256.Length!=64)throw new IOException("Source/resource identity required.");
            Camera camera=null;RenderTexture target=null;Canvas canvas=null;
            try
            {
                camera=new GameObject("Font-only actual UI evidence camera",typeof(Camera)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=UIWidgets.Cream;camera.cullingMask=1<<30;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-20);
                foreach(var size in new[]{new Vector2Int(720,1480),new Vector2Int(640,1072)})
                {
                    if(target!=null){camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}target=new RenderTexture(size.x,size.y,24);target.Create();camera.targetTexture=target;camera.orthographicSize=size.y/4f;camera.aspect=size.x/(float)size.y;
                    foreach(string screen in new[]{"town","conversation","merge","quest","book","settings"})
                    {
                        if(screen=="merge")flow.ShowMerge();else if(screen=="quest")flow.ShowQuest();else if(screen=="settings")flow.ShowSettings();else if(screen=="book"){flow.ShowHome();flow.OpenCharacterBook("momo");}else flow.ShowHome();
                        yield return null;
                        canvas=flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=30;canvas.scaleFactor=2;
                        flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(size.x,size.y),new Rect(0,0,size.x,size.y));
                        foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                        Canvas.ForceUpdateCanvases();foreach(var frame in flow.GetComponentsInChildren<ProductionFrameLayout>())frame.ResizeNow();Canvas.ForceUpdateCanvases();foreach(var grid in flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();
                        foreach(var town in flow.GetComponentsInChildren<TownLifeView>()){town.World.Refresh();town.RefreshWorldPresentation();}
                        if(screen=="conversation")
                        {
                            var town=flow.GetComponentInChildren<TownLifeView>();var actor=town.Actors.First(a=>a.RegionVisible&&!a.Controlled);
                            // Existing bubble at the actor's natural position, using the actual catalog dialogue. No movement/face changes.
                            typeof(TownLifeView).GetMethod("ShowBubble",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(town,new object[]{TownCharacterDialogue.For(actor.CharacterId,1),actor});
                            Assert.That(town.IsSpeaking,Is.True);
                        }
                        Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
                        string state=State(flow),geometry=Geometry(flow);int saves=writes();
                        ReleaseTypography.ApplyTo(flow.transform);Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
                        Assert.That(Geometry(flow),Is.EqualTo(geometry),"Font application must not move/resize UI");
                        var row=new Shot{screen=screen,file=screen+"-"+size.x+"x"+size.y+".png",width=size.x,height=size.y,state_sha256=state,geometry_sha256=geometry};
                        foreach(var text in flow.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrEmpty(t.text)&&ReleaseFontPolicy.For(t.name,t.fontSize,t.text)!=ReleaseFontRole.Unchanged))
                        {
                            AssertTypeface(text);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+.5f),text.name+": existing bounds");
                            foreach(char c in text.text.Where(c=>!char.IsWhiteSpace(c)).Distinct())Assert.That(text.font.HasCharacter(c),Is.True,text.name+": missing glyph");
                            var hachi=Resources.Load<Font>("Mofumachi/UIFontAccent");var compact=text.GetComponent<MergeCompactFontFit>();row.fonts.Add(new FontRow{name=text.name,font=text.font.name,resource=text.font==hachi?"UIFontAccent":text.font==Resources.Load<Font>("Mofumachi/UIFontReadingBold")?"UIFontReadingBold":"UIFontReading",style=text.fontStyle.ToString(),size=text.fontSize,nominal_size=compact!=null?compact.NominalSize:text.GetComponent<HachiFontFit>()!=null?text.GetComponent<HachiFontFit>().NominalSize:text.fontSize,hachi_fits=text.font==hachi,reason=text.font==hachi?"Hachi primary":"Hachi measured overflow; Zen fallback"});
                        }
                        Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
                        foreach(var text in flow.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrEmpty(t.text)&&!t.canvasRenderer.cull&&ReleaseFontPolicy.For(t.name,t.fontSize,t.text)!=ReleaseFontRole.Unchanged))Assert.That(text.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(0),text.name+": actual glyph mesh");
                        var previous=RenderTexture.active;Texture2D image=null;
                        try{camera.Render();RenderTexture.active=target;image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();byte[] bytes=image.EncodeToPNG();File.WriteAllBytes(Path.Combine(directory,row.file),bytes);row.sha256=Sha(bytes);}
                        finally{RenderTexture.active=previous;if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
                        Assert.That(State(flow),Is.EqualTo(state));Assert.That(writes(),Is.EqualTo(saves));proof.images.Add(row);
                    }
                }
                proof.image_count=12;
                var html=new StringBuilder("<!doctype html><meta charset='utf-8'><title>Font-only actual Unity</title><style>body{font:16px sans-serif;background:#fff7ef;color:#523047}img{width:min(90vw,360px);height:auto}figure{display:inline-block;vertical-align:top;margin:8px}</style><h1>フォントのみ：実Unity代表画面</h1><p>Hachi優先／収まらない情報のみZen。A/B/C素材比較ではありません。Visual確認待ち。実機確認ではありません。</p>");
                foreach(var row in proof.images)html.Append("<figure><figcaption>").Append(row.file).Append("</figcaption><a href='").Append(row.file).Append("'><img src='").Append(row.file).Append("'></a></figure>");
                File.WriteAllText(Path.Combine(directory,"font-only.html"),html.ToString());string json=Path.Combine(directory,"font-only-native.json");File.WriteAllText(json,JsonUtility.ToJson(proof,true));Verify(directory,proof);proof.disk_verified=true;File.WriteAllText(json,JsonUtility.ToJson(proof,true));Verify(directory,JsonUtility.FromJson<Proof>(File.ReadAllText(json)));
            }
            finally{if(canvas!=null)canvas.worldCamera=null;if(camera!=null){camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);}if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}}
        }
    }
}
