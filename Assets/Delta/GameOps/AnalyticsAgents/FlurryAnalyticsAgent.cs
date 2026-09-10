#if ENABLE_FLURRY
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FlurrySDK;

namespace Delta.GameOps
{
    public class FlurryAnalyticsAgent : IAnalyticsAgent
    {
        const string EVENT_CLICK_BANNER_AD = "ClickBannerAds";
        const string EVENT_CLICK_INTERSTITIAL_AD = "ClickInterstitialAds";
        const string EVENT_COMPLETE_REWARDED_AD = "CompleteRewardedAds";

        public bool IsUAAgent { get { return false; } }

        bool _flurryDisabled;

        public FlurryAnalyticsAgent(string apiKey, int durationSessionTimeout, bool disabled = false)
        {
            _flurryDisabled = disabled;
            if (_flurryDisabled)
                return;

            new Flurry.Builder().WithContinueSessionMillis(durationSessionTimeout * 1000).WithCrashReporting().WithLogEnabled()
            .WithLogLevel()
            .Build(apiKey);
        }

        public void LogSceneName(string sceneName, string sceneClass) { }
        public void SetUserProperty(string name, string value) { }
        public void LogRevenue(string eventName, double _adjustVerifyingPrice, string _adjustVerifyingCurrency, string _adjustVerifyingTransactionId, string productId, string googlePublickKey = "", string unityReceiptPayload = ""){}
        public void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement) { }
        public void LogEvent(string eventName)
        {
            if (_flurryDisabled)
                return;

            Flurry.LogEvent(eventName);
        }

        public void LogEventWithParameters(string eventName, Dictionary<string, object> parameters)
        {
            if (_flurryDisabled)
                return;

            var stringParams = new Dictionary<string, string>();
            foreach (var keyPair in parameters)
                stringParams.Add(keyPair.Key, keyPair.Value == null ? string.Empty : keyPair.Value.ToString());
            Flurry.LogEvent(eventName, stringParams);
        }

        public void LogLoyalUser()
        {

        }

        public void LogShowBannerAd()
        {

        }

        public void LogShowInterstitialAd()
        {

        }

        public void LogBannerAdClicked(string adUnitId, string adNetwork)
        {
            LogEventWithParameters(EVENT_CLICK_BANNER_AD, new Dictionary<string, object> {
                { "ad_network", adNetwork },
                { "admob_ad_unit", adUnitId }
            });
        }

        public void LogInterstitialAdClicked(string adUnitId, string adNetwork)
        {
            LogEventWithParameters(EVENT_CLICK_INTERSTITIAL_AD, new Dictionary<string, object> {
                { "ad_network", adNetwork },
                { "admob_ad_unit", adUnitId }
            });
        }

        public void LogCompleteRewardedAd(string adUnitId, string adNetwork)
        {
            LogEventWithParameters(EVENT_COMPLETE_REWARDED_AD, new Dictionary<string, object> {
                { "ad_network", adNetwork },
                { "admob_ad_unit", adUnitId }
            });
        }
    }
}
#endif
