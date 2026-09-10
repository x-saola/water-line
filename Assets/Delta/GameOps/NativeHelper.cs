using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using System.Runtime.InteropServices;
using Unity.Advertisement.IosSupport; // need iOS 14 Advertising Support
#endif

namespace Delta.GameOps
{
    public static class NativeHelper
    {
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern string _getCountryCode();

        [DllImport("__Internal")]
        private static extern string _getCountry();

        [DllImport("__Internal")]
        private static extern bool _isMuted();

        [DllImport("__Internal")]
        private static extern bool _isChecking();

        [DllImport("__Internal")]
        private static extern void _checkStatus();

        [DllImport("__Internal")]
        private static extern float _getDeviceNativeScale();

        [DllImport("__Internal")]
        private static extern float _getDeviceScreenSizeHorizontal();

        [DllImport("__Internal")]
        private static extern float _getDeviceScreenSizeVertical();

        [DllImport("__Internal")]
        private static extern bool _initAdClosedObserver();

        [DllImport("__Internal")]
        private static extern bool _isPlayingAd();

        [DllImport("__Internal")]
        private static extern void _startPlayingAd();

        [DllImport("__Internal")]
        private static extern void _stopPlayingAd();

        [DllImport("__Internal")]
        private static extern string _NATIVE_Get_IDFV();

        [DllImport("__Internal")]
        private static extern string _NATIVE_Get_IDFA();
        [DllImport("__Internal")]
        private static extern int _NATIVE_IsTestFlightBuild();
#endif
        public static bool IsTestFlightBuild()
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_IOS
            return _NATIVE_IsTestFlightBuild() > 0;
#elif UNITY_ANDROID
            return false;
#endif
        }
        public static string getIDFA()
        {
#if UNITY_EDITOR
            return "UnityEditor";
#elif UNITY_IOS
            return _NATIVE_Get_IDFA();
#else
            return "";
#endif
        }

        public static string getIDFV()
        {
#if UNITY_EDITOR
            return "UnityEditor";
#elif UNITY_IOS
            return _NATIVE_Get_IDFV();
#else
	    return "";
#endif
        }

        public static void RequestIsLimitedAdTracking(System.Action<bool> callback)
        {
#if UNITY_EDITOR
            callback(false);
#elif UNITY_ANDROID
            AndroidRequestAdvertisingIdentifierAsync.Request((advertisingId, trackingEnabled, error)=> {
                Debug.Log("advertisingId: " + advertisingId + " - trackingEnabled: " + trackingEnabled + " - error: " + error);
                var isLAT = !trackingEnabled || !string.IsNullOrEmpty(error) || string.IsNullOrEmpty(advertisingId);
                callback(isLAT);
            });
#elif UNITY_IOS
            var isLAT = ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
            callback(isLAT);
#endif
        }

        public static string GetCountryCode()
        {
#if UNITY_EDITOR
            return "VN";
#elif UNITY_IOS
            return _getCountryCode();
#elif UNITY_ANDROID
            var countryCode = "US";
            using (AndroidJavaClass cls = new AndroidJavaClass("java.util.Locale"))
            {
                using (AndroidJavaObject locale = cls.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    countryCode = locale.Call<string>("getCountry");
                }
            }
            return countryCode;
#endif
        }

        public static string GetCountry()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _getCountry();
#else
            var code = GetCountryCode();
            var region = new System.Globalization.RegionInfo(code);
            return region.EnglishName;
#endif
        }

        #region  Sound Switch
        public static bool IsMuted()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _isMuted();
#else
            return false;
#endif
        }

        public static bool IsCheckingSoundSwitch()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _isChecking();
#else
            return false;
#endif
        }

        public static void CheckSoundSwitchStatus()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _checkStatus();
#endif
        }
        #endregion

        #region Ad Utils
        public static float GetDeviceNativeScale()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _getDeviceNativeScale();
#elif UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity =
                    playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject resources = activity.Call<AndroidJavaObject>("getResources");
            AndroidJavaObject metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics");
            return metrics.Get<float>("density");
#else
            return 1f;
#endif
        }

        public static float GetDeviceScreenSizeHorizontal()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _getDeviceScreenSizeHorizontal();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return Screen.width;
#else
            return Screen.width;
#endif
        }

        public static float GetDeviceScreenSizeVertical()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _getDeviceScreenSizeVertical();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return Screen.height;
#else
            return Screen.height;
#endif
        }

        public static void InitAdClosedObserver()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _initAdClosedObserver();
#endif
        }

        public static void StartPlayingFullscreenAd()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _startPlayingAd();
#endif
        }

        public static void FullScreenAdClosed()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _stopPlayingAd();
#endif
        }

        public static bool IsFullScreenAdNativeClosed()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return !_isPlayingAd();
#else
            return false;
#endif
        }
        #endregion

#if UNITY_ANDROID
        public static class AndroidRequestAdvertisingIdentifierAsync
        {
            static AndroidJavaObject s_currentActivity;
            static AndroidJavaClass s_client;

            public static void Request(UnityEngine.Application.AdvertisingIdentifierCallback callback)
            {
                if (s_currentActivity == null)
                {
                    AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    s_currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
                }

                if (s_client == null)
                    s_client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");

                string error = null;
                var advertisingID = string.Empty;
                var limitAdvertising = false;
                AndroidJavaObject adInfo = s_client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", s_currentActivity);
                try
                {
                    advertisingID = adInfo.Call<string>("getId").ToString();
                    limitAdvertising = (adInfo.Call<bool>("isLimitAdTrackingEnabled"));
                }
                catch (System.Exception ex)
                {
                    error = ex.Message;
                }

                callback.Invoke(advertisingID, !limitAdvertising, error);
            }
        }
#endif
    }
}
