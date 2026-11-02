using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    // Actual UGUI rendering of isolated saves. Never reads or replaces a player's save.
    public sealed class MergeRewardCaptureTests : SliceUiTestFixture
    {
        [Serializable] private sealed class Shot
        {public string file,sha256,state_sha256,scenario;public int width,height;public bool before_reconstructed,state_unchanged,writes_unchanged;}
        [Serializable] private sealed class Proof
        {
            public string kind="merge-reward-native-candidate",source_revision,product_fingerprint,packed_resource_sha256;
            public string reference_path="References/Merge/merge_visual_lock_reward.png",reference_sha256="";
            public string output_directory;public int image_count;public bool disk_verified;
            public bool native_unity=true,original_png_saved,formal_fixed=false,adopted=false,pixel_verified=false;
            public List<Shot> images=new List<Shot>();
        }
        private Camera camera;private RenderTexture target;private Canvas canvas;
        private static readonly MethodInfo Render=typeof(UIFlowController).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic);
        private static string Argument(string key){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:"";}
        private static string Sha(byte[] bytes){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        [TearDown] public void ReleaseCapture()
        {
            MergeRewardPresentation.CaptureBefore=false;
            if(canvas!=null)canvas.worldCamera=null;
            if(camera!=null){camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(camera.gameObject);}
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        private Texture2D Portrait(int width,int height)
        {
            canvas=Flow.GetComponentInChildren<Canvas>();
            if(camera==null)
            {
                camera=new GameObject("Merge isolated evidence camera",typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=MergeRewardView.Cream;
                camera.cullingMask=1<<30;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                camera.transform.position=new Vector3(0,0,-20);
            }
            if(target==null||target.width!=width||target.height!=height)
            {
                if(target!=null){camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}
                target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;
            }
            camera.orthographicSize=height/4f;camera.aspect=width/(float)height;
            canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=30;canvas.scaleFactor=2;
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(width,height),new Rect(0,0,width,height));
            foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            Canvas.ForceUpdateCanvases();
            foreach(var layout in Flow.GetComponentsInChildren<ProductionFrameLayout>())layout.ResizeNow();
            Canvas.ForceUpdateCanvases();foreach(var grid in Flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();
            Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            foreach(var cell in cells){var r=((RectTransform)cell.transform).rect;Assert.That(r.width,Is.GreaterThanOrEqualTo(48));Assert.That(r.height,Is.GreaterThanOrEqualTo(48));}
            var previous=RenderTexture.active;Texture2D image=null;
            try{camera.Render();RenderTexture.active=target;image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return image;}
            catch{if(image!=null)UnityEngine.Object.DestroyImmediate(image);throw;}
            finally{RenderTexture.active=previous;}
        }
        private Shot Capture(string output,string scenario,int width,int height,bool before)
        {
            MergeRewardPresentation.CaptureBefore=before;
            // Render only: calling ShowMerge here would add navigation saves to the comparison.
            string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            Render.Invoke(Flow,new object[]{"merge",false,false});
            var image=Portrait(width,height);Texture2D decoded=null;
            try
            {
                byte[] bytes=image.EncodeToJPG(92);
                var colors=Enumerable.Range(0,10).SelectMany(x=>Enumerable.Range(0,16).Select(y=>(Color32)image.GetPixel(20+x*(width-40)/10,20+y*(height-40)/16))).Distinct().Count();
                Assert.That(colors,Is.GreaterThan(12),"Reject empty/clear captures");
                Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));
                string name=scenario+"-"+width+"x"+height+(before?"-before-reconstructed":"-after")+".jpg";
                string file=Path.Combine(output,name);File.WriteAllBytes(file,bytes);
                byte[] written=File.ReadAllBytes(file);Assert.That(written,Is.EqualTo(bytes),"The actual output JPEG must equal the encoded capture");
                decoded=new Texture2D(2,2);Assert.That(decoded.LoadImage(written),Is.True);Assert.That(decoded.width,Is.EqualTo(width));Assert.That(decoded.height,Is.EqualTo(height));
                return new Shot{file=name,sha256=Sha(bytes),scenario=scenario,width=width,height=height,before_reconstructed=before,state_sha256=Sha(System.Text.Encoding.UTF8.GetBytes(state)),state_unchanged=true,writes_unchanged=true};
            }
            finally{if(decoded!=null)UnityEngine.Object.DestroyImmediate(decoded);UnityEngine.Object.DestroyImmediate(image);}
        }
        private void Fixture(int questIndex,bool ready=false,bool full=false)
        {
            var state=Flow.Game.State.Clone();state.activeQuestId=QuestCatalog.All[questIndex].Id;state.questState=QuestState.Accepted;
            state.mergeBoard.Clear();state.inventory.Clear();
            if(ready)foreach(var requirement in QuestCatalog.All[questIndex].Requirements)state.inventory.Add(new InventoryEntry(requirement.ItemId,requirement.Level,requirement.Count));
            int count=full?30:3;for(int i=0;i<count;i++)state.mergeBoard.Add(new BoardItem("tea",questIndex==1?3:1,i));
            Store.Save(state);Flow.Initialize(Store);Flow.ShowMerge();
        }
        [UnityTest] public IEnumerator MergeRewardCandidateScreens()
        {
            var proof=new Proof{source_revision=Argument("-mofumachiSourceRevision"),product_fingerprint=Argument("-mofumachiProductFingerprint"),packed_resource_sha256=Argument("-mofumachiPackedResourceSha")};
            string output=MergeCaptureArtifacts.RequireOutput(Environment.GetCommandLineArgs());proof.output_directory=output;
            string reference=Path.Combine(Application.dataPath,"../",proof.reference_path);
            if(File.Exists(reference))
            {
                byte[] bytes=File.ReadAllBytes(reference);var signature=new byte[]{137,80,78,71,13,10,26,10};
                Assert.That(bytes.Take(8),Is.EqualTo(signature),"The fixed reference must be a real PNG, not a renamed preview");
                var original=new Texture2D(2,2);
                try{Assert.That(original.LoadImage(bytes),Is.True);Assert.That(original.width,Is.GreaterThan(0));Assert.That(original.height,Is.GreaterThan(0));}
                finally{UnityEngine.Object.DestroyImmediate(original);}
                proof.original_png_saved=true;proof.reference_sha256=Sha(bytes);
            }
            yield return EnterHome();Flow.ShowMerge();yield return null;
            try
            {
                foreach(int height in new[]{1480,1280,1072})
                {
                    int width=height==1072?640:720;
                    proof.images.Add(Capture(output,"initial",width,height,true));proof.images.Add(Capture(output,"initial",width,height,false));
                }
                Fixture(1);proof.images.Add(Capture(output,"bread-request",720,1480,true));proof.images.Add(Capture(output,"bread-request",720,1480,false));
                Fixture(3);proof.images.Add(Capture(output,"multi-requirement",720,1480,false));
                Fixture(0,true);proof.images.Add(Capture(output,"inventory-ready",720,1480,false));
                Fixture(0,false,true);proof.images.Add(Capture(output,"full-board",720,1280,false));
                Assert.That(proof.images.Count,Is.EqualTo(11));
                proof.image_count=proof.images.Count;
                string proofPath=Path.Combine(output,"merge-reward-native.json");File.WriteAllText(proofPath,JsonUtility.ToJson(proof,true));
                MergeCaptureArtifacts.VerifyWritten(output,proof.images.ToDictionary(i=>i.file,i=>i.sha256,StringComparer.Ordinal));
                proof.disk_verified=true;File.WriteAllText(proofPath,JsonUtility.ToJson(proof,true));
                var saved=JsonUtility.FromJson<Proof>(File.ReadAllText(proofPath));Assert.That(saved,Is.Not.Null);
                Assert.That(saved.disk_verified,Is.True);Assert.That(saved.output_directory,Is.EqualTo(output));Assert.That(saved.image_count,Is.EqualTo(11));
                MergeCaptureArtifacts.VerifyWritten(output,saved.images.ToDictionary(i=>i.file,i=>i.sha256,StringComparer.Ordinal));
                LogAssert.NoUnexpectedReceived();
            }
            finally{MergeRewardPresentation.CaptureBefore=false;}
        }
    }
}
