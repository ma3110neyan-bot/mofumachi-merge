using System;
using Mofumachi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Mofumachi.Presentation
{
    // One reusable background changes only under the opaque midpoint of the fade.
    // The six tiny flower icons share the existing UI atlas; no particles or render textures.
    public sealed class TownRegionView : MonoBehaviour
    {
        private RawImage background;
        private Texture2D central;
        private TownLifeView life;
        private TownRegionSession session;
        private TownViewportView cameraView;
        private TownAmbientGraphic water;
        private TownGrowthView growth;
        private RoundedPanelGraphic fade;
        private CanvasGroup residents;
        private readonly Texture2D[] textures=new Texture2D[3];
        private readonly TownPoint[] centers={new TownPoint(.5f,.5f),new TownPoint(.5f,.5f),new TownPoint(.5f,.5f)};
        private readonly UIIconGraphic[] petals=new UIIconGraphic[6];
        private TownRegionId displayed=TownRegionId.Central;
        public Graphic TransitionGraphic=>fade;
        public int VisibleBackgroundCount=>background!=null&&background.isActiveAndEnabled&&background.texture!=null?1:0;
        public float TransitionOpacity=>fade==null?0:fade.color.a;
        public void Bind(ScreenContext c,RawImage image,TownLifeView view,TownRegionSession owner)
        {
            background=image;life=view;session=owner;cameraView=view.GetComponent<TownViewportView>();
            central=Resources.Load<Texture2D>("Mofumachi/TownBackground");textures[(int)TownRegionId.Central]=central;
            water=view.transform.parent.GetComponentInChildren<TownAmbientGraphic>();growth=view.GetComponentInChildren<TownGrowthView>();
            residents=view.gameObject.AddComponent<CanvasGroup>();
            // Background-only glow stays behind the HUD. Residents fade with it, so
            // the entry position never visibly jumps at the midpoint.
            fade=c.Widgets.Panel("Town region transition",background.rectTransform,new Rect(0,0,1,1),new Color(1,.985f,.94f,0));
            fade.radius=0;fade.decorated=false;fade.raycastTarget=false;
            for(int i=0;i<petals.Length;i++)
            {
                petals[i]=c.Widgets.Icon("Region flower light "+i,UIIcon.Flower,fade.rectTransform,new Rect(.15f+i*.14f,.48f,.035f,.02f));
                petals[i].raycastTarget=false;
            }
            fade.gameObject.SetActive(false);session.RegionChanged+=ShowRegion;session.HeroTransitionChanged+=SetTransition;ShowRegion(session.ViewingRegion);
        }
        private Texture2D TextureFor(TownRegionId region)
        {
            var texture=textures[(int)region];if(texture!=null)return texture;
            texture=Resources.Load<Texture2D>(TownRegionCatalog.ForLevel(region,0).BackgroundResource);
            if(texture==null)throw new InvalidOperationException("Town region background is missing: "+region);
            textures[(int)region]=texture;return texture;
        }
        public void ShowRegion(TownRegionId region)
        {
            if(background==null||session==null||!isActiveAndEnabled)return;
            if(cameraView!=null)centers[(int)displayed]=cameraView.Center;
            background.texture=TextureFor(region);displayed=region;cameraView?.ChangeRegion(background);cameraView?.RestoreCenter(centers[(int)region]);
            if(region==session.HeroRegion)foreach(var actor in life.Actors)if(actor.Controlled)cameraView?.FrameImmediately(actor.Position);
            water?.SetRegion(region);growth?.SetRegion(region);life.RefreshViewport();
        }
        public void SetTransition(TownRegionId from,TownRegionId to,float opacity)
        {
            if(fade==null||!float.IsFinite(opacity))return;
            opacity=Mathf.Clamp01(opacity);fade.color=new Color(1,.985f,.94f,opacity);fade.gameObject.SetActive(opacity>0);
            residents.alpha=1-opacity;residents.blocksRaycasts=opacity<=0;residents.interactable=opacity<=0;
            for(int i=0;i<petals.Length;i++)
            {
                var p=petals[i];p.color=new Color(1,1,1,opacity);p.rectTransform.anchorMin=new Vector2(.10f+i*.14f,.40f+opacity*.10f+(i%2)*.08f);
                p.rectTransform.anchorMax=p.rectTransform.anchorMin+new Vector2(.038f,.024f);
                p.rectTransform.localRotation=Quaternion.Euler(0,0,opacity*24+(i%2)*16);
            }
        }
        internal TownPoint[] CaptureCenters()
        {
            if(cameraView!=null)centers[(int)displayed]=cameraView.Center;return (TownPoint[])centers.Clone();
        }
        internal void RestoreCenters(System.Collections.Generic.IReadOnlyList<TownPoint> saved)
        {
            if(saved==null||saved.Count!=3)return;for(int i=0;i<3;i++)centers[i]=saved[i];cameraView?.RestoreCenter(centers[(int)displayed]);
        }
        public void RestoreCentral()
        {
            if(fade!=null){fade.gameObject.SetActive(false);Destroy(fade.gameObject);fade=null;}
            if(residents!=null){residents.alpha=1;residents.blocksRaycasts=true;residents.interactable=true;}
            if(background==null)return;background.texture=central;water?.SetRegion(TownRegionId.Central);growth?.SetRegion(TownRegionId.Central);
            var cover=background.GetComponent<BackgroundCover>();if(cover!=null){cover.enabled=true;cover.FitNow();}
        }
        private void OnDisable()
        {
            if(session!=null){session.RegionChanged-=ShowRegion;session.HeroTransitionChanged-=SetTransition;}RestoreCentral();
        }
    }
}
