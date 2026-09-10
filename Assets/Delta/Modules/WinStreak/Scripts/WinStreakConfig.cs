using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.Modules.WinStreak
{
    [CreateAssetMenu(fileName = "WinStreakConfig", menuName = "Atom/Modules/WinStreak/WinStreakConfig")]
    public class WinStreakConfig : ScriptableObject
    {
        [Tooltip("Level required to unlock the Win Streak feature")]
        public int unlockLevel;
        [Tooltip("Duration of the Win Streak event in hours")]
        public int duration;
        [Tooltip("Duration of the pause period between events in hours")]
        public int pauseDuration;

        [Header("Bot Configuration")]
        public List<string> botNames = new List<string>() { "Alice", "Bob", "Charlie", "David", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy" };
        public int botCount = 50;
        public int botUpdateIntervalMinutes = 60;
        public int minStreakIncrement = 0;
        public int maxStreakIncrement = 3;

        [Header("Bot Balance")]
        [Tooltip("Chance (0-100) per interval that a bot loses and resets to 0")]
        public int botLossChancePercent = 15;
        [Tooltip("Minimum initial streak for bots")]
        public int initialStreakMin = 0;
        [Tooltip("Maximum initial streak for bots")]
        public int initialStreakMax = 10;
        [Tooltip("Scale bot scores relative to player's current streak")]
        public bool usePlayerRelativeScaling = true;
        [Tooltip("Minimum bot score as % of player score (e.g., 50 = 50%)")]
        public int playerRelativeMinPercent = 50;
        [Tooltip("Maximum bot score as % of player score (e.g., 120 = 120%)")]
        public int playerRelativeMaxPercent = 120;

        public WinStreakMilestone[] milestones;
    }
}
