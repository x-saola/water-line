using System.Collections;
using System.Collections.Generic;

namespace Delta.Services
{
    /// <summary>
    /// Simple notification manager that handles scheduling and cancelling notifications.
    /// </summary>
    public class NotificationManager
    {
        private static INotificationHandler _notificationHandler;

        private Dictionary<string, NotificationData> _scheduledNotifications = new();

        private IEnumerator InitializeHandler()
        {
#if UNITY_ANDROID && ENABLE_NOTIFICATION
            _notificationHandler = new AndroidNotificationHandler();
#elif UNITY_IOS
            _notificationHandler = new IOSNotificationHandler();
#else
            _notificationHandler = new DummyNotificationHandler();
#endif
            yield return _notificationHandler.Initialize();
            _notificationHandler.CancelAll();
        }

        public void ScheduleNotification(NotificationData data)
        {
            _notificationHandler?.ScheduleNotification(data);
        }

        public void RegisterNotification(string notifiCationId, NotificationData data)
        {
            if (_scheduledNotifications.ContainsKey(notifiCationId))
            {
                _scheduledNotifications[notifiCationId] = data;
            }
            else
            {
                _scheduledNotifications.Add(notifiCationId, data);
            }
        }

        public void UnregisterNotification(string notifiCationId)
        {
            if (_scheduledNotifications.ContainsKey(notifiCationId))
            {
                _scheduledNotifications.Remove(notifiCationId);
            }
        }

        public void CancelAll()
        {
            _notificationHandler?.CancelAll();
        }
    }
}
