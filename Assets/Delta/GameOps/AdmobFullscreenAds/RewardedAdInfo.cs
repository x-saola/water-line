#if USE_ADMOB_MEDIATION
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;

namespace Delta.GameOps
{
    public interface RewardedAdListener
    {
        void OnRewardedAdLoaded(RewardedAdInfo adInfo);
        void OnRewardedAdFailedToLoad(RewardedAdInfo adInfo, LoadAdError args);
        void OnRewardedAdOpening(RewardedAdInfo adInfo);
        void OnRewardedAdFailedToShow(RewardedAdInfo adInfo, AdError args);
        void OnRewardedAdPaid(RewardedAdInfo adInfo, AdValue args);
        void OnRewardedAdClosed(RewardedAdInfo adInfo);
        void OnRewardedAdClicked(RewardedAdInfo adInfo);
    }

    public class RewardedAdInfo : FullscreenAdInfo
    {
        public RewardedAdListener Listener;
        public RewardedAd RewardedAd;

        public System.Action<string, string, string, double> OnRewardEarned;

        protected override void OnLoad(string adUnitId, AdRequest request, bool isMuted)
        {
            if (RewardedAd != null)
            {
                RewardedAd.OnAdFullScreenContentOpened -= OnFullscreenAdOpening;
                RewardedAd.OnAdFullScreenContentFailed -= OnFullscreenAdFailedToShow;
                RewardedAd.OnAdFullScreenContentClosed -= OnFullscreenAdClosed;
                RewardedAd.OnAdPaid -= OnFullscreenAdPaid;
                RewardedAd.OnAdClicked -= OnFullscreenAdClicked;
            }

            RewardedAd.Load(adUnitId, request, (rewardedAd, loadAdsError) =>
            {
                if (rewardedAd != null)
                {
                    RewardedAd = rewardedAd;
                    RewardedAd.OnAdFullScreenContentOpened += OnFullscreenAdOpening;
                    RewardedAd.OnAdFullScreenContentFailed += OnFullscreenAdFailedToShow;
                    RewardedAd.OnAdFullScreenContentClosed += OnFullscreenAdClosed;
                    RewardedAd.OnAdPaid += OnFullscreenAdPaid;
                    RewardedAd.OnAdClicked += OnFullscreenAdClicked;
                    OnFullscreenAdLoaded();
                }
                if (loadAdsError != null)
                {
                    OnFullscreenAdFailedToLoad(loadAdsError);
                }
            });
        }

        public override void Show()
        {
            RewardedAd.Show((reward) =>
            {
                OnRewardedAdUserEarned(this, reward);
            });
        }

        public override string MediationAdapterClassName()
        {
            return RewardedAd.GetResponseInfo().GetMediationAdapterClassName();
        }

        protected override void OnFullscreenAdLoaded()
        {
            base.OnFullscreenAdLoaded();
            Listener.OnRewardedAdLoaded(this);
        }

        protected override void OnFullscreenAdFailedToLoad(LoadAdError loadAdError)
        {
            base.OnFullscreenAdFailedToLoad(loadAdError);
            Listener.OnRewardedAdFailedToLoad(this, loadAdError);
        }

        protected override void OnFullscreenAdOpening()
        {
            base.OnFullscreenAdOpening();
            Listener.OnRewardedAdOpening(this);
        }

        protected void OnFullscreenAdFailedToShow(AdError adError)
        {
            Listener.OnRewardedAdFailedToShow(this, adError);
        }

        protected void OnRewardedAdUserEarned(object sender, GoogleMobileAds.Api.Reward reward)
        {
            OnRewardEarned?.Invoke(AdUnitId, AdNetwork, reward.Type, reward.Amount);
        }

        protected override void OnFullscreenAdPaid(AdValue adValue)
        {
            base.OnFullscreenAdPaid(adValue);
            Listener.OnRewardedAdPaid(this, adValue);
        }

        protected override void OnFullscreenAdClosed()
        {
            base.OnFullscreenAdClosed();
            Listener.OnRewardedAdClosed(this);
        }
        protected override void OnFullscreenAdClicked()
        {
            base.OnFullscreenAdClicked();
            Listener.OnRewardedAdClicked(this);
        }
    }
}
#endif