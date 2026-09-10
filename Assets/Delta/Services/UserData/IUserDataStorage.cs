using System;

namespace Delta.Services
{
    public interface IUserDataStorage<TUserData> where TUserData : class, IUserData, new()
    {
        TUserData Data { get; }
        bool IsDataReady { get; }

        void Initialize(string key);
        void Load(Action onComplete);
        void Save(TUserData data, Action<bool> onComplete);
        void Delete();
    }
}
