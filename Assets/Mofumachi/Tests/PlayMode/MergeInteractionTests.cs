using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class MergeInteractionTests : SliceUiTestFixture
    {
        private PointerEventData Pointer(Transform t)=>new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,t.position),button=PointerEventData.InputButton.Left,eligibleForClick=true};
        private static void AssertPointerHitsCell(MergeBoardView cell,PointerEventData pointer)
        {
            var graphic=cell.GetComponent<Graphic>();
            Assert.That(graphic.depth,Is.GreaterThanOrEqualTo(0),cell.name+" must be rendered before pointer input");
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            string details=cell.name+" at "+pointer.position+" in "+Screen.width+"x"+Screen.height+"; hits: "+string.Join(", ",hits.Select(h=>h.gameObject.name));
            Assert.That(hits,Is.Not.Empty,details);
            Assert.That(hits[0].gameObject.GetComponentInParent<MergeBoardView>(),Is.SameAs(cell),details);
        }
        [UnityTest] public IEnumerator PointerDragAndTwoTapsProduceOneMerge()
        {
            yield return EnterHome();Flow.ShowMerge();
            // A new CanvasRenderer gets its raycast depth during rendering.
            // ForceUpdateCanvases rebuilds layout but does not replace that frame.
            yield return null;Canvas.ForceUpdateCanvases();
            var source=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell 0");var target=Flow.GetComponentsInChildren<MergeBoardView>().Single(v=>v.name=="Cell 1");
            var pointer=Pointer(source.transform);AssertPointerHitsCell(source,pointer);
            ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.beginDragHandler);
            Assert.That(pointer.eligibleForClick,Is.False);yield return null;
            pointer.position=Pointer(target.transform).position;ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.dragHandler);
            AssertPointerHitsCell(target,pointer);ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.endDragHandler);
            Assert.That(pointer.eligibleForClick,Is.False);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(1));Assert.That(Flow.Game.Board.At(1).level,Is.EqualTo(2));
            Assert.That(Flow.DropItem(1,2),Is.False);yield return new WaitForSecondsRealtime(.3f);
            var from=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Cell 1");ExecuteEvents.Execute(from.gameObject,Pointer(from.transform),ExecuteEvents.pointerClickHandler);
            yield return null;Canvas.ForceUpdateCanvases();
            var to=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Cell 2");var tap=Pointer(to.transform);AssertPointerHitsCell(to.GetComponent<MergeBoardView>(),tap);ExecuteEvents.Execute(to.gameObject,tap,ExecuteEvents.pointerClickHandler);
            Assert.That(Flow.Game.Board.At(1),Is.Null);Assert.That(Flow.Game.Board.At(2).level,Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator OutsideDropAndSaveFailureDoNotConsumeItems()
        {
            yield return EnterHome();Flow.ShowMerge();Flow.TapCell(0);Canvas.ForceUpdateCanvases();var source=Flow.GetComponentsInChildren<MergeBoardView>()[0];var e=Pointer(source.transform);
            source.OnBeginDrag(e);e.position=new Vector2(-200,-200);source.OnEndDrag(e);yield return null;
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));Assert.That(GameObject.Find("Dragged tea"),Is.Null);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("盤面の外"));
            Assert.That(Flow.SelectedCell,Is.EqualTo(0));
            Store.Fail=true;Assert.That(Flow.DropItem(0,1),Is.False);Assert.That(Flow.Game.Board.At(0).level,Is.EqualTo(1));Assert.That(Flow.Game.Board.IsLocked(1),Is.False);
            Assert.That(Flow.SelectedCell,Is.EqualTo(0));
            Store.Fail=false;Assert.That(Flow.AcceptQuest(),Is.True);var deliver=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");Assert.That(deliver.interactable,Is.False);
        }
        [UnityTest] public IEnumerator DragShowsOnlyValidPartnersAndHoverThenCleansUpWithoutExtraSaves()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();var source=cells.Single(v=>v.name=="Cell 0");var target=cells.Single(v=>v.name=="Cell 1");
            var pointer=Pointer(source.transform);int writes=Store.Writes;string before=Mofumachi.Core.StateCodec.Encode(Flow.Game.State);
            source.OnBeginDrag(pointer);
            Assert.That(target.transform.Find("Drag match guidance"),Is.Not.Null);
            Assert.That(cells.Where(v=>v.name!="Cell 1").All(v=>v.transform.Find("Drag match guidance")==null),Is.True);
            Assert.That(source.GetComponentInChildren<ItemGraphic>().enabled,Is.False);
            pointer.position=Pointer(target.transform).position;source.OnDrag(pointer);
            Assert.That(target.transform.Find("Drag target glow"),Is.Not.Null);
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Mofumachi.Core.StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            pointer.position=new Vector2(-200,-200);source.OnDrag(pointer);source.OnEndDrag(pointer);yield return null;
            Assert.That(GameObject.Find("Dragged tea"),Is.Null);
            Assert.That(Flow.GetComponentsInChildren<MergeSparkleGraphic>().Any(g=>g.name.StartsWith("Drag ")),Is.False);
            Assert.That(source.GetComponentInChildren<ItemGraphic>().enabled,Is.True);
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Mofumachi.Core.StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DifferentMaxLevelAndLockedItemsRefuseWithoutMutation()
        {
            yield return EnterHome();Flow.ShowMerge();
            Flow.Game.State.mergeBoard[1].level=2;Assert.That(Flow.DropItem(0,1),Is.False);
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("Lv.が違います"));
            Flow.Game.State.mergeBoard[0].level=Mofumachi.Core.GameState.MaxLevel;Flow.Game.State.mergeBoard[1].level=Mofumachi.Core.GameState.MaxLevel;
            Assert.That(Flow.DropItem(0,1),Is.False);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));
            Assert.That(Flow.Game.Board.At(0).level,Is.EqualTo(Mofumachi.Core.GameState.MaxLevel));
            Assert.That(Flow.FeedbackFor(ScreenId.Merge),Does.Contain("最高Lv."));
        }
        [UnityTest] public IEnumerator ShortPortraitBoardStaysSquareAndOperable()
        {
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            foreach(var cell in cells){var rect=(RectTransform)cell.transform;Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(48));Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(48));}
            Assert.That(ResponsiveUILayout.CellSize(new Vector2(336,320)),Is.GreaterThanOrEqualTo(48));
        }
    }
}
