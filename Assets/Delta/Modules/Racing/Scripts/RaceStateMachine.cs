using Delta.Core;

namespace Delta.Modules.Racing
{
    public class RaceStateMachine : StateMachine<RaceStateBase>
    {
        private IState _currentState;
        private RaceSystem _raceSystem;

        // State instances
        private RaceState_Locked _lockedState;
        private RaceState_Available _availableState;
        private RaceState_Active _activeState;
        private RaceState_Completed _completedState;
        private RaceState_Cooldown _cooldownState;

        public RaceStateMachine(RaceSystem raceSystem)
        {
            _raceSystem = raceSystem;
            
            // Initialize all states
            _lockedState = new RaceState_Locked(raceSystem);
            _availableState = new RaceState_Available(raceSystem);
            _activeState = new RaceState_Active(raceSystem);
            _completedState = new RaceState_Completed(raceSystem);
            _cooldownState = new RaceState_Cooldown(raceSystem);
        }

        public void Initialize()
        {
            // Determine initial state based on race data
            if (!_raceSystem.RaceData.isUnlocked)
            {
                ChangeState(_lockedState);
            }
            else if (_raceSystem.RaceData.IsOnCooldown())
            {
                ChangeState(_cooldownState);
            }
            else if (_raceSystem.RaceData.isInActiveRace)
            {
                // Resume active race if interrupted
                ChangeState(_activeState);
            }
            else
            {
                ChangeState(_availableState);
            }
        }

        // State accessors
        public RaceState_Locked LockedState => _lockedState;
        public RaceState_Available AvailableState => _availableState;
        public RaceState_Active ActiveState => _activeState;
        public RaceState_Completed CompletedState => _completedState;
        public RaceState_Cooldown CooldownState => _cooldownState;

        public bool IsInState<T>() where T : IState
        {
            return _currentState is T;
        }
    }
}

