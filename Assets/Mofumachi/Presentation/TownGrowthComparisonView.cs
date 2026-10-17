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
                buttons[i]=c.Widgets.Button("Growth region "+region,i==0?"北エリア":i==1?"中央公園":"南エリア",column,new Rect(0,0,1,1),()=>Show(region),true);
                UIWidgets.Top((RectTransform)buttons[i].transform,4,54,4,48);buttons[i].GetComponentInChildren<Text>().fontSize=14;
            }
            for(int i=0;i<2;i++)
            {
                var column=UIWidgets.Node("Growth comparison "+i,root,new Rect(i*.5f,0,.5f,1));
                var preview=UIWidgets.Node(i==0?"Growth before preview":"Growth after preview",column,new Rect(0,0,1,1));UIWidgets.Top(preview,6,108,6,96);
                preview.gameObject.AddComponent<RectMask2D>();images[i]=preview.gameObject.AddComponent<RawImage>();images[i].raycastTarget=false;
                var decor=UIWidgets.Node("Growth preview decorations",preview,new Rect(0,0,1,1));growth[i]=decor.gameObject.AddComponent<TownGrowthView>();growth[i].Bind(c,images[i]);
                var text=c.Widgets.Label(i==0?"Growth before label":"Growth after label",i==0?"お届けの前":"お届けのあと",column,new Rect(0,0,1,1),14);UIWidgets.Top(text.rectTransform,4,206,4,24);
            }
            Show(TownRegionId.Central);
        }
        private void Show(TownRegionId region)
        {
            var texture=Resources.Load<Texture2D>(TownRegionCatalog.ForLevel(region,delivery.CurrentTownLevel).BackgroundResource);
            var addition=TownGrowthPresentation.For(region,delivery.CurrentTownLevel).LastOrDefault();
            float focus=addition==null?.49f:addition.Position.Y+addition.Width/TownGrowthPropGraphic.Aspect(addition.Prop)*texture.width/texture.height*.5f;
            for(int i=0;i<2;i++)
            {
                images[i].texture=texture;var rect=images[i].rectTransform.rect;
                float ratio=Mathf.Max(1,rect.width)/Mathf.Max(1,rect.height),height=Mathf.Clamp((float)texture.width/texture.height/ratio,.1f,1);
                images[i].uvRect=new Rect(0,Mathf.Clamp(focus-height/2,0,1-height),1,height);
                growth[i].SetRegion(region);growth[i].Apply(i==0?delivery.PreviousTownLevel:delivery.CurrentTownLevel);
            }
            growth[1].HighlightNewStage(delivery.PreviousTownLevel,delivery.CurrentTownLevel);
            for(int i=0;i<buttons.Length;i++)buttons[i].GetComponent<RoundedPanelGraphic>().color=(int)region==i?UIWidgets.BlushPaper:UIWidgets.Cream;
        }
    }
}
