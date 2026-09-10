using UnityEngine;
using UnityEngine.Events;

namespace Delta.Modules.SpinWheel
{
    /// <summary>
    /// Simple reward data structure for SpinWheel rewards.
    /// Project-independent replacement for external RewardData.
    /// </summary>
    [System.Serializable]
    public class SpinWheelReward
    {
        [Tooltip("Unique identifier for this reward type")]
        public int rewardId;
        
        [Tooltip("Amount/quantity of this reward")]
        public int amount;
        
        [Tooltip("Icon/sprite to display for this reward")]
        public Sprite icon;
        
        [Tooltip("Optional label/name for the reward")]
        public string label;

        public SpinWheelReward(int id, int count, Sprite rewardIcon, string rewardLabel = "")
        {
            rewardId = id;
            amount = count;
            icon = rewardIcon;
            label = rewardLabel;
        }
    }

    /// <summary>
    /// Serializable UnityEvent for SpinWheelReward
    /// </summary>
    [System.Serializable]
    public class SpinWheelRewardEvent : UnityEvent<SpinWheelReward>
    {
    }
}

