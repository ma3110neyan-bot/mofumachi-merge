using UnityEngine;
namespace Mofumachi.Presentation
{
    // Package-local preferences intentionally do not extend the game-save v3
    // schema. Android's package sandbox separates Town QA and production.
    public sealed class MusicPreferenceStore : IMusicPreferenceStore
    {
        private readonly string Key;
        public MusicPreferenceStore(){Key="Mofumachi.Music.Selection.v1";}
        public MusicPreferenceStore(MusicRole role){Key="Mofumachi.Music.Role.v1."+role;}
        public string Load()=>PlayerPrefs.GetString(Key,MusicCatalog.DefaultId);
        public void Save(string id)
        {
            bool existed=PlayerPrefs.HasKey(Key);string previous=Load();
            try{PlayerPrefs.SetString(Key,id);PlayerPrefs.Save();}
            catch{if(existed)PlayerPrefs.SetString(Key,previous);else PlayerPrefs.DeleteKey(Key);throw;}
        }
    }
}
