using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if USE_ADJUST
using AdjustSdk;
#elif USE_APPSFLYER
using AppsFlyerSDK;
#endif

#if USE_ADJUST_PURCHASE
using com.adjust.sdk.purchase;
#endif

namespace Delta.GameOps
{
    public interface IFullscreenAdDelegate
    {
        bool ShouldMuteVideoAd();
        void FullscreenAdSetAppInputActive(bool active);
    }

    public partial class DeltaApp : MonoBehaviour, IMainAppService, IAdEventsListener
    {
#if USE_MAX_MEDIATION
        public static bool MAX_enable { get { return true; } }
#else
        public static bool MAX_enable { get { return false; } }
#endif

        public event AppPaused OnAppPaused;
        public event System.Action OnAppQuit;
        public event AppPerfChanged OnAppPerfChanged;

        public static DeltaApp Instance
        {
            get;
            private set;
        }

        Dictionary<string, object> _defaultRemoteConfigs = new Dictionary<string, object>();

        // FPS
        bool _enableFPSCounter;
        float _FPSUpdateInterval;
        float _FPSAccumulate;
        int _totalFrames;
        float _refreshFPSTimeleft;
        bool _appInLowPerformance;

        int _currentDay;

        // ad number
        int _interstitialAdNumber;
        int _rewardedAdNumber;
        //
        bool isLogAdRevenue;

        public AppConfigs AppConfigs { get; private set; }
        public int AverageFPS { get; private set; }

        public AnalyticsManager AnalyticsManager { get; private set; }
        public IAdManager AdManager { get; private set; }
        public string AdPlatform { get; private set; }
        public IRemoteConfigsManager RemoteConfigsManager { get; private set; }
        public event System.Action ShouldRunFirstFetchRemoteConfigs;
        public FeedbackManager FeedbackManager { get; private set; }

        public AthenaGameService AthenaGameService { get; private set; }

        public bool IsAppPaused { get; private set; }
        public bool IsFirebaseReady { get; private set; }
#if USE_ADJUST
        public bool IsAdjustReady { get; private set; }
#endif
        public string FBAppId { get; private set; }

#if USE_ADMOB_MEDIATION
        public delegate void BuildAdRequestInterstitialAd(GoogleMobileAds.Api.AdRequest.Builder builder, bool isMuted);
        public delegate void BuildAdRequestRewardedAd(GoogleMobileAds.Api.AdRequest.Builder builder, bool isMuted);

        public event System.Action<GoogleMobileAds.Api.AdRequest.Builder> OnBuildBannerAdRequest;
        public event BuildAdRequestInterstitialAd OnBuildInterstitialAdRequest;
        public event BuildAdRequestRewardedAd OnBuildRewardedAdRequest;
#endif

        #region Custom Ad events - Please use this for analytics purpose
        public delegate void BannerAdRefresh(string adUnitId, string adNetwork);
        public delegate void BannerAdFailedToLoad(string adUnitId);
        public delegate void BannerAdClicked(string adUnitId, string adNetwork);
        public delegate void InterstitialAdStartLoading(string adUnitId);
        public delegate void InterstitialAdLoaded(string adUnitId, string adNetwork);
        public delegate void InterstitialAdFailedToLoad(string adUnitId, VideoAdType adType);
        public delegate void InterstitialAdShow(string placementId, string adUnitId, string adNetwork, VideoAdType adType);
        public delegate void InterstitialFailedToDisplay(VideoAdType adType, string errorMgs);
        public delegate void InterstitialAdClicked(string placementId, string adUnitId, string adNetwork, VideoAdType adType);
        public delegate void InterstitialAdClosed(VideoAdType adType);
        public delegate void RewardedAdStartLoading(string adUnitId);
        public delegate void RewardedAdLoaded(string adUnitId, string adNetwork);
        public delegate void RewardedAdFailedToLoad(string adUnitId);
        public delegate void RewardedAdShow(string placementId, string adUnitId, string adNetwork);
        public delegate void RewardedAdFailedToShow(string placementId, string adUnitId, string adNetwork);
        public delegate void RewardedAdUserEarned(string placementId, string adUnitId, string adNetwork);
        public delegate void RewardedAdClosed();
        public delegate void BannerPaid();
        public delegate void InterstitialPaid(string networkName, VideoAdType adType);
        public delegate void RewardedAdPaid();

        public event BannerPaid evtBannerPaid;
        public event InterstitialPaid evtInterstitialPaid;
        public event RewardedAdPaid evtRewardedAdPaid;

        public event BannerAdRefresh evtBannerAdRefresh;
        public event BannerAdFailedToLoad evtBannerAdFailedToLoad;
        public event BannerAdClicked evtBannerAdClicked;
        public event InterstitialAdStartLoading evtInterstitialAdStartLoading;
        public event InterstitialAdLoaded evtInterstitialAdLoaded;
        public event InterstitialAdFailedToLoad evtInterstitialAdFailedToLoad;
        public event InterstitialAdShow evtInterstitialAdShow;
        public event InterstitialFailedToDisplay evtInterstitialFailedToDisplay;
        public event InterstitialAdClosed evtInterstitialAdClosed;
        public event InterstitialAdClicked evtInterstitialAdClicked;
        public event RewardedAdStartLoading evtRewardedAdStartLoading;
        public event RewardedAdLoaded evtRewardedAdLoaded;
        public event RewardedAdFailedToLoad evtRewardedAdFailedToLoad;
        public event RewardedAdShow evtRewardedAdShow;
        public event RewardedAdFailedToShow evtRewardedAdFailedToShow;
        public event RewardedAdUserEarned evtRewardedAdUserEarned;
        public event RewardedAdClosed evtRewardedAdClosed;
        #endregion

        public event System.Action OnRemoteConfigsUpdated;
        public event System.Action OnDateChanged;
        public event System.Action OnAdSDKInitialized;
        public event System.Action evtFirebaseInited;

        public IFullscreenAdDelegate FullscreenAdDelegate;

        public void SubscribeAppPause(AppPaused listener)
        {
            OnAppPaused += listener;
        }

        public void UnSubscribeAppPause(AppPaused listener)
        {
            OnAppPaused -= listener;
        }

        public void SubscribeAppQuit(System.Action listener)
        {
            OnAppQuit += listener;
        }

        public void UnSubscribeAppQuit(System.Action listener)
        {
            OnAppQuit -= listener;
        }

        public void SubscribeAppPerfChanged(AppPerfChanged listener)
        {
            OnAppPerfChanged += listener;
        }

        public void UnSubscribeAppPerfChanged(AppPerfChanged listener)
        {
            OnAppPerfChanged -= listener;
        }

        public void SubscribeAppDateChanged(System.Action onDateChanged)
        {
            OnDateChanged += onDateChanged;
        }

        public void UnSubscribeAppDateChanged(System.Action onDateChanged)
        {
            OnDateChanged -= onDateChanged;
        }

        public bool ShouldMuteVideoAd()
        {
            if (FullscreenAdDelegate == null)
                return false;

            return FullscreenAdDelegate.ShouldMuteVideoAd();
        }

        public void SetAppInputActive(bool active)
        {
            if (FullscreenAdDelegate != null)
            {
                FullscreenAdDelegate.FullscreenAdSetAppInputActive(active);
            }
        }

        public void AddDefaultRemoteConfig(string key, object value)
        {
            _defaultRemoteConfigs.Add(key, value);
        }

        public void Initialize()
        {
            if (Instance != null)
            {
                Debug.LogError("[AthenaApp] Athena instance is already created!");
                return;
            }
            Instance = this;
            InitializeServices();
            // Firebase
            AthenaGameOpsUtils.InitFirebase(() =>
            {
                Debug.Log($"[DeltaApp] [Initialize]");
                IsFirebaseReady = true;
                evtFirebaseInited?.Invoke();
                SetupDefaultConfigs();
                RemoteConfigsManager.Initialize(_defaultRemoteConfigs);
            });
        }
        public void WaitToFirebaseInit(System.Action callBack = null)
        {
            evtFirebaseInited = callBack;
        }
        void Update()
        {
            if (_enableFPSCounter)
                UpdateFPS();

            CheckDateChanged();
        }

        void OnDestroy()
        {
            if (AdManager != null)
                AdManager.CleanUp();

            Instance = null;
        }

        void CheckDateChanged()
        {
            if (_currentDay != System.DateTime.Now.Day)
            {
                _currentDay = System.DateTime.Now.Day;
                OnDateChanged?.Invoke();

                _interstitialAdNumber = AnalyticsManager.InterstitialAdNumber;
                _rewardedAdNumber = AnalyticsManager.RewardedAdNumber;
            }
        }

        void UpdateFPS()
        {
            _refreshFPSTimeleft -= Time.deltaTime;
            _FPSAccumulate += Time.timeScale / Time.deltaTime;
            ++_totalFrames;

            if (_refreshFPSTimeleft <= 0.0f)
            {
                AverageFPS = (int)(_FPSAccumulate / _totalFrames);
                if (AverageFPS < 48 && !_appInLowPerformance)
                {
                    Debug.LogWarning("[AthenaApp] App is running in low performance!");

                    _appInLowPerformance = true;
                    ProcessAppPerfChanged(true);
                }
                else if (AverageFPS >= 48 && _appInLowPerformance)
                {
                    Debug.LogWarning("[AthenaApp] App back to normal performance!");

                    _appInLowPerformance = false;
                    ProcessAppPerfChanged(false);
                }

                _refreshFPSTimeleft = _FPSUpdateInterval;
                _FPSAccumulate = 0.0f;
                _totalFrames = 0;
            }
        }

        void ProcessAppPerfChanged(bool lowPerf)
        {
            OnAppPerfChanged?.Invoke(lowPerf);
        }

        public void RunOnMainThread(System.Action action)
        {
            AthenaGameOpsUtils.RunOnMainThread(action);
        }

        void InitializeServices()
        {
            _currentDay = System.DateTime.Now.Day;

            // Load app configs
            var textAssets = Resources.Load<TextAsset>("app_configs");
            AppConfigs = new AppConfigs();
            AppConfigs.Load(textAssets.bytes);

#if UNITY_IOS
            FBAppId = AppConfigs.GetValue("ios_fb_app_id", "Analytics").StringValue;
#elif UNITY_ANDROID
            FBAppId = AppConfigs.GetValue("android_fb_app_id", "Analytics").StringValue;
#endif

            // FPS
            _enableFPSCounter = AppConfigs.GetValue("enable_fps_counter", "General").BooleanValue;
            if (_enableFPSCounter)
            {
                _FPSUpdateInterval = (float)AppConfigs.GetValue("fps_update_interval", "General").DoubleValue;
                _refreshFPSTimeleft = _FPSUpdateInterval;
            }

            // Athena GameOps services
            // 1. AnalyticsManager
            var durationSessionTimeout = (int)AppConfigs.GetValue("duration_session_timeout", "Analytics").LongValue;
            AnalyticsManager = new AnalyticsManager(durationSessionTimeout, this);
            AnalyticsManager.OnRequiredUserPropertiesReported += () =>
            {
                StartCoroutine(StartFetchingRemoteConfigs());
            };

            RegisterDefaultAnalyticsAgents();
            AnalyticsManager.Start();

            _interstitialAdNumber = AnalyticsManager.InterstitialAdNumber;
            _rewardedAdNumber = AnalyticsManager.RewardedAdNumber;

            // 2. AdManager
#if UNITY_IOS
            var bannerAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_banner_ad_id" : "ios_banner_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var interstitialAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_interstitial_ad_id" : "ios_interstitial_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var softLaunchAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_softlaunch_ad_id" : "ios_softlaunch_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var coldStartAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_coldstart_ad_id" : "ios_coldstart_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var adBreakId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_adbreak_ad_id" : "ios_adbreak_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var videoAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_rewarded_ad_id" : "ios_rewarded_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;

#elif UNITY_ANDROID
            var bannerAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_banner_ad_id" : "android_banner_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var interstitialAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_interstitial_ad_id" : "android_interstitial_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var softLaunchAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_softlaunch_ad_id" : "android_softlaunch_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var coldStartAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_coldstart_ad_id" : "android_coldstart_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var adBreakId = AppConfigs.GetValue(MAX_enable ? "MAX_android_adbreak_ad_id" : "android_adbreak_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var videoAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_rewarded_ad_id" : "android_rewarded_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#endif
            var enablePaddingBanner = AppConfigs.GetValue("enable_padding_banner", "AdConfig").BooleanValue;
            var usingAdaptiveBanner = AppConfigs.GetValue("use_adaptive_banner", "AdConfig").BooleanValue;
            var hideBannerAdWhenAppPaused = false;
#if UNITY_IOS
            hideBannerAdWhenAppPaused = AppConfigs.GetValue("ios_hide_banner_ad_when_app_paused", "AdConfig").BooleanValue;
#endif
            var hideBannerAdWhenAppLowPerf = AppConfigs.GetValue("ios_hide_banner_ad_when_app_low_perf", "AdConfig").BooleanValue;

#if USE_MAX_MEDIATION
            var amazonAppId = "";
            var amazonTestMode = false;
#if USE_AMAZON
#if UNITY_IOS
            amazonAppId = AppConfigs.GetValue("Amazon_ios_app_id", "AMAZON").StringValue;
            var amazonBannerSlotId = AppConfigs.GetValue("Amazon_ios_banner_slot_id", "AMAZON").StringValue;
            var amazonInterstitialSlotId = AppConfigs.GetValue("Amazon_ios_interstitial_slot_id", "AMAZON").StringValue;
            var amazonRewardedVideoSlotId = AppConfigs.GetValue("Amazon_ios_rewarded_slot_id", "AMAZON").StringValue;

#elif UNITY_ANDROID
            amazonAppId = AppConfigs.GetValue("Amazon_android_app_id", "AMAZON").StringValue;
            var amazonBannerSlotId = AppConfigs.GetValue("Amazon_android_banner_slot_id", "AMAZON").StringValue;
            var amazonInterstitialSlotId = AppConfigs.GetValue("Amazon_android_interstitial_slot_id", "AMAZON").StringValue;
            var amazonRewardedVideoSlotId = AppConfigs.GetValue("Amazon_android_rewarded_slot_id", "AMAZON").StringValue;
#endif
#if CHEAT
            amazonTestMode = AppConfigs.GetValue("Amazon_enable_test", "AMAZON").BooleanValue;
#else
            amazonTestMode = false;
#endif
#endif
            // var sdkKey = AppConfigs.GetValue("MAX_SDK_key", "MAX").StringValue;
            var setUnifiedConsentFlow = AppConfigs.GetValue("MAX_set_unified_consent_flow", "MAX").BooleanValue;
            var setUserJourney = AppConfigs.GetValue("MAX_set_user_journey", "MAX").BooleanValue;

            string userJouneyID = setUserJourney ? PlayerPrefs.GetString("UserIDKey", "") : ""; // UserIDKey get from game (playfab id or gg, or fb id,...)
#if MAX_DEBUGGER
            var showMediationDebugger = true;
#else
            var showMediationDebugger = false;
#endif
            AdManager = new MAXAdManager(MAXSDKInitialized, enablePaddingBanner, usingAdaptiveBanner,
            bannerAdId, interstitialAdId, softLaunchAdId, coldStartAdId, adBreakId, videoAdId,
            this, showMediationDebugger, hideBannerAdWhenAppPaused, hideBannerAdWhenAppLowPerf, false, amazonAppId, amazonTestMode,
            showMediationDebugger, setUserJourney, userJouneyID);
            AdPlatform = AnalyticsManager.AD_PLATFORM_MAX;
#if USE_AMAZON
            (AdManager as MAXAdManager).AmazonBannerSlotId = amazonBannerSlotId;
            (AdManager as MAXAdManager).AmazonInterstitialSlotId = amazonInterstitialSlotId;
            (AdManager as MAXAdManager).AmazonRewardedVideoSlotId = amazonRewardedVideoSlotId;
#endif
            // #if UNITY_ANDROID
            //             //Andoroid only - init Adjust SDK & init MAX SDK the same time to fix install track 
            //             InitializeAdjust();
            // #endif

#elif USE_ADMOB_MEDIATION
            AdManager = new AdManager(AdSDKInitialized, enablePaddingBanner, usingAdaptiveBanner, bannerAdId, interstitialAdId,
            softLaunchAdId, coldStartAdId, adBreakId, videoAdId, this, hideBannerAdWhenAppPaused, hideBannerAdWhenAppLowPerf);

            var enableMediation = AppConfigs.GetValue("enable_admob_mediation", "Admob").BooleanValue;
            AdPlatform = enableMediation ? AnalyticsManager.AD_PLATFORM_ADMOB : AnalyticsManager.AD_PLATFORM_NONE;
#else
            AdManager = new DummyAdManager(enablePaddingBanner, usingAdaptiveBanner, bannerAdId, interstitialAdId, hideBannerAdWhenAppPaused);
            AdPlatform = AnalyticsManager.AD_PLATFORM_NONE;
#endif
            AdManager.BannerPlacementId = AnalyticsManager.AD_PLACEMENT_EMPTY;
            AdManager.AdEventsListener = this;

            var testDevices = AppConfigs.GetValue("test_devices", "AdConfig").StringValue;
            if (!string.IsNullOrEmpty(testDevices))
            {
                var devices_ids = testDevices.Split(';');
                if (devices_ids != null && devices_ids.Length > 0)
                    AdManager.SetTestDevices(devices_ids);
            }

            // 3. RemoteConfigsManager
            var durationCacheExpired = (int)AppConfigs.GetValue("duration_cache_expired", "RemoteConfigs").LongValue;
            var durationRetryNextFetch = (int)AppConfigs.GetValue("duration_retry_next_fetch", "RemoteConfigs").LongValue;
#if ENABLE_FIREBASE
            RemoteConfigsManager = new RemoteConfigsManager(durationCacheExpired, durationRetryNextFetch, this);
#else
            RemoteConfigsManager = new DummyRemoteConfigsManager(this);
#endif

            // 4. AthenaGameService
            var firebaseProjectId = AppConfigs.GetValue("firebase_project_id", "AGS").StringValue;
            var agsRootAPI = AppConfigs.GetValue("ags_root_api", "AGS").StringValue;
            var agsUsersAPI = AppConfigs.GetValue("ags_api_users", "AGS").StringValue;
            var agsUserName = AppConfigs.GetValue("ags_user_name", "AGS").StringValue;
            var agsPassword = AppConfigs.GetValue("ags_password", "AGS").StringValue;
            var maxRetryCount = (int)AppConfigs.GetValue("max_retry_count", "AGS").LongValue;
            var timeToNextRegister = (int)AppConfigs.GetValue("time_to_next_register", "AGS").LongValue;
            AthenaGameService = new AthenaGameService(firebaseProjectId, agsRootAPI, agsUsersAPI, agsUserName, agsPassword, maxRetryCount, timeToNextRegister, this);
            AthenaGameService.OnRegisterSuccessEvent += OnAGSRegisterSuccess;
            AthenaGameService.OnRegisterFailedEvent += OnAGSRegisterFailed;
            AthenaGameService.OnRegisterIgnoreByCache += OnAGSRegisterIgnoreByCache;

            var isDevEnv = AppConfigs.GetValue("dev_env", "ReportIAP").BooleanValue;
            // cheat then show dev env, no cheat show prod env
#if CHEAT
            isDevEnv = true;
#else
            isDevEnv = false;
#endif
            Debug.Log("[ReceiptAPI] Environment: " + (isDevEnv ? "Development" : "Production"));

            var IAPReportKey = AppConfigs.GetValue(isDevEnv ? "encrypt_key_dev" : "encrypt_key", "ReportIAP").StringValue;
            var IAPReceiptAPI = AppConfigs.GetValue(isDevEnv ? "receipt_api_dev" : "receipt_api", "ReportIAP").StringValue;
            var IAPVerifyAPI = AppConfigs.GetValue(isDevEnv ? "verify_api_dev" : "verify_api", "ReportIAP").StringValue;
            var IAPAuthUser = AppConfigs.GetValue(isDevEnv ? "auth_user_dev" : "auth_user", "ReportIAP").StringValue;
            var IAPAuthPass = AppConfigs.GetValue(isDevEnv ? "auth_pass_dev" : "auth_pass", "ReportIAP").StringValue;
            var IAPretryCount = AppConfigs.GetValue(isDevEnv ? "retry_count" : "retry_count", "ReportIAP").IntValue;
            var IAPVerifyReceipt = AppConfigs.GetValue("verify_receipt", "ReportIAP").BooleanValue;
            var iosAppId = AppConfigs.GetValue("ios_app_id", "General").StringValue;
            var androidPackageName = AppConfigs.GetValue("android_package_name", "General").StringValue;
            AthenaGameService.SetupIAPReportService(iosAppId, androidPackageName, IAPReportKey, IAPReceiptAPI, IAPVerifyAPI, IAPAuthUser, IAPAuthPass, IAPretryCount, IAPVerifyReceipt);

            // AthenaGameService.RegisterUser();
            // SubscribeAppPause((pausedStatus) =>
            // {
            //     if (!pausedStatus)
            //         AthenaGameService.RegisterUser();
            // });

            // 5. FeedbackManager
            var feedbackApi = AppConfigs.GetValue("api_endpoint", "Feedback").StringValue;
            FeedbackManager = new FeedbackManager(feedbackApi, this);

            //Google cmp
#if USE_ADMOB_MEDIATION
            var enableGGCMP = AppConfigs.GetValue("enable_google_cmp", "AdConfig").BooleanValue;
            if (enableGGCMP)
            {
                InitGoogleCMP();
            }
#endif
        }

        #region Analytics
        void MAXSDKInitialized()
        {
            AdSDKInitialized();
            // #if UNITY_IOS
            InitializeAdjust();
            // #endif
        }
        void AdSDKInitialized()
        {
            OnAdSDKInitialized?.Invoke();
        }

        void InitializeAdjust()
        {
#if USE_ADJUST
            var adjustLogLevel = 7;//AdjustLogLevel.Suppress
#if CHEAT
            var adjustSandbox = AppConfigs.GetValue("sandbox", "Adjust").BooleanValue;
             adjustLogLevel = AppConfigs.GetValue("log_level", "Adjust").IntValue;//1 = //AdjustLogLevel.Verbose
#else
            var adjustSandbox = false;
#endif
#if UNITY_IOS
            var adjustAppToken = AppConfigs.GetValue("ios_app_token", "Adjust").StringValue;
#elif UNITY_ANDROID
            var adjustAppToken = AppConfigs.GetValue("android_app_token", "Adjust").StringValue;
#endif
            Debug.Log("[Adjust] Environment: " + (adjustSandbox ? "Sandbox" : "Production"));
            var isUseAdjustPurchase = AppConfigs.GetValue("is_use_adjust_purchase", "Adjust").BooleanValue;
            isLogAdRevenue = AppConfigs.GetValue("is_log_ad_revenue", "Adjust").BooleanValue;
            bool isSetMetaReferrer = AppConfigs.GetValue("is_set_meta_referrer", "Adjust").BooleanValue;
            var adjustConfig = new AdjustConfig
            (
                adjustAppToken,
                adjustSandbox ? AdjustEnvironment.Sandbox : AdjustEnvironment.Production,
                true
            );
#if UNITY_ANDROID
            if (isSetMetaReferrer && !string.IsNullOrEmpty(FBAppId))
            {
                adjustConfig.FbAppId = FBAppId;
                Debug.Log("[Adjust] Meta referrer integration sucessfull! Facebook AppId: " + FBAppId);
            }
#endif
            adjustConfig.LogLevel = (AdjustLogLevel)adjustLogLevel;
            adjustConfig.AttributionChangedDelegate = AnalyticsManager.AdjustAttributionChangedDelegate;
            adjustConfig.IsSendingInBackgroundEnabled = true;

#if UNITY_IOS
            if (Adjust.getAppTrackingAuthorizationStatus() == 0)//not set yet
            {
                adjustConfig.setAttConsentWaitingInterval(120);
            }
#endif
            var adjustInstance = new GameObject("Adjust").AddComponent<Adjust>(); // do not remove or rename
            Object.DontDestroyOnLoad(adjustInstance.gameObject);
            Adjust.InitSdk(adjustConfig);
            if (isUseAdjustPurchase)
            {
#if USE_ADJUST_PURCHASE
                var adjustPurchaseInstance = new GameObject("AdjustPurchase").AddComponent<AdjustPurchase>(); // do not remove or rename
                Object.DontDestroyOnLoad(adjustPurchaseInstance.gameObject);
                adjustPurchaseInstance.InitAdjustPurchase(adjustAppToken);
#endif
            }
            IsAdjustReady = true;
#endif
        }
        public void RequestATTPopupAdjust()
        {
#if USE_ADJUST
            if (Adjust.GetAppTrackingAuthorizationStatus() == 0)//not set yet
            {
                Adjust.RequestAppTrackingAuthorization((status) =>
                {

                });
            }
#endif
        }
        void RegisterDefaultAnalyticsAgents()
        {
            var durationSessionTimeout = (int)AppConfigs.GetValue("duration_session_timeout", "Analytics").LongValue;
#if USE_ADJUST
            var adjustEventNames = AppConfigs.GetValue("event_names", "Adjust").StringValue.Split(';');
#if UNITY_IOS
            var adjustAppToken = AppConfigs.GetValue("ios_app_token", "Adjust").StringValue;
            var adjustEventTokens = AppConfigs.GetValue("ios_event_tokens", "Adjust").StringValue.Split(';');
#elif UNITY_ANDROID
            var adjustAppToken = AppConfigs.GetValue("android_app_token", "Adjust").StringValue;
            var adjustEventTokens = AppConfigs.GetValue("android_event_tokens", "Adjust").StringValue.Split(';');
#endif
            var adjustAgent = new AdjustAnalyticsAgent(adjustAppToken, adjustEventNames, adjustEventTokens, this);
            AnalyticsManager.AddAgent(adjustAgent);
#elif USE_APPSFLYER
            // AppsFlyer
            var iosAppId = AppConfigs.GetValue("ios_app_id", "General").StringValue;
            var androidPackageName = AppConfigs.GetValue("android_package_name", "General").StringValue;
#if UNITY_IOS
            var devKey = AppConfigs.GetValue("ios_dev_key", "AppsFlyer").StringValue;
#elif UNITY_ANDROID
            var devKey = AppConfigs.GetValue("android_dev_key", "AppsFlyer").StringValue;
#endif
            var trackEnabled = AppConfigs.GetValue("track_enabled", "AppsFlyer").BooleanValue;

            var appsFlyerListener = new GameObject("AppsFlyerTrackerCallbacks", typeof(AppsFlyerTrackerCallbacks)).GetComponent<AppsFlyerTrackerCallbacks>();
            Object.DontDestroyOnLoad(appsFlyerListener.gameObject);

            var appsflyerAgent = new AppsFlyerAnalyticsAgent(iosAppId, androidPackageName, devKey, trackEnabled, durationSessionTimeout, appsFlyerListener);
            AnalyticsManager.AddAgent(appsflyerAgent);
#endif

            // Firebase
#if ENABLE_FIREBASE
            var firebaseAgent = new FirebaseAnalyticsAgent(this);
            AnalyticsManager.AddAgent(firebaseAgent);
#endif

            // Flury
#if ENABLE_FLURRY
            var usingFlurry = AppConfigs.GetValue("using_flurry", "Flurry").BooleanValue;
            if (usingFlurry)
            {
#if UNITY_IOS
                var flurryApiKey = AppConfigs.GetValue("ios_api_key", "Flurry").StringValue;
                var flurryAgent = new FlurryAnalyticsAgent(flurryApiKey, durationSessionTimeout);
                AnalyticsManager.AddAgent(flurryAgent);
#elif UNITY_ANDROID
                var flurryApiKey = AppConfigs.GetValue("android_api_key", "Flurry").StringValue;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
#if UNITY_EDITOR
                    var apiLevel = 0;
#else
                    var apiLevel = version.GetStatic<int>("SDK_INT");
#endif
                    var flurryAgent = new FlurryAnalyticsAgent(flurryApiKey, durationSessionTimeout);
                    AnalyticsManager.AddAgent(flurryAgent);
                }
#endif
            }
#endif
        }
        #endregion

        #region Ads
        public void OnMaxSdkInterstitialPaid(string networkName, VideoAdType adType)
        {
            evtInterstitialPaid?.Invoke(networkName, adType);
        }
        public void OnMaxSdkRewardedAdPaid()
        {
            evtRewardedAdPaid?.Invoke();
        }
        public void OnMaxSdkBannerPaid()
        {
            evtBannerPaid?.Invoke();
        }
        public void OnLogAdImpression(string adPlatform, string networkName, string adUnitId, string adFormat, string currency, double revenue, string adPlacement = null)
        {
#if USE_MAX_MEDIATION
            RunOnMainThread(() =>
                {
                    AnalyticsManager.LogAdImpression(adPlatform, networkName, adUnitId, adFormat, currency, revenue);
                });
#endif
        }
        public void OnLogBIAdValueMAX(string adFormat, string adPlacement, string adUnitId, string networkName, double revenue, string currency = "USD", int precision = 0)
        {
#if USE_MAX_MEDIATION
            RunOnMainThread(() =>
            {
                if (adFormat.Contains("banner"))
                    AnalyticsManager.LogBIAdValueBanner(AdPlatform, adPlacement, adUnitId, networkName, (float)revenue, currency, precision);
                if (adFormat.Contains("interstitial"))
                    AnalyticsManager.LogBIAdValueInterstitial(AdPlatform, adPlacement, adUnitId, networkName, _interstitialAdNumber, (float)revenue, currency, precision);
                if (adFormat.Contains("rewarded_video"))
                    AnalyticsManager.LogBIAdValueRewarded(AdPlatform, adPlacement, adUnitId, networkName, _rewardedAdNumber, (float)revenue, currency, precision);
            });
#endif

        }

        public void OnLogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement)
        {
            // if (isLogAdRevenue) return;
            RunOnMainThread(() =>
            {
                AnalyticsManager.LogAdRevenue(revenue, networkName, adUnitId, adPlacement);
            });
        }
#if USE_ADMOB_MEDIATION
        public void BuildBannerAdRequest(GoogleMobileAds.Api.AdRequest.Builder builder)
        {
            OnBuildBannerAdRequest?.Invoke(builder);
        }
#endif

        public void OnBannerAdRefresh(string adPlacement, string adUnitId, string adNetwork, float MAXEstValueUSD)
        {
            RunOnMainThread(() =>
            {
                evtBannerAdRefresh?.Invoke(adUnitId, adNetwork);
            });
        }

        public void OnBannerAdFailedToLoad(string adUnitId, string errorMessage)
        {
            RunOnMainThread(() =>
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    evtBannerAdFailedToLoad?.Invoke(adUnitId);
                }
            });
        }

        public void OnBannerAdClicked(string adPlacement, string adUnitId, string adNetwork)
        {
            evtBannerAdClicked?.Invoke(adUnitId, adNetwork);
        }

        public void OnBannerAdPaid(string adPlacement, string adUnitId, string adNetwork, long value, string currency, int precision)
        {
            RunOnMainThread(() =>
            {
                AnalyticsManager.LogBIAdValueBanner(AdPlatform, adPlacement, adUnitId, adNetwork, value / 1000000f, currency, precision);
            });
        }

#if USE_ADMOB_MEDIATION
        public void BuildInterstitialAdRequest(GoogleMobileAds.Api.AdRequest.Builder builder, bool isDeviceMuted)
        {
            OnBuildInterstitialAdRequest?.Invoke(builder, isDeviceMuted);
        }
#endif
        public void OnInterstitialAdStartLoading(string adUnitId)
        {
            evtInterstitialAdStartLoading?.Invoke(adUnitId);
        }

        public void OnInterstitialAdLoaded(string placementId, string adUnitId)
        {
            evtInterstitialAdLoaded?.Invoke(placementId, adUnitId);
        }

        public void OnInterstitialAdShow(string placementId, string adUnitId, string adNetwork, float MAXEstValueUSD, VideoAdType adType)
        {
            RunOnMainThread(() =>
            {
                evtInterstitialAdShow?.Invoke(placementId, adUnitId, adNetwork, adType);
            });
        }
        public void OnInterstitialFailedToDisplay(VideoAdType adType, string errorMgs)
        {
            RunOnMainThread(() =>
            {
                evtInterstitialFailedToDisplay?.Invoke(adType, errorMgs);
            });
        }
        public void OnInterstitialAdClosed(string placemendId, string adUnitId, VideoAdType adType)
        {
            _interstitialAdNumber = AnalyticsManager.InterstitialAdNumber;
            evtInterstitialAdClosed?.Invoke(adType);
        }

        public void OnInterstitialAdFailedToLoad(string adUnitId, string errorMessage, VideoAdType adType)
        {
            RunOnMainThread(() =>
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    evtInterstitialAdFailedToLoad?.Invoke(adUnitId, adType);
                }
            });
        }

        public void OnInterstitialAdClicked(string placementId, string adUnitId, string adNetwork, VideoAdType adType)
        {
            RunOnMainThread(() =>
            {
                evtInterstitialAdClicked?.Invoke(placementId, adUnitId, adNetwork, adType);
            });
        }

        public void OnInterstitialAdPaid(string placementId, string adUnitId, string adNetwork, long value, string currency, int precision)
        {
            RunOnMainThread(() =>
            {
                AnalyticsManager.LogBIAdValueInterstitial(AdPlatform, placementId, adUnitId, adNetwork, _interstitialAdNumber, value / 1000000f, currency, precision);
            });
        }

#if USE_ADMOB_MEDIATION
        public void BuildRewardedAdRequest(GoogleMobileAds.Api.AdRequest.Builder builder, bool isDeviceMuted)
        {
            OnBuildRewardedAdRequest?.Invoke(builder, isDeviceMuted);
        }
#endif

        public void OnRewardedAdStartLoading(string adUnitId)
        {
            evtRewardedAdStartLoading?.Invoke(adUnitId);
        }

        public void OnRewardedAdLoaded(string adUnitId, string adNetwork)
        {
            evtRewardedAdLoaded?.Invoke(adUnitId, adNetwork);
        }

        public void OnRewardedAdShow(string placementId, string adUnitId, string adNetwork)
        {
            evtRewardedAdShow?.Invoke(placementId, adUnitId, adNetwork);
        }

        public void OnRewardedAdClosed(string placemendId, string adUnitId)
        {
            _rewardedAdNumber = AnalyticsManager.RewardedAdNumber;
            evtRewardedAdClosed?.Invoke();
        }

        public void OnRewardedAdFailedToLoad(string adUnitId, string errorMessage)
        {
            RunOnMainThread(() =>
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    evtRewardedAdFailedToLoad?.Invoke(adUnitId);
                }
            });
        }

        public void OnRewardedAdFailedToShow(string placementId, string adUnitId, string adNetwork)
        {
            evtRewardedAdFailedToShow?.Invoke(placementId, adUnitId, adNetwork);
        }

        public void OnRewardedAdUserEarned(string placementId, string adUnitId, string adNetwork, float MAXEstValueUSD)
        {
            RunOnMainThread(() =>
            {
                evtRewardedAdUserEarned?.Invoke(placementId, adUnitId, adNetwork);
            });
        }

        public void OnRewardedAdPaid(string placementId, string adUnitId, string adNetwork, long value, string currency, int precision)
        {
            RunOnMainThread(() =>
            {
                AnalyticsManager.LogBIAdValueRewarded(AdPlatform, placementId, adUnitId, adNetwork, _rewardedAdNumber, value / 1000000f, currency, precision);
            });
        }
        #endregion

        #region Remote Configs
        IEnumerator StartFetchingRemoteConfigs()
        {
            while (RemoteConfigsManager == null || !RemoteConfigsManager.IsInitialized)
                yield return null;

            var autoRun = AppConfigs.GetValue("auto_run_first_fetch", "RemoteConfigs").BooleanValue;
            if (autoRun)
            {
                Debug.Log("[AthenaApp] Start fetching Remote Configs!");
                RemoteConfigsManager.Run();
            }
            else
            {
                ShouldRunFirstFetchRemoteConfigs?.Invoke();
            }
        }

        protected void SetupDefaultConfigs()
        {
#if UNITY_IOS
            var bannerAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_banner_ad_id" : "ios_banner_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var interstitialAdId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_interstitial_ad_id" : "ios_interstitial_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#elif UNITY_ANDROID
            var bannerAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_banner_ad_id" : "android_banner_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
            var interstitialAdId = AppConfigs.GetValue(MAX_enable ? "MAX_android_interstitial_ad_id" : "android_interstitial_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#endif

            if (MAX_enable)
            {
                var showMediationDebugger = AppConfigs.GetValue("MAX_show_mediation_debugger", "MAX").BooleanValue;
                _defaultRemoteConfigs.Add("MAX_show_mediation_debugger", showMediationDebugger);
            }

            _defaultRemoteConfigs.Add("banner_ad_id", bannerAdId);
            _defaultRemoteConfigs.Add("interstitial_ad_id", interstitialAdId);
            _defaultRemoteConfigs.Add("padding_banner_x_value", 0L);
            _defaultRemoteConfigs.Add("padding_banner_y_value", 0L);
            _defaultRemoteConfigs.Add("use_adaptive_banner", AppConfigs.GetValue("use_adaptive_banner", "AdConfig").BooleanValue);
            _defaultRemoteConfigs.Add("test_devices", string.Empty);
        }

        public void ShouldApplyFetchedConfigs()
        {
            var configTestDevices = RemoteConfigsManager.GetValue("test_devices").StringValue;
            if (!string.IsNullOrEmpty(configTestDevices))
            {
                var testDevices = configTestDevices.Split(';');
                if (testDevices != null && testDevices.Length > 0)
                    AdManager.SetTestDevices(testDevices);
            }

            // ad configs
            var bannerId = "";
#if UNITY_IOS
            bannerId = RemoteConfigsManager.GetValue(MAX_enable ? "MAX_ios_banner_ad_id" : "ios_banner_ad_id")?.StringValue;
#elif UNITY_ANDROID
            bannerId = RemoteConfigsManager.GetValue(MAX_enable ? "MAX_android_banner_ad_id" : "android_banner_ad_id")?.StringValue;
#endif

            if (string.IsNullOrEmpty(bannerId))
            {
#if UNITY_IOS
                bannerId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_banner_ad_id" : "ios_banner_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#elif UNITY_ANDROID
                bannerId = AppConfigs.GetValue(MAX_enable ? "MAX_android_banner_ad_id" : "android_banner_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#endif
            }
            var enablePaddingBanner = AppConfigs.GetValue("enable_padding_banner", "AdConfig").BooleanValue;
            var paddingBannerX = enablePaddingBanner ? (int)RemoteConfigsManager.GetValue("padding_banner_x_value").LongValue : 0;
            var paddingBannerY = enablePaddingBanner ? (int)RemoteConfigsManager.GetValue("padding_banner_y_value").LongValue : 0;
            var usingAdaptiveBanner = RemoteConfigsManager.GetValue("use_adaptive_banner").BooleanValue;
            AdManager.RefreshBannerConfigs(bannerId, paddingBannerX, paddingBannerY, usingAdaptiveBanner);

            var interstitialId = "";
#if UNITY_IOS
            interstitialId = RemoteConfigsManager.GetValue(MAX_enable ? "MAX_ios_interstitial_ad_id" : "ios_interstitial_ad_id")?.StringValue;
#elif UNITY_ANDROID
            interstitialId = RemoteConfigsManager.GetValue(MAX_enable ? "MAX_android_interstitial_ad_id" : "android_interstitial_ad_id")?.StringValue;
#endif
            if (string.IsNullOrEmpty(interstitialId))
            {
#if UNITY_IOS
                interstitialId = AppConfigs.GetValue(MAX_enable ? "MAX_ios_interstitial_ad_id" : "ios_interstitial_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#elif UNITY_ANDROID
                interstitialId = AppConfigs.GetValue(MAX_enable ? "MAX_android_interstitial_ad_id" : "android_interstitial_ad_id", MAX_enable ? "MAX" : "Admob").StringValue;
#endif
            }
            AdManager.RefreshInterstitialConfigs(interstitialId);

#if USE_MAX_MEDIATION
            var showMediationDebugger = RemoteConfigsManager.GetValue("MAX_show_mediation_debugger").BooleanValue;
            if (showMediationDebugger)
            {
                var MAXAdManager = (AdManager as MAXAdManager);
                MAXAdManager.ShowMediationDebugger();
            }
#endif
            OnRemoteConfigsUpdated?.Invoke();
        }
        #endregion

        #region AGS
        IEnumerator WaitForFirebaseReady(System.Action cb)
        {
            while (!IsFirebaseReady)
                yield return null;

            cb();
        }

        void OnAGSRegisterSuccess(string userId)
        {
            StartCoroutine(WaitForFirebaseReady(() =>
            {
                AthenaGameOpsUtils.SetFirebaseUserId(userId);

#if USE_APPSFLYER
                AppsFlyer.setCustomerUserId(userId);
#endif
                AnalyticsManager.TrackEvent("AGS_RegisterUserSuccess");
            }));
        }

        void OnAGSRegisterIgnoreByCache(string userId)
        {

        }

        void OnAGSRegisterFailed(AthenaGameService.ErrorCode errorCode, string errorMessage)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("error_message", errorMessage);
            AnalyticsManager.TrackEventWithParameters("AGS_RegisterUserFailed", parameters);
        }
        #endregion

        #region Interrupt
        void ProcessAppPaused(bool pauseStatus)
        {
            if (!pauseStatus)
                CheckDateChanged();

            OnAppPaused?.Invoke(pauseStatus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus != IsAppPaused)
                ProcessAppPaused(pauseStatus);

            IsAppPaused = pauseStatus;
        }

        private void OnApplicationFocus(bool focusStatus)
        {
            if (focusStatus == IsAppPaused)
                ProcessAppPaused(!focusStatus);

            IsAppPaused = !focusStatus;
        }

        private void OnApplicationQuit()
        {
            OnAppQuit?.Invoke();
        }
        #endregion
        #region  Google CMP

#if USE_ADMOB_MEDIATION
        private GoogleCMPManager googleCMPIns = null;
        private void InitGoogleCMP()
        {
            googleCMPIns = new GameObject("GoogleCMP").AddComponent<GoogleCMPManager>(); // do not remove or rename
            Object.DontDestroyOnLoad(googleCMPIns.gameObject);
        }
        public void ShowPrivacyForm()
        {
            googleCMPIns?.ShowPrivacyForm();
        }
        public bool IsShowButtonPrivacy()
        {
            return (googleCMPIns != null) ? googleCMPIns.IsShowButtonPrivacy : false;
        }
        public bool IsOptInConsent()
        {
            return (googleCMPIns != null) ? googleCMPIns.IsOptInConsent : false;
        }
        public void SetupAdjustGoogleCMP(string eea, string ad_personalization, string ad_user_data)
        {
#if USE_ADJUST
            AdjustThirdPartySharing adjustThirdPartySharing = new AdjustThirdPartySharing(null);
            adjustThirdPartySharing.addGranularOption("google_dma", "eea", eea);
            adjustThirdPartySharing.addGranularOption("google_dma", "ad_personalization", ad_personalization);
            adjustThirdPartySharing.addGranularOption("google_dma", "ad_user_data", ad_user_data);
            Adjust.trackThirdPartySharing(adjustThirdPartySharing);
#endif
        }
#endif
        #endregion
    }
}
