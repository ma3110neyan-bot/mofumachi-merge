using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class CharacterSelectionScreen
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"キャラクター図鑑",UIWidgets.RosePaper);
            var chosen=CharacterCatalog.Find(c.Flow.PreviewFavoriteId)??CharacterCatalog.All[0];var entry=TownCharacterBook.For(chosen.Id,c.Game.State,c.Flow.Daily.Data);
            c.Flow.Daily.MarkBookSeen(chosen.Id);
            var discovery=w.StoryPanel("Book discovery bookmark",root,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(discovery,14,70,14,72);
            var stats=w.Label("Book collection counts",Counts(entry),discovery,new Rect(.04f,.58f,.92f,.35f),12);
            var next=w.StoryLabel("Book next discovery",entry.IsNew?"NEW · "+entry.LatestDiscovery:entry.NextDiscovery,discovery,new Rect(.05f,.06f,.90f,.50f),13);
            var content=w.ScrollPaper("Partner content",root,174,150,1132,UIWidgets.BlushPaper);
            for(int i=0;i<6;i++)
            {
                var candidate=CharacterCatalog.All[i];int column=i%3,row=i/3;
                var button=w.Button("Partner "+candidate.Id,"",content,new Rect(column/3f+.025f,1,1/3f-.05f,0),()=>c.Flow.PreviewFavorite(candidate.Id),candidate.Id!=chosen.Id);
                var rect=(RectTransform)button.transform;rect.offsetMin=new Vector2(0,-10-row*76-70);rect.offsetMax=new Vector2(0,-10-row*76);
                button.GetComponent<RoundedPanelGraphic>().color=candidate.Id==chosen.Id?UIWidgets.RosePaper:UIWidgets.BluePaper;
                w.Character("Candidate portrait "+candidate.Id,candidate.PortraitIndex,rect,new Rect(.24f,.32f,.52f,.64f));w.StoryHeading("Candidate name "+candidate.Id,candidate.Name,rect,new Rect(.04f,.015f,.92f,.29f),12);
                if(TownCharacterBook.For(candidate.Id,c.Game.State,c.Flow.Daily.Data).IsNew)
                {var badge=w.Label("Book NEW "+candidate.Id,"NEW",rect,new Rect(.60f,.76f,.42f,.23f),10);badge.color=new Color(.71f,.24f,.38f);}
            }
            var name=w.StoryRibbon("Chosen partner name",chosen.Name+"のページ",content,new Rect(0,0,1,1),20);UIWidgets.Top((RectTransform)name.transform.parent,30,174,30,38);
            var hero=UIWidgets.Node("Partner hero",content,new Rect(0,0,1,1));UIWidgets.Top(hero,85,226,85,144);
            var body=w.Body("Chosen full body",chosen.Id,hero,new Rect(0,0,1,1));body.raycastTarget=true;var reaction=body.gameObject.AddComponent<BodyReaction>();reaction.Bind(c);
            string personality=entry.Personality.Replace("。","。\n").TrimEnd('\n');
            var description=w.NarrativeLabel("Partner description",personality,content,new Rect(0,0,1,1),15);UIWidgets.Top(description.rectTransform,20,382,20,68);
            var memory=w.StoryPanel("Book conversation paper",content,new Rect(0,0,1,1),true).rectTransform;UIWidgets.Top(memory,18,464,18,96);
            var quote=w.NarrativeLabel("Book conversation",TownCharacterDialogue.For(chosen.Id,entry.Meetings),memory,new Rect(.06f,.10f,.88f,.80f),15);
            string[] titles={"好きなもの","得意なこと","好きな場所"};string[] values={entry.Likes,entry.Talent,entry.FavoritePlace};string[] ids={"Book likes","Book talent","Book favorite place"};
            for(int i=0;i<3;i++)
            {
                var panel=w.StoryPanel("Book discovery "+i,content,new Rect(0,0,1,1)).rectTransform;UIWidgets.Top(panel,18,578+i*76,18,66);
                w.StoryLabel("Book field title "+i,titles[i],panel,new Rect(.05f,.60f,.9f,.31f),13);var text=w.NarrativeLabel(ids[i],values[i],panel,new Rect(.06f,.08f,.88f,.49f),14);
            }
            var heading=w.StoryRibbon("Book expressions title","見つけたリアクション",content,new Rect(0,0,1,1),16);UIWidgets.Top((RectTransform)heading.transform.parent,28,816,28,34);
            var react=w.NarrativeLabel("Book unlocked reactions",Reactions(entry),content,new Rect(0,0,1,1),14);UIWidgets.Top(react.rectTransform,18,866,18,54);
            var hint=w.StoryLabel("Book collection hint","街で会って、お話しよう♪\nおさんぽでも新しい一面が見つかるよ",content,new Rect(0,0,1,1),14);UIWidgets.Top(hint.rectTransform,18,936,18,66);
            var request=QuestCatalog.NextFor(c.Game.State);
            string connection=request!=null&&request.RequesterId==chosen.Id?chosen.Name+"のお願い「"+request.Title+"」\n依頼小屋 → Merge → お届けで、街も成長♪":entry.CompletedRequests>0?"この子に届けたお願い "+entry.CompletedRequests+"件\nお届けのおもいでも、このページに♪":"お願いとおさんぽで、\n仲間とのおもいでを重ねよう♪";
            var memories=w.NarrativeLabel("Book request connection",connection,content,new Rect(0,0,1,1),13);UIWidgets.Top(memories.rectTransform,18,1018,18,80);
            bool hasPartner=CharacterCatalog.IsKnown(c.Game.State.favoriteCharacterId);
            var location=w.NarrativeLabel("Book current location",hasPartner?c.Flow.TownCharacterDestination(chosen.Id):"お気に入りを選んで、街で会おう♪",root,new Rect(0,0,1,1),12);UIWidgets.Bottom(location.rectTransform,20,122,20,38);
            var talk=w.Button("Talk to character",chosen.Id==c.Game.State.favoriteCharacterId?"いっしょに、おさんぽへ":"会いに行く",root,new Rect(0,0,1,1),()=>
            {
                c.Flow.PlayCue(AudioCue.Character);c.Flow.VisitTownCharacter(chosen.Id);
            },true);UIWidgets.Bottom((RectTransform)talk.transform,28,70,28,48);talk.GetComponent<RoundedPanelGraphic>().color=UIWidgets.BluePaper;talk.interactable=hasPartner;
            var feedback=w.Label("Selection feedback",c.Flow.FeedbackFor(ScreenId.CharacterSelection),root,new Rect(0,0,1,1),13);UIWidgets.Bottom(feedback.rectTransform,20,122,20,38);feedback.gameObject.SetActive(feedback.text.Length>0);location.gameObject.SetActive(feedback.text.Length==0);
            var confirm=w.Button("Confirm partner","この子にする",root,new Rect(0,0,1,1),()=>c.Flow.ConfirmFavorite());UIWidgets.Bottom((RectTransform)confirm.transform,28,12,28,52);
        }
        private static string Counts(TownBookEntry e)=>"会話 "+e.Meetings+"回 · おさんぽ "+e.Outings+"回 · 依頼 "+e.CompletedRequests+"件";
        private static string Reactions(TownBookEntry e)=>(e.Reactions>=1?"笑顔":"？？？")+"　"+(e.Reactions>=2?"手を振る":"？？？")+"\n"+(e.Reactions>=3?"とっておきのお話":"？？？");
    }
}
