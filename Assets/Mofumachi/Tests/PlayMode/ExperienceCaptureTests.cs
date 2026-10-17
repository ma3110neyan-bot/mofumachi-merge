using System;
using System.Collections;
using System.IO;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    // Runs in the normal native suite. Optional output is actual UGUI rendering
    // from an isolated fixture, never the developer's/Pixel player's saved town.
    public sealed class ExperienceCaptureTests : SliceUiTestFixture
    {
        private static string OutputDirectory()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-mofumachiScreenshots");
            return i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):null;
        }
        private Camera captureCamera;private RenderTexture target;private Canvas captureCanvas;
        private void EnsurePortraitCamera()
        {
            captureCanvas=Flow.GetComponentInChildren<Canvas>();
            if(captureCamera==null)
            {
                var cameraObject=new GameObject("Isolated portrait capture camera",typeof(Camera));captureCamera=cameraObject.GetComponent<Camera>();
                target=new RenderTexture(720,1280,24);target.Create();captureCamera.targetTexture=target;
                captureCamera.clearFlags=CameraClearFlags.SolidColor;captureCamera.backgroundColor=new Color(.98f,.94f,.91f);captureCamera.cullingMask=1<<30;
                captureCamera.orthographic=true;captureCamera.orthographicSize=320;captureCamera.nearClipPlane=.1f;captureCamera.farClipPlane=100;captureCamera.transform.position=new Vector3(0,0,-20);
                captureCanvas.GetComponent<CanvasScaler>().enabled=false;captureCanvas.renderMode=RenderMode.ScreenSpaceCamera;captureCanvas.worldCamera=captureCamera;captureCanvas.planeDistance=10;captureCanvas.scaleFactor=2;
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(720,1280),new Rect(0,0,720,1280));
            }
            foreach(var t in captureCanvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        }
        private Texture2D Portrait()
        {
            EnsurePortraitCamera();var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                Canvas.ForceUpdateCanvases();foreach(var grid in Flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();Canvas.ForceUpdateCanvases();
                captureCamera.Render();RenderTexture.active=target;image=new Texture2D(720,1280,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,1280),0,0);image.Apply();return image;
            }
            catch{if(image!=null)UnityEngine.Object.DestroyImmediate(image);throw;}
            finally{RenderTexture.active=previous;}
        }
        [TearDown] public void ReleasePortraitCamera()
        {
            if(captureCanvas!=null)captureCanvas.worldCamera=null;
            if(captureCamera!=null){captureCamera.targetTexture=null;UnityEngine.Object.DestroyImmediate(captureCamera.gameObject);}
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}captureCamera=null;target=null;captureCanvas=null;
        }
        private void Capture(string directory,string name)
        {
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);var image=Portrait();
            try
            {
                var colors=Enumerable.Range(0,12).SelectMany(x=>Enumerable.Range(0,20).Select(y=>(Color32)image.GetPixel(30+x*55,30+y*60))).Distinct().Count();
                Assert.That(colors,Is.GreaterThan(12),"Portrait rendering must contain more than a clear background");
                if(directory!=null){Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());}
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        [UnityTest] public IEnumerator PortraitScreensRenderFromIsolatedStateWithoutWritingProgress()
        {
            yield return EnterHome();string output=OutputDirectory();Flow.ShowMerge();yield return null;Capture(output,"01-merge-start");
            if(output!=null)
            {
                Flow.Game.State.mergeBoard.Clear();int cell=0;
                foreach(var item in ItemCatalog.All)for(int level=1;level<=3;level++)Flow.Game.State.mergeBoard.Add(new BoardItem(item.Id,level,cell++));
                Flow.ShowMerge();Capture(output,"02-merge-all-items");Flow.TapCell(0);Capture(output,"03-merge-selection");
                foreach(int level in new[]{1,3,6})foreach(TownRegionId region in Enum.GetValues(typeof(TownRegionId)))
                {
                    Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.Regions.LookAtRegion(region);yield return null;Canvas.ForceUpdateCanvases();
                    Capture(output,"04-town-lv"+level+"-"+region);
                }
                Flow.ShowHome();var town=Flow.GetComponentInChildren<TownLifeView>();town.Regions.ReturnToHero();town.GetComponent<TownStreetWalkView>().Arm();Capture(output,"05-street-selected");
                town.GuideToPlace("fountain");
                for(int frame=0;frame<8;frame++){for(int step=0;step<4;step++)town.AdvanceFrame(1f/30);Capture(output,"06-walking-"+frame.ToString("D2"));}
                Flow.ShowQuest();Capture(output,"07-quest-story-goal");Flow.ShowSettings();
                var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();scroll.verticalNormalizedPosition=.12f;Canvas.ForceUpdateCanvases();Capture(output,"08-music-selection");
                Flow.GoBack();Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var seam in new[]{("north",new TownPoint(.30f,.85f)),("south",new TownPoint(.45f,.02f))})
                {town.World.Camera.Jump(seam.Item2);town.World.Refresh();Canvas.ForceUpdateCanvases();Capture(output,"09-connected-path-"+seam.Item1);}
                Flow.ShowCharacterSelection();
                foreach(var character in CharacterCatalog.All){Flow.PreviewFavorite(character.Id);var bookScroll=Flow.GetComponentsInChildren<ScrollRect>().Single();bookScroll.verticalNormalizedPosition=.85f;Canvas.ForceUpdateCanvases();Capture(output,"10-book-"+character.Id);}
                Flow.GetComponentsInChildren<ScrollRect>().Single().verticalNormalizedPosition=.12f;Canvas.ForceUpdateCanvases();Capture(output,"10-book-discovery-details");
                Flow.ShowHome();Flow.ShowToday();Capture(output,"11-today-start");Flow.CloseToday();
                Flow.TalkToCharacter("lulu");Flow.Daily.RecordWalk();for(int n=0;n<3;n++)Flow.Daily.RecordMerge();Flow.ShowToday();Capture(output,"12-today-ready");Flow.ClaimDaily();Capture(output,"13-today-flower-collected");Flow.CloseToday();
                Flow.Game.State.activeQuestId="";Flow.Game.State.completedQuestIds.Clear();Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));Flow.ShowHome();Flow.GetComponentInChildren<TownLifeView>().Regions.ReturnToHero();Capture(output,"14-finale-still-has-daily-goals");
                // Major Polish: same native renderer, actual simulation and actual
                // pointer handlers. These are fixture captures, never Pixel claims.
                Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var side in new[]{("left",new TownPoint(.15f,.55f)),("right",new TownPoint(.79f,.55f))})
                {town.World.Model.Restore("momo",side.Item2);town.World.Camera.Jump(new TownPoint(.5f,.55f));town.World.Refresh();town.AdvanceFrame(1f/30);Capture(output,"15-wide-park-"+side.Item1);}
                town.World.Model.Restore("momo",new TownPoint(.30f,.46f));town.World.Model.Restore("lulu",new TownPoint(.68f,.46f));town.World.Model.TryGuidePoint(new TownPoint(.72f,.50f));town.World.Model.TryGuide("lulu","bench-left");
                for(int frame=0;frame<4;frame++){for(int step=0;step<30;step++)town.AdvanceFrame(1f/30);Capture(output,"16-residents-passing-"+frame.ToString("D2"));}
                town.World.Model.FastTravel(TownRegionId.Central);town.AdvanceFrame(1f/30);EnsurePortraitCamera();
                var controlled=town.Actors.Single(a=>a.Controlled);var drag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=777};
                drag.pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult{module=captureCanvas.GetComponent<GraphicRaycaster>()};
                controlled.OnPointerDown(drag);controlled.OnBeginDrag(drag);
                drag.position=RectTransformUtility.WorldToScreenPoint(captureCamera,town.transform.TransformPoint(town.World.Local(new TownPoint(.78f,.54f))));controlled.OnDrag(drag);
                for(int frame=0;frame<3;frame++){for(int step=0;step<20;step++)town.AdvanceFrame(1f/30);Capture(output,"17-finger-follow-"+frame.ToString("D2"));}
                controlled.OnPointerUp(drag);for(int step=0;step<15;step++)town.AdvanceFrame(1f/30);Capture(output,"17-finger-released");
                town.World.FastTravel(TownRegionId.West);Assert.That(town.GuideToPlace("east.flowers"),Is.True);
                var checkpoints=new[]{("north",1.40f),("north-join",.85f),("central",.48f),("south-join",.01f),("south",-.43f)};
                foreach(var checkpoint in checkpoints)
                {
                    for(int step=0;step<6000&&town.World.Model.Hero.IsWalking&&town.World.Model.Hero.Position.Y>checkpoint.Item2;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.Position.Y,Is.LessThanOrEqualTo(checkpoint.Item2+.01f),checkpoint.Item1+" must be reached by walking");
                    Capture(output,"18-continuous-walk-"+checkpoint.Item1);
                }
                Flow.ShowHome();Flow.TalkToCharacter("noah");Flow.TalkToCharacter("noah");Flow.TalkToCharacter("noah");Capture(output,"19-book-new-in-town");
                Flow.OpenCharacterBook("noah");Capture(output,"19-book-discovery-first-page");
                string[] art={"TownBackground","TownWestBackground","TownEastBackground","CharacterBodyAtlas","TownGrowthPropsAtlas","TownGardenSurface"};
                File.WriteAllLines(Path.Combine(output,"texture-memory.txt"),art.Select(key=>key+": "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Resources.Load<Texture2D>("Mofumachi/"+key))+" bytes"));
                File.WriteAllText(Path.Combine(output,"README.txt"),"Actual Unity UGUI RenderTexture captures, 720x1280. Isolated test-only save state; not Pixel 3a captures. 53 PNG: original36 plus17major-polish views. Level/item/book states are fixtures, not claims of completed gameplay. 15wide-floor positions;16passing frames;17actual pointer-handler following/release;18north-through-central-to-south continuous simulation with camera following;19book discovery. 30Hz simulation, not measured device FPS. Stable region/place/save IDs retained. Texture memory is Editor asset residency, not Pixel RAM. Pixel save retention, audio, 10minuteplay and subjective quality still require acceptance.\n");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
