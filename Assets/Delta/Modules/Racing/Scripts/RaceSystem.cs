using System.Collections.Generic;
using UnityEngine;
using Delta.Core;
using Delta.Modules.Reward;
using Delta.Services;
using Delta.Common.UI;

namespace Delta.Modules.Racing
{
    public class RaceSystem : MonoBehaviour
    {
        private static RaceSystem _instance;
        public static RaceSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = Instantiate(Resources.Load<GameObject>("RaceSystem"));
                    _instance = go.GetComponent<RaceSystem>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private RaceConfig _config;

        // Core Systems
        private RaceStateMachine _stateMachine;
        private AIOpponentSimulator _aiSimulator;
        private RaceData _raceData;
        
        // Services
        private IUserDataService _userDataService;
        private ITrackingService _trackingService;
        private IAdService _adService;

        private const string PREFS_KEY = "RaceSystem_Data";

        // UI References
        private RaceEntryUI _entryUI;
        private RaceActiveUI _activeUI;

        // State
        private bool _isInitialized = false;

        // Properties
        public RaceConfig Config => _config;
        public RaceData RaceData => _raceData;
        public AIOpponentSimulator AISimulator => _aiSimulator;

        private void Awake()
        {
            _userDataService = ServiceLocator.Get<IUserDataService>();
            _trackingService = ServiceLocator.Get<ITrackingService>();
            _adService = ServiceLocator.Get<IAdService>();
        }

        // Events
        public event System.Action OnRaceStarted;
        public event System.Action OnRaceWon;
        public event System.Action OnRaceLost;
        public event System.Action OnPlayerProgressChanged;
        public event System.Action<OpponentProfile> OnOpponentProgressChanged;
        public event System.Action OnHomeIconClicked;

        // Internal methods for states to trigger events
        internal void TriggerRaceStartedEvent()
        {
            OnRaceStarted?.Invoke();
        }

        internal void TriggerRaceWonEvent()
        {
            OnRaceWon?.Invoke();
        }

        internal void TriggerRaceLostEvent()
        {
            OnRaceLost?.Invoke();
        }

        public System.TimeSpan GetRemainingEventTime()
        {
            if (_raceData == null || _config == null) return System.TimeSpan.Zero;
            return _raceData.GetRemainingEventTimeInCycle(_config.eventDurationSeconds, _config.eventPauseSeconds);
        }

        public System.TimeSpan GetRemainingPauseTime()
        {
            if (_raceData == null || _config == null) return System.TimeSpan.Zero;
            return _raceData.GetRemainingPauseTime(_config.eventDurationSeconds, _config.eventPauseSeconds);
        }

        public RaceData.EventPhase GetCurrentEventPhase()
        {
            if (_raceData == null || _config == null) return RaceData.EventPhase.NotStarted;
            if (!_raceData.isUnlocked) return RaceData.EventPhase.NotStarted;
            return _raceData.GetEventPhase(_config.eventDurationSeconds, _config.eventPauseSeconds);
        }

        /// <summary>
        /// Checks for new cycle transition and handles state machine synchronization.
        /// Returns true if a cycle reset occurred.
        /// </summary>
        public bool CheckAndHandleNewCycle()
        {
            // Track race end if there's an ongoing race before reset
            if (_raceData.isInActiveRace)
            {
                TrackRaceEnd(0);
            }
            
            bool wasReset = _raceData.CheckAndResetForNewCycle(_config.eventDurationSeconds, _config.eventPauseSeconds);
            
            if (wasReset)
            {
                // Data was reset for new cycle - transition state machine
                if (_stateMachine.IsInState<RaceState_Completed>() || 
                    _stateMachine.IsInState<RaceState_Cooldown>())
                {
                    // Check if we're still on individual race cooldown
                    if (_raceData.IsOnCooldown())
                    {
                        TransitionToCooldown();
                    }
                    else
                    {
                        TransitionToAvailable();
                    }
                }
                SaveData();
                Debug.Log("[RaceSystem] Race reset for new cycle, state transitioned");
            }
            
            return wasReset;
        }

        public void StartEvent()
        {
            // Check for phase transition and reset progress
            CheckAndHandleNewCycle();
            
            // Check if we need to start a new cycle
            if (_raceData.ShouldStartNewCycle(_config.eventDurationSeconds, _config.eventPauseSeconds))
            {
                _raceData.StartNewEventCycle();
                SaveData();
                Debug.Log($"[RaceSystem] Event cycle {_raceData.eventCycleNumber} started! Active: {_config.eventDurationSeconds}s, Pause: {_config.eventPauseSeconds}s");
            }
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        public void Initialize(RaceConfig config = null)
        {
            if (_isInitialized)
                return;

            if (config != null)
                _config = config;

            if (_config == null)
            {
                Debug.LogError("[RaceSystem] No configuration provided!");
                return;
            }

            // Initialize data
            LoadData();

            // Initialize systems
            _aiSimulator = new AIOpponentSimulator(this, this);
            _stateMachine = new RaceStateMachine(this);

            // Subscribe to AI events
            _aiSimulator.OnOpponentProgress += HandleOpponentProgress;
            _aiSimulator.OnOpponentWin += HandleOpponentWin;

            // Initialize state machine
            _stateMachine.Initialize();

            _isInitialized = true;
            Debug.Log("[RaceSystem] Initialized successfully");
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            _stateMachine?.Update();
        }

        private void PlayGame()
        {
            GameStateMachineEvent.Trigger(GameStateMachineEventType.Play);
        }

        #region Data Management

        private string GetRaceId()
        {
            if (_raceData == null || _raceData.eventStartTimestamp == 0)
                return "race00000000d";
            
            System.DateTime eventDate = new System.DateTime(_raceData.eventStartTimestamp);
            return $"race{eventDate:yyyyMMdd}d";
        }

        private string GetLevelId()
        {
            if (_userDataService == null)
                return "0";

            return _userDataService.CurrentLevel.ToString();
        }

        private void LoadData()
        {
            // Load from PlayerPrefs
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                try 
                {
                    _raceData = JsonUtility.FromJson<RaceData>(json);
                    Debug.Log($"[RaceSystem] Loaded data from PlayerPrefs: {json}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RaceSystem] Failed to load data: {e.Message}");
                    _raceData = new RaceData();
                }
            }
            else
            {
                _raceData = new RaceData();
                Debug.Log("[RaceSystem] No saved data found, creating new.");
            }

            // Ensure data is valid
            if (_raceData == null)
                _raceData = new RaceData();

            Debug.Log($"[RaceSystem] Loaded data - Unlocked: {_raceData.isUnlocked}, Total Wins: {_raceData.totalWins}");
        }

        public void SaveData()
        {
            if (_raceData != null)
            {
                string json = JsonUtility.ToJson(_raceData);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"[RaceSystem] Data saved to PlayerPrefs");
            }
        }

        #endregion

        #region Tracking

        private const string EVENT_RACE_START = "race_start";
        private const string EVENT_RACE_END = "race_end";

        private void TrackRaceStart()
        {
            _trackingService?.TrackCustomEvent(EVENT_RACE_START, new Dictionary<string, object>()
            {
                {"race_id", GetRaceId()},
                {"level_id", GetLevelId()}
            });
        }

        private void TrackRaceEnd(int rank)
        {
            _trackingService?.TrackCustomEvent(EVENT_RACE_END, new Dictionary<string, object>()
            {
                {"race_id", GetRaceId()},
                {"level_id", GetLevelId()},
                {"rank", rank}
            });
        }

        #endregion

        #region State Management

        public void TransitionToAvailable()
        {
            _stateMachine.ChangeState(_stateMachine.AvailableState);
        }

        public void TransitionToCooldown()
        {
            _stateMachine.ChangeState(_stateMachine.CooldownState);
        }

        public void TransitionToActive()
        {
            _stateMachine.ChangeState(_stateMachine.ActiveState);
        }

        public void TransitionToCompleted(bool isVictory, int rank)
        {
            _stateMachine.CompletedState.SetResult(isVictory, rank);
            _stateMachine.ChangeState(_stateMachine.CompletedState);
        }

        public bool IsInCompletedState()
        {
            return _stateMachine.IsInState<RaceState_Completed>();
        }

        #endregion

        #region Entry & Validation

        public bool CheckUnlockConditions()
        {
            
            return _userDataService.CurrentLevel >= _config.unlockLevel;
        }

        /// <summary>
        /// Checks if the race system is able to play the tutorial.
        /// Tutorial can be played when the feature is unlocked, no race has been entered yet, and event is not in Pause phase.
        /// </summary>
        /// <returns>True if tutorial can be played, false otherwise</returns>
        public bool IsAbleToPlayTutorial()
        {
            if (_raceData == null)
                return false;

            // Tutorial can only be played if feature is unlocked
            if (!_raceData.isUnlocked)
                return false;

            // Tutorial should only be available if player hasn't entered any races yet
            if (_raceData.totalRacesEntered > 0)
                return false;

            // Tutorial cannot be played during Pause phase
            var eventPhase = GetCurrentEventPhase();
            if (eventPhase == RaceData.EventPhase.Pause)
                return false;

            return true;
        }

        public void UnlockFeature()
        {
            _raceData.isUnlocked = true;
            _raceData.StartNewEventCycle();
            _stateMachine.ChangeState(_stateMachine.AvailableState);
            SaveData();

            Debug.Log("[RaceSystem] Feature unlocked!");
        }

        public bool CanEnterRace(out string reason)
        {
            // Check if unlocked
            if (!_raceData.isUnlocked)
            {
                reason = "Feature not unlocked";
                return false;
            }

            // Check if event cycle is in active phase
            var eventPhase = GetCurrentEventPhase();
            if (eventPhase == RaceData.EventPhase.Pause)
            {
                var pauseRemaining = GetRemainingPauseTime();
                reason = $"Event on pause: {pauseRemaining.Days}d {pauseRemaining.Hours}h {pauseRemaining.Minutes}m remaining";
                return false;
            }
            else if (eventPhase == RaceData.EventPhase.NotStarted)
            {
                reason = "Event not started";
                return false;
            }

            // Check if already in race
            if (_stateMachine.IsInState<RaceState_Active>())
            {
                reason = "Already in an active race";
                return false;
            }

            // Check cooldown
            if (_raceData.IsOnCooldown())
            {
                var remaining = _raceData.GetRemainingCooldown();
                reason = $"Cooldown: {remaining.Hours}h {remaining.Minutes}m remaining";
                return false;
            }

            // Check daily limit
            _raceData.CheckAndResetDaily();
            if (_config.maxDailyRaces > 0 && _raceData.dailyRacesCompleted >= _config.maxDailyRaces)
            {
                reason = "Daily race limit reached";
                return false;
            }

            reason = "";
            return true;
        }

        public void EnterRace()
        {
            if (!CanEnterRace(out string reason))
            {
                Debug.LogWarning($"[RaceSystem] Cannot enter race: {reason}");
                return;
            }

            // Reset rewards claimed flag for new race
            _raceData.rewardsClaimed = false;

            // Give entry bonus
            if (_config.entryCurrencyAmount > 0)
            {
                _userDataService?.AddCurrency(_config.entryCurrencyId, _config.entryCurrencyAmount, "RaceEntry", "bonus");
            }

            // Initialize AI opponents
            _aiSimulator.Initialize(_config, _config.numberOfOpponents);

            // Transition to active state
            TransitionToActive();

            // Track race start event
            TrackRaceStart();
        }

        #endregion

        #region Race Progress

        public void HandlePlayerWon()
        {
            if (!_stateMachine.IsInState<RaceState_Active>())
                return;

            _raceData.playerProgress++;
            SaveData();
            Debug.Log($"[RaceSystem] Player progress: {_raceData.playerProgress}/{_config.objectivesRequired}");
            
            OnPlayerProgressChanged?.Invoke();
        }

        public void OnPlayerFinished(int rank)
        {
            StopAISimulation();
            // Rank 1, 2, 3 are considered "Victory" (prizes awarded)
            // Rank 0 or >3 is a loss
            bool isVictory = rank > 0 && rank <= 3;
            TransitionToCompleted(isVictory, rank);
            
            // Track race end event
            TrackRaceEnd(rank);
        }

        #endregion

        #region AI Simulation

        public void StartAISimulation()
        {
            _aiSimulator.StartSimulation();
        }

        public void StopAISimulation()
        {
            _aiSimulator.StopSimulation();
        }

        public bool HasAnyOpponentWon()
        {
            return _aiSimulator.HasAnyOpponentWon();
        }

        private void HandleOpponentProgress(OpponentProfile opponent)
        {
            SaveData();
            OnOpponentProgressChanged?.Invoke(opponent);
        }

        private void HandleOpponentWin(OpponentProfile opponent)
        {
            // AI finished - handled by RaceState_Active
        }

        #endregion

        #region Statistics

        public void UpdateStatistics()
        {
            bool isVictory = _stateMachine.CompletedState.IsVictory();
            if (isVictory)
            {
                _raceData.RecordWin();
            }
            else
            {
                _raceData.RecordLoss();
            }
            SaveData();
        }

        #endregion

        #region Rewards

        public int GetLastRaceRank()
        {
            if (_stateMachine.IsInState<RaceState_Completed>())
            {
                return _stateMachine.CompletedState.GetRank();
            }
            return 0;
        }

        public List<RewardData> GetCurrentRewards()
        {
            if (_stateMachine.IsInState<RaceState_Completed>())
            {
                int rank = _stateMachine.CompletedState.GetRank();
                return _config.GetRewardsForRank(rank);
            }
            return new List<RewardData>();
        }

        public void ClaimRewards(bool watchedAd = false, System.Action onRewardsPopupClosed = null)
        {
            // Check if rewards already claimed
            if (_raceData.rewardsClaimed)
            {
                Debug.Log("[RaceSystem] Rewards already claimed for this race");
                onRewardsPopupClosed?.Invoke();
                return;
            }

            var rewards = GetCurrentRewards();
            if (rewards == null || rewards.Count == 0)
            {
                onRewardsPopupClosed?.Invoke();
                return;
            }

            float multiplier = watchedAd ? _config.adRewardMultiplier : 1f;
            
            // Apply multiplier if needed
            ClaimRewardUI rewardPopup = null;
            if (multiplier != 1f)
            {
                List<RewardData> multipliedRewards = new List<RewardData>();
                foreach (var reward in rewards)
                {
                    multipliedRewards.Add(new RewardData
                    {
                        type = reward.type,
                        amount = Mathf.RoundToInt(reward.amount * multiplier),
                        icon = reward.icon
                    });
                }
                rewardPopup = RewardClaimHandle.ClaimReward(multipliedRewards, showPopup: true, from: "RaceReward");
            }
            else
            {
                rewardPopup = RewardClaimHandle.ClaimReward(rewards, showPopup: true, from: "RaceReward");
            }

            // Subscribe to popup closed event
            if (rewardPopup != null && onRewardsPopupClosed != null)
            {
                rewardPopup.OnRewardsClosed += onRewardsPopupClosed;
            }

            // Mark rewards as claimed
            _raceData.rewardsClaimed = true;

            Debug.Log($"[RaceSystem] Claimed rewards (multiplier: {multiplier}x)");
            SaveData();
        }

        public void WatchAdForReward(System.Action<bool> callback)
        {
            _adService?.ShowRewardedAd(_config.adPlacementId, (success) =>
            {
                if (success)
                {
                    ClaimRewards(true);
                }
                callback?.Invoke(success);
            });
        }

        #endregion

        #region Monetization

        public void SkipCooldown()
        {
            // Implement premium currency deduction
            // For now, just clear cooldown
            _raceData.cooldownEndTimestamp = 0;
            TransitionToAvailable();
            SaveData();
        }

        public void PurchaseExtraRace()
        {
            // Implement premium currency deduction
            // For now, just reset daily count
            _raceData.dailyRacesCompleted = Mathf.Max(0, _raceData.dailyRacesCompleted - 1);
            SaveData();
        }

        #endregion

        #region UI Management

        /// <summary>
        /// Called when the racing home icon is clicked. Opens appropriate UI based on current state.
        /// </summary>
        public void OnRacingHomeIconClicked()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[RaceSystem] System not initialized yet");
                return;
            }

            // Trigger event for any listeners
            OnHomeIconClicked?.Invoke();

            // Check and handle new cycle transition to ensure state is synchronized
            CheckAndHandleNewCycle();

            // Determine which UI to show based on current state
            if (_stateMachine.IsInState<RaceState_Active>())
            {
                // Resume active race
                Debug.Log("[RaceSystem] Opening Active UI - race in progress");
                ShowActiveUI();
            }
            else if (_stateMachine.IsInState<RaceState_Completed>())
            {
                // Show active UI even if race is completed
                Debug.Log("[RaceSystem] Opening Active UI - race completed");
                ShowActiveUI();
            }
            else if (_stateMachine.IsInState<RaceState_Locked>())
            {
                // Show entry UI (can display unlock requirements)
                Debug.Log("[RaceSystem] Opening Entry UI - feature locked");
                ShowEntryUI();
            }
            else if (_stateMachine.IsInState<RaceState_Cooldown>())
            {
                // Show entry UI (displays cooldown timer)
                Debug.Log("[RaceSystem] Opening Entry UI - on cooldown");
                ShowEntryUI();
            }
            else if (_stateMachine.IsInState<RaceState_Available>())
            {
                // Ready to start new race
                Debug.Log("[RaceSystem] Opening Entry UI - ready to race");
                ShowEntryUI();
            }
            else
            {
                // Fallback - show entry UI
                Debug.LogWarning("[RaceSystem] Unknown state, showing entry UI");
                ShowEntryUI();
            }
        }

        public void ShowEntryUI()
        {
            if (_entryUI == null)
            {
                _entryUI = UIManager.Instance.ShowUIOnTop<RaceEntryUI>("RaceEntryUI");
            }
        }

        public void ShowActiveUI()
        {
            // Update race progress before showing UI
            if (_aiSimulator != null && _stateMachine.IsInState<RaceState_Active>())
            {
                _aiSimulator.UpdateSimulation();
            }

            if (_activeUI == null)
            {
                _activeUI = UIManager.Instance.ShowUIOnTop<RaceActiveUI>("RaceActiveUI");
                _activeUI.Initialize(this);
                _activeUI.OnPlayButtonClicked += PlayGame;
                _activeUI.OnCloseButtonClicked += HideAllUI;
            }
        }

        public void HideAllUI()
        {
            if (_entryUI != null)
            {
                UIManager.Instance.ReleaseUI(_entryUI, true);
                _entryUI = null;
            }

            if (_activeUI != null)
            {
                UIManager.Instance.ReleaseUI(_activeUI, true);
                _activeUI = null;
            }
        }

        #endregion       
    }
}
