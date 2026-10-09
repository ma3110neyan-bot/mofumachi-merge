using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mofumachi.Tests
{
    public abstract class SliceUiTestFixture
    {
        private static readonly FieldInfo Session = typeof(UIFlowController).GetField("session", BindingFlags.Static | BindingFlags.NonPublic);
        private object previousSession;
        protected string DirectoryPath;
        protected TestStore Store;
        protected UIFlowController Flow => UnityEngine.Object.FindAnyObjectByType<UIFlowController>();
        private bool cleaned;

        [SetUp] public void Isolate()
        {
            previousSession = Session.GetValue(null);
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
            Session.SetValue(null, previousSession);
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
