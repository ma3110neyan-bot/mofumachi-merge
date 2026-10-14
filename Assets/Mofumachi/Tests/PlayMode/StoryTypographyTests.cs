using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Mofumachi.Core;
using UnityEngine.EventSystems;

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
                    Assert.That(text.font,Is.EqualTo(story),text.name);
                    Assert.That(text.resizeTextForBestFit,Is.False);
                    Assert.That(text.lineSpacing,Is.EqualTo(1.2f));
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
                foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Page title"||t.name=="Sound settings title"))
                {
                    Assert.That(text.font,Is.EqualTo(story),text.name);Assert.That(text.resizeTextForBestFit,Is.False);
                    Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);
                }
            }
            Flow.GoBack();Flow.ShowMerge();Flow.OpenMergeActions();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Merge menu title").font,Is.EqualTo(story));
            Flow.CancelMergeActions();Flow.ShowQuest();
            foreach(var t in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Quest title"||t.name.StartsWith("Required name "))){Assert.That(t.font,Is.EqualTo(story));Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+1));}
            Flow.ShowMerge();foreach(var t in Flow.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Requirement name "))){Assert.That(t.font,Is.EqualTo(story));Assert.That(t.fontSize,Is.GreaterThanOrEqualTo(14));Assert.That(t.resizeTextForBestFit,Is.False);}
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BodyTrialPreservesHeadingActionAndNumberRoles()
        {
            yield return EnterHome();
            var body=Resources.Load<Font>("Mofumachi/UIFontBody");Assert.That(body,Is.Not.Null);
            var story=Resources.Load<Font>("Mofumachi/UIFontStory");
            var regular=Resources.Load<Font>("Mofumachi/UIFont");var bold=Resources.Load<Font>("Mofumachi/UIFontBold");
            Flow.ShowQuest();Canvas.ForceUpdateCanvases();
            AssertBody(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Quest instruction"),body);
            foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>t.name=="Quest title"||t.name=="Requester"||t.name.StartsWith("Required name ")))
                Assert.That(text.font,Is.EqualTo(story),text.name);
            foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Required count ")))
                Assert.That(text.font,Is.EqualTo(regular),text.name);
            Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Accept quest").GetComponentInChildren<Text>().font,Is.EqualTo(bold));
            Flow.ShowSettings();Canvas.ForceUpdateCanvases();
            AssertBody(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Settings description"),body);
            foreach(var text in Flow.GetComponentsInChildren<Text>().Where(t=>t.name.EndsWith(" percent")))Assert.That(text.font,Is.EqualTo(regular));
            Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Return settings").GetComponentInChildren<Text>().font,Is.EqualTo(bold));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator BodyTrialFitsPortraitSafeAreasAndNpcReactionDoesNotSave()
        {
            yield return EnterHome();var body=Resources.Load<Font>("Mofumachi/UIFontBody");Assert.That(body,Is.Not.Null);
            foreach(int height in new[]{592,640,808})
            {
                SizeTrialCanvas(height);
                int writes=Store.Writes;int coins=Flow.Game.State.coins;int level=Flow.Game.State.townGrowthLevel;
                Flow.GetComponentsInChildren<TownActor>().First(a=>!a.Controlled).React();Canvas.ForceUpdateCanvases();
                AssertBody(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble"),body);
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Flow.Game.State.coins,Is.EqualTo(coins));Assert.That(Flow.Game.State.townGrowthLevel,Is.EqualTo(level));
            }
            Flow.ShowQuest();
            foreach(int height in new[]{592,640,808})
            {
                SizeTrialCanvas(height);
                var instruction=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Quest instruction");AssertBody(instruction,body);
                foreach(var quest in QuestCatalog.All){instruction.text=quest.Dialogue;Canvas.ForceUpdateCanvases();Assert.That(instruction.preferredHeight,Is.LessThanOrEqualTo(instruction.rectTransform.rect.height+1),quest.Id);}
                var accept=(RectTransform)Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Accept quest").transform;
                var viewport=Flow.GetComponentsInChildren<ScrollRect>().Single().viewport;var a=new Vector3[4];var b=new Vector3[4];accept.GetWorldCorners(a);viewport.GetWorldCorners(b);
                Assert.That(new Rect((Vector2)a[0],(Vector2)(a[2]-a[0])).Overlaps(new Rect((Vector2)b[0],(Vector2)(b[2]-b[0]))),Is.False,"Scrollable detail must not cover the accept action.");
                var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
                var reward=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Reward").transform);
                Assert.That(reward.min.y,Is.GreaterThanOrEqualTo(viewport.rect.yMin-1));Assert.That(reward.max.y,Is.LessThanOrEqualTo(viewport.rect.yMax+1));
                Assert.That(accept.rect.height,Is.GreaterThanOrEqualTo(48));
                var pointer=new PointerEventData(EventSystem.current){pointerId=-1,position=RectTransformUtility.WorldToScreenPoint(null,accept.TransformPoint(accept.rect.center))};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(accept.GetComponent<Button>()),"Pinned action must receive input after scrolling.");
            }
            Flow.ShowSettings();
            foreach(int height in new[]{592,640,808})
            {
                SizeTrialCanvas(height);
                AssertBody(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Settings description"),body);
            }
            LogAssert.NoUnexpectedReceived();
        }
        private void SizeTrialCanvas(int height)
        {
            var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;canvas.scaleFactor=1;
            var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(360,height);rect.position=new Vector3(180,height/2f,0);rect.localScale=Vector3.one;
            var layout=Flow.GetComponentInChildren<ResponsiveUILayout>();layout.ApplyViewport(new Vector2(360,height),new Rect(0,32,360,height-56));Canvas.ForceUpdateCanvases();
            Assert.That(layout.ContentRoot.rect.width,Is.EqualTo(360).Within(.1f));Assert.That(layout.ContentRoot.rect.height,Is.EqualTo(height-56).Within(.1f));
        }
        private static void AssertBody(Text text,Font body)
        {
            Assert.That(text.font,Is.EqualTo(body),text.name);Assert.That(text.fontSize,Is.EqualTo(16),text.name);
            Assert.That(text.alignment,Is.EqualTo(TextAnchor.UpperLeft),text.name);Assert.That(text.resizeTextForBestFit,Is.False,text.name);
            Assert.That(text.lineSpacing,Is.EqualTo(1.15f),text.name);Assert.That(text.raycastTarget,Is.False,text.name);
            Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);
        }
    }
}
