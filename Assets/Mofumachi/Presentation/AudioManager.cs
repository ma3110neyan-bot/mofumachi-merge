using UnityEngine;

namespace Mofumachi.Presentation
{
    public enum AudioCue { Confirm, Merge, Reward, Character }
    public sealed class AudioManager : MonoBehaviour
    {
        // Provisional acoustic-style synthesis; replace clips with formal recordings.
        private AudioSource music, effects;
        private AudioClip[] cues;
        private bool seEnabled;
        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = .15f;
            effects = gameObject.AddComponent<AudioSource>(); effects.volume = .24f;
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            music.clip = Compose("Provisional piano and bells", 8, new[] { 60, 64, 67, 72, 69, 67, 64, 67, 65, 69, 72, 76, 74, 72, 69, 67 }, .5f);
            cues = new[] { Compose("Soft pon", .18f, new[] { 67 }, .18f), Compose("Sparkle and air", .48f, new[] { 76, 83, 88 }, .12f), Compose("Reward chime", .65f, new[] { 72, 76, 79, 84 }, .13f), Compose("Small reaction", .12f, new[] { 72 }, .12f) };
        }
        public void ApplySettings(bool bgm, bool se)
        {
            seEnabled = se;
            if (bgm && !music.isPlaying) music.Play();
            if (!bgm) music.Stop();
            if (!se) effects.Stop();
        }
        public void PlaySe(AudioCue cue) { if (seEnabled) effects.PlayOneShot(cues[(int)cue]); }
        private static AudioClip Compose(string name, float duration, int[] notes, float spacing)
        {
            const int rate = 24000;
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.RoundToInt(n * spacing * rate);
                float frequency = 440 * Mathf.Pow(2, (notes[n] - 69) / 12f);
                for (int i = start; i < samples.Length; i++)
                {
                    float t = (i - start) / (float)rate;
                    float envelope = Mathf.Min(1, t / .008f) * Mathf.Exp(-t * 6);
                    float wave = Mathf.Sin(2 * Mathf.PI * frequency * t) + .3f * Mathf.Sin(2 * Mathf.PI * frequency * 2 * t) + .12f * Mathf.Sin(2 * Mathf.PI * frequency * 3.01f * t) * Mathf.Exp(-t * 10);
                    samples[i] += .32f * envelope * wave;
                }
            }
            // Avoid clicks at loop boundaries and keep headroom when tones overlap.
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(samples[i] * Mathf.Min(1, (samples.Length - 1 - i) / (rate * .02f)), -.8f, .8f);
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy()
        {
            if (music != null && music.clip != null) Destroy(music.clip);
            if (cues != null) foreach (var clip in cues) Destroy(clip);
        }
    }
}
