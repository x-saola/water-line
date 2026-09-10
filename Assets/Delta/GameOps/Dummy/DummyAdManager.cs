using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.GameOps
{
    public class DummyAdManager : IAdManager
    {
        public bool IsPlayingFullscreenlAds { get { return false; } }
        public bool IsInterstitialAdFailedToLoad { get { return true; } }
        public bool IsInterstitialAdLoaded { get { return false; } }

        public bool CheckInterstitialAdLoaded(string adUnitId)
        {
            return false;
        }

        public bool CheckInterstitialAdFailedToLoad(string adUnitId)
        {
            return true;
        }

        public Vector2 BannerPositionInPixels { get; private set; }
        public bool IsBannerAdActive { get { return false; } }
        public bool IsPaddingBannerSupported { get { return true; } }

        public string BannerAdId { get { return _bannerAdId; } }
        public string BannerPlacementId
        {
            get { return _bannerAdPlacement; }
            set { _bannerAdPlacement = value; }
        }
        public string InterstitialAdId { get; private set; }
        public string ColdsStartAdId { get; private set; }
        public int PaddingBannerX { get { return _bannerPaddingX; } }
        public int PaddingBannerY { get { return _bannerPaddingY; } }
        public bool IsUsingAdaptiveBanner { get { return _isUsingAdaptiveBanner; } }

        public event System.Action OnBannerDidUpdateConfigs;
        public IAdEventsListener AdEventsListener { get; set; }

        string _bannerAdId = "unused";
        string _bannerAdPlacement = "null";

        bool _isUsingAdaptiveBanner;
        bool _isPaddingBannerAd;
        int _bannerPaddingX = 0;
        int _bannerPaddingY = 0;

        public DummyAdManager(bool enablePaddingBanner, bool usingAdaptiveBanner, string bannerAdId, string interstitialAdId, bool hideBannerAdWhenAppPaused)
        {
            _isPaddingBannerAd = enablePaddingBanner;
            _isUsingAdaptiveBanner = usingAdaptiveBanner;
            _bannerAdId = bannerAdId;
            BannerPlacementId = string.Empty;
            InterstitialAdId = interstitialAdId;
        }
        public void ShowDebuggerPanel()
        {

        }
        public void CleanUp()
        {

        }

        public void SetTestDevices(string[] testDevices)
        {

        }

        #region Banner Ad
        public void RequestBanner()
        {

        }

        public void DeactiveBannerAd()
        {

        }

        public void ActiveBannerAd()
        {

        }
        public void DestroyBannerAd()
        {

        }

        public void SetSafeTopAdaptiveBanner(float screenPosY)
        {
            OnBannerDidUpdateConfigs?.Invoke();
        }

        public void SetBannerPlacement(string placementId)
        {
            BannerPlacementId = placementId;
        }

        public void RefreshBannerConfigs(string bannerAdId, int paddingX, int paddingY, bool useAdaptiveBanner)
        {
            bool isUpdated = useAdaptiveBanner != _isUsingAdaptiveBanner || paddingX != _bannerPaddingX || paddingY != _bannerPaddingY || (!_bannerAdId.Equals(bannerAdId) && !string.IsNullOrEmpty(bannerAdId));
            if (!isUpdated)
                return;

            _bannerPaddingX = paddingX;
            _bannerPaddingY = paddingY;
            _isUsingAdaptiveBanner = useAdaptiveBanner;
            if (!string.IsNullOrEmpty(bannerAdId))
                _bannerAdId = bannerAdId;

            OnBannerDidUpdateConfigs?.Invoke();
        }
        #endregion

        #region Interstitial Ad
        public void RefreshInterstitialConfigs(string interstitialAdId)
        {
            InterstitialAdId = interstitialAdId;
        }
        public bool IsInterstitialReady()
        {
            return false;
        }
        public bool IsSoftLaunchAdReady()
        {
            return false;
        }
        public bool IsColdStartAdReady()
        {
            return false;
        }
        public bool IsAdBreakReady()
        {
            return false;
        }
        public void RequestInterstitial(bool autoRetry = true)
        {

        }
        public void RequestSoftLaunchAd(bool autoRetry = true)
        {

        }
        public void RequestColdStartAd(bool autoRetry = true)
        {

        }
        public void RequestAdBreak(bool autoRetry = true)
        {

        }
        public void ShowInterstitial(string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            cb?.Invoke(false);
        }
        public void ShowAdBreak(string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            cb?.Invoke(false);
        }

        public void ShowAdSoftLaunch(string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            cb?.Invoke(false);
        }

        public void ShowAdColdStart(string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            cb?.Invoke(false);
        }
        public IEnumerator WaitForInterstitialAdLoadedAndCallback(System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            cb?.Invoke(false, PollFullscreenAdErrorCode.Failed);
            yield break;
        }

        public void AskToReloadMutedInterstitialAd() { }
        #endregion

        #region Other interstitial
        public void RequestInterstitial(string adUnitId, bool autoRetry = true)
        {

        }

        public void ShowInterstitialAd(string adUnitId, string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            cb(false);
        }

        public IEnumerator WaitForInterstitialAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            cb(false, PollFullscreenAdErrorCode.Failed);
            yield break;
        }

        public void AskToReloadMutedInterstitialAd(string adUnitId) { }
        #endregion

        #region Reward Ad
        public bool IsRewardedAdLoaded()
        {
            return false;
        }
        public void ShowRewardedAd(string placemendId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreLoad = true)
        {
        }
        public void RequestRewardedAd(bool autoRetry = true)
        {

        }
        public void RequestRewardedAd(string adUnitId, bool autoRetry = true)
        {

        }

        public IEnumerator WaitForRewardedAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            cb(false, PollFullscreenAdErrorCode.Failed);
            yield break;
        }

        public bool IsRewardedAdLoaded(string adUnitId)
        {
            return false;
        }

        public void ShowRewardedAd(string placemendId, string adUnitId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreload = true)
        {
            cbClosed(false);
        }
        #endregion
    }
}