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
        public const string MergeTitle="Merge";
        public const string TownOutings="ベンチ お花 噴水 ベンチのそばでひと休み♪ お花を眺めて、のんびり。 噴水の音が心地いいね。";
        public const string RegionLabels="西のカフェ 中央の街 東の小道 西の庭 眺める ここを おさんぽ 主人公へ もどる カフェ・ベンチで、ひと休み♪ 花の小道と工房を見てみよう♪ お隣の街へ、おさんぽに行ってくるね♪";
        public const string FrameLabels="の生成枠 選ぶ 生成枠を片づけますか？ 生成枠を片づけます。 依頼部屋へ 生成枠 配置中 枠を置く 生成枠を置いてから作ろう。 生成枠を整理 選択中 選択した 個を片づける 対象一覧 生成枠を置く アイテムは残ります。 保存に失敗した場合はすべて戻します。 生成枠が変わりました。選び直してください。 整理モード 片づける生成枠を選んでください。 盤面のアイテムは消えません。 終了 生成枠を片づけました。";
        public const string PopLabels="Merge モモ ルル ポム フィオ ミエル ノア";
        public const string PolishLabels="会話 おさんぽ 交流 押したまま 広場 新しい一面を発見 見つけたリアクション 図鑑に新しい発見 あと1つ 受取 今日の小さな発見 お話とおさんぽで、この子のページが増えていくよ NEW";
        public const string SeamlessLabels="北の街・依頼小屋 南の花・ショップ 北の依頼小屋 北の依頼小屋へ 南のショップへ 北の依頼小屋へ ↑ 南のショップへ ↓ 北の依頼小屋 南のお花ショップ タップして入る 入口までおさんぽ中 扉がひらくよ 8秒試聴 TOPで聴く Mergeで聴く 街で聴く TOP候補A TOP候補B Merge候補A Merge候補B 使う お花と飾りの小さなお店へ、ようこそ♪ 飾り棚は、ただいま準備中です。";
        public const string TitleReturn="TOPに戻る";
        public const string PartnerSelection="パートナー選択 この子にする パートナーを選び直す パートナーを6名の中から選んでください。";
        public const string UnknownQuest="保存された依頼を確認できません。";
        public const string FinaleDescription="仲間へのお届けで、もふまちが華やかになりました。";
        public const string DeliveryThanks="ありがとう！";
        public const string MergeSelectionHint="同じ種類・Lv.を重ねよう";
        public const string MergeBusy="合成中です。少し待ってから操作しよう。";
        public const string BoardFull="満杯です。マス選択→「生成枠／整理」へ。\n「盤面：…を片づける」で空きを作ろう。";
        public const string OutsideBoard="盤面の外です。マスの中へ移動してください。";
        public const string EmptySource="空のマスです。アイテムのあるマスを選ぼう。";
        public const string SameCell="元のマスです。別のマスへ移動してください。";
        public const string DifferentKind="種類が違います。同じ種類を重ねよう。";
        public const string DifferentLevel="Lv.が違います。同じLv.を重ねよう。";
        public const string HighestLevel="最高Lv.です。これ以上は合成できません。";
        public const string ProductionMenuHint="種類を選ぶと、下の「作る」ボタンが変わります。";
        public const string ExperienceLabels="道をタップしておさんぽ 出口へ歩く 行き先を選ぶ 道や看板をタップしてね 音楽をえらぶ 試聴中 選択中 止める 聴く 選ぶ 秒 下へスクロールすると、BGMを選べます♪ 曲を選びました 音楽の選択を保存できませんでした。元の曲を続けます。";
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
        public const string EastWestLabels="中央広場 西のカフェ通り 東の花屋通り 西の依頼小屋 東のお花ショップ ← → メニュー 街のてちょう すぐ行く ファストトラベル カフェまで歩く 花屋まで歩く 依頼小屋へ歩く 道をタップして、おさんぽ。 キャラを押したままでも歩けるよ♪ 物語のおもいで 西も中央も東も";
        public static IReadOnlyList<string> All { get; } = new[]{MergeTitle,TownOutings,FrameLabels,UnknownQuest,FinaleDescription,MergeSelectionHint,"章のおもいでへ おもいでをふりかえる 章のおもいで お祝いのひととき 最初の物語をふりかえる 次の章へ 保存してTOPへ つぎのおもいで： 仲間とお茶会をひらこう",NoticeTitle,NoticeBody,NoticeConfirm,TitleReturn,DeliveryThanks,"もふまちメルジュ","もふもふの街で、ちいさなお茶会を。","つくって、届けて、街を育てよう。","はじめる","もふまち","キャラ依頼","お茶会の準備","お茶 Lv.2 × 1","報酬 30 Coins","街 Lv.","設定","戻る","依頼を受ける","依頼完了","合成してお茶会の準備をしよう","納品する","街の仲間にお茶を届けよう","同じお茶を重ねて合成","お茶を作る","納品完了！","街が成長しました","街へ戻る","BGM","SE","音量","ON","OFF","街の成長を見る",PartnerSelection,"いいお天気だね♪ 広場をおさんぽしよう！ お茶のいい香りがするね。 お花がいっぱい！ みんなで街を育てよう。 今日は何を届けようかな？ 依頼部屋に入る ショップへ ショップ お店は準備中です。 販売機能は未実装です。 アイテムを選ぶ 選んだアイテムを片づける 片づけますか？ このアイテム1個を取り除きます。 アイテムが変わりました。選び直してください。 すべての依頼が完了しました。 街のみんなとお祝い アイテムを合成しましょう。 納品が完了しました みんなへのお届けが、街を明るくします。"}.Concat(new[]{RegionLabels}).Concat(System.Enum.GetValues(typeof(TownRegionId)).Cast<TownRegionId>().SelectMany(r=>TownRegionCatalog.ForLevel(r,6).Map.Places.SelectMany(p=>new[]{p.Name,p.Dialogue}))).Concat(TownPlaceCatalog.ForLevel(6).Places.SelectMany(p=>new[]{p.Name,p.Dialogue})).Concat(Enumerable.Range(0,7).Select(TownPlaceCatalog.GrowthHint)).Concat(new[]{"街の先までおさんぽしよう♪ こんにちは、 散歩道の先へ お祝いの道へ 作る 置く 家の前 左のベンチ 右のベンチ 花の小道 石畳の散歩道 お祝いの道"}).Concat(new[]{MergeBusy,BoardFull,OutsideBoard,EmptySource,SameCell,DifferentKind,DifferentLevel,HighestLevel,ProductionMenuHint,DiscardMenuHint,"種類 整理 未受注 受注中 納品できます！ まず依頼を見るへ Lv.が違います。 同じLv.を重ねよう。 元のマスです。 別のマスへ移動してください。 マスを選んで片づける 片づける前に確認できます。 画面サイズが変わりました。 もう一度操作してください。 個必要です。 あと を選びました。 そろいました！ 納品するで届けよう。"}).Concat(ChapterCatalog.All.SelectMany(c=>new[]{c.Title,c.Objective,c.ClosingLine})).Concat(ItemCatalog.All.Select(i=>i.Name)).Concat(QuestCatalog.All.SelectMany(q=>new[]{q.Title,q.Dialogue})).Concat(CharacterCatalog.All.SelectMany(c => new[]{c.Name,c.Description})).Concat(TownDistrictLayout.All.Select(TownDistrictLayout.Name)).Concat(MergePresentation.Text).Concat(QuestStoryPresentation.Text).Concat(TownNextGoal.Text).Concat(TownCharacterBook.Text).Concat(new[]{PolishLabels,SeamlessLabels,EastWestLabels}).Concat(TownCharacterDialogue.Text).Concat(new[]{"北のエリア 南のエリア 北のカフェへ ↑ ↓ 南の花の小道へ 中央公園へ 今日のもふまち 今日のお花を集めたよ すぐ移動 北のカフェ 南の花の小道 中央公園 合成を3回 仲間とお話を1回 お隣の地域まで歩く 仲間の図鑑 今日のお願い 集めたお花 出会った回数 達成した依頼 リアクション お話する この子のページ"}).Concat(new[]{"と、どこへ行こう？ 入口まで歩いて、お隣の街へ♪ 西のカフェへ 西のカフェのお庭へ 中央の噴水へ 東の花の小道へ 近くのベンチへ 依頼部屋へ歩く ショップへ歩く 景色だけ見る 主人公は動かず、景色を眺めます 西の庭を眺める 中央の街を眺める 東の小道を眺める 閉じる お隣の街へ… おさんぽ中 · キャラをタップしておさんぽ 歩いています♪ いまここ 歩いて行く お届けできました あなたのお届けが、街の景色になっていきます。 お届けの前 お届けのあと"}).Concat(new[]{ExperienceLabels}).Concat(MusicCatalog.All.SelectMany(t=>new[]{t.Title,t.Description})).ToArray();
    }
}
