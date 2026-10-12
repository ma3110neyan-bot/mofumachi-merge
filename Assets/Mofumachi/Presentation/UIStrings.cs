using System.Collections.Generic;
using System.Linq;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    public static class UIStrings
    {
        public const string NoticeTitle="課金について";
        public const string NoticeBody="未成年の方は、課金する前に必ず保護者の方に相談し、許可をもらってください。";
        public const string NoticeConfirm="確認しました";
        public const string PopLabels="マージ モモ ルル ポム フィオ ミエル ノア";
        public const string TitleReturn="TOPに戻る";
        public const string PartnerSelection="パートナー選択 この子にする パートナーを選び直す パートナーを6名の中から選んでください。";
        public const string UnknownQuest="保存された依頼を確認できません。";
        public const string FinaleDescription="仲間へのお届けで、もふまちが華やかになりました。";
        public const string DeliveryThanks="ありがとう！";
        public const string MergeSelectionHint="同じ種類・Lv.を重ねよう";
        public const string MergeBusy="合成中です。少し待ってから操作しよう。";
        public const string BoardFull="盤面がいっぱいです。マスを選び「種類／整理」で片づけよう。";
        public const string OutsideBoard="盤面の外です。マスの中へ移動してください。";
        public const string EmptySource="空のマスです。アイテムのあるマスを選ぼう。";
        public const string SameCell="元のマスです。別のマスへ移動してください。";
        public const string DifferentKind="種類が違います。同じ種類を重ねよう。";
        public const string DifferentLevel="Lv.が違います。同じLv.を重ねよう。";
        public const string HighestLevel="最高Lv.です。これ以上は合成できません。";
        public const string ProductionMenuHint="種類を選ぶと、下の「作る」ボタンが変わります。";
        public const string DiscardMenuHint="盤面でマスを選んでから開こう。";

        public static bool IsAccepted(GameState state,QuestDefinition quest)=>quest!=null && state.activeQuestId==quest.Id && !state.completedQuestIds.Contains(quest.Id);
        public static string SuggestedProduction(GameStateManager game)
        {
            var quest=QuestCatalog.NextFor(game.State);
            if(quest==null)return "tea";
            var progress=game.Quests.GetProgress(quest);
            return progress.FirstOrDefault(p=>p.Available<p.Requirement.Count)?.Requirement.ItemId??quest.Requirements[0].ItemId;
        }
        public static string RequiredSummary(GameStateManager game,QuestDefinition quest)=>string.Join("　",game.Quests.GetProgress(quest).Select(p=>
            ItemCatalog.Find(p.Requirement.ItemId).Name+" Lv."+p.Requirement.Level+" "+p.Available+" / "+p.Requirement.Count));
        public static string DeliveryHint(GameStateManager game)
        {
            var quest=QuestCatalog.NextFor(game.State);
            if(quest==null)return ChapterProgress.For(game.State).AllCompleted?"すべての依頼が完了しました。":UnknownQuest;
            if(!IsAccepted(game.State,quest))return "「依頼を見る」から依頼を受けよう。";
            if(game.Quests.CheckDelivery())return "そろいました！「納品する」で届けよう。";
            var missing=game.Quests.GetProgress(quest).FirstOrDefault(p=>p.Available<p.Requirement.Count);
            return missing==null?MergeBusy:ItemCatalog.Find(missing.Requirement.ItemId).Name+" Lv."+missing.Requirement.Level+"：あと"+(missing.Requirement.Count-missing.Available)+"個。同じ種類・Lv.を合成しよう。";
        }
        public static string DropReason(MergeBoard board,int from,int to)
        {
            if(from<0 || to<0 || from>=GameState.Columns*GameState.Rows || to>=GameState.Columns*GameState.Rows)return OutsideBoard;
            if(board.IsLocked(from)||board.IsLocked(to))return MergeBusy;
            var source=board.At(from);var target=board.At(to);
            if(source==null)return EmptySource;
            if(from==to)return SameCell;
            if(target==null)return "";
            if(source.itemId!=target.itemId)return DifferentKind;
            if(source.level!=target.level)return DifferentLevel;
            return source.level>=GameState.MaxLevel?HighestLevel:"";
        }
        public static string ItemLabel(BoardItem item)=>(ItemCatalog.Find(item.itemId)?.Name??"アイテム")+" Lv."+item.level;
        public static IReadOnlyList<string> All { get; } = new[]{UnknownQuest,FinaleDescription,MergeSelectionHint,"章のおもいでへ おもいでをふりかえる 章のおもいで お祝いのひととき 最初の物語をふりかえる 次の章へ 保存してTOPへ つぎのおもいで： 仲間とお茶会をひらこう",NoticeTitle,NoticeBody,NoticeConfirm,TitleReturn,DeliveryThanks,"もふまちメルジュ","もふもふの街で、ちいさなお茶会を。","つくって、届けて、街を育てよう。","はじめる","もふまち","キャラ依頼","お茶会の準備","お茶 Lv.2 × 1","報酬 30 Coins","街 Lv.","設定","戻る","依頼を受ける","依頼完了","合成してお茶会の準備をしよう","納品する","街の仲間にお茶を届けよう","同じお茶を重ねて合成","お茶を作る","納品完了！","街が成長しました","街へ戻る","BGM","SE","音量","ON","OFF","街の成長を見る",PartnerSelection,"いいお天気だね♪ 広場をおさんぽしよう！ お茶のいい香りがするね。 お花がいっぱい！ みんなで街を育てよう。 今日は何を届けようかな？ 依頼部屋に入る ショップへ ショップ お店は準備中です。 販売機能は未実装です。 アイテムを選ぶ 選んだアイテムを片づける 片づけますか？ このアイテム1個を取り除きます。 アイテムが変わりました。選び直してください。 すべての依頼が完了しました。 街のみんなとお祝い アイテムを合成しましょう。 納品が完了しました みんなへのお届けが、街を明るくします。"}.Concat(new[]{MergeBusy,BoardFull,OutsideBoard,EmptySource,SameCell,DifferentKind,DifferentLevel,HighestLevel,ProductionMenuHint,DiscardMenuHint,"種類 整理 未受注 受注中 納品できます！ まず依頼を見るへ Lv.が違います。 同じLv.を重ねよう。 元のマスです。 別のマスへ移動してください。 マスを選んで片づける 片づける前に確認できます。 画面サイズが変わりました。 もう一度操作してください。 個必要です。 あと を選びました。 そろいました！ 納品するで届けよう。"}).Concat(ChapterCatalog.All.SelectMany(c=>new[]{c.Title,c.Objective,c.ClosingLine})).Concat(ItemCatalog.All.Select(i=>i.Name)).Concat(QuestCatalog.All.SelectMany(q=>new[]{q.Title,q.Dialogue})).Concat(CharacterCatalog.All.SelectMany(c => new[]{c.Name,c.Description})).ToArray();
    }
}
