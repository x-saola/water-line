using Delta.Core;

namespace Delta.Common
{
    public enum AtomEventType
    {
        AppPaused,
        AppResumed,
        LevelStarted,
        LevelPaused,
        LevelResumed,
        LevelWin,
        LevelLose,
        LevelRevived,
        RetryLevel,
        BackToMainMenu,
    }

    public struct AtomEvent
    {
        
        public AtomEventType EventType;

        public AtomEvent(AtomEventType eventType)
        {
            EventType = eventType;
        }

        static AtomEvent e;
        public static void Trigger(AtomEventType eventType)
        {
            e.EventType = eventType;
            EventBus<AtomEvent>.Raise(e);
        }
    }
}
