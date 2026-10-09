using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Mofumachi.Tests
{
    public sealed class AudioManagerTests : SliceUiTestFixture
    {
        [UnityTest] public IEnumerator SceneAndSettingsChangesKeepOneAudioManagerAndTrackPosition()
        {
            yield return EnterHome(); var audio=AudioManager.Instance;
            var sources=audio.GetComponents<AudioSource>(); Assert.That(sources.Length,Is.EqualTo(2));
            var music=sources.Single(s=>s.loop);var clip=music.clip;
            Assert.That(clip,Is.Not.Null);yield return new WaitForSecondsRealtime(.3f);float position=music.time;
            Flow.ShowSettings();Flow.GoBack();yield return null;
            yield return SceneManager.LoadSceneAsync("TitleScene");yield return null;
            Assert.That(AudioManager.GetOrCreate(),Is.SameAs(audio));Assert.That(music.clip,Is.SameAs(clip));
            Assert.That(music.time,Is.GreaterThanOrEqualTo(position));
            Assert.That(Object.FindObjectsByType<AudioManager>(FindObjectsInactive.Include).Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator AudioChannelsMuteAndResumeIndependently()
        {
            yield return LoadTitle();var audio=AudioManager.Instance;var music=audio.GetComponents<AudioSource>().Single(s=>s.loop);var effects=audio.GetComponents<AudioSource>().Single(s=>!s.loop);
            audio.ApplySettings(true,false,0,1);Assert.That(music.volume,Is.EqualTo(0));audio.PlaySe(AudioCue.Merge);Assert.That(effects.isPlaying,Is.False);
            audio.ApplySettings(false,true,1,.4f);Assert.That(music.isPlaying,Is.False);audio.PlaySe(AudioCue.Growth);Assert.That(effects.isPlaying,Is.True);Assert.That(effects.volume,Is.EqualTo(.4f));
            audio.ApplySettings(true,true,.6f,.8f);yield return new WaitForSecondsRealtime(.1f);float time=music.time;
            audio.SendMessage("OnApplicationPause",true);Assert.That(music.isPlaying,Is.False);
            audio.SendMessage("OnApplicationPause",false);yield return null;Assert.That(music.isPlaying,Is.True);Assert.That(music.time,Is.GreaterThanOrEqualTo(time));
        }
    }
}
