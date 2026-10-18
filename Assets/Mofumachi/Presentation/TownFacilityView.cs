using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class TownFacilityView:MonoBehaviour
    {
        private sealed class Door
        {public string Id;public RectTransform Hit,Leaf,Frame,Sign;public RoundedPanelGraphic Light;public Text Caption;public Mofumachi.Core.TownPoint Art;public Vector2 Size;}
        private readonly List<Door> doors=new List<Door>();private ScreenContext context;private TownLifeView life;private bool dispatched;
        public TownFacilityEntry Entry {get;}=new TownFacilityEntry();
        public void Bind(ScreenContext c,TownLifeView town)
        {
            context=c;life=town;Build("quest","北の依頼小屋",.796f,.214f,.052f,.054f);Build("shop","南のお花ショップ",.806f,.777f,.045f,.054f);Refresh();
        }
        private void Build(string id,string title,float u,float v,float width,float height)
        {
            var door=new Door{Id=id,Art=TownWorldMap.Point(u,v),Size=new Vector2(width*TownWorldMap.Width,height*TownWorldMap.Height)};
            var frame=UIWidgets.Node("Facility doorway "+id,(RectTransform)life.transform,new Rect(.5f,.5f,0,0));door.Frame=frame;
            door.Light=context.Widgets.Panel("Warm doorway "+id,frame,new Rect(0,0,1,1),new Color(.29f,.15f,.12f)).GetComponent<RoundedPanelGraphic>();door.Light.decorated=false;door.Light.radius=8;door.Light.raycastTarget=false;
            var leaf=UIWidgets.Node("Opening door leaf "+id,frame,new Rect(0,0,1,1));leaf.pivot=new Vector2(0,.5f);door.Leaf=leaf;
            var image=leaf.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(TownWorldMap.Resource);image.uvRect=new Rect(u-width/2,1-v-height/2,width,height);image.raycastTarget=false;
            var hit=UIWidgets.Node("Facility entrance "+id,(RectTransform)life.transform,new Rect(.5f,.5f,0,0));hit.sizeDelta=new Vector2(72,80);door.Hit=hit;
            var ray=hit.gameObject.AddComponent<Image>();ray.color=new Color(1,1,1,1f/255);var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=ray;button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>RequestEnter(id));
            var sign=context.Widgets.StoryPanel("Facility sign "+id,hit,new Rect(-.40f,1,1.80f,.37f),true).rectTransform;door.Sign=sign;sign.anchorMin=sign.anchorMax=new Vector2(.5f,1);sign.pivot=new Vector2(.5f,0);sign.anchoredPosition=Vector2.zero;sign.sizeDelta=new Vector2(146,28);
            door.Caption=context.Widgets.NarrativeLabel("Facility name "+id,title,sign,new Rect(.03f,.02f,.94f,.96f),12);doors.Add(door);
        }
        public bool RequestEnter(string id)
        {
            if(!life.CanInteract||life.IsManuallyDragging||!life.GuideToFacility(id))return false;
            dispatched=false;return Entry.Request(id);
        }
        public void Cancel(){Entry.Cancel();dispatched=false;}
        public void Advance(float seconds)
        {
            if(!life.CanSimulate){Cancel();return;}
            Entry.Step(life.World.Model.Hero.Position,life.World.Model.Hero.IsWalking,seconds);Refresh();
            if(Entry.Phase!=TownEntryPhase.Entered||dispatched)return;dispatched=true;
            context.Flow.PlayCue(AudioCue.Confirm);
            if(Entry.Facility=="quest")context.Flow.ShowQuest();else context.Flow.OpenShopPreview();
        }
        public void Refresh()
        {
            if(life?.World?.IsReady!=true)return;float scale=life.World.RenderScale;
            foreach(var door in doors)
            {
                door.Frame.anchoredPosition=life.World.Local(door.Art);door.Frame.sizeDelta=door.Size*scale;
                door.Hit.anchoredPosition=life.World.Local(door.Art);door.Hit.sizeDelta=new Vector2(Mathf.Max(48,door.Frame.sizeDelta.x+18),Mathf.Max(48,door.Frame.sizeDelta.y+10));
                var p=door.Hit.anchoredPosition;var bounds=((RectTransform)life.transform).rect;
                bool visible=p.x>bounds.xMin-64&&p.x<bounds.xMax+64&&p.y>bounds.yMin-80&&p.y<bounds.yMax-10;
                door.Hit.gameObject.SetActive(visible);door.Frame.gameObject.SetActive(visible);
                bool near=TownWalkableArea.Distance(life.World.Model.Hero.Position,TownWorldMap.Place(door.Id))<.23f;
                float amount=Entry.Facility==door.Id?Entry.OpenAmount:0;
                door.Leaf.localScale=new Vector3(1-amount,1,1);door.Light.color=Color.Lerp(new Color(.28f,.14f,.10f),new Color(1,.86f,.48f),Mathf.Max(amount,near?.35f:0));
                string title=door.Id=="quest"?"北の依頼小屋":"南のお花ショップ";
                door.Caption.text=Entry.Facility==door.Id?(Entry.Phase==TownEntryPhase.Walking?"入口までおさんぽ中♪":"扉がひらくよ♪"):near?title+"\nタップして入る":title;
                door.Sign.sizeDelta=new Vector2(146,near||Entry.Facility==door.Id?44:28);
            }
        }
    }
}
