using UnityEngine;
using System.Collections;

namespace Delta.GameOps
{
    public delegate void AppPaused(bool pauseStatus);
    public delegate void AppPerfChanged(bool lowPerformance);

    public interface IMainAppService
    {
        void RunOnMainThread(System.Action action);
        Coroutine StartCoroutine(IEnumerator enumerator);
        void StopCoroutine(Coroutine coroutine);
        bool IsAppPaused { get; }
        bool ShouldMuteVideoAd();
        bool IsFirebaseReady { get; }
#if USE_ADJUST
        bool IsAdjustReady { get; }
#endif
        string FBAppId { get; }
        void SetAppInputActive(bool active);
        void SubscribeAppPause(AppPaused listener);
        void UnSubscribeAppPause(AppPaused listener);
        void SubscribeAppQuit(System.Action listener);
        void UnSubscribeAppQuit(System.Action listener);
        void SubscribeAppPerfChanged(AppPerfChanged listener);
        void UnSubscribeAppPerfChanged(AppPerfChanged listener);
        void SubscribeAppDateChanged(System.Action onDateChanged);
        void UnSubscribeAppDateChanged(System.Action onDateChanged);
        void ShouldApplyFetchedConfigs();
    }
}

