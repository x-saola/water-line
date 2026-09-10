using System.Collections;
using System.Collections.Generic;
using Delta.Common.UI;
using Delta.ConnectMaster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.Reward
{
    public class ClaimRewardUI : BounceAppearUI
    {
        [SerializeField] private List<RewardItem> _rewardItems;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _multiplyRewardButton;
        [SerializeField] private TextMeshProUGUI _multiplyLabel;
        [SerializeField] private Transform _rewardItemContainer;

        public event System.Action OnRewardsClosed;

        protected override void OnEnable()
        {
            base.OnEnable();
            _continueButton.onClick.AddListener(HandleContinueButtonClicked);
            _multiplyRewardButton.onClick.AddListener(HandleMultiplyRewardButtonClicked);
        }

        private void OnDisable()
        {
            _continueButton.onClick.RemoveListener(HandleContinueButtonClicked);
            _multiplyRewardButton.onClick.RemoveListener(HandleMultiplyRewardButtonClicked);
        }

        private List<RewardData> CombineRewards(List<RewardData> rewards)
        {
            Dictionary<RewardType, RewardData> combinedRewards = new Dictionary<RewardType, RewardData>();

            foreach (var reward in rewards)
            {
                if (combinedRewards.ContainsKey(reward.type))
                {
                    combinedRewards[reward.type].amount += reward.amount;
                }
                else
                {
                    combinedRewards[reward.type] = new RewardData
                    {
                        type = reward.type,
                        amount = reward.amount,
                        icon = reward.icon
                    };
                }
            }

            return new List<RewardData>(combinedRewards.Values);
        }

        public void ShowRewards(List<RewardData> rewards)
        {
            // Combine rewards of the same type
            List<RewardData> combinedRewards = CombineRewards(rewards);

            foreach (RewardItem item in _rewardItems)
            {
                item.Hide();
            }

            if (_rewardItems.Count < combinedRewards.Count)
            {
                int itemsToCreate = combinedRewards.Count - _rewardItems.Count;
                for (int i = 0; i < itemsToCreate; i++)
                {
                    RewardItem newItem = Instantiate(_rewardItems[0], _rewardItemContainer);
                    _rewardItems.Add(newItem);
                }
            }

            for (int i = 0; i < _rewardItems.Count && i < combinedRewards.Count; i++)
            {
                _rewardItems[i].Show(combinedRewards[i].icon, combinedRewards[i].amount);
            }

            //_multiplyRewardButton.gameObject.SetActive(canMultiply);
        }

        private void HandleContinueButtonClicked()
        {
            OnRewardsClosed?.Invoke();
            UIManager.Instance.ReleaseUI(this, true);
        }

        private void HandleMultiplyRewardButtonClicked()
        {
            // Implement ad watching or other logic here
            Debug.Log("Multiply Reward Button Clicked");
        }


    }
}
