using System;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;

namespace Mofumachi.Tests
{
    public sealed class ReleaseUxTests
    {
        [TestCase("Page title",18,"ごほうびMerge",ReleaseFontRole.Accent)]
        [TestCase("Start label",18,"はじめる",ReleaseFontRole.Accent)]
        [TestCase("Coins value",13,"1,234",ReleaseFontRole.ReadingBold)]
        [TestCase("Requirement count bread",12,"0 / 1 あと1個",ReleaseFontRole.ReadingBold)]
        [TestCase("Today greeting",13,"今日は一緒におさんぽしよう♪",ReleaseFontRole.Reading)]
        [TestCase("Town menu label",11,"メニュー",ReleaseFontRole.Accent)]
        [TestCase("Town actor name",11,"モモ",ReleaseFontRole.Accent)]
        [TestCase("Speaker name",14,"ノア",ReleaseFontRole.Accent)]
        [TestCase("Nav label",12,"依頼",ReleaseFontRole.Accent)]
        [TestCase("Generate frame tea label",12,"お茶を作る",ReleaseFontRole.Accent)]
        [TestCase("Merge actions label",12,"生成枠整理",ReleaseFontRole.Accent)]
        [TestCase("Town region name",13,"中央の噴水広場",ReleaseFontRole.Accent)]
        [TestCase("Walking partner book label",13,"図鑑",ReleaseFontRole.Accent)]
        [TestCase("Unknown future label",18,"新しい画面",ReleaseFontRole.Unchanged)]
        public void SmallMenusAndNamesKeepHachiAndUnknownSurfacesStayUnchanged(string name,int size,string copy,ReleaseFontRole expected)
        {Assert.That(ReleaseFontPolicy.For(name,size,copy),Is.EqualTo(expected));}

        [Test] public void AButtonDoesNotLoseHachiBecauseItContainsDigitsOrMoreThanEighteenCharacters()
        {
            Assert.That(ReleaseFontPolicy.For("Start label",18,"長い説明文をボタン全体に表示する場合は読みやすさを優先する"),Is.EqualTo(ReleaseFontRole.Accent));
            Assert.That(ReleaseFontPolicy.For("Claim today flower label",14,"今日のお花を受け取りました♪\n集めたお花 12こ"),Is.EqualTo(ReleaseFontRole.Accent));
        }

        [TestCase("Coins value",13,"330", "UIFontReadingBold")]
        [TestCase("Requirement count bread",12,"0 / 1 あと1個", "UIFontReadingBold")]
        [TestCase("Town bubble",15,"今日はどこへ行こう？", "UIFontReading")]
        public void InformationTriesHachiAndFallsBackOnlyWhenItDoesNotFit(string name,int size,string copy,string fallback)
        {
            var role=ReleaseFontPolicy.For(name,size,copy);
            Assert.That(ReleaseFontPolicy.Resource(role,true),Is.EqualTo("UIFontAccent"));
            Assert.That(ReleaseFontPolicy.Resource(role,false),Is.EqualTo(fallback));
        }

        [Test] public void ShortMenusDoNotSilentlySwapToAnotherTypeface()
        {
            Assert.That(ReleaseFontPolicy.Resource(ReleaseFontRole.Accent,false),Is.EqualTo("UIFontAccent"));
            Assert.That(ReleaseFontPolicy.Resource(ReleaseFontRole.Unchanged,true),Is.Null);
        }

        [Test] public void FlowerReceiptUsesExistingDatesAndDoesNotMutateTheJournal()
        {
            var daily=new TownDailyService(new MemoryTownDailyStore(),new DateTime(2026,11,3));
            Assert.That(DailyFlowerPresentation.Receipt(daily.Data),Does.Contain("まだ受け取っていません"));
            for(int i=0;i<3;i++)daily.RecordMerge();daily.RecordTalk("momo");daily.RecordWalk();daily.Claim();
            byte[] before=TownDailyCodec.Encode(daily.Data);
            string receipt=DailyFlowerPresentation.Receipt(daily.Data);
            Assert.That(receipt,Does.Contain("受け取りました"));Assert.That(receipt,Does.Contain("2026/11/03"));
            Assert.That(receipt,Does.Contain("1こ"));Assert.That(receipt,Does.Contain("翌日も残ります"));
            Assert.That(TownDailyCodec.Encode(daily.Data),Is.EqualTo(before));
        }

        [Test] public void ClaimSurvivesReloadAndNextDayWithoutInventingAnInventoryItem()
        {
            var store=new MemoryTownDailyStore();var date=new DateTime(2026,11,3);
            var daily=new TownDailyService(store,date);for(int i=0;i<3;i++)daily.RecordMerge();daily.RecordTalk("lulu");daily.RecordWalk();
            Assert.That(daily.Claim(),Is.True);var reload=new TownDailyService(store,date.AddDays(1));
            Assert.That(reload.Data.Claimed,Is.False);Assert.That(reload.Data.DecorationDays.Single(),Is.EqualTo("2026-11-03"));
            Assert.That(DailyFlowerPresentation.Receipt(reload.Data),Does.Contain("2026/11/03"));
            Assert.That(DailyFlowerPresentation.Receipt(reload.Data),Does.Not.Contain("受け取りました"));
        }

        private sealed class SwitchableStore:ITownDailyStore
        {
            private readonly MemoryTownDailyStore inner=new MemoryTownDailyStore();public bool Fail;
            public TownDailyData Load()=>inner.Load();
            public void Save(TownDailyData data){if(Fail)throw new System.IO.IOException("Claim failure fixture");inner.Save(data);}
        }
        [Test] public void FailedClaimCannotDisplayASuccessfulReceipt()
        {
            var store=new SwitchableStore();var daily=new TownDailyService(store,new DateTime(2026,11,3));
            for(int i=0;i<3;i++)daily.RecordMerge();daily.RecordTalk("momo");daily.RecordWalk();store.Fail=true;
            Assert.That(daily.Claim(),Is.False);Assert.That(daily.LastError,Is.Not.Empty);
            Assert.That(DailyFlowerPresentation.Receipt(daily.Data),Does.Not.Contain("受け取りました"));
            Assert.That(daily.Data.DecorationDays,Is.Empty);
        }
    }
}
