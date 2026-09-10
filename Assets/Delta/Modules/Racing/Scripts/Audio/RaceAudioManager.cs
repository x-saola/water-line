using Delta.Core;
using Delta.Services;
using UnityEngine;

namespace Delta.Modules.Racing
{
    /// <summary>
    /// Manages audio playback for the racing system
    /// </summary>
    public class RaceAudioManager : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip _raceStartSound;
        [SerializeField] private AudioClip _objectiveCompleteSound;
        [SerializeField] private AudioClip _opponentProgressSound;
        [SerializeField] private AudioClip _victorySound;
        [SerializeField] private AudioClip _defeatSound;
        [SerializeField] private AudioClip _tierUpSound;
        [SerializeField] private AudioClip _tierDownSound;
        [SerializeField] private AudioClip _countdownTick;
        [SerializeField] private AudioClip _uiClickSound;

        [Header("Settings")]
        [SerializeField] private float _sfxVolume = 1f;

        private AudioService _audioService;
        private AudioSource _localAudioSource;

        private void Awake()
        {
            _audioService = ServiceLocator.Get<AudioService>();
            _localAudioSource = gameObject.AddComponent<AudioSource>();
            _localAudioSource.playOnAwake = false;
            _localAudioSource.volume = _sfxVolume;
        }

        public void PlayRaceStart()
        {
            PlaySound(_raceStartSound, "race_start");
        }

        public void PlayObjectiveComplete()
        {
            PlaySound(_objectiveCompleteSound, "objective_complete");
        }

        public void PlayOpponentProgress()
        {
            PlaySound(_opponentProgressSound, "opponent_progress");
        }

        public void PlayVictory()
        {
            PlaySound(_victorySound, "victory");
        }

        public void PlayDefeat()
        {
            PlaySound(_defeatSound, "defeat");
        }

        public void PlayTierUp()
        {
            PlaySound(_tierUpSound, "tier_up");
        }

        public void PlayTierDown()
        {
            PlaySound(_tierDownSound, "tier_down");
        }

        public void PlayCountdownTick()
        {
            PlaySound(_countdownTick, "countdown_tick");
        }

        public void PlayUIClick()
        {
            PlaySound(_uiClickSound, "ui_click");
        }

        private void PlaySound(AudioClip clip, string fallbackName)
        {
            if (clip != null)
            {
                _localAudioSource.PlayOneShot(clip);
            }
            else if (_audioService != null)
            {
                // Fallback to AudioService with string name
                _audioService.PlaySfx(fallbackName);
            }
        }

        public void SetVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            if (_localAudioSource != null)
            {
                _localAudioSource.volume = _sfxVolume;
            }
        }
    }
}

