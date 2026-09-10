using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_ADMOB_MEDIATION
using GoogleMobileAds.Api;
#endif

namespace Delta.GameOps
{
    public enum PollFullscreenAdErrorCode
    {
        Success = 0,
        NotRequested = 1,
        Failed = 2,
        Timeout = 3
    }

    public interface IAdEventsListener
    {
        void OnBannerAdRefresh(string adPlacement, string adUnitId, string adNetwork, float MAXEstValueUSD);
        void OnBannerAdFailedToLoad(string adUnitId, string errorMessage);
        void OnBannerAdClicked(string adPlacement, string adUnitId, string adNetwork);
        void OnBannerAdPaid(string adPlacement, string adUnitId, string adNetwork, long value, string currencyCode, int precision);

        void OnInterstitialAdStartLoading(string adUnitId);
        void OnInterstitialAdLoaded(string adUnitId, string adNetwork);
        void OnInterstitialAdShow(string placementId, string adUnitId, string adNetwork, float MAXEstValueUSD, VideoAdType adType);
        void OnInterstitialFailedToDisplay(VideoAdType adType, string errorMgs);
        void OnInterstitialAdClosed(string placemendId, string adUnitId, VideoAdType adType);
        void OnInterstitialAdFailedToLoad(string adUnitId, string errorMessage, VideoAdType adType);
        void OnInterstitialAdClicked(string placemendId, string adUnitId, string adNetwork, VideoAdType adType);
        void OnInterstitialAdPaid(string placemendId, string adUnitId, string adNetwork, long value, string currencyCode, int precision);

        void OnRewardedAdStartLoading(string adUnitId);
        void OnRewardedAdLoaded(string adUnitId, string adNetwork);
        void OnRewardedAdShow(string placemendId, string adUnitId, string adNetwork);
        void OnRewardedAdClosed(string placemendId, string adUnitId);
        void OnRewardedAdUserEarned(string placemendId, string adUnitId, string adNetwork, float MAXEstValueUSD);
        void OnRewardedAdFailedToLoad(string adUnitId, string errorMessage);
        void OnRewardedAdFailedToShow(string placemendId, string adUnitId, string adNetwork);
        void OnRewardedAdPaid(string adPlacement, string adUnitId, string adNetwork, long value, string currencyCode, int precision);
        void OnLogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement);
        void OnLogBIAdValueMAX(string adFormat, string adPlacement, string adUnitId, string networkName, double revenue, string currency = "USD", int precision = 0);
        void OnLogAdImpression(string adPlatform, string networkName, string adUnitId, string adFormat, string currency, double revenue, string adPlacement = null);
        void OnMaxSdkInterstitialPaid(string networkName, VideoAdType adType);
        void OnMaxSdkRewardedAdPaid();
        void OnMaxSdkBannerPaid();

#if USE_ADMOB_MEDIATION
        void BuildInterstitialAdRequest(AdRequest.Builder builder, bool isMuted);
        void BuildBannerAdRequest(AdRequest.Builder builder);
        void BuildRewardedAdRequest(AdRequest.Builder builder, bool isMuted);
#endif
    }

#if USE_ADMOB_MEDIATION
    public class AdManager : RewardedAdListener, InterstitialAdListener, IAdManager
    {
        bool _isIPad;
        const int DURATION_FULLSCREEN_AD_PAID_EVENT_EXPIRED = 5;

        public bool IsMobileAdsInitialized { get { return _initializedMobileAds; } }

        public bool IsPlayingFullscreenlAds { get { return _isPlayingFullscreenAd; } }
        public bool IsInterstitialAdFailedToLoad { get { return _interstitialAdInfo.AdState == FullscreenAdState.LoadFailed; } }
        public bool IsInterstitialAdLoaded { get { return _interstitialAdInfo.AdState == FullscreenAdState.Loaded; } }

        public bool CheckInterstitialAdLoaded(string adUnitId)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
                return false;

            return adInfo.AdState == FullscreenAdState.Loaded;
        }

        public bool CheckInterstitialAdFailedToLoad(string adUnitId)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
                return false;

            return adInfo.AdState == FullscreenAdState.LoadFailed;
        }

        public Vector2 BannerPositionInPixels { get; private set; }
        public bool IsBannerAdCreated { get; private set; }
        public bool IsBannerAdActive { get { return IsBannerAdCreated && _bannerAdLoaded; } }
        public bool IsBannerDeactivated { get { return _bannerIsAskedToBeHidden; } }
        public bool IsPaddingBannerSupported { get { return true; } }

        public string BannerAdId { get { return _bannerAdId; } }
        public string BannerPlacementId
        {
            get { return _bannerAdPlacement; }
            set { _bannerAdPlacement = value; }
        }
        public string InterstitialAdId { get { return _interstitialAdInfo.AdUnitId; } }
        public string ColdsStartAdId { get { return _coldStartAdInfo.AdUnitId; } }

        public bool IsPaddingBannerAd { get { return _isPaddingBannerAd; } }
        public int PaddingBannerX { get { return _bannerPaddingX; } }
        public int PaddingBannerY { get { return _bannerPaddingY; } }
        public bool IsUsingAdaptiveBanner { get { return _isUsingAdaptiveBanner; } }

        public event System.Action OnBannerDidUpdateConfigs;
        public IAdEventsListener AdEventsListener { get; set; }

        string _bannerAdId = "unused";
        string _bannerAdPlacement = string.Empty;
        int _safeBannerTopY;

        Coroutine _checkingSoundSwitch;
        bool _isPlayingFullscreenAd;
        string _fullscreenAdPlacementId;

        InterstitialAdInfo _interstitialAdInfo;
        InterstitialAdInfo _softLaunchAdInfo;
        InterstitialAdInfo _coldStartAdInfo;
        InterstitialAdInfo _adBreakInfo;
        RewardedAdInfo _rewardedAdInfo;
        Dictionary<string, FullscreenAdInfo> _allFullscreenAds = new Dictionary<string, FullscreenAdInfo>();

        bool _initializedMobileAds;
        IMainAppService _appService;

        BannerView _bannerView;
        string _bannerAdNetworkName;
        bool _bannerAdLoaded;
        bool _shouldUpdateBannerAdNetworkName;
        Coroutine _requestBannerCoroutine;
        AdValue _pendingBannerPaidValue;
        bool _paidValueIsReady;
        bool _bannerIsHiddenByAppPerf;
        bool _bannerIsAskedToBeHidden;

        bool _isUsingAdaptiveBanner;
        bool _isPaddingBannerAd;
        int _bannerPaddingX = 0;
        int _bannerPaddingY = 0;
        System.Action _onSDKInitialized;

        public AdManager(System.Action onSDKInitialized, bool enablePaddingBanner, bool usingAdaptiveBanner, string bannerAdId, string interstitialAdId,
        string softLaunchAdId, string coldStartAdId, string adBreakId, string rewardedAdId, IMainAppService appService,
            bool hideBannerAdWhenAppPaused = false, bool hideBannerAdWhenAppLowPerf = false, bool iOSAppPausedOnBackground = false)
        {
            _onSDKInitialized = onSDKInitialized;
            _isPaddingBannerAd = enablePaddingBanner;
            _isUsingAdaptiveBanner = usingAdaptiveBanner;
            _appService = appService;

            NativeHelper.InitAdClosedObserver();

            _bannerAdId = bannerAdId;
            BannerPlacementId = string.Empty;
            if (!string.IsNullOrEmpty(interstitialAdId))
            {
                _interstitialAdInfo = new InterstitialAdInfo()
                {
                    AdUnitId = interstitialAdId,
                    adType = VideoAdType.InterNormal,
                    Listener = this
                };
                _allFullscreenAds.Add(interstitialAdId, _interstitialAdInfo);
            }
            if (!string.IsNullOrEmpty(softLaunchAdId))
            {
                _softLaunchAdInfo = new InterstitialAdInfo()
                {
                    AdUnitId = softLaunchAdId,
                    adType = VideoAdType.InterSoftLaunch,
                    Listener = this
                };
                _allFullscreenAds.Add(softLaunchAdId, _softLaunchAdInfo);
            }
            if (!string.IsNullOrEmpty(coldStartAdId))
            {
                _coldStartAdInfo = new InterstitialAdInfo()
                {
                    AdUnitId = coldStartAdId,
                    adType = VideoAdType.InterColdStart,
                    Listener = this
                };
                _allFullscreenAds.Add(coldStartAdId, _coldStartAdInfo);

            }
            if (!string.IsNullOrEmpty(adBreakId))
            {
                _adBreakInfo = new InterstitialAdInfo()
                {
                    AdUnitId = adBreakId,
                    adType = VideoAdType.InterAdBreak,
                    Listener = this
                };
                _allFullscreenAds.Add(adBreakId, _adBreakInfo);
            }
            if (!string.IsNullOrEmpty(rewardedAdId))
            {
                _rewardedAdInfo = new RewardedAdInfo()
                {
                    AdUnitId = rewardedAdId,
                    adType = VideoAdType.RewardedAd,
                    Listener = this
                };
                _allFullscreenAds.Add(rewardedAdId, _rewardedAdInfo);
            }

            MobileAds.SetiOSAppPauseOnBackground(iOSAppPausedOnBackground);
            MobileAds.Initialize(initStatus =>
            {
                _initializedMobileAds = true;
                _onSDKInitialized?.Invoke();
            });

#if UNITY_IOS
            _isIPad = SystemInfo.deviceModel.Contains("iPad");

            if (hideBannerAdWhenAppPaused)
                _appService.SubscribeAppPause(OnAppPaused);

            if (hideBannerAdWhenAppLowPerf)
                _appService.SubscribeAppPerfChanged(OnAppPerfChanged);
#endif

            _appService.StartCoroutine(ThreadSafePollingBannerAdNetwork());
        }
        public void ShowDebuggerPanel()
        {

        }

        public void CleanUp()
        {

        }

        public void SetTestDevices(string[] testDevices)
        {
            RequestConfiguration requestConfiguration = new RequestConfiguration.Builder().SetTestDeviceIds(new List<string>(testDevices)).build();
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        #region Banner Ad
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

            if (_bannerView != null)
            {
                _bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
                _bannerView.OnBannerAdLoadFailed -= OnBannerAdFailedToLoad;
                _bannerView.OnAdClicked -= OnBannerAdClicked;
                _bannerView.OnAdPaid -= OnBannerAdPaid;
                _bannerView.Destroy();
            }
        }
        public void DeactiveBannerAd()
        {
            _bannerIsAskedToBeHidden = true;

            if (IsBannerAdCreated)
            {
                _bannerView.Hide();
            }
        }

        public void ActiveBannerAd()
        {
            _bannerIsAskedToBeHidden = false;

            if (IsBannerAdCreated)
            {
                if (!_bannerIsHiddenByAppPerf)
                {
                    _bannerView.Show();
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

            if (_bannerView != null)
            {
                Debug.Log("[AdManager] Banner ad configs are updated!");

                _bannerAdLoaded = false;
                IsBannerAdCreated = false;
                RequestBanner();

                OnBannerDidUpdateConfigs?.Invoke();
            }
        }
        #endregion

        #region Interstitial Ad
        public bool IsInterstitialReady()
        {
            return _interstitialAdInfo.InterstitialAd.CanShowAd();
        }
        public bool IsSoftLaunchAdReady()
        {
            return _softLaunchAdInfo.InterstitialAd.CanShowAd();
        }
        public bool IsColdStartAdReady()
        {
            return _coldStartAdInfo.InterstitialAd.CanShowAd();
        }
        public bool IsAdBreakReady()
        {
            return _adBreakInfo.InterstitialAd.CanShowAd();
        }
        public void RequestColdStartAd(bool autoRetry = true)
        {
            LoadInterstitialAd(_coldStartAdInfo.AdUnitId, autoRetry);
        }
        public void RequestSoftLaunchAd(bool autoRetry = true)
        {
            LoadInterstitialAd(_softLaunchAdInfo.AdUnitId, autoRetry);
        }
        public void RequestAdBreak(bool autoRetry = true)
        {
            LoadInterstitialAd(_adBreakInfo.AdUnitId, autoRetry);
        }
        public void ShowAdBreak(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            ShowFullscreenAd(_adBreakInfo, placemendId, (success) =>
            {
                if (success)
                    AdEventsListener?.OnInterstitialAdClosed(placemendId, _adBreakInfo.AdUnitId, VideoAdType.InterAdBreak);

                cb(success);

                if (success && autoPreLoad)
                    RequestAdBreak();
            });
        }
        public void ShowAdSoftLaunch(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            ShowFullscreenAd(_softLaunchAdInfo, placemendId, (success) =>
                {
                    if (success)
                        AdEventsListener?.OnInterstitialAdClosed(placemendId, _softLaunchAdInfo.AdUnitId, VideoAdType.InterSoftLaunch);

                    cb(success);

                    if (success && autoPreLoad)
                        RequestSoftLaunchAd();
                });
        }
        public void ShowAdColdStart(string placemendId, System.Action<bool> cb, bool autoPreLoad = true)
        {
            ShowFullscreenAd(_coldStartAdInfo, placemendId, (success) =>
                {
                    if (success)
                        AdEventsListener?.OnInterstitialAdClosed(placemendId, _coldStartAdInfo.AdUnitId, VideoAdType.InterColdStart);

                    cb(success);

                    if (success && autoPreLoad)
                        RequestColdStartAd();
                });
        }
        public void RefreshInterstitialConfigs(string interstitialAdId)
        {
            if (_interstitialAdInfo.AdUnitId != interstitialAdId && !string.IsNullOrEmpty(interstitialAdId))
            {
                _allFullscreenAds.Remove(_interstitialAdInfo.AdUnitId);
                _interstitialAdInfo.AdUnitId = interstitialAdId;
                _allFullscreenAds.Add(interstitialAdId, _interstitialAdInfo);
            }
        }

        public void RequestInterstitial(bool autoRetry = true)
        {
            LoadInterstitialAd(_interstitialAdInfo.AdUnitId, autoRetry);
        }

        public void ShowInterstitial(string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            ShowFullscreenAd(_interstitialAdInfo, placemendId, (success) =>
            {
                if (success)
                    AdEventsListener?.OnInterstitialAdClosed(placemendId, _interstitialAdInfo.AdUnitId, VideoAdType.InterAdBreak);

                cb(success);

                if (success && autoPreload)
                    RequestInterstitial();
            });
        }

        public IEnumerator WaitForInterstitialAdLoadedAndCallback(System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            yield return WaitForFullscreenAdLoadedAndCallback(_interstitialAdInfo, cb, timeout);
        }

        public void AskToReloadMutedInterstitialAd() { }
        #endregion

        #region Other interstitial
        public void RequestInterstitial(string adUnitId, bool autoRetry = true)
        {
            LoadInterstitialAd(adUnitId, autoRetry);
        }

        public void ShowInterstitialAd(string adUnitId, string placemendId, System.Action<bool> cb, bool autoPreload = true)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
            {
                cb(false);
                return;
            }

            ShowFullscreenAd(adInfo, placemendId, (success) =>
            {
                if (success)
                    AdEventsListener?.OnInterstitialAdClosed(placemendId, adUnitId, VideoAdType.InterNormal);

                cb(success);

                if (success && autoPreload)
                    RequestInterstitial(adUnitId);
            });
        }

        public IEnumerator WaitForInterstitialAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
            {
                cb(false, PollFullscreenAdErrorCode.NotRequested);
                yield break;
            }

            yield return WaitForFullscreenAdLoadedAndCallback(adInfo, cb, timeout);
        }

        public void AskToReloadMutedInterstitialAd(string adUnitId) { }
        #endregion

        #region Reward Ad
        public void RequestRewardedAd(bool autoRetry = true)
        {
            LoadRewardedAd(_rewardedAdInfo.AdUnitId, autoRetry);
        }
        public void ShowRewardedAd(string placemendId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreLoad = true)//update config to file
        {
            ShowRewardedAd(placemendId, _rewardedAdInfo.AdUnitId, cbRewardEarned, cbClosed, autoPreLoad);
        }
        public bool IsRewardedAdLoaded()
        {
            return _rewardedAdInfo.RewardedAd.CanShowAd();
        }
        public void RequestRewardedAd(string adUnitId, bool autoRetry = true)
        {
            LoadRewardedAd(adUnitId, autoRetry);
        }

        public IEnumerator WaitForRewardedAdLoadedAndCallback(string adUnitId, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
            {
                cb(false, PollFullscreenAdErrorCode.NotRequested);
                yield break;
            }

            yield return WaitForFullscreenAdLoadedAndCallback(adInfo, cb, timeout);
        }

        public bool IsRewardedAdLoaded(string adUnitId)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
                return false;

            return adInfo.AdState == FullscreenAdState.Loaded;
        }

        public void ShowRewardedAd(string placemendId, string adUnitId, System.Action<string, double> cbRewardEarned, System.Action<bool> cbClosed, bool autoPreload = true)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
            {
                cbClosed(false);
                return;
            }

            (adInfo as RewardedAdInfo).OnRewardEarned = (rewardedAdUnitId, adNetwork, type, amount) =>
            {
                AdEventsListener?.OnRewardedAdUserEarned(placemendId, rewardedAdUnitId, adNetwork, -1);
                cbRewardEarned.Invoke(type, amount);
            };

            ShowFullscreenAd(adInfo, placemendId, (success) =>
            {
                if (success)
                    AdEventsListener?.OnRewardedAdClosed(placemendId, adUnitId);

                cbClosed(success);

                if (success && autoPreload)
                    LoadRewardedAd(adInfo.AdUnitId, adInfo.AutoRetry);
            });
        }
        #endregion

        #region Banner Ad Internal
        void OnAppPaused(bool pausedStatus)
        {
#if UNITY_IOS
            if (pausedStatus && IsBannerAdActive)
            {
                _bannerView.Hide();
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
                Debug.LogWarningFormat("[AdManager] OnAppPerfChanged({0}) - Hide banner ad!", lowPerf);
                _bannerIsHiddenByAppPerf = true;

                if (IsBannerAdCreated)
                    _bannerView.Hide();
            }
            else if (!lowPerf && _bannerIsHiddenByAppPerf)
            {
                Debug.LogWarningFormat("[AdManager] OnAppPerfChanged({0}) - Show banner ad!", lowPerf);
                _bannerIsHiddenByAppPerf = false;

                if (IsBannerAdCreated && !_bannerIsAskedToBeHidden)
                    _bannerView.Show();
            }
        }

        IEnumerator LazyResumeBanner()
        {
            yield return new WaitForSeconds(0.5f);

            if (!_appService.IsAppPaused && !_bannerIsHiddenByAppPerf && !_bannerIsAskedToBeHidden)
            {
                _bannerView.Show();
            }
        }

        IEnumerator WaitRequestBanner(float delay)
        {
            yield return new WaitForSeconds(delay);

            while (!_initializedMobileAds)
                yield return null;

            RequestBanner();
        }

        IEnumerator ThreadSafePollingBannerAdNetwork()
        {
            while (true)
            {
                if (_shouldUpdateBannerAdNetworkName && _bannerView != null && _bannerAdLoaded)
                {
                    _shouldUpdateBannerAdNetworkName = false;
                    _paidValueIsReady = true;

                    _bannerAdNetworkName = _bannerView.GetResponseInfo().GetMediationAdapterClassName();
                    AdEventsListener?.OnBannerAdRefresh(_bannerAdPlacement, _bannerAdId, _bannerAdNetworkName, -1);

                    if (_pendingBannerPaidValue != null)
                    {
                        AdEventsListener?.OnBannerAdPaid(_bannerAdPlacement, _bannerAdId, _bannerAdNetworkName, _pendingBannerPaidValue.Value, _pendingBannerPaidValue.CurrencyCode, (int)_pendingBannerPaidValue.Precision);
                        _pendingBannerPaidValue = null;
                        _paidValueIsReady = false;
                    }
                }

                yield return null;
            }
        }

        IEnumerator CreateBanner()
        {
            while (!_initializedMobileAds)
                yield return null;

            if (_bannerView != null)
            {
                _bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
                _bannerView.OnBannerAdLoadFailed -= OnBannerAdFailedToLoad;
                _bannerView.OnAdClicked -= OnBannerAdClicked;
                _bannerView.OnAdPaid -= OnBannerAdPaid;
                _bannerView.Destroy();
            }

            _bannerAdLoaded = false;

            _bannerView = CreateBannerView();
            IsBannerAdCreated = true;

            var builder = new AdRequest.Builder();
            AdEventsListener?.BuildBannerAdRequest(builder);
            AdRequest adRequest = builder.Build();

            _bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
            _bannerView.OnBannerAdLoadFailed += OnBannerAdFailedToLoad;
            _bannerView.OnAdClicked += OnBannerAdClicked;
            _bannerView.OnAdPaid += OnBannerAdPaid;
            _bannerView.LoadAd(adRequest);
        }

        AdSize CalculateBannerSize()
        {
            if (_isPaddingBannerAd && (_bannerPaddingX != 0 || _bannerPaddingY != 0))
            {
                var scale = NativeHelper.GetDeviceNativeScale();
                var bannerW = (int)(Screen.width / scale - 2 * _bannerPaddingX);
                if (_isUsingAdaptiveBanner)
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(bannerW);

                var bannerH = AthenaGameOpsUtils.DefaultBannerHeight(_isIPad);
                return new AdSize((int)bannerW, bannerH);
            }

            return DefaultBannerSize();
        }

        AdSize DefaultBannerSize()
        {
            if (_isUsingAdaptiveBanner)
                return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

#if UNITY_ANDROID
            return new AdSize((int)(Screen.width / NativeHelper.GetDeviceNativeScale()), AthenaGameOpsUtils.DefaultBannerHeight(_isIPad));
#else
            return AdSize.SmartBanner;
#endif
        }

        BannerView CreateBannerView()
        {
            var scale = NativeHelper.GetDeviceNativeScale();
            var scaleBannerViewSizeToPoint = GoogleMobileAds.Api.MobileAds.Utils.GetDeviceScale();
            var bannerSize = CalculateBannerSize();
#if UNITY_IOS
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.safeArea.yMax - Screen.safeArea.yMin;
#elif UNITY_ANDROID
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.height - Screen.safeArea.yMin;
#endif

            var paddingBannerAd = _isPaddingBannerAd && (_bannerPaddingX != 0 || _bannerPaddingY != 0);

            if (_isUsingAdaptiveBanner)
            {
                var view = new BannerView(_bannerAdId, bannerSize, AdPosition.Bottom);
                if (paddingBannerAd)
                {
                    var bannerHeight = (int)(view.GetHeightInPixels() / scaleBannerViewSizeToPoint);
                    var bannerNativePositionY = (int)(screenBottomY / scale) - bannerHeight - _bannerPaddingY;
                    BannerPositionInPixels = new Vector2(_bannerPaddingX * scale, (bannerHeight + _bannerPaddingY) * scale + Screen.safeArea.yMin);
                    view.SetPosition(_bannerPaddingX, bannerNativePositionY);
                }
                else
                {
                    BannerPositionInPixels = new Vector2(0, view.GetHeightInPixels() + Screen.safeArea.yMin);
                }

                if (_safeBannerTopY != 0 && (int)BannerPositionInPixels.y > _safeBannerTopY)
                {
                    var realW = view.GetWidthInPixels() / scaleBannerViewSizeToPoint;
                    var realH = view.GetHeightInPixels() / scaleBannerViewSizeToPoint;

                    var paddingY = _isPaddingBannerAd && _bannerPaddingY != 0 ? _bannerPaddingY : 0;
                    var maxHeight = (_safeBannerTopY - Screen.safeArea.yMin) / scale - paddingY;

                    view.Destroy();
                    return CreateBannerViewInSafeApplicationArea(realW, realH, maxHeight);
                }
                return view;
            }
            else if (paddingBannerAd)
            {
                var bannerNativePositionY = (int)(screenBottomY / scale) - bannerSize.Height - _bannerPaddingY;
                BannerPositionInPixels = new Vector2(_bannerPaddingX * scale, (bannerSize.Height + _bannerPaddingY) * scale + Screen.safeArea.yMin);
                return new BannerView(_bannerAdId, bannerSize, _bannerPaddingX, bannerNativePositionY);
            }
            else
            {
                var view = new BannerView(_bannerAdId, bannerSize, AdPosition.Bottom);
                BannerPositionInPixels = new Vector2(0, AthenaGameOpsUtils.DefaultBannerHeight(_isIPad) * scale + Screen.safeArea.yMin);
                return view;
            }
        }

        BannerView CreateBannerViewInSafeApplicationArea(float realSizeW, float realSizeH, float maxHeight)
        {
            if ((int)maxHeight < 50)
                maxHeight = 50;

#if UNITY_IOS
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.safeArea.yMax - Screen.safeArea.yMin;
#elif UNITY_ANDROID
            var screenBottomY = (int)Screen.safeArea.size.x == 0 || (int)Screen.safeArea.size.y == 0 ? Screen.height : Screen.height - Screen.safeArea.yMin;
#endif

            var scale = NativeHelper.GetDeviceNativeScale();
            var scaleBannerViewSizeToPoint = GoogleMobileAds.Api.MobileAds.Utils.GetDeviceScale();
            var realRatio = realSizeH / realSizeW;
            var adjustW = maxHeight / realRatio;

            var adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth((int)adjustW);
            var adaptiveView = new BannerView(_bannerAdId, adaptiveSize, AdPosition.Bottom);
            var adaptiveHeight = adaptiveView.GetHeightInPixels() / scaleBannerViewSizeToPoint;
            var adaptiveWidth = adaptiveHeight / realRatio;
            var bannerNativePositionY = (int)(screenBottomY / scale) - adaptiveHeight - _bannerPaddingY;
            BannerPositionInPixels = new Vector2(_bannerPaddingX * scale, (adaptiveHeight + _bannerPaddingY) * scale + Screen.safeArea.yMin);
            var finalX = (Screen.width / scale - adaptiveSize.Width) / 2f;
            adaptiveView.SetPosition((int)finalX, (int)bannerNativePositionY);
            return adaptiveView;
        }

        void OnBannerAdLoaded()
        {
            _bannerAdLoaded = true;
            _shouldUpdateBannerAdNetworkName = true;
            _requestBannerCoroutine = null;

            if (_bannerIsAskedToBeHidden || _bannerIsHiddenByAppPerf)
                _bannerView.Hide();
        }

        void OnBannerAdPaid(AdValue adValue)
        {
            if (_paidValueIsReady)
            {
                AdEventsListener?.OnBannerAdPaid(_bannerAdPlacement, _bannerAdId, _bannerAdNetworkName, adValue.Value, adValue.CurrencyCode, (int)adValue.Precision);
                _paidValueIsReady = false;
            }
            else
            {
                _pendingBannerPaidValue = adValue;
            }
        }

        void OnBannerAdClicked()
        {
            AdEventsListener?.OnBannerAdClicked(_bannerAdPlacement, _bannerAdId, _bannerAdNetworkName);
        }

        void OnBannerAdFailedToLoad(LoadAdError loadAdError)
        {
            _bannerAdLoaded = false;
            _requestBannerCoroutine = null;

            string message = loadAdError.GetMessage();
            AdEventsListener?.OnBannerAdFailedToLoad(_bannerAdId, message);
        }
        #endregion

        #region FullscreenAd Internal
        void ShowFullscreenAd(FullscreenAdInfo adInfo, string placemendId, System.Action<bool> cb)
        {
            if (_checkingSoundSwitch != null)
            {
                cb(false);
                return;
            }

            _appService.SetAppInputActive(false);
            if (_appService.ShouldMuteVideoAd())
            {
                PlayFullscreenAd(adInfo, placemendId, true, (success) =>
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
                    PlayFullscreenAd(adInfo, placemendId, isMuted, (success) =>
                    {
                        _appService.SetAppInputActive(true);
                        cb(success);
                    });
                }));
            }
        }

        void PlayFullscreenAd(FullscreenAdInfo adInfo, string placementId, bool isMuted, System.Action<bool> cb)
        {
#if UNITY_IOS || UNITY_ANDROID
            if (adInfo.AdState == FullscreenAdState.Loaded)
            {
                GoogleMobileAds.Api.MobileAds.SetApplicationMuted(isMuted);
                GoogleMobileAds.Api.MobileAds.SetApplicationVolume(isMuted ? 0f : 1f);
                adInfo.AdNetwork = adInfo.MediationAdapterClassName();

                // Device is muted and our loaded interstitial has sound on, we should ignore this interstitial
                if (isMuted && !adInfo.LoadedAdWithMutedSound
                // Google Ad is controlled by SetApplicationVolume
                && adInfo.AdNetwork != null && !adInfo.AdNetwork.Contains("oogle")
                // Facebook Ad is always muted
                && !adInfo.AdNetwork.Contains("acebook"))
                {
                    Debug.LogWarning("[AdManager] Device is muted and interstitial ad has sound on. This interstitial is ignored!");
                    adInfo.AdState = FullscreenAdState.Used;
                    cb(false);
                    return;
                }

                _isPlayingFullscreenAd = true;
                adInfo.AdState = FullscreenAdState.Used;
                _fullscreenAdPlacementId = placementId;

                adInfo.WaitForAdPaidEventExpiredAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DURATION_FULLSCREEN_AD_PAID_EVENT_EXPIRED;
                adInfo.Show();

                NativeHelper.StartPlayingFullscreenAd();
                _appService.StartCoroutine(WaitForFullscreenAdClosedAndCallback(() =>
                {
                    cb(true);
                }));
            }
            else
            {
                Debug.LogWarning("[AdManager] FullscreenAd is not loaded yet!");
                cb(false);
            }
#endif
        }

        IEnumerator WaitForFullscreenAdLoadedAndCallback(FullscreenAdInfo adInfo, System.Action<bool, PollFullscreenAdErrorCode> cb, float timeout)
        {
#if UNITY_EDITOR
            cb(false, PollFullscreenAdErrorCode.Failed);
            yield break;
#else
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
#endif
        }

        IEnumerator WaitForFullscreenAdClosedAndCallback(System.Action cb)
        {
            while (_isPlayingFullscreenAd && !NativeHelper.IsFullScreenAdNativeClosed())
                yield return null;

            _isPlayingFullscreenAd = false;

            cb();
        }

        IEnumerator CheckSoundSwitch(System.Action<bool> cb)
        {
            NativeHelper.CheckSoundSwitchStatus();
            while (NativeHelper.IsCheckingSoundSwitch())
                yield return null;

            bool isMuted = NativeHelper.IsMuted();
            cb(isMuted);
        }
        #endregion

        #region Interstitial Ad Internal
        void LoadInterstitialAd(string adUnitId, bool autoRetry, float delay = 0f)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
            {
                adInfo = new InterstitialAdInfo()
                {
                    AdUnitId = adUnitId,
                    Listener = this
                };
                _allFullscreenAds.Add(adUnitId, adInfo);
            }
            adInfo.AutoRetry = autoRetry;

            if (adInfo.AdState == FullscreenAdState.Loading)
            {
                Debug.Log("[AdManager] Waiting for last interstitial ad response!");
                return;
            }

            if (adInfo.AdState == FullscreenAdState.Loaded)
            {
                Debug.Log("[AdManager] Use last interstitial ad response!");
                return;
            }

            if (adInfo.RequestAdCoroutine != null)
                return;

            adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateInterstitial(adInfo, delay));
        }

        IEnumerator CreateInterstitial(FullscreenAdInfo adInfo, float delay)
        {
            while (!_initializedMobileAds)
                yield return null;

            if (adInfo.WaitForAdPaidEventExpiredAt > System.DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                Debug.Log("[AdManager] Wait for interstitial ad paid event!");

            while (adInfo.WaitForAdPaidEventExpiredAt > System.DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                yield return null;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            Debug.Log("[AdManager] Requesting interstitial ad...");

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

            var builder = new AdRequest.Builder();
            AdEventsListener?.BuildInterstitialAdRequest(builder, isMuted);

            GoogleMobileAds.Api.MobileAds.SetApplicationVolume(isMuted ? 0f : 1f);
            GoogleMobileAds.Api.MobileAds.SetApplicationMuted(isMuted);

            AdRequest adRequest = builder.Build();
            adInfo.Load(adInfo.AdUnitId, adRequest, isMuted);
#endif
            adInfo.RequestAdCoroutine = null;
        }
        public void OnInterstitialAdClick(InterstitialAdInfo adInfo)
        {
            AdEventsListener?.OnInterstitialAdClicked(_fullscreenAdPlacementId, adInfo.AdUnitId, adInfo.AdNetwork, adInfo.adType);
        }
        public void OnInterstitialAdPaid(InterstitialAdInfo adInfo, AdValue adValue)
        {
            AdEventsListener?.OnInterstitialAdPaid(_fullscreenAdPlacementId, adInfo.AdUnitId, adInfo.AdNetwork, adValue.Value, adValue.CurrencyCode, (int)adValue.Precision);
        }

        public void OnInterstitialAdOpening(InterstitialAdInfo adInfo)
        {
            AdEventsListener?.OnInterstitialAdShow(_fullscreenAdPlacementId, adInfo.AdUnitId, adInfo.AdNetwork, -1, adInfo.adType);
        }

        public void OnInterstitialAdFailedToLoad(InterstitialAdInfo adInfo, LoadAdError loadAdError)
        {
            string message = loadAdError.GetMessage();

            AdEventsListener?.OnInterstitialAdFailedToLoad(adInfo.AdUnitId, message, adInfo.adType);

            if (adInfo.AutoRetry)
            {
                _appService.RunOnMainThread(() =>
                {
                    // We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds)
                    var retryDelay = Mathf.Pow(2, Mathf.Min(6, adInfo.RetryAttempt));
                    LoadInterstitialAd(adInfo.AdUnitId, adInfo.AutoRetry, retryDelay);
                });
            }
        }

        public void OnInterstitialAdLoaded(InterstitialAdInfo adInfo)
        {
            AdEventsListener?.OnInterstitialAdLoaded(adInfo.AdUnitId, adInfo.MediationAdapterClassName());
        }

        public void OnInterstitialAdClosed()
        {
            _isPlayingFullscreenAd = false;
            NativeHelper.FullScreenAdClosed();
        }
        #endregion

        #region Rewarded Ad Internal
        void LoadRewardedAd(string adUnitId, bool autoRetry, float delay = 0f)
        {
            FullscreenAdInfo adInfo;
            if (!_allFullscreenAds.TryGetValue(adUnitId, out adInfo))
            {
                adInfo = new RewardedAdInfo()
                {
                    AdUnitId = adUnitId,
                    Listener = this
                };
                _allFullscreenAds.Add(adUnitId, adInfo);
            }
            adInfo.AutoRetry = autoRetry;

            if (adInfo.AdState == FullscreenAdState.Loading)
            {
                Debug.Log("[AdManager] Waiting for last rewarded ad response!");
                return;
            }

            if (adInfo.AdState == FullscreenAdState.Loaded)
            {
                Debug.Log("[AdManager] Use last rewarded ad response!");
                return;
            }

            if (adInfo.RequestAdCoroutine != null)
                return;

            adInfo.RequestAdCoroutine = _appService.StartCoroutine(CreateRewardedAd(adInfo, delay));
        }

        IEnumerator CreateRewardedAd(FullscreenAdInfo adInfo, float delay)
        {
            while (!_initializedMobileAds)
                yield return null;

            if (adInfo.WaitForAdPaidEventExpiredAt > System.DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                Debug.Log("[AdManager] Wait for rewarded ad paid event!");

            while (adInfo.WaitForAdPaidEventExpiredAt > System.DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                yield return null;

            if (delay > 0)
                yield return new WaitForSeconds(delay);

            Debug.Log("[AdManager] Requesting rewarded ad...");

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

            var builder = new AdRequest.Builder();
            AdEventsListener?.BuildRewardedAdRequest(builder, isMuted);

            GoogleMobileAds.Api.MobileAds.SetApplicationVolume(isMuted ? 0f : 1f);
            GoogleMobileAds.Api.MobileAds.SetApplicationMuted(isMuted);

            AdRequest adRequest = builder.Build();
            adInfo.Load(adInfo.AdUnitId, adRequest, isMuted);
#endif
            adInfo.RequestAdCoroutine = null;
        }

        public void OnRewardedAdLoaded(RewardedAdInfo adInfo)
        {
            AdEventsListener?.OnRewardedAdLoaded(adInfo.AdUnitId, adInfo.MediationAdapterClassName());
        }

        public void OnRewardedAdFailedToLoad(RewardedAdInfo adInfo, LoadAdError loadAdError)
        {
            string message = loadAdError.GetMessage();
            AdEventsListener?.OnRewardedAdFailedToLoad(adInfo.AdUnitId, loadAdError.GetMessage());

            if (adInfo.AutoRetry)
            {
                _appService.RunOnMainThread(() =>
                {
                    // We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds)
                    var retryDelay = Mathf.Pow(2, Mathf.Min(6, adInfo.RetryAttempt));
                    LoadRewardedAd(adInfo.AdUnitId, adInfo.AutoRetry, retryDelay);
                });
            }
        }
        public void OnRewardedAdClicked(RewardedAdInfo adInfo)
        {
            Debug.Log("[AdManager] OnRewardedAdClicked");
        }
        public void OnRewardedAdOpening(RewardedAdInfo adInfo)
        {
            AdEventsListener?.OnRewardedAdShow(_fullscreenAdPlacementId, adInfo.AdUnitId, adInfo.AdNetwork);
        }

        public void OnRewardedAdFailedToShow(RewardedAdInfo adInfo, AdError args)
        {
            AdEventsListener?.OnRewardedAdFailedToShow(_fullscreenAdPlacementId, adInfo.AdUnitId, adInfo.AdNetwork);

            _isPlayingFullscreenAd = false;
            NativeHelper.FullScreenAdClosed();
        }

        public void OnRewardedAdPaid(RewardedAdInfo adInfo, AdValue adValue)
        {
            AdEventsListener?.OnRewardedAdPaid(_fullscreenAdPlacementId, adInfo.AdUnitId, adInfo.AdNetwork, adValue.Value, adValue.CurrencyCode, (int)adValue.Precision);
        }

        public void OnRewardedAdClosed(RewardedAdInfo adInfo)
        {
            _isPlayingFullscreenAd = false;
            NativeHelper.FullScreenAdClosed();
        }
        #endregion
    }
#endif
}