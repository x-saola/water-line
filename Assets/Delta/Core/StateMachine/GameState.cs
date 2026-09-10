namespace Delta.Core
{
    public abstract class GameState : IState
    {
        protected IGameStateMachine Machine { get; private set; }

        public void Enter(IStateMachine machine)
        {
            Machine = (IGameStateMachine)machine;
            OnEnter();
        }

        public void Exit() => OnExit();
        public void Update() => OnUpdate();

        protected virtual void OnEnter() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnExit() { }
    }
}
