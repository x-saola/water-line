#if USE_MAX_MEDIATION
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudienceNetwork;

#if USE_AMAZON && !UNITY_EDITOR
using AmazonAds;
#endif
namespace Delta.GameOps
{
    public class MAXAdManager : IAdManager
    {
        abstract class VideoAdInfo
        {
            public string AdUnitId;
            public VideoAdType AdType;
            public FullscreenAdState AdState;
            public Coroutine RequestAdCoroutine;
            public bool LoadedAdWithMutedSound;
            public System.Action<bool> OnAdClosed;
            public int RetryAttempt;
            public bool AutoRetry;
            public bool AutoPreload;
            public bool IsFirstLoadAd;

            public abstract bool IsReady();
            public abstract void Show(string placementId = null);
        }

        class InterstitialAdInfo : VideoAdInfo
        {
            public override bool IsReady()
            {
                return AdState == FullscreenAdState.Loaded && MaxSdk.IsInterstitialReady(AdUnitId);
            }

            public override void Show(string placementId)
            {
                MaxSdk.ShowInterstitial(AdUnitId, placementId);
            }
        }

        class RewardedAdInfo : VideoAdInfo
        {
            public System.Action<string, double> OnRewardedEarned;

            public override bool IsReady()
            {
                return AdState == FullscreenAdState.Loaded && MaxSdk.IsRewardedAdReady(AdUnitId);
            }

            public override void Show(string placementId)
            {
                MaxSdk.ShowRewardedAd(AdUnitId, placementId);
            }
        }

        bool _isIPad;
        public IAdEventsListener AdEventsListener { get; set; }

        public string BannerAdId { get; private set; }
        public int PaddingBannerY { get { return _bannerPaddingY; } }
        public int PaddingBannerX { get { return _bannerPaddingX; } }
        public bool IsPaddingBannerSupported { get { return true; } }
        public bool IsUsingAdaptiveBanner { get { return _isUsingAdaptiveBanner; } }
        public bool IsBannerAdActive { get { return IsBannerAdCreated && _bannerAdLoaded; } }
        public bool IsBannerAdCreated { get; private set; }
        public Vector2 BannerPositionInPixels { get; private set; }
        public string BannerPlacementId
        {
            get { return _bannerAdPlacement; }
            set { _bannerAdPlacement = value; }
        }

        public string InterstitialAdId { get { return _interstitialAdInfo.AdUnitId; } }
        public string ColdsStartAdId { get { return _coldStartAdInfo.AdUnitId; } }
        public bool IsPlayingFullscreenlAds { get; private set; }
        public bool IsInterstitialAdFailedToLoad { get { return _interstitialAdInfo.AdState == FullscreenAdState.LoadFailed; } }
        public bool IsInterstitialAdLoaded { get { return _interstitialAdInfo.AdState == FullscreenAdState.Loaded; } }
        public string AmazonAppId { get; set; }
        public string AmazonBannerSlotId { get; set; }
        public string AmazonInterstitialSlotId { get; set; }
        public string AmazonRewardedVideoSlotId { get; set; }
        public bool IsAmazonTestMode { get; set; }

        public event System.Action OnBannerDidUpdateConfigs;

        bool _isInitialized;
        bool _showMediationDebugger;
        System.Action _onSDKInitialized;
        IMainAppService _appService;
        string[] _testDevices;

        Coroutine _requestBannerCoroutine;
        bool _bannerAdLoaded;
        string _bannerAdPlacement = string.Empty;
        bool _bannerIsAskedToBeHidden;
        bool _bannerIsHiddenByAppPerf;
        bool _isUsingAdaptiveBanner;
        bool _isPaddingBannerAd;
        int _bannerPaddingX = 0;
        int _bannerPaddingY = 0;
        int _safeBannerTopY;

        Coroutine _checkingSoundSwitch;
        string _videoAdPlacementId;
        string _interstitialPlacementId;
        string _coldStartPlacementId;
        string _softLaunchPlacementId;
        string _adBreakPlacementId;


        VideoAdInfo _interstitialAdInfo;
        VideoAdInfo _softLaunchAdInfo;
        VideoAdInfo _coldStartAdInfo;
        VideoAdInfo _adBreakInfo;
        VideoAdInfo _rewardedAdInfo;
        Dictionary<string, VideoAdInfo> _allVideoAds = new Dictionary<string, VideoAdInfo>();
        // private bool _isFirstLoadInterstitialAd = true;
        // private bool _isFirstLoadVideoInterstitialAd = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass _unityPlayer;
        AndroidJavaObject _activity;
        float _bannerWidth;
#endif

        public MAXAdManager(System.Action onSDKInitialized, bool enablePaddingBanner, bool usingAdaptiveBanner, string bannerAdId, string interstitialAdId,
            string softlaunchAdId, string coldstartAdId, string adBreakId, string rewardedAdId, IMainAppService appService,
            bool showMediationDebugger, bool hideBannerAdWhenAppPaused = false, bool hideBannerAdWhenAppLowPerf = false, bool iOSAppPausedOnBackground = false, string amazonID = "", bool amazonTestMode = false,
            bool setUnifiedConsentFlow = false, bool isSetUserJourney = false, string userJourneyId = "")
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _activity = _unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
#endif
            _isPaddingBannerAd = enablePaddingBanner;
            _isUsingAdaptiveBanner = usingAdaptiveBanner;
            _appService = appService;
            _onSDKInitialized = onSDKInitialized;
            _showMediationDebugger = showMediationDebugger;
            BannerAdId = bannerAdId;
            _bannerAdPlacement = "banner_all";
            _videoAdPlacementId = "null";
            _interstitialPlacementId = "null";
            _coldStartPlacementId = "null";
            _softLaunchPlacementId = "null";
            _adBreakPlacementId = "null";
            AmazonAppId = amazonID;
            IsAmazonTestMode = amazonTestMode;

#if UNITY_IOS
            _isIPad = SystemInfo.deviceModel.Contains("iPad");

            if (hideBannerAdWhenAppPaused)
                _appService.SubscribeAppPause(OnAppPaused);

            if (hideBannerAdWhenAppLowPerf)
                _appService.SubscribeAppPerfChanged(OnAppPerfChanged);
#endif
            if (!string.IsNullOrEmpty(interstitialAdId))
            {
                _interstitialAdInfo = new InterstitialAdInfo()
                {
                    AdUnitId = interstitialAdId,
                    AdType = VideoAdType.InterNormal,
                    IsFirstLoadAd = true
                };
                _allVideoAds.Add(interstitialAdId, _interstitialAdInfo);
            }

            if (!string.IsNullOrEmpty(softlaunchAdId))
            {
                _softLaunchAdInfo = new InterstitialAdInfo()
                {
                    AdUnitId = softlaunchAdId,
                    AdType = VideoAdType.InterSoftLaunch,
                    IsFirstLoadAd = true
                };
                _allVideoAds.Add(softlaunchAdId, _softLaunchAdInfo);
            }
            if (!string.IsNullOrEmpty(coldstartAdId))
            {
                _coldStartAdInfo = new InterstitialAdInfo()
                {
                    AdUnitId = coldstartAdId,
                    AdType = VideoAdType.InterColdStart,
                    IsFirstLoadAd = true
                };
                _allVideoAds.Add(coldstartAdId, _coldStartAdInfo);
            }
            if (!string.IsNullOrEmpty(adBreakId))
            {
                _adBreakInfo = new InterstitialAdInfo()
                {
                    AdUnitId = adBreakId,
                    AdType = VideoAdType.InterAdBreak,
                    IsFirstLoadAd = true
                };
                _allVideoAds.Add(adBreakId, _adBreakInfo);
            }
            if (!string.IsNullOrEmpty(rewardedAdId))
            {
                _rewardedAdInfo = new RewardedAdInfo()
                {
                    AdUnitId = rewardedAdId,
                    AdType = VideoAdType.RewardedAd,
                    IsFirstLoadAd = true
                };
                _allVideoAds.Add(rewardedAdId, _rewardedAdInfo);
            }

            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitializedEvent;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

            //Log ad revenue
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialPaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdPaidEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerPaidEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMrecRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenRevenuePaidEvent;

            // user journey
            if (isSetUserJourney)
            {
                // string userID = PlayerPrefs.GetString(PlayerPrefKey.UserIDKey, "");
                if (!string.IsNullOrEmpty(userJourneyId))
                {
                    Debug.LogError(">>>UserID: " + userJourneyId + "<<<");
#if UNITY_ANDROID && !UNITY_EDITOR
                MaxSdkAndroid.SetUserId(userJourneyId);
#elif UNITY_IOS && !UNITY_EDITOR
                MaxSdkiOS.SetUserId(userJourneyId);
#else
                    MaxSdk.SetUserId(userJourneyId);
#endif
                }
            }
            // if (setUnifiedConsentFlow)
            // {
            //     MaxSdk.SetExtraParameter("eifc", "iOf8gUDWef");//The Unified Consent Flow is available to internal studios only
            // }
            MaxSdk.InitializeSdk();
        }

        public void OnDestroy()
        {
            CleanUp();
        }

        public void CleanUp()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitializedEvent;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClickedEvent;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClickedEvent;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReceivedRewardEvent;

            //Log ad revenue
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialPaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedAdPaidEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerPaidEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMrecRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAppOpenRevenuePaidEvent;
        }

        private void OnSdkInitializedEvent(MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
#if UNITY_IOS
            if (MaxSdkUtils.CompareVersions(UnityEngine.iOS.Device.systemVersion, "14.5") != MaxSdkUtils.VersionComparisonResult.Lesser)
            {
                AdSettings.SetAdvertiserTrackingEnabled(true);
            }
#endif
            AdSettings.SetDataProcessingOptions(new string[] { });
#if USE_AMAZON && !UNITY_EDITOR
            InitAmazonSDK(AmazonAppId);
#endif
            _isInitialized = true;
            _onSDKInitialized?.Invoke();
            if (_showMediationDebugger)
                MaxSdk.ShowMediationDebugger();
        }

        private void InitAmazonSDK(string amazonAppId)
        {
#if USE_AMAZON && !UNITY_EDITOR
            Amazon.Initialize(amazonAppId);
            Amazon.SetAdNetworkInfo(new AdNetworkInfo(DTBAdNetwork.MAX));
            Amazon.EnableLogging(IsAmazonTestMode);
            Amazon.EnableTesting(IsAmazonTestMode);
#endif
        }

        public void SetTestDevices(string[] testDevices)
        {
            _testDevices = new string[testDevices.Length];
            for (int i = 0; i < testDevices.Length; i++)
                _testDevices[i] = testDevices[i];
        }

        public void ShowMediationDebugger()
        {
            if (_isInitialized)
                MaxSdk.ShowMediationDebugger();
        }
        public void ShowDebuggerPanel()
        {
#if MAX_DEBUGGER
            ShowMediationDebugger();
#endif
        }

        public void RequestBanner()
        {
            if (_requestBannerCoroutine != null)
            {
                return;
            }

            if (IsBannerAdCreated)
            {
                return;
            }

            if (_bannerAdLoaded)
            {
                return;
            }

            _requestBannerCoroutine = _appService.StartCoroutine(CreateBanner());
        }
        public void DestroyBannerAd()
        {
            _bannerIsAskedToBeHidden = true;
            if (IsBannerAdCreated)
            {
                MaxSdk.DestroyBanner(BannerAdId);
            }
        }
        public void DeactiveBannerAd()
        {
            _bannerIsAskedToBeHidden = true;

            if (IsBannerAdCreated)
            {
                MaxSdk.HideBanner(BannerAdId);
            }
        }
        public void ActiveBannerAd()
        {
            _bannerIsAskedToBeHidden = false;

            if (IsBannerAdCreated)
            {
                if (!_bannerIsHiddenByAppPerf)
                {
                    MaxSdk.ShowBanner(BannerAdId);
                }
            }
        }

        public void SetSafeTopAdaptiveBanner(float screenPosY)
        {
            var value = (int)screenPosY;
            if (_safeBannerTopY != value)
            {
                _safeBannerTopY = value;

                if (IsBannerAdCreated)
                {
                    _bannerAdLoaded = false;
                    IsBannerAdCreated = false;
                    RequestBanner();

                    OnBannerDidUpdateConfigs?.Invoke();
                }
            }
        }

        public void SetBannerPlacement(string placementId)
        {
            BannerPlacementId = placementId;
            MaxSdk.SetBannerPlacement(BannerAdId, placementId);
        }

        public void RefreshBannerConfigs(string bannerAdId, int paddingX, int paddingY, bool useAdaptiveBanner)
        {
            bool isUpdated = _isUsingAdaptiveBanner != useAdaptiveBanner || paddingX != _bannerPaddingX || paddingY != _bannerPaddingY || (!BannerAdId.Equals(bannerAdId) && !string.IsNullOrEmpty(bannerAdId));
            if (!isUpdated)
                return;

            _bannerPaddingX = paddingX;
            _bannerPaddingY = paddingY;
            _isUsingAdaptiveBanner = useAdaptiveBanner;

            if (!string.IsNullOrEmpty(bannerAdId))
                BannerAdId = bannerAdId;

            if (IsBannerAdCreated)
            {
                Debug.Log("[MAXAdManager] Banner ad configs are updated! Let re-create banner ad!");

                _bannerAdLoaded = false;
                IsBannerAdCreated = false;
                RequestBanner();

                OnBannerDidUpdateConfigs?.Invoke();
            }
        }

        public void RefreshInterstitialConfigs(string interstitialAdId)
        {
            if (_interstitialAdInfo.AdUnitId != interstitialAdId && !string.IsNullOrEmpty(interstitialAdId))
            {
                _allVideoAds.Remove(_interstitialAdInfo.AdUnitId);
                _interstitialAdInfo.AdUnitId = interstitialAdId;
                _allVideoAds.Add(interstitialAdId, _interstitialAdInfo);
            }
        }
        public bool IsInterstitialReady()
        {
            return _interstitialAdInfo.IsReady();
        }
        public bool IsSoftLaunchAdReady()
        {
            return _softLaunchAdInfo.IsReady();
        }
        public bool IsColdStartAdReady()
        {
            return _coldStartAdInfo.IsReady();
        }
        public bool IsAdBreakReady()
        {
            return _adBreakInfo.IsReady();
        }
        public void RequestAdBreak(bool autoRetry = true)
        {
            RequestInterstitial(_adBreakInfo.AdUnitId, autoRetry);
        }
        public void RequestSoftLaunchAd(bool autoRetry = true)
        {
            RequestInterstitial(_softLaunchAdInfo.AdUnitId, autoRetry);
        }
        public void RequestColdStartAd(bool autoRetry = true)
        {
            RequestInterstitial(_coldStartAdInfo.AdUnitId, autoRetry);
        }
        public void RequestInterstitial(bool autoRetry = true)
        {
            _interstitialAdInfo.AutoRetry = autoRetry;

            if (_interstitialAdInfo.AdState == FullscreenAdState.Loading)
            {
                Debug.Log("[MAXAdManager] Waiting for last interstitial response!");
                return;
            }

            if (_interstitialAdInfo.AdState == FullscreenAdState.Loaded)
            {
                Debug.Log("[MAXAdManager] Use last interstitial response!");
                return;
            }

            if (_interstitialAdInfo.RequestAdCoroutine != null)
            {
                Debug.Log("[MAXAdManager] Interstitial is already requested!");
                return;
            }

            _interstitialAdInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(_interstitialAdInfo));
        }

        public void ShowInterstitial(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            _interstitialPlacementId = string.IsNullOrEmpty(placemendId) ? "null" : placemendId;
            _interstitialAdInfo.AutoPreload = autoPreLoad;
            ShowVideoAd(_interstitialAdInfo, _interstitialPlacementId, cb);
        }
        public void ShowAdColdStart(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            _coldStartPlacementId = string.IsNullOrEmpty(placemendId) ? "null" : placemendId;
            _coldStartAdInfo.AutoPreload = autoPreLoad;
            ShowVideoAd(_coldStartAdInfo, _coldStartPlacementId, cb);
        }
        public void ShowAdSoftLaunch(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            _softLaunchPlacementId = string.IsNullOrEmpty(placemendId) ? "null" : placemendId;
            _softLaunchAdInfo.AutoPreload = autoPreLoad;
            ShowVideoAd(_softLaunchAdInfo, _softLaunchPlacementId, cb);
        }
        public void ShowAdBreak(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            _adBreakPlacementId = string.IsNullOrEmpty(placemendId) ? "null" : placemendId;
            _adBreakInfo.AutoPreload = autoPreLoad;
            ShowVideoAd(_adBreakInfo, _adBreakPlacementId, cb);
        }

        public IEnumerator WaitForInterstitialAdLoadedAndCallback(System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            yield return WaitForVideoAdLoadedAndCallback(_interstitialAdInfo, cb, timeout);
        }

        public void AskToReloadMutedInterstitialAd()
        {
            if (_interstitialAdInfo != null && _interstitialAdInfo.AdState == FullscreenAdState.Loaded && !_interstitialAdInfo.LoadedAdWithMutedSound)
            {
                Debug.Log("[MAXAdManager] Ask to reload muted interstitial ad: " + _interstitialAdInfo.AdUnitId);
                _interstitialAdInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(_interstitialAdInfo, 0f));
            }
        }

        public bool CheckInterstitialAdLoaded(string adUnitId)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return false;

            return adInfo.AdState == FullscreenAdState.Loaded;
        }

        public bool CheckInterstitialAdFailedToLoad(string adUnitId)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return false;

            return adInfo.AdState == FullscreenAdState.LoadFailed;
        }

        public void RequestInterstitial(string adUnitId, bool autoRetry = true)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
            {
                adInfo = new InterstitialAdInfo()
                {
                    AdUnitId = adUnitId
                };
                _allVideoAds.Add(adUnitId, adInfo);
            }
            adInfo.AutoRetry = autoRetry;

            if (adInfo.AdState == FullscreenAdState.Loading)
            {
                Debug.Log("[MAXAdManager] Waiting for last interstitial ad response!");
                return;
            }

            if (adInfo.AdState == FullscreenAdState.Loaded)
            {
                Debug.Log("[MAXAdManager] Use last interstitial ad response!");
                return;
            }

            if (adInfo.RequestAdCoroutine != null)
                return;

            adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(adInfo));
        }

        public void ShowInterstitialAd(string adUnitId, string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
            {
                cb(false);
                return;
            }
            adInfo.AutoPreload = autoPreload;
            _interstitialPlacementId = string.IsNullOrEmpty(placemendId) ? "null" : placemendId;
            ShowVideoAd(adInfo, _interstitialPlacementId, cb);
        }

        public IEnumerator WaitForInterstitialAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
            {
                cb(false, PollFullscreenAdErrorCode.NotRequested);
                yield break;
            }

            yield return WaitForVideoAdLoadedAndCallback(adInfo, cb, timeout);
        }

        public void AskToReloadMutedInterstitialAd(string adUnitId)
        {
            VideoAdInfo adInfo;
            if (_allVideoAds.TryGetValue(adUnitId, out adInfo) && adInfo.AdState == FullscreenAdState.Loaded && !adInfo.LoadedAdWithMutedSound)
            {
                Debug.Log("[MAXAdManager] Ask to reload muted interstitial ad: " + adInfo.AdUnitId);
                adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(adInfo, 0f));
            }
        }
        public void RequestRewardedAd(bool autoRetry = true)
        {
            RequestRewardedAd(_rewardedAdInfo.AdUnitId, autoRetry);
        }
        public void RequestRewardedAd(string adUnitId, bool autoRetry = true)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
            {
                adInfo = new RewardedAdInfo()
                {
                    AdUnitId = adUnitId
                };
                _allVideoAds.Add(adUnitId, adInfo);
            }
            adInfo.AutoRetry = autoRetry;

            if (adInfo.AdState == FullscreenAdState.Loading)
            {
                Debug.Log("[MAXAdManager] Waiting for last rewarded ad response!");
                return;
            }

            if (adInfo.AdState == FullscreenAdState.Loaded)
            {
                Debug.Log("[MAXAdManager] Use last rewarded ad response!");
                return;
            }

            if (adInfo.RequestAdCoroutine != null)
                return;

            adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateRewardedAd(adInfo));
        }

        public IEnumerator WaitForRewardedAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
            {
                cb(false, PollFullscreenAdErrorCode.NotRequested);
                yield break;
            }

            yield return WaitForVideoAdLoadedAndCallback(adInfo, cb, timeout);
        }
        public bool IsRewardedAdLoaded()
        {
            return IsRewardedAdLoaded(_rewardedAdInfo.AdUnitId);
        }
        public bool IsRewardedAdLoaded(string adUnitId)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return false;

            return adInfo.AdState == FullscreenAdState.Loaded;
        }
        public void ShowRewardedAd(string placemendId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreLoad = true)
        {
            ShowRewardedAd(placemendId, _rewardedAdInfo.AdUnitId, cbRewardEarned, cbClosed, autoPreLoad);
        }
        public void ShowRewardedAd(string placemendId, string adUnitId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreLoad = true)
        {
            VideoAdInfo adInfo;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
            {
                cbClosed(false);
                return;
            }
            adInfo.AutoPreload = autoPreLoad;

            (adInfo as RewardedAdInfo).OnRewardedEarned = cbRewardEarned;
            _videoAdPlacementId = string.IsNullOrEmpty(placemendId) ? "null" : placemendId;
            ShowVideoAd(adInfo, _videoAdPlacementId, cbClosed);
        }

        #region Banner Internal
        IEnumerator CreateBanner()
        {
            while (!_isInitialized)
                yield return null;

            if (IsBannerAdCreated)
                MaxSdk.DestroyBanner(BannerAdId);

            if (_testDevices != null && _testDevices.Length > 0)
                MaxSdk.SetTestDeviceAdvertisingIdentifiers(_testDevices);
            Vector2 bannerSize = CalculateBannerSize();
#if USE_AMAZON && !UNITY_EDITOR
            var apsBanner = new APSBannerAdRequest((int)bannerSize.x, (int)bannerSize.y, AmazonBannerSlotId);
            apsBanner.onSuccess += (adResponse) =>
            {
                MaxSdk.SetBannerLocalExtraParameter(BannerAdId, "amazon_ad_response", adResponse.GetResponse());
                CreateBannerView(bannerSize);
                IsBannerAdCreated = true;
                MaxSdk.ShowBanner(BannerAdId);
            };
            apsBanner.onFailedWithError += (adError) =>
            {
                MaxSdk.SetBannerLocalExtraParameter(BannerAdId, "amazon_ad_error", adError.GetAdError());
                CreateBannerView(bannerSize);
                IsBannerAdCreated = true;
                MaxSdk.ShowBanner(BannerAdId);
            };
            apsBanner.LoadAd();
#else
            CreateBannerView(bannerSize);
            IsBannerAdCreated = true;
            MaxSdk.ShowBanner(BannerAdId);
#endif

            if (_bannerIsAskedToBeHidden || _bannerIsHiddenByAppPerf)
                MaxSdk.HideBanner(BannerAdId);
        }

        void OnAppPaused(bool pausedStatus)
        {
#if UNITY_IOS
            if (pausedStatus && IsBannerAdActive)
            {
                MaxSdk.HideBanner(BannerAdId);
            }
            else if (!pausedStatus && IsBannerAdActive)
            {
                _appService.StartCoroutine(LazyResumeBanner());
            }
#endif
        }

        void OnAppPerfChanged(bool lowPerf)
        {
            if (lowPerf)
            {
                Debug.LogWarningFormat("[MAXAdManager] OnAppPerfChanged({0}) - Hide banner ad!", lowPerf);
                _bannerIsHiddenByAppPerf = true;

                if (IsBannerAdCreated)
                    MaxSdk.HideBanner(BannerAdId);
            }
            else if (!lowPerf && _bannerIsHiddenByAppPerf)
            {
                Debug.LogWarningFormat("[MAXAdManager] OnAppPerfChanged({0}) - Show banner ad!", lowPerf);
                _bannerIsHiddenByAppPerf = false;

                if (IsBannerAdCreated && !_bannerIsAskedToBeHidden)
                    MaxSdk.ShowBanner(BannerAdId);
            }
        }

        IEnumerator LazyResumeBanner()
        {
            yield return new WaitForSeconds(0.5f);

            if (!_appService.IsAppPaused && !_bannerIsHiddenByAppPerf && !_bannerIsAskedToBeHidden)
            {
                MaxSdk.ShowBanner(BannerAdId);
            }
        }

        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _bannerAdLoaded = true;
            _requestBannerCoroutine = null;

            AdEventsListener?.OnBannerAdRefresh(_bannerAdPlacement, adUnitId, adInfo.NetworkName, (float)adInfo.Revenue);

            if (_bannerIsAskedToBeHidden || _bannerIsHiddenByAppPerf)
                MaxSdk.HideBanner(BannerAdId);
#if UNITY_ANDROID
            else
            {
                _appService.StartCoroutine(_FixedAndroidBannerAutoHidden());
            }
#endif
        }

#if UNITY_ANDROID
        IEnumerator _FixedAndroidBannerAutoHidden()
        {
            yield return new WaitForSeconds(0.5f);
            MaxSdk.HideBanner(BannerAdId);
            MaxSdk.ShowBanner(BannerAdId);
        }
#endif

        private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            _bannerAdLoaded = false;
            _requestBannerCoroutine = null;

            AdEventsListener?.OnBannerAdFailedToLoad(adUnitId, errorInfo.Message);
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdEventsListener?.OnBannerAdClicked(_bannerAdPlacement, adUnitId, adInfo == null ? string.Empty : adInfo.NetworkName);
        }

        Vector2 CalculateBannerSize()
        {
            if (_isPaddingBannerAd && (_bannerPaddingX != 0 || _bannerPaddingY != 0))
            {
                var scale = NativeHelper.GetDeviceNativeScale();
                var bannerW = (int)(Screen.width / scale - 2 * _bannerPaddingX);
                if (_isUsingAdaptiveBanner)
                {
                    float adaptiveH = MaxSdkUtils.GetAdaptiveBannerHeight(bannerW);
                    return new Vector2(bannerW, adaptiveH);
                }

                var bannerH = AthenaGameOpsUtils.DefaultBannerHeight(_isIPad);
                return new Vector2((int)bannerW, bannerH);
            }

            return BannerSize();
        }

        Vector2 BannerSize()
        {
            var width = Screen.width / NativeHelper.GetDeviceNativeScale();
            if (_isUsingAdaptiveBanner)
                return new Vector2(width, MaxSdkUtils.GetAdaptiveBannerHeight());

            return new Vector2(width, AthenaGameOpsUtils.DefaultBannerHeight(_isIPad));
        }

        void CreateBannerView(Vector2 bannerSize)
        {
            var scale = NativeHelper.GetDeviceNativeScale();

#if UNITY_IOS
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.safeArea.yMax - Screen.safeArea.yMin;
#elif UNITY_ANDROID
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.height - Screen.safeArea.yMin;
#endif
            var paddingBannerAd = _isPaddingBannerAd && (_bannerPaddingX != 0 || _bannerPaddingY != 0);
            if (_isUsingAdaptiveBanner && paddingBannerAd)
            {
                var bannerNativePositionY = (int)(screenBottomY / scale) - bannerSize.y - _bannerPaddingY;
                BannerPositionInPixels = new Vector2(_bannerPaddingX * scale, (bannerSize.y + _bannerPaddingY) * scale + Screen.safeArea.yMin);

                MaxSdk.CreateBanner(BannerAdId, new MaxSdkBase.AdViewConfiguration(_bannerPaddingX, bannerNativePositionY));
                MaxSdk.SetBannerExtraParameter(BannerAdId, "adaptive_banner", "true");
                MaxSdk.SetBannerPlacement(BannerAdId, _bannerAdPlacement);
                SetBannerWidth(bannerSize.x);
            }
            else if (_isUsingAdaptiveBanner && !paddingBannerAd)
            {
                MaxSdk.CreateBanner(BannerAdId, new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter));
                MaxSdk.SetBannerExtraParameter(BannerAdId, "adaptive_banner", "true");
                MaxSdk.SetBannerPlacement(BannerAdId, _bannerAdPlacement);
                BannerPositionInPixels = new Vector2(0, bannerSize.y * scale + Screen.safeArea.yMin);
            }
            else if (paddingBannerAd)
            {
                var bannerNativePositionY = (int)(screenBottomY / scale) - bannerSize.y - _bannerPaddingY;
                BannerPositionInPixels = new Vector2(_bannerPaddingX * scale, (bannerSize.y + _bannerPaddingY) * scale + Screen.safeArea.yMin);
                MaxSdk.CreateBanner(BannerAdId, new MaxSdkBase.AdViewConfiguration(_bannerPaddingX, bannerNativePositionY));
                MaxSdk.SetBannerPlacement(BannerAdId, _bannerAdPlacement);
                SetBannerWidth(bannerSize.x);
            }
            else
            {
                MaxSdk.CreateBanner(BannerAdId, new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter));
                MaxSdk.SetBannerPlacement(BannerAdId, _bannerAdPlacement);
                BannerPositionInPixels = new Vector2(0, AthenaGameOpsUtils.DefaultBannerHeight(_isIPad) * scale + Screen.safeArea.yMin);
            }

            if (_isUsingAdaptiveBanner && _safeBannerTopY != 0 && (int)BannerPositionInPixels.y > _safeBannerTopY)
            {
                var paddingY = _isPaddingBannerAd && _bannerPaddingY != 0 ? _bannerPaddingY : 0;
                var maxHeight = (_safeBannerTopY - Screen.safeArea.yMin) / scale - paddingY;

                MaxSdk.DestroyBanner(BannerAdId);
                CreateBannerViewInSafeApplicationArea(bannerSize, maxHeight);
            }
        }

        void CreateBannerViewInSafeApplicationArea(Vector2 realSize, float maxHeight)
        {
#if UNITY_IOS
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.safeArea.yMax - Screen.safeArea.yMin;
#elif UNITY_ANDROID
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.height - Screen.safeArea.yMin;
#endif
            var scale = NativeHelper.GetDeviceNativeScale();
            var realRatio = realSize.y / realSize.x;
            var adjustW = maxHeight / realRatio;
            var adaptiveHeight = MaxSdkUtils.GetAdaptiveBannerHeight(adjustW);
            var finalW = adaptiveHeight / realRatio;
            var bannerNativePositionY = (int)(screenBottomY / scale) - adaptiveHeight - _bannerPaddingY;
            BannerPositionInPixels = new Vector2(_bannerPaddingX * scale, (adaptiveHeight + _bannerPaddingY) * scale + Screen.safeArea.yMin);

            var finalX = (Screen.width / scale - finalW) / 2f;
            MaxSdk.CreateBanner(BannerAdId, new MaxSdkBase.AdViewConfiguration(finalX, bannerNativePositionY));
            MaxSdk.SetBannerExtraParameter(BannerAdId, "adaptive_banner", "true");
            MaxSdk.SetBannerPlacement(BannerAdId, _bannerAdPlacement);
            SetBannerWidth(finalW);
        }

        void SetBannerWidth(float width)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _bannerWidth = width;
            _activity.Call("runOnUiThread", new AndroidJavaRunnable(setBannerWidthOnUIThread));
#else
            MaxSdk.SetBannerWidth(BannerAdId, width);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void setBannerWidthOnUIThread()
        {
            MaxSdk.SetBannerWidth(BannerAdId, _bannerWidth);
        }
#endif
        #endregion

        #region Interstitial Internal
        private VideoAdType GetInterAdType(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId))
            {
                return VideoAdType.InterNormal;
            }
            if (_adBreakInfo != null && (adUnitId == _adBreakInfo.AdUnitId))
            {
                return VideoAdType.InterAdBreak;
            }
            if (_coldStartAdInfo != null && (adUnitId == _coldStartAdInfo.AdUnitId))
            {
                return VideoAdType.InterColdStart;
            }
            if (_softLaunchAdInfo != null && (adUnitId == _softLaunchAdInfo.AdUnitId))
            {
                return VideoAdType.InterSoftLaunch;
            }
            return VideoAdType.InterNormal;
        }
        IEnumerator CreateInterstitial(VideoAdInfo adInfo, float delay = 0f)
        {
            adInfo.AdState = FullscreenAdState.Loading;

            while (!_isInitialized)
                yield return null;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            Debug.Log("[MAXAdManager] Requesting interstitial ad...");

#if UNITY_IOS || UNITY_ANDROID
            AdEventsListener?.OnInterstitialAdStartLoading(adInfo.AdUnitId);
            bool isMuted = _appService.ShouldMuteVideoAd();
            if (!isMuted)
            {
                NativeHelper.CheckSoundSwitchStatus();
                while (NativeHelper.IsCheckingSoundSwitch())
                    yield return null;

                isMuted = NativeHelper.IsMuted();
            }
            if (_testDevices != null && _testDevices.Length > 0)
                MaxSdk.SetTestDeviceAdvertisingIdentifiers(_testDevices);
            MaxSdk.SetMuted(isMuted);
#if USE_AMAZON && !UNITY_EDITOR
            if (adInfo.IsFirstLoadAd)
            {
                adInfo.IsFirstLoadAd = false;
                APSVideoAdRequest interstitialAd = new APSVideoAdRequest((int)Screen.width, (int)Screen.height, AmazonInterstitialSlotId);
                interstitialAd.onSuccess += (adResponse) =>
                {
                    MaxSdk.SetInterstitialLocalExtraParameter(adInfo.AdUnitId, "amazon_ad_response", adResponse.GetResponse());
                    MaxSdk.LoadInterstitial(adInfo.AdUnitId);
                };
                interstitialAd.onFailedWithError += (adError) =>
                {
                    MaxSdk.SetInterstitialLocalExtraParameter(adInfo.AdUnitId, "amazon_ad_error", adError.GetAdError());
                    MaxSdk.LoadInterstitial(adInfo.AdUnitId);
                };
                interstitialAd.LoadAd();
            }
            else
            {
                MaxSdk.LoadInterstitial(adInfo.AdUnitId);
            }
#else
            MaxSdk.LoadInterstitial(adInfo.AdUnitId);
#endif
#endif
            adInfo.LoadedAdWithMutedSound = isMuted;
            adInfo.RequestAdCoroutine = null;
        }

        void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdEventsListener?.OnInterstitialAdLoaded(adUnitId, maxAdInfo == null ? string.Empty : maxAdInfo.NetworkName);

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            // Reset retry attempt
            adInfo.RetryAttempt = 0;

            // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(adUnitId) will now return 'true'
            adInfo.AdState = FullscreenAdState.Loaded;
        }

        void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            VideoAdType adType = GetInterAdType(adUnitId);
            AdEventsListener?.OnInterstitialAdFailedToLoad(adUnitId, errorInfo.Message, adType);

            // Interstitial ad failed to load 
            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            adInfo.AdState = FullscreenAdState.LoadFailed;

            if (adInfo.AutoRetry)
            {
                // We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds)
                adInfo.RetryAttempt++;
                var retryDelay = Mathf.Pow(2, Mathf.Min(6, adInfo.RetryAttempt));

                adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(adInfo, retryDelay));
            }
        }
        private void OnBannerPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdEventsListener?.OnMaxSdkBannerPaid();
            LogAdRevenue(adInfo, "banner");
        }
        private void OnInterstitialPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            VideoAdType adType = GetInterAdType(adUnitId);
            AdEventsListener?.OnMaxSdkInterstitialPaid(adInfo.NetworkName, adType);
            LogAdRevenue(adInfo, "interstitial");
        }
        private void OnRewardedAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdEventsListener?.OnMaxSdkRewardedAdPaid();
            LogAdRevenue(adInfo, "rewarded_video");
        }
        private void OnMrecRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogAdRevenue(adInfo, "Mrec");
        }
        private void OnAppOpenRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogAdRevenue(adInfo, "AppOpen");
        }
        private void LogAdRevenue(MaxSdkBase.AdInfo adInfo, string adFormat)
        {
            AdEventsListener?.OnLogAdRevenue(adInfo.Revenue, adInfo.NetworkName, adInfo.AdUnitIdentifier, adInfo.Placement);
            AdEventsListener?.OnLogBIAdValueMAX(adFormat, adInfo.Placement, adInfo.AdUnitIdentifier, adInfo.NetworkName, adInfo.Revenue, "USD", GetPrecision(adInfo.RevenuePrecision));
            AdEventsListener?.OnLogAdImpression("AppLovin", adInfo.NetworkName, adInfo.AdUnitIdentifier, adInfo.AdFormat, "USD", adInfo.Revenue, adInfo.Placement);
        }
        void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            VideoAdType adType = GetInterAdType(adUnitId);
            AdEventsListener?.OnInterstitialAdShow(_interstitialPlacementId, adUnitId, maxAdInfo.NetworkName, (float)maxAdInfo.Revenue, adType);
        }

        void OnInterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo maxAdInfo)
        {
            // Interstitial ad failed to display.
            IsPlayingFullscreenlAds = false;

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;
            VideoAdType adType = GetInterAdType(adUnitId);
            AdEventsListener?.OnInterstitialFailedToDisplay(adType, errorInfo.Message);
            adInfo.OnAdClosed?.Invoke(false);
            adInfo.OnAdClosed = null;

            if (adInfo.AutoPreload)
            {
                // We recommend loading the next ad
                adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(adInfo));
            }
        }

        void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            // Interstitial ad is hidden.
            IsPlayingFullscreenlAds = false;

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            AdEventsListener?.OnInterstitialAdClosed(_interstitialPlacementId, adUnitId, adInfo.AdType);

            adInfo.OnAdClosed?.Invoke(true);
            adInfo.OnAdClosed = null;

            if (adInfo.AutoPreload)
            {
                // Pre-load the next ad
                RequestInterstitial(adUnitId, adInfo.AutoRetry);
            }
        }

        void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            VideoAdType adType = GetInterAdType(adUnitId);
            AdEventsListener?.OnInterstitialAdClicked(_interstitialPlacementId, adUnitId, maxAdInfo.NetworkName, adType);
        }
        #endregion
        private int GetPrecision(string precision)
        {
            if (precision.Equals("publisher_defined"))
            {
                return 2;
            }
            else if (precision.Equals("exact"))
            {
                return 3;
            }
            else if (precision.Equals("estimated"))
            {
                return 1;
            }
            return 0;
        }
        #region RewardedAd Internal
        IEnumerator CreateRewardedAd(VideoAdInfo adInfo, float delay = 0f)
        {
            adInfo.AdState = FullscreenAdState.Loading;

            while (!_isInitialized)
                yield return null;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            Debug.Log("[MAXAdManager] Requesting rewarded ad...");

#if UNITY_IOS || UNITY_ANDROID
            AdEventsListener?.OnRewardedAdStartLoading(adInfo.AdUnitId);

            bool isMuted = _appService.ShouldMuteVideoAd();
            if (!isMuted)
            {
                NativeHelper.CheckSoundSwitchStatus();
                while (NativeHelper.IsCheckingSoundSwitch())
                    yield return null;

                isMuted = NativeHelper.IsMuted();
            }

            if (_testDevices != null && _testDevices.Length > 0)
                MaxSdk.SetTestDeviceAdvertisingIdentifiers(_testDevices);
            MaxSdk.SetMuted(isMuted);
#if USE_AMAZON && !UNITY_EDITOR
            if (adInfo.IsFirstLoadAd)
            {
                adInfo.IsFirstLoadAd = false;
                Vector2 bannerSize = CalculateBannerSize();
                APSVideoAdRequest interstitialVideoAd = new APSVideoAdRequest((int)Screen.width, (int)Screen.height, AmazonRewardedVideoSlotId);
                interstitialVideoAd.onSuccess += (adResponse) =>
                {
                    MaxSdk.SetRewardedAdLocalExtraParameter(adInfo.AdUnitId, "amazon_ad_response", adResponse.GetResponse());
                    MaxSdk.LoadRewardedAd(adInfo.AdUnitId);
                };
                interstitialVideoAd.onFailedWithError += (adError) =>
                {
                    MaxSdk.SetRewardedAdLocalExtraParameter(adInfo.AdUnitId, "amazon_ad_error", adError.GetAdError());
                    MaxSdk.LoadRewardedAd(adInfo.AdUnitId);
                };
                interstitialVideoAd.LoadAd();
            }
            else
            {
                MaxSdk.LoadRewardedAd(adInfo.AdUnitId);
            }
#else
            MaxSdk.LoadRewardedAd(adInfo.AdUnitId);
#endif
#endif
            adInfo.LoadedAdWithMutedSound = isMuted;
            adInfo.RequestAdCoroutine = null;
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdEventsListener?.OnRewardedAdLoaded(adUnitId, maxAdInfo == null ? string.Empty : maxAdInfo.NetworkName);

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            // Reset retry attempt
            adInfo.RetryAttempt = 0;

            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(adUnitId) will now return 'true'
            adInfo.AdState = FullscreenAdState.Loaded;
        }

        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            AdEventsListener?.OnRewardedAdFailedToLoad(adUnitId, errorInfo.Message);

            // Rewarded ad failed to load 
            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            adInfo.AdState = FullscreenAdState.LoadFailed;

            if (adInfo.AutoRetry)
            {
                // We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds)
                adInfo.RetryAttempt++;
                var retryDelay = Mathf.Pow(2, Mathf.Min(6, adInfo.RetryAttempt));

                adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateRewardedAd(adInfo, retryDelay));
            }
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo maxAdInfo)
        {
            // Rewarded ad failed to display.
            IsPlayingFullscreenlAds = false;

            AdEventsListener?.OnRewardedAdFailedToShow(_videoAdPlacementId, adUnitId, maxAdInfo.NetworkName);

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            if (adInfo.AutoPreload)
            {
                // We recommend loading the next ad
                adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateRewardedAd(adInfo));
            }
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdEventsListener?.OnRewardedAdShow(_videoAdPlacementId, adUnitId, maxAdInfo.NetworkName);
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo) { }

        private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo maxAdInfo)
        {
            // Rewarded ad is hidden.
            IsPlayingFullscreenlAds = false;

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            AdEventsListener?.OnRewardedAdClosed(_videoAdPlacementId, adUnitId);

            adInfo.OnAdClosed?.Invoke(true);
            adInfo.OnAdClosed = null;

            if (adInfo.AutoPreload)
            {
                // Pre-load the next ad
                RequestRewardedAd(adUnitId, adInfo.AutoRetry);
            }
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo maxAdInfo)
        {
            AdEventsListener?.OnRewardedAdUserEarned(_videoAdPlacementId, adUnitId, maxAdInfo.NetworkName, (float)maxAdInfo.Revenue);

            VideoAdInfo adInfo = null;
            if (!_allVideoAds.TryGetValue(adUnitId, out adInfo))
                return;

            // Rewarded ad was displayed and user should receive the reward
            (adInfo as RewardedAdInfo).OnRewardedEarned?.Invoke(reward.Label, reward.Amount);
        }
        #endregion

        #region VideoAd Internal
        IEnumerator CheckSoundSwitch(System.Action<bool> cb)
        {
            NativeHelper.CheckSoundSwitchStatus();
            while (NativeHelper.IsCheckingSoundSwitch())
                yield return null;

            bool isMuted = NativeHelper.IsMuted();
            cb(isMuted);
        }

        void ShowVideoAd(VideoAdInfo adInfo, string placemendId, System.Action<bool> cb)
        {
            if (_checkingSoundSwitch != null)
            {
                cb(false);
                return;
            }

            _appService.SetAppInputActive(false);
            if (_appService.ShouldMuteVideoAd())
            {
                PlayVideoAd(adInfo, placemendId, true, (success) =>
                {
                    _appService.SetAppInputActive(true);
                    cb(success);
                });
            }
            else
            {
                _checkingSoundSwitch = _appService.StartCoroutine(CheckSoundSwitch((isMuted) =>
                {
                    _checkingSoundSwitch = null;
                    PlayVideoAd(adInfo, placemendId, isMuted, (success) =>
                    {
                        _appService.SetAppInputActive(true);
                        cb(success);
                    });
                }));
            }
        }

        void PlayVideoAd(VideoAdInfo adInfo, string placementId, bool isMuted, System.Action<bool> cb)
        {
#if UNITY_IOS || UNITY_ANDROID
            if (adInfo.IsReady())
            {
                IsPlayingFullscreenlAds = true;
                adInfo.AdState = FullscreenAdState.Used;
                adInfo.OnAdClosed = cb;
                _videoAdPlacementId = placementId;

                MaxSdk.SetMuted(isMuted);
                adInfo.Show(placementId);
            }
            else
            {
                Debug.LogWarning("[MAXAdManager] VideoAd is not loaded yet!");
                cb(false);
            }
#endif
        }

        IEnumerator WaitForVideoAdLoadedAndCallback(VideoAdInfo adInfo, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            var t = 0f;
            while (true)
            {
                if (adInfo.AdState == FullscreenAdState.LoadFailed
                || adInfo.AdState == FullscreenAdState.Loaded)
                    break;

                yield return null;

                if (timeout > 0)
                {
                    t += Time.deltaTime;
                    if (t >= timeout)
                        break;
                }
            }

            var success = adInfo.AdState == FullscreenAdState.Loaded;
            cb(success, success ? PollFullscreenAdErrorCode.Success : (adInfo.AdState == FullscreenAdState.LoadFailed ? PollFullscreenAdErrorCode.Failed : PollFullscreenAdErrorCode.Timeout));
        }
        #endregion
    }
}
#endif