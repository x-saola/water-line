using System;
using Delta.Common;

namespace Delta.Services
{
    public interface IUserDataService
    {
        IUserData Data { get; }
        bool IsDataReady { get; }
        int CurrentLevel { get; }

        event Action OnDataLoaded;

        void Load();
        void Save(Action onComplete = null);
        void Delete();

        int GetCurrency(string id);
        void AddCurrency(string id, int addValue, string from, string levelId);
        bool SpendCurrency(string id, int spendValue, string to, string levelId);
        void SetCurrency(string id, int value);

        int GetItemQuantity(string id);
        void AddItem(string id, int addValue, string from, string levelId);
        bool SpendItem(string id, int spendValue, string to, string levelId);
        void SetItem(string id, int value);
    }
}
