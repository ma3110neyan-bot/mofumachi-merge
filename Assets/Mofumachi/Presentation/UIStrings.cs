using System.Collections.Generic;
namespace Mofumachi.Presentation
{
    public static class UIStrings
    {
        public const string NoticeTitle="課金について";
        public const string NoticeBody="未成年の方は、課金する前に必ず保護者の方に相談し、許可をもらってください。";
        public const string NoticeConfirm="確認しました";
        public const string PopLabels="マージ モモ ルル ポム フィオ ミエル ノア";
        public static IReadOnlyList<string> All { get; } = new[]{NoticeTitle,NoticeBody,NoticeConfirm,"もふまちメルジュ","もふもふの街で、ちいさなお茶会を。","つくって、届けて、街を育てよう。","はじめる","もふまち","キャラ依頼","お茶会の準備","お茶 Lv.2 × 1","報酬 30 Coins","街 Lv.","設定","戻る","依頼を受ける","依頼完了","合成してお茶会の準備をしよう","納品する","街の仲間にお茶を届けよう","同じお茶を重ねて合成","お茶を作る","納品完了！","街が成長しました","街へ戻る","BGM","SE","音量","ON","OFF","街の成長を見る"};
    }
}
