using System;
using System.Collections.Generic;

namespace Delta.Modules.Racing
{
    [Serializable]
    public class RaceData
    {
        // Statistics
        public int totalRacesEntered = 0;
        public int totalWins = 0;
        public int totalLosses = 0;
        public int currentWinStreak = 0;
        public int bestWinStreak = 0;
        
        // Cooldown & limits
        public long cooldownEndTimestamp = 0;
        public int dailyRacesCompleted = 0;
        public string lastRaceDateString = "";
        
        // Current race state
        public bool isInActiveRace = false;
        public int playerProgress = 0;
        public List<OpponentProfile> activeOpponents = new List<OpponentProfile>();
        public long lastSimulationTimestamp = 0;
        
        // Unlock status
        public bool isUnlocked = false;

        // Event cycle tracking
        public int eventCycleNumber = 0; // Which cycle we're in (0, 1, 2, ...)
        public long eventCycleStartTimestamp = 0; // When the current cycle started (not just event, the whole cycle)
        
        // Legacy field - now managed by cycle system
        public long eventStartTimestamp = 0;
        
        // Rewards status
        public bool rewardsClaimed = false;
        
        // Phase tracking for cycle transitions
        public int lastEventPhase = 0; // 0=NotStarted, 1=Active, 2=Pause
        
        public RaceData()
        {
            totalRacesEntered = 0;
            totalWins = 0;
            totalLosses = 0;
            currentWinStreak = 0;
            bestWinStreak = 0;
            cooldownEndTimestamp = 0;
            dailyRacesCompleted = 0;
            lastRaceDateString = DateTime.UtcNow.ToString("yyyy-MM-dd");
            isInActiveRace = false;
            playerProgress = 0;
            activeOpponents = new List<OpponentProfile>();
            lastSimulationTimestamp = 0;
            isUnlocked = false;
            eventStartTimestamp = 0;
            rewardsClaimed = false;
            lastEventPhase = 0;
        }

        public bool IsEventActive(int durationSeconds)
        {
            if (eventStartTimestamp == 0) return false;
            long endTime = eventStartTimestamp + (long)durationSeconds * 10000000; // Ticks
            return DateTime.UtcNow.Ticks < endTime;
        }

        public TimeSpan GetRemainingEventTime(int durationSeconds)
        {
            if (eventStartTimestamp == 0) return TimeSpan.Zero;
            long endTime = eventStartTimestamp + (long)durationSeconds * 10000000; // Ticks
            long remainingTicks = endTime - DateTime.UtcNow.Ticks;
            return remainingTicks > 0 ? TimeSpan.FromTicks(remainingTicks) : TimeSpan.Zero;
        }

        public bool IsOnCooldown()
        {
            return DateTime.UtcNow.Ticks < cooldownEndTimestamp;
        }

        public TimeSpan GetRemainingCooldown()
        {
            if (!IsOnCooldown())
                return TimeSpan.Zero;
            
            return TimeSpan.FromTicks(cooldownEndTimestamp - DateTime.UtcNow.Ticks);
        }

        public void SetCooldown(int cooldownSeconds)
        {
            cooldownEndTimestamp = DateTime.UtcNow.AddSeconds(cooldownSeconds).Ticks;
        }

        public void CheckAndResetDaily()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (lastRaceDateString != today)
            {
                dailyRacesCompleted = 0;
                lastRaceDateString = today;
            }
        }

        public void RecordWin()
        {
            totalWins++;
            totalRacesEntered++;
            currentWinStreak++;
            if (currentWinStreak > bestWinStreak)
            {
                bestWinStreak = currentWinStreak;
            }
            dailyRacesCompleted++;
        }

        public void RecordLoss()
        {
            totalLosses++;
            totalRacesEntered++;
            currentWinStreak = 0;
            dailyRacesCompleted++;
        }

        #region Event Cycle Management

        public enum EventPhase
        {
            NotStarted,  // No cycle has been started yet
            Active,      // Event is active, players can race
            Pause        // Event is on pause, waiting for next cycle
        }

        /// <summary>
        /// Initialize a new event cycle
        /// </summary>
        public void StartNewEventCycle()
        {
            eventCycleNumber++;
            eventCycleStartTimestamp = DateTime.UtcNow.Ticks;
            eventStartTimestamp = DateTime.UtcNow.Ticks; // Keep legacy field in sync
        }

        /// <summary>
        /// Get the current phase of the event cycle
        /// </summary>
        public EventPhase GetEventPhase(int eventDurationSeconds, int pauseDurationSeconds)
        {
            // No cycle started yet
            if (eventCycleStartTimestamp == 0)
                return EventPhase.NotStarted;

            long currentTicks = DateTime.UtcNow.Ticks;
            long cycleDurationTicks = (long)(eventDurationSeconds + pauseDurationSeconds) * 10000000;
            long elapsedTicks = currentTicks - eventCycleStartTimestamp;
            
            // Calculate position within the current cycle
            long positionInCycle = elapsedTicks % cycleDurationTicks;
            long eventDurationTicks = (long)eventDurationSeconds * 10000000;
            
            // If we're within the event duration, we're in Active phase
            if (positionInCycle < eventDurationTicks)
                return EventPhase.Active;
            
            // Otherwise, we're in Pause phase
            return EventPhase.Pause;
        }

        /// <summary>
        /// Get remaining time in the current event (active phase)
        /// </summary>
        public TimeSpan GetRemainingEventTimeInCycle(int eventDurationSeconds, int pauseDurationSeconds)
        {
            EventPhase phase = GetEventPhase(eventDurationSeconds, pauseDurationSeconds);
            
            if (phase != EventPhase.Active)
                return TimeSpan.Zero;

            long currentTicks = DateTime.UtcNow.Ticks;
            long cycleDurationTicks = (long)(eventDurationSeconds + pauseDurationSeconds) * 10000000;
            long elapsedTicks = currentTicks - eventCycleStartTimestamp;
            long positionInCycle = elapsedTicks % cycleDurationTicks;
            long eventDurationTicks = (long)eventDurationSeconds * 10000000;
            
            long remainingTicks = eventDurationTicks - positionInCycle;
            return TimeSpan.FromTicks(remainingTicks);
        }

        /// <summary>
        /// Get remaining time in the current pause phase
        /// </summary>
        public TimeSpan GetRemainingPauseTime(int eventDurationSeconds, int pauseDurationSeconds)
        {
            EventPhase phase = GetEventPhase(eventDurationSeconds, pauseDurationSeconds);
            
            if (phase != EventPhase.Pause)
                return TimeSpan.Zero;

            long currentTicks = DateTime.UtcNow.Ticks;
            long cycleDurationTicks = (long)(eventDurationSeconds + pauseDurationSeconds) * 10000000;
            long elapsedTicks = currentTicks - eventCycleStartTimestamp;
            long positionInCycle = elapsedTicks % cycleDurationTicks;
            long eventDurationTicks = (long)eventDurationSeconds * 10000000;
            
            long pauseElapsed = positionInCycle - eventDurationTicks;
            long pauseDurationTicks = (long)pauseDurationSeconds * 10000000;
            long remainingTicks = pauseDurationTicks - pauseElapsed;
            
            return TimeSpan.FromTicks(remainingTicks);
        }

        /// <summary>
        /// Check if we should start a new cycle (for backward compatibility and initial setup)
        /// </summary>
        public bool ShouldStartNewCycle(int eventDurationSeconds, int pauseDurationSeconds)
        {
            // If no cycle started, we should start one
            if (eventCycleStartTimestamp == 0)
                return true;

            // Check if current phase is Active - if so, we don't need a new cycle
            EventPhase phase = GetEventPhase(eventDurationSeconds, pauseDurationSeconds);
            return phase == EventPhase.NotStarted;
        }

        /// <summary>
        /// Check for phase transition and reset race progress if transitioning from Pause to Active
        /// </summary>
        public bool CheckAndResetForNewCycle(int eventDurationSeconds, int pauseDurationSeconds)
        {
            EventPhase currentPhase = GetEventPhase(eventDurationSeconds, pauseDurationSeconds);
            EventPhase previousPhase = (EventPhase)lastEventPhase;
            
            // Detect Pause -> Active transition (new cycle starting)
            if (previousPhase == EventPhase.Pause && currentPhase == EventPhase.Active)
            {
                // Reset race progress for new cycle
                playerProgress = 0;
                activeOpponents.Clear();
                isInActiveRace = false;
                rewardsClaimed = false;
                
                lastEventPhase = (int)currentPhase;
                return true; // Indicates reset occurred
            }
            
            lastEventPhase = (int)currentPhase;
            return false;
        }

        #endregion
    }
}
