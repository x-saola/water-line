using System;
using System.Collections.Generic;
using Delta.Core;
using Delta.Modules.Reward;
using Delta.Services;
using Delta.Common.UI;
using UnityEngine;

namespace Delta.Modules.WinStreak
{
    public class WinStreakSystem : MonoBehaviour
    {
        private static WinStreakSystem _instance;
        public static WinStreakSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = Instantiate(Resources.Load<GameObject>("WinStreakSystem"));
                    _instance = go.GetComponent<WinStreakSystem>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private WinStreakConfig _config;
        private WinStreakData _data;


        // UI References
        private WinStreakUI _winStreakUI;


        // Properties
        public WinStreakConfig Config => _config;
        public WinStreakData WinStreakData => _data;


        // Services
        private IUserDataService _userDataService;
        private ITrackingService _trackingService;

        // Constants
        private const string PREFS_KEY = "WinStreakSystem_Data";
        public const string LAST_DISPLAYED_STREAK_KEY = "WinStreak_LastDisplayedStreak";
        private const string IS_LEVEL_ACTIVE_KEY = "WinStreak_IsLevelActive";

        // Tracking
        private const string EVENT_STREAK_ATTEMPT = "streak_attempt";
        private const string EVENT_STREAK_HIGHEST = "streak_highest";

        private void Awake()
        {
            _userDataService = ServiceLocator.Get<IUserDataService>();
            _trackingService = ServiceLocator.Get<ITrackingService>();
        }

        // Events
        public event Action OnWinStreakUpdated;

        public int GetUpdateStreakCount()
        {
            int count = _data.currentStreak - PlayerPrefs.GetInt(LAST_DISPLAYED_STREAK_KEY, 0);
            return count;
        }

        private void OnDestroy()
        {
            _instance = null;
        }
        public void SetLevelActive(bool isActive)
        {
            PlayerPrefs.SetInt(IS_LEVEL_ACTIVE_KEY, isActive ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void CheckLevelState()
        {
            if(PlayerPrefs.GetInt(IS_LEVEL_ACTIVE_KEY, 0) == 1)
            {
                HandlePlayerLost();
                SetLevelActive(false);
            }
        }

        public void Initialize()
        {
            // Only create events if feature is unlocked
            if (!IsUnlocked())
            {
                Debug.Log("[WinStreakSystem] Feature not unlocked yet. Event will start when player reaches the required level.");
                return;
            }

            LoadData();

            // Check if we're in pause period or if event is over
            if (_data == null)
            {
                CreateNewEvent();
            }
            else if (_data.IsInPausePhase())
            {
                // Still in pause period, don't create new event yet
                Debug.Log($"[WinStreakSystem] In pause period. Next event starts in {_data.GetRemainingPauseTime().TotalHours:F1} hours.");
            }
            else if (_data.IsOver())
            {
                // Event is over and pause period is over, create new event
                CreateNewEvent();
            }
            else
            {
                // Event is active
                UpdateBotStreaks();
            }
            CheckLevelState();
        }

        public bool IsUnlocked()
        {
            if (_userDataService == null) return false;
            return _userDataService.CurrentLevel >= _config.unlockLevel;
        }

        public bool IsAvailable()
        {
            if (!IsUnlocked()) return false;
            if (_data == null) return false;
            if (_data.IsInPausePhase()) return false;
            return true;
        }

        /// <summary>
        /// Checks if the win streak system is able to play the tutorial.
        /// Tutorial can be played when the feature is unlocked, no streak has been started yet, and event is not in Pause phase.
        /// </summary>
        /// <returns>True if tutorial can be played, false otherwise</returns>
        public bool IsAbleToPlayTutorial()
        {
            if (_data == null)
                return false;

            // Tutorial can only be played if feature is unlocked
            if (!IsUnlocked())
                return false;

            // Tutorial should only be available if player hasn't started any streak yet
            if (_data.streakAttempt > 0)
                return false;

            // Tutorial cannot be played during Pause phase
            if (_data.IsInPausePhase())
                return false;

            return true;
        }

        public void HandlePlayerWon()
        {
            // Check if player just reached the unlock level and unlock the feature
            if (!IsUnlocked())
            {
                return;
            }

            var playerLevel = _userDataService.CurrentLevel;
            if (playerLevel == _config.unlockLevel)
            {
                Debug.Log("[WinStreakSystem] Feature just unlocked! Initializing first event.");
                Initialize();
            }

            // Only process if system is available
            if (!IsAvailable())
            {
                Debug.Log("[WinStreakSystem] System not available. Ignoring win.");
                return;
            }

            // Track if this is a 0->1 transition (new attempt)
            int previousStreak = _data.currentStreak;
            
            // Increase streak count
            _data.currentStreak++;
            
            // Track analytics for streak changes
            TrackStreakAnalytics(previousStreak);
            
            SaveData();
            OnWinStreakUpdated?.Invoke();
            Debug.Log("[WinStreakSystem] Streak updated. Current streak: " + _data.currentStreak);

            // Check for rewards
            var rewards = GetRewardsForCurrentStreak();
            if (rewards != null && rewards.Length > 0)
            {
                _data.unclaimedRewards.AddRange(rewards);
                SaveData();
            }
        }

        private void TrackStreakAnalytics(int previousStreak)
        {
            if (previousStreak == 0 && _data.currentStreak == 1)
            {
                _data.streakAttempt++;
                TrackStreakEvent(EVENT_STREAK_ATTEMPT, "attempt", _data.streakAttempt);
            }

            if (_data.currentStreak > _data.highestStreak)
            {
                _data.highestStreak = _data.currentStreak;
                TrackStreakEvent(EVENT_STREAK_HIGHEST, "highest_streak", _data.highestStreak);
            }
        }

        private void TrackStreakEvent(string eventId, string valueKey, int value)
        {
            _trackingService?.TrackCustomEvent(eventId, new Dictionary<string, object>()
            {
                {"streak_event_id", _data.streakEventId},
                {valueKey, value}
            });
        }

        public void HandlePlayerLost()
        {
            // Only process if system is available
            if (!IsAvailable())
            {
                Debug.Log("[WinStreakSystem] System not available. Ignoring loss.");
                return;
            }

            _data.currentStreak = 0;
            _data.streakSinceLastClaim = 0;
            PlayerPrefs.SetInt("CurrentMilestoneIndex", 0);
            PlayerPrefs.Save();
            SaveData();
            OnWinStreakUpdated?.Invoke();
            Debug.Log("[WinStreakSystem] Streak reset due to loss.");
        }

        private void CreateNewEvent()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Generate streakEventId from start timestamp
            DateTime eventStart = DateTimeOffset.FromUnixTimeSeconds(now).DateTime;
            string streakEventId = $"streak{eventStart:yyyyMMdd}d";
            
            long eventEndTime = now + _config.duration * 60 * 60;
            long pauseEndTime = eventEndTime + _config.pauseDuration * 60 * 60;
            
            _data = new WinStreakData
            {
                currentStreak = 0,
                highestStreak = 0,
                startTimestamp = now,
                endTimestamp = eventEndTime,
                pauseEndTimestamp = pauseEndTime,
                lastBotUpdateTimestamp = now,
                streakEventId = streakEventId,
                streakAttempt = 0
            };
            
            InitializeBots();
            SaveData();
            
            Debug.Log($"[WinStreakSystem] New event created. Event ID: {streakEventId}");
            Debug.Log($"[WinStreakSystem] Event duration: {_config.duration} hours");
            Debug.Log($"[WinStreakSystem] Pause duration: {_config.pauseDuration} hours");
        }

        private RewardData[] GetRewardsForCurrentStreak()
        {
            foreach (var milestone in _config.milestones)
            {
                if (_data.currentStreak >= milestone.startValue &&
                    (_data.currentStreak - milestone.startValue) % milestone.interval == 0)
                {
                    return milestone.rewards;
                }
            }
            return null;
        }

        private void ClaimRewards()
        {
            if (_data.unclaimedRewards != null && _data.unclaimedRewards.Count > 0)
            {
                RewardClaimHandle.ClaimReward(_data.unclaimedRewards, true, "WinStreak");

                // Clear unclaimed rewards after showing
                _data.unclaimedRewards.Clear();
                _data.streakSinceLastClaim = _data.currentStreak;
                SaveData();
            }
        }

        public TimeSpan GetRemainingEventTime()
        {
            return _data.GetRemainingEventTime();
        }

        public void OnWinStreakHomeIconClicked()
        {
            Debug.Log("[WinStreakSystem] Home icon clicked.");
            // Show Win Streak UI
            _winStreakUI = UIManager.Instance.ShowUIOnTop<WinStreakUI>("WinStreakUI");
            _winStreakUI.CloseButtonClicked += CloseWinStreakUI;
            _winStreakUI.PlayButtonClicked += PlayGame;
            _winStreakUI.InfoButtonClicked += ShowInfo;
            _winStreakUI.LeaderboardButtonClicked += ShowLeaderboard;
            //_winStreakUI.RefreshCompleted += ClaimRewards;
            _winStreakUI.Refresh(_data, () =>
            {
                ClaimRewards();
            }
            );
        }

        private void CloseWinStreakUI()
        {
            ClaimRewards();
            UIManager.Instance.ReleaseUI(_winStreakUI, false);
            StopAllCoroutines();
        }

        private void PlayGame()
        {
            GameStateMachineEvent.Trigger(GameStateMachineEventType.Play);
        }

        private void ShowInfo()
        {
            Debug.Log("[WinStreakSystem] Info button clicked.");
            // Implement info display logic here
        }

#region Leaderboard
        private void ShowLeaderboard()
        {
            Debug.Log("[WinStreakSystem] Leaderboard button clicked.");
            UpdateBotStreaks();
            
           _winStreakUI.ShowLeaderboard();
        }

        private void InitializeBots()
        {
            if (_data.bots == null) _data.bots = new List<WinStreakBotData>();
            
            _data.bots.Clear();

            for (int i = 0; i < _config.botCount; i++)
            {
                // Generate random 4-digit number for player name
                int randomNumber = UnityEngine.Random.Range(1000, 10000);
                string name = $"Player{randomNumber}";
                
                // Initial streak distribution: 60% at 0-3, 30% at 4-8, 10% at 9-15
                int initialStreak;
                float roll = UnityEngine.Random.value;
                if (roll < 0.6f)
                {
                    initialStreak = UnityEngine.Random.Range(0, 4); // 60% get 0-3
                }
                else if (roll < 0.9f)
                {
                    initialStreak = UnityEngine.Random.Range(4, 9); // 30% get 4-8
                }
                else
                {
                    initialStreak = UnityEngine.Random.Range(9, 16); // 10% get 9-15
                }
                
                // Clamp to config bounds
                initialStreak = Mathf.Clamp(initialStreak, _config.initialStreakMin, _config.initialStreakMax);
                
                _data.bots.Add(new WinStreakBotData
                {
                    name = name,
                    currentStreak = initialStreak,
                    highestStreak = initialStreak,
                    avatarIndex = UnityEngine.Random.Range(0, 10)
                });
            }
            
            Debug.Log($"[WinStreakSystem] Initialized {_data.bots.Count} bots with varied starting streaks.");
        }

        private void UpdateBotStreaks()
        {
            if (_data.bots == null || _data.bots.Count == 0)
            {
                InitializeBots();
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - _data.lastBotUpdateTimestamp;
            long intervalSeconds = _config.botUpdateIntervalMinutes * 60;
            
            if (intervalSeconds <= 0) intervalSeconds = 3600;

            long intervalsPassed = elapsed / intervalSeconds;

            if (intervalsPassed > 0)
            {
                int botsLost = 0;
                foreach (var bot in _data.bots)
                {
                    // Update streak for each interval
                    for (int i = 0; i < intervalsPassed; i++)
                    {
                        int increment = UnityEngine.Random.Range(_config.minStreakIncrement, _config.maxStreakIncrement + 1);
                        bot.currentStreak += increment;
                        
                        // Update highest streak if current exceeds it
                        if (bot.currentStreak > bot.highestStreak)
                        {
                            bot.highestStreak = bot.currentStreak;
                        }
                        
                        // Bot loss simulation - check if bot "loses" this interval
                        if (UnityEngine.Random.Range(0, 100) < _config.botLossChancePercent)
                        {
                            bot.currentStreak = 0;
                            botsLost++;
                            break; // Stop processing more intervals for this bot
                        }
                    }
                }
                
                _data.lastBotUpdateTimestamp += intervalsPassed * intervalSeconds;
                
                if (botsLost > 0)
                {
                    Debug.Log($"[WinStreakSystem] {botsLost} bot(s) lost their streak.");
                }
                
                // Apply player-relative scaling
                ApplyPlayerRelativeScaling();
                
                SaveData();
                Debug.Log($"[WinStreakSystem] Updated bot streaks for {intervalsPassed} intervals.");
            }
        }

        public List<LeaderboardEntry> GetLeaderboardEntries()
        {
            var list = new List<LeaderboardEntry>
            {
                new LeaderboardEntry
                {
                    name = "You",
                    streak = _data.highestStreak,
                    isPlayer = true,
                    avatarIndex = 0
                }
            };
            
            if (_data.bots != null)
            {
                foreach (var bot in _data.bots)
                {
                    list.Add(new LeaderboardEntry
                    {
                        name = bot.name,
                        streak = bot.highestStreak,
                        isPlayer = false,
                        avatarIndex = bot.avatarIndex
                    });
                }
            }
            
            list.Sort((a, b) => b.streak.CompareTo(a.streak));
            
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                entry.rank = i + 1;
                list[i] = entry;
            }
            
            return list;
        }

        private void ApplyPlayerRelativeScaling()
        {
            if (!_config.usePlayerRelativeScaling || _data.currentStreak <= 0)
            {
                return;
            }

            int minTarget = (_data.currentStreak * _config.playerRelativeMinPercent) / 100;
            int maxTarget = (_data.currentStreak * _config.playerRelativeMaxPercent) / 100;

            int adjustedBots = 0;
            foreach (var bot in _data.bots)
            {
                // Gradually adjust bots toward target range
                if (bot.currentStreak < minTarget)
                {
                    bot.currentStreak = Mathf.Min(bot.currentStreak + 1, minTarget);
                    adjustedBots++;
                }
                else if (bot.currentStreak > maxTarget)
                {
                    bot.currentStreak = Mathf.Max(bot.currentStreak - 1, maxTarget);
                    adjustedBots++;
                }
            }

            if (adjustedBots > 0)
            {
                Debug.Log($"[WinStreakSystem] Adjusted {adjustedBots} bot(s) to player-relative range ({minTarget}-{maxTarget}).");
            }
        }

        public LeaderboardEntry GetPlayerLeaderboardEntry()
        {
            return new LeaderboardEntry
            {
                name = "You",
                streak = _data.highestStreak,
                isPlayer = true,
                avatarIndex = 0
            };
        }

#endregion

        #region Data Management

        private void LoadData()
        {
            // Load from PlayerPrefs
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                try
                {
                    _data = JsonUtility.FromJson<WinStreakData>(json);
                    Debug.Log($"[WinStreakSystem] Loaded data from PlayerPrefs: {json}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WinStreakSystem] Failed to load data: {e.Message}");
                    _data = new WinStreakData();
                }
            }
            else
            {
                Debug.Log("[WinStreakSystem] No saved data found, creating new.");
            }

            //Debug.Log($"[WinStreakSystem] Loaded data - Unlocked: {_winStreakData.isUnlocked}, Current streak: {_winStreakData.totalWins}");
        }

        public void SaveData()
        {
            if (_data != null)
            {
                string json = JsonUtility.ToJson(_data);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"[WinStreakSystem] Data saved to PlayerPrefs: {json}");
            }
        }

        #endregion
    }
}
