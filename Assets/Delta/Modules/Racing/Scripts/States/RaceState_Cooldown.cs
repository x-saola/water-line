using Delta.Core;
using UnityEngine;

namespace Delta.Modules.Racing
{
    public class RaceState_Cooldown : RaceStateBase
    {
        public RaceState_Cooldown(RaceSystem raceSystem) : base(raceSystem)
        {
        }

        public override void Enter(IStateMachine machine)
        {
            Debug.Log("[RaceSystem] State: Cooldown - Waiting for next race");
        }

        public override void Exit()
        {
        }

        public override void Update()
        {
            // Check if cooldown has expired
            if (!_raceSystem.RaceData.IsOnCooldown())
            {
                // Check if event is in active phase before transitioning
                var eventPhase = _raceSystem.GetCurrentEventPhase();
                if (eventPhase == RaceData.EventPhase.Active)
                {
                    // Check and handle new cycle transition before transitioning to Available
                    _raceSystem.CheckAndHandleNewCycle();
                    
                    Debug.Log("[RaceSystem] Cooldown expired and event is active, transitioning to Available");
                    _raceSystem.TransitionToAvailable();
                }
                else if (eventPhase == RaceData.EventPhase.Pause)
                {
                    // Event is on pause, stay in cooldown
                    Debug.Log("[RaceSystem] Cooldown expired but event is on pause, staying in Cooldown");
                }
            }
            else
            {
                // Still on individual race cooldown, check if event phase changed
                var eventPhase = _raceSystem.GetCurrentEventPhase();
                if (eventPhase == RaceData.EventPhase.Active)
                {
                    // Event is active, normal cooldown behavior
                }
                else if (eventPhase == RaceData.EventPhase.Pause)
                {
                    // Event entered pause, this is correct state
                }
            }
        }
    }
}
