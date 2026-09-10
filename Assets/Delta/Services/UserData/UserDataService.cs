using System;
using System.Collections.Generic;
using System.Linq;
using Delta.Common;
using UnityEngine;

namespace Delta.Services
{
    public class UserDataService<TUserData> : IUserDataService where TUserData : class, IUserData, new()
    {
        private readonly IUserDataStorage<TUserData> _localDataStorage;
#if PLAYFAB_ENABLED
        private readonly IUserDataStorage<TUserData> _remoteDataStorage;
#endif
        private TUserData _cachedUserData;

        private const string KEY_USER_DATA = "USER_DATA";

        private bool _localDataLoaded;
        private bool _remoteDataLoaded;
        private bool _isSaving;
        private bool _isDataReady;
        private readonly object _syncLock = new();

        private Dictionary<string, ItemEntry> _currencyLookup;
        private Dictionary<string, ItemEntry> _itemLookup;

        public IUserData Data => _cachedUserData;
        public bool IsDataReady => _isDataReady;
        public int CurrentLevel => Data.CurrentLevel;

        public event Action OnDataLoaded;

        public UserDataService()
        {
            _localDataStorage = new LocalUserDataStorage<TUserData>();
            _localDataStorage.Initialize(KEY_USER_DATA);

#if PLAYFAB_ENABLED
            var playFabService = ServiceLocator.Get<IPlayFabService>();
            _remoteDataStorage = new PlayFabUserDataStorage<TUserData>(playFabService);
            _remoteDataStorage.Initialize(KEY_USER_DATA);
#endif

            _cachedUserData = new TUserData();
            SetupLookUp();
        }

        private void SetupLookUp()
        {
            _currencyLookup = _cachedUserData.Currencies?.ToDictionary(c => c.Id) ?? new();
            _itemLookup = _cachedUserData.Items?.ToDictionary(i => i.Id) ?? new();
        }

        public void Load()
        {
            _localDataStorage.Load(() =>
            {
                lock (_syncLock) { _localDataLoaded = true; TryCompleteAsyncAndNotify(); }
            });

#if PLAYFAB_ENABLED
            _remoteDataStorage.Load(() =>
            {
                lock (_syncLock) { _remoteDataLoaded = true; TryCompleteAsyncAndNotify(); }
            });
#else
            lock (_syncLock) { _remoteDataLoaded = true; TryCompleteAsyncAndNotify(); }
#endif
        }

        private void TryCompleteAsyncAndNotify()
        {
            if (!_localDataLoaded || !_remoteDataLoaded || _isDataReady)
                return;

            SyncData(syncSuccess =>
            {
                if (syncSuccess)
                {
                    SetupLookUp();
                    _isDataReady = true;
                    OnDataLoaded?.Invoke();
                    Debug.Log("[UserDataService] Data loaded and synchronized successfully");
                }
                else
                {
                    Debug.LogError("[UserDataService] Data synchronization failed, using default data");
                    _cachedUserData = GetBestAvailableData();
                    SetupLookUp();
                    _isDataReady = true;
                    OnDataLoaded?.Invoke();
                }
            });
        }

        private TUserData GetBestAvailableData()
        {
            var localData = _localDataStorage.Data;
#if PLAYFAB_ENABLED
            var remoteData = _remoteDataStorage.Data;
            if (localData != null && remoteData != null)
                return localData.LastUpdate >= remoteData.LastUpdate ? localData : remoteData;
            return localData ?? remoteData ?? new TUserData();
#else
            return localData ?? new TUserData();
#endif
        }

        public void Save(Action onComplete = null)
        {
            if (_isSaving) return;
            _isSaving = true;
            _cachedUserData.LastUpdate = DateTime.UtcNow.Ticks;

            _localDataStorage.Save(_cachedUserData, success =>
            {
                _isSaving = false;
                if (!success) Debug.LogError("[UserDataService] Local save failed");
                onComplete?.Invoke();
            });

#if PLAYFAB_ENABLED
            if (Application.internetReachability != NetworkReachability.NotReachable)
                _remoteDataStorage.Save(_cachedUserData, _ => { });
#endif
        }

        public void Delete()
        {
            _localDataStorage.Delete();
#if PLAYFAB_ENABLED
            _remoteDataStorage.Delete();
#endif
            _cachedUserData = new TUserData();
        }

        private void SyncData(Action<bool> onSyncComplete)
        {
            var localData = _localDataStorage.Data;
#if PLAYFAB_ENABLED
            var remoteData = _remoteDataStorage.Data;
#else
            TUserData remoteData = null;
#endif

            if (localData == null && remoteData == null)
            {
                _cachedUserData = new TUserData();
                onSyncComplete?.Invoke(true);
                return;
            }

            if (localData == null)
            {
                // Assign before invoking the completion callback: the callback rebuilds the
                // item/currency lookups from _cachedUserData, so the data must be in place first.
                _cachedUserData = remoteData;
                _localDataStorage.Save(remoteData, _ => onSyncComplete?.Invoke(true));
                return;
            }

            if (remoteData == null)
            {
                _cachedUserData = localData;
#if PLAYFAB_ENABLED
                _remoteDataStorage.Save(localData, _ => onSyncComplete?.Invoke(true));
#else
                onSyncComplete?.Invoke(true);
#endif
                return;
            }

#if PLAYFAB_ENABLED
            if (localData.LastUpdate >= remoteData.LastUpdate)
            {
                _cachedUserData = localData;
                _remoteDataStorage.Save(localData, success => onSyncComplete?.Invoke(success));
            }
            else
            {
                _cachedUserData = remoteData;
                _localDataStorage.Save(remoteData, success => onSyncComplete?.Invoke(success));
            }
#endif
        }

        #region Currency

        public int GetCurrency(string id)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] GetCurrency called before data is ready: {id}"); return 0; }
            return _currencyLookup?.TryGetValue(id, out var entry) == true ? entry.Quantity : 0;
        }

        public void AddCurrency(string id, int addValue, string from, string levelId)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] AddCurrency called before data is ready: {id}"); return; }
            if (addValue < 0) { Debug.LogError($"[UserDataService] AddCurrency called with negative amount: {addValue}"); return; }

            if (_currencyLookup.TryGetValue(id, out var entry))
                entry.Quantity += addValue;
            else
            {
                entry = new ItemEntry { Id = id, Quantity = addValue };
                _cachedUserData.Currencies.Add(entry);
                _currencyLookup[id] = entry;
            }

            ResourceChangedEvent.Trigger(ResourceChangeEventType.Add, id, addValue, from, null, levelId, entry.Quantity);
        }

        public bool SpendCurrency(string id, int spendValue, string to, string levelId)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] SpendCurrency called before data is ready: {id}"); return false; }
            if (spendValue < 0) { Debug.LogError($"[UserDataService] SpendCurrency called with negative amount: {spendValue}"); return false; }

            if (_currencyLookup.TryGetValue(id, out var entry) && entry.Quantity >= spendValue)
            {
                entry.Quantity -= spendValue;
                ResourceChangedEvent.Trigger(ResourceChangeEventType.Spend, id, spendValue, null, to, levelId, entry.Quantity);
                return true;
            }

            Debug.LogWarning($"[UserDataService] Not enough or non-existing currency: {id}");
            return false;
        }

        public void SetCurrency(string id, int value)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] SetCurrency called before data is ready: {id}"); return; }
            if (value < 0) { Debug.LogError($"[UserDataService] SetCurrency called with negative amount: {value}"); return; }

            if (_currencyLookup.TryGetValue(id, out var entry))
                entry.Quantity = value;
            else
            {
                entry = new ItemEntry { Id = id, Quantity = value };
                _cachedUserData.Currencies.Add(entry);
                _currencyLookup[id] = entry;
            }

            ResourceChangedEvent.Trigger(ResourceChangeEventType.Add, id, entry.Quantity, null, null, null, entry.Quantity);
            Debug.Log($"[UserDataService] Set currency {id} to {value}");
        }

        #endregion

        #region Item

        public int GetItemQuantity(string id)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] GetItemQuantity called before data is ready: {id}"); return 0; }
            return _itemLookup?.TryGetValue(id, out var entry) == true ? entry.Quantity : 0;
        }

        public void AddItem(string id, int addValue, string from, string levelId)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] AddItem called before data is ready: {id}"); return; }
            if (addValue < 0) { Debug.LogError($"[UserDataService] AddItem called with negative amount: {addValue}"); return; }

            if (_itemLookup.TryGetValue(id, out var entry))
                entry.Quantity += addValue;
            else
            {
                entry = new ItemEntry { Id = id, Quantity = addValue };
                _cachedUserData.Items.Add(entry);
                _itemLookup[id] = entry;
            }

            ResourceChangedEvent.Trigger(ResourceChangeEventType.Add, id, addValue, from, null, levelId, entry.Quantity);
        }

        public bool SpendItem(string id, int spendValue, string to, string levelId)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] SpendItem called before data is ready: {id}"); return false; }
            if (spendValue < 0) { Debug.LogError($"[UserDataService] SpendItem called with negative amount: {spendValue}"); return false; }

            if (_itemLookup.TryGetValue(id, out var entry) && entry.Quantity >= spendValue)
            {
                entry.Quantity -= spendValue;
                ResourceChangedEvent.Trigger(ResourceChangeEventType.Spend, id, spendValue, null, to, levelId, entry.Quantity);
                return true;
            }

            Debug.LogWarning($"[UserDataService] Not enough or non-existing item: {id}");
            return false;
        }

        public void SetItem(string id, int value)
        {
            if (!_isDataReady) { Debug.LogWarning($"[UserDataService] SetItem called before data is ready: {id}"); return; }
            if (value < 0) { Debug.LogError($"[UserDataService] SetItem called with negative amount: {value}"); return; }

            if (_itemLookup.TryGetValue(id, out var entry))
                entry.Quantity = value;
            else
            {
                entry = new ItemEntry { Id = id, Quantity = value };
                _cachedUserData.Items.Add(entry);
                _itemLookup[id] = entry;
            }

            ResourceChangedEvent.Trigger(ResourceChangeEventType.Add, id, entry.Quantity, null, null, null, entry.Quantity);
            Debug.Log($"[UserDataService] Set item {id} to {value}");
        }

        #endregion
    }
}
