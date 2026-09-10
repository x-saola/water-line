using System;
using Delta.ConnectMaster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Delta.Modules.Utilities;
using Delta.Common.UI;

namespace Delta.Modules.WinStreak
{
    public class WinStreakUI : BounceAppearUI
    {
        [Header("Components")]
        [SerializeField] private MilestonesAnimation  _milestonesAnimation;
        [SerializeField] private WinStreakLeaderboardUI _leaderboardUI;
        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _infoButton;
        [SerializeField] private Button _leaderboardButton;

        [Header("Others")]
        [SerializeField] private TextMeshProUGUI _streakCountLabel;
        [SerializeField] private TextMeshProUGUI[] _highestStreakLabels;
        [SerializeField] private TextMeshProUGUI _timeLeftLabel;
        [SerializeField] private string _streakCountLabelPrefix;
        [SerializeField] private string _highestStreakLabelPrefix;
        [SerializeField] private string _timeLeftLabelPrefix;

        //Properties
        public WinStreakLeaderboardUI LeaderboardUI => _leaderboardUI;


        public Action CloseButtonClicked;
        public Action PlayButtonClicked;
        public Action InfoButtonClicked;
        public Action LeaderboardButtonClicked;

        public void Refresh(WinStreakData data, Action onCompleted)
        {
            //_streakCountLabel.text = _streakCountLabelPrefix + data.currentStreak.ToString();
            foreach (var label in _highestStreakLabels)
            {
                label.text = _highestStreakLabelPrefix + data.highestStreak.ToString();
            }
            //_timeLeftLabel.text = _timeLeftLabelPrefix + data.GetRemainingEventTime().ToString(@"hh\:mm\:ss");
            HandleSecondPassed();
            var currentStreak = WinStreakSystem.Instance.WinStreakData.streakSinceLastClaim;
            var target = WinStreakSystem.Instance.WinStreakData.currentStreak;
            _milestonesAnimation.PlayMilestoneAnimation(currentStreak, target, onCompleted);

            // onCompleted.Invoke();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _closeButton.onClick.AddListener(HandleCloseButtonClicked);
            _playButton.onClick.AddListener(HandlePlayButtonClicked);
            _infoButton.onClick.AddListener(HandleInfoButtonClicked);
            _leaderboardButton.onClick.AddListener(HandleLeaderboardButtonClicked);
            TimeManager.Instance.OnSecondPassed += HandleSecondPassed;

            _leaderboardUI.gameObject.SetActive(false);
        }

        private void HandleSecondPassed()
        {
            var remainTime = WinStreakSystem.Instance.GetRemainingEventTime();
            _timeLeftLabel.text = _timeLeftLabelPrefix + TimeFormatter.FormatTimeSpan(remainTime);
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            _playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            _infoButton.onClick.RemoveListener(HandleInfoButtonClicked);
            _leaderboardButton.onClick.RemoveListener(HandleLeaderboardButtonClicked);
        }

        private void HandleCloseButtonClicked()
        {
            CloseButtonClicked?.Invoke();
        }

        private void HandlePlayButtonClicked()
        {
            PlayButtonClicked?.Invoke();
        }

        private void HandleInfoButtonClicked()
        {
            InfoButtonClicked?.Invoke();
        }

        private void HandleLeaderboardButtonClicked()
        {
            LeaderboardButtonClicked?.Invoke();
        }

        public void ShowLeaderboard()
        {
            _leaderboardUI.gameObject.SetActive(true);
            _leaderboardUI.Refresh(WinStreakSystem.Instance.GetLeaderboardEntries());
        }
    }
}
