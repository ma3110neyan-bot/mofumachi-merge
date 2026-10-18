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
            Assert.That(MusicCatalog.All.Select(t=>Resources.Load<AudioClip>("Mofumachi/Audio/"+t.Resource)).Distinct().Count(),Is.EqualTo(8));
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
        [UnityTest] public IEnumerator PopPromenadePreviewExpiresAndSceneListeningUsesTheSavedMergeChoice()
        {
            yield return EnterHome();var audio=AudioManager.Instance;var title=new MusicStore{Value="title-garden"};var town=new MusicStore();var merge=new MusicStore{Value="merge-ribbon"};
            audio.UseMusicRolePreferences(title,town,merge);var source=audio.GetComponents<AudioSource>().Single(a=>a.loop);
            var clip=Resources.Load<AudioClip>("Mofumachi/Audio/bgm-merge-pop-promenade");Assert.That(clip,Is.Not.Null);Assert.That(clip.length,Is.EqualTo(64).Within(.02f));
            Flow.ShowSettings();int writes=Store.Writes;Flow.PreviewMusic("merge-pop-promenade");Assert.That(source.clip,Is.SameAs(clip));Assert.That(merge.Value,Is.EqualTo("merge-ribbon"));
            yield return new WaitForSecondsRealtime(8.2f);Assert.That(audio.PreviewMusicId,Is.Null);Assert.That(source.clip,Is.SameAs(Resources.Load<AudioClip>("Mofumachi/Audio/bgm-town-loop")));
            Assert.That(Flow.SelectMusic("merge-pop-promenade"),Is.True);Assert.That(Store.Writes,Is.EqualTo(writes));
            Flow.ListenMusicInScene("merge-pop-promenade");yield return null;
            Assert.That(Flow.CurrentScreen,Is.EqualTo(ScreenId.Merge));Assert.That(audio.CurrentRole,Is.EqualTo(MusicRole.Merge));Assert.That(audio.SelectedMusicId,Is.EqualTo("merge-pop-promenade"));Assert.That(source.clip,Is.SameAs(clip));Assert.That(merge.Value,Is.EqualTo("merge-pop-promenade"));
            Flow.ShowHome();Assert.That(audio.SelectedMusicId,Is.EqualTo("town"));Assert.That(title.Value,Is.EqualTo("title-garden"));Assert.That(town.Value,Is.EqualTo("town"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FailedPreferenceSaveAndPauseRestorePreviousMusic()
        {
            yield return EnterHome();var preferences=new MusicStore();var audio=AudioManager.Instance;audio.UseMusicPreferences(preferences);Flow.ShowSettings();int writes=Store.Writes;
            Flow.PreviewMusic("merge-time");preferences.Fail=true;Assert.That(Flow.SelectMusic("merge-time"),Is.False);
            Assert.That(audio.SelectedMusicId,Is.EqualTo("town"));Assert.That(audio.PreviewMusicId,Is.Null);Assert.That(preferences.Value,Is.EqualTo("town"));Assert.That(Store.Writes,Is.EqualTo(writes));
            preferences.Fail=false;Flow.PreviewMusic("beginning");audio.SendMessage("OnApplicationPause",true);Assert.That(audio.PreviewMusicId,Is.Null);audio.SendMessage("OnApplicationPause",false);yield return null;
            Assert.That(audio.GetComponents<AudioSource>().Single(a=>a.loop).isPlaying,Is.True);LogAssert.NoUnexpectedReceived();
        }
        private static Rect Bounds(RectTransform root,RectTransform item)
        {var corners=new Vector3[4];item.GetWorldCorners(corners);var min=(Vector2)root.InverseTransformPoint(corners[0]);var max=(Vector2)root.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
        [UnityTest] public IEnumerator LastMusicCardAndOptionalPartnerStayVisibleAboveTheFixedFooterAtScrollBottom()
        {
            yield return EnterHome();
            foreach(bool hasPartner in new[]{true,false})
            {
                if(!hasPartner){Flow.ReturnToTitle();Flow.Game.State.favoriteCharacterId="";}
                Flow.ShowSettings();int writes=Store.Writes;string before=StateCodec.Encode(Flow.Game.State);
                var scroll=Flow.GetComponentsInChildren<ScrollRect>().Single();var page=scroll.viewport.parent as RectTransform;
                var card=Flow.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="Music track merge-pop-promenade");
                var partner=Flow.GetComponentsInChildren<Button>().SingleOrDefault(b=>b.name=="Reselect partner");Assert.That(partner!=null,Is.EqualTo(hasPartner));
                foreach(var size in new[]{new Vector2(320,592),new Vector2(360,808)})
                {
                    var canvas=Flow.GetComponentInChildren<Canvas>();canvas.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)canvas.transform).sizeDelta=size;
                    Flow.GetComponentInChildren<ResponsiveUILayout>().ApplyViewport(size,new Rect(0,24,size.x,size.y-56));Canvas.ForceUpdateCanvases();scroll.StopMovement();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
                    var cardInContent=Bounds(scroll.content,card);Assert.That(cardInContent.yMin,Is.GreaterThanOrEqualTo(scroll.content.rect.yMin),"The whole last card must belong to scrollable content");
                    foreach(var text in card.GetComponentsInChildren<Text>().Where(t=>t.text.Length>0))Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);
                    var controls=card.GetComponentsInChildren<Button>().ToList();Assert.That(controls.Count,Is.EqualTo(3));if(partner!=null)controls.Add(partner);
                    foreach(var button in controls)
                    {
                        var rect=Bounds(scroll.viewport,button.transform as RectTransform);var visible=scroll.viewport.rect;
                        Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(visible.xMin-.5f),button.name);Assert.That(rect.xMax,Is.LessThanOrEqualTo(visible.xMax+.5f),button.name);
                        Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(visible.yMin-.5f),button.name);Assert.That(rect.yMax,Is.LessThanOrEqualTo(visible.yMax+.5f),button.name);
                        Assert.That(rect.width,Is.GreaterThanOrEqualTo(48),button.name);Assert.That(rect.height,Is.GreaterThanOrEqualTo(48),button.name);
                        var text=button.GetComponentInChildren<Text>();Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),button.name);
                        foreach(var fixedControl in page.GetComponentsInChildren<RectTransform>().Where(t=>t.name=="Return settings"||t.name=="Return title"||t.name=="Settings feedback"||t.name=="QA build revision"))
                            Assert.That(Bounds(page,button.transform as RectTransform).Overlaps(Bounds(page,fixedControl)),Is.False,button.name+" overlaps "+fixedControl.name);
                    }
                    if(partner!=null)Assert.That(Bounds(page,card).Overlaps(Bounds(page,partner.transform as RectTransform)),Is.False,"Partner action must follow the last music card");
                }
                Assert.That(Store.Writes,Is.EqualTo(writes));Assert.That(StateCodec.Encode(Flow.Game.State),Is.EqualTo(before));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
