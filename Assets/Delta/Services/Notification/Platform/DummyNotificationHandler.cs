using System.Collections;
using UnityEngine;

namespace Delta.Services
{
    public class DummyNotificationHandler : INotificationHandler
    {
        public IEnumerator Initialize()
        {
            yield return null; // Simulate initialization delay
            Debug.Log("Dummy Notification Handler Initialized");
        }

        public void ScheduleNotification(NotificationData data)
        {
            Debug.Log($"Dummy Notification Scheduled: {data.Title} - {data.Message}");
        }

        public void CancelLastNotificationIntent()
        {
            Debug.Log("Dummy Notification Cancelled");
        }

        public void CancelAll()
        {
            Debug.Log("All Dummy Notifications Cancelled");
        }
    }
}
