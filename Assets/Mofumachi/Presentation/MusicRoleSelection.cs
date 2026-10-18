using System;
namespace Mofumachi.Presentation
{
    // Separate auxiliary preferences. No changes to game save v3 or audio volumes.
    public sealed class MusicRoleSelection
    {
        private readonly IMusicPreferenceStore[] stores;private readonly string[] songs=new string[3];
        public string Error {get;private set;}="";
        public MusicRoleSelection(IMusicPreferenceStore title,IMusicPreferenceStore town,IMusicPreferenceStore merge,IMusicPreferenceStore legacy=null)
        {
            stores=new[]{title,town,merge};string[] defaults={"title-letter","town","merge-picnic"};
            string oldId=null;try{oldId=legacy?.Load();}catch(Exception){Error="音楽の設定を読み込めませんでした。";}
            var oldTrack=MusicCatalog.Find(oldId);
            for(int i=0;i<3;i++)
            {
                string id=null;try{id=stores[i].Load();}catch(Exception){Error="音楽の設定を読み込めませんでした。";}
                if(string.IsNullOrEmpty(id)&&oldTrack!=null&&(int)oldTrack.Role==i)id=oldId;
                var track=MusicCatalog.Find(id);songs[i]=track!=null&&(int)track.Role==i?id:defaults[i];
            }
        }
        public string For(MusicRole role)=>songs[(int)role];
        public bool Select(string id)
        {
            var track=MusicCatalog.Find(id);if(track==null)return false;int index=(int)track.Role;
            if(songs[index]==id){Error="";return true;}
            try{stores[index].Save(id);}catch(Exception){Error="選曲を保存できませんでした。前の曲に戻します。";return false;}
            songs[index]=id;Error="";return true;
        }
    }
}
