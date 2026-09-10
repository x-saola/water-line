using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delta.GameOps;
using CustomUtils;

public partial class OneSDKBridge : SingletonMono<OneSDKBridge>
{
    private DeltaApp _deltaApp;
    public bool IsReadyFirebase { get { return (_deltaApp == null) ? false : _deltaApp.IsFirebaseReady; } }
    protected override void Awake()
    {
        base.Awake();
        SetupAthenaApp();
        AddListener();
    }
    void OnDestroy()
    {
        RemoveListener();
    }
    void AddListener()
    {
        _deltaApp.OnRemoteConfigsUpdated += HandleRemoteConfigUpdated;
        AddListenerAds();
    }
    void RemoveListener()
    {
        _deltaApp.OnRemoteConfigsUpdated += HandleRemoteConfigUpdated;
        RemoveListenerAds();
    }
    void SetupAthenaApp()
    {
        FetchingIsCompleted = false;
        _deltaApp = new GameObject("AthenaApp", typeof(DeltaApp)).GetComponent<DeltaApp>();
        SetupDefaultRemoteConfig();
        _deltaApp.Initialize();
        DontDestroyOnLoad(_deltaApp.gameObject);
    }
    IEnumerator Start()
    {
        if (!_deltaApp.IsFirebaseReady)
        {
            while (!_deltaApp.IsFirebaseReady)
                yield return null;
            //after firebase inited
            //--
        }
    }
    public void ShowDebuggerPanel()
    {
        _deltaApp.AdManager.ShowDebuggerPanel();
    }
}
