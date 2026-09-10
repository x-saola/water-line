using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Delta.Core;
using Delta.Common.UI;
using Delta.Modules.Utilities;
using Delta.Services;

namespace Delta.Modules.Racing
{
    public class RaceEntryUI : UIController
    {
        [Header("References")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _enterButton;
        [SerializeField] private Button _watchAdButton;
        [SerializeField] private TextMeshProUGUI _eventTimerText;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [SerializeField] private string _readyDescription;
        [SerializeField] private string _pauseDescription;
        

        private RaceSystem _raceSystem;
        private AdService _adService;

        private void Awake()
        {
            _adService = ServiceLocator.Get<AdService>();
        }

        private void OnEnable()
        {
            _raceSystem = RaceSystem.Instance;
            _closeButton.onClick.AddListener(OnCloseClicked);
            _enterButton.onClick.AddListener(OnEnterClicked);
            _watchAdButton.onClick.AddListener(OnWatchAdClicked);
            
            // Initialize description text based on current state
            UpdateUI();
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _enterButton.onClick.RemoveListener(OnEnterClicked);
            _watchAdButton.onClick.RemoveListener(OnWatchAdClicked);
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_raceSystem == null)
                return;

            // Check if race is in cooldown (pause) state
            if (_raceSystem.RaceData.IsOnCooldown())
            {
                // Pause state - show cooldown timer
                if (_eventTimerText != null)
                {
                    var remaining = _raceSystem.GetRemainingEventTime();
                    _eventTimerText.text = TimeFormatter.FormatTimeSpan(remaining);
                }

                _cooldownText.text = TimeFormatter.FormatTimeSpan(_raceSystem.RaceData.GetRemainingCooldown());
                
                if (_descriptionText != null)
                {
                    _descriptionText.text = _pauseDescription;
                }
                
                // Hide enter button during cooldown
                if (_enterButton != null)
                {
                    _enterButton.gameObject.SetActive(false);
                }

                _watchAdButton.gameObject.SetActive(true);
            }
            else
            {
                // Ready state - show event timer
                if (_eventTimerText != null)
                {
                    var remaining = _raceSystem.GetRemainingEventTime();
                    _eventTimerText.text = TimeFormatter.FormatTimeSpan(remaining);
                }
                
                if (_descriptionText != null)
                {
                    _descriptionText.text = _readyDescription;
                }
                
                // Show enter button when ready
                if (_enterButton != null)
                {
                    _enterButton.gameObject.SetActive(true);
                }

                _watchAdButton.gameObject.SetActive(false);
            }
        }

        private void OnEnterClicked()
        {
            if (_raceSystem.CanEnterRace(out string reason))
            {
                _raceSystem.EnterRace();
                _raceSystem.ShowActiveUI();
                Hide();
            }
            else
            {
                Debug.LogWarning($"[RaceEntryUI] Cannot enter: {reason}");
            }
        }

        private void OnWatchAdClicked()
        {
            _adService.ShowRewardedAd("AirRace", (success) =>
            {
                if (success)
                {
                    _raceSystem.SkipCooldown();
                }
                
            });
        }

        private void OnCloseClicked()
        {
            Hide();
        }

        private void Hide()
        {
            UIManager.Instance.ReleaseUI(this, true);
        }
    }
}
