using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed partial class ExperienceCaptureTests
    {
        [Serializable] private sealed class CalibrationRow { public string character=null,direction=null;public float[] head_local=null; }
        [Serializable] private sealed class CalibrationReference { public CalibrationRow[] rows=null; }
        [Serializable] private sealed class CalibrationFrame
        { public string character,direction;public int frame;public bool blink,front_single_mesh;public float error,root_x,root_y; }
        [Serializable] private sealed class CalibrationCharacter
        { public string character,result="PENDING",visual_result="PENDING";public int frames,misalignments=0; }
        [Serializable] private sealed class CalibrationProof
        {
            public string kind="individual-character-head-calibration",gitRevision,productFingerprint,packed_resource_sha256,reference_sha256;
            public string setup="isolated character-only UGUI; fixed60Hz pose sequence; no navigation, town or Pixel acceptance";
            public bool native_unity=true,non_head_unchanged,head_art_size_angle_unchanged,continuous_verified,jpeg_decode_verified;
            public bool full_qa_verified=false,pixel_verified=false,formal_fixed=false,adopted=false;
            public List<CalibrationCharacter> characters=new List<CalibrationCharacter>();
            public List<CalibrationFrame> frames=new List<CalibrationFrame>();public List<NativeImage> images=new List<NativeImage>();
        }
        private static NoahAuthoredDirection CalibrationDirection(int frame)
        {
            return frame<180?NoahAuthoredDirection.Front:frame<360?NoahAuthoredDirection.Left:
                frame<540?NoahAuthoredDirection.Right:frame<720?NoahAuthoredDirection.Back:
                frame<990?NoahAuthoredDirection.Front:(NoahAuthoredDirection)(frame%4);
        }
        private static float CalibrationHeadError(Graphic graphic,CalibrationRow expected)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var pose=(NoahAuthoredPose)graphic.GetType().GetField("pose",flags).GetValue(graphic);
            Vector2 pivot=default;int index=-1;float fw=254,fh=323;
            if(graphic is NoahAuthoredGraphic)
            {
                var view=(NoahAuthoredView)graphic.GetType().GetField("view",flags).GetValue(graphic);
                for(int i=0;i<view.parts.Length;i++)if(view.parts[i].id=="head"){index=i*4;pivot=view.parts[i].pivot;}
            }
            else
            {
                var view=(CharacterAuthoredView)graphic.GetType().GetField("view",flags).GetValue(graphic);
                var config=(CharacterAuthoredConfig)graphic.GetType().GetField("config",flags).GetValue(graphic);
                fw=config.formalWidth;fh=config.formalHeight;
                for(int i=0;i<view.parts.Length;i++)if(view.parts[i].id=="head"){index=i*4;pivot=view.PartFor(view.parts[i],((CharacterAuthoredGraphic)graphic).EyesClosed).pivot;}
            }
            Assert.That(index,Is.GreaterThanOrEqualTo(0));var mesh=graphic.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);var v=mesh.vertices;
            var actual=v[index+1]+(v[index+2]-v[index+1])*pivot.x+(v[index]-v[index+1])*pivot.y;
            // Independent test fixture, not the production offset lookup.
            pose.Body.Point(expected.head_local[0],expected.head_local[1],out float x,out float y);
            var rect=graphic.GetPixelAdjustedRect();float s=Mathf.Min(rect.width/fw,rect.height/fh)*fh/600;
            return Vector3.Distance(actual,new Vector3(rect.center.x+x*s,rect.yMin-y*s));
        }
        [UnityTest,Timeout(600000)] public IEnumerator SixCharactersKeepCalibratedHeadsAttached()
        {
            if(!Environment.GetCommandLineArgs().Contains("-mofumachiHeadCalibration"))yield break;
            string run=EvidenceArgument("-mofumachiHeadCalibrationDirectory"),output=Path.Combine(run,"screenshots","head-calibration");Directory.CreateDirectory(output);
            byte[] referenceBytes=System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(Application.dataPath,"../Tests/Fixtures/character-head-calibration-2026-11-02.json")).Replace("\r\n","\n"));
            var reference=JsonUtility.FromJson<CalibrationReference>(System.Text.Encoding.UTF8.GetString(referenceBytes));
            var proof=new CalibrationProof{gitRevision=EvidenceArgument("-mofumachiSourceRevision"),productFingerprint=EvidenceArgument("-mofumachiProductFingerprint"),packed_resource_sha256=EvidenceArgument("-mofumachiPackedDigest"),reference_sha256=EvidenceSha(referenceBytes)};
            // No scene load, town, navigation, conversation or Save assertions.
            var canvasObject=new GameObject("HeadCalibrationOnly",typeof(RectTransform),typeof(Canvas));canvasObject.layer=31;
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvasObject.GetComponent<RectTransform>().sizeDelta=new Vector2(128,160);
            var cameraObject=new GameObject("HeadCalibrationCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();cameraObject.transform.position=new Vector3(0,0,-10);
            camera.orthographic=true;camera.orthographicSize=80;camera.aspect=.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.93f,.94f,.96f);camera.cullingMask=1<<31;camera.enabled=false;canvas.worldCamera=camera;
            var target=new RenderTexture(480,600,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            try
            {
                foreach(string id in new[]{"noah","momo","lulu","pom","fio","miel"})
                {
                    var result=new CalibrationCharacter{character=id};proof.characters.Add(result);
                    var rootObject=new GameObject("CharacterRoot-"+id,typeof(RectTransform));rootObject.layer=31;rootObject.transform.SetParent(canvasObject.transform,false);
                    var root=rootObject.GetComponent<RectTransform>();root.pivot=new Vector2(.5f,0);root.sizeDelta=new Vector2(62,78);root.anchoredPosition=new Vector2(0,-42);
                    Graphic MakeGraphic(Type type)
                    {var go=new GameObject(type.Name,typeof(RectTransform),typeof(CanvasRenderer),type);go.layer=31;go.transform.SetParent(root,false);var r=go.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return go.GetComponent<Graphic>();}
                    var front=(CharacterBodyGraphic)MakeGraphic(typeof(CharacterBodyGraphic));front.Bind(id);
                    Graphic graphic=MakeGraphic(id=="noah"?typeof(NoahAuthoredGraphic):typeof(CharacterAuthoredGraphic));
                    if(graphic is NoahAuthoredGraphic noah)noah.Configure();else ((CharacterAuthoredGraphic)graphic).Configure(id);
                    var previous=graphic.GetType().GetField("capturePreviousNeck",flags);Assert.That(previous,Is.Not.Null);
                    try
                    {
                        for(int frame=0;frame<1054;frame++)
                        {
                            var direction=CalibrationDirection(frame);bool split=direction!=NoahAuthoredDirection.Front,blink=frame%17==0;
                            float walking=frame>=750&&frame<810?0:1,t=frame/60f,phase=frame*.19f;
                            var mode=walking==0?NoahAuthoredMode.Idle:NoahAuthoredMode.Walk;
                            float travelFrame=frame<750?frame:frame<810?749:frame-60,travel=travelFrame/60f;
                            root.anchoredPosition=new Vector2(Mathf.Sin(travel*.7f)*6,-42+Mathf.Cos(travel*.5f)*2);
                            front.enabled=!split;graphic.enabled=split;front.Pose(walking*Mathf.Sin(phase),0,0,blink);
                            void Pose(bool old)
                            {previous.SetValue(graphic,old);if(graphic is NoahAuthoredGraphic n)n.SetPose(t,phase,walking,mode,direction);else ((CharacterAuthoredGraphic)graphic).SetPose(t,phase,walking,mode,direction,blink);Canvas.ForceUpdateCanvases();}
                            Pose(true);var before=split?graphic.canvasRenderer.GetMesh():null;var beforeVertices=before?.vertices;var beforeUv=before?.uv;
                            Pose(false);var sample=new CalibrationFrame{character=id,frame=frame,direction=direction.ToString(),front_single_mesh=!split,blink=split&&id!="noah"&&direction!=NoahAuthoredDirection.Back&&blink,root_x=root.anchoredPosition.x,root_y=root.anchoredPosition.y};
                            if(frame>=750&&frame<810){var last=proof.frames.Last();Assert.That(new Vector2(sample.root_x,sample.root_y),Is.EqualTo(new Vector2(last.root_x,last.root_y)),"The common root must remain stopped");}
                            if(split)
                            {
                                string name=direction.ToString().ToLowerInvariant();var row=reference.rows.Single(r=>r.character==id&&r.direction==name);
                                sample.error=CalibrationHeadError(graphic,row);Assert.That(sample.error,Is.LessThanOrEqualTo(.002f),id+" frame "+frame);
                                var mesh=graphic.canvasRenderer.GetMesh();var v=mesh.vertices;Assert.That(mesh.uv,Is.EqualTo(beforeUv));
                                int hi;if(graphic is NoahAuthoredGraphic)hi=Array.FindIndex(((NoahAuthoredView)graphic.GetType().GetField("view",flags).GetValue(graphic)).parts,p=>p.id=="head")*4;
                                else hi=Array.FindIndex(((CharacterAuthoredView)graphic.GetType().GetField("view",flags).GetValue(graphic)).parts,p=>p.id=="head")*4;
                                for(int i=0;i<v.Length;i++)if(i<hi||i>=hi+4)Assert.That(v[i],Is.EqualTo(beforeVertices[i]),"Only head position changes");
                                for(int i=1;i<4;i++)Assert.That(Vector3.Distance(v[hi+i]-v[hi],beforeVertices[hi+i]-beforeVertices[hi]),Is.LessThan(.0002f),"Head size and angle unchanged");
                            }
                            proof.frames.Add(sample);result.frames++;
                            // 5fps review plus exact turn/stop boundary frames; all60Hz poses measured.
                            bool boundary=new[]{179,180,181,359,360,361,539,540,541,719,720,721,749,750,751,809,810,811,989,990,991,1053}.Contains(frame);
                            if(frame%12==0||boundary||frame>=990)
                            {
                                var old=RenderTexture.active;Texture2D image=null;
                                try
                                {
                                    Pose(false);camera.Render();RenderTexture.active=target;image=new Texture2D(480,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,600),0,0);image.Apply();byte[] jpeg=image.EncodeToJPG(95);
                                    var decoded=new Texture2D(1,1,TextureFormat.RGB24,false);
                                    try{Assert.That(ImageConversion.LoadImage(decoded,jpeg),Is.True,"JPEG must fully decode");Assert.That(decoded.width,Is.EqualTo(480));Assert.That(decoded.height,Is.EqualTo(600));}finally{UnityEngine.Object.DestroyImmediate(decoded);}
                                    proof.images.Add(SaveNativeEncoding(output,id+"-"+frame.ToString("D4")+".jpg",image,jpeg));
                                }
                                finally{RenderTexture.active=old;if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
                            }
                            yield return null;
                        }
                        Assert.That(result.frames,Is.EqualTo(1054));result.result="PASS";
                    }
                    finally{if(result.result!="PASS")result.result="FAIL";previous.SetValue(graphic,false);UnityEngine.Object.DestroyImmediate(rootObject);File.WriteAllText(Path.Combine(output,"head-calibration-proof.json"),JsonUtility.ToJson(proof,true));}
                    File.WriteAllText(Path.Combine(output,"head-calibration-proof.json"),JsonUtility.ToJson(proof,true));
                }
                proof.non_head_unchanged=proof.head_art_size_angle_unchanged=proof.continuous_verified=proof.jpeg_decode_verified=true;
                File.WriteAllText(Path.Combine(output,"head-calibration-proof.json"),JsonUtility.ToJson(proof,true));
                File.WriteAllText(Path.Combine(output,"head-calibration-review.html"),"<!doctype html><meta charset=utf-8><title>頭と胴体の連続確認</title><p>実Unity・キャラ単体。60Hz姿勢を全件計測、画像は5fpsと切替直後。Pixel／街／全件テストの結果ではありません。</p><select id=who></select><button id=toggle>停止／再生</button><input id=seek type=range min=0 value=0><p id=label></p><img id=picture width=480><script>const p="+JsonUtility.ToJson(proof).Replace("<","\\u003c")+";let i=0,play=true;for(const c of p.characters){const o=document.createElement('option');o.value=o.textContent=c.character;who.append(o)}function show(){let a=p.images.filter(x=>x.file.startsWith(who.value+'-'));seek.max=a.length-1;i=Math.min(i,a.length-1);picture.src=a[i].file;label.textContent=a[i].file;seek.value=i}who.onchange=()=>{i=0;show()};seek.oninput=()=>{i=+seek.value;show()};toggle.onclick=()=>play=!play;setInterval(()=>{if(play){i=(i+1)%(+seek.max+1);show()}},200);show()</script>");
                LogAssert.NoUnexpectedReceived();
            }
            finally{camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(canvasObject);}
        }
    }
}
