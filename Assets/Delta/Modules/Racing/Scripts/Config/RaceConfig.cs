using System.Collections.Generic;
using UnityEngine;
using Delta.Modules.Reward;

namespace Delta.Modules.Racing
{
    [CreateAssetMenu(fileName = "RaceConfig", menuName = "Racing/Race Config")]
    public class RaceConfig : ScriptableObject
    {
        [Header("Unlock Requirements")]
        [Tooltip("Player level required to unlock the racing feature")]
        public int unlockLevel = 5;
        
        [Header("Race Settings")]
        public int objectivesRequired = 4;
        
        [Header("AI Settings")]
        [Tooltip("Minimum time in seconds between AI progress updates")]
        public float aiProgressIntervalMin = 45f;
        [Tooltip("Maximum time in seconds between AI progress updates")]
        public float aiProgressIntervalMax = 90f;
        [Tooltip("Chance (0-1) that multiple AI will progress in same cycle")]
        public float multipleAIProgressChance = 0.3f;
        
        [Header("Event Cycle")]
        [Tooltip("Duration of the active racing event in seconds (72 hours = 3 days)")]
        public int eventDurationSeconds = 259200; // 3 days
        [Tooltip("Duration of the pause period between events in seconds (72 hours = 3 days)")]
        public int eventPauseSeconds = 259200; // 3 days pause between events
        
        [Header("Rewards")]
        public List<RewardData> firstPlaceRewards;
        public List<RewardData> secondPlaceRewards;
        public List<RewardData> thirdPlaceRewards;
        
        [Header("Opponent Settings")]
        [Tooltip("Number of AI opponents in each race")]
        [Range(2, 8)]
        public int numberOfOpponents = 4;
        [Tooltip("Number of available avatar sprites")]
        public int avatarCount = 7;
        
        [Header("Cooldown & Limits")]
        [Tooltip("Cooldown in seconds after completing a race")]
        public int cooldownSeconds = 3600; // 1 hour
        [Tooltip("Maximum number of races per day (0 = unlimited)")]
        public int maxDailyRaces = 5;
        
        [Header("Entry Bonuses")]
        [Tooltip("Bonus currency given on race entry")]
        public string entryCurrencyId = "coin";
        public int entryCurrencyAmount = 50;
        
        [Header("Monetization")]
        [Tooltip("Ad reward multiplier for watching ad")]
        public float adRewardMultiplier = 2f;
        [Tooltip("Premium currency cost to skip cooldown")]
        public int skipCooldownCost = 50;
        [Tooltip("Premium currency cost for extra daily race")]
        public int extraRaceCost = 100;
        
        [Tooltip("Placement id passed to IAdService.ShowRewardedAd")]
        public string adPlacementId = "RaceSystem";

        public float GetRandomAIInterval()
        {
            return Random.Range(aiProgressIntervalMin, aiProgressIntervalMax);
        }

        public List<RewardData> GetRewardsForRank(int rank)
        {
            switch (rank)
            {
                case 1: return firstPlaceRewards;
                case 2: return secondPlaceRewards;
                case 3: return thirdPlaceRewards;
                default: return new List<RewardData>();
            }
        }
    }
}
