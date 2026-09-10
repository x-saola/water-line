using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayFab.ClientModels;

namespace Delta.Services
{
    public interface IPlayFabService
    {
        string PlayFabId { get; }

        event Action OnLoginSuccess;
        event Action<string> OnLoginFail;

        void LoginWithAndroidDeviceID();
        void LoginWithIOSDeviceID();
        void LoginWithCustomID();
        void UpdateUserData(Dictionary<string, string> data, Action<bool> onComplete);
        void GetUserData(List<string> keys, Action<Dictionary<string, string>> onComplete);
        Task<GetTitleDataResult> GetTitleDataAsync(List<string> keys);
        void DeleteUserData(List<string> keys, Action<bool> onComplete);
    }
}
