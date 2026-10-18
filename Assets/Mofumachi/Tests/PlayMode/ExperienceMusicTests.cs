using System;
using System.Collections;
using System.Linq;
using Mofumachi.Core;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class ExperienceMusicTests : SliceUiTestFixture
    {
        private sealed class MusicStore:IMusicPreferenceStore
        {public string Value="town";public bool Fail;public string Load()=>Value;public void Save(string id){if(Fail)throw new InvalidOperationException("Test preference failure");Value=id;}}
        [UnityTest] public IEnumerator LegacyAndCandidateClipsCanBeSelectedWithoutWritingTheGameSave()
        {
            yield return EnterHome();var preferences=new MusicStore();var audio=AudioManager.Instance;audio.UseMusicPreferences(preferences);Flow.ShowSettings();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
            var music=audio.GetComponents<AudioSource>().Single(a=>a.loop);var original=music.clip;
            Assert.That(MusicCatalog.All.Select(t=>Resources.Load<AudioClip>("Mofumachi/Audio/"+t.Resource)).Distinct().Count(),Is.EqualTo(7));
            foreach(var track in MusicCatalog.Candidates)Assert.That(Flow.GetComponentsInChildren<Button>().Any(b=>b.name=="Preview music "+track.Id),Is.True);
            Assert.That(Flow.SelectMusic("beginning"),Is.True);Assert.That(music.clip,Is.Not.SameAs(original));Assert.That(preferences.Value,Is.EqualTo("beginning"));
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ExplicitPreviewDoesNotTurnOnSavedBgmAndBackRestoresTheSavedSong()
        {
            yield return EnterHome();var audio=AudioManager.Instance;audio.UseMusicPreferences(new MusicStore());Flow.ShowSettings();Flow.CommitAudioPreferences(false,true,.5f,.5f);
            Flow.PreviewMusic("merge-time");Assert.That(audio.PreviewMusicId,Is.EqualTo("merge-time"));Assert.That(Flow.Game.State.bgmEnabled,Is.False);
            Flow.GoBack();Assert.That(audio.PreviewMusicId,Is.Null);Assert.That(audio.SelectedMusicId,Is.EqualTo("town"));Assert.That(audio.GetComponents<AudioSource>().Single(a=>a.loop).isPlaying,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FailedPreferenceSaveAndPauseRestorePreviousMusic()
        {
            yield return EnterHome();var preferences=new MusicStore();var audio=AudioManager.Instance;audio.UseMusicPreferences(preferences);Flow.ShowSettings();int writes=Store.Writes;
            Flow.PreviewMusic("merge-time");preferences.Fail=true;Assert.That(Flow.SelectMusic("merge-time"),Is.False);
            Assert.That(audio.SelectedMusicId,Is.EqualTo("town"));Assert.That(audio.PreviewMusicId,Is.Null);Assert.That(preferences.Value,Is.EqualTo("town"));Assert.That(Store.Writes,Is.EqualTo(writes));
            preferences.Fail=false;Flow.PreviewMusic("beginning");audio.SendMessage("OnApplicationPause",true);Assert.That(audio.PreviewMusicId,Is.Null);audio.SendMessage("OnApplicationPause",false);yield return null;
            Assert.That(audio.GetComponents<AudioSource>().Single(a=>a.loop).isPlaying,Is.True);LogAssert.NoUnexpectedReceived();
        }
    }
}
