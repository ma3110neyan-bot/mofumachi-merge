using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Location only. Travel choices are expanded explicitly through TownMenuView.
    public sealed class TownRegionControls:MonoBehaviour
    {
        private TownLifeView life;private TownRegionSession session;private Text heading;private string previous;
        public void Bind(ScreenContext c,RectTransform root,TownLifeView town)
        {
            life=town;session=town.Regions;
            var panel=c.Widgets.Panel("Town location ribbon",root,new Rect(0,0,1,1),new Color(1,.988f,.945f,.82f)).rectTransform;
            UIWidgets.Top(panel,60,53,60,24);panel.GetComponent<RoundedPanelGraphic>().raycastTarget=false;
            heading=c.Widgets.StoryLabel("Town region name","",panel,new Rect(.03f,0,.94f,1),12);
            session.RegionChanged+=RegionChanged;Refresh();
        }
        private void RegionChanged(TownRegionId region)=>Refresh();
        private void LateUpdate()=>Refresh();
        private void Refresh()
        {
            if(life==null||session==null||heading==null)return;
            string text=(life.World?.IsReady==true?TownDistrictLayout.Name(life.World.Model.Hero.District):TownWorldLayout.Name(session.ViewingRegion))+(life.World?.Model?.Hero.IsWalking==true?" · おさんぽ♪":"");
            if(previous==text)return;previous=text;heading.text=text;
        }
        private void OnDisable(){if(session!=null)session.RegionChanged-=RegionChanged;}
    }
}
