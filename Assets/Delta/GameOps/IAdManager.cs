using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.GameOps
{
    public enum VideoAdType
    {
        InterNormal,
        InterColdStart,
        InterSoftLaunch,
        InterAdBreak,
        RewardedAd
    }
    public interface IAdManager
    {
        IAdEventsListener AdEventsListener { get; set; }
        void CleanUp();

        bool IsPlayingFullscreenlAds { get; }
        string InterstitialAdId { get; }
        string ColdsStartAdId { get; }
        bool IsInterstitialAdLoaded { get; }
        bool IsInterstitialAdFailedToLoad { get; }

        string BannerAdId { get; }
        string BannerPlacementId { get; set; }
        bool IsPaddingBannerSupported { get; }
        bool IsUsingAdaptiveBanner { get; }
        int PaddingBannerY { get; }
        int PaddingBannerX { get; }
        bool IsBannerAdActive { get; }
        Vector2 BannerPositionInPixels { get; }
        event System.Action OnBannerDidUpdateConfigs;

        void SetTestDevices(string[] testDevices);
        void ShowDebuggerPanel();

        void RequestBanner();
        void ActiveBannerAd();
        void DeactiveBannerAd();
        void DestroyBannerAd();
        void RefreshBannerConfigs(string bannerAdId, int paddingX, int paddingY, bool useAdaptiveBanner);
        void SetSafeTopAdaptiveBanner(float screenPosY);
        void SetBannerPlacement(string placementId);

        void RefreshInterstitialConfigs(string interstitialAdId);
        bool IsInterstitialReady();
        bool IsSoftLaunchAdReady();
        bool IsColdStartAdReady();
        bool IsAdBreakReady();
        void RequestInterstitial(bool autoRetry = true);
        void RequestColdStartAd(bool autoRetry = true);
        void RequestSoftLaunchAd(bool autoRetry = true);
        void RequestAdBreak(bool autoRetry = true);
        void ShowInterstitial(string placemendId, System.Action<bool> cb, bool autoPreLoad = true);
        void ShowAdBreak(string placemendId, System.Action<bool> cb, bool autoPreLoad = true);
        void ShowAdSoftLaunch(string placemendId, System.Action<bool> cb, bool autoPreLoad = true);
        void ShowAdColdStart(string placemendId, System.Action<bool> cb, bool autoPreLoad = true);
        IEnumerator WaitForInterstitialAdLoadedAndCallback(System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout);
        void AskToReloadMutedInterstitialAd();

        void RequestInterstitial(string adUnitId, bool autoRetry = true);
        void ShowInterstitialAd(string adUnitId, string placemendId, System.Action<bool> cb, bool autoPreload = true);
        bool CheckInterstitialAdFailedToLoad(string adUnitId);
        bool CheckInterstitialAdLoaded(string adUnitId);
        IEnumerator WaitForInterstitialAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout);
        void AskToReloadMutedInterstitialAd(string adUnitId);

        void RequestRewardedAd(bool autoRetry = true);
        void RequestRewardedAd(string adUnitId, bool autoRetry = true);
        void ShowRewardedAd(string placemendId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreLoad = true);//update config to file
        void ShowRewardedAd(string placemendId, string adUnitId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreLoad = true);
        bool IsRewardedAdLoaded();
        bool IsRewardedAdLoaded(string adUnitId);
        IEnumerator WaitForRewardedAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout);
    }
}
