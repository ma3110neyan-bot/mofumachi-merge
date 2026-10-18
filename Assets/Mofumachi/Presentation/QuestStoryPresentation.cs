using System;
using System.Collections.Generic;
using System.Linq;

namespace Mofumachi.Presentation
{
    public sealed class QuestStory
    {
        public string QuestId {get;} public string RequesterId {get;}
        public string Before {get;} public string Thanks {get;} public string Reunion {get;} public string TownMoment {get;}
        internal QuestStory(string id,string who,string before,string thanks,string reunion,string moment)
        {QuestId=id;RequesterId=who;Before=before;Thanks=thanks;Reunion=reunion;TownMoment=moment;}
    }
    // Copy only: never advances quests or grants rewards. Existing completed IDs
    // unlock reunion lines after loading, without adding any save fields.
    public static class QuestStoryPresentation
    {
        public static IReadOnlyList<QuestStory> All {get;}=Array.AsReadOnly(new[]{
            new QuestStory("tea-01","lulu","いい香りのお茶で、モモをお茶会に誘いたいな。","わあ、いい香り！\nモモとお庭で半分こするね。","モモとお茶を飲んだよ。次はパンもあるといいな♪","街のあちこちに、お茶会のお花が届きました。"),
            new QuestStory("bread-01","momo","ルルのお茶に合うパンを焼いて、半分こしたいな。","ふわふわのパンだね！\nルルとカフェで待ってるよ。","ルルと分けたパン、おいしかったな。お花も見に行こう♪","西のカフェに、お茶会の席が増えました。"),
            new QuestStory("flower-01","miel","お茶会の帰り道に、小さな花の広場を作りたいの。","このお花、だいすき。\nみんなの帰り道に飾るね。","あのお花、ポムも気に入ってくれたの。うれしいな♪","街のあちこちに、お花の街灯が灯りました。"),
            new QuestStory("tea-02","pom","花を見ながら、のんびり。みんなのお茶もお願いね。","ほっとする味だねえ。\n工房のみんなにも届けよう。","ミエルのお花とお茶、ゆっくり楽しめたよ。","東の花屋に、手作りの飾りが並びました。"),
            new QuestStory("flower-02","fio","工房のお花を飾って、街のお祝いを準備しよう！","ぴったりのお花だね！\nノアとアーチを飾ってくる！","作ったアーチ、見てくれた？ノアも大はりきりだよ！","街のあちこちに、お祝いの花のアーチができました。"),
            new QuestStory("bread-02","noah","みんなの好きなパンとお花で、お祝いを開こう！","みんなの笑顔も集まった！\nさあ、街のお祝いを始めよう。","パンもお花も、みんなのお気に入り。次は一緒に歩こう♪","西も中央も東も、リボンでつながるお祝いの日です。")});
        public static QuestStory For(string id)=>All.FirstOrDefault(s=>s.QuestId==id);
        public static string ReunionFor(string characterId,IEnumerable<string> completed)
        {return completed==null?"":All.LastOrDefault(s=>s.RequesterId==characterId&&completed.Contains(s.QuestId))?.Reunion??"";}
        public static IEnumerable<string> Text=>All.SelectMany(s=>new[]{s.Before,s.Thanks,s.Reunion,s.TownMoment});
    }
}
