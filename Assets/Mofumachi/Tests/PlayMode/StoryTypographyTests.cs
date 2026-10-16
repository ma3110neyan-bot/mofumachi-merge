using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Mofumachi.Tests
{
    public sealed class StoryTypographyTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator StoryRolesAndNoticeFitPortraitSizes()
        {
            yield return LoadTitle(); Flow.BeginGame();
            var notice = Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Notice body");
            Assert.That(notice.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFont")));
            Assert.That(notice.text.Replace("\n",""),Is.EqualTo(UIStrings.NoticeBody));
            Assert.That(notice.text,Does.Contain("保護者"));
            Assert.That(Flow.ConfirmPurchaseNotice(),Is.True);yield return null;
            var story=Resources.Load<Font>("Mofumachi/UIFontStory");Assert.That(story,Is.Not.Null);
            foreach(int height in new[]{592,640,808})
            {
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,0,360,height));
                Canvas.ForceUpdateCanvases();
                foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Chosen partner name"||t.name=="Partner description"||t.name.StartsWith("Candidate name")))
                {
                    Assert.That(text.font,Is.EqualTo(text.name=="Partner description"?Resources.Load<Font>("Mofumachi/UIFontBody"):story),text.name);
                    Assert.That(text.resizeTextForBestFit,Is.False);
                    Assert.That(text.lineSpacing,Is.EqualTo(text.name=="Partner description"?1.28f:1.2f));
                    Assert.That(text.fontSize,Is.GreaterThanOrEqualTo(text.name.StartsWith("Candidate")?12:14));
                    Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);
                }
            }
            var confirm=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Confirm partner");
            Assert.That(confirm.GetComponentInChildren<Text>().font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontBold")));
            confirm.onClick.Invoke();yield return null;Flow.ShowSettings();
            foreach(int height in new[]{592,808})
            {
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,0,360,height));Canvas.ForceUpdateCanvases();
                foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Page title"||t.name=="Sound settings title"||t.name=="Settings description"))
                {
                    Assert.That(text.font,Is.EqualTo(text.name=="Settings description"?Resources.Load<Font>("Mofumachi/UIFontBody"):story),text.name);Assert.That(text.resizeTextForBestFit,Is.False);
                    Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);
                }
            }
            Flow.GoBack();Flow.ShowMerge();Flow.OpenMergeActions();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge menu title").font,Is.EqualTo(story));
            Flow.CancelMergeActions();Flow.ShowQuest();
            foreach(var t in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Quest title"||t.name.StartsWith("Required name "))){Assert.That(t.font,Is.EqualTo(t.name=="Quest title"?story:Resources.Load<Font>("Mofumachi/UIFont")));Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+1));}
            Flow.ShowMerge();foreach(var t in Flow.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Requirement name "))){Assert.That(t.font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFont")));Assert.That(t.fontSize,Is.GreaterThanOrEqualTo(14));Assert.That(t.resizeTextForBestFit,Is.False);}
            LogAssert.NoUnexpectedReceived();
        }
    }
}
