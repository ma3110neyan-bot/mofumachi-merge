using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class QualityPassMergeTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator ThirtyDishesKeepRectangularInputAndReadableLevelBadges()
        {
            yield return EnterHome();Flow.ShowMerge();Canvas.ForceUpdateCanvases();
            var dishes=Flow.GetComponentsInChildren<Graphic>().Where(g=>g.name=="Cell porcelain dish").ToArray();
            Assert.That(dishes.Length,Is.EqualTo(30));
            foreach(var dish in dishes){Assert.That(dish.raycastTarget,Is.False);Assert.That(dish.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(100));}
            Assert.That(Flow.GetComponentsInChildren<MergeBoardView>().Length,Is.EqualTo(30));
            foreach(var level in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Item level"))
            {Assert.That(level.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontBold")));Assert.That(level.rectTransform.rect.height,Is.GreaterThanOrEqualTo(level.preferredHeight-1));}
            LogAssert.NoUnexpectedReceived();
        }
    }
}
