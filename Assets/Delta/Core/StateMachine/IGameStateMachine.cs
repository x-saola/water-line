namespace Delta.Core
{
    public interface IGameStateMachine : IStateMachine
    {
        GameState CurrentState { get; }
        void ChangeState<T>() where T : GameState;
    }
}
