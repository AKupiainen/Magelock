using System.Collections.Generic;
using UnityEngine;

namespace MageLock.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private int poolSize = 10;

        private Queue<AudioSource> _sfxPool;
        private List<AudioSource> _activeSfx;
        private GameObject _sfxContainer;

        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

        public void InitializeSfxPool()
        {
            _sfxPool = new Queue<AudioSource>();
            _activeSfx = new List<AudioSource>();

            _sfxContainer = new GameObject("SFX Container");
            _sfxContainer.transform.SetParent(transform);

            for (int i = 0; i < poolSize; i++)
            {
                AudioSource source = CreateAudioSource();
                _sfxPool.Enqueue(source);
            }
        }

        private AudioSource CreateAudioSource()
        {
            GameObject sfxObject = new("SFX Source");
            sfxObject.transform.SetParent(_sfxContainer.transform);
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            SfxAutoReturn autoReturn = sfxObject.AddComponent<SfxAutoReturn>();
            autoReturn.Initialize(this);

            return source;
        }

        public void PlaySfx(Sfx sfx)
        {
            if (sfx == null || sfx.Clip == null)
            {
                return;
            }

            AudioSource source = _sfxPool.Count > 0 ? _sfxPool.Dequeue() : CreateAudioSource();
            source.clip = sfx.Clip;
            source.volume = sfx.Volume * sfxVolume * masterVolume;
            source.pitch = sfx.Pitch;
            source.loop = sfx.Loop;
            source.Play();
            _activeSfx.Add(source);

            if (!sfx.Loop && source.TryGetComponent(out SfxAutoReturn autoReturn))
            {
                autoReturn.SetReturnTime(sfx.Clip.length);
            }
        }

        public void PlayBGM(BGM bgm)
        {
            if (bgm == null || bgm.Clip == null)
            {
                return;
            }

            bgmSource.clip = bgm.Clip;
            bgmSource.volume = bgm.Volume * bgmVolume * masterVolume;
            bgmSource.loop = bgm.Loop;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            bgmSource.Stop();
        }

        public void ReturnToPool(AudioSource source)
        {
            source.Stop();
            _activeSfx.Remove(source);
            _sfxPool.Enqueue(source);
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            bgmSource.volume = bgmVolume * masterVolume;
        }

        private void UpdateVolumes()
        {
            foreach (AudioSource source in _activeSfx)
            {
                source.volume = sfxVolume * masterVolume;
            }

            bgmSource.volume = bgmVolume * masterVolume;
        }
        
        public float GetBGMVolume()
        {
            return bgmSource != null ? bgmSource.volume : 0f;
        }

        public float GetSfxVolume()
        {
            return sfxVolume;
        }
    }
}