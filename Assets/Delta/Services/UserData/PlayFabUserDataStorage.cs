using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Delta.Services
{
    public class PlayFabUserDataStorage<TUserData> : IUserDataStorage<TUserData> where TUserData : class, IUserData, new()
    {
        private readonly IPlayFabService _playFabService;
        private string _key;
        private TUserData _data;
        private bool _isDataReady;

        public TUserData Data => _data;
        public bool IsDataReady => _isDataReady;

        public PlayFabUserDataStorage(IPlayFabService playFabService)
        {
            _playFabService = playFabService;
        }

        public void Initialize(string key)
        {
            _key = key;
            _isDataReady = false;
        }

        public void Load(Action onComplete)
        {
            _playFabService.GetUserData(new List<string> { _key }, result =>
            {
                if (result != null && result.ContainsKey(_key))
                {
                    _data = JsonConvert.DeserializeObject<TUserData>(result[_key]);
                }
                else
                {
                    _data = new TUserData();
                }

                _data.Initialize();
                _isDataReady = true;
                onComplete?.Invoke();
            });
        }

        public void Save(TUserData data, Action<bool> onComplete)
        {
            _data = data;

            string json = JsonConvert.SerializeObject(data);
            var updateRequest = new Dictionary<string, string>
            {
                { _key, json }
            };

            _playFabService.UpdateUserData(updateRequest, success =>
            {
                onComplete?.Invoke(success);
            });
        }

        public void Delete()
        {
            _playFabService.DeleteUserData(new List<string> { _key }, null);
        }
    }
}
