using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.GameOps
{
    public abstract class UAAnalyticsAgent : IAnalyticsAgent
    {
        public const string EVENT_SHOW_AD = "show_ad";
        public const string EVENT_SHOW_BANNER_AD = "show_banner";
        public const string EVENT_SHOW_INTERSTITIAL_AD = "show_interstitial";
        public const string EVENT_CLICK_AD = "click_ad";
        public const string EVENT_CLICK_BANNER_AD = "click_banner_ad";
        public const string EVENT_CLICK_INTERSTITIAL_AD = "click_interstitial_ad";
        public const string EVENT_COMPLETE_REWARDED_ADS = "complete_rewarded_ads";
        public const string EVENT_LOYAL_USERS = "loyal_users";

        public bool IsUAAgent { get { return true; } }

        public abstract void LogSceneName(string sceneName, string sceneClass);
        public abstract void SetUserProperty(string name, string value);
        public abstract void LogEvent(string eventName);
        public abstract void LogEventWithParameters(string eventName, Dictionary<string, object> parameters);
        public abstract void LogRevenue(string eventName, double value, string currency, string transactionId, string productId, string googlePublickKey = "", string unityReceiptPayload = "");
        public abstract void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement);

        public virtual void LogLoyalUser()
        {
            LogEvent(EVENT_LOYAL_USERS);
        }

        public void LogShowBannerAd()
        {
            LogEvent(EVENT_SHOW_AD);
            LogEvent(EVENT_SHOW_BANNER_AD);
        }

        public void LogShowInterstitialAd()
        {
            LogEvent(EVENT_SHOW_AD);
            LogEvent(EVENT_SHOW_INTERSTITIAL_AD);
        }

        public void LogBannerAdClicked(string adUnitId, string adNetwork)
        {
            LogEvent(EVENT_CLICK_AD);
            LogEvent(EVENT_CLICK_BANNER_AD);
        }

        public void LogInterstitialAdClicked(string adUnitId, string adNetwork)
        {
            LogEvent(EVENT_CLICK_AD);
            LogEvent(EVENT_CLICK_INTERSTITIAL_AD);
        }

        public void LogCompleteRewardedAd(string adUnitId, string adNetwork)
        {
            LogEvent(EVENT_COMPLETE_REWARDED_ADS);
        }
    }
}
