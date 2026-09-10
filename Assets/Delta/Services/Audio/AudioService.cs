using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Delta.Services
{
    public class AudioService : IAudioService
    {
        private AudioSource _musicAudioSource;
        private AudioSource _sfxAudioSource;
        private Tween _musicVolumeTween;
        private bool _isMusicPaused;

        private Dictionary<string, AudioClip> _musicDict = new();
        private Dictionary<string, AudioClip> _sfxDict = new();

        private const string KEY_SFX_ENABLE = "KEY_SFX_ENABLE";
        private const string KEY_SFX_VOLUME = "KEY_SFX_VOLUME";
        private const string KEY_MUSIC_ENABLE = "KEY_MUSIC_ENABLE";
        private const string KEY_MUSIC_VOLUME = "KEY_MUSIC_VOLUME";
        private bool _isSfxEnable;
        private float _sfxVolume;
        private bool _isMusicEnable;
        private float _musicVolume;

        private AudioService() {}
        public AudioService(AudioConfigSO audioConfig)
        {
            Initialize();

            foreach (var audio in audioConfig.MusicData)
            {
                _musicDict.Add(audio.AudioId, audio.AudioClip);
                if (audio.AudioClip != null) audio.AudioClip.LoadAudioData();
            }

            foreach (var audio in audioConfig.SfxData)
            {
                _sfxDict.Add(audio.AudioId, audio.AudioClip);
                if (audio.AudioClip != null) audio.AudioClip.LoadAudioData();
            }
        }

        public void Initialize()
        {
            GameObject go = new("AudioService");
            Object.DontDestroyOnLoad(go);

            go.AddComponent<AudioListener>();

            _musicAudioSource = go.AddComponent<AudioSource>();
            _musicAudioSource.loop = true;

            _sfxAudioSource = go.AddComponent<AudioSource>();
            _sfxAudioSource.loop = false;

            _isMusicEnable = Load(KEY_MUSIC_ENABLE, true);
            _isSfxEnable = Load(KEY_SFX_ENABLE, true);
            _musicVolume = Load(KEY_MUSIC_VOLUME, 1f);
            _sfxVolume = Load(KEY_SFX_VOLUME, 1f);

            _musicAudioSource.volume = _isMusicEnable ? _musicVolume : 0f;
            _sfxAudioSource.volume = _isSfxEnable ? _sfxVolume : 0f;
        }

        public bool IsSfxEnable
        {
            get
            {
                return _isSfxEnable;
            }
            set
            {
                _isSfxEnable = value;
                _sfxAudioSource.volume = _isSfxEnable ? _sfxVolume : 0f;
                Save(KEY_SFX_ENABLE, _isSfxEnable);
            }
        }

        public float SfxVolume
        {
            get
            {
                return _sfxVolume;
            }
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                _sfxAudioSource.volume = _isSfxEnable ? _sfxVolume : 0f;
                Save(KEY_SFX_VOLUME, _sfxVolume);
            }
        }

        public bool IsMusicPaused => _isMusicPaused;

        public bool IsMusicEnable
        {
            get
            {
                return _isMusicEnable;
            }
            set
            {
                _isMusicEnable = value;
                _musicAudioSource.volume = _isMusicEnable ? _musicVolume : 0f;
                Save(KEY_MUSIC_ENABLE, _isMusicEnable);
            }
        }

        public float MusicVolume
        {
            get
            {
                return _musicVolume;
            }
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                _musicAudioSource.volume = _isMusicEnable ? _musicVolume : 0f;
                Save(KEY_MUSIC_VOLUME, _musicVolume);
            }
        }

        private void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Save(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        private bool Load(string key, bool defaultValue)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key) != 0;
            }
            return defaultValue;
        }

        private float Load(string key, float defaultValue)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetFloat(key);
            }
            return defaultValue;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (!IsSfxEnable || clip == null)
            {
                return;
            }

            _sfxAudioSource.PlayOneShot(clip, SfxVolume);
        }

        public void PlaySfx(string audioId)
        {
            if (!_sfxDict.TryGetValue(audioId, out var audioClip))
            {
                Debug.LogWarning($"Audio clip with ID '{audioId}' not found in SFX dictionary.");
                return;
            }

            PlaySfx(audioClip);
        }

        public void PlayBackgroundMusic(string audioId)
        {
            if (!_musicDict.TryGetValue(audioId, out var audioClip))
            {
                Debug.LogWarning($"Audio clip with ID '{audioId}' not found in music dictionary.");
                return;
            }

            _isMusicPaused = false;
            if (_musicVolumeTween != null && _musicVolumeTween.IsActive())
                _musicVolumeTween.Kill();
            _musicAudioSource.clip = audioClip;
            _musicAudioSource.Play();

            float targetVolume = _isMusicEnable ? _musicVolume : 0f;
            _musicAudioSource.volume = 0f;
            _musicVolumeTween = DOTween.To(() => _musicAudioSource.volume, x => _musicAudioSource.volume = x, targetVolume, 1f);
        }

        public void StopBackgroundMusic()
        {
            if (!_musicAudioSource.isPlaying && !_isMusicPaused) return;

            _isMusicPaused = false;
            if (_musicVolumeTween != null && _musicVolumeTween.IsActive())
                _musicVolumeTween.Kill();
            _musicVolumeTween = DOTween.To(() => _musicAudioSource.volume, x => _musicAudioSource.volume = x, 0f, 1f)
                .OnComplete(() => _musicAudioSource.Stop());
        }

        public void PauseBackgroundMusic()
        {
            if (!_musicAudioSource.isPlaying) return;

            _isMusicPaused = true;
            if (_musicVolumeTween != null && _musicVolumeTween.IsActive())
                _musicVolumeTween.Kill();
            _musicVolumeTween = DOTween.To(() => _musicAudioSource.volume, x => _musicAudioSource.volume = x, 0f, 0.5f)
                .OnComplete(() => _musicAudioSource.Pause());
        }

        public void ResumeBackgroundMusic()
        {
            if (!_isMusicPaused || _musicAudioSource.clip == null) return;

            _isMusicPaused = false;
            if (_musicVolumeTween != null && _musicVolumeTween.IsActive())
                _musicVolumeTween.Kill();
            _musicAudioSource.UnPause();
            float targetVolume = _isMusicEnable ? _musicVolume : 0f;
            _musicVolumeTween = DOTween.To(() => _musicAudioSource.volume, x => _musicAudioSource.volume = x, targetVolume, 0.5f);
        }
    }
}
