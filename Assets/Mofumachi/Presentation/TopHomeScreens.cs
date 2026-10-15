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
            var w=c.Widgets;
            var hud=w.OrnatePanel("HUD",root,new Rect(0,0,1,1),UIWidgets.Cream,18).rectTransform; UIWidgets.Top(hud,10,8,10,48);
            w.Icon("Town star",UIIcon.Star,hud,new Rect(.02f,.2f,.1f,.6f));
            w.Label("Town value","街 Lv."+c.Game.State.townGrowthLevel,hud,new Rect(.13f,.05f,.28f,.9f),14);
            w.Icon("Coin",UIIcon.Coin,hud,new Rect(.41f,.2f,.1f,.6f));
            w.Heading("Coins value",c.Game.State.coins.ToString("N0"),hud,new Rect(.51f,.08f,.32f,.84f),14);
            var settings=w.Button("Settings","",hud,new Rect(.85f,0,.15f,1),c.Flow.ShowSettings,true); w.Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.2f,.2f,.6f,.6f));
            var world=UIWidgets.Node("Town characters",root,new Rect(0,0,1,1));world.offsetMin=new Vector2(0,178);world.offsetMax=new Vector2(0,-142);
            var life=world.gameObject.AddComponent<TownLifeView>();life.Bind(c,background??c.Flow.TownBackground);
            world.gameObject.AddComponent<TownViewportView>().Bind(c,background??c.Flow.TownBackground,life);
            var decorations=UIWidgets.Node("Town growth decorations",world,new Rect(0,0,1,1));decorations.SetAsFirstSibling();
            var growth=decorations.gameObject.AddComponent<TownGrowthView>();growth.Bind(c,life);growth.Apply(c.Game.State.townGrowthLevel);
            var next=QuestCatalog.NextFor(c.Game.State);var requester=CharacterCatalog.Find(next?.RequesterId??c.Game.State.favoriteCharacterId);
            var quest=w.OrnatePanel("Home quest",root,new Rect(0,0,1,1),null,24).rectTransform; UIWidgets.Bottom(quest,14,78,14,96);
            var chapter=ChapterProgress.For(c.Game.State);bool unknown=next==null&&!chapter.AllCompleted;
            w.StoryHeading("Home quest title",chapter.CurrentChapter?.Title??"最初の物語をふりかえる",quest,new Rect(.035f,.74f,.75f,.23f),14);
            w.Label("Chapter progress",chapter.CurrentChapter==null?"6 / 6":chapter.CompletedInCurrent+" / 2",quest,new Rect(.81f,.74f,.15f,.23f),12);
            string goal=unknown?UIStrings.UnknownQuest:chapter.AllCompleted?"みんなのお茶会がひらけました！":chapter.CompletedCount==0&&c.Game.State.activeQuestId.Length==0?"仲間とお茶会をひらこう":requester.Name+"："+next.Title;
            w.StoryLabel("Home quest summary",goal,quest,new Rect(.04f,.49f,.92f,.23f),14);
            var nextGoal=TownNextGoal.For(c.Game.State);
            w.StoryLabel("Next goal title",nextGoal.Title,quest,new Rect(.04f,.27f,.92f,.20f),12);
            w.Label("Town growth hint",nextGoal.Detail,quest,new Rect(.04f,.01f,.92f,.24f),12);
            if(chapter.LastCompletedChapter!=null)
            {
                quest.Find("Home quest title").GetComponent<RectTransform>().anchorMax=new Vector2(.68f,.97f);
                var count=quest.Find("Chapter progress").GetComponent<RectTransform>();count.anchorMin=new Vector2(.68f,.74f);count.anchorMax=new Vector2(.79f,.97f);
                foreach(string label in new[]{"Home quest summary","Town growth hint","Next goal title"}){var r=quest.Find(label).GetComponent<RectTransform>();r.anchorMax=new Vector2(.79f,r.anchorMax.y);}
                var replay=w.Button("Review chapter","おも\nいで",quest,new Rect(1,.5f,0,0),()=>c.Flow.ReviewLastChapter(),true);
                var replayRect=(RectTransform)replay.transform;replayRect.pivot=new Vector2(1,.5f);replayRect.anchoredPosition=new Vector2(-8,0);replayRect.sizeDelta=new Vector2(48,48);replay.GetComponentInChildren<Text>().fontSize=12;
            }
            var tap=quest.gameObject.AddComponent<Button>();quest.GetComponent<RoundedPanelGraphic>().raycastTarget=true;tap.targetGraphic=quest.GetComponent<RoundedPanelGraphic>();tap.onClick.AddListener(()=>{if(chapter.AllCompleted)c.Flow.ReviewLastChapter();else c.Flow.ShowQuest();});
            w.Nav(root,c,ScreenId.Home);
            string message=c.Flow.FeedbackFor(ScreenId.Home);
            if(message.Length>0){var error=w.Panel("Home feedback surface",root,new Rect(0,0,1,1),UIWidgets.Cream).rectTransform;UIWidgets.Top(error,12,144,12,64);w.Label("Feedback",message,error,new Rect(.04f,.03f,.92f,.94f),14);}
            world.gameObject.AddComponent<TownRegionView>().Bind(c,background??c.Flow.TownBackground,life,life.Regions);
            world.gameObject.AddComponent<TownRegionControls>().Bind(c,root,life);
            life.Regions.RestoreSnapshot(c.Flow.HomeRegionSnapshot);
            world.gameObject.AddComponent<TownStreetWalkView>().Bind(c,life);
        }
        private static void AttachReaction(RawImage image,ScreenContext c)
        {
            var frame=image.transform.parent.parent.gameObject;var graphic=frame.GetComponent<RoundedPanelGraphic>();graphic.raycastTarget=true;
            var b=frame.AddComponent<Button>();b.transition=Selectable.Transition.None;b.targetGraphic=graphic;
            var reaction=frame.AddComponent<CharacterReaction>();b.onClick.AddListener(()=>{c.Flow.PlayCue(AudioCue.Character);reaction.React();});
        }
    }
}
