using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class CentralMasterReviewTests:SliceUiTestFixture
    {
        [UnityTest] public IEnumerator CentralGroundTracksWorldProjectionAndNeverCapturesPointerInput()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            var ground=Flow.GetComponentInChildren<TownCentralGroundGraphic>();
            Assert.That(ground,Is.Not.Null);Assert.That(ground.raycastTarget,Is.False);
            var texture=Resources.Load<Texture2D>(TownCentralMasterLayout.Resource);
            Assert.That(ground.mainTexture,Is.SameAs(texture));Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.width,Is.LessThanOrEqualTo(1024));Assert.That(texture.height,Is.LessThanOrEqualTo(2048));
            void AssertProjection()
            {
                Canvas.ForceUpdateCanvases();var mesh=ground.canvasRenderer.GetMesh();Assert.That(mesh.vertexCount,Is.EqualTo(4));
                var expected=life.World.Local(new TownPoint(TownCentralMasterLayout.Left,TownCentralMasterLayout.Bottom),ground.rectTransform);
                Assert.That(Vector2.Distance(mesh.vertices[0],expected),Is.LessThan(.01f));
            }
            AssertProjection();life.World.Pan(new Vector2(36,-20));AssertProjection();
            var fountain=Flow.GetComponentInChildren<TownFountainGraphic>();
            Assert.That(fountain.mainTexture,Is.SameAs(Resources.Load<Texture2D>(TownFountainGraphic.Resource)),"Never reuse an opaque meadow crop over the new stone");
            Assert.That(fountain.raycastTarget,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CentralGardensReplaceTheirVisibleStageWithoutChangingProgress()
        {
            yield return EnterHome();var life=Flow.GetComponentInChildren<TownLifeView>();var scene=life.GetComponent<TownSceneryView>();
            life.World.Camera.Jump(new TownPoint(0,-.25f));life.World.Refresh();Canvas.ForceUpdateCanvases();
            string before=StateCodec.Encode(Flow.Game.State);int writes=Store.Writes;
            foreach(int level in new[]{1,3,6})
            {
                scene.SetTownLevel(level);
                foreach(float side in new[]{-1f,1f})
                {
                    var bank=scene.Pieces.Where(p=>TownCentralMasterLayout.Dressing.Contains(p.Placement)&&Mathf.Abs(p.Placement.Feet.X-side*.50f)<.001f&&Mathf.Abs(p.Placement.Feet.Y+.89f)<.001f).ToArray();
                    Assert.That(bank.Count(p=>p.Rect.gameObject.activeSelf),Is.EqualTo(1));
                    Assert.That(bank.Single(p=>p.Rect.gameObject.activeSelf).Placement.Level,Is.EqualTo(level==1?0:level));
                }
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
