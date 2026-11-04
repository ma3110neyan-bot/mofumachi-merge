using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public static class MergeFontComparisonCapture
    {
#if UNITY_EDITOR
        [Serializable] private sealed class Image
        {public string file,sha256,variant,font,state_sha256,layout_sha256;public int width,height;public bool bold;public float outline;}
        [Serializable] private sealed class Proof
        {
            public string kind="merge-font-abc-native",source_revision,product_fingerprint,packed_resource_sha256,output_directory;
            public int image_count;public bool native_unity=true,disk_verified,formal_fixed=false,adopted=false,pixel_verified=false;
            public List<Image> images=new List<Image>();
        }
        private static string Hash(byte[] bytes)=>MergeCaptureArtifacts.Hash(bytes);
        private static string State(UIFlowController flow)=>Hash(Encoding.UTF8.GetBytes(StateCodec.Encode(flow.Game.State)));
        private static string Layout(UIFlowController flow)
        {
            var text=new StringBuilder();
            foreach(var r in flow.GetComponentsInChildren<RectTransform>())
                text.Append(r.name).Append('|').Append(r.rect.ToString("F3")).Append('|').Append(r.anchoredPosition.ToString("F3")).Append('\n');
            return Hash(Encoding.UTF8.GetBytes(text.ToString()));
        }
        private static void Verify(string directory,Proof proof)
        {
            if(proof.images.Count!=6||Directory.GetFiles(directory,"*.jpg").Length!=6)throw new IOException("Six actual font comparison JPEGs required.");
            foreach(var row in proof.images)
            {
                var file=Path.Combine(directory,row.file);
                if(!File.Exists(file)||new FileInfo(file).Length==0||Hash(File.ReadAllBytes(file))!=row.sha256)throw new IOException("Font comparison missing/changed: "+row.file);
            }
            foreach(string name in new[]{"merge-font-native.json","merge-font.html"})
                if(!File.Exists(Path.Combine(directory,name))||new FileInfo(Path.Combine(directory,name)).Length==0)throw new IOException("Font comparison proof/viewer missing.");
        }
#endif
        public static void Write(UIFlowController flow,string directory,string revision,string fingerprint,string packed,Func<int,int,Texture2D> render,Func<int> writes)
        {
#if UNITY_EDITOR
            if(string.IsNullOrWhiteSpace(directory)||!Path.IsPathRooted(directory))throw new IOException("Absolute font comparison directory required.");
            directory=Path.GetFullPath(directory);
            if(Directory.Exists(directory)&&(Directory.EnumerateFileSystemEntries(directory).Any()||(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0))throw new IOException("Preserve old font comparisons; use a fresh folder.");
            Directory.CreateDirectory(directory);
            var proof=new Proof{source_revision=revision,product_fingerprint=fingerprint,packed_resource_sha256=packed,output_directory=directory};
            var previous=MergeTypography.PreviewVariant;bool previousBaseline=MergeRewardPresentation.CapturePinkBaseline;
            try
            {
                MergeRewardPresentation.CapturePinkBaseline=false;
                foreach(var size in new[]{new Vector2Int(720,1480),new Vector2Int(640,1072)})
                {
                    string sameLayout=null;
                    foreach(MergeFontVariant variant in Enum.GetValues(typeof(MergeFontVariant)))
                    {
                        MergeTypography.PreviewVariant=variant;string state=State(flow);int count=writes();
                        var texture=render(size.x,size.y);
                        try
                        {
                            var title=flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Page title");
                            Assert.That(title.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/"+MergeFontProfile.Font(variant))));
                            Assert.That(title.fontStyle,Is.EqualTo(MergeFontProfile.Bold(variant)?FontStyle.Bold:FontStyle.Normal));
                            Assert.That(title.preferredHeight,Is.LessThanOrEqualTo(title.rectTransform.rect.height+1));
                            var menus=flow.GetComponentsInChildren<Text>().Where(t=>MergeFontProfile.IsMenu(t.name)&&t.text.Length>0).ToArray();
                            Assert.That(menus.Length,Is.GreaterThanOrEqualTo(9));
                            foreach(var menu in menus)
                            {
                                Assert.That(menu.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontAccent")),menu.name);
                                Assert.That(menu.fontStyle,Is.EqualTo(FontStyle.Bold));
                                Assert.That(menu.preferredHeight,Is.LessThanOrEqualTo(menu.rectTransform.rect.height+1),menu.name);
                            }
                            foreach(var level in flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Item level"))
                            {
                                Assert.That(level.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontReadingBold")));
                                Assert.That(level.fontSize,Is.GreaterThanOrEqualTo(8));
                                Assert.That(level.preferredHeight,Is.LessThanOrEqualTo(level.rectTransform.rect.height+.5f));
                                Assert.That(level.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(0),"Lv glyphs must actually render");
                            }
                            Assert.That(flow.GetComponentsInChildren<Transform>().Any(t=>t.name=="Current reward band"||t.name=="Current reward coin"),Is.False,"No meaningless band/coin in After");
                            string layout=Layout(flow);if(sameLayout==null)sameLayout=layout;else Assert.That(layout,Is.EqualTo(sameLayout),"Only typography may differ");
                            Assert.That(State(flow),Is.EqualTo(state));Assert.That(writes(),Is.EqualTo(count));
                            var row=new Image{file=variant+"-"+size.x+"x"+size.y+".jpg",variant=variant.ToString(),font=MergeFontProfile.Font(variant),bold=MergeFontProfile.Bold(variant),outline=MergeFontProfile.Outline(variant),width=size.x,height=size.y,state_sha256=state,layout_sha256=layout};
                            byte[] bytes=texture.EncodeToJPG(95);File.WriteAllBytes(Path.Combine(directory,row.file),bytes);row.sha256=Hash(bytes);
                            Assert.That(File.ReadAllBytes(Path.Combine(directory,row.file)),Is.EqualTo(bytes));proof.images.Add(row);
                        }
                        finally{UnityEngine.Object.DestroyImmediate(texture);}
                    }
                }
                proof.image_count=6;
                var html=new StringBuilder("<!doctype html><meta charset='utf-8'><title>Merge font A/B/C</title><style>body{font:16px sans-serif;background:#fff7ef;color:#5c3535}section{display:flex;gap:12px}figure{margin:4px}img{width:min(30vw,360px);height:auto}</style><h1>Mergeフォント A/B/C：実Unity</h1><p>A＝Hachi軽い太字、B＝A＋薄い濃ピンク輪郭、C＝Kiwi Maru Medium。画像クリックで原寸。レイアウト・配色・盤面・保存状態は共通。Visual確認待ち。APK／Pixel確認ではありません。</p>");
                foreach(int height in new[]{1480,1072})
                {html.Append("<h2>").Append(height==1480?"720×1480":"640×1072").Append("</h2><section>");foreach(var row in proof.images.Where(i=>i.height==height))html.Append("<figure><figcaption>").Append(row.variant).Append("</figcaption><a href='").Append(row.file).Append("'><img src='").Append(row.file).Append("'></a></figure>");html.Append("</section>");}
                File.WriteAllText(Path.Combine(directory,"merge-font.html"),html.ToString());
                string json=Path.Combine(directory,"merge-font-native.json");File.WriteAllText(json,JsonUtility.ToJson(proof,true));Verify(directory,proof);
                proof.disk_verified=true;File.WriteAllText(json,JsonUtility.ToJson(proof,true));Verify(directory,JsonUtility.FromJson<Proof>(File.ReadAllText(json)));
            }
            finally{MergeTypography.PreviewVariant=previous;MergeRewardPresentation.CapturePinkBaseline=previousBaseline;}
#else
            throw new NotSupportedException("Font comparison capture requires Unity Editor.");
#endif
        }
    }
}
