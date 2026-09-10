using System;
using Delta.Common.UI;
using Delta.GameOps;
using UnityEngine;

namespace Delta.Services
{
    public class AdService : IAdService
    {
        private long _lastTimeShowInterstitialAd;

        public event Action<float> OnRefreshBannerAdBorder;

        public void SetBannerPlacement(string placementId)
        {
            DeltaApp.Instance.AdManager.SetBannerPlacement(placementId);
        }

        public void RequestAd()
        {
            DeltaApp.Instance.AdManager.RequestBanner();
            DeltaApp.Instance.AdManager.RequestInterstitial();
            DeltaApp.Instance.AdManager.RequestRewardedAd();
        }

        public void ActiveBannerAd()
        {
            DeltaApp.Instance.AdManager.ActiveBannerAd();
        }

        public void DeactiveBannerAd()
        {
            DeltaApp.Instance.AdManager.DeactiveBannerAd();
        }

        public bool IsReadyToShowInterstitialAd(int minIntervalSeconds)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now - _lastTimeShowInterstitialAd >= minIntervalSeconds;
        }

        public void ShowInterstitialAd(string placementId, Action<bool> cb)
        {
            DeltaApp.Instance.AdManager.ShowInterstitial(placementId, success =>
            {
                cb?.Invoke(success);
                _lastTimeShowInterstitialAd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            });
        }

        public bool IsRewardedAdLoaded()
        {
            return DeltaApp.Instance.AdManager.IsRewardedAdLoaded();
        }

        public void ShowRewardedAd(string placementId, Action<bool> onClosed, bool autoRequest = true)
        {
            if (!DeltaApp.Instance.AdManager.IsRewardedAdLoaded())
            {
                var noticeUI = UIManager.Instance.ShowUIOnTop<NoticeUI>("NoticeUI");
                noticeUI.ShowNoticeText("<color=#FF1E1E>Oops! No ad available right now.</color>");
                return;
            }

            bool isRewarded = false;
            DeltaApp.Instance.AdManager.ShowRewardedAd(placementId,
                (id, earned) => isRewarded = true,
                didShown =>
                {
                    if (autoRequest)
                        DeltaApp.Instance.AdManager.RequestRewardedAd();
                    onClosed?.Invoke(isRewarded);
                });
        }

        private void RefreshBannerAdBorder()
        {
            OnRefreshBannerAdBorder?.Invoke(BannerAdOffsetY());
        }

        private float BannerAdOffsetY()
        {
            if (!DeltaApp.Instance.AdManager.IsBannerAdActive)
                return 0f;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(UIManager.Instance.GameRect, DeltaApp.Instance.AdManager.BannerPositionInPixels, UIManager.Instance.CameraUI, out var p1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(UIManager.Instance.GameRect, Vector2.zero, UIManager.Instance.CameraUI, out var p2);

            return Mathf.Abs(p1.y - p2.y);
        }
    }
}
