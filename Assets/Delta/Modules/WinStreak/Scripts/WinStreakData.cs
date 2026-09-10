

using System;
using System.Collections.Generic;
using Delta.Modules.Reward;

namespace Delta.Modules.WinStreak
{
    [System.Serializable]
    public class WinStreakData
    {
        public int currentStreak;
        public int highestStreak;
        public long startTimestamp;
        public long endTimestamp;
        public long pauseEndTimestamp;
        public int streakSinceLastClaim;
        public List<RewardData> unclaimedRewards;
        public List<WinStreakBotData> bots;
        public long lastBotUpdateTimestamp;
        
        // Tracking fields
        public string streakEventId;
        public int streakAttempt;

        public WinStreakData()
        {
            currentStreak = 0;
            highestStreak = 0;
            startTimestamp = 0;
            endTimestamp = 0;
            pauseEndTimestamp = 0;
            streakSinceLastClaim = 0;
            unclaimedRewards = new List<RewardData>();
            bots = new List<WinStreakBotData>();
            lastBotUpdateTimestamp = 0;
            streakEventId = "";
            streakAttempt = 0;
        }

        public TimeSpan GetRemainingEventTime()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remainingSeconds = endTimestamp - currentTimestamp;
            if (remainingSeconds < 0) remainingSeconds = 0;
            return TimeSpan.FromSeconds(remainingSeconds);
        }

        public bool IsOver()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return currentTimestamp >= endTimestamp;
        }

        public bool IsInPausePhase()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Only in pause phase if event has ended but pause period hasn't ended yet
            return currentTimestamp >= endTimestamp && currentTimestamp < pauseEndTimestamp;
        }

        public TimeSpan GetRemainingPauseTime()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remainingSeconds = pauseEndTimestamp - currentTimestamp;
            if (remainingSeconds < 0) remainingSeconds = 0;
            return TimeSpan.FromSeconds(remainingSeconds);
        }
    }

    [System.Serializable]
    public class WinStreakBotData
    {
        public string name;
        public int currentStreak;
        public int highestStreak;
        public int avatarIndex;
    }
}
