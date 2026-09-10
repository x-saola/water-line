using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace ATT
{
    public class AppTrackingTransparency
    {
#if UNITY_IOS && !UNITY_EDITOR
        public enum AuthorizationStatus
        {
            Authorized = 0,
            Denied = 1,
            NotDetermined = 2,
            Restricted = 3,
        }

        public delegate void RequestResultCallbackType(int result);

        [DllImport("__Internal")]
        private static extern void _NATIVE_RequestTrackingAuthorization(RequestResultCallbackType func);

        static System.Action<AuthorizationStatus> OnRequestResult;

        [AOT.MonoPInvokeCallback(typeof(RequestResultCallbackType))]
        static void RequestResult(int result)
        {
            Debug.Log("[AppTrackingTransparency] result=" + (AuthorizationStatus)result);
            isRequesting = false;
            OnRequestResult?.Invoke((AuthorizationStatus)result);
        }

        static bool isRequesting = false;

        public static void RequestTrackingAuthorization(System.Action<AuthorizationStatus> callback)
        {
            if (isRequesting)
            {
                Debug.LogError("[AppTrackingTransparency] is requesting... Do not call many times");
                return;
            }
            isRequesting = true;
            OnRequestResult = callback;
            _NATIVE_RequestTrackingAuthorization(RequestResult);
        }
#endif

    }

}
