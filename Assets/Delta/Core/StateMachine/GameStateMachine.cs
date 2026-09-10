using System;

namespace Delta.Core
{
    public class GameStateMachine : StateMachine<GameState>, IGameStateMachine
    {
        public void ChangeState<T>() where T : GameState => ChangeState(Activator.CreateInstance<T>());
    }
}
