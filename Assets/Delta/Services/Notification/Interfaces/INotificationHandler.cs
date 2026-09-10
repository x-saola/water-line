using System.Collections;

namespace Delta.Services
{
    public interface INotificationHandler
    {
        IEnumerator Initialize();
        void ScheduleNotification(NotificationData data);
        void CancelLastNotificationIntent();
        void CancelAll();
    }
}
