# if UNITY_ANDROID && ENABLE_NOTIFICATION
using System.Collections;
using Unity.Notifications.Android;
using UnityEngine;
using UnityEngine.Android;

namespace DeltaTools.Core.Services.Notification
{
    public class AndroidNotificationHandler : INotificationHandler
    {
        public IEnumerator Initialize()
        {
            // RequestNotificationPermission();

            var request = new PermissionRequest();
            while (request.Status == PermissionStatus.RequestPending)
                yield return null;

            var channel = new AndroidNotificationChannel()
            {
                Id = "default_channel",
                Name = "Default Channel",
                Importance = Importance.Default,
                Description = "Generic notifications",
                EnableVibration = true,
                VibrationPattern = new long[] { 0, 1000, 500, 1000 },
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

        public void ScheduleNotification(NotificationData data)
        {
            var notification = new AndroidNotification()
            {
                Title = data.Title,
                Text = data.Message,
                SmallIcon = "icon_0",
                LargeIcon = "icon_1", 
                FireTime = System.DateTime.Now.AddSeconds(data.DelaySeconds),
            };

            AndroidNotificationCenter.SendNotification(notification, "default_channel");
        }

        public void CancelLastNotificationIntent()
        {
            var data = AndroidNotificationCenter.GetLastNotificationIntent();
            if (data != null)
            {
                AndroidNotificationCenter.CancelNotification(data.Id);
            }
        }

        public void CancelAll()
        {
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            AndroidNotificationCenter.CancelAllScheduledNotifications();
        }

        private void RequestNotificationPermission()
        {
            if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
        }
    }
}
#endif
