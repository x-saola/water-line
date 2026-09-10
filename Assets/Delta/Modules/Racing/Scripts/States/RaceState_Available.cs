using Delta.Core;
using UnityEngine;

namespace Delta.Modules.Racing
{
    public class RaceState_Available : RaceStateBase
    {
        public RaceState_Available(RaceSystem raceSystem) : base(raceSystem)
        {
        }

        public override void Enter(IStateMachine machine)
        {
            Debug.Log("[RaceSystem] State: Available - Ready to start a race");
            
            // Check and reset daily limits if needed
            _raceSystem.RaceData.CheckAndResetDaily();
        }

        public override void Update()
        {
            // Check if cooldown has expired (in case we were on cooldown)
            if (_raceSystem.RaceData.IsOnCooldown())
            {
                // Still on cooldown, shouldn't be in Available state
                _raceSystem.TransitionToCooldown();
                return;
            }

            // Check if event phase has changed to Pause
            var eventPhase = _raceSystem.GetCurrentEventPhase();
            if (eventPhase == RaceData.EventPhase.Pause)
            {
                // Event has entered pause phase, should lock the feature temporarily
                Debug.Log("[RaceSystem] Event entered pause phase, locking feature");
                _raceSystem.TransitionToCooldown(); // Use cooldown state to represent pause
            }
        }
    }
}
