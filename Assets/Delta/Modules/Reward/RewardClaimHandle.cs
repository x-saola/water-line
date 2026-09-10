using System.Collections.Generic;
using Delta.Core;
using Delta.Services;
using Delta.Common.UI;
using UnityEngine;

namespace Delta.Modules.Reward
{
    public class RewardClaimHandle : MonoBehaviour
    {

        private static List<RewardData> CombineRewards(List<RewardData> rewards)
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

        public static ClaimRewardUI ClaimReward(List<RewardData> rewardDatas, bool showPopup = false, string from = "")
        {
            // Combine rewards of the same type
            List<RewardData> combinedRewards = CombineRewards(rewardDatas);

            var userDataService = ServiceLocator.Get<IUserDataService>();
            foreach (var reward in combinedRewards)
            {
                switch (reward.type)
                {
                    case RewardType.Coin:
                        // Add coins to player account
                        userDataService.AddCurrency("GameConstants.ID_COIN", reward.amount, from, "");
                        Debug.Log($"Claimed {reward.amount} coins.");
                        break;
                    case RewardType.Gem:
                        // Add gems to player account
                        userDataService.AddCurrency("GameConstants.ID_GEM", reward.amount, from, "");
                        Debug.Log($"Claimed {reward.amount} gems.");
                        break;
                    case RewardType.Hint:
                        // Add item to player inventory
                        userDataService.AddItem("GameConstants.ID_BOOSTER_HINT", reward.amount, from, "");
                        Debug.Log($"Claimed {reward.amount} hints.");
                        break;
                    default:
                        Debug.LogWarning("Unknown reward type.");
                        break;
                }
            }

            ClaimRewardUI popup = null;
            if (showPopup)
            {
                popup = UIManager.Instance.ShowUIOnTop<ClaimRewardUI>("ClaimRewardUI");
                popup.ShowRewards(combinedRewards);
            }

            if (!string.IsNullOrEmpty(from))
            {
                Debug.Log($"Rewards claimed from: {from}");
            }

            return popup;
        }
    }
}
