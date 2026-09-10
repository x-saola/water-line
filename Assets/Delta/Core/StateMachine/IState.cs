namespace Delta.Core
{
    public interface IState
    {
        void Enter(IStateMachine machine);
        void Exit();
        void Update();
    }
}
