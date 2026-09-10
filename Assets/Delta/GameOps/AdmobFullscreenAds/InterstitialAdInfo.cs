#if USE_ADMOB_MEDIATION
using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;

namespace Delta.GameOps
{
    public interface InterstitialAdListener
    {
        void OnInterstitialAdLoaded(InterstitialAdInfo adInfo);
        void OnInterstitialAdFailedToLoad(InterstitialAdInfo adInfo, LoadAdError args);
        void OnInterstitialAdOpening(InterstitialAdInfo adInfo);
        void OnInterstitialAdPaid(InterstitialAdInfo adInfo, AdValue args);
        void OnInterstitialAdClosed();
        void OnInterstitialAdClick(InterstitialAdInfo adInfo);
    }

    public class InterstitialAdInfo : FullscreenAdInfo
    {
        public InterstitialAdListener Listener;
        public InterstitialAd InterstitialAd;

        protected override void OnLoad(string adUnitId, AdRequest request, bool isMuted)
        {
            if (InterstitialAd != null)
            {
                InterstitialAd.OnAdFullScreenContentOpened -= OnFullscreenAdOpening;
                InterstitialAd.OnAdFullScreenContentClosed -= OnFullscreenAdClosed;
                InterstitialAd.OnAdPaid -= OnFullscreenAdPaid;
                InterstitialAd.OnAdClicked -= OnFullscreenAdClicked;
                InterstitialAd.Destroy();
            }
            InterstitialAd.Load(adUnitId, request, (interAd, error) =>
            {
                if (interAd != null)
                {
                    InterstitialAd = interAd;
                    InterstitialAd.OnAdFullScreenContentOpened += OnFullscreenAdOpening;
                    InterstitialAd.OnAdFullScreenContentClosed += OnFullscreenAdClosed;
                    InterstitialAd.OnAdPaid += OnFullscreenAdPaid;
                    InterstitialAd.OnAdClicked += OnFullscreenAdClicked;
                    OnFullscreenAdLoaded();
                }
                if (error != null)
                {
                    OnFullscreenAdFailedToLoad(error);
                }
            });
        }

        public override void Show()
        {
            InterstitialAd.Show();
        }

        public override string MediationAdapterClassName()
        {
            return InterstitialAd.GetResponseInfo().GetMediationAdapterClassName();
        }

        protected override void OnFullscreenAdLoaded()
        {
            base.OnFullscreenAdLoaded();
            Listener.OnInterstitialAdLoaded(this);
        }

        protected override void OnFullscreenAdFailedToLoad(LoadAdError error)
        {
            base.OnFullscreenAdFailedToLoad(error);
            Listener.OnInterstitialAdFailedToLoad(this, error);
        }

        protected override void OnFullscreenAdOpening()
        {
            base.OnFullscreenAdOpening();
            Listener.OnInterstitialAdOpening(this);
        }

        protected override void OnFullscreenAdPaid(AdValue adValue)
        {
            base.OnFullscreenAdPaid(adValue);
            Listener.OnInterstitialAdPaid(this, adValue);
        }

        protected override void OnFullscreenAdClosed()
        {
            base.OnFullscreenAdClosed();
            Listener.OnInterstitialAdClosed();
        }
        protected override void OnFullscreenAdClicked()
        {
            base.OnFullscreenAdClicked();
            Listener.OnInterstitialAdClick(this);
        }
    }

}
#endif