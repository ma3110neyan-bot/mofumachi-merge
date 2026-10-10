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
            var logo=w.Panel("Logo",root,new Rect(.09f,.65f,.82f,.22f),new Color(1,1,1,.92f)).rectTransform;w.Flowers(logo);
            var line1=w.Heading("Logo first","もふまち",logo,new Rect(.05f,.47f,.9f,.47f),40); line1.color=new Color(.91f,.28f,.49f);
            var line2=w.Heading("Logo second","メルジュ",logo,new Rect(.05f,.015f,.9f,.47f),40); line2.color=new Color(.23f,.62f,.72f);
            var tag=w.Panel("Tagline",root,new Rect(.06f,.589f,.88f,.048f),UIWidgets.Cream).rectTransform;
            w.Label("Tagline text","もふもふの街で、ちいさなお茶会を。",tag,new Rect(0,0,1,1),14);
            var hero=UIWidgets.Node("Title hero region",root,new Rect(.37f,0,.26f,.575f));hero.offsetMin=new Vector2(0,238);
            var image=w.Character("Character 0",0,hero,new Rect(0,0,1,1));AttachReaction(image,c);
            var companions=UIWidgets.Node("Title companions",root,new Rect(0,0,1,1));UIWidgets.Bottom(companions,20,170,20,56);
            for(int i=1;i<6;i++)AttachReaction(w.Character("Character "+i,i,companions,new Rect((i-1)*.2f+.018f,0,.164f,1)),c);
            var start=w.Button("Start","はじめる",root,new Rect(0,0,1,1),()=>{c.Flow.PlayCue(AudioCue.Confirm);c.Flow.BeginGame();}); UIWidgets.Bottom((RectTransform)start.transform,28,54,28,58);
            var footerSurface=w.Panel("Title footer surface",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.94f));UIWidgets.Bottom(footerSurface.rectTransform,24,14,24,28);
            var footer=w.Label("Title footer","つくって、届けて、街を育てよう。",root,new Rect(0,0,1,1),12); UIWidgets.Bottom(footer.rectTransform,14,14,14,28);
            var feedback=w.Label("Title feedback",c.Flow.FeedbackFor(ScreenId.Title),root,new Rect(0,0,1,1),12);UIWidgets.Bottom(feedback.rectTransform,16,118,16,42);
        }
        public static void BuildHome(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;
            var hud=w.Panel("HUD",root,new Rect(0,0,1,1),UIWidgets.Cream).rectTransform; UIWidgets.Top(hud,10,8,10,64);
            w.Icon("Town star",UIIcon.Star,hud,new Rect(.04f,.55f,.08f,.34f));
            w.Label("Town value","街 Lv."+c.Game.State.townGrowthLevel,hud,new Rect(.14f,.48f,.64f,.48f),15);
            w.Icon("Coin",UIIcon.Coin,hud,new Rect(.04f,.08f,.08f,.34f));
            w.Heading("Coins value",c.Game.State.coins.ToString("N0"),hud,new Rect(.14f,.01f,.64f,.46f),19);
            var settings=w.Button("Settings","",hud,new Rect(.82f,.1f,.16f,.8f),c.Flow.ShowSettings,true); w.Icon("Settings icon",UIIcon.Settings,(RectTransform)settings.transform,new Rect(.17f,.17f,.66f,.66f));
            var world=UIWidgets.Node("Town characters",root,new Rect(0,0,1,1));world.offsetMin=new Vector2(0,228);world.offsetMax=new Vector2(0,-94);
            var points=new[]{new Vector2(.1f,.69f),new Vector2(.72f,.73f),new Vector2(.72f,.41f),new Vector2(.72f,.12f),new Vector2(.1f,.12f),new Vector2(.41f,.12f)};
            for(int i=0;i<6;i++)
            {
                var pos=points[i];var image=w.Character("Character "+i,i,world,new Rect(pos.x,pos.y,.16f,.18f));AttachReaction(image,c);
                var name=w.Panel("Name "+i,world,new Rect(pos.x-.025f,pos.y-.06f,.21f,.06f),UIWidgets.Cream).rectTransform;
                w.Label("Character name "+i,Names[i],name,new Rect(0,0,1,1),11);
            }
            bool complete=c.Game.State.completedQuestIds.Contains(QuestDefinition.First.Id);
            var quest=w.Panel("Home quest",root,new Rect(0,0,1,1)).rectTransform; UIWidgets.Bottom(quest,14,124,14,92);
            w.Character("Quest character",1,quest,new Rect(.025f,.13f,.2f,.74f));
            w.Heading("Home quest title","お茶会の準備",quest,new Rect(.24f,.54f,.72f,.38f),19);
            w.Label("Home quest summary",complete?"お茶会の準備ができました！":"お茶を合成して、仲間に届けよう",quest,new Rect(.24f,.08f,.72f,.44f),13);
            var tap=quest.gameObject.AddComponent<Button>();quest.GetComponent<RoundedPanelGraphic>().raycastTarget=true;tap.targetGraphic=quest.GetComponent<RoundedPanelGraphic>();tap.onClick.AddListener(c.Flow.ShowQuest);
            w.Feedback(root,c,ScreenId.Home); w.Nav(root,c,ScreenId.Home);
        }
        private static void AttachReaction(RawImage image,ScreenContext c)
        {
            var frame=image.transform.parent.parent.gameObject;var graphic=frame.GetComponent<RoundedPanelGraphic>();graphic.raycastTarget=true;
            var b=frame.AddComponent<Button>();b.transition=Selectable.Transition.None;b.targetGraphic=graphic;
            var reaction=frame.AddComponent<CharacterReaction>();b.onClick.AddListener(()=>{c.Flow.PlayCue(AudioCue.Character);reaction.React();});
        }
    }
}
