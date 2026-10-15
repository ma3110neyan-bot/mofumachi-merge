using System;
using UnityEngine;
namespace Mofumachi.Presentation
{
    public enum AudioCue { Confirm, Merge, Reward, Character, Delivery, Growth }
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        private AudioSource music,effects;
        private AudioClip[] cues;
        private bool seEnabled,bgmEnabled,paused,started;
        private float effectUntil;
        private AudioCue playingCue;
        private MusicSelection selection;private AudioClip[] tracks;private float previewUntil,resumeTime;
        public string SelectedMusicId=>selection.SelectedId;
        public string PreviewMusicId=>selection.IsPreviewing?selection.ActiveId:null;
        public string MusicError=>selection.Error;
        public static AudioManager GetOrCreate()
        { if(Instance!=null)return Instance; return new GameObject("Mofumachi Audio").AddComponent<AudioManager>(); }
        private void Awake()
        {
            if(Instance!=null && Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;music.spatialBlend=0;
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;
            if(FindAnyObjectByType<AudioListener>()==null)gameObject.AddComponent<AudioListener>();
            tracks=new AudioClip[MusicCatalog.All.Count];for(int i=0;i<tracks.Length;i++)tracks[i]=Resources.Load<AudioClip>("Mofumachi/Audio/"+MusicCatalog.All[i].Resource);
            selection=new MusicSelection(new MusicPreferenceStore());music.clip=TrackClip(selection.SelectedId);
            string[] keys={"se-confirm","se-merge","se-reward","se-character","se-delivery","se-growth"};
            cues=new AudioClip[keys.Length];for(int i=0;i<keys.Length;i++)cues[i]=Resources.Load<AudioClip>("Mofumachi/Audio/"+keys[i]);
            if(Array.Exists(tracks,c=>c==null) || Array.Exists(cues,c=>c==null))throw new InvalidOperationException("Mofumachi recorded audio resources are missing.");
        }
        public void ApplySettings(bool bgm,bool se,float bgmVolume=.75f,float seVolume=.65f)
        {
            CancelMusicPreview();
            bgmEnabled=bgm;seEnabled=se;music.volume=Mathf.Clamp01(bgmVolume);effects.volume=Mathf.Clamp01(seVolume);
            if(!bgm || paused)music.Pause();else if(!started){music.Play();started=true;}else music.UnPause();
            if(!se){effects.Stop();effectUntil=0;}
        }
        private AudioClip TrackClip(string id)
        {for(int i=0;i<MusicCatalog.All.Count;i++)if(MusicCatalog.All[i].Id==id)return tracks[i];return tracks[1];}
        private void ChangeTrack(string id,float position)
        {
            music.Stop();music.clip=TrackClip(id);started=false;
            if(music.clip!=null)music.time=Mathf.Clamp(position,0,Mathf.Max(0,music.clip.length-.05f));
            if(!paused&&(bgmEnabled||selection.IsPreviewing)){music.Play();started=true;}
        }
        public void UseMusicPreferences(IMusicPreferenceStore store)
        {CancelMusicPreview();selection=new MusicSelection(store);ChangeTrack(selection.SelectedId,0);}
        public bool PreviewMusic(string id)
        {
            if(MusicCatalog.Find(id)==null||paused)return false;
            if(selection.IsPreviewing&&selection.ActiveId==id){CancelMusicPreview();return true;}
            if(!selection.IsPreviewing)resumeTime=music.time;
            selection.Preview(id);previewUntil=Time.unscaledTime+8;ChangeTrack(id,0);return true;
        }
        public bool SelectMusic(string id)
        {
            if(MusicCatalog.Find(id)==null)return false;
            string previous=selection.SelectedId;bool previewed=selection.IsPreviewing;bool saved=selection.Select(id);
            if(saved&&previous!=selection.SelectedId)ChangeTrack(selection.SelectedId,0);
            else if(previewed)ChangeTrack(selection.SelectedId,resumeTime);
            return saved;
        }
        public void CancelMusicPreview()
        {if(selection==null||!selection.IsPreviewing)return;selection.CancelPreview();ChangeTrack(selection.SelectedId,resumeTime);}
        private void Update(){if(selection.IsPreviewing&&Time.unscaledTime>=previewUntil)CancelMusicPreview();}
        public void PlaySe(AudioCue cue)
        {
            if(!seEnabled || paused || effects.volume<=0)return;
            // One bounded effects voice prevents rapid taps from summing into clipping.
            // Tap sounds cannot interrupt a success cue; a newer success event can replace it.
            bool tap=cue==AudioCue.Confirm || cue==AudioCue.Character;
            if(tap && Time.unscaledTime<effectUntil)return;
            playingCue=cue;effects.clip=cues[(int)cue];effects.Play();effectUntil=Time.unscaledTime+(tap?.10f:effects.clip.length);
        }
        private void OnApplicationPause(bool value)
        { paused=value;if(value){CancelMusicPreview();music.Pause();effects.Pause();}else{if(bgmEnabled){if(!started){music.Play();started=true;}else music.UnPause();}if(seEnabled)effects.UnPause();} }
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
