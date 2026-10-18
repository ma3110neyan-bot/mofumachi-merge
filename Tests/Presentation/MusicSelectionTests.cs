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
        [Test] public void LegacySongsAndFourCandidatesHaveDistinctReplaceableResources()
        {Assert.That(MusicCatalog.All.Count,Is.EqualTo(7));Assert.That(MusicCatalog.All.Select(t=>t.Resource).Distinct().Count(),Is.EqualTo(7));}
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
