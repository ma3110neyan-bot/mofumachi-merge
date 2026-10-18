using System;
using System.Collections.Generic;
using System.Linq;
namespace Mofumachi.Presentation
{
    public enum MusicRole { Title,Town,Merge }
    public sealed class MusicTrack
    {
        public string Id {get;} public string Title {get;} public string Resource {get;} public string Description {get;}
        public MusicRole Role=>Id.StartsWith("title-",StringComparison.Ordinal)||Id=="beginning"?MusicRole.Title:Id=="town"?MusicRole.Town:MusicRole.Merge;
        internal MusicTrack(string id,string title,string resource,string description){Id=id;Title=title;Resource=resource;Description=description;}
    }
    public static class MusicCatalog
    {
        public const string DefaultId="town";
        public static IReadOnlyList<MusicTrack> All {get;}=Array.AsReadOnly(new[]{
            new MusicTrack("beginning","もふまちのはじまり","bgm-beginning-loop","やさしいピアノのお茶会"),
            new MusicTrack("town","おさんぽタウン","bgm-town-loop","街を歩く、軽やかなメロディ"),
            new MusicTrack("merge-time","もふもふマージタイム","bgm-merge-loop","はずむマリンバときらめき"),
            new MusicTrack("title-letter","花あかりの招待状","bgm-title-letter","TOP候補A · ピアノとベル、やさしいワルツ"),
            new MusicTrack("title-garden","ひだまりの庭の扉","bgm-title-garden","TOP候補B · マリンバと弦、明るいおとぎ話"),
            new MusicTrack("merge-picnic","こもれびピクニック","bgm-merge-picnic","Merge候補A · 軽快な木の音とピアノ"),
            new MusicTrack("merge-ribbon","リボンを結ぶ午後","bgm-merge-ribbon","Merge候補B · はずむピアノ、きらめくベル"),
            new MusicTrack("merge-pop-promenade","はずむ花みち","bgm-merge-pop-promenade","Merge候補C · 120 BPM、軽快なピアノとマリンバのPOP")});
        public static IReadOnlyList<MusicTrack> Candidates {get;}=Array.AsReadOnly(All.Where(t=>t.Id!="beginning"&&t.Id!="merge-time").OrderBy(t=>t.Role).ToArray());
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
