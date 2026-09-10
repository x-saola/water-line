using Delta.Core;
using UnityEngine;

namespace Delta.Modules.Racing
{
    public class RaceState_Active : RaceStateBase
    {
        public RaceState_Active(RaceSystem raceSystem) : base(raceSystem)
        {
        }

        public override void Enter(IStateMachine machine)
        {
            Debug.Log("[RaceSystem] State: Active - Race in progress");
            
            // Check if we are starting a new race or resuming
            if (!_raceSystem.RaceData.isInActiveRace)
            {
                // New race
                _raceSystem.RaceData.isInActiveRace = true;
                _raceSystem.RaceData.playerProgress = 0;
                _raceSystem.SaveData(); // Save state immediately
            }
            else
            {
                // Resuming race - restore AI state
                if (_raceSystem.RaceData.activeOpponents != null && _raceSystem.RaceData.activeOpponents.Count > 0)
                {
                    _raceSystem.AISimulator.LoadState(_raceSystem.RaceData.activeOpponents, _raceSystem.Config);
                }
                else
                {
                    // Fallback if no saved opponents (shouldn't happen with new saves)
                    Debug.LogWarning("[RaceState_Active] Resuming race but no saved opponents found. Re-initializing.");
                    _raceSystem.AISimulator.Initialize(_raceSystem.Config, _raceSystem.Config.numberOfOpponents);
                }
            }
            
            // Start AI simulation
            _raceSystem.StartAISimulation();
            
            // Notify listeners
            _raceSystem.TriggerRaceStartedEvent();
        }

        public override void Exit()
        {
            Debug.Log("[RaceSystem] State: Active - Exiting race");
            
            // Stop AI simulation
            _raceSystem.StopAISimulation();
            
            // Clear race state
            _raceSystem.RaceData.isInActiveRace = false;
            _raceSystem.SaveData();
        }

        public override void Update()
        {
            // Check for player finish
            if (_raceSystem.RaceData.playerProgress >= _raceSystem.Config.objectivesRequired)
            {
                int finishedOpponents = _raceSystem.AISimulator.GetFinishedOpponentCount();
                int playerRank = finishedOpponents + 1;
                
                Debug.Log($"[RaceSystem] Player finished race! Rank: {playerRank}");
                _raceSystem.OnPlayerFinished(playerRank);
                return;
            }

            // Check if all prize spots are taken (3 opponents finished)
            int finishedCount = _raceSystem.AISimulator.GetFinishedOpponentCount();
            if (finishedCount >= 3)
            {
                Debug.Log("[RaceSystem] All prize spots taken by AI!");
                // Player lost (rank 0 or >3)
                _raceSystem.OnPlayerFinished(4);
                return;
            }
        }

    }
}
