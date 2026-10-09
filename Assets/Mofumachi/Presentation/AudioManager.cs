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
        public static AudioManager GetOrCreate()
        { if(Instance!=null)return Instance; return new GameObject("Mofumachi Audio").AddComponent<AudioManager>(); }
        private void Awake()
        {
            if(Instance!=null && Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;music.spatialBlend=0;
            effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;
            if(FindAnyObjectByType<AudioListener>()==null)gameObject.AddComponent<AudioListener>();
            music.clip=Resources.Load<AudioClip>("Mofumachi/Audio/bgm-town-loop");
            string[] keys={"se-confirm","se-merge","se-reward","se-character","se-delivery","se-growth"};
            cues=new AudioClip[keys.Length];for(int i=0;i<keys.Length;i++)cues[i]=Resources.Load<AudioClip>("Mofumachi/Audio/"+keys[i]);
            if(music.clip==null || Array.Exists(cues,c=>c==null))throw new InvalidOperationException("Mofumachi recorded audio resources are missing.");
        }
        public void ApplySettings(bool bgm,bool se,float bgmVolume=.75f,float seVolume=.65f)
        {
            bgmEnabled=bgm;seEnabled=se;music.volume=Mathf.Clamp01(bgmVolume);effects.volume=Mathf.Clamp01(seVolume);
            if(!bgm || paused)music.Pause();else if(!started){music.Play();started=true;}else music.UnPause();
            if(!se){effects.Stop();effectUntil=0;}
        }
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
        { paused=value;if(value){music.Pause();effects.Pause();}else{if(bgmEnabled)music.UnPause();if(seEnabled)effects.UnPause();} }
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
