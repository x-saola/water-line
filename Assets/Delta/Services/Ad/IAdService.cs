using System;

namespace Delta.Services
{
    public interface IAdService
    {
        event Action<float> OnRefreshBannerAdBorder;

        void SetBannerPlacement(string placementId);
        void RequestAd();
        void ActiveBannerAd();
        void DeactiveBannerAd();
        bool IsReadyToShowInterstitialAd(int minIntervalSeconds);
        void ShowInterstitialAd(string placementId, Action<bool> cb);
        bool IsRewardedAdLoaded();
        void ShowRewardedAd(string placementId, Action<bool> onClosed, bool autoRequest = true);
    }
}
