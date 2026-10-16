namespace Mofumachi.Presentation
{
    public static class TownCharacterDialogue
    {
        private static readonly string[][] Lines={
            new[]{"今日も会えたね。いっしょに、ちいさな発見を探そう♪","北のカフェで、ふわふわのパンを半分こしよう。","あなたと歩く道は、いつもよりきらきらして見えるね。"},
            new[]{"ねえ、噴水の光を見つけたよ！いっしょに見よう♪","お茶の香りがする方へ、ちょっとおさんぽしない？","また会えた！今日はどんな発見が待っているかな。"},
            new[]{"のんびり、のんびり。ベンチでお茶にしよう。","焼きたてのパンって、待つ時間までおいしいねえ。","今日もよく歩いたね。ほっとひと息つこうか。"},
            new[]{"南の工房で、かわいい飾りを考えているんだ！","あの花の色、リボンにしたらきっとすてきだよ！","あなたと作った街、どんどん楽しくなるね！"},
            new[]{"今日のお花、とっても元気。あなたに会えてうれしいのね。","花の小道を、ゆっくり歩いてみませんか？","いつも会いに来てくれて、ありがとう。小さなお花をどうぞ♪"},
            new[]{"今日はどんなお気に入りを見つけようかな。","みんなの笑顔も、わたしの大切なコレクション！","またひとつ、あなたとの思い出が増えたね♪"}};
        public static string For(string id,int meetings)
        {var c=Mofumachi.Core.CharacterCatalog.Find(id);return c==null?"":Lines[c.PortraitIndex][meetings>=6&&(meetings-6)%3==0?2:System.Math.Max(0,meetings-1)%2];}
        public static System.Collections.Generic.IEnumerable<string> Text=>System.Linq.Enumerable.SelectMany(Lines,x=>x);
    }
}
