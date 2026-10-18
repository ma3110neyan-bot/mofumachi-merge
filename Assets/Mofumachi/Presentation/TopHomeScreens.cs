using UnityEngine;
using UnityEngine.UI;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public static class TopHomeScreens
    {
        // Latest user-specified canonical names, in the original collage's order.
        private static readonly string[] Names={"モモ","ルル","ポム","フィオ","ミエル","ノア"};
        public static void BuildTitle(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var settings=w.Button("Settings","",root,new Rect(.82f,.91f,.14f,.07f),c.Flow.ShowSettings,true); var sr=(RectTransform)settings.transform;sr.anchorMin=sr.anchorMax=Vector2.one;sr.pivot=Vector2.one;sr.anchoredPosition=new Vector2(-12,-10);sr.sizeDelta=new Vector2(48,48);
            w.Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.26f,.26f,.48f,.48f));
            var logo=UIWidgets.Node("Logo",root,new Rect(.04f,.645f,.92f,.24f));
            var logoImage=UIWidgets.Node("Title logo artwork",logo,new Rect(0,0,1,1)).gameObject.AddComponent<RawImage>();
            logoImage.texture=Resources.Load<Texture2D>("Mofumachi/TitleLogo");logoImage.raycastTarget=false;
            var logoFit=logoImage.gameObject.AddComponent<AspectRatioFitter>();logoFit.aspectRatio=1659f/948;logoFit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            var tag=w.OrnatePanel("Tagline",root,new Rect(.06f,.589f,.88f,.048f),UIWidgets.Cream,10).rectTransform;
            w.StoryLabel("Tagline text","もふもふの街で、ちいさなお茶会を。",tag,new Rect(.05f,0,.9f,1),14);
            var hero=UIWidgets.Node("Title hero region",root,new Rect(.37f,0,.26f,.575f));hero.offsetMin=new Vector2(0,238);
            string favorite=CharacterCatalog.IsKnown(c.Game.State.favoriteCharacterId)?c.Game.State.favoriteCharacterId:"momo";
            var image=w.Body("Title partner",favorite,hero,new Rect(0,0,1,1));image.raycastTarget=true;image.gameObject.AddComponent<BodyReaction>().Bind(c);
            var companions=UIWidgets.Node("Title companions",root,new Rect(0,0,1,1));UIWidgets.Bottom(companions,20,170,20,56);
            int slot=0;
            foreach(var character in CharacterCatalog.All)
            {
                if(character.Id==favorite)continue;
                AttachReaction(w.Character("Character "+character.PortraitIndex,character.PortraitIndex,companions,new Rect(slot*.2f+.018f,0,.164f,1)),c);slot++;
            }
            var start=w.Button("Start","はじめる",root,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);c.Flow.BeginGame();}); UIWidgets.Bottom((RectTransform)start.transform,28,54,28,58);
            var footerSurface=w.Panel("Title footer surface",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.94f));UIWidgets.Bottom(footerSurface.rectTransform,24,14,24,28);
            var footer=w.StoryLabel("Title footer","つくって、届けて、街を育てよう。",root,new Rect(0,0,1,1),14); UIWidgets.Bottom(footer.rectTransform,14,14,14,28);
            var feedback=w.Label("Title feedback",c.Flow.FeedbackFor(ScreenId.Title),root,new Rect(0,0,1,1),12);UIWidgets.Bottom(feedback.rectTransform,16,118,16,42);
        }
        public static void BuildHome(RectTransform root,ScreenContext c,RawImage background=null)
        {
            var w=c.Widgets;c.Flow.Daily.EnsureDay(System.DateTime.Now);
            // The street is the main view. Only two small floating bars stay open.
            var world=UIWidgets.Node("Town characters",root,new Rect(0,0,1,1));world.offsetMin=new Vector2(0,62);world.offsetMax=new Vector2(0,-80);
            var life=world.gameObject.AddComponent<TownLifeView>();life.Bind(c,background??c.Flow.TownBackground);
            world.gameObject.AddComponent<TownContinuousWorldView>().Bind(c,life);
            world.gameObject.AddComponent<TownViewportView>().Bind(c,background??c.Flow.TownBackground,life);
            var decorations=UIWidgets.Node("Town growth decorations",world,new Rect(0,0,1,1));decorations.SetAsFirstSibling();
            var growth=decorations.gameObject.AddComponent<TownGrowthView>();growth.Bind(c,life);growth.Apply(c.Game.State.townGrowthLevel);
            var hud=w.Panel("HUD",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.94f)).rectTransform;UIWidgets.Top(hud,8,6,8,44);
            var today=w.Button("Today town","今日",hud,new Rect(0,0,.18f,1),c.Flow.ShowToday,true);
            today.GetComponentInChildren<Text>().fontSize=11;
            var todayNew=w.Label("Today NEW","",(RectTransform)today.transform,new Rect(.54f,.76f,.65f,.24f),9);todayNew.color=new Color(.71f,.24f,.38f);
            today.gameObject.AddComponent<TownTodayBadge>().Bind(c,today.GetComponentInChildren<Text>(),todayNew);
            w.Label("Town value","街 Lv."+c.Game.State.townGrowthLevel,hud,new Rect(.20f,.05f,.24f,.9f),13);
            w.Icon("Coin",UIIcon.Coin,hud,new Rect(.44f,.22f,.075f,.56f));
            w.Heading("Coins value",c.Game.State.coins.ToString("N0"),hud,new Rect(.52f,.08f,.29f,.84f),13);
            var menu=w.Button("Town menu","メニュー",hud,new Rect(.82f,0,.18f,1),c.Flow.OpenTownMenu,true);menu.GetComponentInChildren<Text>().fontSize=11;
            world.gameObject.AddComponent<TownRegionView>().Bind(c,background??c.Flow.TownBackground,life,life.Regions);
            world.gameObject.AddComponent<TownRegionControls>().Bind(c,root,life);
            // Compact next-step card replaces the old permanently expanded chapter panel.
            var bar=w.Panel("Navigation",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.94f)).rectTransform;UIWidgets.Bottom(bar,8,6,8,50);
            var goal=TownNextGoal.For(c.Game.State);bool finale=ChapterProgress.For(c.Game.State).AllCompleted;
            var quest=w.Panel("Home quest",bar,new Rect(0,0,.59f,1),new Color(1,1,1,0)).rectTransform;quest.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            var title=w.StoryLabel("Next goal title",finale?"今日のお願い "+c.Flow.Daily.CompletedGoals+" / 3":goal.Title,quest,new Rect(.05f,.50f,.90f,.45f),12);
            var hint=w.Label("Town growth hint",finale?c.Flow.Daily.NextGoal:goal.Detail,quest,new Rect(.05f,.06f,.90f,.42f),10);
            quest.gameObject.AddComponent<TownDailyHomeHint>().Bind(c,finale,title,hint);
            var tap=quest.gameObject.AddComponent<Button>();tap.targetGraphic=quest.GetComponent<RoundedPanelGraphic>();tap.onClick.AddListener(()=>{if(finale)c.Flow.ShowToday();else life.GuideToFacility("quest");});
            var merge=w.Button("Nav Merge","マージ",bar,new Rect(.60f,0,.25f,1),c.Flow.ShowMerge);merge.GetComponentInChildren<Text>().fontSize=13;
            var more=w.Button("Town menu bottom","…",bar,new Rect(.86f,0,.14f,1),c.Flow.OpenTownMenu,true);
            more.GetComponentInChildren<Text>().fontSize=22;
            life.Regions.RestoreSnapshot(c.Flow.HomeRegionSnapshot);life.World.Restore(c.Flow.HomeWorldSnapshot);
            world.gameObject.AddComponent<TownStreetWalkView>().Bind(c,life);
            string message=c.Flow.FeedbackFor(ScreenId.Home);
            if(message.Length>0){var error=w.Panel("Home feedback surface",root,new Rect(0,0,1,1),UIWidgets.Cream).rectTransform;UIWidgets.Top(error,12,84,12,52);w.Label("Feedback",message,error,new Rect(.04f,.03f,.92f,.94f),13);}
            TownTodayView.Welcome(root,c);
        }
        private static void AttachReaction(RawImage image,ScreenContext c)
        {
            var frame=image.transform.parent.parent.gameObject;var graphic=frame.GetComponent<RoundedPanelGraphic>();graphic.raycastTarget=true;
            var b=frame.AddComponent<Button>();b.transition=Selectable.Transition.None;b.targetGraphic=graphic;
            var reaction=frame.AddComponent<CharacterReaction>();b.onClick.AddListener(()=>{c.Flow.PlayCue(AudioCue.Character);reaction.React();});
        }
    }
}
