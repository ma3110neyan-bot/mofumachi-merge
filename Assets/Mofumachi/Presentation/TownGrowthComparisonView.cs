using Mofumachi.Core;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    // Compare the exact previous/current persisted growth stages. This view never
    // awards progress and never changes the background of the playable town.
    public sealed class TownGrowthComparisonView : MonoBehaviour
    {
        private readonly RawImage[] images=new RawImage[2];
        private readonly TownGrowthView[] growth=new TownGrowthView[2];
        private readonly Button[] buttons=new Button[3];
        private DeliveryPresentation delivery;
        public void Bind(ScreenContext c,DeliveryPresentation result)
        {
            delivery=result;var root=(RectTransform)transform;
            for(int i=0;i<3;i++)
            {
                var region=(TownRegionId)i;
                var column=UIWidgets.Node("Growth region column",root,new Rect(i/3f,0,1/3f,1));
                buttons[i]=c.Widgets.Button("Growth region "+region,i==0?"西のカフェ":i==1?"中央広場":"東の花屋",column,new Rect(0,0,1,1),()=>Show(region),true);
                UIWidgets.Top((RectTransform)buttons[i].transform,4,54,4,48);buttons[i].GetComponentInChildren<Text>().fontSize=14;
            }
            for(int i=0;i<2;i++)
            {
                var column=UIWidgets.Node("Growth comparison "+i,root,new Rect(i*.5f,0,.5f,1));
                var preview=UIWidgets.Node(i==0?"Growth before preview":"Growth after preview",column,new Rect(0,0,1,1));UIWidgets.Top(preview,6,108,6,96);
                preview.gameObject.AddComponent<RectMask2D>();images[i]=preview.gameObject.AddComponent<RawImage>();images[i].raycastTarget=false;images[i].texture=Resources.Load<Texture2D>(TownWorldMap.Resource);
                var decor=UIWidgets.Node("Growth preview decorations",preview,new Rect(0,0,1,1));growth[i]=decor.gameObject.AddComponent<TownGrowthView>();growth[i].Bind(c,images[i]);
                var text=c.Widgets.Label(i==0?"Growth before label":"Growth after label",i==0?"お届けの前":"お届けのあと",column,new Rect(0,0,1,1),14);UIWidgets.Top(text.rectTransform,4,206,4,24);
            }
            Show(TownRegionId.Central);
        }
        private void Show(TownRegionId region)
        {
            var texture=Resources.Load<Texture2D>(TownWorldMap.Resource);
            var addition=TownGrowthPresentation.FocusFor(region,delivery.CurrentTownLevel);
            var anchor=TownWorldMap.Place(region==TownRegionId.West?"west.entry":region==TownRegionId.East?"east.entry":"square");
            float u=addition?.Position.X??(anchor.X-TownWorldMap.Left)/TownWorldMap.Width;
            float v=addition?.Position.Y??(TownWorldMap.Top-anchor.Y)/TownWorldMap.Height;
            for(int i=0;i<2;i++)
            {
                images[i].texture=texture;var rect=images[i].rectTransform.rect;
                float ratio=Mathf.Max(1,rect.width)/Mathf.Max(1,rect.height),width=.36f,height=Mathf.Min(1,width*texture.width/texture.height/ratio);
                images[i].uvRect=new Rect(Mathf.Clamp(u-width/2,0,1-width),Mathf.Clamp(1-v-height*.35f,0,1-height),width,height);
                growth[i].SetRegion(region);growth[i].Apply(i==0?delivery.PreviousTownLevel:delivery.CurrentTownLevel);
            }
            growth[1].HighlightNewStage(delivery.PreviousTownLevel,delivery.CurrentTownLevel);
            for(int i=0;i<buttons.Length;i++)buttons[i].GetComponent<RoundedPanelGraphic>().color=(int)region==i?UIWidgets.BlushPaper:UIWidgets.Cream;
        }
    }
}
