using System;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
namespace Mofumachi.Tests
{
    public sealed class MusicSelectionTests
    {
        private sealed class Store:IMusicPreferenceStore
        {public string Value;public int Writes;public bool Fail;public string Load()=>Value;public void Save(string id){if(Fail)throw new InvalidOperationException("disk");Value=id;Writes++;}}
        [Test] public void LegacySongsAndFiveCandidatesHaveDistinctReplaceableResources()
        {Assert.That(MusicCatalog.All.Count,Is.EqualTo(8));Assert.That(MusicCatalog.All.Select(t=>t.Resource).Distinct().Count(),Is.EqualTo(8));}
        [TestCase("beginning","bgm-beginning-loop")] [TestCase("town","bgm-town-loop")]
        [TestCase("merge-time","bgm-merge-loop")] [TestCase("title-letter","bgm-title-letter")]
        [TestCase("title-garden","bgm-title-garden")] [TestCase("merge-picnic","bgm-merge-picnic")]
        [TestCase("merge-ribbon","bgm-merge-ribbon")]
        public void ExistingPreferenceIdsStillResolveToTheirOriginalResources(string id,string resource)
        {Assert.That(MusicCatalog.Find(id)?.Resource,Is.EqualTo(resource));var store=new Store{Value=id};Assert.That(new MusicSelection(store).SelectedId,Is.EqualTo(id));Assert.That(store.Writes,Is.Zero);}
        [Test] public void PopPromenadeCanBePreviewedThenPersistedThroughTheExistingSelectionApi()
        {
            var store=new Store{Value="merge-ribbon"};var music=new MusicSelection(store);
            Assert.That(music.Preview("merge-pop-promenade"),Is.True);Assert.That(music.ActiveId,Is.EqualTo("merge-pop-promenade"));Assert.That(store.Writes,Is.Zero);
            music.CancelPreview();Assert.That(music.ActiveId,Is.EqualTo("merge-ribbon"));
            Assert.That(music.Select("merge-pop-promenade"),Is.True);Assert.That(store.Writes,Is.EqualTo(1));Assert.That(new MusicSelection(store).SelectedId,Is.EqualTo("merge-pop-promenade"));
        }
        [Test] public void OldInstallAndUnknownTrackKeepTheExistingTownMusicWithoutWriting()
        {var store=new Store{Value="future"};var music=new MusicSelection(store);Assert.That(music.SelectedId,Is.EqualTo("town"));Assert.That(store.Writes,Is.Zero);}
        [Test] public void PreviewIsTemporaryAndNeverPersistsASelection()
        {var store=new Store();var music=new MusicSelection(store);Assert.That(music.Preview("beginning"),Is.True);Assert.That(music.ActiveId,Is.EqualTo("beginning"));Assert.That(music.SelectedId,Is.EqualTo("town"));music.CancelPreview();Assert.That(music.ActiveId,Is.EqualTo("town"));Assert.That(store.Writes,Is.Zero);}
        [Test] public void SelectionSurvivesARecreatedSessionAndRepeatedSelectionDoesNotWrite()
        {var store=new Store();var music=new MusicSelection(store);Assert.That(music.Select("merge-time"),Is.True);Assert.That(music.Select("merge-time"),Is.True);Assert.That(store.Writes,Is.EqualTo(1));Assert.That(new MusicSelection(store).SelectedId,Is.EqualTo("merge-time"));}
        [Test] public void FailedChoiceRestoresThePreviousSavedSongAndStopsPreview()
        {var store=new Store{Value="beginning"};var music=new MusicSelection(store);music.Preview("merge-time");store.Fail=true;Assert.That(music.Select("merge-time"),Is.False);Assert.That(music.ActiveId,Is.EqualTo("beginning"));Assert.That(music.IsPreviewing,Is.False);Assert.That(store.Value,Is.EqualTo("beginning"));Assert.That(music.Error,Is.Not.Empty);}
        [Test] public void InvalidTrackCannotBePreviewedOrSaved()
        {var store=new Store();var music=new MusicSelection(store);Assert.That(music.Preview(null),Is.False);Assert.That(music.Select("bad"),Is.False);Assert.That(store.Writes,Is.Zero);}
    }
}
