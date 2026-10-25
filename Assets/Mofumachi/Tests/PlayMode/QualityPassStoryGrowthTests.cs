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
                foreach(var t in Flow.GetComponentsInChildren<Text>().Where(t=>t.text.Length>0))Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+1),t.name);
            }
            foreach(bool finished in new[]{false,true})
            {
                Flow.Game.State.completedQuestIds.Clear();if(finished)Flow.Game.State.completedQuestIds.AddRange(QuestCatalog.All.Select(q=>q.Id));
                Flow.Game.State.activeQuestId=finished?"":"unknown-quest";Flow.ShowQuest();Canvas.ForceUpdateCanvases();
                var title=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Quest title");Assert.That(title.preferredHeight,Is.LessThanOrEqualTo(title.rectTransform.rect.height+1));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EachRegionVisiblyGrowsAndDecorationNeverSavesOrIntercepts()
        {
            yield return EnterHome();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);var growth=Flow.GetComponentInChildren<TownGrowthView>();
            foreach(int height in new[]{592,640,808})
            {
                var life=FrameTownPortrait(height);
                foreach(TownRegionId region in System.Enum.GetValues(typeof(TownRegionId)))
                {
                    life.Regions.LookAtRegion(region);growth.Apply(0);Assert.That(Flow.GetComponentsInChildren<TownGrowthPropGraphic>().Length,Is.Zero);
                    growth.Apply(1);Assert.That(life.GetComponentsInChildren<TownGrowthPropGraphic>().Any(p=>p.Region==region&&p.Prop==TownGrowthProp.Flowers),Is.True,region+" first flowers at height "+height);
                    growth.Apply(3);Assert.That(life.GetComponentsInChildren<TownGrowthPropGraphic>().Any(p=>p.Region==region&&p.Prop==TownGrowthProp.Lamp),Is.True,region+" new lamp at height "+height);
                    growth.Apply(6);var props=life.GetComponentsInChildren<TownGrowthPropGraphic>(true).Where(p=>p.Region==region).ToArray();
                    Assert.That(props.Length,Is.EqualTo(TownGrowthPresentation.For(region,6).Count));Assert.That(props.Length,Is.GreaterThanOrEqualTo(12));
                    Assert.That(props.All(p=>!p.raycastTarget),Is.True);Assert.That(props.Any(p=>p.gameObject.activeInHierarchy),Is.True,region+" must show growth at height "+height);
                }
            }
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DeliveryShowsPersonalThanksAndReunionSurvivesReloadWithoutRewardReplay()
        {
            yield return EnterHome();Assert.That(Flow.AcceptQuest(),Is.True);Assert.That(Flow.DropItem(0,1),Is.True);yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Flow.Deliver().Success,Is.True);
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Delivery thanks").text,Is.EqualTo(QuestStoryPresentation.For("tea-01").Thanks));
            yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();
            Assert.That(Flow.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Growth region ")),Is.EqualTo(5));
            Assert.That(Flow.GetComponentsInChildren<TownGrowthView>().Length,Is.EqualTo(2),"Compare the same region before and after the delivery");
            Flow.ContinueAfterGrowth();Flow.Initialize(Store);Flow.ShowHome();var life=Flow.GetComponentInChildren<TownLifeView>();
            life=FrameTownPortrait();var requester=life.Actors.Single(a=>a.CharacterId=="lulu");
            // Lulu now gathers in Central. Frame her actual position before
            // tapping; offscreen residents intentionally cannot be tapped.
            life.World.LookAt(TownDistrictLayout.At(requester.WorldPosition));
            life.World.Camera.Jump(new TownPoint(requester.WorldPosition.X,requester.WorldPosition.Y+.18f));
            life.World.Refresh();life.AdvanceFrame(1f/30);Canvas.ForceUpdateCanvases();
            Assert.That(requester.RegionVisible,Is.True);int writes=Store.Writes;requester.React();
            Assert.That(Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Town bubble").text,Does.Contain(QuestStoryPresentation.For("tea-01").Reunion));
            Assert.That(Flow.Game.State.coins,Is.EqualTo(30));Assert.That(Flow.Game.State.claimedRewardIds.Count,Is.EqualTo(1));Assert.That(Store.Writes,Is.EqualTo(writes));LogAssert.NoUnexpectedReceived();
        }
        private static Rect Bounds(RectTransform root,RectTransform item)
        {var corners=new Vector3[4];item.GetWorldCorners(corners);var min=(Vector2)root.InverseTransformPoint(corners[0]);var max=(Vector2)root.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
        [UnityTest] public IEnumerator EveryDeliveryAndGrowthStoryFitsShortSafePortraits()
        {
            yield return EnterHome();Assert.That(Flow.AcceptQuest(),Is.True);Assert.That(Flow.DropItem(0,1),Is.True);yield return new WaitForSecondsRealtime(.3f);Assert.That(Flow.Deliver().Success,Is.True);
            var canvas=new GameObject("Quality story layout fixture",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var root=(RectTransform)canvas.transform;var context=new ScreenContext(Flow,new UIWidgets(Resources.Load<Font>("Mofumachi/UIFont"),Resources.Load<Texture2D>("Mofumachi/CharacterMaster")));
            void Check(bool grown)
            {
                foreach(int height in new[]{536,752})foreach(var quest in QuestCatalog.All)
                {
                    root.sizeDelta=new Vector2(360,height);var page=UIWidgets.Node("Story layout sample",root,new Rect(0,0,1,1));var delivery=new DeliveryPresentation(quest,quest.Coins,QuestCatalog.All.ToList().IndexOf(quest),QuestCatalog.All.ToList().IndexOf(quest)+1);
                    if(grown)RewardGrowthScreens.BuildGrowth(page,context,delivery);else RewardGrowthScreens.BuildResult(page,context,delivery);Canvas.ForceUpdateCanvases();
                    // Measure screen copy, not the best-fit architectural
                    // lettering inside the scaled before/after town previews.
                    // preferredHeight measures that tiny sign at its unshrunk
                    // font size; its world geometry is not story paragraph UI.
                    foreach(var t in page.GetComponentsInChildren<Text>().Where(t=>t.text.Length>0&&t.GetComponentInParent<TownSceneryView>()==null))Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+1),quest.Id+" "+t.name);
                    var description=page.GetComponentsInChildren<Text>().Single(t=>t.name==(grown?"Growth description":"Reward description"));
                    if(!grown){var next=page.GetComponentsInChildren<Button>().Single(b=>b.name=="Show growth");Assert.That(Bounds(page,next.transform as RectTransform).Overlaps(Bounds(page,description.rectTransform)),Is.False);}
                    Object.DestroyImmediate(page.gameObject);
                }
            }
            try{Check(false);yield return new WaitForSecondsRealtime(1.3f);Check(false);Check(true);}finally{Object.DestroyImmediate(canvas);}
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GrowthSaveErrorKeepsAnUnobscuredUsableReturnButton()
        {
            yield return EnterHome();Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);Assert.That(Flow.Deliver().Success,Is.True);yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();
            Store.Fail=true;Flow.ShowHome();Canvas.ForceUpdateCanvases();
            var button=Flow.GetComponentsInChildren<Button>().Single(b=>b.name=="Return town");var root=button.transform.parent as RectTransform;
            var text=Flow.GetComponentsInChildren<Text>().Single(t=>t.name=="Feedback");var surface=root.Find("Feedback surface") as RectTransform;
            Assert.That(button.interactable,Is.True);Assert.That(Bounds(root,button.transform as RectTransform).Overlaps(Bounds(root,text.rectTransform)),Is.False);
            Assert.That(Bounds(root,button.transform as RectTransform).Overlaps(Bounds(root,surface)),Is.False);
            Store.Fail=false;button.onClick.Invoke();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Home));Assert.That(Flow.Game.State.coins,Is.EqualTo(30));LogAssert.NoUnexpectedReceived();
        }
    }
}
