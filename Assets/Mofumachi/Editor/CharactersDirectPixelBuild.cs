using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mofumachi.Editor
{
    // Explicit user-requested device preview. This entry never runs test suites
    // and never represents an APK build as full QA or device acceptance.
    public static class CharactersDirectPixelBuild
    {
        [Serializable] private sealed class Context
        { public string gitRevision=null,productFingerprint=null,packed_resource_sha256=null; }
        [Serializable] private sealed class Identity
        {
            public string scope="characters-direct-pixel-no-tests",mode="authored",package=AndroidBuild.TownQaApplicationId;
            public string gitRevision,productFingerprint,packed_resource_sha256,sha256,apk="characters-authored.apk";
            public bool tests_run=false,full_qa_verified=false,pixel_verified=false;
        }
        private static string Root=>Directory.GetParent(Application.dataPath).FullName;
        private static string Hash(byte[] bytes)
        { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        private static string Packed(string run)
        {
            var rows=new System.Collections.Generic.List<string>();
            foreach(string relative in new[]{CharacterAuthoredBuild.ResourceFolder,NoahAuthoredBuild.ResourceFolder})
            {
                string folder=Path.Combine(Root,relative),owner=Path.Combine(folder,".owner");
                if(!File.Exists(owner)||File.ReadAllText(owner)!=run)throw new BuildFailedException("Resources do not belong to this direct preview.");
                foreach(string path in Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Where(p=>Path.GetFileName(p)!=".owner"))
                    rows.Add(path.Substring(Root.Length+1).Replace('\\','/')+":"+Hash(File.ReadAllBytes(path)));
            }
            return Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",rows.OrderBy(s=>s,StringComparer.Ordinal))));
        }
        public static void Build()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-mofumachiCharactersDirectDirectory");
            if(i<0||i+1>=args.Length)throw new BuildFailedException("Exact direct-preview directory required.");
            string run=Path.GetFullPath(args[i+1]);
            if(!run.StartsWith(Path.Combine(Root,"Builds","Diagnostics")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!Directory.Exists(run))throw new BuildFailedException("Use a new Diagnostics run.");
            var c=JsonUtility.FromJson<Context>(File.ReadAllText(Path.Combine(run,"direct-context.json")));
            if(c==null||!Regex.IsMatch(c.gitRevision??"","^[a-f0-9]{40}$")||!Regex.IsMatch(c.productFingerprint??"","^[a-f0-9]{64}$")||!Regex.IsMatch(c.packed_resource_sha256??"","^[a-f0-9]{64}$")||Packed(run)!=c.packed_resource_sha256)throw new BuildFailedException("Direct preview source/resource identity missing or changed.");
            if(Application.unityVersion!="6000.6.4f1"||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android||!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new BuildFailedException("Unity6000.6.4f1 with Android support required.");
            if(PlayerSettings.Android.targetArchitectures!=AndroidArchitecture.ARM64||PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)!=ScriptingImplementation.IL2CPP)throw new BuildFailedException("Keep the existing ARM64/IL2CPP Pixel settings.");
            string[] defines=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';');
            if(defines.Any(s=>s=="MOFUMACHI_NOAH_AUTHORED"||s=="MOFUMACHI_CHARACTERS_AUTHORED"))throw new BuildFailedException("Authored defines must stay build-local.");
            string apk=Path.Combine(run,"characters-authored.apk"),identity=Path.Combine(run,"characters-authored-build.json"),stamp="Assets/Mofumachi/Resources/Mofumachi/QaBuildRevision.txt";
            if(File.Exists(apk)||File.Exists(identity)||File.Exists(stamp)||File.Exists(stamp+".meta"))throw new BuildFailedException("Preserve existing APK, identity and revision assets.");
            AndroidBuild.ValidateContent();
            string temporary=Path.Combine(run,"building-"+Guid.NewGuid().ToString("N")+".apk");bool wrote=false;
            try
            {
                File.WriteAllText(stamp,c.gitRevision);wrote=true;AssetDatabase.ImportAsset(stamp,ImportAssetOptions.ForceSynchronousImport);
                AndroidBuild.WithTownQaIdentity(()=>
                {
                    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                        scenes=AndroidBuild.Scenes,locationPathName=temporary,target=BuildTarget.Android,options=BuildOptions.Development,
                        extraScriptingDefines=new[]{"MOFUMACHI_NOAH_AUTHORED","MOFUMACHI_CHARACTERS_AUTHORED"}});
                    if(report.summary.result!=BuildResult.Succeeded||!File.Exists(temporary)||new FileInfo(temporary).Length==0||Packed(run)!=c.packed_resource_sha256)throw new BuildFailedException("Direct APK build/resource verification failed.");
                });
                File.Move(temporary,apk);
                File.WriteAllText(identity,JsonUtility.ToJson(new Identity{gitRevision=c.gitRevision,productFingerprint=c.productFingerprint,packed_resource_sha256=c.packed_resource_sha256,sha256=Hash(File.ReadAllBytes(apk))},true));
                Debug.Log("MOFUMACHI_DIRECT_PIXEL_APK_SUCCESS "+apk);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);if(wrote)AssetDatabase.DeleteAsset(stamp);}
        }
    }
}
