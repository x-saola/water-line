using UnityEngine;
#if ENABLE_FIREBASE
using Firebase.Extensions;
#endif

namespace Delta.GameOps
{
#if UNITY_ANDROID
    [System.Serializable]
    public class AndroidUnityReceipt
    {
        public string Payload;
    }

    [System.Serializable]
    public class AndroidPayload
    {
        public string json;
    }

    [System.Serializable]
    public class AndroidReceipt
    {
        public string orderId;
    }
#endif

    public static class AthenaGameOpsUtils
    {
#if UNITY_IOS
        const int BANNER_AD_HEIGHT_IPHONE = 50;
        const int BANNER_AD_HEIGHT_IPAD = 90;
#elif UNITY_ANDROID
        const int BANNER_AD_HEIGHT_ANDROID_LARGE = 90;
        const int BANNER_AD_HEIGHT_ANDROID_MEDIUM = 50;
        const int BANNER_AD_HEIGHT_ANDROID_SMALL = 32;
#endif

        public static void InitFirebase(System.Action callback)
        {
#if ENABLE_FIREBASE
            // Firebase
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == Firebase.DependencyStatus.Available)
                {
                    Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Warning;
                    Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                    Debug.Log("[AthenaApp] Firebase is ready!");
                    callback?.Invoke();
                }
            });
#else
            Debug.Log("[AthenaApp] Firebase fake is ready!");
            callback?.Invoke();
#endif
        }

        public static void SetFirebaseUserId(string userId)
        {
#if ENABLE_FIREBASE
            Firebase.Analytics.FirebaseAnalytics.SetUserId(userId);
            Firebase.Crashlytics.Crashlytics.SetUserId(userId);
#endif
        }

        public static void RunOnMainThread(System.Action action)
        {
#if UNITY_ANDROID && ENABLE_FIREBASE
            System.Threading.Tasks.Task.Run(() => { }).ContinueWithOnMainThread(task =>
            {
                action();
            });
#else
            action();
#endif
        }

        public static int DefaultBannerHeight(bool isIPad)
        {
#if UNITY_IOS
            return isIPad ? BANNER_AD_HEIGHT_IPAD : BANNER_AD_HEIGHT_IPHONE;
#elif UNITY_ANDROID
            var scale = NativeHelper.GetDeviceNativeScale();
            var dp = Mathf.CeilToInt(Screen.height / scale);
            if (dp >= 720)
            {
                float screenWidth = Screen.width / Screen.dpi;
                float screenHeight = Screen.height / Screen.dpi;
                float size = Mathf.Sqrt(Mathf.Pow(screenWidth, 2) + Mathf.Pow(screenHeight, 2));
                if (size >= 6.5f)
                    return BANNER_AD_HEIGHT_ANDROID_LARGE;

                return BANNER_AD_HEIGHT_ANDROID_MEDIUM;
            }
            else if (dp >= 400)
                return BANNER_AD_HEIGHT_ANDROID_MEDIUM;
            else
                return BANNER_AD_HEIGHT_ANDROID_SMALL;
#else
            return 0;
#endif
        }

        public static string GetIAPProductTransactionId(string unityReceipt, string unityTransactionId)
        {
            var transactionId = unityTransactionId;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var receipt = JsonUtility.FromJson<AndroidUnityReceipt>(unityReceipt);
                var payload = JsonUtility.FromJson<AndroidPayload>(receipt.Payload);
                var androidReceipt = JsonUtility.FromJson<AndroidReceipt>(payload.json);
                transactionId = androidReceipt.orderId;
            }
            catch (System.Exception) {}
#endif
            return transactionId;
        }
    }
}
