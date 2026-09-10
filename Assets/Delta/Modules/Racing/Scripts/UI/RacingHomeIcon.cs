using Delta.Modules.Utilities;
using Delta.Services;
using Coffee.UIEffects;
using Delta.Core;
using Delta.Common.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.Racing
{
    public class RacingHomeIcon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _unlockText;
        [SerializeField] private GameObject _lockedState;
        [SerializeField] private GameObject _unlockedState;
        [SerializeField] private List<UIEffect> _lockEffects;
        [SerializeField] private RectTransform _unlockParticleSystem;
        [SerializeField] private Animator _unlockAnimator;
        bool tutorialCompleted => PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        private Button _button;

        private IUserDataService _userDataService;

        private const string TUTORIAL_COMPLETED_KEY = "Racing_TutorialCompleted";

        private void Awake()
        {
            _userDataService = ServiceLocator.Get<IUserDataService>();
#if !ENABLE_RACE
            gameObject.SetActive(false);
            return;
#endif
            _button = _unlockedState.GetComponent<Button>();
        }

        private void OnEnable()
        {
            UpdateLockState();

            // Only setup events if unlocked
            if (_button.interactable)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }

            TimeManager.Instance.OnSecondPassed += UpdateTimer;
        }

        private void OnDisable()
        {
            _button?.onClick.RemoveListener(OnButtonClicked);
            TimeManager.Instance.OnSecondPassed -= UpdateTimer;
        }

        private void UpdateTimer()
        {
            if (RaceSystem.Instance == null) return;
            var remaining = RaceSystem.Instance.GetRemainingEventTime();
            if (remaining.TotalSeconds > 0)
            {
                if (_timerText != null)
                {
                    _timerText.text = TimeFormatter.FormatTimeSpan(remaining);
                }
            }
        }

        private void Update()
        {
            if (RaceSystem.Instance == null) return;

            // Check event phase and hide icon during pause
            var eventPhase = RaceSystem.Instance.GetCurrentEventPhase();
            if (eventPhase == RaceData.EventPhase.Pause)
            {
                // Hide the icon during pause phase
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
                return;
            }
            else
            {
                // Show the icon during active phase
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    UpdateLockState(); // Refresh lock state when showing
                }
            }
        }

        private void UpdateLockState()
        {
            if (RaceSystem.Instance == null) return;

            int currentLevel = _userDataService.CurrentLevel;
            int unlockLevel = RaceSystem.Instance.Config.unlockLevel;

            bool isLocked = currentLevel < unlockLevel;

            if (!isLocked)
            {
                // Check and reset for new cycle transition (also handles state machine)
                bool wasReset = RaceSystem.Instance.CheckAndHandleNewCycle();

                if (wasReset)
                {
                    Debug.Log("[RacingHomeIcon] Race progress reset for new cycle");
                }
            }

            if (isLocked)
            {
                _button.interactable = false;
                if (_lockedState != null) _lockedState.SetActive(true);
                if (_unlockedState != null) _unlockedState.SetActive(false);
                if (_unlockText != null) _unlockText.text = $"Level {unlockLevel}";
            }
            else
            {
                _button.interactable = true;
                if (_lockedState != null && tutorialCompleted) _lockedState.SetActive(false);
                if (_unlockedState != null) _unlockedState.SetActive(true);
                if (_unlockText != null)
                {
                    _unlockText.text = $"Level {unlockLevel}";
                }
            }
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

            RaceSystem.Instance.OnRacingHomeIconClicked();
        }
    }
}
