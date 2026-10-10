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
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();var source=Flow.GetComponentsInChildren<MergeBoardView>()[0];var e=Pointer(source.transform);
            source.OnBeginDrag(e);e.position=new Vector2(-200,-200);source.OnEndDrag(e);yield return null;
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));Assert.That(GameObject.Find("Dragged tea"),Is.Null);
            Store.Fail=true;Assert.That(Flow.DropItem(0,1),Is.False);Assert.That(Flow.Game.Board.At(0).level,Is.EqualTo(1));Assert.That(Flow.Game.Board.IsLocked(1),Is.False);
            Store.Fail=false;Assert.That(Flow.AcceptQuest(),Is.True);var deliver=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Deliver");Assert.That(deliver.interactable,Is.False);
        }
        [UnityTest] public IEnumerator DifferentMaxLevelAndLockedItemsRefuseWithoutMutation()
        {
            yield return EnterHome();Flow.ShowMerge();
            Flow.Game.State.mergeBoard[1].level=2;Assert.That(Flow.DropItem(0,1),Is.False);
            Flow.Game.State.mergeBoard[0].level=Mofumachi.Core.GameState.MaxLevel;Flow.Game.State.mergeBoard[1].level=Mofumachi.Core.GameState.MaxLevel;
            Assert.That(Flow.DropItem(0,1),Is.False);Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2));
            Assert.That(Flow.Game.Board.At(0).level,Is.EqualTo(Mofumachi.Core.GameState.MaxLevel));
        }
        [UnityTest] public IEnumerator ShortPortraitBoardStaysSquareAndOperable()
        {
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            foreach(var cell in cells){var rect=(RectTransform)cell.transform;Assert.That(rect.rect.width,Is.EqualTo(rect.rect.height).Within(.01));Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(48));}
            Assert.That(ResponsiveUILayout.CellSize(new Vector2(336,320)),Is.GreaterThanOrEqualTo(48));
        }
    }
}
