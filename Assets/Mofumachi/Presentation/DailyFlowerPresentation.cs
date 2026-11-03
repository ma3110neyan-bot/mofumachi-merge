using System;
using System.Linq;
namespace Mofumachi.Presentation
{
    public static class DailyFlowerPresentation
    {
        // Existing journal dates are the received flowers, not inventory items.
        public static string Receipt(TownDailyData data)
        {
            var dates=data.DecorationDays;
            if(dates.Count==0)return "まだ受け取っていません\n今日のお願いを達成して受け取ろう♪\n受取記録は翌日も残ります";
            string latest=dates.OrderBy(d=>d,StringComparer.Ordinal).Last().Replace('-','/');
            return (data.Claimed?"今日のお花を受け取りました♪":"今日のお花の記録")+"\n集めたお花 "+dates.Count+"こ · 最新 "+latest+"\n記録は翌日も残ります";
        }
    }
}
