using System.Collections;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mofumachi.Tests
{
    public sealed class VerticalSliceFlowTests
    {
        private static readonly FieldInfo Session = typeof(UIFlowController).GetField("session", BindingFlags.NonPublic | BindingFlags.Static);
        private object previousSession;
        private string dir;
        private bool cleaned;
        [SetUp]
        public void IsolateSession()
        {
            previousSession = Session.GetValue(null);
            dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
            var store = new SaveService(dir);
            Session.SetValue(null, new GameStateManager(store.Load().State, store));
            cleaned = false;
        }
        [TearDown]
        public void Cleanup()
        {
            if (cleaned) return;
            // Stop native callbacks before changing the session or deleting its storage.
            foreach (var ui in Object.FindObjectsByType<UIFlowController>(FindObjectsInactive.Include))
            {
                ui.StopAllCoroutines();
                Object.DestroyImmediate(ui.gameObject);
            }
            Session.SetValue(null, previousSession);
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            cleaned = true;
        }
        [UnityTest]
        public IEnumerator LoopRewardsOnceAndResumesAfterSceneReload()
        {
            yield return SceneManager.LoadSceneAsync("TitleScene");
            var ui = Object.FindAnyObjectByType<UIFlowController>();
            // Replace only this test's in-memory session/store; never erase a player save.
            ui.Initialize(new SaveService(dir));
            ui.BeginGame();
            yield return null;
            yield return null;
            ui = Object.FindAnyObjectByType<UIFlowController>();
            Assert.That(ui.AcceptQuest(), Is.True);
            Assert.That(ui.Deliver().Success, Is.False);
            Assert.That(ui.Game.State.mergeBoard.Count, Is.EqualTo(2));
            Assert.That(ui.DropItem(0, 1), Is.True);
            Assert.That(ui.Deliver().Success, Is.False, "Animation locks exclude delivery.");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(ui.Deliver().Success, Is.True);
            Assert.That(ui.Deliver().Success, Is.False);
            Assert.That(ui.Game.State.coins, Is.EqualTo(30));
            Assert.That(ui.Game.State.townGrowthLevel, Is.EqualTo(1));
            ui.ShowHome();
            Assert.That(ui.Persist(), Is.True);
            yield return SceneManager.LoadSceneAsync("TitleScene");
            ui = Object.FindAnyObjectByType<UIFlowController>();
            ui.Initialize(new SaveService(dir));
            Assert.That(ui.Game.State.coins, Is.EqualTo(30));
            Assert.That(ui.Game.State.completedQuestIds.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CancelAndSettingsPersistWithoutLosingMergedItems()
        {
            yield return SceneManager.LoadSceneAsync("TitleScene");
            var ui = Object.FindAnyObjectByType<UIFlowController>();
            ui.Initialize(new SaveService(dir));
            Assert.That(ui.DropItem(0, 1), Is.True);
            ui.ShowHome();
            Assert.That(ui.Game.Board.IsLocked(1), Is.False);
            Assert.That(ui.Game.Board.At(1).level, Is.EqualTo(2));
            Assert.That(ui.Game.SetAudio(false, true), Is.True);
            Assert.That(ui.Persist(), Is.True);
            var resumed = new SaveService(dir).Load().State;
            Assert.That(resumed.bgmEnabled, Is.False);
            Assert.That(resumed.seEnabled, Is.True);
            Assert.That(resumed.mergeBoard.Count, Is.EqualTo(1));
            Assert.That(resumed.mergeBoard[0].level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TeardownDuringAnimationLeavesNoCallbacksOrStore()
        {
            yield return SceneManager.LoadSceneAsync("TitleScene");
            var ui = Object.FindAnyObjectByType<UIFlowController>();
            Assert.That(ui.DropItem(0, 1), Is.True);
            Assert.That(ui.Game.Board.IsLocked(1), Is.True);
            Cleanup();
            yield return new WaitForSecondsRealtime(.3f);
            LogAssert.NoUnexpectedReceived();
            Assert.That(Object.FindAnyObjectByType<UIFlowController>(), Is.Null);
            Assert.That(System.IO.Directory.Exists(dir), Is.False);
            Assert.That(Session.GetValue(null), Is.SameAs(previousSession));
        }

        [UnityTest]
        public IEnumerator PauseDuringMergeAndDragRebuildsUnlockedBoard()
        {
            yield return SceneManager.LoadSceneAsync("TitleScene");
            var ui = Object.FindAnyObjectByType<UIFlowController>();
            Assert.That(ui.DropItem(0, 1), Is.True);
            var lockedColor = ui.GetComponentsInChildren<MergeBoardView>()[1].GetComponent<UnityEngine.UI.Image>().color;
            ui.SendMessage("OnApplicationPause", true);
            Assert.That(ui.Game.Board.IsLocked(1), Is.False);
            Assert.That(ui.GetComponentsInChildren<MergeBoardView>()[1].GetComponent<UnityEngine.UI.Image>().color, Is.Not.EqualTo(lockedColor));
            var drag = ui.GetComponentsInChildren<MergeBoardView>()[1];
            drag.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Assert.That(GameObject.Find("Dragged tea"), Is.Not.Null);
            ui.SendMessage("OnApplicationPause", true);
            yield return null;
            ui.SendMessage("OnApplicationPause", false);
            Assert.That(GameObject.Find("Dragged tea"), Is.Null);
            Assert.That(ui.Game.Board.At(1).level, Is.EqualTo(2));
        }
    }
}
