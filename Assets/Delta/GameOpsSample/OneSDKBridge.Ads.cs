using UnityEngine;
using CustomUtils;
using Delta.GameOps;

public partial class OneSDKBridge : SingletonMono<OneSDKBridge>
{
    private bool isWaitToShowAdsColStart = false;
    //ads banner
    private bool isLoadedBanner = false;
    private bool isHidingBanner = false;
    private void AddListenerAds()
    {
        //athena app
        _deltaApp.OnAdSDKInitialized += HandleAdSDKInitialized;
        //banner
        _deltaApp.evtBannerAdRefresh += HandleBannerAdLoaded;
        _deltaApp.evtBannerAdFailedToLoad += HandleBannerFailedToLoad;
        _deltaApp.evtBannerAdClicked += HandleBannerAdPaid;
        //interstitial
        _deltaApp.evtInterstitialAdLoaded += HandleInterstitialAdLoaded;
        _deltaApp.evtInterstitialAdFailedToLoad += HandleInterstitialAdFailedToLoad;
        _deltaApp.evtInterstitialAdShow += HandleInterstitialAdShow;
        _deltaApp.evtInterstitialAdClosed += HandleInterstitialAdClosed;
        _deltaApp.evtInterstitialAdClicked += HandleInterstitialAdClicked;
        //rewarded ad
        _deltaApp.evtRewardedAdFailedToLoad += HandleRewardedAdFailedToLoad;
        _deltaApp.evtRewardedAdLoaded += HandleRewardedAdLoaded;
        _deltaApp.evtRewardedAdUserEarned += HandleRewardedAdUserEarned;
        _deltaApp.evtRewardedAdShow += HandleRewardedAdShow;
        _deltaApp.evtRewardedAdClosed += HandleRewardedAdClosed;

    }
    private void RemoveListenerAds()
    {

        _deltaApp.OnAdSDKInitialized -= HandleAdSDKInitialized;
        //banner
        _deltaApp.evtBannerAdRefresh -= HandleBannerAdLoaded;
        _deltaApp.evtBannerAdFailedToLoad -= HandleBannerFailedToLoad;
        _deltaApp.evtBannerAdClicked -= HandleBannerAdPaid;
        //interstitial
        _deltaApp.evtInterstitialAdLoaded -= HandleInterstitialAdLoaded;
        _deltaApp.evtInterstitialAdFailedToLoad -= HandleInterstitialAdFailedToLoad;
        _deltaApp.evtInterstitialAdShow -= HandleInterstitialAdShow;
        _deltaApp.evtInterstitialAdClosed -= HandleInterstitialAdClosed;
        _deltaApp.evtInterstitialAdClicked -= HandleInterstitialAdClicked;
        //rewareded ads
        _deltaApp.evtRewardedAdFailedToLoad -= HandleRewardedAdFailedToLoad;
        _deltaApp.evtRewardedAdLoaded -= HandleRewardedAdLoaded;
        _deltaApp.evtRewardedAdUserEarned -= HandleRewardedAdUserEarned;
        _deltaApp.evtRewardedAdShow -= HandleRewardedAdShow;
        _deltaApp.evtRewardedAdClosed -= HandleRewardedAdClosed;
    }

    private void HandleAdSDKInitialized()
    {
        // RequestAdBreak();
        RequestInterstitial();
        RequestRewardedAds();
    }

    #region  Banner
    public void RequestBanner()
    {
        _deltaApp.AdManager.RequestBanner();
    }
    public void RequestDisableBannerOnLoad()
    {
        _deltaApp.AdManager.DeactiveBannerAd();
    }
    public void ShowBanner()
    {
        if (!isLoadedBanner) return;
        isHidingBanner = false;
        _deltaApp.AdManager.ActiveBannerAd();
    }
    public void HideBanner()
    {
        if (!isLoadedBanner) return;
        isHidingBanner = true;
        _deltaApp.AdManager.DeactiveBannerAd();
    }
    public void DestroyBanner()
    {
        _deltaApp.AdManager.DestroyBannerAd();
    }

    //callback
    private void HandleBannerAdLoaded(string adUnitId, string adNetwork)
    {
        isLoadedBanner = true;
    }
    private void HandleBannerFailedToLoad(string adUnitId)
    {
    }
    private void HandleBannerAdPaid(string adUnitId, string adNetwork)
    {
    }

    #endregion
    #region  Interstitial
    public void HandleInterstitialAdLoaded(string adUnitId, string adNetwork)
    {
        // HandleColdStartAdLoaded(adUnitId, adNetwork);
    }
    private void HandleInterstitialAdClicked(string placementId, string adUnitId, string adNetwork, VideoAdType videoAdType)
    {
    }
    private void HandleInterstitialAdFailedToLoad(string adUnitId, VideoAdType videoAdType)
    {
    }
    private void HandleInterstitialAdShow(string placementId, string adUnitId, string adNetwork, VideoAdType videoAdType)
    {
    }

    private void HandleInterstitialAdClosed(VideoAdType videoAdType)
    {
    }

    public bool IsInterstitialAvailable()
    {
        return _deltaApp.AdManager.IsInterstitialReady();
    }
    public void RequestInterstitial()
    {
        _deltaApp.AdManager.RequestInterstitial(autoRetry: true);//don't auto retry because logic load ads in AdsMgr.cs
    }
    public void ShowInterstitial(string adPlacement)
    {
        // string adPlacement = "Interstitial_Placement";
        _deltaApp.AdManager.ShowInterstitial(adPlacement, (result) =>
        {

        }, autoPreLoad: true);//don't auto load because logic load ads in AdsMgr.cs
    }
    #endregion
    #region  AdBreak
    public void RequestAdBreak()
    {
        _deltaApp.AdManager.RequestAdBreak(autoRetry: true);
    }
    public bool IsAdsBreakAvailable()
    {
        return _deltaApp.AdManager.IsAdBreakReady();
    }
    public void ShowAdBreak()
    {
        if (_deltaApp.AdManager.IsAdBreakReady())
        {
            string adPlacement = "AdBreak_Placement";
            _deltaApp.AdManager.ShowAdBreak(adPlacement, (result) =>
            {

            }, autoPreLoad: true);
        }
        else
        {
            RequestAdBreak();
        }
    }
    #endregion
    #region AdSoftLaunch
    public void RequestAdSoftLaunch()
    {
        _deltaApp.AdManager.RequestSoftLaunchAd(autoRetry: true);
    }
    public void ShowInterstitialSoftLaunch()
    {
        if (_deltaApp.AdManager.IsSoftLaunchAdReady())
        {
            string adPlacement = "InterSoftLaunch";
            _deltaApp.AdManager.ShowAdSoftLaunch(adPlacement, (result) =>
            {

            }, autoPreLoad: true);
        }
        else
        {
            RequestAdSoftLaunch();
        }
    }
    #endregion
    #region  AdColdStart
    public void HandleColdStartAdLoaded(string adUnitId, string adNetwork)
    {
        if (adUnitId == _deltaApp.AdManager.ColdsStartAdId && isWaitToShowAdsColStart)
        {
            isWaitToShowAdsColStart = false;
            if (_deltaApp.AdManager.IsColdStartAdReady())
            {
                ShowInterstitialColdStart();
            }
        }
    }
    private void HandleTimeoutColdStart()
    {
        isWaitToShowAdsColStart = false;
    }
    public void RequestColdStartAd()
    {
        _deltaApp.AdManager.RequestColdStartAd(autoRetry: true);
    }
    public void ShowInterstitialColdStart()
    {
        isWaitToShowAdsColStart = true;
        if (_deltaApp.AdManager.IsColdStartAdReady())
        {
            isWaitToShowAdsColStart = false;
            string adPlacement = "InterColdStart";
            _deltaApp.AdManager.ShowAdColdStart(adPlacement, (result) =>
            {

            }, autoPreLoad: true);
        }
    }
    #endregion
    #region  RewardedAd
    private void HandleRewardedAdFailedToLoad(string adUnitId)
    {
    }

    private void HandleRewardedAdLoaded(string adUnitId, string adNetwork)
    {
    }

    private void HandleRewardedAdUserEarned(string placementId, string adUnitId, string adNetwork)
    {
    }

    private void HandleRewardedAdShow(string placementId, string adUnitId, string adNetwork)
    {
    }

    private void HandleRewardedAdClosed()
    {
    }

    public bool IsRewardedVideoAvailable()
    {
        return _deltaApp.AdManager.IsRewardedAdLoaded();
    }

    public void RequestRewardedAds()
    {
        _deltaApp.AdManager.RequestRewardedAd(autoRetry: true);
    }

    public void ShowRewardedAd(string adPlacement)
    {
        Debug.Log("ShowRewardedAd: " + adPlacement);

        _deltaApp.AdManager.ShowRewardedAd(adPlacement, (lable, amount) =>
        {
        }, (closed) =>
        {
            Debug.Log("result: " + closed + " closed callback ");
        }, autoPreLoad: true
        );
    }
    #endregion
}
