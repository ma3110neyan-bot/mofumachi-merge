using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Presentation
{
    public sealed class MusicTrack
    {
        public string Id {get;} public string Title {get;} public string Resource {get;} public string Description {get;}
        internal MusicTrack(string id,string title,string resource,string description){Id=id;Title=title;Resource=resource;Description=description;}
    }
    public static class MusicCatalog
    {
        public const string DefaultId="town";
        public static IReadOnlyList<MusicTrack> All {get;}=Array.AsReadOnly(new[]{
            new MusicTrack("beginning","もふまちのはじまり","bgm-beginning-loop","やさしいピアノのお茶会"),
            new MusicTrack("town","おさんぽタウン","bgm-town-loop","街を歩く、軽やかなメロディ"),
            new MusicTrack("merge-time","もふもふマージタイム","bgm-merge-loop","はずむマリンバときらめき")});
        public static MusicTrack Find(string id)=>All.FirstOrDefault(t=>t.Id==id);
    }
    public interface IMusicPreferenceStore {string Load();void Save(string id);}
    public sealed class MusicSelection
    {
        private readonly IMusicPreferenceStore store;private string preview;
        public string SelectedId {get;private set;} public string ActiveId=>preview??SelectedId;
        public bool IsPreviewing=>preview!=null;public string Error {get;private set;}="";
        public MusicSelection(IMusicPreferenceStore preferences)
        {
            store=preferences??throw new ArgumentNullException(nameof(preferences));string id=null;
            try{id=store.Load();}catch(Exception){Error="音楽の設定を読み込めませんでした。";}
            SelectedId=MusicCatalog.Find(id)?.Id??MusicCatalog.DefaultId;
        }
        public bool Preview(string id){if(MusicCatalog.Find(id)==null)return false;preview=id;return true;}
        public void CancelPreview()=>preview=null;
        public bool Select(string id)
        {
            if(MusicCatalog.Find(id)==null)return false;CancelPreview();
            if(id==SelectedId){Error="";return true;}
            try{store.Save(id);}catch(Exception){Error="選曲を保存できませんでした。前の曲に戻します。";return false;}
            SelectedId=id;Error="";return true;
        }
    }
}
