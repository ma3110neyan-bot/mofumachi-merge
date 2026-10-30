using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Mofumachi.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
namespace Mofumachi.Editor
{
    // Preparation for ONE sequential native gate only. No APK or install here.
    public static class CharacterAuthoredBuild
    {
        public const string ResourceFolder="Assets/Mofumachi/Resources/Mofumachi/CharacterAuthored";
        [Serializable] private sealed class Source{public string file=null,sha256=null;public int width=0,height=0;}
        [Serializable] private sealed class Sources{public Source left=null,right=null,back=null;}
        [Serializable] private sealed class MaskRow{public int min=-1,max=-1;}
        [Serializable] private sealed class Part
        {public string id=null;public int[] crop=null;public float[] pivot=null,offset=null;public float scale=0,rotation=0;public MaskRow[] maskRows=null;}
        [Serializable] private sealed class View{public Part[] parts=null;public Part closedHead=null;}
        [Serializable] private sealed class Views{public View left=null,right=null,back=null;}
        [Serializable] private sealed class Layout{public string characterId=null;public float formalWidth=0,formalHeight=0;public Sources sources=null;public Views views=null;}
        private static string Root=>Directory.GetParent(Application.dataPath).FullName;
        private static string Arg(string name)
        {var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);if(i<0||i+1>=a.Length||a[i+1].StartsWith("-"))throw new BuildFailedException("Missing "+name);return a[i+1];}
        private static string Run()
        {
            var p=Path.GetFullPath(Arg("-mofumachiCharacterDirectory"));var prefix=Path.Combine(Root,"Builds","Diagnostics")+Path.DirectorySeparatorChar;
            if(!Directory.Exists(p)||!p.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new BuildFailedException("Use this run's Diagnostics directory.");return p;
        }
        private static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static void Prepare()
        {
            string run=Run(),id=Arg("-mofumachiCharacterId"),folder=Path.Combine(Root,ResourceFolder);
            if(id!="momo")throw new BuildFailedException("Only Momo is at the current sequential gate. No other character was prepared.");
            if(Directory.Exists(folder)||File.Exists(folder+".meta"))throw new BuildFailedException("Preserve existing character resources; inspect their owner.");
            if(Hash(File.ReadAllBytes(Path.Combine(Root,"Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png")))!="23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d")throw new BuildFailedException("Formal atlas changed.");
            string art=Path.Combine(Root,"References","Characters",id+"-authored-parts-2026-10-30");byte[] raw=File.ReadAllBytes(Path.Combine(art,"layout.json"));
            var layout=JsonUtility.FromJson<Layout>(System.Text.Encoding.UTF8.GetString(raw));if(layout==null||layout.characterId!=id||layout.sources==null||layout.views==null)throw new BuildFailedException("Wrong character registration.");
            var owned=new List<Texture2D>();var chips=new List<Texture2D>();var registrations=new List<CharacterAuthoredPart>();
            var output=new CharacterAuthoredConfig{characterId=id,formalWidth=layout.formalWidth,formalHeight=layout.formalHeight,views=new CharacterAuthoredView[3]};
            var digests=new List<string>{Hash(raw)};
            try
            {
                int viewIndex=0;
                foreach(var pair in new[]{("left",layout.sources.left,layout.views.left),("right",layout.sources.right,layout.views.right),("back",layout.sources.back,layout.views.back)})
                {
                    if(pair.Item2==null||pair.Item3?.parts==null||pair.Item3.parts.Length!=7)throw new BuildFailedException("Missing direction/parts.");
                    string path=Path.GetFullPath(Path.Combine(art,pair.Item2.file));if(!path.StartsWith(art+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new BuildFailedException("Source escaped this character.");
                    byte[] bytes=File.ReadAllBytes(path);string digest=Hash(bytes);if(digest!=pair.Item2.sha256)throw new BuildFailedException("Changed part source: "+pair.Item1);digests.Add(pair.Item1+":"+digest);
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);owned.Add(source);if(!source.LoadImage(bytes,false)||source.width!=pair.Item2.width||source.height!=pair.Item2.height)throw new BuildFailedException("Invalid source PNG.");
                    CharacterAuthoredPart Register(Part p)
                    {
                        if(p==null||p.crop==null||p.crop.Length!=4||p.pivot==null||p.pivot.Length!=2||p.offset==null||p.offset.Length!=2||!float.IsFinite(p.scale)||p.scale<=0)throw new BuildFailedException("Invalid registration.");
                        default(NoahAuthoredPose).Part(p.id);int x=p.crop[0],y=p.crop[1],w=p.crop[2],h=p.crop[3];
                        if(x<0||y<0||w<1||h<1||x+w>source.width||y+h>source.height)throw new BuildFailedException("Crop outside source.");
                        var pixels=source.GetPixels(x,source.height-y-h,w,h);
                        // Registration gutters isolate the two neighboring rear
                        // parts. Raw images/colors are never repainted or saved over.
                        if(p.maskRows!=null)
                        {
                            if(p.maskRows.Length!=h)throw new BuildFailedException("Invalid gutter rows.");
                            for(int row=0;row<h;row++)
                            {
                                var span=p.maskRows[h-1-row];if(span==null||span.min < -1||span.max>=w||span.min>span.max)throw new BuildFailedException("Invalid gutter span.");
                                for(int col=0;col<w;col++)if(span.min<0||col<span.min||col>span.max)pixels[row*w+col]=Color.clear;
                            }
                        }
                        var chip=new Texture2D(w,h,TextureFormat.RGBA32,false);owned.Add(chip);chip.SetPixels(pixels);chip.Apply();chips.Add(chip);
                        var registered=new CharacterAuthoredPart{id=p.id,width=w,height=h,pivot=new Vector2(p.pivot[0],p.pivot[1]),offset=new Vector2(p.offset[0],p.offset[1]),scale=p.scale,rotation=p.rotation};registrations.Add(registered);return registered;
                    }
                    var view=new CharacterAuthoredView{direction=pair.Item1,parts=new CharacterAuthoredPart[7]};output.views[viewIndex++]=view;
                    for(int i=0;i<7;i++)view.parts[i]=Register(pair.Item3.parts[i]);
                    if(pair.Item1!="back")view.closedHead=Register(pair.Item3.closedHead);
                }
                output.input_sha256=Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",digests)));
                var atlas=new Texture2D(2,2,TextureFormat.RGBA32,false);owned.Add(atlas);var uv=atlas.PackTextures(chips.ToArray(),4,2048,false);
                if(uv.Length!=23||atlas.width>2048||atlas.height>2048)throw new BuildFailedException("Unexpected packed parts.");
                for(int i=0;i<uv.Length;i++)registrations[i].uv=uv[i];
                Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,".owner"),run);string character=Path.Combine(folder,id);Directory.CreateDirectory(character);
                File.WriteAllBytes(Path.Combine(character,"atlas.png"),atlas.EncodeToPNG());File.WriteAllText(Path.Combine(character,"layout.json"),JsonUtility.ToJson(output,true));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(ResourceFolder+"/"+id+"/atlas.png");importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ETC2_RGBA8});importer.SaveAndReimport();
                File.WriteAllText(Path.Combine(run,"prepared.json"),JsonUtility.ToJson(output,true));
            }
            finally{foreach(var texture in owned)UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
