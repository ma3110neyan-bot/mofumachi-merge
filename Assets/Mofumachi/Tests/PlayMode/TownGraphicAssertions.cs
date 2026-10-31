using System.Collections.Generic;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    internal static class TownGraphicAssertions
    {
        public static void NativeMasking(IEnumerable<TownActor> actors,RectMask2D mask)
        {
            int active=0;
            foreach(var actor in actors)
            foreach(var graphic in actor.GetComponentsInChildren<MaskableGraphic>())
            {
                string context=actor.CharacterId+" / "+graphic.name;
                Assert.That(graphic.maskable,Is.True,context);
                Assert.That(graphic.GetComponentInParent<RectMask2D>(),Is.SameAs(mask),context);
                // Disabled legacy shadows unregister from RectMask2D by design.
                Assert.That(graphic.canvasRenderer.hasRectClipping,Is.EqualTo(graphic.isActiveAndEnabled),context);
                if(graphic.isActiveAndEnabled)active++;
            }
            Assert.That(active,Is.GreaterThan(0),"The clipping check must inspect enabled town graphics.");
        }
    }
}
