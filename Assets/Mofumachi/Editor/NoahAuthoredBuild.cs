using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using Mofumachi.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Mofumachi.Editor
{
    // Explicit experiment; never opens the retired rig or normal full-QA gates.
    public static class NoahAuthoredBuild
    {
        public const string ResourceFolder="Assets/Mofumachi/Resources/Mofumachi/NoahAuthoredTrial";
        public const string TestMethod="Mofumachi.Tests.ExperienceCaptureTests.NoahAuthoredPartsPreviewPreservesState";
        [Serializable] private sealed class Source{public string file=null,sha256=null;}
        [Serializable] private sealed class Sources{public Source parts=null,torso=null,right=null,back=null;}
        [Serializable] private sealed class Clip{public int min=0,max=0;}
        [Serializable] private sealed class Part{public string id=null,source=null;public int[] crop=null;public float[] pivot=null;public float scale=0;public Clip clipSum=null;}
        [Serializable] private sealed class View{public Part[] parts=null;}
        [Serializable] private sealed class Views{public View left=null,right=null,back=null;}
        [Serializable] private sealed class Layout{public Sources sources=null;public Views views=null;}
        [Serializable] private sealed class Context{public string gitRevision=null,productFingerprint=null;}
        [Serializable] private sealed class Proof{public string gitRevision=null,productFingerprint=null,input_sha256=null,packed_resource_sha256=null;public bool native_unity=false;public int directional_frames=0;}
        [Serializable] private sealed class BuildInfo
        {
            public string scope="experimental-noah-authored-not-release",mode,package=AndroidBuild.TownQaApplicationId,gitRevision,productFingerprint,input_sha256,packed_resource_sha256,sha256,apk;
            public bool native_capture_method_verified=true,full_qa_verified=false,pixel_verified=false,adopted=false;
        }
        private static string Root=>Directory.GetParent(Application.dataPath).FullName;
        private static string Arg(string name)
        {var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);if(i<0||i+1>=a.Length||a[i+1].StartsWith("-"))throw new BuildFailedException("Missing "+name);return a[i+1];}
        private static string RunDirectory()
        {
            string p=Path.GetFullPath(Arg("-mofumachiNoahAuthoredDirectory")),allowed=Path.Combine(Root,"Builds","Diagnostics")+Path.DirectorySeparatorChar;
            if(!p.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||!Directory.Exists(p))throw new BuildFailedException("Use a fresh Diagnostics run directory.");return p;
        }
        private static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
        private static string ResourceDigest()
        {
            string folder=Path.GetFullPath(Path.Combine(Root,ResourceFolder));
            var rows=Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(p=>Path.GetFileName(p)!=".owner").Select(p=>p.Substring(folder.Length+1).Replace('\\','/')+":"+Hash(File.ReadAllBytes(p))).OrderBy(s=>s,StringComparer.Ordinal);
            return Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",rows)));
        }
        public static void Prepare()
        {
            string run=RunDirectory(),folder=Path.Combine(Root,ResourceFolder),art=Path.Combine(Root,"References/Characters/noah-directional-parts-2026-10-30");
            if(Directory.Exists(folder)||File.Exists(folder+".meta"))throw new BuildFailedException("Preserve existing authored trial resources; cleanup the owning run first.");
            if(Hash(File.ReadAllBytes(Path.Combine(Root,"Assets/Mofumachi/Resources/Mofumachi/CharacterBodyAtlas.png")))!="23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d")throw new BuildFailedException("Formal atlas changed.");
            byte[] layoutBytes=File.ReadAllBytes(Path.Combine(art,"directions-layout.json"));var layout=JsonUtility.FromJson<Layout>(System.Text.Encoding.UTF8.GetString(layoutBytes));
            var sourceFiles=new Dictionary<string,Source>{{"parts",layout.sources.parts},{"torso",layout.sources.torso},{"right",layout.sources.right},{"back",layout.sources.back}};
            var textures=new Dictionary<string,Texture2D>();var owned=new List<Texture2D>();var chips=new List<Texture2D>();var registered=new List<NoahAuthoredPart>();
            var output=new NoahAuthoredRigConfig{views=new NoahAuthoredView[3]};var sourceDigests=new List<string>{Hash(layoutBytes)};
            try
            {
                foreach(var entry in sourceFiles)
                {
                    string path=Path.GetFullPath(Path.Combine(art,entry.Value.file));
                    if(!path.StartsWith(Path.GetFullPath(Path.Combine(Root,"References","Characters"))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new BuildFailedException("Art input escaped Characters.");
                    byte[] bytes=File.ReadAllBytes(path);string digest=Hash(bytes);if(digest!=entry.Value.sha256)throw new BuildFailedException("Noah source hash mismatch: "+entry.Key);
                    sourceDigests.Add(entry.Key+":"+digest);var t=new Texture2D(2,2,TextureFormat.RGBA32,false);owned.Add(t);
                    if(!t.LoadImage(bytes,false))throw new BuildFailedException("Cannot decode Noah art.");textures.Add(entry.Key,t);
                }
                output.input_sha256=Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",sourceDigests)));
                int vi=0;
                foreach(var pair in new[]{("left",layout.views.left),("right",layout.views.right),("back",layout.views.back)})
                {
                    if(pair.Item2.parts.Length!=8)throw new BuildFailedException("Eight parts required for "+pair.Item1);
                    var view=new NoahAuthoredView{direction=pair.Item1,parts=new NoahAuthoredPart[8]};output.views[vi++]=view;
                    for(int i=0;i<8;i++)
                    {
                        var p=pair.Item2.parts[i];default(NoahAuthoredPose).Part(p.id);var source=textures[p.source];
                        int x=p.crop[0],y=p.crop[1],w=p.crop[2],h=p.crop[3];
                        if(x<0||y<0||w<1||h<1||x+w>source.width||y+h>source.height)throw new BuildFailedException("Part crop outside source.");
                        var pixels=source.GetPixels(x,source.height-y-h,w,h);
                        if(p.clipSum!=null)for(int row=0;row<h;row++)for(int col=0;col<w;col++)
                        {int sum=x+col+y+h-1-row;if(p.clipSum.min>0&&sum<p.clipSum.min||p.clipSum.max>0&&sum>p.clipSum.max)pixels[row*w+col]=Color.clear;}
                        var chip=new Texture2D(w,h,TextureFormat.RGBA32,false);owned.Add(chip);chip.SetPixels(pixels);chip.Apply();chips.Add(chip);
                        var part=new NoahAuthoredPart{id=p.id,width=w,height=h,pivot=new Vector2(p.pivot[0],p.pivot[1]),scale=p.scale};view.parts[i]=part;registered.Add(part);
                    }
                }
                var atlas=new Texture2D(2,2,TextureFormat.RGBA32,false);owned.Add(atlas);var uv=atlas.PackTextures(chips.ToArray(),4,2048,false);
                if(uv.Length!=24||atlas.width>2048||atlas.height>2048)throw new BuildFailedException("Invalid packed Noah atlas.");
                for(int i=0;i<uv.Length;i++)registered[i].uv=uv[i];
                Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,".owner"),run);
                File.WriteAllBytes(Path.Combine(folder,"atlas.png"),atlas.EncodeToPNG());File.WriteAllText(Path.Combine(folder,"layout.json"),JsonUtility.ToJson(output,true));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(ResourceFolder+"/atlas.png");
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ETC2_RGBA8});importer.SaveAndReimport();
                File.WriteAllText(Path.Combine(run,"prepared.json"),JsonUtility.ToJson(output,true));
            }
            finally{foreach(var t in owned)UnityEngine.Object.DestroyImmediate(t);}
        }
        public static void Cleanup()
        {
            string folder=Path.Combine(Root,ResourceFolder),run=RunDirectory();if(!Directory.Exists(folder))return;
            string marker=Path.Combine(folder,".owner");if(!File.Exists(marker)||File.ReadAllText(marker)!=run)throw new BuildFailedException("Refuse to delete resources not owned by this run.");
            if(!AssetDatabase.DeleteAsset(ResourceFolder))throw new BuildFailedException("Cannot cleanup owned Noah resources.");
        }
        public static void BuildPair()
        {
            string run=RunDirectory();
            if(Application.unityVersion!="6000.6.4f1"||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android||!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Pinned Unity/Android Build Support required.");
            var context=JsonUtility.FromJson<Context>(File.ReadAllText(Path.Combine(run,"context.json")));
            if(context==null||!System.Text.RegularExpressions.Regex.IsMatch(context.gitRevision??"","^[a-f0-9]{40}$")||!System.Text.RegularExpressions.Regex.IsMatch(context.productFingerprint??"","^[a-f0-9]{64}$"))throw new BuildFailedException("Invalid source identity.");
            string symbols=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            if(symbols.Split(';').Any(s=>s=="MOFUMACHI_NOAH_AUTHORED"||s=="MOFUMACHI_NOAH_FINAL_RIG"))throw new BuildFailedException("Trial defines must not be stored in normal PlayerSettings.");
            var proof=JsonUtility.FromJson<Proof>(File.ReadAllText(Path.Combine(run,"screenshots/noah-authored/proof.json")));
            var config=JsonUtility.FromJson<NoahAuthoredRigConfig>(File.ReadAllText(Path.Combine(Root,ResourceFolder,"layout.json")));
            var xml=new XmlDocument();xml.Load(Path.Combine(run,"playmode.xml"));var r=xml.DocumentElement;var cases=xml.SelectNodes("//test-case");
            if(r==null||r.GetAttribute("result")!="Passed"||r.GetAttribute("total")!="1"||r.GetAttribute("passed")!="1"||r.GetAttribute("failed")!="0"||r.GetAttribute("skipped")!="0"||r.GetAttribute("inconclusive")!="0"||cases.Count!=1||cases[0].Attributes["fullname"]?.Value!=TestMethod||cases[0].Attributes["result"]?.Value!="Passed")throw new BuildFailedException("Exactly the new single passed Noah method required.");
            string packed=ResourceDigest();
            if(!proof.native_unity||proof.directional_frames<6||proof.gitRevision!=context.gitRevision||proof.productFingerprint!=context.productFingerprint||proof.input_sha256!=config.input_sha256||proof.packed_resource_sha256!=packed)throw new BuildFailedException("Native/source/art identity mismatch.");
            AndroidBuild.ValidateContent();
            AndroidBuild.WithTownQaIdentity(()=>{Build(run,"current",context,config,packed,false);Build(run,"authored",context,config,packed,true);});
        }
        private static void Build(string run,string mode,Context context,NoahAuthoredRigConfig config,string packed,bool candidate)
        {
            string apk=Path.Combine(run,"noah-"+mode+".apk"),identity=Path.Combine(run,"noah-"+mode+"-build.json"),stamp="Assets/Mofumachi/Resources/Mofumachi/QaBuildRevision.txt";
            if(File.Exists(apk)||File.Exists(identity)||File.Exists(stamp))throw new BuildFailedException("Preserve existing build outputs/stamp.");
            string temporary=Path.Combine(run,"building-"+Guid.NewGuid().ToString("N")+".apk");bool wrote=false;
            try
            {
                File.WriteAllText(stamp,context.gitRevision);wrote=true;AssetDatabase.ImportAsset(stamp,ImportAssetOptions.ForceSynchronousImport);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=AndroidBuild.Scenes,locationPathName=temporary,target=BuildTarget.Android,options=BuildOptions.Development,extraScriptingDefines=candidate?new[]{"MOFUMACHI_NOAH_AUTHORED"}:Array.Empty<string>()});
                if(report.summary.result!=BuildResult.Succeeded||!File.Exists(temporary)||new FileInfo(temporary).Length==0)throw new BuildFailedException("Noah "+mode+" APK failed.");
                if(ResourceDigest()!=packed)throw new BuildFailedException("Packed art/import settings changed after native check.");
                File.Move(temporary,apk);File.WriteAllText(identity,JsonUtility.ToJson(new BuildInfo{mode=mode,gitRevision=context.gitRevision,productFingerprint=context.productFingerprint,input_sha256=config.input_sha256,packed_resource_sha256=packed,sha256=Hash(File.ReadAllBytes(apk)),apk=Path.GetFileName(apk)},true));
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);if(wrote)AssetDatabase.DeleteAsset(stamp);}
        }
    }
}
