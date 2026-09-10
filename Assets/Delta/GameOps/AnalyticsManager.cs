using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if USE_ADJUST
using AdjustSdk;
#elif USE_APPSFLYER
using AppsFlyerSDK;
#endif

namespace Delta.GameOps
{
    public interface IAnalyticsAgent
    {
        bool IsUAAgent { get; }
        void LogSceneName(string sceneName, string sceneClass);
        void SetUserProperty(string name, string value);
        void LogEvent(string eventName);
        void LogEventWithParameters(string eventName, Dictionary<string, object> parameters);
        void LogRevenue(string eventName, double value, string currency, string transactionId, string productId, string googlePublickKey = "", string unityReceiptPayload = "");
        void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement);
        void LogLoyalUser();
        void LogShowBannerAd();
        void LogShowInterstitialAd();
        void LogBannerAdClicked(string adUnitId, string adNetwork);
        void LogInterstitialAdClicked(string adUnitId, string adNetwork);
        void LogCompleteRewardedAd(string adUnitId, string adNetwork);
    }

    public class AnalyticsManager
    {
        #region PlayerPrefs keys
        const string KEY_SESSION_COUNT = "SESSION_COUNT";
        const string KEY_MATCH_COUNT = "MATCH_COUNT";
        const string KEY_DAY_MATCH_COUNT = "DAY_MATCH_COUNT";

        const string KEY_FIRST_OPEN_DATE = "FIRST_OPEN_DATE_APPSFLYER";
        const string KEY_LAST_OPEN_DATE = "LAST_OPEN_DATE";
        const string KEY_APPSPFLYER_MEDIASOURCE = "APPSFLYER_MEDIASOURCE";
        const string KEY_ADJUST_MEDIASOURCE = "ADJUST_MEDIASOURCE";

        const string KEY_ENTER_BACKGROUND_START_TIME = "ENTER_BACKGROUND_START_TIME";

        const string KEY_VIEW_INTERSTITIAL_COUNT = "VIEW_INTERSTITIAL_COUNT";

        const string KEY_SHOW_BANNER_NUMBER_TODAY = "SHOW_BANNER_NUMBER_TODAY";
        const string KEY_SHOW_INTERSTITIAL_NUMBER_TODAY = "SHOW_INTERSTITIAL_NUMBER_TODAY";
        const string KEY_SHOW_REWARDED_NUMBER_TODAY = "SHOW_REWARDED_NUMBER_TODAY";

        const string KEY_BANNER_AD_CUM_VALUE_USD = "BANNER_AD_CUM_VALUE_USD";
        const string KEY_INTERSTITIAL_AD_CUM_VALUE_USD = "INTERSTITIAL_AD_CUM_VALUE_USD";
        const string KEY_REWARDED_AD_CUM_VALUE_USD = "REWARDED_AD_CUM_VALUE_USD";
        const string KEY_AD_CUM_SUM_VALUE_USD = "AD_CUM_SUM_VALUE_USD";

        const string KEY_USER_PROPERTY_JOINDATE = "USER_PROPERTY_JOIN_DATE";
        const string KEY_USER_PROPERTY_RETAINEDDAYS = "USER_PROPERTY_RETAINEDDAYS";
        const string KEY_USER_PROPERTY_VERSION = "USER_PROPERTY_VERSION";
        const string KEY_USER_PROPERTY_DEVICE_TYPE = "USER_PROPERTY_DEVICE_TYPE";
        #endregion

        #region Event names
        const string EVENT_START_SESSION = "start_{0}th_session";
        const string EVENT_RETAINED_USER_DAY = "retained_user_day_{0}";
        const string EVENT_OPEN_2ND_SESSION_DAY = "open_2nd_session_d{0}";
        const string EVENT_VIEW_INTERSTITIAL_DAY = "view_{0}_inters_in_{1}_days";
        const string EVENT_FINISHED_TUTORIAL = "finish_tutorial";
        const string EVENT_FINISH_2ND_GAME = "finish_2nd_game";
        const string EVENT_FINISH_3RD_GAME = "finish_3rd_game";
        const string EVENT_FINISH_5TH_GAME = "finish_5th_games";
        const string EVENT_FINISH_10TH_GAME = "finish_10th_games";
        const string EVENT_FINISH_1ST_GAME_D1 = "finish_1st_game_d1";
        const string EVENT_REVENUE = "in_app_purchase";
        const string AD_IMPRESSION = "ad_impression";
        #endregion

        #region BI Ad Events
        const string EVENT_BI_AD_IMPRESSION = "bi_ad_impression";
        const string EVENT_BI_AD_CLICK = "bi_ad_click";
        const string EVENT_BI_AD_REQUEST_FAILED = "bi_ad_request_failed";
        const string EVENT_BI_AD_VALUE = "bi_ad_value";

        public const string AD_PLATFORM_NONE = "none";
        public const string AD_PLATFORM_ADMOB = "admob";
        public const string AD_PLATFORM_MAX = "max";

        public const string AD_FORMAT_BANNER = "banner";
        public const string AD_FORMAT_INTER = "interstitial";
        public const string AD_FORMAT_REWARDED = "rewarded";

        public const string AD_PLACEMENT_EMPTY = "null";
        public const string AD_PLACEMENT_BANNER_DEFAULT = "banner_all";
        #endregion

        #region User Properties
        const string USER_PROPERTY_JOIN_DATE = "JoinDate";
        const string USER_PROPERTY_MEDIASOURCE = "Mediasource";
        const string USER_PROPERTY_RETAINEDDAYS = "RetainedDays";
        const string USER_PROPERTY_VERSION = "Version";
        const string USER_PROPERTY_LAT = "LimitedAdTracking";
        const string USER_PROPERTY_DEVICE_TYPE = "Device";
        #endregion

        public class UserInfo
        {
            public string JoinDate;
            public string Mediasource;
            public int RetainedDays;
            public string Version;
            public bool LimitedAdTracking;
            public string Device;

            public void Clone(UserInfo other)
            {
                JoinDate = other.JoinDate;
                Mediasource = other.Mediasource;
                RetainedDays = other.RetainedDays;
                Version = other.Version;
                LimitedAdTracking = other.LimitedAdTracking;
                Device = other.Device;
            }
        }

        System.DateTime _firstOpenDate;
        int _retainedDays;

        int _durationSessionTimeout;

        int _interstitialAdViewCount = -1;

        int _bannerAdViewNumberToday = -1;
        int _interstitialAdViewNumberToday = -1;
        int _rewardedAdViewNumberToday = -1;

        float _bannerCumValueUSD = 0f;
        float _interstitialCumValueUSD = 0f;
        float _rewardedCumValueUSD = 0f;
        float _adCumSumValueUSD = 0f;

        List<IAnalyticsAgent> _analyticsAgents = new List<IAnalyticsAgent>();
        IMainAppService _appService;
        Coroutine _reportUserPropertiesCoroutine;

        public System.DateTime FirstOpenDate { get { return _firstOpenDate; } }

        public UserInfo PlayerInfo { get; private set; }

        public int BannerAdNumber { get { return _bannerAdViewNumberToday; } }
        public int InterstitialAdNumber { get { return _interstitialAdViewNumberToday; } }
        public int RewardedAdNumber { get { return _rewardedAdViewNumberToday; } }

        public int RetainedDays
        {
            get { return _retainedDays; }
        }

        public int MatchCount
        {
            get
            {
                if (_matchCount < 0)
                    _matchCount = PlayerPrefs.GetInt(KEY_MATCH_COUNT, 0);

                return _matchCount;
            }

            private set
            {
                if (value != _matchCount)
                {
                    _matchCount = value;
                    PlayerPrefs.SetInt(KEY_MATCH_COUNT, value);
                    PlayerPrefs.Save();
                }
            }
        }

        public int DayMatchCount
        {
            get
            {
                if (_dayMatchCount < 0)
                    _dayMatchCount = PlayerPrefs.GetInt(KEY_DAY_MATCH_COUNT, 0);

                return _dayMatchCount;
            }

            set
            {
                if (value != _dayMatchCount)
                {
                    _dayMatchCount = value;
                    PlayerPrefs.SetInt(KEY_DAY_MATCH_COUNT, value);
                    PlayerPrefs.Save();
                }
            }
        }

        public int SessionCount
        {
            get
            {
                if (_sessionCount < 0)
                    _sessionCount = PlayerPrefs.GetInt(KEY_SESSION_COUNT, 0);

                return _sessionCount;
            }

            private set
            {
                if (_sessionCount != value)
                {
                    _sessionCount = value;
                    PlayerPrefs.SetInt(KEY_SESSION_COUNT, value);
                    PlayerPrefs.Save();
                }
            }
        }

        public int InterstitialAdViewCount
        {
            get
            {
                if (_interstitialAdViewCount < 0)
                    _interstitialAdViewCount = PlayerPrefs.GetInt(KEY_VIEW_INTERSTITIAL_COUNT, 0);

                return _interstitialAdViewCount;
            }

            set
            {
                if (_interstitialAdViewCount != value)
                {
                    _interstitialAdViewCount = value;
                    PlayerPrefs.SetInt(KEY_VIEW_INTERSTITIAL_COUNT, value);
                    PlayerPrefs.Save();
                }
            }
        }

        public event System.Action OnRequiredUserPropertiesReported;

        int _matchCount = -1;
        int _sessionCount = -1;
        int _dayMatchCount = -1;

        public AnalyticsManager(int durationSessionTimeout, IMainAppService appService)
        {
            PlayerInfo = new UserInfo();

            _durationSessionTimeout = durationSessionTimeout;
            _appService = appService;

#if USE_APPSFLYER
            AppsFlyerTrackerCallbacks.OnReceivedConversionData = OnReceivedAppsFlyerConversionData;
#endif
            appService.SubscribeAppPause((pausedStatus) =>
            {
                if (pausedStatus)
                {
                    SaveEnterBackgroundStartTime();
                }
                else
                {
                    CheckToUpdateNewDateData();
                    LogSession();
                }
            });

            appService.SubscribeAppDateChanged(() =>
            {
                LogUserRetained();
                CheckToUpdateNewDateData();
                _appService.StartCoroutine(ReportUserRetainedDays());
            });

            appService.SubscribeAppQuit(() =>
            {
                SaveEnterBackgroundStartTime();
            });

            CheckToUpdateNewDateData();

            _bannerCumValueUSD = PlayerPrefs.GetFloat(KEY_BANNER_AD_CUM_VALUE_USD, 0);
            _interstitialCumValueUSD = PlayerPrefs.GetFloat(KEY_INTERSTITIAL_AD_CUM_VALUE_USD, 0);
            _rewardedCumValueUSD = PlayerPrefs.GetFloat(KEY_REWARDED_AD_CUM_VALUE_USD, 0);
            _adCumSumValueUSD = PlayerPrefs.GetFloat(KEY_AD_CUM_SUM_VALUE_USD, 0);

            _bannerAdViewNumberToday = PlayerPrefs.GetInt(KEY_SHOW_BANNER_NUMBER_TODAY, 0);
            _interstitialAdViewNumberToday = PlayerPrefs.GetInt(KEY_SHOW_INTERSTITIAL_NUMBER_TODAY, 0);
            _rewardedAdViewNumberToday = PlayerPrefs.GetInt(KEY_SHOW_REWARDED_NUMBER_TODAY, 0);
        }

        public void Start()
        {
            LogFirstOpenDate();
            LogUserRetained();
            LogSession();

            if (_reportUserPropertiesCoroutine == null)
                _reportUserPropertiesCoroutine = _appService.StartCoroutine(ReportUserProperties());
        }

        public void AddAgent(IAnalyticsAgent agent)
        {
            _analyticsAgents.Add(agent);
        }

        public void LogTutorialFinished()
        {
            TrackUAEvent(EVENT_FINISHED_TUTORIAL);
        }

        public void LogBussinessEvent(string productName, string productId, double localPrice, string currency, string unityReceipt, string unityTransactionId, string screenName, string promoCode = null)
        {
            var transactionId = AthenaGameOpsUtils.GetIAPProductTransactionId(unityReceipt, unityTransactionId);
            TrackEventWithParameters("bi_business_event", new Dictionary<string, object>()
            {
                {"currency", currency},
                {"product_name", productName},
                {"quantity", 1},
                {"product_id", productId},
                {"value", localPrice},
                {"price", localPrice},
                {"transaction_id", transactionId},
                {"event_source_screen_class", screenName},
                {"promo_code", string.IsNullOrEmpty(promoCode) ? "null" : promoCode}
            });

            // LogRevenue(localPrice, currency, transactionId, productId);
        }

        public void LogRevenue(double value, string currency, string transactionId, string productId)
        {
            foreach (var agent in _analyticsAgents)
            {
                if (agent.IsUAAgent)
                    agent.LogRevenue(EVENT_REVENUE, value, currency, transactionId, productId);
            }
        }
        public void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement)
        {
            foreach (var agent in _analyticsAgents)
            {
                if (agent.IsUAAgent)
                    agent.LogAdRevenue(revenue, networkName, adUnitId, adPlacement);
            }
        }
        public void LogAdImpression(string adPlatform, string adNetwork,string adUnitId, string adFormat, string currency, double revenue, string adPlacement = null)
        {
            TrackEventWithParameters(AD_IMPRESSION, new Dictionary<string, object>()
            {
                {"ad_platform", adPlatform},
                {"ad_source", adNetwork},
                {"ad_platform_unit_id",adUnitId},
                {"ad_source_unit_id", adNetwork},
                {"ad_format", adFormat},
                {"ad_placement", adPlacement ?? "null"},
                {"currency", currency},
                {"value", revenue},
            });
        }
        public void LogFinishedGameCount()
        {
            DayMatchCount++;
            MatchCount++;

            switch (MatchCount)
            {
                case 1:
                    var timeSinceFirstDay = System.DateTime.UtcNow.Subtract(_firstOpenDate);
                    if (timeSinceFirstDay.TotalDays <= 1)
                    {
                        TrackUAEvent(EVENT_FINISH_1ST_GAME_D1);
                    }
                    break;

                case 2:
                    TrackUAEvent(EVENT_FINISH_2ND_GAME);
                    break;
                case 3:
                    TrackEvent(EVENT_FINISH_3RD_GAME, true);
                    break;
                case 5:
                    TrackEvent(EVENT_FINISH_5TH_GAME, true);
                    break;
                case 10:
                    TrackEvent(EVENT_FINISH_10TH_GAME, true);
                    break;
            }
        }

        // If you are using with Firebase Remote Configs, this property is not guaranteed to submit before your first configs fetching
        // This might lead to Firebase A/B testing failed to run.
        // Use this without A/B testing purpose
        public void SetUserProperty(string name, string value)
        {
            foreach (var agent in _analyticsAgents)
                agent.SetUserProperty(name, value);
        }

        public void TrackSceneName(string sceneName, string sceneClass)
        {
            foreach (var agent in _analyticsAgents)
                agent.LogSceneName(sceneName, sceneClass);
        }

        public void TrackEvent(string eventName, bool allAgents = false)
        {
            foreach (var agent in _analyticsAgents)
            {
                if (!allAgents && agent.IsUAAgent)
                    continue;

                agent.LogEvent(eventName);
            }
        }

        public void TrackEventWithParameters(string eventName, Dictionary<string, object> parameters, bool allAgents = false)
        {
            foreach (var agent in _analyticsAgents)
            {
                if (!allAgents && agent.IsUAAgent)
                    continue;

                agent.LogEventWithParameters(eventName, parameters);
            }
        }

        public void TrackUAEvent(string eventName)
        {
            foreach (var agent in _analyticsAgents)
            {
                if (agent.IsUAAgent)
                    agent.LogEvent(eventName);
            }
        }

        public void TrackUAEventWithParameters(string eventName, Dictionary<string, object> parameters)
        {
            foreach (var agent in _analyticsAgents)
            {
                if (agent.IsUAAgent)
                    agent.LogEventWithParameters(eventName, parameters);
            }
        }

        void CheckToUpdateNewDateData()
        {
            var today = System.DateTime.Now;
            string todayStr = string.Format("{0}{1:00}{2}", today.Day, today.Month, today.Year);
            string lastOpenDate = PlayerPrefs.GetString(KEY_LAST_OPEN_DATE, "");
            if (lastOpenDate != todayStr)
            {
                PlayerPrefs.SetString(KEY_LAST_OPEN_DATE, todayStr);
                PlayerPrefs.SetInt(KEY_SHOW_BANNER_NUMBER_TODAY, 0);
                PlayerPrefs.SetInt(KEY_SHOW_INTERSTITIAL_NUMBER_TODAY, 0);
                PlayerPrefs.SetInt(KEY_SHOW_REWARDED_NUMBER_TODAY, 0);

                _bannerAdViewNumberToday = 0;
                _interstitialAdViewNumberToday = 0;
                _rewardedAdViewNumberToday = 0;

                DayMatchCount = 0;
            }
        }

        void SaveEnterBackgroundStartTime()
        {
            PlayerPrefs.SetString(KEY_ENTER_BACKGROUND_START_TIME, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();
        }

        #region BI Ad Events

        public void LogBIAdValueBanner(string adPlatform, string adPlacement, string adUnitId, string adNetwork, float estimatedValue, string currency, int precisionType)
        {
            if (currency.ToLower() == "usd")
            {
                _bannerCumValueUSD += estimatedValue;
                PlayerPrefs.SetFloat(KEY_BANNER_AD_CUM_VALUE_USD, _bannerCumValueUSD);

                _adCumSumValueUSD += estimatedValue;
                PlayerPrefs.SetFloat(KEY_AD_CUM_SUM_VALUE_USD, _adCumSumValueUSD);

                PlayerPrefs.Save();
            }

            LogBIAdValue(adPlatform, AD_FORMAT_BANNER, adPlacement, adUnitId, adNetwork, _bannerAdViewNumberToday, estimatedValue, currency, precisionType, _adCumSumValueUSD);
        }

        public void LogBIAdValueInterstitial(string adPlatform, string adPlacement, string adUnitId, string adNetwork, int prevAdNumber, float estimatedValue, string currency, int precisionType)
        {
            if (prevAdNumber == _interstitialAdViewNumberToday)
                _interstitialAdViewNumberToday++;

            if (currency.ToLower() == "usd")
            {
                _interstitialCumValueUSD += estimatedValue;
                PlayerPrefs.SetFloat(KEY_INTERSTITIAL_AD_CUM_VALUE_USD, _interstitialCumValueUSD);

                _adCumSumValueUSD += estimatedValue;
                PlayerPrefs.SetFloat(KEY_AD_CUM_SUM_VALUE_USD, _adCumSumValueUSD);

                PlayerPrefs.Save();
            }

            LogBIAdValue(adPlatform, AD_FORMAT_INTER, adPlacement, adUnitId, adNetwork, _interstitialAdViewNumberToday, estimatedValue, currency, precisionType, _adCumSumValueUSD);
        }

        public void LogBIAdValueRewarded(string adPlatform, string adPlacement, string adUnitId, string adNetwork, int prevAdNumber, float estimatedValue, string currency, int precisionType)
        {
            if (prevAdNumber == _rewardedAdViewNumberToday)
                _rewardedAdViewNumberToday++;

            if (currency.ToLower() == "usd")
            {
                _rewardedCumValueUSD += estimatedValue;
                PlayerPrefs.SetFloat(KEY_REWARDED_AD_CUM_VALUE_USD, _rewardedCumValueUSD);

                _adCumSumValueUSD += estimatedValue;
                PlayerPrefs.SetFloat(KEY_AD_CUM_SUM_VALUE_USD, _adCumSumValueUSD);
                PlayerPrefs.Save();
            }

            LogBIAdValue(adPlatform, AD_FORMAT_REWARDED, adPlacement, adUnitId, adNetwork, _rewardedAdViewNumberToday, estimatedValue, currency, precisionType, _adCumSumValueUSD);
        }

        void LogBIAdValue(string adPlatform, string adFormat, string adPlacement, string adUnitId, string adNetwork, int adNumber, float estimatedValue, string currency, int precisionType, float cumAdValueUSD)
        {
            var parameters = new Dictionary<string, object>()
            {
                {"ad_platform", adPlatform},
                {"ad_source", adNetwork},
                {"ad_platform_unit_id", adUnitId},
                {"ad_source_unit_id", adNetwork},
                {"ad_format", adFormat},
                {"ad_placement", adPlacement == null ? "null" : adPlacement},
                {"estimate_value_currency", currency},
                // {"ad_number", adNumber},
                {"estimated_value", estimatedValue},
                {"precision_type", precisionType},
                // {"cum_value_usd", cumAdValueUSD}
            };

            if (currency.ToLower() == "usd")
                parameters.Add("estimate_value_in_usd", estimatedValue);

            TrackEventWithParameters(EVENT_BI_AD_VALUE, parameters);
        }
        #endregion

        #region Athena auto-events
        void LogFirstOpenDate()
        {
            string stringFirstOpenDate = PlayerPrefs.GetString(KEY_FIRST_OPEN_DATE);
            if (string.IsNullOrEmpty(stringFirstOpenDate))
            {
                PlayerPrefs.SetString(KEY_FIRST_OPEN_DATE, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                PlayerPrefs.Save();

                _firstOpenDate = System.DateTime.UtcNow;
            }
            else
            {
                long firstOpenDateTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long.TryParse(stringFirstOpenDate, out firstOpenDateTimestamp);
                _firstOpenDate = System.DateTimeOffset.FromUnixTimeSeconds(firstOpenDateTimestamp).UtcDateTime;
            }
        }

        void LogSession()
        {
            var currentTimeInSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long lastTimeEnterBackground = 0;
            long.TryParse(PlayerPrefs.GetString(KEY_ENTER_BACKGROUND_START_TIME, "0"), out lastTimeEnterBackground);
            if (currentTimeInSeconds - lastTimeEnterBackground >= _durationSessionTimeout)
            {
                SessionCount++;

                if (SessionCount == 3)
                {
                    foreach (var agent in _analyticsAgents)
                        agent.LogLoyalUser();
                }

                //Only check first 30 sessions
                if (SessionCount < 30)
                {
                    int secondsPassed = (int)System.DateTime.UtcNow.Subtract(_firstOpenDate).TotalSeconds;
                    int daysSinceJoinDate = secondsPassed / 24 / 60 / 60;

                    //N sessions in M days
                    if (SessionCount == 2)
                    {
                        if (daysSinceJoinDate <= 1)
                        {
                            LogOpenSecondTimeInDay(1);
                        }
                        if (daysSinceJoinDate <= 2)
                        {
                            LogOpenSecondTimeInDay(2);
                        }
                    }

                    //N sessions
                    LogStartSession(SessionCount);
                }

                if (_reportUserPropertiesCoroutine == null)
                    _reportUserPropertiesCoroutine = _appService.StartCoroutine(ReportUserProperties());
            }
        }
        #endregion

        void LogStartSession(int session)
        {
            switch (session)
            {
                case 10:
                case 15:
                case 20:
                    var eventName = string.Format(EVENT_START_SESSION, session);
                    TrackEvent(eventName, true);
                    break;
            }
        }

        void LogUserRetained()
        {
            var timePassed = System.DateTime.UtcNow.Date.Subtract(_firstOpenDate.Date);
            int day = (int)timePassed.TotalDays;
            _retainedDays = day;
            PlayerInfo.RetainedDays = _retainedDays;

            switch (day)
            {
                case 1:
                case 3:
                case 7:
                case 14:
                case 21:
                case 28:
                case 30:
                    {
                        var eventName = string.Format(EVENT_RETAINED_USER_DAY, day);
                        if (PlayerPrefs.GetInt(eventName, 0) == 0)
                        {
                            PlayerPrefs.SetInt(eventName, 1);
                            PlayerPrefs.Save();
                            TrackUAEvent(eventName);
                        }
                    }
                    break;
            }
        }

        void LogOpenSecondTimeInDay(int dayOpen)
        {
            switch (dayOpen)
            {
                case 1:
                case 2:
                    {
                        var eventName = string.Format(EVENT_OPEN_2ND_SESSION_DAY, dayOpen);
                        TrackUAEvent(eventName);
                    }
                    break;
            }
        }

        void LogViewInterstitialAd()
        {
            InterstitialAdViewCount++;

            var timeSinceFirstDay = System.DateTime.UtcNow.Subtract(_firstOpenDate);
            int day = 0;
            if (InterstitialAdViewCount == 30 && timeSinceFirstDay.TotalDays <= 3)
                day = 3;
            else if (InterstitialAdViewCount == 50 && timeSinceFirstDay.TotalDays <= 7)
                day = 7;
            else if (InterstitialAdViewCount == 75 && timeSinceFirstDay.TotalDays <= 14)
                day = 14;

            if (day > 0)
                TrackUAEvent(string.Format(EVENT_VIEW_INTERSTITIAL_DAY, InterstitialAdViewCount, day));
        }

#if USE_ADJUST
        public void AdjustAttributionChangedDelegate(AdjustAttribution attribution)
        {
            _appService.RunOnMainThread(() =>
            {
                var mediaSource = PlayerPrefs.GetString(KEY_ADJUST_MEDIASOURCE, string.Empty);
                if (string.IsNullOrEmpty(mediaSource) && !string.IsNullOrEmpty(attribution.Network))
                {
                    mediaSource = attribution.Network;
                    PlayerPrefs.SetString(KEY_ADJUST_MEDIASOURCE, mediaSource);
                    PlayerPrefs.Save();

                    _appService.StartCoroutine(ReportFirebaseUserProperty(USER_PROPERTY_MEDIASOURCE, mediaSource));
                }
            });
        }
#elif USE_APPSFLYER
        void OnReceivedAppsFlyerConversionData(AppsFlyerConversionData conversionData)
        {
            if (conversionData.is_first_launch)
            {
                var mediaSource = "";
                if (conversionData.af_status == "Organic")
                {
                    mediaSource = "Organic";
                }
                else if (conversionData.af_status == "Non-organic")
                {
                    mediaSource = conversionData.media_source;

                    var fbAppId = _appService.FBAppId;
                    if (fbAppId == mediaSource)
                    {
                        mediaSource = "Facebook Ads";
                    }
                }

                PlayerPrefs.SetString(KEY_APPSPFLYER_MEDIASOURCE, mediaSource);
                PlayerPrefs.Save();
                if (!string.IsNullOrEmpty(mediaSource))
                    _appService.StartCoroutine(ReportFirebaseUserProperty(USER_PROPERTY_MEDIASOURCE, mediaSource));
            }

            PlayerInfo.Mediasource = PlayerPrefs.GetString(KEY_APPSPFLYER_MEDIASOURCE);
        }
#endif
        #region Require User Properties

#if ENABLE_FIREBASE
        IEnumerator ReportUserProperties()
        {
            while (!_appService.IsFirebaseReady)
                yield return null;

#if UNITY_IOS && !UNITY_EDITOR
            // Device
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(KEY_USER_PROPERTY_DEVICE_TYPE, string.Empty)))
            {
                var screenHorizontal = (int)NativeHelper.GetDeviceScreenSizeHorizontal();
                var screenVertical = (int)NativeHelper.GetDeviceScreenSizeVertical();
                var w = Mathf.Min(screenHorizontal, screenVertical);
                var h = Mathf.Max(screenHorizontal, screenVertical);
                var isIphone = SystemInfo.deviceModel.Contains("iPhone");
                var device = string.Format("{0}_{1}_{2}", isIphone ? "iPhone" : "iPad", w, h);
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(USER_PROPERTY_DEVICE_TYPE, device);

                PlayerPrefs.SetString(KEY_USER_PROPERTY_DEVICE_TYPE, device);
            }
            PlayerInfo.Device = PlayerPrefs.GetString(KEY_USER_PROPERTY_DEVICE_TYPE);

#elif UNITY_ANDROID && !UNITY_EDITOR
            // Device
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(KEY_USER_PROPERTY_DEVICE_TYPE, string.Empty)))
            {
                using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (
                        AndroidJavaObject metricsInstance = new AndroidJavaObject("android.util.DisplayMetrics"),
                        activityInstance = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity"),
                        windowManagerInstance = activityInstance.Call<AndroidJavaObject>("getWindowManager"),
                        displayInstance = windowManagerInstance.Call<AndroidJavaObject>("getDefaultDisplay")
                    )
                    {
                        displayInstance.Call("getMetrics", metricsInstance);
                        var density = metricsInstance.Get<float>("density");
                        var heightPixels = metricsInstance.Get<int>("heightPixels");
                        var widthPixels = metricsInstance.Get<int>("widthPixels");
                        var xdpi = metricsInstance.Get<float>("xdpi");
                        var ydpi = metricsInstance.Get<float>("ydpi");

                        float yInches = heightPixels / ydpi;
                        float xInches = widthPixels / xdpi;
                        var diagonalInches = Mathf.Sqrt(xInches * xInches + yInches * yInches);
                        bool isTablet = diagonalInches >= 6.5f;

                        var dpW = (int)(Mathf.Min(heightPixels, widthPixels) / density);
                        var dpH = (int)(Mathf.Max(heightPixels, widthPixels) / density);
                        var device = string.Format("{0}_{1}_{2}", isTablet ? "Tablet" : "Phone", dpW, dpH);
                        Firebase.Analytics.FirebaseAnalytics.SetUserProperty(USER_PROPERTY_DEVICE_TYPE, device);

                        PlayerPrefs.SetString(KEY_USER_PROPERTY_DEVICE_TYPE, device);
                    }
                }
            }
            PlayerInfo.Device = PlayerPrefs.GetString(KEY_USER_PROPERTY_DEVICE_TYPE);
#endif

            var userJoinDate = PlayerPrefs.GetString(KEY_USER_PROPERTY_JOINDATE, string.Empty);
            if (string.IsNullOrEmpty(userJoinDate))
            {
                var localJoinDate = _firstOpenDate.ToLocalTime();
                userJoinDate = string.Format("{0}{1:00}{2:00}", localJoinDate.Year, localJoinDate.Month, localJoinDate.Day);
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(USER_PROPERTY_JOIN_DATE, userJoinDate);
                PlayerPrefs.SetString(KEY_USER_PROPERTY_JOINDATE, userJoinDate);
                PlayerPrefs.Save();
            }
            PlayerInfo.JoinDate = PlayerPrefs.GetString(KEY_USER_PROPERTY_JOINDATE);

            var userAppVersion = PlayerPrefs.GetString(KEY_USER_PROPERTY_VERSION, string.Empty);
            if (userAppVersion != Application.version)
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(USER_PROPERTY_VERSION, Application.version);
                PlayerPrefs.SetString(KEY_USER_PROPERTY_VERSION, Application.version);
                PlayerPrefs.Save();
            }
            PlayerInfo.Version = PlayerPrefs.GetString(KEY_USER_PROPERTY_VERSION);

            yield return ReportUserRetainedDays();

            _reportUserPropertiesCoroutine = null;
            OnRequiredUserPropertiesReported?.Invoke();

            // LAT
            NativeHelper.RequestIsLimitedAdTracking((isLAT) =>
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(USER_PROPERTY_LAT, isLAT.ToString());
                PlayerInfo.LimitedAdTracking = isLAT;
            });
        }

        IEnumerator ReportFirebaseUserProperty(string property, string value)
        {
            while (!_appService.IsFirebaseReady)
                yield return null;

            Firebase.Analytics.FirebaseAnalytics.SetUserProperty(property, value);
        }

        IEnumerator ReportUserRetainedDays()
        {
            var userRetainedDays = PlayerPrefs.GetInt(KEY_USER_PROPERTY_RETAINEDDAYS, -1);
            if (userRetainedDays != _retainedDays)
            {
                PlayerPrefs.SetInt(KEY_USER_PROPERTY_RETAINEDDAYS, _retainedDays);
                PlayerPrefs.Save();

                if (_retainedDays >= 0)
                {
                    while (!_appService.IsFirebaseReady)
                        yield return null;

                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty(USER_PROPERTY_RETAINEDDAYS, _retainedDays.ToString());
                }
            }
        }
#else
        IEnumerator ReportUserProperties()
        {
            OnRequiredUserPropertiesReported?.Invoke();
            yield break;
        }

        IEnumerator ReportFirebaseUserProperty(string property, string value)
        {
            yield break;
        }

        IEnumerator ReportUserRetainedDays()
        {
            yield break;
        }
#endif
        #endregion
    }
}

