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
        private sealed class CaptureMusicStore:IMusicPreferenceStore
        {private string value;public string Load()=>value;public void Save(string id)=>value=id;}
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
                Canvas.ForceUpdateCanvases();foreach(var grid in Flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();foreach(var hud in Flow.GetComponentsInChildren<TownStreetHud>())hud.Refresh();Canvas.ForceUpdateCanvases();
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
                foreach(int level in new[]{1,3,6})foreach(var district in TownDistrictLayout.All)
                {
                    Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();life.World.LookAt(district);yield return null;Canvas.ForceUpdateCanvases();
                    Capture(output,"04-town-lv"+level+"-"+district);
                }
                Flow.ShowHome();var town=Flow.GetComponentInChildren<TownLifeView>();town.Regions.ReturnToHero();town.GetComponent<TownStreetWalkView>().Arm();Capture(output,"05-street-selected");
                town.GuideToPlace("fountain");
                for(int frame=0;frame<8;frame++){for(int step=0;step<4;step++)town.AdvanceFrame(1f/30);Capture(output,"06-walking-"+frame.ToString("D2"));}
                Flow.ShowQuest();Capture(output,"07-quest-story-goal");Flow.ShowSettings();
                var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();scroll.verticalNormalizedPosition=.12f;Canvas.ForceUpdateCanvases();Capture(output,"08-music-selection");
                Flow.GoBack();Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                foreach(var seam in new[]{("west",new TownPoint(-.60f,0)),("east",new TownPoint(.60f,0)),("north",new TownPoint(0,1)),("south",new TownPoint(0,-1))})
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
                foreach(var side in new[]{("left",new TownPoint(-.30f,.30f)),("right",new TownPoint(.30f,.30f))})
                {town.World.Model.Restore("momo",side.Item2);town.World.Camera.Jump(TownWorldMap.Place("square"));town.World.Refresh();town.AdvanceFrame(1f/30);Capture(output,"15-wide-park-"+side.Item1);}
                town.World.Model.Restore("momo",new TownPoint(-.30f,-.20f));town.World.Model.Restore("lulu",new TownPoint(.30f,-.20f));town.World.Model.TryGuidePoint(new TownPoint(.40f,-.20f));town.World.Model.TryGuide("lulu","flowers");
                for(int frame=0;frame<4;frame++){for(int step=0;step<30;step++)town.AdvanceFrame(1f/30);Capture(output,"16-residents-passing-"+frame.ToString("D2"));}
                town.World.Model.FastTravel(TownRegionId.Central);town.AdvanceFrame(1f/30);EnsurePortraitCamera();
                var controlled=town.Actors.Single(a=>a.Controlled);var drag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=777};
                drag.pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult{module=captureCanvas.GetComponent<GraphicRaycaster>()};
                controlled.OnPointerDown(drag);controlled.OnBeginDrag(drag);
                drag.position=RectTransformUtility.WorldToScreenPoint(captureCamera,town.transform.TransformPoint(town.World.Local(new TownPoint(.40f,-.15f))));controlled.OnDrag(drag);
                for(int frame=0;frame<3;frame++){for(int step=0;step<20;step++)town.AdvanceFrame(1f/30);Capture(output,"17-finger-follow-"+frame.ToString("D2"));}
                controlled.OnPointerUp(drag);for(int step=0;step<15;step++)town.AdvanceFrame(1f/30);Capture(output,"17-finger-released");
                town.World.FastTravel(TownRegionId.West);Assert.That(town.GuideToPlace("east.flowers"),Is.True);
                var checkpoints=new[]{("west",-1.20f),("west-join",-.60f),("central",0f),("east-join",.60f),("east",1.20f)};
                foreach(var checkpoint in checkpoints)
                {
                    for(int step=0;step<6000&&town.World.Model.Hero.IsWalking&&town.World.Model.Hero.Position.X<checkpoint.Item2;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.Position.X,Is.GreaterThanOrEqualTo(checkpoint.Item2-.01f),checkpoint.Item1+" must be reached by walking");
                    Capture(output,"18-continuous-walk-"+checkpoint.Item1);
                }
                Flow.ShowHome();Flow.TalkToCharacter("noah");Flow.TalkToCharacter("noah");Flow.TalkToCharacter("noah");Capture(output,"19-book-new-in-town");
                Flow.OpenCharacterBook("noah");Capture(output,"19-book-discovery-first-page");
                // Three physical facilities: arrival, deliberate door tap and opening, not
                // repositioned actors or generated mockups of a future screen.
                foreach(string facilityId in new[]{"quest","shop","cafe"})
                {
                    Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(facilityId=="quest"?TownDistrictId.North:facilityId=="cafe"?TownDistrictId.West:TownDistrictId.East);
                    string prefix=facilityId=="quest"?"20-quest":facilityId=="cafe"?"21b-cafe":"21-shop";Capture(output,prefix+"-cabin");
                    var facility=town.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter(facilityId),Is.True);
                    for(int step=0;step<18;step++)town.AdvanceFrame(1f/30);Capture(output,prefix+"-walking");
                    for(int step=0;step<3000&&facility.Entry.Phase==TownEntryPhase.Walking;step++)town.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));Capture(output,prefix+"-arrived-door-prompt");
                    Assert.That(facility.RequestEnter(facilityId),Is.True);Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Opening));
                    for(int step=0;step<8;step++)town.AdvanceFrame(1f/30);Capture(output,prefix+"-door-opening");
                    for(int step=0;step<30&&Flow.CurrentScreen==ScreenId.Home&&!Flow.IsModalOpen;step++)town.AdvanceFrame(1f/30);
                    Assert.That(facilityId=="quest"?Flow.CurrentScreen==ScreenId.Quest:Flow.IsModalOpen,Is.True);Capture(output,prefix+"-inside");
                    if(Flow.IsModalOpen)Flow.GoBack();
                }
                // Keep editor preferences untouched while exercising the real
                // candidate selection and context playback code.
                AudioManager.Instance.UseMusicRolePreferences(new CaptureMusicStore(),new CaptureMusicStore(),new CaptureMusicStore());
                Flow.ShowSettings();Flow.GetComponentsInChildren<ScrollRect>().Single().verticalNormalizedPosition=.55f;Canvas.ForceUpdateCanvases();Capture(output,"22-title-candidates");
                Flow.GetComponentsInChildren<ScrollRect>().Single().verticalNormalizedPosition=.10f;Canvas.ForceUpdateCanvases();Capture(output,"22-merge-candidates");
                foreach(string song in new[]{"title-letter","title-garden","merge-picnic","merge-ribbon","merge-pop-promenade"})
                {
                    if(Flow.CurrentScreen!=ScreenId.Settings)Flow.ShowSettings();Flow.ListenMusicInScene(song);yield return null;
                    Assert.That(AudioManager.Instance.SelectedMusicId,Is.EqualTo(song));Assert.That(AudioManager.Instance.GetComponents<AudioSource>().Single(s=>s.loop).clip,Is.SameAs(Resources.Load<AudioClip>("Mofumachi/Audio/"+MusicCatalog.Find(song).Resource)));
                    Capture(output,"23-"+song);
                }
                Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownRegionId.Central);town.AdvanceFrame(1f/30);
                Capture(output,"24-home-unobstructed");Flow.OpenTownMenu();Capture(output,"24-menu-open");Flow.GoBack();Capture(output,"24-menu-return");
                var representative=town.Actors.Single(a=>a.Controlled);for(int step=0;step<30;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-idle");
                Assert.That(town.World.GuidePlace("bench-right"),Is.True);for(int step=0;step<8;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-step-a");
                for(int step=0;step<12;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-step-b");typeof(TownActor).GetMethod("Surprise",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(representative,null);for(int step=0;step<4;step++)town.AdvanceFrame(1f/30);Capture(output,"25-character-greeting");
                foreach(var depth in new[]{("behind",new TownPoint(0,.53f)),("in-front",new TownPoint(0,.06f))})
                {town.World.Model.Restore(representative.CharacterId,depth.Item2);town.World.ReturnToHero();town.AdvanceFrame(1f/30);Capture(output,"25-character-"+depth.Item1+"-fountain");}
                foreach(var district in new[]{TownDistrictId.North,TownDistrictId.South})
                {
                    town.World.FastTravel(TownDistrictId.Central);Assert.That(town.World.GuideDistrict(district),Is.True);
                    bool north=district==TownDistrictId.North;float direction=north?1:-1;
                    for(int step=0;step<5000&&town.World.Model.Hero.IsWalking&&town.World.Model.Hero.Position.Y*direction<.99f;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.Position.Y*direction,Is.GreaterThanOrEqualTo(.99f));Capture(output,"26-cardinal-"+district+"-join");
                    for(int step=0;step<5000&&town.World.Model.Hero.IsWalking;step++)town.AdvanceFrame(1f/30);
                    Assert.That(town.World.Model.Hero.District,Is.EqualTo(district));Assert.That(town.World.Model.Hero.IsWalking,Is.False);Capture(output,"26-cardinal-"+district+"-arrival");
                }
                Flow.ShowMerge();Flow.TapCell(0);Capture(output,"27-merge-finale-live-purpose");
                Flow.ShowHome();town=Flow.GetComponentInChildren<TownLifeView>();
                string visiting=town.World.Model.Residents.Where(r=>!r.Controlled).OrderByDescending(r=>TownWalkableArea.Distance(r.Position,town.World.Model.Hero.Position)).First().Id;
                int meetings=Flow.Daily.Data.Characters.Single(c=>c.Id==visiting).Meetings;
                Assert.That(Flow.VisitTownCharacter(visiting),Is.True);var meeting=town.GetComponent<TownMeetingJourney>();
                for(int step=0;step<12;step++){town.AdvanceFrame(1f/30);meeting.Advance(1f/30);}Capture(output,"28-meet-friend-walking");
                for(int step=0;step<6000&&meeting.IsActive;step++){town.AdvanceFrame(1f/30);meeting.Advance(1f/30);}
                Assert.That(meeting.IsActive,Is.False);Assert.That(Flow.Daily.Data.Characters.Single(c=>c.Id==visiting).Meetings,Is.EqualTo(meetings));Capture(output,"28-meet-friend-arrived");
                // Use the real generate/drag handlers in this isolated save. No
                // player's sparse board is filled to make a prettier screenshot.
                Flow.ShowMerge();Flow.GenerateFromFrame("tea");yield return null;EnsurePortraitCamera();Canvas.ForceUpdateCanvases();captureCamera.Render();
                var mergeSource=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell 0");
                int targetCell=Flow.Game.State.mergeBoard.First(b=>b.cellIndex!=0&&b.itemId=="tea"&&b.level==1).cellIndex;
                var mergeTarget=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell "+targetCell);
                Vector2 PointerAt(Transform item)=>RectTransformUtility.WorldToScreenPoint(captureCamera,item.TransformPoint(((RectTransform)item).rect.center));
                var mergeDrag=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=778,position=PointerAt(mergeSource.transform)};
                mergeDrag.pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult{module=captureCanvas.GetComponent<GraphicRaycaster>()};
                mergeSource.OnBeginDrag(mergeDrag);Capture(output,"29-merge-drag-lift");
                mergeDrag.position=PointerAt(mergeTarget.transform);mergeSource.OnDrag(mergeDrag);Capture(output,"29-merge-drag-match");mergeSource.OnEndDrag(mergeDrag);
                yield return new WaitForSecondsRealtime(.8f);Assert.That(Flow.Game.Board.At(targetCell).level,Is.EqualTo(2));Capture(output,"29-merge-landed");
                string[] art={"TownBackground","TownWestBackground","TownEastBackground","CharacterBodyAtlas","TownGrowthPropsAtlas","TownGardenSurface","TownSeamlessMap","TownStreetPropsAtlas","TownLandmarksAtlas","TownNatureAtlas","TownVillageDetailsAtlas"};
                File.WriteAllLines(Path.Combine(output,"texture-memory.txt"),art.Select(key=>key+": "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Resources.Load<Texture2D>("Mofumachi/"+key))+" bytes"));
                File.WriteAllText(Path.Combine(output,"README.txt"),"Actual Unity UGUI RenderTexture captures, 720x1280. Isolated test-only save state; not Pixel 3a captures. 102 PNG: integrated village scenery/compact street HUD/real resident visits/explicit facility doors/flat Merge interaction bundle. Level/item/book states are fixtures, not claims of completed gameplay. 15wide-floor positions;16passing frames;17actual pointer-handler following/release;18west-through-central-to-east continuous simulation with camera following;19book discovery. 20/21 actual facility walking, opening and entry;22 candidate UI;23 actual role playback screens including POP;24 collapsed/open/returned menu;25 resident idle/walking/reaction;26 continuous North/South joins and arrivals;27 finale live daily-purpose Merge;28 real resident approach and arrival without invented conversation;29 real drag lift, matching and landed merge. Three growth stages in each of five independent plazas, four shared boundary views. Building/ground sprite layers are product code, not a stretched world painting. 30Hz simulation, not measured device FPS. Stable region/place/save IDs retained. Texture memory is Editor asset residency, not Pixel RAM. Pixel save retention, audio, 10minuteplay and subjective quality still require acceptance.\n");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
