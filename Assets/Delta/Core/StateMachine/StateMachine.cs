namespace Delta.Core
{
    public abstract class StateMachine<TState> : IStateMachine where TState : IState
    {
        public TState CurrentState { get; private set; }

        public void ChangeState(TState newState)
        {
            if (newState == null || newState.Equals(CurrentState))
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter(this);
        }

        public void Update() => CurrentState?.Update();
    }
}
