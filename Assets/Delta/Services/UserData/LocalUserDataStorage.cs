using Newtonsoft.Json;
using UnityEngine;

namespace Delta.Services
{
    public class LocalUserDataStorage<TUserData> : IUserDataStorage<TUserData> where TUserData : class, IUserData, new()
    {
        private string _key;
        private TUserData _data;
        private bool _isDataReady;

        public TUserData Data => _data;
        public bool IsDataReady => _isDataReady;

        public void Initialize(string key)
        {
            _key = key;
            _isDataReady = false;
        }

        public void Load(System.Action onComplete)
        {
            if (PlayerPrefs.HasKey(_key))
            {
                string base64 = PlayerPrefs.GetString(_key);
                byte[] bytes = System.Convert.FromBase64String(base64);
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                _data = JsonConvert.DeserializeObject<TUserData>(json);
            }
            else
            {
                _data = new TUserData();
            }
            
            _data.Initialize();
            _isDataReady = true;
            onComplete?.Invoke();
        }

        public void Save(TUserData data, System.Action<bool> onComplete)
        {
            _data = data;

            string json = JsonConvert.SerializeObject(data);
            string base64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            PlayerPrefs.SetString(_key, base64);
            onComplete?.Invoke(true);
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(_key);
        }
    }
}
