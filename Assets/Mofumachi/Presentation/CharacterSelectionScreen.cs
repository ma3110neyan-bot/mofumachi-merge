using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public static class CharacterSelectionScreen
    {
        public static void Build(RectTransform root,ScreenContext c)
        {
            var w=c.Widgets;w.Header(root,c,"キャラクター図鑑",UIWidgets.Pink);
            var content=w.ScrollPaper("Partner content",root,120,78,948,UIWidgets.BlushPaper);
            var chosen=CharacterCatalog.Find(c.Flow.PreviewFavoriteId)??CharacterCatalog.All[0];var entry=TownCharacterBook.For(chosen.Id,c.Game.State,c.Flow.Daily.Data);
            for(int i=0;i<6;i++)
            {
                var candidate=CharacterCatalog.All[i];int column=i%3,row=i/3;
                var button=w.Button("Partner "+candidate.Id,"",content,new Rect(column/3f+.025f,1,1/3f-.05f,0),()=>c.Flow.PreviewFavorite(candidate.Id),candidate.Id!=chosen.Id);
                var rect=(RectTransform)button.transform;rect.offsetMin=new Vector2(0,-10-row*76-70);rect.offsetMax=new Vector2(0,-10-row*76);
                w.Character("Candidate portrait "+candidate.Id,candidate.PortraitIndex,rect,new Rect(.24f,.32f,.52f,.64f));w.StoryHeading("Candidate name "+candidate.Id,candidate.Name,rect,new Rect(.04f,.015f,.92f,.29f),12);
            }
            var name=w.StoryRibbon("Chosen partner name",chosen.Name+"のページ",content,new Rect(0,0,1,1),20);UIWidgets.Top((RectTransform)name.transform.parent,30,174,30,38);
            var hero=UIWidgets.Node("Partner hero",content,new Rect(0,0,1,1));UIWidgets.Top(hero,85,224,85,144);
            var body=w.Body("Chosen full body",chosen.Id,hero,new Rect(0,0,1,1));body.raycastTarget=true;body.gameObject.AddComponent<BodyReaction>().Bind(c);
            var description=w.NarrativeLabel("Partner description",entry.Personality,content,new Rect(0,0,1,1),15);UIWidgets.Top(description.rectTransform,25,382,25,60);description.lineSpacing=1.28f;
            string[] titles={"好きなもの","得意なこと","好きな場所"};string[] values={entry.Likes,entry.Talent,entry.FavoritePlace};string[] ids={"Book likes","Book talent","Book favorite place"};
            for(int i=0;i<3;i++)
            {
                var panel=w.OrnatePanel("Book discovery "+i,content,new Rect(0,0,1,1),i==1?UIWidgets.BluePaper:UIWidgets.Cream,12).rectTransform;UIWidgets.Top(panel,18,454+i*70,18,62);
                w.StoryLabel("Book field title "+i,titles[i],panel,new Rect(.05f,.58f,.9f,.34f),13);var text=w.NarrativeLabel(ids[i],values[i],panel,new Rect(.06f,.07f,.88f,.50f),14);text.color=new Color(.39f,.24f,.31f);
            }
            var stats=w.Label("Book collection counts","出会った回数 "+entry.Meetings+"回　達成した依頼 "+entry.CompletedRequests+"件",content,new Rect(0,0,1,1),13);UIWidgets.Top(stats.rectTransform,16,670,16,40);
            string reactions=(entry.Reactions>=1?"笑顔":"？？？")+"　"+(entry.Reactions>=2?"手を振る":"？？？")+"　"+(entry.Reactions>=3?"とっておきのお話":"？？？");
            var react=w.NarrativeLabel("Book unlocked reactions",reactions,content,new Rect(0,0,1,1),13);UIWidgets.Top(react.rectTransform,18,712,18,36);
            var next=w.StoryLabel("Book next discovery",entry.NextDiscovery,content,new Rect(0,0,1,1),13);UIWidgets.Top(next.rectTransform,20,752,20,40);
            var quote=w.NarrativeLabel("Book conversation",TownCharacterDialogue.For(chosen.Id,entry.Meetings),content,new Rect(0,0,1,1),15);UIWidgets.Top(quote.rectTransform,24,854,24,76);quote.lineSpacing=1.28f;
            var talk=w.Button("Talk to character","お話する",content,new Rect(0,0,1,1),()=>
            {
                if(!c.Flow.TalkToCharacter(chosen.Id)){quote.text=c.Flow.Daily.LastError;return;}
                c.Flow.PlayCue(AudioCue.Character);var updated=TownCharacterBook.For(chosen.Id,c.Game.State,c.Flow.Daily.Data);quote.text=TownCharacterDialogue.For(chosen.Id,updated.Meetings);
                stats.text="出会った回数 "+updated.Meetings+"回　達成した依頼 "+updated.CompletedRequests+"件";next.text=updated.NextDiscovery;
                react.text=(updated.Reactions>=1?"笑顔":"？？？")+"　"+(updated.Reactions>=2?"手を振る":"？？？")+"　"+(updated.Reactions>=3?"とっておきのお話":"？？？");
            },true);UIWidgets.Top((RectTransform)talk.transform,44,798,44,48);
            var feedback=w.Label("Selection feedback",c.Flow.FeedbackFor(ScreenId.CharacterSelection),root,new Rect(0,0,1,1),13);UIWidgets.Bottom(feedback.rectTransform,20,74,20,38);
            var confirm=w.Button("Confirm partner","この子にする",root,new Rect(0,0,1,1),()=>c.Flow.ConfirmFavorite());UIWidgets.Bottom((RectTransform)confirm.transform,28,12,28,52);
        }
    }
}
