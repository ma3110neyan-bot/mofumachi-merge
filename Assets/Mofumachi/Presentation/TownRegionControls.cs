using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // Fast travel stays separate from normal character-led walking.
    public sealed class TownRegionControls : MonoBehaviour
    {
        private TownLifeView life;
        private TownRegionSession session;
        private Text heading,hint;private string growthGoal;
        private readonly Button[] buttons=new Button[3];
        private readonly Text[] labels=new Text[3];
        private TownRegionId previousView=(TownRegionId)(-1),previousHero=(TownRegionId)(-1);
        private bool previousAllowed,previousWalking;
        private string previousDestination;
        public void Bind(ScreenContext c,RectTransform root,TownLifeView town)
        {
            life=town;session=town.Regions;growthGoal=TownNextGoal.For(c.Game.State).Detail;
            var panel=c.Widgets.OrnatePanel("Town region choices",root,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;
            UIWidgets.Top(panel,10,62,10,76);
            heading=c.Widgets.StoryLabel("Town region name","",panel,new Rect(.04f,.73f,.92f,.25f),12);
            for(int i=0;i<3;i++)
            {
                var region=(TownRegionId)i;var column=UIWidgets.Node("Region choice column "+i,panel,new Rect(i/3f,0,1/3f,1));
                buttons[i]=c.Widgets.Button("Region walk "+region,"",column,new Rect(0,0,1,1),()=>Choose(region),true);
                buttons[i].GetComponent<ButtonPressFeedback>().PreserveHitbox=true;
                UIWidgets.Bottom((RectTransform)buttons[i].transform,3,4,3,48);labels[i]=buttons[i].GetComponentInChildren<Text>();labels[i].fontSize=14;
            }
            hint=root.Find("Home quest/Town growth hint")?.GetComponent<Text>();session.RegionChanged+=RegionChanged;Refresh();
        }
        private void Choose(TownRegionId region)
        {
            if(!life.CanInteract)return;
            if(life.World?.IsReady==true)life.World.FastTravel(region);
            else if(region==session.HeroRegion&&session.ViewingRegion!=session.HeroRegion)session.ReturnToHero();
            else session.GuideToPlace(TownOutingDestinations.ForRegion(region,life.TownLevel));
            Refresh();
        }
        private void RegionChanged(TownRegionId region)=>Refresh();
        private void LateUpdate()=>Refresh();
        private void Refresh()
        {
            if(life==null||session==null)return;
            bool allowed=life.CanInteract;var viewed=session.ViewingRegion;var hero=session.HeroRegion;
            string destination=session.HeroDestinationId;bool walking=false;foreach(var actor in life.Actors)if(actor.Controlled)walking=actor.IsGuided;
            if(previousView==viewed&&previousHero==hero&&previousAllowed==allowed&&previousDestination==destination&&previousWalking==walking)return;
            previousView=viewed;previousHero=hero;previousAllowed=allowed;previousDestination=destination;previousWalking=walking;
            if(life.World?.IsReady==true){heading.text=life.World.Model.Hero.IsWalking?"おさんぽ中♪ · 上はすぐ移動":TownWorldLayout.Name(hero)+" · キャラをタップ";for(int j=0;j<3;j++){labels[j].text=(j==0?"北のエリア":j==1?"中央公園":"南のエリア")+"\nすぐ移動";buttons[j].interactable=allowed;}return;}
            heading.text=session.IsHeroTransitioning?"お隣の街へ…":destination!=null?ShortName(session.HeroDestinationRegion.Value)+"へ、おさんぽ中♪":
                walking?"道を歩いています♪":ShortName(viewed)+(viewed==hero?" · キャラをタップ":" · 主人公は"+ShortName(hero));
            for(int i=0;i<3;i++)
            {
                var region=(TownRegionId)i;
                bool returnToHero=region==hero&&viewed!=hero;
                labels[i].text=returnToHero?"主人公へ\n戻る":(region==TownRegionId.West?"西のカフェ":region==TownRegionId.Central?"中央の噴水":"東の花の小道")+"\n"+
                    (destination!=null&&session.HeroDestinationRegion==region?"おさんぽ中":region==TownRegionId.West&&life.TownLevel<2?"お庭へ歩く":"歩いて行く");
                buttons[i].interactable=allowed;
                buttons[i].GetComponent<RoundedPanelGraphic>().color=(session.HeroDestinationRegion??hero)==region?UIWidgets.BlushPaper:UIWidgets.BluePaper;
            }
            if(hint!=null)hint.text=growthGoal;
        }
        private static string ShortName(TownRegionId region)=>region==TownRegionId.West?"西の庭":region==TownRegionId.Central?"中央の街":"東の小道";
        private void OnDisable(){if(session!=null)session.RegionChanged-=RegionChanged;}
    }
}
