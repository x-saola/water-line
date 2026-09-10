namespace Delta.Services
{
    [System.Serializable]
    public class NotificationData
    {
        public string Title;
        public string Message;
        public double DelaySeconds;
        public string Id;
        public string ChannelId; // Android only
    }
}
