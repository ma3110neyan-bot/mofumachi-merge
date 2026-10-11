using System;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownLifeView : MonoBehaviour
    {
        // Feet on the paved ring around the fountain; entrances are in front of the blue/pink awnings.
        public static readonly TownPoint RequestEntrance=new TownPoint(.18f,.635f),ShopEntrance=new TownPoint(.76f,.605f);
        public string NearbyFacility { get; private set; }
        public bool CanInteract=>isActiveAndEnabled&&context!=null&&!context.Flow.IsModalOpen&&context.Flow.CurrentScreen==ScreenId.Home;
        private RawImage background;private ScreenContext context;private TownActor[] actors;private RectTransform actorLayer;
        private GameObject bubble,entrance;private float nextFrame,nextTalk,bubbleUntil;private int talkIndex;
        private static readonly TownPoint[] Nodes={new TownPoint(.42f,.47f),new TownPoint(.68f,.515f),new TownPoint(.68f,.60f),new TownPoint(.42f,.645f),new TownPoint(.18f,.60f),new TownPoint(.15f,.52f),RequestEntrance,ShopEntrance};
        private static readonly TownEdge[] Edges={new TownEdge(0,1),new TownEdge(1,2),new TownEdge(2,3),new TownEdge(3,4),new TownEdge(4,5),new TownEdge(5,0),new TownEdge(4,6),new TownEdge(2,7)};
        private static readonly string[] Dialogues={"いいお天気だね♪","広場をおさんぽしよう！","お茶のいい香りがするね。","お花がいっぱい！","みんなで街を育てよう。","今日は何を届けようかな？"};
        public void Bind(ScreenContext c,RawImage image)
        {
            context=c;background=image;actorLayer=(RectTransform)transform;
            actorLayer.gameObject.AddComponent<RectMask2D>();
            var nav=new TownNavigation(Nodes,Edges,.028f);actors=new TownActor[6];
            int npcIndex=1;
            for(int i=0;i<6;i++)
            {
                var character=CharacterCatalog.All[i];bool controlled=character.Id==c.Game.State.favoriteCharacterId;
                var point=controlled?Nodes[0]:Nodes[npcIndex];var end=controlled?Nodes[0]:Nodes[npcIndex%5+1];if(!controlled)npcIndex++;
                var rect=UIWidgets.Node("Town actor "+character.Id,actorLayer,new Rect(.5f,.5f,0,0));rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(62,78);
                var body=rect.gameObject.AddComponent<CharacterBodyGraphic>();body.raycastTarget=true;
                var actor=rect.gameObject.AddComponent<TownActor>();actor.Bind(character.Id,controlled,new TownMovement(nav,point),nav);actor.Connect(this,point,end,i*.4f);actors[i]=actor;
                var label=c.Widgets.Label("Town actor name",character.Name,rect,new Rect(-.12f,-.2f,1.24f,.2f),11);
                var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=UIWidgets.Cream;outline.effectDistance=new Vector2(1,-1);
                actor.Advance(0);
            }
            nextTalk=Time.unscaledTime+4.5f;
        }
        private void Update()
        {
            if(context==null||Time.unscaledTime<nextFrame)return;
            nextFrame=Time.unscaledTime+1f/30;
            if(!CanInteract){CancelInteraction();return;}
            foreach(var actor in actors)actor.Advance(1f/30);
            Array.Sort(actors,(a,b)=>b.Position.Y.CompareTo(a.Position.Y));foreach(var actor in actors)actor.transform.SetAsLastSibling();
            bool dragging=false;foreach(var actor in actors)if(actor.Movement?.OwnedPointerId!=null)dragging=true;
            if(bubble!=null&&Time.unscaledTime>=bubbleUntil)HideBubble();
            if(dragging){HideBubble();return;}
            if(entrance==null&&Time.unscaledTime>=nextTalk){ShowBubble(Dialogues[talkIndex++%Dialogues.Length]);nextTalk=Time.unscaledTime+5.5f;}
        }
        public Vector2 Local(TownPoint point)
        {
            if(background==null)return Vector2.zero;
            var uv=background.uvRect;var r=background.rectTransform.rect;
            var p=r.min+Vector2.Scale((new Vector2(point.X,point.Y)-uv.min)/uv.size,r.size);
            return actorLayer.InverseTransformPoint(background.rectTransform.TransformPoint(p));
        }
        public TownPoint Source(Vector2 screen,Camera camera)
        {
            if(background==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(background.rectTransform,screen,camera,out var p))return Nodes[0];
            var r=background.rectTransform.rect;var uv=background.uvRect;var normalized=(p-r.min)/r.size;
            return new TownPoint(uv.x+normalized.x*uv.width,uv.y+normalized.y*uv.height);
        }
        public void StartInteraction(){HideBubble();HideEntrance();nextTalk=Time.unscaledTime+5.5f;}
        public void ActorReleased(TownActor actor)
        {
            if(!CanInteract||!actor.Controlled||actor.Movement.OwnedPointerId.HasValue)return;
            var p=actor.Position;
            string facility=Near(p,RequestEntrance)?"quest":Near(p,ShopEntrance)?"shop":null;
            HideEntrance();if(facility==null)return;NearbyFacility=facility;HideBubble();actor.Surprise();
            var parent=(RectTransform)transform.parent;
            var button=context.Widgets.Button("Enter facility",facility=="quest"?"依頼部屋に入る":"ショップへ",parent,new Rect(0,0,1,1),()=>{if(NearbyFacility=="quest")context.Flow.ShowQuest();else context.Flow.OpenShopPreview();});
            UIWidgets.Bottom((RectTransform)button.transform,52,220,52,48);entrance=button.gameObject;
        }
        private static bool Near(TownPoint p,TownPoint q)=>Vector2.Distance(new Vector2(p.X,p.Y),new Vector2(q.X,q.Y))<.034f;
        internal void React(TownActor actor)
        {
            if(!CanInteract)return;context.Flow.PlayCue(AudioCue.Character);bool dragging=false;foreach(var a in actors)if(a.Movement?.OwnedPointerId!=null)dragging=true;
            if(!dragging&&entrance==null)ShowBubble(CharacterCatalog.Find(actor.CharacterId).Name+"："+Dialogues[CharacterCatalog.Find(actor.CharacterId).PortraitIndex]);
        }
        private void ShowBubble(string text)
        {
            HideBubble();var parent=(RectTransform)transform.parent;
            var panel=context.Widgets.OrnatePanel("Town bubble panel",parent,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;UIWidgets.Top(panel,28,80,28,40);
            context.Widgets.Label("Town bubble",text,panel,new Rect(.04f,.04f,.92f,.92f),12);bubble=panel.gameObject;bubbleUntil=Time.unscaledTime+2.5f;
        }
        private void HideBubble(){if(bubble!=null){bubble.SetActive(false);Destroy(bubble);}bubble=null;}
        private void HideEntrance(){NearbyFacility=null;if(entrance!=null){entrance.SetActive(false);Destroy(entrance);}entrance=null;}
        public void CancelInteraction(){if(actors!=null)foreach(var actor in actors)actor.Movement?.CancelInput();HideBubble();HideEntrance();nextTalk=Time.unscaledTime+5.5f;}
        private void OnRectTransformDimensionsChange(){CancelInteraction();}
        private void OnDisable()=>CancelInteraction();
    }
}
