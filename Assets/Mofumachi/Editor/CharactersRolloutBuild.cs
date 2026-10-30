using System;
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
    public static class CharactersRolloutBuild
    {
        [Serializable] private sealed class Context{public string gitRevision=null,productFingerprint=null;public string[] singleRuns=null;}
        [Serializable] public sealed class Input{public string characterId,input_sha256;}
        [Serializable] public sealed class Seal{public string gitRevision,productFingerprint,packed_resource_sha256;public Input[] inputs;}
        [Serializable] private sealed class Proof{public string character=null,gitRevision=null,productFingerprint=null,input_sha256=null,packed_resource_sha256=null;public bool native_unity=false,all_six_enabled=false,save_unchanged=false,tap_verified=false,conversation_verified=false,greeting_authored=false,talk_authored=false,blink_authored=false;}
        [Serializable] private sealed class BuildInfo{public string mode,gitRevision,productFingerprint,packed_resource_sha256,sha256,apk,package;public bool full_qa_verified=true,six_native_verified=true,pixel_verified=false,adopted=false;}
        private static readonly string[] Ids={"momo","lulu","pom","fio","miel","noah"};
        private static string Root=>Directory.GetParent(Application.dataPath).FullName;
        private static string Run()
        {
            var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-mofumachiCharactersDirectory");
            if(i<0||i+1>=a.Length)throw new BuildFailedException("Exact characters run required.");
            string p=Path.GetFullPath(a[i+1]);if(!p.StartsWith(Path.Combine(Root,"Builds","Diagnostics")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!Directory.Exists(p))throw new BuildFailedException("Invalid run directory.");return p;
        }
        private static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","").ToLowerInvariant();}
        private static string Packed(string run)
        {
            var rows=new System.Collections.Generic.List<string>();
            foreach(string relative in new[]{CharacterAuthoredBuild.ResourceFolder,NoahAuthoredBuild.ResourceFolder})
            {
                string folder=Path.Combine(Root,relative);
                if(!File.Exists(Path.Combine(folder,".owner"))||File.ReadAllText(Path.Combine(folder,".owner"))!=run)throw new BuildFailedException("Packed resources have another owner.");
                foreach(string path in Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(p=>Path.GetFileName(p)!=".owner"))rows.Add(path.Substring(Root.Length+1).Replace('\\','/')+":"+Hash(File.ReadAllBytes(path)));
            }
            return Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",rows.OrderBy(s=>s,StringComparer.Ordinal))));
        }
        private static Context ReadContext(string run)
        {
            var c=JsonUtility.FromJson<Context>(File.ReadAllText(Path.Combine(run,"context.json")));
            if(c==null||!System.Text.RegularExpressions.Regex.IsMatch(c.gitRevision??"","^[a-f0-9]{40}$")||!System.Text.RegularExpressions.Regex.IsMatch(c.productFingerprint??"","^[a-f0-9]{64}$")||c.singleRuns?.Length!=5)throw new BuildFailedException("Incomplete source and five single gates.");return c;
        }
        public static void SealPrepared()
        {
            string run=Run();var c=ReadContext(run);var seal=new Seal{gitRevision=c.gitRevision,productFingerprint=c.productFingerprint,packed_resource_sha256=Packed(run),inputs=new Input[6]};
            for(int i=0;i<Ids.Length;i++)
            {
                string path=Ids[i]=="noah"?Path.Combine(Root,NoahAuthoredBuild.ResourceFolder,"layout.json"):Path.Combine(Root,CharacterAuthoredBuild.ResourceFolder,Ids[i],"layout.json");
                var input=JsonUtility.FromJson<Input>(File.ReadAllText(path));input.characterId=Ids[i];if(!System.Text.RegularExpressions.Regex.IsMatch(input.input_sha256??"","^[a-f0-9]{64}$"))throw new BuildFailedException("Invalid packed input.");seal.inputs[i]=input;
            }
            File.WriteAllText(Path.Combine(run,"prepared-six.json"),JsonUtility.ToJson(seal,true));
        }
        private static void Passed(string path,int minimum,string method=null)
        {
            var xml=new XmlDocument();xml.Load(path);var root=xml.DocumentElement;var cases=xml.SelectNodes("//test-case");
            if(root?.GetAttribute("result")!="Passed"||cases.Count<minimum||cases.Cast<XmlNode>().Any(n=>n.Attributes["result"]?.Value!="Passed")||(method!=null&&(cases.Count!=1||cases[0].Attributes["fullname"]?.Value!=method)))throw new BuildFailedException("Required native tests did not pass: "+path);
        }
        public static void BuildPair()
        {
            string run=Run();var c=ReadContext(run);var seal=JsonUtility.FromJson<Seal>(File.ReadAllText(Path.Combine(run,"prepared-six.json")));
            if(Application.unityVersion!="6000.6.4f1"||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android||!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Pinned Unity/Android support required.");
            if(seal.gitRevision!=c.gitRevision||seal.productFingerprint!=c.productFingerprint||seal.packed_resource_sha256!=Packed(run))throw new BuildFailedException("Prepared/source identity changed.");
            Passed(Path.Combine(run,"full-editmode.xml"),144);Passed(Path.Combine(run,"full-playmode.xml"),240);
            for(int i=0;i<5;i++)
            {
                string id=Ids[i],single=Path.GetFullPath(c.singleRuns[i]);
                if(!single.StartsWith(Path.Combine(Root,"Builds","Diagnostics")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new BuildFailedException("Single gate outside diagnostics.");
                Passed(Path.Combine(single,"playmode.xml"),1,"Mofumachi.Tests.ExperienceCaptureTests."+char.ToUpperInvariant(id[0])+id.Substring(1)+"AuthoredPartsPreserveIdentityInputAndState");
                var p=JsonUtility.FromJson<Proof>(File.ReadAllText(Path.Combine(single,"screenshots",id+"-authored","proof.json")));
                if(p.character!=id||p.gitRevision!=c.gitRevision||p.productFingerprint!=c.productFingerprint||p.input_sha256!=seal.inputs[i].input_sha256||!p.native_unity||!p.save_unchanged||!p.tap_verified||!p.conversation_verified||!p.greeting_authored||!p.talk_authored||!p.blink_authored)throw new BuildFailedException("Invalid single-character proof: "+id);
            }
            Passed(Path.Combine(run,"six-playmode.xml"),1,"Mofumachi.Tests.ExperienceCaptureTests.SixAuthoredCharactersPreserveWorldAndInput");
            var six=JsonUtility.FromJson<Proof>(File.ReadAllText(Path.Combine(run,"screenshots","six-authored","six-proof.json")));
            if(!six.native_unity||!six.all_six_enabled||!six.save_unchanged||!six.tap_verified||six.gitRevision!=c.gitRevision||six.productFingerprint!=c.productFingerprint||six.packed_resource_sha256!=seal.packed_resource_sha256)throw new BuildFailedException("Six-character native/source proof missing.");
            string symbols=PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android);
            if(symbols.Split(';').Any(s=>s=="MOFUMACHI_NOAH_AUTHORED"||s=="MOFUMACHI_CHARACTERS_AUTHORED"))throw new BuildFailedException("Do not persist candidate build defines.");
            AndroidBuild.ValidateContent();AndroidBuild.WithTownQaIdentity(()=>{Build(run,"baseline",c,seal,false);Build(run,"authored",c,seal,true);});
        }
        private static void Build(string run,string mode,Context c,Seal seal,bool authored)
        {
            string apk=Path.Combine(run,"characters-"+mode+".apk"),identity=Path.Combine(run,"characters-"+mode+"-build.json"),stamp="Assets/Mofumachi/Resources/Mofumachi/QaBuildRevision.txt";
            if(File.Exists(apk)||File.Exists(identity)||File.Exists(stamp))throw new BuildFailedException("Preserve existing outputs/stamp.");
            string temporary=Path.Combine(run,"building-"+Guid.NewGuid().ToString("N")+".apk");bool wrote=false;
            try
            {
                File.WriteAllText(stamp,c.gitRevision);wrote=true;AssetDatabase.ImportAsset(stamp,ImportAssetOptions.ForceSynchronousImport);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=AndroidBuild.Scenes,locationPathName=temporary,target=BuildTarget.Android,options=BuildOptions.Development,extraScriptingDefines=authored?new[]{"MOFUMACHI_NOAH_AUTHORED","MOFUMACHI_CHARACTERS_AUTHORED"}:new[]{"MOFUMACHI_NOAH_AUTHORED"}});
                if(report.summary.result!=BuildResult.Succeeded||!File.Exists(temporary)||new FileInfo(temporary).Length==0||Packed(run)!=seal.packed_resource_sha256)throw new BuildFailedException("APK build or resource identity failed.");
                File.Move(temporary,apk);File.WriteAllText(identity,JsonUtility.ToJson(new BuildInfo{mode=mode,gitRevision=c.gitRevision,productFingerprint=c.productFingerprint,packed_resource_sha256=seal.packed_resource_sha256,sha256=Hash(File.ReadAllBytes(apk)),apk=Path.GetFileName(apk),package=AndroidBuild.TownQaApplicationId},true));
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);if(wrote)AssetDatabase.DeleteAsset(stamp);}
        }
    }
}
