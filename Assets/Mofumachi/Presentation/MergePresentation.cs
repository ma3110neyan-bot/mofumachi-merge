using System;
using Mofumachi.Core;
namespace Mofumachi.Presentation
{
    // Read-only presentation of the existing board and separate daily journal.
    public static class MergePresentation
    {
        public const string PlayHint="お茶・パン・お花を作って、同じ種類・Lv.を重ねよう♪";
        public static string DailySummary(TownDailyData daily)=>"今日の合成　"+Math.Min(3,Math.Max(0,daily.Merges))+" / 3";
        public static string DailyPurpose(TownDailyData daily)
        {
            if(daily.Claimed)return "今日のお花を集めたよ♪ 仲間に会いに行こう";
            if(daily.Merges<3)return "あと"+(3-daily.Merges)+"回、同じ種類・Lv.を合成しよう♪";
            if(daily.Talks<1)return "合成クリア！ 街で仲間に話しかけよう♪";
            if(daily.Walks<1)return "合成クリア！ 別の広場へおさんぽしよう♪";
            return "今日のお花を受け取りに、街へ戻ろう♪";
        }
        public static string SelectionName(BoardItem item)=>item==null?"アイテムを選ぶ":(ItemCatalog.Find(item.itemId)?.Name??"アイテム")+" Lv."+item.level;
        public static string SelectionHint(BoardItem item)=>item==null?"タップで名前・Lv.を確認":item.level>=GameState.MaxLevel?"最高Lv.！ 空きマスへ移動できるよ":"同じ種類・Lv.を重ねよう";
        public static bool IsMatch(BoardItem source,BoardItem target,bool sourceLocked,bool targetLocked)
            =>source!=null&&target!=null&&!sourceLocked&&!targetLocked&&source.itemId==target.itemId&&source.level==target.level&&source.level>=1&&source.level<GameState.MaxLevel;
        public static string RequirementCount(long available,int needed)=>available+" / "+needed+(available>=needed?"  完了":"  あと"+Math.Max(0,needed-available)+"個");
        public static string CompactFeedback(string message,string deliveryHint,string selectionHint)
        {
            if(string.IsNullOrEmpty(message)||message==deliveryHint)return "";
            foreach(var item in ItemCatalog.All)
            {
                string created=item.Name+" Lv.1を作りました。",chosen=item.Name+"を作れます。";
                if(message==created+selectionHint)return created;
                if(message==chosen+selectionHint)return chosen;
            }
            return message;
        }
        public static MergeCellRect ItemArt(int index)
        {
            float scale=MergePerspective.ArtScale(index);
            // Leave room for the 1.06 selection scale and 3-unit lift, including in a 48-unit cell.
            return new MergeCellRect(.5f-.46f*scale,.5f-.40f*scale,.92f*scale,.80f*scale);
        }
        public static string[] Text=>new[]{PlayHint,"今日の合成　0 / 3","あと3回、同じ種類・Lv.を合成しよう♪","合成クリア！ 街で仲間に話しかけよう♪","合成クリア！ 別の広場へおさんぽしよう♪","今日のお花を受け取りに、街へ戻ろう♪","今日のお花を集めたよ♪ 仲間に会いに行こう",SelectionName(null),SelectionHint(null),SelectionHint(new BoardItem("tea",3,0)),"同じ種類・Lv. → 光る1個へ","0 / 1  あと1個","Lv.UP!  2","今日のお願いへ","街で続きを見る","生成枠と整理"};
    }
    public readonly struct MergeScreenLayout
    {
        public float ProgressTop {get;} public float ProgressHeight {get;} public float TrayTop {get;} public float TrayBottom {get;}
        public float ToolbarBottom {get;} public float ToolbarHeight {get;} public float GridInset {get;} public float NavBottom {get;} public float NavHeight {get;}
        public bool TwoToolbarRows {get;}
        public float DetailsHeight=>TwoToolbarRows?24:22;
        private MergeScreenLayout(bool roomy,float feedbackHeight)
        {
            TwoToolbarRows=roomy;ProgressTop=roomy?62:50;ProgressHeight=roomy?80:72;TrayTop=roomy?146:124;
            ToolbarBottom=roomy?72+(feedbackHeight>0?feedbackHeight+6:0):52;ToolbarHeight=roomy?100:48;TrayBottom=ToolbarBottom+ToolbarHeight+(roomy?4:2);
            GridInset=roomy?4:1;NavBottom=roomy?8:2;NavHeight=roomy?58:48;
        }
        public MergeCellRect FeedbackRect(float width,float height,float textHeight)
        {
            if(TwoToolbarRows)return new MergeCellRect(14,72,width-28,Math.Max(46,textHeight+4));
            // Compact screens have exactly enough room for six 48-unit rows.
            // A normal one-line notice replaces only the item-details row so
            // live delivery counts stay visible. Longer errors use the card.
            bool detailRow=textHeight<=DetailsHeight;
            float available=detailRow?DetailsHeight:ProgressHeight-2;
            return new MergeCellRect(14,height-ProgressTop-ProgressHeight+(detailRow?0:1),width-28,available);
        }
        public MergeCellRect ToolbarAction(int index,float width)
        {
            if(index<0||index>=5)throw new ArgumentOutOfRangeException(nameof(index));
            if(!float.IsFinite(width)||width<292)throw new ArgumentOutOfRangeException(nameof(width));
            const float gap=4;
            if(TwoToolbarRows)
            {
                float production=(width-2*gap)/3,management=(width-gap)/3;
                return index<3?new MergeCellRect(index*(production+gap),52,production,48):index==3?new MergeCellRect(0,0,management,48):new MergeCellRect(management+gap,0,width-management-gap,48);
            }
            float delivery=Math.Max(80,width*.25f),menu=52,generate=(width-delivery-menu-4*gap)/3;
            return index<3?new MergeCellRect(index*(generate+gap),0,generate,48):index==3?new MergeCellRect(3*(generate+gap),0,menu,48):new MergeCellRect(width-delivery,0,delivery,48);
        }
        public static MergeScreenLayout For(float height,float feedbackHeight)
        {
            // Keep ordinary feedback from moving the board under the finger.
            // An idle strip is invisible, but its geometry remains reserved.
            var roomy=new MergeScreenLayout(true,Math.Max(46,feedbackHeight));
            return height>=700&&height-roomy.TrayTop-roomy.TrayBottom-roomy.GridInset*2>=308?roomy:new MergeScreenLayout(false,46);
        }
    }
}
