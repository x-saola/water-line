using Delta.Core;
using UnityEngine;

namespace Delta.Modules.Racing
{
    public class RaceState_Completed : RaceStateBase
    {
        private bool _isVictory;
        private int _rank;

        public RaceState_Completed(RaceSystem raceSystem) : base(raceSystem)
        {
        }

        public void SetResult(bool isVictory, int rank)
        {
            _isVictory = isVictory;
            _rank = rank;
        }

        public override void Enter(IStateMachine machine)
        {
            Debug.Log($"[RaceSystem] State: Completed - Victory: {_isVictory}, Rank: {_rank}");
            
            // Update statistics
            _raceSystem.UpdateStatistics();

            // Set cooldown
            var cooldown = _raceSystem.Config.cooldownSeconds;
            _raceSystem.RaceData.SetCooldown(cooldown);

            // Save data
            _raceSystem.SaveData();

            // Notify listeners
            if (_isVictory)
            {
                _raceSystem.TriggerRaceWonEvent();
            }
            else
            {
                _raceSystem.TriggerRaceLostEvent();
            }
        }

        public override void Exit()
        {
            _rank = 0;
        }

        public bool IsVictory()
        {
            return _isVictory;
        }

        public int GetRank()
        {
            return _rank;
        }
    }
}
