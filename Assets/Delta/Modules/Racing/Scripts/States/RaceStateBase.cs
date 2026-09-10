using Delta.Core;

namespace Delta.Modules.Racing
{
    public abstract class RaceStateBase : IState
    {
        protected RaceSystem _raceSystem;

        public RaceStateBase(RaceSystem raceSystem)
        {
            _raceSystem = raceSystem;
        }

        public virtual void Enter(IStateMachine machine)
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Update()
        {
        }
    }
}

