using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Delta.Services
{
    public class PlayFabService : IPlayFabService
    {
        private string _playFabId;
        private bool _isLoggedIn;

        public string PlayFabId => _playFabId;

        public event Action OnLoginSuccess;
        public event Action<string> OnLoginFail;

        public void LoginWithAndroidDeviceID()
        {
            var request = new LoginWithAndroidDeviceIDRequest
            {
                CreateAccount = true,
                AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
                AndroidDevice = SystemInfo.deviceModel,
                OS = SystemInfo.operatingSystem,
            };
            PlayFabClientAPI.LoginWithAndroidDeviceID(request, OnLoginWithAnonymousIdSuccess, OnLoginWithAnonymousIdFail);
        }

        public void LoginWithIOSDeviceID()
        {
            var request = new LoginWithIOSDeviceIDRequest
            {
                CreateAccount = true,
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                DeviceModel = SystemInfo.deviceModel,
                OS = SystemInfo.operatingSystem,
            };
            PlayFabClientAPI.LoginWithIOSDeviceID(request, OnLoginWithAnonymousIdSuccess, OnLoginWithAnonymousIdFail);
        }

        public void LoginWithCustomID()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = Guid.NewGuid().ToString(),
                CreateAccount = true,
            };
            PlayFabClientAPI.LoginWithCustomID(request, OnLoginWithAnonymousIdSuccess, OnLoginWithAnonymousIdFail);
        }

        private void OnLoginWithAnonymousIdSuccess(LoginResult result)
        {
            Debug.Log("[PlayFab] Congratulations, login PlayFab success!");
            _playFabId = result.PlayFabId;
            _isLoggedIn = true;
            OnLoginSuccess?.Invoke();
        }

        private void OnLoginWithAnonymousIdFail(PlayFabError error)
        {
            Debug.LogWarning("[PlayFab] Something went wrong with your first API call.  :(");
            Debug.LogError(error.GenerateErrorReport());
            OnLoginFail?.Invoke(error.GenerateErrorReport());
        }

        public void UpdateUserData(Dictionary<string, string> data, Action<bool> onComplete)
        {
            if (!_isLoggedIn) { Debug.LogError("[PlayFab] Can't update user data, not logged in."); onComplete?.Invoke(false); return; }

            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = data },
                result => { Debug.Log("[PlayFab] User data updated successfully."); onComplete?.Invoke(true); },
                error => { Debug.LogError("[PlayFab] Failed to update user data: " + error.GenerateErrorReport()); onComplete?.Invoke(false); });
        }

        public void GetUserData(List<string> keys, Action<Dictionary<string, string>> onComplete)
        {
            if (!_isLoggedIn) { Debug.LogError("[PlayFab] Can't get user data, not logged in."); onComplete?.Invoke(null); return; }

            PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = keys },
                result =>
                {
                    Debug.Log("[PlayFab] User data retrieved successfully.");
                    var data = new Dictionary<string, string>();
                    foreach (var entry in result.Data) data[entry.Key] = entry.Value.Value;
                    onComplete?.Invoke(data);
                },
                error => { Debug.LogError("[PlayFab] Failed to get user data: " + error.GenerateErrorReport()); onComplete?.Invoke(null); });
        }

        public Task<GetTitleDataResult> GetTitleDataAsync(List<string> keys)
        {
            if (!_isLoggedIn) { Debug.LogError("[PlayFab] Can't get title data, not logged in."); return null; }

            var tcs = new TaskCompletionSource<GetTitleDataResult>();
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest { Keys = keys },
                result => tcs.SetResult(result),
                error => { Debug.Log("[PlayFab] Failed to get title data: " + error.GenerateErrorReport()); tcs.SetResult(null); });
            return tcs.Task;
        }

        public void DeleteUserData(List<string> keys, Action<bool> onComplete)
        {
            if (!_isLoggedIn) { Debug.LogError("[PlayFab] Can't delete user data, not logged in."); onComplete?.Invoke(false); return; }

            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { KeysToRemove = keys },
                result => { Debug.Log("[PlayFab] User data deleted successfully."); onComplete?.Invoke(true); },
                error => { Debug.LogError("[PlayFab] Failed to delete user data: " + error.GenerateErrorReport()); onComplete?.Invoke(false); });
        }
    }
}
