using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Mofumachi.Tests
{
    public sealed class PresentationContentTests
    {
        [Test] public void RequiredJapaneseTextHasGlyphs()
        {
            var font = Resources.Load<Font>("Mofumachi/UIFont");
            Assert.That(font, Is.Not.Null);
            foreach (char c in string.Join("", UIStrings.All).Concat("可年必方相者許課談護量金").Distinct())
                if (!char.IsWhiteSpace(c)) Assert.That(font.HasCharacter(c), Is.True, "Missing glyph: " + c);
        }
    }
}
