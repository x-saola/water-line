#if ENABLE_FIREBASE
using System.Collections;
using System.Collections.Generic;

namespace Delta.GameOps
{
    public class FirebaseAnalyticsAgent : IAnalyticsAgent
    {
        const string EVENT_LOYAL_USERS = "LoyalUsers";

        struct EventData
        {
            public string EventName;
            public Dictionary<string, object> Parameters;
        }

        struct UserProperty
        {
            public string Name;
            public string Value;
        }

        struct SceneData
        {
            public string Name;
            public string Class;
        }

        List<EventData> _pendingEvents = new List<EventData>();
        List<UserProperty> _pendingUserProperties = new List<UserProperty>();
        List<SceneData> _pendingSceneNames = new List<SceneData>();
        IMainAppService _appService;

        public bool IsUAAgent { get { return false; } }

        public FirebaseAnalyticsAgent(IMainAppService appService)
        {
            _appService = appService;
            _appService.StartCoroutine(WaitForFirebaseReady());
        }

        public void LogRevenue(string eventName, double _adjustVerifyingPrice, string _adjustVerifyingCurrency, string _adjustVerifyingTransactionId, string productId, string googlePublickKey = "", string unityReceiptPayload = "")
        {

        }
        public void LogAdRevenue(double revenue, string networkName, string adUnitId, string adPlacement)
        {
            
        }
        public void LogSceneName(string sceneName, string sceneClass)
        {
            if (_appService.IsFirebaseReady)
            {
                var firebaseParams = new Firebase.Analytics.Parameter[2];
                firebaseParams[0] = new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterScreenName, sceneName);
                firebaseParams[1] = new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterScreenClass, sceneClass);
                Firebase.Analytics.FirebaseAnalytics.LogEvent(Firebase.Analytics.FirebaseAnalytics.EventScreenView, firebaseParams);
            }
            else
            {
                _pendingSceneNames.Add(new SceneData()
                {
                    Name = sceneName,
                    Class = sceneClass
                });
            }
        }

        public void SetUserProperty(string name, string value)
        {
            if (_appService.IsFirebaseReady)
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(name, value);
            }
            else
            {
                _pendingUserProperties.Add(new UserProperty()
                {
                    Name = name,
                    Value = value
                });
            }
        }

        public void LogEvent(string eventName)
        {
            if (_appService.IsFirebaseReady)
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
            }
            else
            {
                _pendingEvents.Add(new EventData() { EventName = eventName });
            }
        }

        public void LogEventWithParameters(string eventName, Dictionary<string, object> parameters)
        {
            if (_appService.IsFirebaseReady)
            {
                var firebaseParams = new Firebase.Analytics.Parameter[parameters.Count];
                int idx = 0;
                foreach (var keyPair in parameters)
                {
                    if (IsIntegralType(keyPair.Value))
                        firebaseParams[idx++] = new Firebase.Analytics.Parameter(keyPair.Key, System.Convert.ToInt64(keyPair.Value));
                    else if (keyPair.Value is float || keyPair.Value is double)
                        firebaseParams[idx++] = new Firebase.Analytics.Parameter(keyPair.Key, System.Convert.ToDouble(keyPair.Value));
                    else
                    {
                        var paramValue = keyPair.Value == null ? string.Empty : keyPair.Value.ToString();
                        if (paramValue.Length > 100)
                            paramValue = paramValue.Substring(0, 100);

                        firebaseParams[idx++] = new Firebase.Analytics.Parameter(keyPair.Key, paramValue);
                    }

                }

                Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, firebaseParams);
            }
            else
            {
                _pendingEvents.Add(new EventData() { EventName = eventName, Parameters = parameters });
            }
        }

        public void LogLoyalUser()
        {
            LogEvent(EVENT_LOYAL_USERS);
        }

        public void LogShowBannerAd()
        {

        }

        public void LogShowInterstitialAd()
        {

        }

        public void LogBannerAdClicked(string adUnitId, string adNetwork)
        {

        }

        public void LogInterstitialAdClicked(string adUnitId, string adNetwork)
        {

        }

        public void LogCompleteRewardedAd(string adUnitId, string adNetwork)
        {

        }

        IEnumerator WaitForFirebaseReady()
        {
            while (!_appService.IsFirebaseReady)
                yield return null;

            if (_pendingEvents.Count > 0)
            {
                foreach (var eventData in _pendingEvents)
                {
                    if (eventData.Parameters == null)
                        LogEvent(eventData.EventName);
                    else
                        LogEventWithParameters(eventData.EventName, eventData.Parameters);
                }

                _pendingEvents.Clear();
            }

            if (_pendingUserProperties.Count > 0)
            {
                foreach (var property in _pendingUserProperties)
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty(property.Name, property.Value);

                _pendingUserProperties.Clear();
            }

            if (_pendingSceneNames.Count > 0)
            {
                foreach (var scene in _pendingSceneNames)
                {
                    var firebaseParams = new Firebase.Analytics.Parameter[2];
                    firebaseParams[0] = new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterScreenName, scene.Name);
                    firebaseParams[1] = new Firebase.Analytics.Parameter(Firebase.Analytics.FirebaseAnalytics.ParameterScreenClass, scene.Class);
                    Firebase.Analytics.FirebaseAnalytics.LogEvent(Firebase.Analytics.FirebaseAnalytics.EventScreenView, firebaseParams);
                }

                _pendingSceneNames.Clear();
            }
        }

        bool IsIntegralType(object value)
        {
            return value is byte
            || value is sbyte
            || value is ushort
            || value is short
            || value is int
            || value is uint
            || value is long
            || value is ulong;
        }
    }
}
#endif