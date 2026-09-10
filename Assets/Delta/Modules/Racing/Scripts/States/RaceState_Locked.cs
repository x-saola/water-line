using Delta.Core;
using UnityEngine;

namespace Delta.Modules.Racing
{
    public class RaceState_Locked : RaceStateBase
    {
        public RaceState_Locked(RaceSystem raceSystem) : base(raceSystem)
        {
        }

        public override void Enter(IStateMachine machine)
        {
            Debug.Log("[RaceSystem] State: Locked - Feature not yet unlocked");
        }

        public override void Update()
        {
            // Check if unlock conditions are met
            if (_raceSystem.CheckUnlockConditions())
            {
                _raceSystem.UnlockFeature();
            }
        }
    }
}

