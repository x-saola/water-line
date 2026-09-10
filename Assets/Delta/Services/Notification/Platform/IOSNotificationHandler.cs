#if UNITY_IOS
using System.Collections;
using Unity.Notifications.iOS;

namespace DeltaTools.Core.Services.Notification
{
    public class IOSNotificationHandler : INotificationHandler
    {
        public IEnumerator Initialize()
        {
            return RequestAuthorization();
        }

        public void ScheduleNotification(NotificationData data)
        {
            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = new System.TimeSpan(0, 0, (int)data.DelaySeconds),
                Repeats = false
            };

            var notification = new iOSNotification()
            {
                Identifier = data.Id,
                Title = data.Title,
                Body = data.Message,
                ShowInForeground = true,
                ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
                CategoryIdentifier = "default",
                Trigger = timeTrigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
        }

        public void CancelLastNotificationIntent()
        {
            var notification = iOSNotificationCenter.GetLastRespondedNotification();
            if (notification != null)
            {
                iOSNotificationCenter.RemoveDeliveredNotification(notification.Identifier);
            }
        }

        public void CancelAll()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }

        private IEnumerator RequestAuthorization()
        {
            var authorizationOptions = AuthorizationOption.Alert | AuthorizationOption.Badge;
            var request = new AuthorizationRequest(authorizationOptions, true);
            while (!request.IsFinished)
                yield return null;
        }
    }
}
#endif
