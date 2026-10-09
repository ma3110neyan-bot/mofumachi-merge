using System.Collections;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        public IEnumerator TitleShowsSeparatePortraitsAndVisibleStartButtonThenOpensHome()
        {
            yield return SceneManager.LoadSceneAsync("TitleScene");
            yield return null;
            Canvas.ForceUpdateCanvases();
            var ui = Object.FindAnyObjectByType<UIFlowController>();
            Button start = null;
            foreach (var button in ui.GetComponentsInChildren<Button>())
            {
                var label = button.GetComponentInChildren<Text>();
                if (label != null && label.text == "はじめる") start = button;
            }
            Assert.That(start, Is.Not.Null);
            var page = (RectTransform)start.transform.parent;
            Assert.That(page.rect.width / page.rect.height, Is.EqualTo(9f / 16).Within(.001f),
                "Keep the portrait layout even in a landscape Editor Game view.");
            Assert.That(start.GetComponentInChildren<Text>().cachedTextGenerator.characterCountVisible, Is.GreaterThan(0));
            var camera = ui.GetComponentInChildren<Camera>();
            Assert.That(camera, Is.Not.Null, "A camera must render behind the overlay UI.");
            Assert.That(camera.isActiveAndEnabled, Is.True);

            var portraits = ui.GetComponentsInChildren<RawImage>();
            Assert.That(portraits.Length, Is.EqualTo(6));
            System.Array.Sort(portraits, (left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
            for (int i = 0; i < portraits.Length; i++)
            {
                var rect = portraits[i].rectTransform;
                Assert.That(rect.rect.width, Is.LessThanOrEqualTo(page.rect.width * .16f));
                Assert.That(rect.rect.height, Is.LessThanOrEqualTo(page.rect.height * .11f));
                if (i > 0) Assert.That(rect.position.x, Is.GreaterThan(portraits[i - 1].transform.position.x));
            }

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, start.transform.position),
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(start),
                "Character images must not cover or intercept the start button.");
            ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameScene"));

            Canvas.ForceUpdateCanvases();
            ui = Object.FindAnyObjectByType<UIFlowController>();
            RawImage town = null;
            foreach (var image in ui.GetComponentsInChildren<RawImage>())
                if (image.name == "Town") town = image;
            Assert.That(town, Is.Not.Null);
            var home = (RectTransform)town.transform.parent.parent;
            Assert.That(town.rectTransform.rect.width, Is.LessThanOrEqualTo(home.rect.width * .84f));
            Assert.That(town.rectTransform.rect.height, Is.LessThanOrEqualTo(home.rect.height * .38f + .01f));
            LogAssert.NoUnexpectedReceived();
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
