using Delta.Core;

namespace Delta.Common
{
    public enum ResourceChangeEventType
    {
        Add,
        Spend,
    }

    public struct ResourceChangedEvent
    {
        public ResourceChangeEventType EventType;
        public string ResourceId;
        public int Quantity;
        public string From;
        public string To;
        public string LevelId;
        public int Balance;

        public ResourceChangedEvent(ResourceChangeEventType eventType, string resourceId, int quantity, string from, string to, string levelId, int balance)
        {
            EventType = eventType;
            ResourceId = resourceId;
            Quantity = quantity;
            From = from;
            To = to;
            LevelId = levelId;
            Balance = balance;
        }

        static ResourceChangedEvent e;
        public static void Trigger(ResourceChangeEventType eventType, string resourceId, int quantity, string from, string to, string levelId, int balance)
        {
            e.EventType = eventType;
            e.ResourceId = resourceId;
            e.Quantity = quantity;
            e.From = from;
            e.To = to;
            e.LevelId = levelId;
            e.Balance = balance;
            EventBus<ResourceChangedEvent>.Raise(e);
        }
    }
}
