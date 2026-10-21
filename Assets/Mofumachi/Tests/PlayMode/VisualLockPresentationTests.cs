using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class VisualLockPresentationTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator CloseResidentLabelsNeverCoverAnotherCharactersBody()
        {
            yield return EnterHome();var town=Flow.GetComponentInChildren<TownLifeView>();
            var hero=town.Actors.Single(a=>a.Controlled);var friend=town.Actors.First(a=>!a.Controlled);
            town.World.Model.Restore(hero.CharacterId,new TownPoint(0,-.25f));
            town.World.Model.Restore(friend.CharacterId,new TownPoint(.02f,-.12f));
            town.World.ReturnToHero();town.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();town.AdvanceFrame(1f/30);
            void AssertNamesClear()
            {
                foreach(var actor in town.Actors)
                {
                    var name=actor.transform.Find("Town actor name panel");if(!name.gameObject.activeSelf)continue;
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(town.transform,name);
                    foreach(var other in town.Actors.Where(a=>a!=actor&&a.RegionVisible))
                    {
                        var body=RectTransformUtility.CalculateRelativeRectTransformBounds(town.transform,other.VisualRoot);
                        Assert.That(bounds.Intersects(body),Is.False,actor.CharacterId+" label covers "+other.CharacterId);
                    }
                }
            }
            AssertNamesClear();
            // Refresh also belongs to the real manual-pointer path, which
            // returns before social interactions and facility simulation.
            town.World.Model.Restore(hero.CharacterId,new TownPoint(0,-.65f));town.AdvanceFrame(1f/30);
            Assert.That(friend.transform.Find("Town actor name panel").gameObject.activeSelf,Is.True);
            var pointer=new PointerEventData(EventSystem.current){pointerId=72};hero.OnPointerDown(pointer);hero.OnBeginDrag(pointer);
            Assert.That(town.IsManuallyDragging,Is.True);
            town.World.Model.Restore(hero.CharacterId,new TownPoint(0,-.25f));town.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();
            AssertNamesClear();hero.OnPointerUp(pointer);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FlatBoardKeepsItsAuthoredFrameAndDistinctCellColorsWithoutNewTextures()
        {
            yield return EnterHome();Flow.ShowMerge();yield return null;Canvas.ForceUpdateCanvases();
            var frame=Flow.GetComponentInChildren<PerspectiveBoardGraphic>();
            Assert.That(frame.mainTexture,Is.SameAs(Resources.Load<Texture2D>("Mofumachi/MergeBoardFrame")));
            var cells=Flow.GetComponentsInChildren<MergeBoardView>();Assert.That(cells.Length,Is.EqualTo(30));
            var empty=cells.Where(c=>Flow.Game.Board.At(int.Parse(c.name.Substring(5)))==null).ToArray();
            var colors=empty.Take(2).Select(c=>c.GetComponentInChildren<MergeCellSurfaceGraphic>().canvasRenderer.GetMesh().colors32.Distinct().ToArray()).ToArray();
            Assert.That(colors.Length,Is.EqualTo(2));Assert.That(colors[0].Any(color=>!colors[1].Contains(color)),Is.True,"Compare the full ceramic palette, including the face; the final highlight vertices are shared white");
            Assert.That(Flow.Game.State.mergeBoard.Count,Is.EqualTo(2),"Presentation must never populate the saved board");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
