using System.Collections;
using UnityEngine;

namespace HayuNgaksara
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource audioSourceMusic;
        [SerializeField] private AudioSource audioSourceSFX;
        [SerializeField] private AudioSource audioSourcePelafalan;

        private const string KeyMusicVol = "MusicVolume";
        private const string KeySFXVol   = "SFXVolume";
        private const float  DefaultVol  = 0.8f;

        private Coroutine _musicFadeCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            float musicVol = PlayerPrefs.GetFloat(KeyMusicVol, DefaultVol);
            float sfxVol   = PlayerPrefs.GetFloat(KeySFXVol, DefaultVol);
            // Null-guard: bila AudioSource belum di-assign di Inspector, jangan sampai melempar
            // exception (UnassignedReferenceException) yang bisa memutus alur.
            if (audioSourceMusic != null)     { audioSourceMusic.volume = musicVol; audioSourceMusic.loop = true; }
            if (audioSourceSFX != null)       audioSourceSFX.volume       = sfxVol;
            if (audioSourcePelafalan != null) audioSourcePelafalan.volume = sfxVol;
        }

        public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
        {
            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeInMusic(clip, fadeDuration));
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeOutMusic(fadeDuration));
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            audioSourceSFX.PlayOneShot(clip);
        }

        public void PlayPelafalan(AudioClip clip)
        {
            if (clip == null) return;
            audioSourcePelafalan.Stop();
            audioSourcePelafalan.clip = clip;
            audioSourcePelafalan.Play();
        }

        public void SetMusicVolume(float vol)
        {
            vol = Mathf.Clamp01(vol);
            if (audioSourceMusic != null) audioSourceMusic.volume = vol;
            PlayerPrefs.SetFloat(KeyMusicVol, vol);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float vol)
        {
            vol = Mathf.Clamp01(vol);
            if (audioSourceSFX != null)       audioSourceSFX.volume       = vol;
            if (audioSourcePelafalan != null) audioSourcePelafalan.volume = vol;
            PlayerPrefs.SetFloat(KeySFXVol, vol);
            PlayerPrefs.Save();
        }

        private IEnumerator FadeInMusic(AudioClip clip, float duration)
        {
            float targetVol = audioSourceMusic.volume;
            yield return FadeOutMusic(duration * 0.5f);

            audioSourceMusic.clip = clip;
            audioSourceMusic.Play();

            float t = 0f;
            while (t < duration * 0.5f)
            {
                t += Time.deltaTime;
                audioSourceMusic.volume = Mathf.Lerp(0f, targetVol, t / (duration * 0.5f));
                yield return null;
            }
            audioSourceMusic.volume = targetVol;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            float startVol = audioSourceMusic.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                audioSourceMusic.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            audioSourceMusic.Stop();
            audioSourceMusic.volume = startVol;
        }
    }
}
