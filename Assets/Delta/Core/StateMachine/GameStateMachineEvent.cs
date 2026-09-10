namespace Delta.Core
{
    public enum GameStateMachineEventType
    {
        MainMenu,
        Play,
    }

    public struct GameStateMachineEvent
    {
        public GameStateMachineEventType EventType;

        public GameStateMachineEvent(GameStateMachineEventType eventType)
        {
            EventType = eventType;
        }

        static GameStateMachineEvent e;
        public static void Trigger(GameStateMachineEventType eventType)
        {
            e.EventType = eventType;
            EventBus<GameStateMachineEvent>.Raise(e);
        }
    }
}
