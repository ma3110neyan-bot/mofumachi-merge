using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

namespace Mofumachi.Tests
{
    public abstract class SliceUiTestFixture
    {
        private static readonly FieldInfo Session = typeof(UIFlowController).GetField("session", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo SocialSession=typeof(UIFlowController).GetField("socialSession",BindingFlags.Static|BindingFlags.NonPublic);
        private object previousSession,previousSocialSession;
        protected string DirectoryPath;
        protected TestStore Store;
        protected UIFlowController Flow => UnityEngine.Object.FindAnyObjectByType<UIFlowController>();
        private bool cleaned;

        [SetUp] public void Isolate()
        {
            previousSession = Session.GetValue(null);previousSocialSession=SocialSession.GetValue(null);
            SocialSession.SetValue(null,new TownDailyService(new MemoryTownDailyStore(),DateTime.Now));
            DirectoryPath = Path.Combine(Path.GetTempPath(), "mofumachi-ui-" + Guid.NewGuid());
            Store = new TestStore(new SaveService(DirectoryPath));
            Session.SetValue(null, new GameStateManager(Store.Load().State, Store));
            cleaned = false;
        }
        [TearDown] public void Cleanup()
        {
            if (cleaned) return;
            foreach (var ui in UnityEngine.Object.FindObjectsByType<UIFlowController>(FindObjectsInactive.Include))
            { ui.StopAllCoroutines(); UnityEngine.Object.DestroyImmediate(ui.gameObject); }
            foreach (var audio in UnityEngine.Object.FindObjectsByType<AudioManager>(FindObjectsInactive.Include))
            { audio.StopAllCoroutines(); UnityEngine.Object.DestroyImmediate(audio.gameObject); }
            Session.SetValue(null, previousSession);SocialSession.SetValue(null,previousSocialSession);
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
            cleaned = true;
        }
        protected IEnumerator LoadTitle()
        { yield return SceneManager.LoadSceneAsync("TitleScene"); yield return null; Canvas.ForceUpdateCanvases(); }
        protected IEnumerator EnterHome()
        {
            yield return LoadTitle(); Flow.BeginGame();
            if (Flow.IsPurchaseNoticeOpen) Assert.That(Flow.ConfirmPurchaseNotice(), Is.True);
            yield return null; yield return null;
            if(Flow.CurrentScreen==ScreenId.CharacterSelection){Flow.PreviewFavorite("momo");Assert.That(Flow.ConfirmFavorite(),Is.True);}
        }
        protected TownLifeView FrameTownPortrait(int height=740)
        {
            // Native batch runs inherit the Editor's Game view, which may be
            // landscape. Exercise the phone field without moving any resident.
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;
            canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,height);
            Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));
            Canvas.ForceUpdateCanvases();var life=Flow.GetComponentInChildren<TownLifeView>();life.World.Refresh();life.AdvanceFrame(1f/30);return life;
        }
        protected TownActor FrameTownNpc(int height=740)
        {
            var life=FrameTownPortrait(height);Assert.That(life.Regions.LookAtRegion(TownRegionId.Central),Is.True);
            life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();
            var npc=life.Actors.FirstOrDefault(a=>!a.Controlled&&a.RegionVisible);
            Assert.That(npc,Is.Not.Null,"The framed central park must contain a real visible resident");return npc;
        }
        protected sealed class TestStore : IStateStore
        {
            private readonly IStateStore disk;
            public bool Fail;
            public int Writes;
            public TestStore(IStateStore disk) { this.disk = disk; }
            public void Save(GameState s) { if (Fail) throw new IOException("Test save failure"); disk.Save(s); Writes++; }
            public LoadResult Load() => disk.Load();
            public void ResetSave() => disk.ResetSave();
        }
    }
}
