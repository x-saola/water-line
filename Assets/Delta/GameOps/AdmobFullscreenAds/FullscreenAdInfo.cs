using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_ADMOB_MEDIATION
using GoogleMobileAds.Api;
#endif

namespace Delta.GameOps
{
    public enum FullscreenAdState
    {
        Null,
        Loading,
        LoadFailed,
        Loaded,
        Used
    }
#if USE_ADMOB_MEDIATION

    public abstract class FullscreenAdInfo
    {
        public string AdUnitId;
        public FullscreenAdState AdState;
        public VideoAdType adType;

        public string AdNetwork;

        public Coroutine RequestAdCoroutine;
        public long WaitForAdPaidEventExpiredAt;
        public bool LoadedAdWithMutedSound;
        public bool AutoRetry;
        public int RetryAttempt;

        public void Load(string adUnitId, AdRequest request, bool isMuted)
        {
            AdState = FullscreenAdState.Loading;
            WaitForAdPaidEventExpiredAt = 0;
            LoadedAdWithMutedSound = isMuted;

            OnLoad(adUnitId, request, isMuted);
        }

        protected abstract void OnLoad(string adUnitId, AdRequest request, bool isMuted);
        public abstract void Show();
        public abstract string MediationAdapterClassName();

        protected virtual void OnFullscreenAdLoaded()
        {
            RetryAttempt = 0;
            AdState = FullscreenAdState.Loaded;
        }

        protected virtual void OnFullscreenAdFailedToLoad(LoadAdError error)
        {
            AdState = FullscreenAdState.LoadFailed;
            RetryAttempt++;
        }

        protected virtual void OnFullscreenAdOpening()
        {
            AdState = FullscreenAdState.Used;
        }

        protected virtual void OnFullscreenAdPaid(AdValue adValue)
        {
            WaitForAdPaidEventExpiredAt = 0;
        }

        protected virtual void OnFullscreenAdClosed()
        {

        }
        protected virtual void OnFullscreenAdClicked()
        {

        }
    }
#endif
}

