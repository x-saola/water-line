using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Delta.Common.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using Delta.Modules.Utilities;
using Delta.ConnectMaster;

namespace Delta.Modules.Racing
{
    public class RaceActiveUI : UIController
    {
        [Header("References")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Transform _competitorsParent;
        [SerializeField] private GameObject _competitorViewPrefab;
        [SerializeField] private List<RaceCompetitorView> _competitorViews;
        [SerializeField] private List<Sprite> _competitorIcons;
        [SerializeField] private RaceTreasureComponent _goldChest;
        [SerializeField] private RaceTreasureComponent _silverChest;
        [SerializeField] private RaceTreasureComponent _bronzeChest;

        [Header("Player Display")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private Slider _playerProgressBar;
        [SerializeField] private TextMeshProUGUI _playerProgressText;
        [SerializeField] private GameObject _playerGoldBadge;
        [SerializeField] private GameObject _playerSilverBadge;
        [SerializeField] private GameObject _playerBronzeBadge;

        private RaceSystem _raceSystem;

        public Action OnCloseButtonClicked;
        public Action OnPlayButtonClicked;

        private void OnEnable()
        {
            _playButton.onClick.AddListener(() => OnPlayButtonClicked?.Invoke());
            _closeButton.onClick.AddListener(() => OnCloseButtonClicked?.Invoke());

            var remaining = _raceSystem.GetRemainingEventTime();

            if (remaining.TotalSeconds > 0)
            {
                _timerText.text = TimeFormatter.FormatTimeSpan(remaining);
            }
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveAllListeners();
            _closeButton.onClick.RemoveAllListeners();
        }

        public void Initialize(RaceSystem raceSystem)
        {
            _raceSystem = raceSystem;

            SetupUI();
            SubscribeToEvents();
        }

        protected override void OnUIStart()
        {
            base.OnUIStart();
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        protected override void OnUIRemoved()
        {
            base.OnUIRemoved();
            UnsubscribeFromEvents();
            _closeButton.onClick.RemoveAllListeners();
        }

        private void SubscribeToEvents()
        {
            if (_raceSystem != null)
            {
                _raceSystem.OnPlayerProgressChanged += UpdatePlayerProgress;
                _raceSystem.OnOpponentProgressChanged += UpdateOpponentProgress;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_raceSystem != null)
            {
                _raceSystem.OnPlayerProgressChanged -= UpdatePlayerProgress;
                _raceSystem.OnOpponentProgressChanged -= UpdateOpponentProgress;
            }
        }

        private void SetupUI()
        {
            if (_raceSystem == null)
                return;

            // Setup player display
            _playerNameText.text = "YOU";
            _playerProgressBar.maxValue = _raceSystem.Config.objectivesRequired;
            _playerProgressBar.value = _raceSystem.RaceData.playerProgress;
            _playerProgressText.text = $"{_raceSystem.RaceData.playerProgress}";

            // Hide all player badges initially
            if (_playerGoldBadge != null) _playerGoldBadge.SetActive(false);
            if (_playerSilverBadge != null) _playerSilverBadge.SetActive(false);
            if (_playerBronzeBadge != null) _playerBronzeBadge.SetActive(false);

            // Setup AI opponent displays
            SetUpCompetitorViews();

            // Check if race is still in completed state (hasn't been handled yet)
            if (_raceSystem.IsInCompletedState())
            {
                int rank = _raceSystem.GetLastRaceRank();
                // Show player badge and unlock treasure if player won
                ShowPlayerBadgeAndUnlockTreasure(rank);
                // Race is completed and hasn't transitioned to next state yet, handle win/loss flow
                StartCoroutine(HandleRaceCompletedFlow(rank));
            }
        }

        private void SetUpCompetitorViews()
        {
            var opponents = _raceSystem.AISimulator.Opponents;
            //opponents.Sort((o1, o2) => o2.progress.CompareTo(o1.progress));

            for (int i = 0; i < opponents.Count && i < _competitorViews.Count; i++)
            {
                var opponent = opponents[i];
                RaceCompetitorView competitorView = _competitorViews[i];
                
                // Use the persisted avatarIndex from opponent, with safety check
                var iconIndex = Mathf.Clamp(opponent.avatarIndex, 0, _competitorIcons.Count - 1);

                if (competitorView != null)
                {
                    competitorView.Initialize(opponent, _raceSystem.Config.objectivesRequired, _competitorIcons[iconIndex]);
                    Debug.Log("haha " + opponent.rank);
                    // Set rank badge if opponent has finished
                    if (opponent.rank > 0)
                    {
                        competitorView.SetRank(opponent.rank);
                        
                        // Unlock corresponding treasure chest
                        if (opponent.rank == 1 && _goldChest != null)
                        {
                            _goldChest.SetLocked(false);
                        }
                        else if (opponent.rank == 2 && _silverChest != null)
                        {
                            _silverChest.SetLocked(false);
                        }
                        else if (opponent.rank == 3 && _bronzeChest != null)
                        {
                            _bronzeChest.SetLocked(false);
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (_raceSystem == null)
                return;

            UpdateTimer();
        }

        private void UpdateTimer()
        {
            // Get remaining event time
            var remaining = _raceSystem.GetRemainingEventTime();

            if (remaining.TotalSeconds > 0)
            {
                _timerText.text = TimeFormatter.FormatTimeSpan(remaining);
            }
            // else
            // {
            //     _timerText.text = "Event Ended";
            //     _timerText.color = Color.red;
            // }
        }

        private void UpdatePlayerProgress()
        {
            _playerProgressBar.value = _raceSystem.RaceData.playerProgress;
            _playerProgressText.text = $"{_raceSystem.RaceData.playerProgress}";
        }

        private void UpdateOpponentProgress(OpponentProfile opponent)
        {
            // Find the corresponding view and update it
            foreach (var view in _competitorViews)
            {
                if (view.Opponent == opponent)
                {
                    view.UpdateProgress();
                    
                    // Update rank badge if opponent has finished
                    if (opponent.rank > 0)
                    {
                        view.SetRank(opponent.rank);
                        
                        // Unlock corresponding treasure chest
                        if (opponent.rank == 1 && _goldChest != null)
                        {
                            _goldChest.SetLocked(false);
                        }
                        else if (opponent.rank == 2 && _silverChest != null)
                        {
                            _silverChest.SetLocked(false);
                        }
                        else if (opponent.rank == 3 && _bronzeChest != null)
                        {
                            _bronzeChest.SetLocked(false);
                        }
                    }
                    break;
                }
            }

            // Sort competitors by progress
            // SortCompetitorViews();
        }

        private void SortCompetitorViews()
        {
            // Sort views by progress (descending)
            _competitorViews.Sort((a, b) => b.Opponent.progress.CompareTo(a.Opponent.progress));

            // Update visual order
            for (int i = 0; i < _competitorViews.Count; i++)
            {
                _competitorViews[i].transform.SetSiblingIndex(i);
            }
        }

        private void ShowPlayerBadgeAndUnlockTreasure(int rank)
        {
            // Hide all badges first
            if (_playerGoldBadge != null) _playerGoldBadge.SetActive(false);
            if (_playerSilverBadge != null) _playerSilverBadge.SetActive(false);
            if (_playerBronzeBadge != null) _playerBronzeBadge.SetActive(false);

            // Show badge and unlock treasure based on player's rank
            if (rank == 1)
            {
                if (_playerGoldBadge != null) _playerGoldBadge.SetActive(true);
                if (_goldChest != null) _goldChest.SetLocked(false);
            }
            else if (rank == 2)
            {
                if (_playerSilverBadge != null) _playerSilverBadge.SetActive(true);
                if (_silverChest != null) _silverChest.SetLocked(false);
            }
            else if (rank == 3)
            {
                if (_playerBronzeBadge != null) _playerBronzeBadge.SetActive(true);
                if (_bronzeChest != null) _bronzeChest.SetLocked(false);
            }
        }

        private void OnCloseClicked()
        {
            // Warn about closing during active race
            Debug.LogWarning("[RaceActiveUI] Closing during active race - progress will be saved");
            Hide();
        }

        private void Hide()
        {
            UIManager.Instance.ReleaseUI(this, true);
        }

        private IEnumerator HandleRaceCompletedFlow(int rank)
        {
            // Wait 1 second
            yield return new WaitForSeconds(1f);

            // Check if player won (rank 1-3)
            bool isVictory = rank > 0 && rank <= 3;

            if (isVictory)
            {
                // Show player badge and unlock treasure
                ShowPlayerBadgeAndUnlockTreasure(rank);
                
                // Player won - claim rewards and close UI after rewards popup is dismissed
                Debug.Log($"[RaceActiveUI] Player won with rank {rank}! Claiming rewards...");
                _raceSystem.ClaimRewards(false, onRewardsPopupClosed: () =>
                {
                    Debug.Log("[RaceActiveUI] Rewards popup closed, hiding active UI and showing entry UI");
                    Hide();
                    _raceSystem.ShowEntryUI();
                });
            }
            else
            {
                // Player lost - show lose UI and close active UI
                Debug.Log($"[RaceActiveUI] Player lost with rank {rank}. Showing lose UI...");
                var loseUI = UIManager.Instance.ShowUIOnTop<RaceLoseUI>("RaceLoseUI");
                loseUI.OnClosed += () =>
                {
                    Debug.Log("[RaceActiveUI] Lose UI closed, showing entry UI");
                    _raceSystem.ShowEntryUI();
                };
                Hide();
            }

            // Transition to cooldown state to prevent this flow from running again
            _raceSystem.TransitionToCooldown();
        }
    }
}
