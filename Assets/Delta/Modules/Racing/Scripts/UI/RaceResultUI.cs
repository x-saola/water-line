using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Delta.Common.UI;
using DG.Tweening;

namespace Delta.Modules.Racing
{
    public class RaceResultUI : UIController
    {
        [Header("References")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _claimWithAdButton;
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private GameObject _defeatPanel;
        
        [Header("Victory UI")]
        [SerializeField] private TextMeshProUGUI _victoryTitleText;
        [SerializeField] private TextMeshProUGUI _victoryMessageText;
        [SerializeField] private GameObject _victoryParticles;
        
        [Header("Defeat UI")]
        [SerializeField] private TextMeshProUGUI _defeatTitleText;
        [SerializeField] private TextMeshProUGUI _defeatMessageText;
        
        [Header("Common UI")]
        [SerializeField] private Transform _rewardsParent;
        [SerializeField] private GameObject _rewardItemPrefab;
        [SerializeField] private TextMeshProUGUI _statisticsText;
        [SerializeField] private TextMeshProUGUI _nextRaceText;
        [SerializeField] private TextMeshProUGUI _adMultiplierText;

        private RaceSystem _raceSystem;
        private bool _isVictory;
        private bool _rewardsClaimed = false;

        public void Initialize(RaceSystem raceSystem, bool isVictory)
        {
            _raceSystem = raceSystem;
            _isVictory = isVictory;

            SetupUI();
            PlayResultAnimation();
        }

        protected override void OnUIStart()
        {
            base.OnUIStart();
            
            _closeButton.onClick.AddListener(OnCloseClicked);
            _claimButton.onClick.AddListener(OnClaimClicked);
            _claimWithAdButton.onClick.AddListener(OnClaimWithAdClicked);
        }

        protected override void OnUIRemoved()
        {
            base.OnUIRemoved();
            
            _closeButton.onClick.RemoveAllListeners();
            _claimButton.onClick.RemoveAllListeners();
            _claimWithAdButton.onClick.RemoveAllListeners();
        }

        private void SetupUI()
        {
            if (_raceSystem == null)
                return;

            // Show appropriate panel
            _victoryPanel.SetActive(_isVictory);
            _defeatPanel.SetActive(!_isVictory);

            if (_isVictory)
            {
                int rank = _raceSystem.GetLastRaceRank();
                string rankText = "VICTORY!";
                string messageText = "You finished in the top 3!";

                switch (rank)
                {
                    case 1:
                        rankText = "1ST PLACE!";
                        messageText = "You won the race!";
                        break;
                    case 2:
                        rankText = "2ND PLACE!";
                        messageText = "So close! Great racing!";
                        break;
                    case 3:
                        rankText = "3RD PLACE!";
                        messageText = "You made it to the podium!";
                        break;
                }
                
                _victoryTitleText.text = rankText;
                _victoryMessageText.text = messageText;
            }
            else
            {
                _defeatTitleText.text = "DEFEAT";
                _defeatMessageText.text = "You didn't finish in the top 3. Better luck next time!";
            }

            // Display rewards
            DisplayRewards();

            // Display tier progress
            UpdateTierInfo();

            // Display next race availability
            UpdateNextRaceInfo();

            // Display ad multiplier option (only for victory)
            if (_claimWithAdButton != null)
            {
                _claimWithAdButton.gameObject.SetActive(_isVictory);
                if (_adMultiplierText != null)
                {
                    _adMultiplierText.text = $"{_raceSystem.Config.adRewardMultiplier}x";
                }
            }
        }

        private void DisplayRewards()
        {
            // Clear existing rewards
            foreach (Transform child in _rewardsParent)
            {
                Destroy(child.gameObject);
            }

            if (!_isVictory)
            {
                // No rewards for defeat (or minimal consolation prize)
                return;
            }

            var rewards = _raceSystem.GetCurrentRewards();
            foreach (var reward in rewards)
            {
                if (_rewardItemPrefab != null)
                {
                    GameObject item = Instantiate(_rewardItemPrefab, _rewardsParent);
                    
                    var icon = item.transform.Find("Icon")?.GetComponent<Image>();
                    var amountText = item.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
                    
                    if (icon != null && reward.icon != null)
                        icon.sprite = reward.icon;
                    
                    if (amountText != null)
                        amountText.text = reward.amount.ToString();
                }
            }
        }

        private void UpdateTierInfo()
        {
            var data = _raceSystem.RaceData;
            
            // Show win/loss statistics
            if (_statisticsText != null)
            {
                _statisticsText.text = $"Total Wins: {data.totalWins} | Losses: {data.totalLosses}\n" +
                                      $"Current Streak: {data.currentWinStreak} | Best: {data.bestWinStreak}";
            }
        }

        private void UpdateNextRaceInfo()
        {
            if (_nextRaceText != null)
            {
                var cooldown = _raceSystem.RaceData.GetRemainingCooldown();
                if (cooldown.TotalSeconds > 0)
                {
                    _nextRaceText.text = $"Next race in: {cooldown.Hours:D2}:{cooldown.Minutes:D2}:{cooldown.Seconds:D2}";
                }
                else
                {
                    _nextRaceText.text = "Ready for next race!";
                }
            }
        }

        private void PlayResultAnimation()
        {
            if (_isVictory)
            {
                // Play victory animation
                if (_victoryParticles != null)
                {
                    _victoryParticles.SetActive(true);
                }

                // Scale animation
                if (_victoryPanel != null)
                {
                    _victoryPanel.transform.localScale = Vector3.zero;
                    _victoryPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
                }
            }
            else
            {
                // Play defeat animation
                if (_defeatPanel != null)
                {
                    _defeatPanel.transform.localScale = Vector3.zero;
                    _defeatPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutQuad);
                }
            }
        }

        private void OnClaimClicked()
        {
            if (_rewardsClaimed)
                return;

            if (_isVictory)
            {
                _raceSystem.ClaimRewards(false);
                _rewardsClaimed = true;
            }

            Hide();
        }

        private void OnClaimWithAdClicked()
        {
            if (_rewardsClaimed)
                return;

            _raceSystem.WatchAdForReward((success) =>
            {
                if (success)
                {
                    _rewardsClaimed = true;
                    
                    // Show feedback
                    if (_adMultiplierText != null)
                    {
                        _adMultiplierText.text = "Claimed!";
                    }

                    // Auto close after short delay
                    DOVirtual.DelayedCall(1f, () => Hide());
                }
            });
        }

        private void OnCloseClicked()
        {
            // Auto-claim if not claimed yet
            if (!_rewardsClaimed && _isVictory)
            {
                _raceSystem.ClaimRewards(false);
            }

            Hide();
        }

        private void Hide()
        {
            UIManager.Instance.ReleaseUI(this, true);
        }

        private void Update()
        {
            // Update cooldown timer
            UpdateNextRaceInfo();
        }
    }
}
