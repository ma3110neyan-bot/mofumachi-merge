using UnityEngine;
using UnityEngine.UI;
namespace Mofumachi.Presentation
{
    public sealed class MusicSettingsView : MonoBehaviour
    {
        private AudioManager audio;private Text state,chooseLabel,previewLabel;private Button choose;private string track;
        public static void Build(RectTransform content,ScreenContext c,float top)
        {
            var header=c.Widgets.StoryHeading("Music selection heading","音楽をえらぶ",content,new Rect(0,0,1,1),18);UIWidgets.Top(header.rectTransform,18,top,18,32);
            for(int i=0;i<MusicCatalog.All.Count;i++)
            {
                var song=MusicCatalog.All[i];var card=c.Widgets.OrnatePanel("Music track "+song.Id,content,new Rect(0,0,1,1),UIWidgets.Cream,12).rectTransform;
                UIWidgets.Top(card,12,top+40+i*100,12,92);
                c.Widgets.StoryLabel("Music title "+song.Id,song.Title,card,new Rect(.035f,.58f,.93f,.37f),15);
                var status=c.Widgets.Label("Music state "+song.Id,"",card,new Rect(.035f,.06f,.33f,.47f),12);
                var preview=c.Widgets.Button("Preview music "+song.Id,"試聴",card,new Rect(.39f,.02f,.28f,.54f),()=>c.Flow.PreviewMusic(song.Id),true);preview.GetComponentInChildren<Text>().fontSize=13;
                var select=c.Widgets.Button("Select music "+song.Id,"選ぶ",card,new Rect(.69f,.02f,.28f,.54f),()=>c.Flow.SelectMusic(song.Id));select.GetComponentInChildren<Text>().fontSize=13;
                var view=card.gameObject.AddComponent<MusicSettingsView>();view.audio=AudioManager.Instance;view.track=song.Id;view.state=status;view.choose=select;view.chooseLabel=select.GetComponentInChildren<Text>();view.previewLabel=preview.GetComponentInChildren<Text>();view.Refresh();
            }
        }
        private void Update()=>Refresh();
        private void Refresh()
        {
            if(audio==null||choose==null)return;bool selected=audio.SelectedMusicId==track,playing=audio.PreviewMusicId==track;
            state.text=playing?"試聴中 · 8秒":selected?"選択中":"";choose.interactable=!selected;chooseLabel.text=selected?"選択中":"選ぶ";previewLabel.text=playing?"止める":"試聴";
        }
    }
}
