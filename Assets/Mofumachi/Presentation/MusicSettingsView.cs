using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class MusicSettingsView : MonoBehaviour
    {
        private const float HeadingOffset=40,CardHeight=160,CardGap=8;
        internal static float RequiredHeight=>HeadingOffset+MusicCatalog.Candidates.Count*(CardHeight+CardGap)-CardGap;
        private AudioManager audio;private Text state,chooseLabel,previewLabel;private Button choose;private string track;
        public static void Build(RectTransform content,ScreenContext c,float top)
        {
            var header=c.Widgets.StoryHeading("Music selection heading","音楽をえらぶ",content,new Rect(0,0,1,1),18);UIWidgets.Top(header.rectTransform,18,top,18,32);
            for(int i=0;i<MusicCatalog.Candidates.Count;i++)
            {
                var song=MusicCatalog.Candidates[i];var card=c.Widgets.OrnatePanel("Music track "+song.Id,content,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;
                UIWidgets.Top(card,12,top+HeadingOffset+i*(CardHeight+CardGap),12,CardHeight);
                c.Widgets.StoryLabel("Music title "+song.Id,song.Title,card,new Rect(.035f,.78f,.93f,.20f),15);
                c.Widgets.NarrativeLabel("Music direction "+song.Id,song.Description,card,new Rect(.045f,.42f,.91f,.33f),13);
                var status=c.Widgets.Label("Music state "+song.Id,"",card,new Rect(.04f,.32f,.92f,.12f),11);
                var preview=c.Widgets.Button("Preview music "+song.Id,"8秒試聴",card,new Rect(.03f,.02f,.28f,.31f),()=>c.Flow.PreviewMusic(song.Id),true);preview.GetComponentInChildren<Text>().fontSize=13;
                var select=c.Widgets.Button("Select music "+song.Id,"使う",card,new Rect(.34f,.02f,.25f,.31f),()=>c.Flow.SelectMusic(song.Id));select.GetComponentInChildren<Text>().fontSize=13;
                string scene=song.Role==MusicRole.Title?"TOPで聴く":song.Role==MusicRole.Merge?"Mergeで聴く":"街で聴く";
                var listen=c.Widgets.Button("Listen in scene "+song.Id,scene,card,new Rect(.62f,.02f,.35f,.31f),()=>c.Flow.ListenMusicInScene(song.Id),true);listen.GetComponentInChildren<Text>().fontSize=12;
                listen.interactable=song.Role==MusicRole.Title||Mofumachi.Core.CharacterCatalog.IsKnown(c.Game.State.favoriteCharacterId);
                var view=card.gameObject.AddComponent<MusicSettingsView>();view.audio=AudioManager.Instance;view.track=song.Id;view.state=status;view.choose=select;view.chooseLabel=select.GetComponentInChildren<Text>();view.previewLabel=preview.GetComponentInChildren<Text>();view.Refresh();
            }
        }
        private void Update()=>Refresh();
        private void Refresh()
        {
            if(audio==null||choose==null)return;bool selected=audio.IsRoleSelected(track),playing=audio.PreviewMusicId==track;
            state.text=playing?"試聴中 · 8秒":selected?"選択中":"";choose.interactable=!selected;chooseLabel.text=selected?"選択中":"使う";previewLabel.text=playing?"止める":"8秒試聴";
        }
    }
}
