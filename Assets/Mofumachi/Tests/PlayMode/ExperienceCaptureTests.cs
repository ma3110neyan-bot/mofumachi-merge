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
        // Real production/delivery progression in this fixture's temporary disk
        // save. Never assigns a growth level or completion/reward collection.
        [UnityTest] public IEnumerator DeliveriesGrowFiveDistrictsAndPersistAfterSceneReload()
        {
            string output=OutputDirectory();yield return EnterHome();int totalCoins=0;
            var proof=new System.Text.StringBuilder("{\"kind\":\"isolated-real-delivery-growth-and-reload\",\"pixel_verified\":false,\"adopted\":false,\"transitions\":[");bool first=true;
            string[] VisibleDecor(TownDistrictId district)
            {
                // Layout-only inspection uses a world-space canvas. Release the
                // optional portrait camera so the next capture configures its
                // own render/event projection again after a scene/layout change.
                ReleasePortraitCamera();
                var town=FrameTownPortrait();town.StartInteraction();town.World.Freeze();town.World.LookAt(district);
                var center=TownDistrictLayout.Center(district);town.World.Camera.Jump(new TownPoint(center.X,center.Y+.20f));town.World.Refresh();Canvas.ForceUpdateCanvases();
                var scenery=town.GetComponent<TownSceneryView>();
                return scenery.Pieces.Where(p=>p.Rect.gameObject.activeInHierarchy).Select(p=>FormattableString.Invariant($"scenery:{p.Placement.Kind}:{p.Placement.Feet.X:F4}:{p.Placement.Feet.Y:F4}:{p.Placement.Level}:{p.Placement.Width:F4}"))
                    .Concat(town.GetComponentsInChildren<TownGrowthPropGraphic>().Select(p=>FormattableString.Invariant($"growth:{p.Prop}:{p.Region}:{((RectTransform)p.transform).anchoredPosition.x:F3}:{((RectTransform)p.transform).anchoredPosition.y:F3}")))
                    .OrderBy(s=>s,StringComparer.Ordinal).ToArray();
            }
            foreach(var quest in QuestCatalog.All)
            {
                int previous=Flow.Game.State.townGrowthLevel;bool check=previous==1||previous==2||previous==5;
                Flow.ShowHome();var before=check?TownDistrictLayout.All.Select(VisibleDecor).ToArray():null;
                Flow.ShowQuest();Assert.That(Flow.AcceptQuest(),Is.True);Assert.That(Flow.Game.Quests.ActiveQuest.Id,Is.EqualTo(quest.Id));
                foreach(var requirement in quest.Requirements)
                {
                    int guard=0;
                    while(Flow.Game.State.mergeBoard.Count(i=>i.itemId==requirement.ItemId&&i.level==requirement.Level)<requirement.Count)
                    {
                        Assert.That(++guard,Is.LessThan(40),"Generate/merge must progress without synthetic inventory");
                        var pair=Flow.Game.State.mergeBoard.Where(i=>i.itemId==requirement.ItemId&&i.level<requirement.Level).GroupBy(i=>i.level).FirstOrDefault(g=>g.Count()>=2);
                        if(pair==null){int count=Flow.Game.State.mergeBoard.Count;Flow.GenerateFromFrame(requirement.ItemId);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(count+1));}
                        else{var items=pair.Take(2).ToArray();Assert.That(Flow.DropItem(items[0].cellIndex,items[1].cellIndex),Is.True);yield return new WaitForSecondsRealtime(.30f);}
                    }
                }
                Assert.That(Flow.Deliver().Success,Is.True);totalCoins+=quest.Coins;
                Assert.That(Flow.LastDelivery.PreviousTownLevel,Is.EqualTo(previous));Assert.That(Flow.LastDelivery.CurrentTownLevel,Is.EqualTo(previous+1));
                Assert.That(Store.Load().State.townGrowthLevel,Is.EqualTo(previous+1),"Delivery committed the level before any growth presentation");
                yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Growth));
                if(check)
                {
                    foreach(var district in TownDistrictLayout.All)
                    {
                        var button=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Growth region "+district);button.onClick.Invoke();Canvas.ForceUpdateCanvases();
                        var previews=Flow.GetComponentsInChildren<TownGrowthView>();Assert.That(previews.Length,Is.EqualTo(2));
                        Assert.That(previews[0].VisibleStage,Is.EqualTo(previous));Assert.That(previews[1].VisibleStage,Is.EqualTo(previous+1));
                        Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Growth after label").text,Is.EqualTo(TownGrowthPresentation.ChangeSummary(district,previous,previous+1)));
                        Assert.That(previews[1].IsHighlighting,Is.True,"Keep the short existing new-decoration highlight");
                    }
                    Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Growth region "+(previous==5?TownDistrictId.South:previous==2?TownDistrictId.North:TownDistrictId.Central)).onClick.Invoke();
                    Capture(output,"32-growth-"+previous+"-to-"+(previous+1));
                }
                Flow.ShowHome();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
                if(!check)continue;
                var after=TownDistrictLayout.All.Select(VisibleDecor).ToArray();
                for(int i=0;i<TownDistrictLayout.All.Count;i++)
                {
                    var district=TownDistrictLayout.All[i];bool nextAtThree=previous==1&&(district==TownDistrictId.North||district==TownDistrictId.South);
                    Assert.That(after[i].SequenceEqual(before[i]),Is.EqualTo(nextAtThree),district+" must reflect authored additions, not an artificial level sample");
                }
                Assert.That(Flow.Game.State.coins,Is.EqualTo(totalCoins));int completed=Flow.Game.State.completedQuestIds.Count;
                Flow.SendMessage("OnApplicationPause",true);ReleasePortraitCamera();
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("TitleScene");yield return null;Flow.Initialize(Store);Flow.BeginGame();yield return null;yield return null;
                Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(previous+1));
                Assert.That(Flow.Game.State.coins,Is.EqualTo(totalCoins));Assert.That(Flow.Game.State.completedQuestIds.Count,Is.EqualTo(completed));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(completed));
                var reloaded=TownDistrictLayout.All.Select(VisibleDecor).ToArray();
                for(int i=0;i<after.Length;i++)Assert.That(reloaded[i],Is.EqualTo(after[i]),TownDistrictLayout.All[i]+" decorations persisted without applying a test level");
                Flow.ShowQuest();int replayCoins=Flow.Game.State.coins;var replayRewards=Flow.Game.State.claimedRewardIds.ToArray();
                Assert.That(Flow.Deliver().Success,Is.False,"A real delivery-enabled screen must reject the completed reward after restart");
                Assert.That(Flow.Game.State.coins,Is.EqualTo(replayCoins));Assert.That(Flow.Game.State.claimedRewardIds,Is.EqualTo(replayRewards));
                Flow.ShowHome();VisibleDecor(TownDistrictId.South);
                Capture(output,"32-growth-"+(previous+1)+"-reloaded");
                if(!first)proof.Append(',');first=false;proof.Append("{\"from\":"+previous+",\"to\":"+(previous+1)+",\"coins\":"+totalCoins+",\"districts_checked\":5,\"reload_matched\":true}");
            }
            Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(6));Assert.That(Flow.Game.State.coins,Is.EqualTo(330));
            proof.Append("]}");if(output!=null){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"town-growth-progression.json"),proof.ToString());}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FountainWaterBeforeAfterPreservesFieldAndCharacterDepth()
        {
            string output=OutputDirectory();yield return EnterHome();Flow.Game.State.townGrowthLevel=6;Flow.ShowHome();yield return null;
            var town=Flow.GetComponentInChildren<TownLifeView>();town.StartInteraction();
            var water=Flow.GetComponentInChildren<TownFountainWaterGraphic>();var originalWater=Flow.GetComponentInChildren<TownAmbientGraphic>();var fountain=Flow.GetComponentInChildren<TownFountainGraphic>();
            Assert.That(water.transform.parent,Is.SameAs(fountain.transform));Assert.That(water.raycastTarget,Is.False);
            var pairs=new System.Text.StringBuilder();bool first=true;
            foreach(var view in new[]{("overview",new TownPoint(0,.12f),new TownPoint(0,.64f)),("front",new TownPoint(0,.12f),new TownPoint(0,.32f)),("behind",new TownPoint(0,.441f),new TownPoint(0,.55f))})
            {
                Assert.That(town.World.GuidePoint(view.Item2),Is.True);int steps=0;
                while(town.World.Model.Hero.IsWalking&&steps++<6000){town.AdvanceFrame(1f/30);if(steps%240==0)yield return null;}
                Assert.That(town.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(town.World.Model.Hero.Position,view.Item2),Is.LessThan(.002f));
                town.World.Freeze();town.World.Camera.Jump(view.Item3);town.World.Refresh();
                // Warm font/canvas resources before the paired pixel comparison.
                var warm=Portrait();UnityEngine.Object.DestroyImmediate(warm);
                string state=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;var positions=town.World.Model.Residents.Select(r=>r.Position).ToArray();var camera=town.World.Camera.Center;
                float originalPhase=originalWater.Phase,newPhase=water.Phase;Texture2D before=null,after=null;
                int changed=0,outside=0;
                try
                {
                    water.enabled=false;before=Portrait();Capture(output,"33-fountain-"+view.Item1+"-before");
                    water.enabled=true;after=Portrait();Capture(output,"33-fountain-"+view.Item1+"-after");
                    Vector2 Project(TownPoint point)=>captureCamera.WorldToScreenPoint(town.transform.TransformPoint(town.World.Local(point)));
                    var low=Project(new TownPoint(-TownCentralMasterLayout.FountainWidth/2,TownCentralMasterLayout.FountainFeetY));
                    var high=Project(new TownPoint(TownCentralMasterLayout.FountainWidth/2,TownCentralMasterLayout.FountainFeetY+TownSceneryLayout.FountainHeight));
                    var allowed=Rect.MinMaxRect(low.x-3,low.y-3,high.x+3,high.y+3);var a=before.GetPixels32();var b=after.GetPixels32();
                    for(int i=0;i<a.Length;i++)
                    {
                        if(a[i].r==b[i].r&&a[i].g==b[i].g&&a[i].b==b[i].b&&a[i].a==b[i].a)continue;
                        changed++;if(!allowed.Contains(new Vector2(i%720,i/720)))outside++;
                    }
                    Assert.That(changed,Is.GreaterThan(0),"The new water must be visible in actual native pixels");
                    Assert.That(outside,Is.Zero,"Adding water must not alter the field/UI/characters outside the fountain projection");
                    Assert.That(water.canvasRenderer.GetMesh().vertexCount,Is.EqualTo(320));
                    Assert.That(originalWater.Phase,Is.EqualTo(originalPhase));Assert.That(water.Phase,Is.EqualTo(newPhase));
                    Assert.That(town.World.Model.Residents.Select(r=>r.Position).ToArray(),Is.EqualTo(positions));Assert.That(town.World.Camera.Center,Is.EqualTo(camera));
                    Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));Assert.That(Store.Writes,Is.EqualTo(writes));
                    var hero=town.Actors.Single(a=>a.Controlled);
                    Assert.That(hero.transform.GetSiblingIndex()>fountain.transform.GetSiblingIndex(),Is.EqualTo(view.Item2.Y<TownCentralMasterLayout.FountainCenterY),"The entire new water layer must inherit the original fountain depth");
                }
                finally {water.enabled=true;if(before!=null)UnityEngine.Object.DestroyImmediate(before);if(after!=null)UnityEngine.Object.DestroyImmediate(after);}
                if(!first)pairs.Append(',');first=false;
                pairs.Append("{\"view\":\""+view.Item1+"\",\"changed_pixels\":"+changed+",\"outside_fountain_pixels\":"+outside+",\"state_unchanged\":true}");
            }
            var phases=new System.Text.StringBuilder();Vector3[] previous=null;
            for(int frame=0;frame<4;frame++)
            {
                if(frame>0)for(int tick=0;tick<3;tick++){water.AdvanceWater(.1f);originalWater.AdvanceWater(.1f);}
                Capture(output,"33-fountain-phase-"+frame.ToString("00"));
                var mesh=water.canvasRenderer.GetMesh().vertices;
                if(previous!=null)Assert.That(mesh.SequenceEqual(previous),Is.False,"Time samples must animate actual water geometry");previous=mesh;
                if(frame>0)phases.Append(',');phases.Append(water.Phase.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
            if(output!=null)
            {
                File.WriteAllText(Path.Combine(output,"fountain-water-comparison.json"),"{\"kind\":\"same-state-native-additive-water-comparison\",\"before\":\"new layer OFF; original fountain and original water retained\",\"after\":\"product code\",\"pairs\":["+pairs+"],\"phases\":["+phases+"],\"added_vertices\":320,\"max_update_hz\":30,\"pixel_verified\":false,\"adopted\":false}");
                var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Fountain water Before After</title><style>body{background:#fff7ef;color:#50343b;font-family:sans-serif}section{display:flex;gap:12px}img{width:360px;max-width:46vw}figure{margin:8px}</style><h1>Fountain water / same-state Unity comparison</h1><p>Before: only the new water layer OFF. Original fountain, original ripples, field, characters and camera retained. After: product code. Not a previous APK reconstruction or Pixel capture.</p>");
                foreach(string view in new[]{"overview","front","behind"})html.Append("<h2>"+view+"</h2><section><figure>Before<br><img src='33-fountain-"+view+"-before.png'></figure><figure>After<br><img src='33-fountain-"+view+"-after.png'></figure></section>");
                html.Append("<h2>Four real animation samples</h2><section>");for(int frame=0;frame<4;frame++)html.Append("<figure>"+frame+"<br><img src='33-fountain-phase-"+frame.ToString("00")+".png'></figure>");
                html.Append("</section><p>Pixel visibility, live depth transitions, temperature and performance acceptance pending.</p>");File.WriteAllText(Path.Combine(output,"fountain-water-before-after.html"),html.ToString());
            }
            LogAssert.NoUnexpectedReceived();
        }
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
                Canvas.ForceUpdateCanvases();foreach(var grid in Flow.GetComponentsInChildren<MergeGridLayout>())grid.ResizeNow();foreach(var hud in Flow.GetComponentsInChildren<TownStreetHud>())hud.Refresh();
                // Manual simulation deliberately avoids yielding a frame. Run the
                // existing location-only LateUpdate at the same simulated instant.
                foreach(var region in Flow.GetComponentsInChildren<TownRegionControls>())region.SendMessage("LateUpdate",SendMessageOptions.RequireReceiver);
                foreach(var town in Flow.GetComponentsInChildren<TownLifeView>())
                {
                    town.World.Refresh();town.RefreshWorldPresentation();
                    foreach(var actor in town.Actors.Where(a=>a.RegionVisible))
                        Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,town.World.Local(actor.WorldPosition)),Is.LessThan(.01f),"Direct capture must not mix the current scenery with stale actor positions");
                }
                Canvas.ForceUpdateCanvases();
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
        private void Capture(string directory,string name,TownDistrictId? expectedDistrict=null)
        {
            int writes=Store.Writes;string state=StateCodec.Encode(Flow.Game.State);var image=Portrait();
            try
            {
                var colors=Enumerable.Range(0,12).SelectMany(x=>Enumerable.Range(0,20).Select(y=>(Color32)image.GetPixel(30+x*55,30+y*60))).Distinct().Count();
                Assert.That(colors,Is.GreaterThan(12),"Portrait rendering must contain more than a clear background");
                if(expectedDistrict.HasValue)Assert.That(Flow.GetComponentsInChildren<Text>().Single(label=>label.name=="Town region name").text,Is.EqualTo(TownDistrictLayout.Name(expectedDistrict.Value)),"Cardinal arrival capture must show the current district ribbon");
                if(directory!=null)
                {
                    Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());
                    foreach(var town in Flow.GetComponentsInChildren<TownLifeView>())
                    {
                        var camera=town.World.Camera.Center;
                        string actors=string.Join(";",town.Actors.Select(a=>FormattableString.Invariant($"{a.CharacterId}@world({a.WorldPosition.X:F4},{a.WorldPosition.Y:F4})/view({((RectTransform)a.transform).anchoredPosition.x:F2},{((RectTransform)a.transform).anchoredPosition.y:F2})/visible={a.RegionVisible}")));
                        CaptureWorldTrace.Append(directory,FormattableString.Invariant($"MOFUMACHI_CAPTURE_WORLD {name} hero={town.World.Model.Hero.Id} camera=({camera.X:F4},{camera.Y:F4}) {actors}"));
                    }
                }
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(state));
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        [Serializable] private sealed class NorthRoutePoint {public float x,y;}
        [Serializable] private sealed class NorthRoute {public string id;public NorthRoutePoint[] points;}
        [Serializable] private sealed class NorthRouteEvidence
        {
            public string image="04-town-lv1-North.png",image_sha256,kind="navigation-route-overlay-not-gameplay-trace",origin="top-left";
            public int width=720,height=1280;public NorthRoute[] routes;
        }
        private static readonly string[] ReviewRouteIds={"route-1","route-2","route-3","route-4","route-5","route-6"};
        private void WriteNorthRouteEvidence(string directory,TownLifeView town)
        {
            WriteRouteEvidence(directory,town,"North",TownNorthMasterLayout.Circulation,new[]{"cottage-left","cottage-right","left-waiting","right-waiting","mosaic-cross","mosaic-loop"});
        }
        private void WriteRouteEvidence(string directory,TownLifeView town,string district,System.Collections.Generic.IReadOnlyList<TownPoint[]> links,string[] ids)
        {
            // MOFUMACHI_NORTH_DELIVERY_QUALITY: actual viewport projections from
            // the same Lv1 native capture, never hand-positioned route arrows.
            if(directory==null)return;
            var navigation=new TownAreaNavigation(town.World.Model.Area);
            NorthRoutePoint Project(TownPoint point)
            {
                var local=town.World.Local(point);var screen=captureCamera.WorldToScreenPoint(town.transform.TransformPoint(local));
                return new NorthRoutePoint{x=screen.x,y=1280-screen.y};
            }
            var evidence=new NorthRouteEvidence{image="04-town-lv1-"+district+".png",routes=links.Select((link,index)=>
            {
                var points=new System.Collections.Generic.List<TownPoint>{link[0]};
                foreach(var destination in link.Skip(1))
                {
                    var route=navigation.FindRoute(points[points.Count-1],destination);Assert.That(route,Is.Not.Empty,"Every annotated pair must exist on the real walkable area");
                    foreach(var point in route){Assert.That(town.World.Model.Area.CanTravel(points[points.Count-1],point),Is.True);points.Add(point);}
                }
                return new NorthRoute{id=ids[index],points=points.Select(Project).ToArray()};
            }).ToArray()};
            using(var sha=System.Security.Cryptography.SHA256.Create())
                evidence.image_sha256=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(directory,evidence.image)))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(Path.Combine(directory,district.ToLowerInvariant()+"-route-projection.json"),JsonUtility.ToJson(evidence,true));
        }
        private IEnumerator CaptureBoundaryComparisons(string output)
        {
            Flow.Game.State.townGrowthLevel=6;Flow.ShowHome();yield return null;
            var town=Flow.GetComponentInChildren<TownLifeView>();town.StartInteraction();town.World.Freeze();
            var curve=Flow.GetComponentInChildren<TownBoundaryCurveGraphic>();
            Assert.That(curve,Is.Not.Null);bool curveEnabled=curve.enabled;
            Assert.That(Flow.GetComponentInChildren<TownSouthEntryBlendGraphic>().enabled,Is.True);
            Assert.That(Flow.GetComponentInChildren<TownBoundaryGardenGraphic>().enabled,Is.True);
            var html=new System.Text.StringBuilder("<!doctype html><meta charset='utf-8'><title>Street boundary comparison</title><style>body{font-family:sans-serif;background:#fff7ef;color:#50343b}section{display:flex;gap:12px}figure{margin:8px}img{width:360px;max-width:45vw}h2{clear:both}</style><h1>Street boundary / same-state Unity comparison</h1><p>Before disables only the curved band in the current field-stability runtime. Existing planting, entry bands and the depth/camera/navigation fixes stay active. This is a boundary-only comparison, NOT a complete old renderer or previous APK capture. After uses product code. Neither is a Pixel capture or acceptance.</p>");
            try
            {
                foreach(var view in new[]{
                    ("west-upper",new TownPoint(-1.05f,.80f)),("east-upper",new TownPoint(1.05f,.80f)),
                    ("west-lower",new TownPoint(-1.05f,-.85f)),("east-lower",new TownPoint(1.05f,-.85f)),
                    ("north-left",new TownPoint(-.55f,.92f)),("north-right",new TownPoint(.55f,.92f)),
                    ("south-left",new TownPoint(-.60f,-.90f)),("south-right",new TownPoint(.60f,-.90f)),
                    ("south-entry",new TownPoint(0,-1)),("west-join",new TownPoint(-.60f,0)),("east-join",new TownPoint(.60f,0))})
                {
                    town.World.LookAt(TownDistrictId.Central);town.World.Camera.Jump(view.Item2);town.World.Refresh();
                    var positions=town.World.Model.Residents.Select(r=>r.Position).ToArray();
                    curve.enabled=false;
                    string before="30-boundary-"+view.Item1+"-before-reconstructed",after="30-boundary-"+view.Item1+"-after";
                    Capture(output,before);curve.enabled=true;Capture(output,after);
                    Assert.That(town.World.Model.Residents.Select(r=>r.Position).ToArray(),Is.EqualTo(positions),"Only rendering layers may change within a comparison pair");
                    html.Append("<h2>"+view.Item1+"</h2><section><figure><figcaption>Before / frozen renderer reconstruction</figcaption><img loading='lazy' src='"+before+".png'></figure><figure><figcaption>After / product candidate</figcaption><img loading='lazy' src='"+after+".png'></figure></section>");
                }
                html.Append("<h1>Five areas / candidate overview</h1>");
                foreach(int level in new[]{1,3,6})foreach(var district in TownDistrictLayout.All)
                    html.Append("<figure><figcaption>"+district+" Lv."+level+" / After</figcaption><img loading='lazy' src='04-town-lv"+level+"-"+district+".png'></figure>");
                File.WriteAllText(Path.Combine(output,"street-boundary-before-after.html"),html.ToString());
                File.WriteAllText(Path.Combine(output,"street-boundary-comparison.json"),"{\"kind\":\"same-state-native-renderer-comparison\",\"before\":\"current field-stability runtime with curved band off; not historicalAPK\",\"pairs\":11,\"after\":\"product code\",\"pixel_verified\":false,\"adopted\":false}");
            }
            finally {curve.enabled=curveEnabled;}
        }
        private IEnumerator CaptureFieldStability(string output)
        {
            Flow.Game.State.townGrowthLevel=6;Flow.Game.State.favoriteCharacterId="miel";Flow.ShowHome();yield return null;EnsurePortraitCamera();
            var town=Flow.GetComponentInChildren<TownLifeView>();var hero=town.Actors.Single(a=>a.Controlled);
            IEnumerator Walk(TownPoint target)
            {
                Assert.That(town.World.GuidePoint(target),Is.True);int steps=0;
                while(town.World.Model.Hero.IsWalking&&steps<6000){town.AdvanceFrame(1f/30);steps++;if(steps%240==0)yield return null;}
                Assert.That(town.World.Model.Hero.IsWalking,Is.False,$"Stalled at {hero.WorldPosition.X},{hero.WorldPosition.Y}");
                Assert.That(TownWalkableArea.Distance(hero.WorldPosition,target),Is.LessThan(.002f),"Walking arrival, never Restore/FastTravel rescue");
            }
            foreach(var view in new[]{("front",new TownPoint(0,.12f)),("behind",new TownPoint(0,.441f))})
            {
                yield return Walk(view.Item2);for(int settle=0;settle<45;settle++)town.AdvanceFrame(1f/30);
                town.RefreshWorldPresentation();var fountain=town.GetComponentInChildren<TownFountainGraphic>();
                Assert.That(hero.transform.GetSiblingIndex()>fountain.transform.GetSiblingIndex(),Is.EqualTo(view.Item1=="front"));
                if(view.Item1=="behind")
                {
                    Assert.That(town.World.Model.Area.Contains(hero.WorldPosition),Is.True,"Far-side proof must stand outside the real fountain obstacle");
                    var body=RectTransformUtility.CalculateRelativeRectTransformBounds((RectTransform)town.transform,hero.VisualRoot);
                    // The fountain Graphic's RectTransform is the whole field;
                    // use its actual mesh projection, not that oversized rect.
                    float waterTop=town.World.Local(new TownPoint(0,fountain.Feet.Y+TownSceneryLayout.FountainHeight)).y;
                    Assert.That(body.min.y,Is.LessThan(waterTop),"Far-side evidence must overlap the fountain mesh bounds, not stand well above it");
                }
                Capture(output,"31-field-fountain-"+view.Item1);
            }
            var entry=TownWorldMap.Place("east.entry");yield return Walk(entry);yield return Walk(new TownPoint(1.5999999f,.45000005f));Capture(output,"31-field-east-corner-out");
            yield return Walk(entry);Capture(output,"31-field-east-corner-return");
            foreach(var edge in new[]{("East",new TownPoint(1.8f,.68f)),("West",new TownPoint(-1.8f,.68f)),("North",new TownPoint(.43f,2.98f)),("South",new TownPoint(.43f,-2.98f)),("Central",new TownPoint(0,.97f))})
            {
                yield return Walk(edge.Item2);Capture(output,"31-field-"+edge.Item1+"-edge");
                yield return Walk(TownWorldMap.Place("square"));Capture(output,"31-field-"+edge.Item1+"-return");
            }
            File.WriteAllText(Path.Combine(output,"field-stability.json"),"{\"kind\":\"native-walking-and-depth-fixture\",\"edge_return_pairs\":5,\"east_corner_return\":true,\"fountain_front_and_behind\":true,\"teleport_rescue\":false,\"pixel_verified\":false,\"adopted\":false}");
        }
        [UnityTest] public IEnumerator PortraitScreensRenderFromIsolatedStateWithoutWritingProgress()
        {
            yield return EnterHome();string output=OutputDirectory();
            bool fiveAreaReview=Environment.GetCommandLineArgs().Contains("-mofumachiFiveAreaReview");
            bool southReview=fiveAreaReview||Environment.GetCommandLineArgs().Contains("-mofumachiSouthReview");
            bool northReview=southReview||Environment.GetCommandLineArgs().Contains("-mofumachiNorthReview");
            bool eastReview=northReview||Environment.GetCommandLineArgs().Contains("-mofumachiEastReview");
            bool westReview=eastReview||Environment.GetCommandLineArgs().Contains("-mofumachiWestReview");
            if(Environment.GetCommandLineArgs().Contains("-mofumachiCentralReview")||westReview)
            {
                // Configure portrait before building the first town, rather than
                // changing its projection for the first time inside Capture().
                EnsurePortraitCamera();Canvas.ForceUpdateCanvases();yield return null;
                foreach(int level in new[]{1,3,6})
                {
                    Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                    var life=Flow.GetComponentInChildren<TownLifeView>();life.World.LookAt(TownDistrictId.Central);life.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                    foreach(var piece in life.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                        Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,life.World.Local(piece.Placement.Feet,(RectTransform)life.transform)),Is.LessThan(.01f),"First portrait must not retain scenery coordinates from the Editor viewport");
                    foreach(var actor in life.Actors.Where(a=>a.RegionVisible))
                        Assert.That(Vector2.Distance(((RectTransform)actor.transform).anchoredPosition,life.World.Local(actor.WorldPosition)),Is.LessThan(.01f),"Portrait actor and ground must use the same settled projection");
                    var guest=life.Actors.Single(a=>!a.Controlled&&TownDistrictLayout.At(a.WorldPosition)==TownDistrictId.Central);
                    var name=guest.transform.Find("Town actor name panel");Assert.That(name.gameObject.activeInHierarchy,Is.True);
                    var plate=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,name);
                    foreach(var piece in life.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf&&p.Placement.Feet.Y<guest.WorldPosition.Y))
                        Assert.That(plate.Intersects(RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,piece.Rect)),Is.False,"Arrival nameplate must not be covered by foreground scenery");
                    if(level==6)
                    {
                        var bunting=Flow.GetComponentsInChildren<TownGrowthPropGraphic>().Single(p=>p.Prop==TownGrowthProp.StreetBunting&&p.Region==TownRegionId.Central);
                        var ribbon=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town region name");
                        var festive=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,bunting.transform);
                        var caption=RectTransformUtility.CalculateRelativeRectTransformBounds(life.transform,ribbon.transform);
                        Assert.That(festive.max.y,Is.LessThan(caption.min.y),"The festival display must fit below the location ribbon");
                    }
                    Capture(output,"04-town-lv"+level+"-Central",TownDistrictId.Central);
                    if(fiveAreaReview&&level==1)WriteRouteEvidence(output,life,"Central",TownReviewRoutes.For(TownDistrictId.Central),ReviewRouteIds);
                }
                if(westReview)
                {
                    string westOutput=output==null?null:Path.Combine(output,"west");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                        var town=Flow.GetComponentInChildren<TownLifeView>();town.World.LookAt(TownDistrictId.West);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var piece in town.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                            Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,town.World.Local(piece.Placement.Feet,(RectTransform)town.transform)),Is.LessThan(.01f));
                        Capture(westOutput,"04-town-lv"+level+"-West",TownDistrictId.West);
                        if(fiveAreaReview&&level==1)WriteRouteEvidence(westOutput,town,"West",TownReviewRoutes.For(TownDistrictId.West),ReviewRouteIds);
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var west=Flow.GetComponentInChildren<TownLifeView>();
                    west.World.LookAt(TownDistrictId.West);west.World.Camera.Jump(new TownPoint(-.60f,0));west.World.Refresh();Capture(westOutput,"west-connection");
                    west.World.FastTravel(TownDistrictId.Central);Assert.That(west.World.GuideDistrict(TownDistrictId.West),Is.True);
                    for(int i=0;i<3000&&west.World.Model.Hero.Position.X>-.85f;i++)west.AdvanceFrame(1f/30);
                    Assert.That(west.World.Model.Hero.Position.X,Is.LessThanOrEqualTo(-.85f));
                    Assert.That(west.IsSpeaking,Is.False,"Walking must stay visible without unsolicited dialogue");Capture(westOutput,"west-walking");
                    for(int i=0;i<3000&&west.World.Model.Hero.IsWalking;i++)west.AdvanceFrame(1f/30);
                    Assert.That(west.World.Model.Hero.IsWalking,Is.False);
                    var facility=west.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter("cafe"),Is.True);
                    for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)west.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                    Assert.That(west.IsSpeaking,Is.False,"The cafe arrival cue must not be covered by dialogue");
                    Assert.That(west.transform.Find("Facility entrance cafe/Facility sign cafe").gameObject.activeInHierarchy,Is.True);
                    Capture(westOutput,"west-door-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Walking to the cafe does not automatically enter");
                }
                if(eastReview)
                {
                    string eastOutput=output==null?null:Path.Combine(output,"east");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                        var town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownDistrictId.Central);town.World.LookAt(TownDistrictId.East);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var piece in town.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                            Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,town.World.Local(piece.Placement.Feet,(RectTransform)town.transform)),Is.LessThan(.01f));
                        EastMasterReviewTests.AssertNameClearOfBenches(town,town.Actors.Single(a=>a.CharacterId=="fio"));
                        Capture(eastOutput,"04-town-lv"+level+"-East",TownDistrictId.East);
                        if(fiveAreaReview&&level==1)WriteRouteEvidence(eastOutput,town,"East",TownReviewRoutes.For(TownDistrictId.East),ReviewRouteIds);
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var east=Flow.GetComponentInChildren<TownLifeView>();
                    east.World.LookAt(TownDistrictId.East);east.World.Camera.Jump(new TownPoint(.60f,0));east.World.Refresh();Capture(eastOutput,"east-connection");
                    east.World.FastTravel(TownDistrictId.Central);Assert.That(east.World.GuideDistrict(TownDistrictId.East),Is.True);
                    for(int i=0;i<3000&&east.World.Model.Hero.Position.X<.85f;i++)east.AdvanceFrame(1f/30);
                    Assert.That(east.World.Model.Hero.Position.X,Is.GreaterThanOrEqualTo(.85f));
                    Assert.That(east.IsSpeaking,Is.False,"Walking must stay visible without unsolicited dialogue");Capture(eastOutput,"east-walking");
                    for(int i=0;i<3000&&east.World.Model.Hero.IsWalking;i++)east.AdvanceFrame(1f/30);
                    Assert.That(east.World.Model.Hero.IsWalking,Is.False);
                    var facility=east.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter("shop"),Is.True);
                    for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)east.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                    Assert.That(east.IsSpeaking,Is.False,"The florist arrival cue must not be covered by dialogue");
                    Assert.That(east.transform.Find("Facility entrance shop/Facility sign shop").gameObject.activeInHierarchy,Is.True);
                    Capture(eastOutput,"east-door-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Walking to the florist does not automatically enter");
                }
                if(northReview)
                {
                    string northOutput=output==null?null:Path.Combine(output,"north");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();Canvas.ForceUpdateCanvases();
                        var town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownDistrictId.Central);town.World.LookAt(TownDistrictId.North);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var piece in town.GetComponent<TownSceneryView>().Pieces.Where(p=>p.Rect.gameObject.activeSelf))
                            Assert.That(Vector2.Distance(piece.Rect.anchoredPosition,town.World.Local(piece.Placement.Feet,(RectTransform)town.transform)),Is.LessThan(.01f));
                        foreach(var resident in town.Actors.Where(a=>a.RegionVisible&&a.transform.Find("Town actor name panel").gameObject.activeInHierarchy))
                            EastMasterReviewTests.AssertNameClearOfBenches(town,resident);
                        Capture(northOutput,"04-town-lv"+level+"-North",TownDistrictId.North);
                        if(level==1)WriteNorthRouteEvidence(northOutput,town);
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var north=Flow.GetComponentInChildren<TownLifeView>();
                    north.World.LookAt(TownDistrictId.North);north.World.Camera.Jump(new TownPoint(0,1f));north.World.Refresh();Capture(northOutput,"north-connection");
                    north.World.FastTravel(TownDistrictId.Central);Assert.That(north.World.GuideDistrict(TownDistrictId.North),Is.True);
                    for(int i=0;i<3000&&north.World.Model.Hero.Position.Y<1.32f;i++)north.AdvanceFrame(1f/30);
                    Assert.That(north.World.Model.Hero.Position.Y,Is.GreaterThanOrEqualTo(1.32f));
                    Assert.That(north.IsSpeaking,Is.False,"Walking must stay visible without unsolicited dialogue");Capture(northOutput,"north-walking");
                    for(int i=0;i<3000&&north.World.Model.Hero.IsWalking;i++)north.AdvanceFrame(1f/30);
                    Assert.That(north.World.Model.Hero.IsWalking,Is.False);
                    var facility=north.GetComponent<TownFacilityView>();Assert.That(facility.RequestEnter("quest"),Is.True);
                    for(int i=0;i<3000&&facility.Entry.Phase==TownEntryPhase.Walking;i++)north.AdvanceFrame(1f/30);
                    Assert.That(facility.Entry.Phase,Is.EqualTo(TownEntryPhase.Arrived));
                    Assert.That(north.IsSpeaking,Is.False,"The cottage arrival cue must not be covered by dialogue");
                    Assert.That(north.transform.Find("Facility entrance quest/Facility sign quest").gameObject.activeInHierarchy,Is.True);
                    Capture(northOutput,"north-door-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home),"Walking to the cottage does not automatically enter");
                }
                if(southReview)
                {
                    string southOutput=output==null?null:Path.Combine(output,"south");
                    foreach(int level in new[]{1,3,6})
                    {
                        Flow.Game.State.townGrowthLevel=level;Flow.ShowHome();EnsurePortraitCamera();var town=Flow.GetComponentInChildren<TownLifeView>();town.World.FastTravel(TownDistrictId.Central);town.World.LookAt(TownDistrictId.South);town.World.Refresh();yield return null;Canvas.ForceUpdateCanvases();
                        foreach(var resident in town.Actors.Where(a=>a.RegionVisible&&a.transform.Find("Town actor name panel").gameObject.activeInHierarchy))EastMasterReviewTests.AssertNameClearOfBenches(town,resident);
                        Capture(southOutput,"04-town-lv"+level+"-South",TownDistrictId.South);
                        if(level==1)WriteRouteEvidence(southOutput,town,"South",TownSouthMasterLayout.Circulation,new[]{"gazebo-left","gazebo-right","left-rest","right-rest","rest-cross","garden-loop"});
                    }
                    Flow.Game.State.townGrowthLevel=1;Flow.ShowHome();EnsurePortraitCamera();var south=Flow.GetComponentInChildren<TownLifeView>();
                    south.World.LookAt(TownDistrictId.South);south.World.Camera.Jump(new TownPoint(0,-1f));south.World.Refresh();Capture(southOutput,"south-connection");
                    south.World.FastTravel(TownDistrictId.Central);Assert.That(south.World.GuidePoint(TownWorldMap.Place("front-promenade")),Is.True);
                    for(int i=0;i<3000&&south.World.Model.Hero.Position.Y> -1.32f;i++)south.AdvanceFrame(1f/30);
                    Assert.That(south.World.Model.Hero.Position.Y,Is.LessThanOrEqualTo(-1.32f));Assert.That(south.IsSpeaking,Is.False);Capture(southOutput,"south-walking");
                    for(int i=0;i<3000&&south.World.Model.Hero.IsWalking;i++)south.AdvanceFrame(1f/30);
                    Assert.That(south.World.Model.Hero.IsWalking,Is.False);Assert.That(TownWalkableArea.Distance(south.World.Model.Hero.Position,TownWorldMap.Place("front-promenade")),Is.LessThan(.002f));
                    south.World.Model.Pause(south.World.Model.Hero.Id,2);south.RefreshWorldPresentation();Capture(southOutput,"south-rest-arrived");
                    Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));
                }
                if(output!=null)File.WriteAllLines(Path.Combine(output,"texture-memory.txt"),new[]{TownCentralMasterLayout.Resource,TownFountainGraphic.Resource,TownWestMasterLayout.Resource,TownSouthMasterLayout.Resource}.Select(key=>key+": "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Resources.Load<Texture2D>(key))+" bytes"));
                LogAssert.NoUnexpectedReceived();yield break;
            }
            Flow.ShowMerge();yield return null;Capture(output,"01-merge-start");
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
                    Assert.That(town.World.Model.Hero.District,Is.EqualTo(district));Assert.That(town.World.Model.Hero.IsWalking,Is.False);Capture(output,"26-cardinal-"+district+"-arrival",district);
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
                yield return CaptureBoundaryComparisons(output);
                yield return CaptureFieldStability(output);
                string[] art={"TownBackground","TownWestBackground","TownEastBackground","CharacterBodyAtlas","TownGrowthPropsAtlas","TownGardenSurface","TownSeamlessMap","TownStreetPropsAtlas","TownLandmarksAtlas","TownNatureAtlas","TownVillageDetailsAtlas","MergeBoardFrame","TownCentralGround","TownCentralFountain","TownWestGround"};
                File.WriteAllLines(Path.Combine(output,"texture-memory.txt"),art.Select(key=>key+": "+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Resources.Load<Texture2D>("Mofumachi/"+key))+" bytes"));
                File.WriteAllText(Path.Combine(output,"README.txt"),"Actual Unity UGUI RenderTexture captures, 720x1280. Isolated test-only save state; not Pixel 3a captures. Full suite154 PNG: existing102 integrated frames,22 boundary comparison frames,14 field-stability frames,6 actual delivery-growth/reload frames,10 fountain OFF/ON and animation frames. Fountain Before disables only the new water layer and retains the original fountain/ripples; not a previous APK reconstruction. 30-boundary Before disables only the curved band in the CURRENT runtime, retaining the new field-stability depth/camera/navigation fixes; it is a boundary-only comparison, NOT a complete frozen5a49 renderer or previous APK/Pixel capture. 31-field shows actual walking arrival/return in all5areas and the East building corner, plus front/behind fountain depth; no teleport rescue. 30Hz simulation is not device FPS; Editor texture memory is not Pixel RAM. Stable region/place/save IDs retained. Pixel save retention, audio,10minuteplay and subjective quality still require actual acceptance.\n");
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
