#if USE_APPSFLYER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using AppsFlyerSDK;

namespace Delta.GameOps
{
    public class AppsFlyerAnalyticsAgent : UAAnalyticsAgent
    {
        Dictionary<string, string> _emptyEventValues = new Dictionary<string, string>();
        bool _trackEnabled;
        MonoBehaviour objCallBack = null;
        public AppsFlyerAnalyticsAgent(string iOSAppId, string androidPackageName, string devKey, bool trackEnabled, int durationSessionTimeout, AppsFlyerTrackerCallbacks appsFlyerListener)
        {
            _trackEnabled = trackEnabled;
            objCallBack = appsFlyerListener;
            AppsFlyer.setMinTimeBetweenSessions(durationSessionTimeout);
            AppsFlyer.initSDK(devKey, iOSAppId, appsFlyerListener);
            AppsFlyer.startSDK();
        }

        public override void LogSceneName(string sceneName, string sceneClass) { }
        public override void SetUserProperty(string name, string value) { }
        public override void LogRevenue(string eventName, double _adjustVerifyingPrice, string _adjustVerifyingCurrency, string _adjustVerifyingTransactionId, string productId, string googlePublickKey = "", string unityReceiptPayload = "")
        {
            // IAP Validation
#if UNITY_IOS
            // set for Testflight test
            Debug.Log(">>>NativeHelper.IsTestFlightBuild(): " + NativeHelper.IsTestFlightBuild());
            if (NativeHelper.IsTestFlightBuild())
            {
                AppsFlyer.setUseReceiptValidationSandbox(true);
            }
            AppsFlyer.validateAndSendInAppPurchase(productId,
                _adjustVerifyingPrice.ToString(),
                _adjustVerifyingCurrency,
                _adjustVerifyingTransactionId, null, objCallBack);
#elif UNITY_ANDROID && !UNITY_EDITOR
            var googleReceipt = JsonUtility.FromJson<UnityGooglePlayReceipt>(unityReceiptPayload);
            Debug.Log("[Appsflyer] IAP json: " + googleReceipt.json);
            var googlePurchaseData = JsonUtility.FromJson<GooglePurchaseData>(googleReceipt.json);

            Debug.Log("[Appsflyer] IAP purchaseToken: " + googlePurchaseData.purchaseToken);
            Debug.Log("[Appsflyer] IAP developerPayload: " + googlePurchaseData.developerPayload);
            string developerPayload = googlePurchaseData.developerPayload;
            if (string.IsNullOrEmpty(developerPayload))
                developerPayload = "";

            AppsFlyer.validateAndSendInAppPurchase(googlePublickKey,
                googleReceipt.signature,
                googleReceipt.json,
                _adjustVerifyingPrice.ToString(),
                _adjustVerifyingCurrency, null, objCallBack);
#endif
        }
        public override void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement)
        {
#if USE_ADMOB_MEDIATION
            AppsFlyerAdRevenue.logAdRevenue(networkName, AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeGoogleAdMob, revenue, "USD", null);
#endif
        }

        public override void LogEvent(string eventName)
        {
            if (_trackEnabled)
                AppsFlyer.sendEvent(eventName, _emptyEventValues);
        }

        public override void LogEventWithParameters(string eventName, Dictionary<string, object> parameters)
        {
            if (_trackEnabled)
            {
                var stringParams = new Dictionary<string, string>();
                foreach (var keyPair in parameters)
                    stringParams.Add(keyPair.Key, keyPair.Value.ToString());
                AppsFlyer.sendEvent(eventName, stringParams);
            }
        }

        public override void LogLoyalUser()
        {

        }
        class UnityGooglePlayReceipt
        {
            public string json;
            public string signature;
        }

        [Serializable]
        class GooglePurchaseData
        {
            public string purchaseToken;
            public string developerPayload;
            public string orderId;
        }
    }
}
#endif