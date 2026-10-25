using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Both sides render the same playable ground and landmarks. Only the saved
    // growth stage differs; no old map painting is substituted for the town.
    public sealed class TownGrowthComparisonView:MonoBehaviour
    {
        private readonly TownGroundGraphic[] ground=new TownGroundGraphic[2];
        private readonly TownCentralGroundGraphic[] centralGround=new TownCentralGroundGraphic[2];
        private readonly TownWestGroundGraphic[] westGround=new TownWestGroundGraphic[2];
        private readonly TownEastGroundGraphic[] eastGround=new TownEastGroundGraphic[2];
        private readonly TownNorthGroundGraphic[] northGround=new TownNorthGroundGraphic[2];
        private readonly TownSouthGroundGraphic[] southGround=new TownSouthGroundGraphic[2];
        private readonly TownSceneryView[] scenery=new TownSceneryView[2];
        private readonly TownFountainGraphic[] fountain=new TownFountainGraphic[2];
        private readonly TownGrowthView[] growth=new TownGrowthView[2];
        private readonly Button[] buttons=new Button[5];private DeliveryPresentation delivery;private Text afterLabel;
        public void Bind(ScreenContext c,DeliveryPresentation result)
        {
            delivery=result;var root=(RectTransform)transform;
            string[] labels={"西\nカフェ","中央\n噴水","東\n花屋","北\n依頼","南\n庭園"};
            for(int i=0;i<5;i++)
            {
                var district=(TownDistrictId)i;var column=UIWidgets.Node("Growth region column",root,new Rect(i/5f,0,1/5f,1));
                buttons[i]=c.Widgets.Button("Growth region "+district,labels[i],column,new Rect(0,0,1,1),()=>Show(district),true);
                UIWidgets.Top((RectTransform)buttons[i].transform,2,54,2,48);buttons[i].GetComponentInChildren<Text>().fontSize=12;
            }
            for(int i=0;i<2;i++)
            {
                var column=UIWidgets.Node("Growth comparison "+i,root,new Rect(i*.5f,0,.5f,1));
                var preview=UIWidgets.Node(i==0?"Growth before preview":"Growth after preview",column,new Rect(0,0,1,1));UIWidgets.Top(preview,6,108,6,96);preview.gameObject.AddComponent<RectMask2D>();
                ground[i]=UIWidgets.Node("Growth shared ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownGroundGraphic>();ground[i].BindPreview(new TownPoint(0,0),.70f);
                centralGround[i]=UIWidgets.Node("Growth central master ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownCentralGroundGraphic>();centralGround[i].BindPreview(new TownPoint(0,0),.70f);
                westGround[i]=UIWidgets.Node("Growth west master ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownWestGroundGraphic>();westGround[i].BindPreview(new TownPoint(0,0),.70f);
                eastGround[i]=UIWidgets.Node("Growth east master ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownEastGroundGraphic>();eastGround[i].BindPreview(new TownPoint(0,0),.70f);
                northGround[i]=UIWidgets.Node("Growth north master ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownNorthGroundGraphic>();northGround[i].BindPreview(new TownPoint(0,0),.70f);
                southGround[i]=UIWidgets.Node("Growth south garden master ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownSouthGroundGraphic>();southGround[i].BindPreview(new TownPoint(0,0),.70f);
                scenery[i]=preview.gameObject.AddComponent<TownSceneryView>();scenery[i].BindPreview(new TownPoint(0,0),.70f);
                fountain[i]=UIWidgets.Node("Growth fountain",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownFountainGraphic>();fountain[i].BindPreview(new TownPoint(0,0),.70f);
                growth[i]=UIWidgets.Node("Growth preview decorations",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownGrowthView>();growth[i].BindPreview(c,new TownPoint(0,0),.70f);
                var text=c.Widgets.Label(i==0?"Growth before label":"Growth after label",i==0?"お届けの前":"お届けのあと",column,new Rect(0,0,1,1),12);UIWidgets.Top(text.rectTransform,4,206,4,24);if(i==1)afterLabel=text;
            }
            Show(TownDistrictId.Central);
        }
        private void Show(TownDistrictId district)
        {
            var region=TownDistrictLayout.LegacyRegion(district);
            var anchor=TownGrowthPresentation.FocusForDistrict(district,delivery.CurrentTownLevel);
            var center=new TownPoint(anchor.X,anchor.Y+.07f);
            for(int i=0;i<2;i++)
            {
                ground[i].SetPreview(center,.70f);centralGround[i].SetPreview(center,.70f);westGround[i].SetPreview(center,.70f);eastGround[i].SetPreview(center,.70f);northGround[i].SetPreview(center,.70f);southGround[i].SetPreview(center,.70f);scenery[i].SetTownLevel(i==0?delivery.PreviousTownLevel:delivery.CurrentTownLevel);scenery[i].SetPreview(center,.70f);fountain[i].SetPreview(center,.70f);
                growth[i].SetPreview(center,.70f);growth[i].SetRegion(region);growth[i].Apply(i==0?delivery.PreviousTownLevel:delivery.CurrentTownLevel);growth[i].transform.SetAsLastSibling();
            }
            growth[1].HighlightNewStage(delivery.PreviousTownLevel,delivery.CurrentTownLevel);
            afterLabel.text=TownGrowthPresentation.ChangeSummary(district,delivery.PreviousTownLevel,delivery.CurrentTownLevel);
            for(int i=0;i<buttons.Length;i++)buttons[i].GetComponent<RoundedPanelGraphic>().color=(int)district==i?UIWidgets.BlushPaper:UIWidgets.Cream;
        }
    }
}
