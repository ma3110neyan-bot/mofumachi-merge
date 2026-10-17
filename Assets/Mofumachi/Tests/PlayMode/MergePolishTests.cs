using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class MergePolishTests : SliceUiTestFixture
    {
        private ItemGraphic Art(int cell)=>Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell "+cell).GetComponentInChildren<ItemGraphic>();
        private MergeEffectsView Effects()=>Flow.GetComponentsInChildren<MergeEffectsView>(true).Single();

        [UnityTest] public IEnumerator StaticPolishKeepsBoardAndUsesSharedArtWithoutInterceptingInput()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            var art=Art(0);Assert.That(art.Polished,Is.True);Assert.That(art.mainTexture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/ItemAtlas")));
            Assert.That(art.raycastTarget,Is.False);
            var mesh=art.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.EqualTo(8),"A silhouette shadow and the original image share one mesh.");
            Assert.That(mesh.colors.Any(c=>c.a>0&&c.a<.3f),Is.True,"The shadow should be translucent.");
            Assert.That(cells[0].transform.Find("Item level badge"),Is.Not.Null);
            Assert.That(cells[0].transform.Find("Item contact shadow").GetComponent<Graphic>().raycastTarget,Is.False);
            var dish=cells[0].GetComponentInChildren<MergeCellSurfaceGraphic>();Assert.That(dish.canvasRenderer.GetMesh().colors.Any(c=>c.r>.99f&&c.g>.99f&&c.b>.99f&&c.a>.4f&&c.a<.8f),Is.True,"The oval dish retains its visible white highlight.");
            Assert.That(cells[0].GetComponent<RoundedPanelGraphic>().color.a,Is.LessThan(.005f),"The input rectangle must not cover the oval dish with a flat seat");
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SelectionScalesOnlyTheItemAndDoesNotSave()
        {
            yield return EnterHome();Flow.ShowMerge();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            Flow.TapCell(0);yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Art(0).transform.localScale.x,Is.EqualTo(1.06f).Within(.001));
            Assert.That(Art(0).transform.parent.localScale,Is.EqualTo(Vector3.one));
            Assert.That(Art(0).rectTransform.anchoredPosition.y,Is.EqualTo(3).Within(.001));
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell 1").transform.Find("Merge candidate glow"),Is.Not.Null);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("お茶 Lv.1"));
            Assert.That(Flow.GetComponentsInChildren<MergeSparkleGraphic>().All(g=>!g.raycastTarget),Is.True);
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Flow.Game.State.mergeBoard[1].itemId="bread";Flow.ShowMerge();Flow.TapCell(0);
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().All(v=>v.transform.Find("Merge candidate glow")==null),Is.True,"A different kind must never be suggested.");
            Flow.Game.State.mergeBoard[1].itemId="tea";Flow.Game.State.mergeBoard[1].level=2;Flow.ShowMerge();Flow.TapCell(0);
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().All(v=>v.transform.Find("Merge candidate glow")==null),Is.True,"A different level must never be suggested.");
            Flow.Game.State.mergeBoard[1].level=1;
            Flow.GenerateItem("bread");Assert.That(Flow.SelectedCell,Is.EqualTo(-1),"Producing an item starts a new selection gesture.");
            Assert.That(Art(0).transform.localScale,Is.EqualTo(Vector3.one));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SavedMergeUnlocksBeforeTheLightTailAndNeverReplaysOnReentry()
        {
            yield return EnterHome();Flow.ShowMerge();int before=Store.Writes;
            float startedAt=Time.unscaledTime;Assert.That(Flow.DropItem(0,1),Is.True);var effects=Effects();Assert.That(effects.IsPlaying,Is.True);
            Assert.That(Store.Writes,Is.EqualTo(before+1));Assert.That(Flow.CanInteractBoard,Is.False);
            Assert.That(effects.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),Is.True);
            yield return new WaitForSecondsRealtime(.27f);
            Assert.That(Flow.CanInteractBoard,Is.True);
            if(Time.unscaledTime-startedAt<MergeVisualTiming.LifetimeSeconds)Assert.That(effects.IsPlaying,Is.True);
            Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));int writes=Store.Writes;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(effects.IsPlaying,Is.False);Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.ShowMerge();Assert.That(effects.IsPlaying,Is.False);Assert.That(Flow.Game.State.coins,Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FailedAndRejectedMergesDoNotShowSuccessOrConsumeItems()
        {
            yield return EnterHome();Flow.ShowMerge();string before=StateCodec.Encode(Flow.Game.State);
            Store.Fail=true;Assert.That(Flow.DropItem(0,1),Is.False);Store.Fail=false;
            Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>().Any(v=>v.IsPlaying),Is.False);
            Flow.Game.State.mergeBoard[1].itemId="bread";before=StateCodec.Encode(Flow.Game.State);
            Assert.That(Flow.DropItem(0,1),Is.False);Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>().Any(v=>v.IsPlaying),Is.False);
            Flow.Game.State.mergeBoard[0].itemId=Flow.Game.State.mergeBoard[1].itemId="tea";
            Flow.Game.State.mergeBoard[0].level=Flow.Game.State.mergeBoard[1].level=3;before=StateCodec.Encode(Flow.Game.State);
            Assert.That(Flow.DropItem(0,1),Is.False);Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>().Any(v=>v.IsPlaying),Is.False);LogAssert.NoUnexpectedReceived();
            Flow.Game.State.mergeBoard[0].level=Flow.Game.State.mergeBoard[1].level=1;Flow.ShowMerge();
            Assert.That(Flow.DropItem(0,1),Is.True);Store.Fail=true;yield return new WaitForSecondsRealtime(.27f);Store.Fail=false;
            Assert.That(Effects().IsPlaying,Is.False,"A late save failure must cancel the remaining light.");
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Is.Not.Empty);Assert.That(Store.Load().State.mergeBoard.Single().level,Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator PauseAndSettingsCancelVisualsAndKeepTheSavedMerge()
        {
            yield return EnterHome();Flow.ShowMerge();Assert.That(Flow.DropItem(0,1),Is.True);var effects=Effects();
            Assert.That(Flow.Deliver().Success,Is.False);Assert.That(Flow.FeedbackFor(ScreenId.Merge),Is.EqualTo(UIStrings.MergeBusy));
            Flow.SendMessage("OnApplicationPause",true);Assert.That(effects.IsPlaying,Is.False);
            Assert.That(Flow.GetComponentsInChildren<MergeSparkleGraphic>().Length,Is.Zero);
            Flow.SendMessage("OnApplicationPause",false);Assert.That(effects.IsPlaying,Is.False);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Is.Not.EqualTo(UIStrings.MergeBusy));
            Assert.That(Art(1).enabled,Is.True);Assert.That(Art(1).transform.localScale,Is.EqualTo(Vector3.one));
            Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));Assert.That(Flow.Game.AddItem("tea",2),Is.True);
            Flow.ShowMerge();Assert.That(Flow.DropItem(0,1),Is.True);Flow.ShowSettings();
            Assert.That(effects.IsPlaying,Is.False);Assert.That(Store.Load().State.mergeBoard.Single().level,Is.EqualTo(3));
            Flow.GoBack();Assert.That(Art(1).enabled,Is.True);Assert.That(effects.IsPlaying,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ConsecutiveMergesReuseOneSmallEffectLayerAndRestartKeepsProgress()
        {
            yield return EnterHome();Flow.ShowMerge();Assert.That(Flow.DropItem(0,1),Is.True);var first=Effects();
            yield return new WaitForSecondsRealtime(.27f);
            Assert.That(Flow.Game.AddItem("tea",1),Is.True);Assert.That(Flow.Game.AddItem("tea",1),Is.True);Flow.ShowMerge();
            Assert.That(Flow.DropItem(0,2),Is.True);Assert.That(Effects(),Is.SameAs(first));
            var fixture=UIWidgets.Node("Sparkle mesh budget fixture",(RectTransform)Flow.GetComponentInChildren<Canvas>().transform,new Rect(.5f,.5f,0,0));fixture.sizeDelta=new Vector2(48,48);
            try
            {
                var sparkle=fixture.gameObject.AddComponent<MergeSparkleGraphic>();sparkle.SetBurstFrame(.13f);
                yield return null;Canvas.ForceUpdateCanvases();var mesh=sparkle.canvasRenderer.GetMesh();
                Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.InRange(1,120),"One short burst of eight stars and a soft glow.");
            }finally{Object.DestroyImmediate(fixture.gameObject);}
            Assert.That(first.GetComponentsInChildren<ItemGraphic>(true).Length,Is.EqualTo(3));
            yield return new WaitForSecondsRealtime(.27f);Assert.That(Flow.DropItem(1,2),Is.True);
            Assert.That(Effects(),Is.SameAs(first));Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>(true).Length,Is.EqualTo(1));
            Assert.That(Flow.Game.State.coins,Is.Zero);Assert.That(Flow.Game.State.claimedRewardIds,Is.Empty);
            yield return SceneManager.LoadSceneAsync("TitleScene");yield return null;
            Assert.That(Flow.Game.Board.At(2).level,Is.EqualTo(3));Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>().Length,Is.Zero);
            Flow.BeginGame();yield return null;yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Flow.ShowMerge();
            Assert.That(Art(2).enabled,Is.True);Assert.That(Flow.GetComponentsInChildren<MergeEffectsView>().Length,Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LayoutCancelsVisualsAndBusyDeliveryPreservesSavedMergeUntilUnlock()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;
            var drag=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell 0");
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            string before=StateCodec.Encode(Flow.Game.State);drag.OnBeginDrag(pointer);Assert.That(GameObject.Find("Dragged tea"),Is.Not.Null);
            ((RectTransform)drag.transform).sizeDelta+=Vector2.one;Assert.That(GameObject.Find("Dragged tea"),Is.Null);
            drag.OnEndDrag(pointer);Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            Assert.That(Flow.DropItem(0,1),Is.True);var effects=Effects();var target=Art(1);Assert.That(target.enabled,Is.False);
            ((RectTransform)effects.transform).offsetMax-=Vector2.one;
            Assert.That(effects.IsPlaying,Is.False);Assert.That(target.enabled,Is.True);
            yield return new WaitForSecondsRealtime(.27f);
            Assert.That(Flow.Game.AddItem("tea",2),Is.True);Flow.ShowMerge();Assert.That(Flow.DropItem(0,1),Is.True);
            Assert.That(Flow.Deliver().Success,Is.False,"Locked animation cannot deliver.");
            Assert.That(effects.IsPlaying,Is.True,"A busy delivery must not rebuild the board or interrupt the saved merge.");
            Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(3));Assert.That(Flow.Game.State.coins,Is.Zero);
            yield return new WaitForSecondsRealtime(.27f);
            Assert.That(Art(1).enabled,Is.True);Assert.That(Flow.CanInteractBoard,Is.True);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Is.Not.EqualTo(UIStrings.MergeBusy));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ShortAndTallLayoutsKeepLevelTextAndSelectedArtInsideCells()
        {
            yield return EnterHome();Flow.ShowMerge();Flow.TapCell(0);
            var canvas=new GameObject("Polish layout fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var root=(RectTransform)canvas.transform;var widgets=new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster"));
            try{foreach(float height in new[]{592f,640f,808f})
            {
                root.sizeDelta=new Vector2(360,height);var page=UIWidgets.Node("Merge fixture",root,new Rect(0,0,1,1));
                QuestMergeScreens.BuildMerge(page,new ScreenContext(Flow,widgets));Canvas.ForceUpdateCanvases();
                page.GetComponentInChildren<MergeGridLayout>().ResizeNow();Canvas.ForceUpdateCanvases();yield return new WaitForSecondsRealtime(.18f);
                var cells=page.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
                foreach(var cell in cells)
                {
                    var rect=(RectTransform)cell.transform;Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(48));Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(48));
                    var art=cell.GetComponentInChildren<ItemGraphic>();if(art==null)continue;
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(rect,art.rectTransform);
                    Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(rect.rect.xMin));Assert.That(bounds.max.x,Is.LessThanOrEqualTo(rect.rect.xMax));
                    Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(rect.rect.yMin));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(rect.rect.yMax));
                    var level=cell.GetComponentsInChildren<Text>().Single(t=>t.name=="Item level");
                    Assert.That(level.resizeTextForBestFit,Is.False);Assert.That(level.fontSize,Is.GreaterThanOrEqualTo(11));
                    Assert.That(level.preferredHeight,Is.LessThanOrEqualTo(level.rectTransform.rect.height+1));
                }
                Object.DestroyImmediate(page.gameObject);
            }}finally{Object.DestroyImmediate(canvas);}LogAssert.NoUnexpectedReceived();
        }
    }
}
