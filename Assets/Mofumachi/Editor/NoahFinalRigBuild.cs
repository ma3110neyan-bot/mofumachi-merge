using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mofumachi.Editor
{
    // Explicit experimental route. The normal AndroidBuild and its full-QA
    // gate are unchanged. This cannot declare a release or six-person pass.
    public static class NoahFinalRigBuild
    {
        [Serializable] private sealed class SourceInfo
        {public string gitRevision=null,productFingerprint=null;public bool native_test_passed=false,source_unchanged=false,adopted=false,pixel_verified=false;}
        [Serializable] private sealed class ProofHeader
        {public int noah_reprototype_version=0;public string source_revision=null,product_fingerprint=null,baseline_source_revision=null,kind=null;public bool native_unity=false,current_before_branch_verified=false,adopted=false,pixel_verified=false;}
        [Serializable] private sealed class BuildInfo
        {
            public string scope="experimental-noah-only-not-release",mode,package=AndroidBuild.TownQaApplicationId,gitRevision,productFingerprint,sha256,apk;
            public bool native_capture_method_verified=true,full_qa_verified=false,pixel_verified=false,adopted=false;
        }
        private static string Arg(string key)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);
            if(i<0||i+1>=args.Length||args[i+1].StartsWith("-",StringComparison.Ordinal))throw new BuildFailedException("Missing "+key);
            return args[i+1];
        }
        public static void BuildPair()
        {
            if(Application.unityVersion!="6000.6.4f1"||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android||!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Use pinned Unity with Android Build Support and active Android profile.");
            string revision=AndroidBuild.QaRevision(Environment.GetCommandLineArgs()),fingerprint=Arg("-mofumachiProductFingerprint"),directory=Path.GetFullPath(Arg("-mofumachiNoahTrialDirectory"));
            if(!System.Text.RegularExpressions.Regex.IsMatch(fingerprint,"^[a-f0-9]{64}$"))throw new BuildFailedException("Invalid source fingerprint.");
            var source=JsonUtility.FromJson<SourceInfo>(File.ReadAllText(Path.Combine(directory,"source-info.json")));
            var proof=JsonUtility.FromJson<ProofHeader>(File.ReadAllText(Path.Combine(directory,"screenshots/noah-reprototype/noah-reprototype.json")));
            var xml=new XmlDocument();xml.Load(Path.Combine(directory,"playmode.xml"));var run=xml.DocumentElement;var cases=xml.SelectNodes("//test-case");
            if(source.gitRevision!=revision||source.productFingerprint!=fingerprint||!source.native_test_passed||!source.source_unchanged||source.adopted||source.pixel_verified||proof.source_revision!=revision||proof.product_fingerprint!=fingerprint||proof.noah_reprototype_version!=3||proof.kind!="noah-final-rig-comparison"||proof.baseline_source_revision!="f45106840405e0f2ff27296efa12d6c1255c1db9"||!proof.native_unity||!proof.current_before_branch_verified||proof.adopted||proof.pixel_verified)throw new BuildFailedException("Missing same-source unadopted final-rig proof.");
            if(run==null||run.Name!="test-run"||run.GetAttribute("result")!="Passed"||run.GetAttribute("total")!="1"||run.GetAttribute("passed")!="1"||run.GetAttribute("failed")!="0"||run.GetAttribute("skipped")!="0"||run.GetAttribute("inconclusive")!="0"||cases.Count!=1||cases[0].Attributes["result"]?.Value!="Passed"||cases[0].Attributes["fullname"]?.Value!="Mofumachi.Tests.ExperienceCaptureTests.NoahReprototypeContinuousSequencePreservesArtAndSavedState")throw new BuildFailedException("Exactly the single passed Noah method is required.");
            string previousId=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),previousName=PlayerSettings.productName;
            try
            {
                AndroidBuild.ConfigurePixel3a();AndroidBuild.ValidateContent();
                AndroidBuild.WithTownQaIdentity(()=>{Build(directory,"current",revision,fingerprint,false);Build(directory,"rig",revision,fingerprint,true);});
            }
            finally{PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,previousId);PlayerSettings.productName=previousName;AssetDatabase.SaveAssets();}
        }
        private static void Build(string directory,string mode,string revision,string fingerprint,bool rig)
        {
            string output=Path.Combine(directory,"noah-"+mode+".apk"),infoPath=Path.Combine(directory,"noah-"+mode+"-build.json");
            if(File.Exists(output)||File.Exists(infoPath))throw new BuildFailedException("Preserve existing trial APK; choose a fresh run.");
            string temporary=Path.Combine(directory,"noah-"+Guid.NewGuid().ToString("N")+".apk"),stamp=Path.Combine(Application.dataPath,"Mofumachi/Resources/Mofumachi/QaBuildRevision.txt");bool wrote=false;
            try
            {
                if(File.Exists(stamp))throw new BuildFailedException("Preserve existing QA revision stamp.");
                File.WriteAllText(stamp,revision,new System.Text.UTF8Encoding(false));wrote=true;AssetDatabase.ImportAsset("Assets/Mofumachi/Resources/Mofumachi/QaBuildRevision.txt",ImportAssetOptions.ForceSynchronousImport);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=AndroidBuild.Scenes,locationPathName=temporary,target=BuildTarget.Android,options=BuildOptions.Development,extraScriptingDefines=rig?new[]{"MOFUMACHI_NOAH_FINAL_RIG"}:Array.Empty<string>()});
                if(report.summary.result!=BuildResult.Succeeded||!File.Exists(temporary)||new FileInfo(temporary).Length==0)throw new BuildFailedException("Experimental "+mode+" APK failed.");
                File.Move(temporary,output);string hash;using(var sha=SHA256.Create())using(var stream=File.OpenRead(output))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
                File.WriteAllText(infoPath,JsonUtility.ToJson(new BuildInfo{mode=mode,gitRevision=revision,productFingerprint=fingerprint,sha256=hash,apk=Path.GetFileName(output)},true));
                Debug.Log("MOFUMACHI_NOAH_TRIAL_APK "+mode+" "+output+" "+hash+"; not full-QA or release acceptance");
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);if(wrote)AssetDatabase.DeleteAsset("Assets/Mofumachi/Resources/Mofumachi/QaBuildRevision.txt");}
        }
    }
}
