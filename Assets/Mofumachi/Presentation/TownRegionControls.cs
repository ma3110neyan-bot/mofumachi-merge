using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // The same three large buttons provide preview, walking, and return without
    // adding a second toolbar over the residents or the existing quest summary.
    public sealed class TownRegionControls : MonoBehaviour
    {
        private TownLifeView life;
        private TownRegionSession session;
        private Text heading,hint;
        private readonly Button[] buttons=new Button[3];
        private readonly Text[] labels=new Text[3];
        private TownRegionId previousView=(TownRegionId)(-1),previousHero=(TownRegionId)(-1);
        private bool previousAllowed;
        public void Bind(ScreenContext c,RectTransform root,TownLifeView town)
        {
            life=town;session=town.Regions;
            var panel=c.Widgets.OrnatePanel("Town region choices",root,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;
            UIWidgets.Top(panel,10,62,10,76);
            heading=c.Widgets.StoryLabel("Town region name","",panel,new Rect(.04f,.73f,.92f,.25f),12);
            for(int i=0;i<3;i++)
            {
                var region=(TownRegionId)i;var column=UIWidgets.Node("Region choice column "+i,panel,new Rect(i/3f,0,1/3f,1));
                buttons[i]=c.Widgets.Button("Region view "+region,"",column,new Rect(0,0,1,1),()=>Choose(region),true);
                UIWidgets.Bottom((RectTransform)buttons[i].transform,3,4,3,48);labels[i]=buttons[i].GetComponentInChildren<Text>();labels[i].fontSize=14;
            }
            hint=root.Find("Home quest/Town growth hint")?.GetComponent<Text>();session.RegionChanged+=RegionChanged;Refresh();
        }
        private void Choose(TownRegionId region)
        {
            if(!life.CanInteract)return;
            if(region==session.ViewingRegion&&region!=session.HeroRegion)session.GuideToRegion(region);
            else if(region==session.HeroRegion&&region!=session.ViewingRegion)session.ReturnToHero();
            else session.LookAtRegion(region);
            Refresh();
        }
        private void RegionChanged(TownRegionId region)=>Refresh();
        private void LateUpdate()=>Refresh();
        private void Refresh()
        {
            if(life==null||session==null)return;
            bool allowed=life.CanInteract;var viewed=session.ViewingRegion;var hero=session.HeroRegion;
            if(previousView==viewed&&previousHero==hero&&previousAllowed==allowed)return;
            previousView=viewed;previousHero=hero;previousAllowed=allowed;
            heading.text=TownRegionCatalog.ForLevel(viewed,life.TownLevel).Name;
            for(int i=0;i<3;i++)
            {
                var region=(TownRegionId)i;bool walk=region==viewed&&region!=hero,back=region==hero&&region!=viewed;
                buttons[i].name=walk?"Walk to viewed region":back?"Return to town hero":"Region view "+region;
                labels[i].text=walk?"ここを\nおさんぽ":back?"主人公へ\nもどる":(region==TownRegionId.West?"西の庭":region==TownRegionId.Central?"中央の街":"東の小道")+"\n眺める";
                buttons[i].interactable=allowed;
                buttons[i].GetComponent<RoundedPanelGraphic>().color=region==viewed?UIWidgets.BlushPaper:UIWidgets.BluePaper;
            }
            if(hint!=null)hint.text=viewed==TownRegionId.Central?TownPlaceCatalog.GrowthHint(life.TownLevel):
                viewed==TownRegionId.West?"カフェ・ベンチで、ひと休み♪":"花の小道と工房を見てみよう♪";
        }
        private void OnDisable(){if(session!=null)session.RegionChanged-=RegionChanged;}
    }
}
