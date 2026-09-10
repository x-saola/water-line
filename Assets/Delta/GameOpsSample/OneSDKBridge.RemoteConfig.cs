using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delta.GameOps;
using CustomUtils;

public partial class OneSDKBridge : SingletonMono<OneSDKBridge>
{
    public bool FetchingIsCompleted { get; set; }
    public bool IsFirstFetchCompleted => _deltaApp.RemoteConfigsManager.IsFirstFetchCompleted;

    private void HandleRemoteConfigUpdated()
    {
        Debug.Log("[onesdk] OnRemoteConfigFetched");
        FetchingIsCompleted = true;
        
    }

    public IConfigValue GetRemoteConfigData(string key)
    {
        return _deltaApp.RemoteConfigsManager.GetValue(key);
    }
    private void SetupDefaultRemoteConfig()
    {
        // _athenaApp.AddDefaultRemoteConfig(key_sample, value_sample);
    }
}
