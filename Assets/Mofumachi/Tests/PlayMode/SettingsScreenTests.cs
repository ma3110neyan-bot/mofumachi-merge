using System.Collections;
using System.Linq;
using Mofumachi.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Mofumachi.Tests
{
    public sealed class SettingsScreenTests : SliceUiTestFixture
    {
        private Slider Slider(string name)=>Flow.GetComponentsInChildren<Slider>().Single(s=>s.name==name);
        [UnityTest] public IEnumerator SliderGesturePreviewsButCommitsOnlyOnce()
        {
            yield return EnterHome();Flow.ShowSettings();var s=Slider("BGM volume");var control=s.GetComponent<VolumeControl>();var e=new PointerEventData(EventSystem.current);int writes=Store.Writes;
            control.OnPointerDown(e);s.value=.21f;s.value=.37f;
            Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(Flow.Game.State.bgmVolume,Is.EqualTo(.75f));
            Assert.That(AudioManager.Instance.GetComponents<AudioSource>().Single(a=>a.loop).volume,Is.EqualTo(.37f));
            control.OnPointerUp(e);control.OnEndDrag(e);Assert.That(Store.Writes,Is.EqualTo(writes+1));Assert.That(Store.Load().State.bgmVolume,Is.EqualTo(.37f));
        }
        [UnityTest] public IEnumerator FailedOrInterruptedVolumeEditRestoresSavedAudioAndUi()
        {
            yield return EnterHome();Flow.ShowSettings();var s=Slider("BGM volume");var e=new PointerEventData(EventSystem.current);var control=s.GetComponent<VolumeControl>();int before=Store.Writes;
            control.OnPointerDown(e);s.value=.1f;Flow.SendMessage("OnApplicationPause",true);yield return null;
            Assert.That(Store.Load().State.bgmVolume,Is.EqualTo(.75f));Assert.That(Slider("BGM volume").value,Is.EqualTo(.75f));
            Store.Fail=true;s=Slider("BGM volume");control=s.GetComponent<VolumeControl>();control.OnPointerDown(e);s.value=.2f;control.OnPointerUp(e);
            Assert.That(Flow.Game.State.bgmVolume,Is.EqualTo(.75f));Assert.That(Slider("BGM volume").value,Is.EqualTo(.75f));
            Assert.That(Flow.FeedbackFor(ScreenId.Settings),Is.Not.Empty);Assert.That(AudioManager.Instance.GetComponents<AudioSource>().Single(a=>a.loop).volume,Is.EqualTo(.75f));
        }
        [UnityTest] public IEnumerator SettingsReturnsToEveryOrigin()
        {
            yield return LoadTitle();Flow.ShowSettings();Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Title));
            Flow.BeginGame();Flow.ConfirmPurchaseNotice();yield return null;yield return null;
            foreach(var target in new[]{ScreenId.Home,ScreenId.Quest,ScreenId.Merge}){if(target==ScreenId.Home)Flow.ShowHome();else if(target==ScreenId.Quest)Flow.ShowQuest();else Flow.ShowMerge();Flow.ShowSettings();Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(target));}
            Flow.AcceptQuest();Flow.DropItem(0,1);yield return new WaitForSecondsRealtime(.3f);Flow.Deliver();Flow.ShowSettings();Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Result));
            yield return new WaitForSecondsRealtime(1.3f);Flow.ShowGrowth();Flow.ShowSettings();Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Growth));
        }
        [UnityTest] public IEnumerator VolumeZeroAndOffRemainDistinctAndSettingsReturnToOrigin()
        {
            yield return EnterHome();Flow.ShowQuest();Flow.ShowSettings();Assert.That(Flow.CommitAudioPreferences(false,true,0,.43f),Is.True);
            Assert.That(Flow.CommitAudioPreferences(true,true,0,.43f),Is.True);Flow.GoBack();Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Quest));
            Assert.That(Store.Load().State.bgmEnabled,Is.True);Assert.That(Store.Load().State.bgmVolume,Is.Zero);
            Flow.ShowMerge();Flow.Deliver();Flow.ShowSettings();Assert.That(Flow.FeedbackFor(ScreenId.Settings),Is.Empty);
            foreach(var text in Flow.GetComponentsInChildren<Text>())Assert.That(text.text,Does.Not.Contain("納品条件"));
        }
    }
}
