using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.Modules.Racing
{
    public class AIOpponentSimulator
    {
        private RaceSystem _raceSystem;
        private RaceConfig _config;
        private List<OpponentProfile> _opponents;
        private Coroutine _simulationCoroutine;
        private MonoBehaviour _coroutineRunner;

        public List<OpponentProfile> Opponents => _opponents;

        public event System.Action<OpponentProfile> OnOpponentProgress;
        public event System.Action<OpponentProfile> OnOpponentWin;

        public AIOpponentSimulator(RaceSystem raceSystem, MonoBehaviour coroutineRunner)
        {
            _raceSystem = raceSystem;
            _coroutineRunner = coroutineRunner;
            _opponents = new List<OpponentProfile>();
        }

        public void Initialize(RaceConfig config, int numberOfOpponents)
        {
            _config = config;
            _opponents.Clear();

            // Generate AI opponents
            for (int i = 0; i < numberOfOpponents; i++)
            {
                string name = $"Player{Random.Range(1000, 10000)}";
                int avatarIndex = Random.Range(0, config.avatarCount);
                var opponent = new OpponentProfile(name, config.objectivesRequired, avatarIndex);
                _opponents.Add(opponent);
            }

            // Save to RaceData
            _raceSystem.RaceData.activeOpponents = new List<OpponentProfile>(_opponents);
            _raceSystem.RaceData.lastSimulationTimestamp = System.DateTime.UtcNow.Ticks;
            _raceSystem.SaveData();

            Debug.Log($"[AIOpponentSimulator] Initialized {numberOfOpponents} opponents");
        }

        public void LoadState(List<OpponentProfile> savedOpponents, RaceConfig config)
        {
            _config = config;
            _opponents = new List<OpponentProfile>(savedOpponents);
            
            // Handle offline progression
            long lastTime = _raceSystem.RaceData.lastSimulationTimestamp;
            if (lastTime > 0)
            {
                long currentTime = System.DateTime.UtcNow.Ticks;
                long elapsedTicks = currentTime - lastTime;
                
                if (elapsedTicks > 0)
                {
                    float elapsedSeconds = (float)System.TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
                    SimulateOfflineProgress(elapsedSeconds);
                }
            }
            
            Debug.Log($"[AIOpponentSimulator] Loaded {savedOpponents.Count} opponents");
        }

        private void SimulateOfflineProgress(float elapsedSeconds)
        {
            if (_config == null || _opponents == null || _opponents.Count == 0)
                return;

            float avgInterval = (_config.aiProgressIntervalMin + _config.aiProgressIntervalMax) / 2f;
            if (avgInterval <= 0) avgInterval = 1f;

            int steps = Mathf.FloorToInt(elapsedSeconds / avgInterval);
            Debug.Log($"[AIOpponentSimulator] Simulating offline progress: {elapsedSeconds:F1}s elapsed, {steps} steps");

            for (int i = 0; i < steps; i++)
            {
                // Check if race is already over (3 winners)
                if (GetFinishedOpponentCount() >= 3)
                    break;

                ProgressRandomOpponents(false); // Don't trigger events/save for every step
            }
            
            // Update timestamp and save once
            _raceSystem.RaceData.lastSimulationTimestamp = System.DateTime.UtcNow.Ticks;
            _raceSystem.SaveData();
        }

        public int GetFinishedOpponentCount()
        {
            int count = 0;
            foreach (var opponent in _opponents)
            {
                if (opponent.HasWon())
                {
                    count++;
                }
            }
            return count;
        }

        public void StartSimulation()
        {
            // Simulation now runs manually when UI is opened, not continuously
            Debug.Log("[AIOpponentSimulator] Simulation ready (manual update mode)");
        }

        public void StopSimulation()
        {
            // No continuous simulation to stop
            Debug.Log("[AIOpponentSimulator] Simulation stopped");
        }

        /// <summary>
        /// Manually updates simulation by calculating elapsed time and simulating progress.
        /// Called when RaceActiveUI is opened.
        /// </summary>
        public void UpdateSimulation()
        {
            long lastTime = _raceSystem.RaceData.lastSimulationTimestamp;
            if (lastTime > 0)
            {
                long currentTime = System.DateTime.UtcNow.Ticks;
                long elapsedTicks = currentTime - lastTime;
                
                if (elapsedTicks > 0)
                {
                    float elapsedSeconds = (float)System.TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
                    Debug.Log($"[AIOpponentSimulator] Updating simulation: {elapsedSeconds:F1}s elapsed");
                    SimulateOfflineProgress(elapsedSeconds);
                }
            }
        }

        private IEnumerator SimulationLoop()
        {
            while (true)
            {
                // Wait for random interval based on config
                float waitTime = _config.GetRandomAIInterval();
                yield return new WaitForSeconds(waitTime);

                // Progress one or more AI opponents
                ProgressRandomOpponents();
            }
        }

        private void ProgressRandomOpponents(bool triggerEvents = true)
        {
            if (_opponents == null || _opponents.Count == 0)
                return;
            
            // Update timestamp
            if (triggerEvents)
            {
                _raceSystem.RaceData.lastSimulationTimestamp = System.DateTime.UtcNow.Ticks;
            }

            // Get list of opponents who haven't won yet
            List<OpponentProfile> activeOpponents = new List<OpponentProfile>();
            foreach (var opponent in _opponents)
            {
                if (!opponent.HasWon())
                {
                    activeOpponents.Add(opponent);
                }
            }

            if (activeOpponents.Count == 0)
                return;

            // Always progress at least one opponent
            int randomIndex = Random.Range(0, activeOpponents.Count);
            ProgressOpponent(activeOpponents[randomIndex], triggerEvents);

            // Chance to progress additional opponents
            if (Random.value < _config.multipleAIProgressChance && activeOpponents.Count > 1)
            {
                // Progress a second random opponent
                int secondIndex = Random.Range(0, activeOpponents.Count);
                if (secondIndex != randomIndex)
                {
                    ProgressOpponent(activeOpponents[secondIndex], triggerEvents);
                }
            }
        }

        private void ProgressOpponent(OpponentProfile opponent, bool triggerEvents)
        {
            opponent.IncrementProgress();
            
            // Check if opponent won and needs a rank assigned
            if (opponent.HasWon() && opponent.rank == 0)
            {
                // Assign rank based on how many opponents already have ranks
                int rankedCount = 0;
                foreach (var opp in _opponents)
                {
                    if (opp.rank > 0)
                    {
                        rankedCount++;
                    }
                }
                opponent.rank = rankedCount + 1;  // Next available rank
                
                Debug.Log($"[AIOpponentSimulator] {opponent.name} has won the race! Rank: {opponent.rank}");
                
                if (triggerEvents)
                {
                    OnOpponentWin?.Invoke(opponent);
                }
            }
            
            if (triggerEvents)
            {
                Debug.Log($"[AIOpponentSimulator] {opponent.name} progressed to {opponent.progress}/{opponent.targetObjectives}");
                OnOpponentProgress?.Invoke(opponent);
            }
        }

        public bool HasAnyOpponentWon()
        {
            foreach (var opponent in _opponents)
            {
                if (opponent.HasWon())
                {
                    return true;
                }
            }
            return false;
        }

        public OpponentProfile GetWinningOpponent()
        {
            foreach (var opponent in _opponents)
            {
                if (opponent.HasWon())
                {
                    return opponent;
                }
            }
            return null;
        }

        public void ResetAllOpponents()
        {
            foreach (var opponent in _opponents)
            {
                opponent.ResetProgress();
            }
        }

        public List<OpponentProfile> GetOpponentsSortedByProgress()
        {
            var sorted = new List<OpponentProfile>(_opponents);
            sorted.Sort((a, b) => b.progress.CompareTo(a.progress));
            return sorted;
        }
    }
}
