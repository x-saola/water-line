using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConfigValue
{
    bool BooleanValue { get; }
    double DoubleValue { get; }
    long LongValue { get; }
    string StringValue { get; }
}

public interface IRemoteConfigsManager
{
    bool IsInitialized { get; }
    long LastSuccessfulFetchTimeStamp { get; }
    bool IsFirstFetchCompleted { get; }
    bool IsFirstFetchSuccessful { get; }
    void CheckApplyLastFetchedConfigs();
    void Initialize(Dictionary<string, object> defaultConfigs);
    void Run();
    IConfigValue GetValue(string key);
}
