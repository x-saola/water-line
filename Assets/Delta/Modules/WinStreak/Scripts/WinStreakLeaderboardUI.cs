using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Delta.ConnectMaster;
using UnityEngine.Assertions;
using TMPro;
using Delta.Modules.Utilities;
using KenToolbox.UI;

namespace Delta.Modules.WinStreak
{
    public class WinStreakLeaderboardUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private List<WinStreakLeaderboardItem> _items;
        [SerializeField] private WinStreakLeaderboardItem _playerItem;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private Button _closeButton;
        [SerializeField] private UIAnimationController _animationController;

        public Action CloseButtonClicked;

        protected void OnEnable()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseButtonClicked);

            TimeManager.Instance.OnSecondPassed += HandleSecondPassed;
            HandleSecondPassed();
        }

        private void HandleSecondPassed()
        {
            _timerLabel.text = TimeFormatter.FormatTimeSpan(WinStreakSystem.Instance.GetRemainingEventTime());
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);

            TimeManager.Instance.OnSecondPassed -= HandleSecondPassed;
        }

        private void HandleCloseButtonClicked()
        {
            _animationController.PlayAnimationAndClose();
        }

        public void Refresh(List<LeaderboardEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                _items[i].SetData(entries[i]);
                if (entries[i].isPlayer)
                {
                    _playerItem.SetData(entries[i]);
                }
            }
        }
    }

    public struct LeaderboardEntry
    {
        public int rank;
        public string name;
        public int streak;
        public bool isPlayer;
        public int avatarIndex;
    }
}
