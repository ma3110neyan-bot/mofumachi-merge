using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    public static class CharacterSelectionScreen
    {
        public static void Build(RectTransform root, ScreenContext c)
        {
            var w=c.Widgets; w.Header(root,c,"パートナー選択",UIWidgets.Pink);
            var viewport=UIWidgets.Node("Partner viewport",root,new Rect(0,0,1,1));
            viewport.offsetMin=new Vector2(12,120);viewport.offsetMax=new Vector2(-12,-78);
            var capture=viewport.gameObject.AddComponent<Image>();capture.color=Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=w.OrnatePanel("Partner content",viewport,new Rect(0,1,1,0),UIWidgets.BlushPaper).rectTransform;
            content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,440);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var chosen=CharacterCatalog.Find(c.Flow.PreviewFavoriteId)??CharacterCatalog.All[0];
            var hero=UIWidgets.Node("Partner hero",content,new Rect(0,0,1,1));UIWidgets.Top(hero,80,18,80,132);
            var body=w.Body("Chosen full body",chosen.Id,hero,new Rect(0,0,1,1));body.raycastTarget=true;body.gameObject.AddComponent<BodyReaction>().Bind(c);
            var name=w.Ribbon("Chosen partner name",chosen.Name,content,new Rect(0,0,1,1),20,UIWidgets.Pink);
            UIWidgets.Top((RectTransform)name.transform.parent,66,156,66,36);
            var description=w.Label("Partner description",chosen.Description,content,new Rect(0,0,1,1),14);
            UIWidgets.Top(description.rectTransform,24,204,24,54);
            for(int i=0;i<CharacterCatalog.All.Count;i++)
            {
                var candidate=CharacterCatalog.All[i];int column=i%3,row=i/3;
                var button=w.Button("Partner "+candidate.Id,"",content,new Rect(column/3f+.025f,0,1/3f-.05f,0),()=>c.Flow.PreviewFavorite(candidate.Id),candidate.Id!=chosen.Id);
                var rect=(RectTransform)button.transform;rect.anchorMin=new Vector2(rect.anchorMin.x,1);rect.anchorMax=new Vector2(rect.anchorMax.x,1);
                rect.offsetMin=new Vector2(0,-276-row*76-70);rect.offsetMax=new Vector2(0,-276-row*76);
                w.Character("Candidate portrait "+candidate.Id,candidate.PortraitIndex,rect,new Rect(.24f,.32f,.52f,.64f));
                w.Heading("Candidate name "+candidate.Id,candidate.Name,rect,new Rect(.04f,.015f,.92f,.29f),12);
            }
            var feedback=w.Label("Selection feedback",c.Flow.FeedbackFor(ScreenId.CharacterSelection),root,new Rect(0,0,1,1),13);
            UIWidgets.Bottom(feedback.rectTransform,20,74,20,38);
            var confirm=w.Button("Confirm partner","この子にする",root,new Rect(0,0,1,1),()=>c.Flow.ConfirmFavorite());
            UIWidgets.Bottom((RectTransform)confirm.transform,28,12,28,52);
        }
    }
}
