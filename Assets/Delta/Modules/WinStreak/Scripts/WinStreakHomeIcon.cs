using System;
using System.Collections;
using System.Collections.Generic;
using Delta.Common.UI;
using Delta.Modules.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIEffects;

namespace Delta.Modules.WinStreak
{
    public class WinStreakHomeIcon : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _streakLabel;
        [SerializeField] private TextMeshProUGUI _unlockText;
        [SerializeField] private GameObject _lockedState;
        [SerializeField] private GameObject _unlockedState;
        [SerializeField] private ParticleSystem _initParticleSystem;
        [SerializeField] private ParticleSystem _attractedParticleSystem;
        [SerializeField] private List<UIEffect> _lockEffects;
        [SerializeField] private RectTransform _unlockParticleSystem;
        bool tutorialCompleted => PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;


        private TimeSpan _remainingTime;
        private System.Action _onAttractionComplete;

        private const string TUTORIAL_COMPLETED_KEY = "WinStreak_TutorialCompleted";

        private System.Action _onUnlockComplete;

        private void Awake()
        {
#if !ENABLE_WINSTREAK
            gameObject.SetActive(false);
            return;
#endif
            // UpdateLockState();
        }

        private void OnEnable()
        {
            UpdateLockState();

            // Only setup events if unlocked and available
            if (WinStreakSystem.Instance.IsAvailable())
            {
                _button.onClick.AddListener(OnButtonClicked);
                _remainingTime = WinStreakSystem.Instance.GetRemainingEventTime();
                TimeManager.Instance.OnSecondPassed += UpdateTimer;
                WinStreakSystem.Instance.OnWinStreakUpdated += Refresh;
                _onAttractionComplete = () =>
                {
                    Refresh();
                    transform.DOScale(Vector3.one * 1.1f, 0.2f).SetLoops(2, LoopType.Yoyo);
                    _attractedParticleSystem.Play();
                };
                if (tutorialCompleted)
                {
                    PlayInitEffect();
                }
                _onUnlockComplete = () =>
                {
                    PlayInitEffect();
                };
            }
        }

        private void UpdateLockState()
        {
            bool isUnlocked = WinStreakSystem.Instance.IsUnlocked();

            // Check if we need to create a new event (pause period has ended)
            if (isUnlocked && WinStreakSystem.Instance.WinStreakData != null)
            {
                if (!WinStreakSystem.Instance.WinStreakData.IsInPausePhase() &&
                    WinStreakSystem.Instance.WinStreakData.IsOver())
                {
                    WinStreakSystem.Instance.Initialize();
                }
            }

            bool isAvailable = WinStreakSystem.Instance.IsAvailable();

            if (!isUnlocked)
            {
                // Show locked state
                _button.interactable = false;
                if (_lockedState != null) _lockedState.SetActive(true);
                if (_unlockedState != null) _unlockedState.SetActive(false);

                // Update unlock text
                if (_unlockText != null)
                {
                    int unlockLevel = WinStreakSystem.Instance.Config.unlockLevel;
                    _unlockText.text = $"Level {unlockLevel}";
                }
            }
            else if (!isAvailable)
            {
                // In pause period - hide icon
                gameObject.SetActive(false);
            }
            else
            {
                // Show unlocked state
                _button.interactable = true;
                if (_lockedState != null && tutorialCompleted) _lockedState.SetActive(false);
                if (_unlockedState != null) _unlockedState.SetActive(true);
                // Update unlock text
                if (_unlockText != null)
                {
                    int unlockLevel = WinStreakSystem.Instance.Config.unlockLevel;
                    _unlockText.text = $"Level {unlockLevel}";
                }
            }
        }
        private void OnDisable()
        {
            _button?.onClick.RemoveListener(OnButtonClicked);
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnSecondPassed -= UpdateTimer;
            }
            if (WinStreakSystem.Instance != null)
            {
                WinStreakSystem.Instance.OnWinStreakUpdated -= Refresh;
            }
        }

        public void Refresh()
        {
            _streakLabel.text = WinStreakSystem.Instance.WinStreakData.currentStreak.ToString();
        }

        private void UpdateTimer()
        {
            _remainingTime -= TimeSpan.FromSeconds(1);
            _timerText.text = TimeFormatter.FormatTimeSpan(_remainingTime);
        }

        private void OnButtonClicked()
        {
            // Hide tutorial on first click
            bool tutorialCompleted = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
            if (!tutorialCompleted)
            {
                PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
                PlayerPrefs.Save();
            }
            Refresh();

            WinStreakSystem.Instance.OnWinStreakHomeIconClicked();
        }
        public void OnAttracted()
        {
            _onAttractionComplete?.Invoke();
        }

        public void PlayInitEffect()
        {
            int streakUpdateCount = WinStreakSystem.Instance.GetUpdateStreakCount();
            if (streakUpdateCount <= 0)
            {
                Refresh();
                return;
            }

            UpdateStreakLabel(streakUpdateCount);
            SaveDisplayedStreak();
            ConfigureParticleEmission(streakUpdateCount);
            StartCoroutine(PlayParticleEffect());
        }

        private void UpdateStreakLabel(int streakUpdateCount)
        {
            int previousStreak = WinStreakSystem.Instance.WinStreakData.currentStreak - streakUpdateCount;
            _streakLabel.text = previousStreak.ToString();
            Debug.Log("PlayInitEffect with count: " + streakUpdateCount);
        }

        private void SaveDisplayedStreak()
        {
            PlayerPrefs.SetInt(WinStreakSystem.LAST_DISPLAYED_STREAK_KEY,
                WinStreakSystem.Instance.WinStreakData.currentStreak);
        }

        private void ConfigureParticleEmission(int streakUpdateCount)
        {
            var emission = _initParticleSystem.emission;
            var burst = emission.GetBurst(0);
            burst.count = Mathf.Clamp(streakUpdateCount, 1, 5);
            emission.SetBurst(0, burst);
        }

        private IEnumerator PlayParticleEffect()
        {
            yield return new WaitForEndOfFrame();
            // For UI Particle Systems, we need to work with screen space coordinates
            Camera uiCamera = UIManager.Instance.CameraUI;
            Canvas canvas = GetComponentInParent<Canvas>();

            // Get screen center
            Vector2 screenCenter = new Vector2(-Screen.width * 0.5f, Screen.height * 0.5f);

            // Convert screen position to local position within the canvas
            RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenCenter,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera,
                out Vector2 localPoint);

            // Set the particle system's anchored position (since it's a UI element)
            RectTransform particleRectTransform = _initParticleSystem.GetComponent<RectTransform>();
            if (particleRectTransform != null)
            {
                particleRectTransform.anchoredPosition = localPoint;
            }

            _initParticleSystem.Play();
        }
    }
}
