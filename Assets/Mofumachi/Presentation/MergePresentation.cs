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
        public static MergeCellRect ItemArt(int index)
        {
            float scale=MergePerspective.ArtScale(index);
            // Leave room for the 1.06 selection scale and 3-unit lift, including in a 48-unit cell.
            return new MergeCellRect(.5f-.46f*scale,.5f-.40f*scale,.92f*scale,.80f*scale);
        }
        public static string[] Text=>new[]{PlayHint,"今日の合成　0 / 3","あと3回、同じ種類・Lv.を合成しよう♪","合成クリア！ 街で仲間に話しかけよう♪","合成クリア！ 別の広場へおさんぽしよう♪","今日のお花を受け取りに、街へ戻ろう♪","今日のお花を集めたよ♪ 仲間に会いに行こう",SelectionName(null),SelectionHint(null),SelectionHint(new BoardItem("tea",3,0)),"今日のお願いへ","街で続きを見る","生成枠と整理"};
    }
    public readonly struct MergeScreenLayout
    {
        public float ProgressTop {get;} public float ProgressHeight {get;} public float TrayTop {get;} public float TrayBottom {get;}
        public float ToolbarBottom {get;} public float ToolbarHeight {get;} public float GridInset {get;} public float NavBottom {get;} public float NavHeight {get;}
        public bool TwoToolbarRows {get;}
        private MergeScreenLayout(bool roomy,float feedbackHeight)
        {
            TwoToolbarRows=roomy;ProgressTop=roomy?62:50;ProgressHeight=roomy?72:64;TrayTop=roomy?140:116;
            ToolbarBottom=roomy?72+feedbackHeight+6:52;ToolbarHeight=roomy?100:48;TrayBottom=ToolbarBottom+ToolbarHeight+4;
            GridInset=4;NavBottom=roomy?8:2;NavHeight=roomy?58:48;
        }
        public MergeCellRect ToolbarAction(int index,float width)
        {
            if(index<0||index>=5)throw new ArgumentOutOfRangeException(nameof(index));
            if(!float.IsFinite(width)||width<292)throw new ArgumentOutOfRangeException(nameof(width));
            const float gap=4;
            if(TwoToolbarRows)
            {
                float production=(width-2*gap)/3,action=(width-gap)/2;
                return index<3?new MergeCellRect(index*(production+gap),52,production,48):new MergeCellRect((index-3)*(action+gap),0,action,48);
            }
            float delivery=Math.Max(80,width*.25f),menu=52,generate=(width-delivery-menu-4*gap)/3;
            return index<3?new MergeCellRect(index*(generate+gap),0,generate,48):index==3?new MergeCellRect(3*(generate+gap),0,menu,48):new MergeCellRect(width-delivery,0,delivery,48);
        }
        public static MergeScreenLayout For(float height,float feedbackHeight)
        {
            var roomy=new MergeScreenLayout(true,Math.Max(46,feedbackHeight));
            return height>=700&&height-roomy.TrayTop-roomy.TrayBottom-roomy.GridInset*2>=308?roomy:new MergeScreenLayout(false,46);
        }
    }
}
