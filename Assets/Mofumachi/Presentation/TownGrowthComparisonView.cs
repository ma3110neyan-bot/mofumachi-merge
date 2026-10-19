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
        private readonly TownSceneryView[] scenery=new TownSceneryView[2];
        private readonly TownFountainGraphic[] fountain=new TownFountainGraphic[2];
        private readonly TownGrowthView[] growth=new TownGrowthView[2];
        private readonly Button[] buttons=new Button[3];private DeliveryPresentation delivery;
        public void Bind(ScreenContext c,DeliveryPresentation result)
        {
            delivery=result;var root=(RectTransform)transform;
            for(int i=0;i<3;i++)
            {
                var region=(TownRegionId)i;var column=UIWidgets.Node("Growth region column",root,new Rect(i/3f,0,1/3f,1));
                buttons[i]=c.Widgets.Button("Growth region "+region,i==0?"西のカフェ":i==1?"中央広場":"東の花屋",column,new Rect(0,0,1,1),()=>Show(region),true);
                UIWidgets.Top((RectTransform)buttons[i].transform,4,54,4,48);buttons[i].GetComponentInChildren<Text>().fontSize=14;
            }
            for(int i=0;i<2;i++)
            {
                var column=UIWidgets.Node("Growth comparison "+i,root,new Rect(i*.5f,0,.5f,1));
                var preview=UIWidgets.Node(i==0?"Growth before preview":"Growth after preview",column,new Rect(0,0,1,1));UIWidgets.Top(preview,6,108,6,96);preview.gameObject.AddComponent<RectMask2D>();
                ground[i]=UIWidgets.Node("Growth shared ground",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownGroundGraphic>();ground[i].BindPreview(new TownPoint(0,0),.70f);
                scenery[i]=preview.gameObject.AddComponent<TownSceneryView>();scenery[i].BindPreview(new TownPoint(0,0),.70f);
                fountain[i]=UIWidgets.Node("Growth fountain",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownFountainGraphic>();fountain[i].BindPreview(new TownPoint(0,0),.70f);
                growth[i]=UIWidgets.Node("Growth preview decorations",preview,new Rect(0,0,1,1)).gameObject.AddComponent<TownGrowthView>();growth[i].BindPreview(c,new TownPoint(0,0),.70f);
                var text=c.Widgets.Label(i==0?"Growth before label":"Growth after label",i==0?"お届けの前":"お届けのあと",column,new Rect(0,0,1,1),14);UIWidgets.Top(text.rectTransform,4,206,4,24);
            }
            Show(TownRegionId.Central);
        }
        private void Show(TownRegionId region)
        {
            var addition=TownGrowthPresentation.FocusFor(region,delivery.CurrentTownLevel);
            var anchor=addition?.Position??TownWorldMap.Place(region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square");
            var center=new TownPoint(anchor.X,anchor.Y+.07f);
            for(int i=0;i<2;i++)
            {
                ground[i].SetPreview(center,.70f);scenery[i].SetPreview(center,.70f);fountain[i].SetPreview(center,.70f);
                growth[i].SetPreview(center,.70f);growth[i].SetRegion(region);growth[i].Apply(i==0?delivery.PreviousTownLevel:delivery.CurrentTownLevel);growth[i].transform.SetAsLastSibling();
            }
            growth[1].HighlightNewStage(delivery.PreviousTownLevel,delivery.CurrentTownLevel);
            for(int i=0;i<buttons.Length;i++)buttons[i].GetComponent<RoundedPanelGraphic>().color=(int)region==i?UIWidgets.BlushPaper:UIWidgets.Cream;
        }
    }
}
