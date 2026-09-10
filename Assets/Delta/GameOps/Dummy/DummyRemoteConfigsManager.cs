using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.GameOps
{
    public class DummyRemoteConfigsManager : IRemoteConfigsManager
    {
        public class ConfigValue : IConfigValue
        {
            public bool BooleanValue { get { return System.Convert.ToBoolean(_rawValue); } }
            public double DoubleValue { get { return System.Convert.ToDouble(_rawValue); } }
            public long LongValue { get { return System.Convert.ToInt64(_rawValue); } }
            public string StringValue { get { return System.Convert.ToString(_rawValue); } }

            object _rawValue;

            public ConfigValue(object value)
            {
                _rawValue = value;
            }
        }

        Dictionary<string, ConfigValue> _defaultValues = new Dictionary<string, ConfigValue>();
        IMainAppService _appService;

        public bool IsInitialized { get; private set; }
        public long LastSuccessfulFetchTimeStamp { get; private set; }
        public long LastFetchTimeStamp { get; private set; }
        public bool IsFirstFetchCompleted { get { return true; } }
        public bool IsFirstFetchSuccessful { get { return true; } }

        public void CheckApplyLastFetchedConfigs()
        {
            _appService.ShouldApplyFetchedConfigs();
        }

        public void Initialize(Dictionary<string, object> defaultConfigs)
        {
            foreach (var keyPair in defaultConfigs)
                _defaultValues.Add(keyPair.Key, new ConfigValue(keyPair.Value));

            IsInitialized = true;

            _appService.ShouldApplyFetchedConfigs();
            CheckApplyLastFetchedConfigs();
        }

        public void Run()
        {
            LastSuccessfulFetchTimeStamp = LastFetchTimeStamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _appService.ShouldApplyFetchedConfigs();
        }

        public IConfigValue GetValue(string key)
        {
            return _defaultValues.ContainsKey(key) ? _defaultValues[key] : null;
        }

        public DummyRemoteConfigsManager(IMainAppService appService)
        {
            _appService = appService;
        }
    }

}