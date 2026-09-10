#if ENABLE_FIREBASE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.RemoteConfig;
using System.Threading.Tasks;
using Firebase.Extensions;

namespace Delta.GameOps
{
    public class RemoteConfigsManager : IRemoteConfigsManager
    {
        enum RemoteConfigsStatus
        {
            ShouldFetch,
            Fetching,
            FetchedSuccess
        }

        struct ConfigValueWrapper : IConfigValue
        {
            Firebase.RemoteConfig.ConfigValue _orignalValue;

            public ConfigValueWrapper(Firebase.RemoteConfig.ConfigValue value)
            {
                _orignalValue = value;
            }

            public bool BooleanValue { get { return _orignalValue.BooleanValue; } }
            public double DoubleValue { get { return _orignalValue.DoubleValue; } }
            public long LongValue { get { return _orignalValue.LongValue; } }
            public string StringValue { get { return _orignalValue.StringValue; } }
        }

        const int DURATION_THROTTLED_WAIT = 30 * 60;
        const string KEY_REMOTE_CONFIGS_EXISTED = "REMOTE_CONFIGS_EXISTED";

        int _durationCacheExpired;
        int _durationRetryNextFetch;
        bool _isRemoteConfigsExisted;

        RemoteConfigsStatus _remoteConfigsStatus = RemoteConfigsStatus.ShouldFetch;
        long _shouldWaitForNextFetchTime;
        bool _shouldApplyFetchedResult;
        bool _isFirstFetchCompleted;
        bool _isFirstFetchSuccessful;

        public bool IsInitialized { get; private set; }

        public bool IsFirstFetchCompleted { get { return _isFirstFetchCompleted; } }
        public bool IsFirstFetchSuccessful { get { return _isFirstFetchSuccessful; } }
        public long LastSuccessfulFetchTimeStamp { get; private set; }
        public long LastFetchTimeStamp { get; private set; }

        IMainAppService _appService;

        bool IsRemoteConfigsExisted
        {
            get { return _isRemoteConfigsExisted; }
            set
            {
                if (value != _isRemoteConfigsExisted)
                {
                    _isRemoteConfigsExisted = value;
                    PlayerPrefs.SetInt(KEY_REMOTE_CONFIGS_EXISTED, value ? 1 : 0);
                    PlayerPrefs.Save();
                }
            }
        }

        public RemoteConfigsManager(int durationCacheExpired, int durationRetryNextFetch, IMainAppService appService)
        {
            _appService = appService;
            _durationCacheExpired = durationCacheExpired;
            _durationRetryNextFetch = durationRetryNextFetch;
            _isRemoteConfigsExisted = PlayerPrefs.GetInt(KEY_REMOTE_CONFIGS_EXISTED, 0) > 0 ? true : false;
        }

        public void CheckApplyLastFetchedConfigs()
        {
            if (_isRemoteConfigsExisted && _appService.IsFirebaseReady && _shouldApplyFetchedResult)
            {
                Debug.Log("[RemoteConfigs] Apply last fetched remote configs!");
                _appService.ShouldApplyFetchedConfigs();
                _shouldApplyFetchedResult = false;
            }
        }

        public void Initialize(Dictionary<string, object> defaultConfigs)
        {
            if (IsInitialized)
                return;

            Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaultConfigs).ContinueWithOnMainThread(setupTask =>
            {
                IsInitialized = true;

                if (_isRemoteConfigsExisted)
                {
                    _shouldApplyFetchedResult = true;
                    CheckApplyLastFetchedConfigs();
                }
                // apply default configs
                else
                {
                    _appService.ShouldApplyFetchedConfigs();
                }
            });
        }

        public void Run()
        {
            _appService.StartCoroutine(RemoteConfigsTask());
        }

        public IConfigValue GetValue(string key)
        {
            var value = new ConfigValueWrapper(FirebaseRemoteConfig.DefaultInstance.GetValue(key));
            return value;
        }
        public Firebase.RemoteConfig.ConfigValue GetFirebaseValue(string key)
        {
            return FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        }

        IEnumerator RemoteConfigsTask()
        {
            while (true)
            {
                switch (_remoteConfigsStatus)
                {
                    case RemoteConfigsStatus.ShouldFetch:
                        {
                            var now = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds();
                            var dt = _shouldWaitForNextFetchTime - now;
                            if (dt > 0)
                            {
                                Debug.LogFormat("[RemoteConfigs] Waiting for next fetch time: {0}s", dt);
                                yield return new WaitForSeconds(dt);
                            }

                            _remoteConfigsStatus = RemoteConfigsStatus.Fetching;
                            FetchRemoteConfigsAsync();
                        }

                        break;

                    case RemoteConfigsStatus.FetchedSuccess:
                        {
                            Debug.LogFormat("[RemoteConfigs] Waiting for cache expired: {0}s", _durationCacheExpired);
                            yield return new WaitForSeconds(_durationCacheExpired);

                            // Ready for next fetch
                            _remoteConfigsStatus = RemoteConfigsStatus.ShouldFetch;
                        }
                        break;
                }

                yield return null;
            }
        }

        Task FetchRemoteConfigsAsync()
        {
            Debug.Log("[RemoteConfigs] FetchRemotConfigsAsync");

            LastFetchTimeStamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // FetchAsync only fetches new data if the current data is older than the provided
            // timespan.  Otherwise it assumes the data is "recent enough", and does nothing.
            // By default the timespan is 12 hours, and for production apps, this is a good
            // number.  For this example though, it's set to a timespan of zero, so that
            // changes in the console will always show up immediately.
            var cacheExpiration = new System.TimeSpan(0, 0, _durationCacheExpired);
            System.Threading.Tasks.Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(cacheExpiration);
            return fetchTask.ContinueWithOnMainThread(FetchRemoteConfigsComplete);
        }

        void FetchRemoteConfigsComplete(Task fetchTask)
        {
            if (fetchTask.IsCompleted)
            {
                var info = FirebaseRemoteConfig.DefaultInstance.Info;
                if (info.LastFetchStatus == LastFetchStatus.Success)
                {
                    Debug.Log("[RemoteConfigs] FetchRemoteConfigsComplete => Activating...!");

                    FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(task =>
                    {
                        Debug.Log(string.Format("[RemoteConfigs] Remote data is loaded and ready (last fetch time {0}).", info.FetchTime));

                        IsRemoteConfigsExisted = true;
                        LastSuccessfulFetchTimeStamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        _isFirstFetchCompleted = true;
                        _isFirstFetchSuccessful = true;
                        _remoteConfigsStatus = RemoteConfigsStatus.FetchedSuccess;
                        _shouldApplyFetchedResult = true;
                    });
                }
                else if (info.LastFetchStatus == LastFetchStatus.Failure)
                {
                    _remoteConfigsStatus = RemoteConfigsStatus.ShouldFetch;
                    var waitForNextFetchDuration = _durationRetryNextFetch;
                    if (info.LastFetchFailureReason == FetchFailureReason.Throttled)
                    {
                        waitForNextFetchDuration = DURATION_THROTTLED_WAIT;
                    }
                    _shouldWaitForNextFetchTime = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds() + waitForNextFetchDuration;

                    Debug.LogFormat("[RemoteConfigs] FetchRemoteConfigs failed with reason: {0}", info.LastFetchFailureReason);
                }
                else
                {
                    Debug.LogFormat("[RemoteConfigs] Fetch status: {0} ({1})", info.LastFetchStatus, info.LastFetchFailureReason);
                }
            }
            else
            {
                _remoteConfigsStatus = RemoteConfigsStatus.ShouldFetch;
                _shouldWaitForNextFetchTime = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds() + _durationRetryNextFetch;
            }
        }
    }
}
#endif