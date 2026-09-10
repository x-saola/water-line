using System;
using UnityEngine;


namespace Delta.Modules.Reward
{
    public enum RewardType
    {
        Coin,
        Gem,
        Hint
    }

    [Serializable]
    public class RewardData
    {
        public RewardType type;
        [Tooltip("Amount of the item rewarded")]
        public int amount;
        public Sprite icon;
    }
}
