using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class QualityPassStoryGrowthTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator BodyUsesKleeWhileButtonsCountsAndLevelsKeepUiFonts()
        {
            yield return EnterHome();Flow.ShowQuest();Canvas.ForceUpdateCanvases();
            var body=Resources.Load<Font>("Mofumachi/UIFontBody");Assert.That(body,Is.Not.Null);
            var instruction=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Quest instruction");
            Assert.That(instruction.font,Is.EqualTo(body));Assert.That(instruction.text,Is.EqualTo(QuestStoryPresentation.For("tea-01").Before));
            Assert.That(Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Accept quest").GetComponentInChildren<Text>().font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFontBold")));
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Required count 0").font,Is.EqualTo(Resources.Load<Font>("Mofumachi/UIFont")));
            foreach(int height in new[]{592,640,808})
            {
                var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=new Vector2(360,height);
                Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(new Vector2(360,height),new Rect(0,24,360,height-56));Canvas.ForceUpdateCanvases();
                foreach(var t in Flow.GetComponentsInChildren<Text>().Where(t=>t.font==body))Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+1),t.name);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EachRegionVisiblyGrowsAndDecorationNeverSavesOrIntercepts()
        {
            yield return EnterHome();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);var growth=Flow.GetComponentInChildren<TownGrowthView>();
            foreach(TownRegionId region in System.Enum.GetValues(typeof(TownRegionId)))
            {
                growth.SetRegion(region);growth.Apply(0);Assert.That(Flow.GetComponentsInChildren<TownGrowthPropGraphic>().Length,Is.Zero);
                growth.Apply(6);var props=Flow.GetComponentsInChildren<TownGrowthPropGraphic>();Assert.That(props.Length,Is.EqualTo(6),region.ToString());
                Assert.That(props.All(p=>!p.raycastTarget),Is.True);
                Assert.That(props.Select(p=>p.Region).Distinct(),Is.EqualTo(new[]{region}));
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DeliveryShowsPersonalThanksAndReunionSurvivesReloadWithoutRewardReplay()
        {
            yield return EnterHome();Assert.That(Flow.AcceptQuest(),Is.True);Assert.That(Flow.DropItem(0,1),Is.True);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Flow.Deliver().Success,Is.True);
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Delivery thanks").text,Is.EqualTo(QuestStoryPresentation.For("tea-01").Thanks));
            yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();
            Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Growth region ")),Is.EqualTo(3));
            Assert.That(Flow.GetComponentsInChildren<TownGrowthView>().Length,Is.EqualTo(2),"Compare the same region before and after the delivery");
            Flow.ContinueAfterGrowth();Flow.Initialize(Store);Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            life.Regions.LookAtRegion(TownRegionId.West);int writes=Store.Writes;life.Actors.Single(a=>a.CharacterId=="lulu").React();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble").text,Does.Contain(QuestStoryPresentation.For("tea-01").Reunion));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(30));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(1));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
    }
}
