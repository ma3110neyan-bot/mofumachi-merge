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
        public bool CanInteract=>isActiveAndEnabled&&context!=null&&!context.Flow.IsModalOpen&&!context.Flow.IsAppPaused&&context.Flow.CurrentScreen==ScreenId.Home;
        private RawImage background;private ScreenContext context;private TownActor[] actors;private RectTransform actorLayer;
        private GameObject bubble,entrance,outings;private float nextTalk,bubbleUntil,outingsUntil;private string restTarget;private int talkIndex;private readonly TownFrameClock clock=new TownFrameClock();
        private static readonly TownPoint[] Nodes={new TownPoint(.42f,.47f),new TownPoint(.68f,.515f),new TownPoint(.68f,.60f),new TownPoint(.42f,.645f),new TownPoint(.18f,.60f),new TownPoint(.15f,.52f),RequestEntrance,ShopEntrance};
        public static System.Collections.Generic.IReadOnlyList<TownPoint> WalkStops {get;}=Array.AsReadOnly(Nodes);
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
                var actor=rect.gameObject.AddComponent<TownActor>();actor.Bind(character.Id,controlled,new TownMovement(nav,point),nav);actor.Connect(this,point,end,i*.4f);if(!controlled)actor.SetWalkRoute(WalkStops,npcIndex-1);actors[i]=actor;
                var ground=c.Widgets.Panel("Town actor ground shadow",rect,new Rect(.17f,-.035f,.66f,.095f),new Color(.24f,.19f,.26f,.18f));ground.radius=100;ground.decorated=false;ground.transform.SetAsFirstSibling();
                var namePanel=c.Widgets.Panel("Town actor name panel",rect,new Rect(-.13f,-.26f,1.26f,.26f)).rectTransform;
                var label=c.Widgets.StoryLabel("Town actor name",character.Name,namePanel,new Rect(.04f,.03f,.92f,.94f),12);
                var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=UIWidgets.Cream;outline.effectDistance=new Vector2(1,-1);
                actor.Render(0,0);
            }
            AddEntranceMarker("依頼部屋",RequestEntrance,UIWidgets.BluePaper);
            AddEntranceMarker("ショップ",ShopEntrance,UIWidgets.BlushPaper);
            nextTalk=Time.unscaledTime+4.5f;
        }
        private void AddEntranceMarker(string name,TownPoint location,Color tint)
        {
            var marker=context.Widgets.Panel("Facility marker "+name,actorLayer,new Rect(.5f,.5f,0,0),tint).rectTransform;
            marker.sizeDelta=new Vector2(82,23);marker.anchoredPosition=Local(location)+Vector2.up*92;
            context.Widgets.StoryLabel("Facility marker label",name,marker,new Rect(.04f,0,.92f,1),11);
        }
        public bool GuideToFacility(string facility)
        {
            if(!CanInteract || (facility!="quest"&&facility!="shop"))return false;
            foreach(var actor in actors)if(actor.Controlled)return actor.GuideTo(facility=="quest"?RequestEntrance:ShopEntrance);
            return false;
        }
        // Rest stops reuse surveyed street nodes; no new shortcut crosses the fountain or buildings.
        private static TownPoint RestPoint(string place)=>place=="bench"?Nodes[1]:place=="flowers"?Nodes[5]:Nodes[3];
        public bool GuideToRestStop(string place)
        {
            if(!CanInteract||(place!="bench"&&place!="flowers"&&place!="fountain"))return false;
            foreach(var actor in actors)if(actor.Controlled&&actor.GuideTo(RestPoint(place))){restTarget=place;return true;}
            return false;
        }
        private void ShowOutings()
        {
            HideOutings();HideBubble();
            var parent=(RectTransform)transform.parent;var row=context.Widgets.Panel("Town outing choices",parent,new Rect(0,0,1,1),UIWidgets.Cream).rectTransform;UIWidgets.Top(row,14,130,14,48);
            string[] ids={"bench","flowers","fountain"},labels={"ベンチ","お花","噴水"};
            for(int i=0;i<ids.Length;i++)
            {
                string id=ids[i];var b=context.Widgets.Button("Visit "+id,labels[i],row,new Rect(i/3f+.008f,0,1/3f-.016f,1),()=>{context.Flow.PlayCue(AudioCue.Confirm);GuideToRestStop(id);},true);
                b.GetComponentInChildren<Text>().fontSize=14;
            }
            outings=row.gameObject;outingsUntil=Time.unscaledTime+6;nextTalk=outingsUntil+3;
        }
        private void HideOutings(){if(outings!=null){outings.SetActive(false);Destroy(outings);}outings=null;}
        private void Update()=>AdvanceFrame(Time.unscaledDeltaTime);
        public void AdvanceFrame(float elapsedSeconds)
        {
            if(context==null||actors==null)return;
            if(!CanInteract){CancelInteraction();return;}
            int steps=clock.AddElapsed(elapsedSeconds);
            for(int step=0;step<steps;step++)foreach(var actor in actors)actor.Advance(TownFrameClock.StepSeconds);
            foreach(var actor in actors)actor.Render(clock.Alpha,elapsedSeconds);
            Array.Sort(actors,(a,b)=>b.RenderPosition.Y.CompareTo(a.RenderPosition.Y));foreach(var actor in actors)actor.transform.SetAsLastSibling();
            bool dragging=false;foreach(var actor in actors)if(actor.Movement?.OwnedPointerId!=null&&!actor.IsGuided)dragging=true;
            if(bubble!=null&&Time.unscaledTime>=bubbleUntil)HideBubble();
            if(outings!=null&&Time.unscaledTime>=outingsUntil)HideOutings();
            if(dragging){HideBubble();return;}
            if(entrance==null&&outings==null&&Time.unscaledTime>=nextTalk){ShowBubble(Dialogues[talkIndex++%Dialogues.Length]);nextTalk=Time.unscaledTime+5.5f;}
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
        public void StartInteraction(){restTarget=null;HideOutings();HideBubble();HideEntrance();nextTalk=Time.unscaledTime+5.5f;}
        public void ActorReleased(TownActor actor)
        {
            if(!CanInteract||!actor.Controlled||actor.Movement.OwnedPointerId.HasValue)return;
            actor.FreezeMovement();var p=actor.Position;
            string facility=Near(p,RequestEntrance)?"quest":Near(p,ShopEntrance)?"shop":null;
            HideEntrance();
            if(facility==null)
            {
                if(restTarget!=null&&Near(p,RestPoint(restTarget)))
                {
                    string caption=restTarget=="bench"?"ベンチのそばでひと休み♪":restTarget=="flowers"?"お花を眺めて、のんびり。":"噴水の音が心地いいね。";
                    restTarget=null;actor.Surprise();ShowBubble(caption);
                }
                return;
            }
            NearbyFacility=facility;HideBubble();actor.Surprise();
            var parent=(RectTransform)transform.parent;
            var button=context.Widgets.Button("Enter facility",facility=="quest"?"依頼部屋に入る":"ショップへ",parent,new Rect(0,0,1,1),()=>{if(NearbyFacility=="quest")context.Flow.ShowQuest();else context.Flow.OpenShopPreview();});
            UIWidgets.Bottom((RectTransform)button.transform,52,220,52,48);entrance=button.gameObject;
        }
        private static bool Near(TownPoint p,TownPoint q)=>Vector2.Distance(new Vector2(p.X,p.Y),new Vector2(q.X,q.Y))<.034f;
        internal void React(TownActor actor)
        {
            if(!CanInteract)return;context.Flow.PlayCue(AudioCue.Character);bool dragging=false;foreach(var a in actors)if(a.Movement?.OwnedPointerId!=null&&!a.IsGuided)dragging=true;
            if(!dragging&&entrance==null){if(actor.Controlled)ShowOutings();else{HideOutings();ShowBubble(CharacterCatalog.Find(actor.CharacterId).Name+"："+Dialogues[CharacterCatalog.Find(actor.CharacterId).PortraitIndex]);}}
        }
        private void ShowBubble(string text)
        {
            HideBubble();var parent=(RectTransform)transform.parent;
            var panel=context.Widgets.OrnatePanel("Town bubble panel",parent,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;UIWidgets.Top(panel,28,130,28,40);
            context.Widgets.StoryLabel("Town bubble",text,panel,new Rect(.04f,.04f,.92f,.92f),14);bubble=panel.gameObject;bubbleUntil=Time.unscaledTime+2.5f;
        }
        private void HideBubble(){if(bubble!=null){bubble.SetActive(false);Destroy(bubble);}bubble=null;}
        private void HideEntrance(){NearbyFacility=null;if(entrance!=null){entrance.SetActive(false);Destroy(entrance);}entrance=null;}
        public void CancelInteraction(){clock.Reset();restTarget=null;HideOutings();if(actors!=null)foreach(var actor in actors){if(actor==null)continue;actor.Movement?.CancelInput();actor.ResetVisual();}HideBubble();HideEntrance();nextTalk=Time.unscaledTime+5.5f;}
        private void OnRectTransformDimensionsChange()
        {
            CancelInteraction();
            if(actorLayer==null||context==null)return;
            var quest=actorLayer.Find("Facility marker 依頼部屋") as RectTransform;var shop=actorLayer.Find("Facility marker ショップ") as RectTransform;
            if(quest!=null)quest.anchoredPosition=Local(RequestEntrance)+Vector2.up*92;
            if(shop!=null)shop.anchoredPosition=Local(ShopEntrance)+Vector2.up*92;
        }
        private void OnDisable()=>CancelInteraction();
    }
}
