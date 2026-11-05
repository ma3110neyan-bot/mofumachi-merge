#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;
using UnityEngine.UI;

[assembly: TestRunCallback(typeof(Mofumachi.FontValidation.FontValidationCallback))]
namespace Mofumachi.FontValidation
{
    // Tooling installed only in the disposable 75cd7c0 worktree. No test methods.
    public static class PassedFontTests
    {
        public const string Source="75cd7c0ef234934a17aa623860937d894a1d875c";
        public const string First="Mofumachi.Tests.ReleaseUxNativeTests.ScopedFontsFitTitleTownMergeBookSettingsAndTodayAtPhoneSizes";
        public const string Second="Mofumachi.Tests.StoryTypographyTests.StoryRolesAndNoticeFitPortraitSizes";
        public static bool CanCapture(string[] names,string[] states,bool runPassed)
        {
            return runPassed&&names!=null&&states!=null&&names.Length==2&&states.Length==2&&
                names.Count(n=>n==First)==1&&names.Count(n=>n==Second)==1&&states.All(s=>s=="Passed");
        }
    }
    public sealed class FontValidationCallback:ITestRunCallback
    {
        private static bool handled;
        [Serializable] private sealed class ImageRow {public string file,sha256;public int width,height;}
        [Serializable] private sealed class Proof
        {
            public string source_revision=PassedFontTests.Source,unity_version;
            public bool tests_passed_before_capture=true;
            public int image_count;
            public List<ImageRow> images=new List<ImageRow>();
        }
        private sealed class MemoryState:IStateStore
        {
            private GameState saved;
            public int Writes;
            public MemoryState(GameState state){saved=state.Clone();}
            public LoadResult Load()=>new LoadResult(saved.Clone());
            public void Save(GameState state){saved=state.Clone();Writes++;}
            public void ResetSave(){throw new InvalidOperationException("Capture must not reset saves.");}
        }
        private static string Argument(string name)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);
            return i>=0&&i+1<args.Length?args[i+1]:null;
        }
        private static IEnumerable<ITestResult> Leaves(ITestResult result)
        {
            if(!result.Test.IsSuite){yield return result;yield break;}
            foreach(var child in result.Children)foreach(var leaf in Leaves(child))yield return leaf;
        }
        private static string Hash(byte[] bytes)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public void RunStarted(ITest testsToRun){}
        public void TestStarted(ITest test){}
        public void TestFinished(ITestResult result){}
        public void RunFinished(ITestResult result)
        {
            string directory=Argument("-mofumachiFontCheckOutput");
            if(directory==null||handled)return;
            handled=true;
            try
            {
                var leaves=Leaves(result).ToArray();
                bool passed=result.ResultState.Status==TestStatus.Passed&&result.PassCount==2&&result.FailCount==0&&result.SkipCount==0&&result.InconclusiveCount==0;
                if(!PassedFontTests.CanCapture(leaves.Select(r=>r.Test.FullName).ToArray(),leaves.Select(r=>r.ResultState.Status.ToString()).ToArray(),passed))return;
                if(!Application.isPlaying)throw new InvalidOperationException("Post-test capture callback must run before leaving PlayMode.");
                if(Application.unityVersion!="6000.6.4f1"||UnityEditor.EditorUserBuildSettings.activeBuildTarget!=UnityEditor.BuildTarget.Android)
                    throw new InvalidOperationException("Unity6000.6.4f1 / Android target required.");
                if(Argument("-mofumachiSourceRevision")!=PassedFontTests.Source)throw new InvalidOperationException("Pinned source revision differs.");
                if(!Path.IsPathRooted(directory))throw new IOException("Absolute diagnostic output required.");
                Capture(directory);
            }
            catch(Exception error)
            {
                File.WriteAllText(Path.Combine(directory,"capture-error.log"),error.ToString(),new UTF8Encoding(true));
            }
        }
        private static void Capture(string directory)
        {
            if(UnityEngine.Object.FindAnyObjectByType<UIFlowController>()!=null)throw new InvalidOperationException("The isolated tests did not clean their UI.");
            string images=Path.Combine(directory,"images");
            if(Directory.Exists(images))throw new IOException("Capture output must be fresh.");
            Directory.CreateDirectory(images);
            var session=typeof(UIFlowController).GetField("session",BindingFlags.Static|BindingFlags.NonPublic);
            var social=typeof(UIFlowController).GetField("socialSession",BindingFlags.Static|BindingFlags.NonPublic);
            object previousSession=session.GetValue(null),previousSocial=social.GetValue(null);
            var previousAudio=AudioManager.Instance;
            GameObject ui=null;Camera camera=null;RenderTexture target=null;
            try
            {
                var state=GameState.CreateInitial();state.purchaseNoticeAcknowledged=true;state.favoriteCharacterId="momo";
                var store=new MemoryState(state);
                session.SetValue(null,new GameStateManager(state,store));
                social.SetValue(null,new TownDailyService(new MemoryTownDailyStore(),DateTime.Now));
                ui=new GameObject("Post-success font evidence only");
                var flow=ui.AddComponent<UIFlowController>();
                camera=new GameObject("Post-success native evidence camera",typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=UIWidgets.Cream;
                camera.cullingMask=1<<30;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                camera.transform.position=new Vector3(0,0,-20);
                var proof=new Proof{unity_version=Application.unityVersion};
                foreach(var size in new[]{new Vector2Int(720,1480),new Vector2Int(640,1072)})
                {
                    if(target!=null){camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}
                    target=new RenderTexture(size.x,size.y,24);target.Create();camera.targetTexture=target;
                    camera.orthographicSize=size.y/4f;camera.aspect=size.x/(float)size.y;
                    foreach(string screen in new[]{"merge","conversation"})
                    {
                        if(screen=="merge")flow.ShowMerge();else flow.ShowHome();
                        var canvas=flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;
                        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=30;canvas.scaleFactor=2;
                        flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(size.x,size.y),new Rect(0,0,size.x,size.y));
                        foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                        Canvas.ForceUpdateCanvases();foreach(var frame in flow.GetComponentsInChildren<ProductionFrameLayout>())frame.ResizeNow();
                        Canvas.ForceUpdateCanvases();foreach(var grid in flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();
                        var town=flow.GetComponentInChildren<TownLifeView>();
                        if(town!=null)
                        {
                            town.World.Refresh();town.RefreshWorldPresentation();
                            var actor=town.Actors.First(a=>a.RegionVisible&&!a.Controlled);
                            typeof(TownLifeView).GetMethod("ShowBubble",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(town,new object[]{TownCharacterDialogue.For(actor.CharacterId,1),actor});
                            if(!town.IsSpeaking)throw new InvalidOperationException("Actual conversation was not opened.");
                        }
                        foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                        Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
                        string before=StateCodec.Encode(flow.Game.State);int writes=store.Writes;
                        ReleaseTypography.ApplyTo(flow.transform);Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
                        var labels=flow.GetComponentsInChildren<Text>().Where(t=>t.IsActive()&&!string.IsNullOrWhiteSpace(t.text)&&!t.canvasRenderer.cull&&ReleaseFontPolicy.For(t.name,t.fontSize,t.text)!=ReleaseFontRole.Unchanged).ToArray();
                        if(labels.Length==0)throw new InvalidOperationException("No actual UI labels were rendered.");
                        foreach(var text in labels)
                            if(text.canvasRenderer.GetMesh().vertexCount<=0)throw new InvalidOperationException(text.name+": empty native glyph mesh after passed tests.");
                        string file=screen+"-"+size.x+"x"+size.y+".png";
                        var previous=RenderTexture.active;Texture2D picture=null;
                        try
                        {
                            camera.Render();RenderTexture.active=target;picture=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                            picture.ReadPixels(new Rect(0,0,size.x,size.y),0,0);picture.Apply();byte[] bytes=picture.EncodeToPNG();
                            File.WriteAllBytes(Path.Combine(images,file),bytes);
                            proof.images.Add(new ImageRow{file=file,width=size.x,height=size.y,sha256=Hash(bytes)});
                        }
                        finally{RenderTexture.active=previous;if(picture!=null)UnityEngine.Object.DestroyImmediate(picture);}
                        if(store.Writes!=writes||StateCodec.Encode(flow.Game.State)!=before)throw new InvalidOperationException("Capture changed its private in-memory game state.");
                    }
                }
                proof.image_count=proof.images.Count;
                File.WriteAllText(Path.Combine(directory,"capture-native.json"),JsonUtility.ToJson(proof,true),new UTF8Encoding(true));
            }
            finally
            {
                if(ui!=null)UnityEngine.Object.DestroyImmediate(ui);
                if(camera!=null){camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);}
                if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                if(previousAudio==null&&AudioManager.Instance!=null)UnityEngine.Object.DestroyImmediate(AudioManager.Instance.gameObject);
                session.SetValue(null,previousSession);social.SetValue(null,previousSocial);
            }
        }
    }
}
#endif
