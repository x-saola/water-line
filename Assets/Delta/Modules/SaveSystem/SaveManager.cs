using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Delta.Modules.SaveSystem
{
    /// <summary>
    /// Manages saving and loading of all ISaveable objects in the scene.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        // registry of saveables for quick lookup
        private static readonly List<ISaveable> registeredSaveables = new List<ISaveable>();
        // cached save entries loaded once
        private Dictionary<string, EntityData> savedEntitiesCache;

        private string saveFilePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");
            Debug.Log("Save file path: " + saveFilePath);
        }
        
        /// <summary>
        /// Register a saveable to be included in SaveAll/LoadAll without using FindObjectOfType.
        /// </summary>
        public static void Register(ISaveable s)
        {
            if (!registeredSaveables.Contains(s))
                registeredSaveables.Add(s);
        }

        /// <summary>
        /// Unregister a saveable when destroyed or disabled.
        /// </summary>
        public static void Unregister(ISaveable s)
        {
            registeredSaveables.Remove(s);
        }

        /// <summary>
        /// Save the state of all ISaveable objects to disk.
        /// </summary>
        [ContextMenu("Save All")]        
        public void SaveAll()
        {
            var saveables = registeredSaveables;
            var dataList = new List<EntityData>();

            foreach (var s in saveables)
            {
                try
                {
                    var stateObj = s.CaptureState();
                    var jsonState = JsonConvert.SerializeObject(stateObj);
                    var typeName = stateObj.GetType().AssemblyQualifiedName;

                    dataList.Add(new EntityData
                    {
                        Id = s.SaveId,
                        StateJson = jsonState,
                        StateType = typeName
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving {s.SaveId}: {e}");
                }
            }

            var saveData = new SaveData { Entities = dataList };
            var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"Saved {dataList.Count} entities to {saveFilePath}");
        }

        // load file contents into cache once
        private void EnsureCacheLoaded()
        {
            if (savedEntitiesCache != null) return;
            if (!File.Exists(saveFilePath))
            {
                Debug.LogWarning("Save file not found: " + saveFilePath);
                savedEntitiesCache = new Dictionary<string, EntityData>();
                return;
            }
            var json = File.ReadAllText(saveFilePath);
            var saveData = JsonConvert.DeserializeObject<SaveData>(json);
            savedEntitiesCache = saveData.Entities.ToDictionary(e => e.Id);
        }

        /// <summary>
        /// Load the state for all ISaveable objects from disk.
        /// </summary>
        [ContextMenu("Load All")]
        public void LoadAll()
        {
            EnsureCacheLoaded();
            var entityDict = savedEntitiesCache;

            var saveables = registeredSaveables;
            int restored = 0;

            foreach (var s in saveables)
            {
                if (entityDict.TryGetValue(s.SaveId, out var entity))
                {
                    try
                    {
                        var stateType = Type.GetType(entity.StateType);
                        var stateObj = JsonConvert.DeserializeObject(entity.StateJson, stateType);
                        s.RestoreState(stateObj);
                        restored++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error loading {s.SaveId}: {e}");
                    }
                }
                else
                {
                    Debug.LogWarning($"No save data for {s.SaveId}");
                }
            }

            Debug.Log($"Restored {restored} entities from {saveFilePath}");
        }

        // convenience static methods, mimic PlayerPrefs API
        public static void SaveAllState() => Instance?.SaveAll();
        public static void LoadAllState() => Instance?.LoadAll();

        /// <summary>
        /// Try to get the saved state object for a given ID without restoring to a component.
        /// </summary>
        public static bool TryGetState<T>(string id, out T state)
        {
            if (Instance == null) { state = default; return false; }
            Instance.EnsureCacheLoaded();
            if (Instance.savedEntitiesCache != null && Instance.savedEntitiesCache.TryGetValue(id, out var entity))
            {
                var targetType = Type.GetType(entity.StateType);
                state = (T)JsonConvert.DeserializeObject(entity.StateJson, targetType);
                return true;
            }
            state = default;
            return false;
        }

        /// <summary>
        /// Get the saved state object for a given ID, or default if not present.
        /// </summary>
        public static T GetState<T>(string id)
        {
            return TryGetState<T>(id, out var state) ? state : default;
        }
        
        /// <summary>
        /// Remove a specific save entry by ID and update the save file.
        /// </summary>
        public void ClearEntry(string id)
        {
            EnsureCacheLoaded();
            if (savedEntitiesCache.Remove(id))
            {
                WriteCacheToFile();
                Debug.Log($"Cleared save entry: {id}");
            }
        }
        
        /// <summary>
        /// Remove all save data (delete file and clear cache).
        /// </summary>
        public void ClearAll()
        {
            savedEntitiesCache = new Dictionary<string, EntityData>();
            if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
            Debug.Log("Cleared all save data");
        }
        
        /// <summary>
        /// Write current cache contents back to the save file.
        /// </summary>
        private void WriteCacheToFile()
        {
            var saveData = new SaveData { Entities = savedEntitiesCache.Values.ToList() };
            var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(saveFilePath, json);
        }

        [Serializable]
        private class EntityData
        {
            public string Id;
            public string StateJson;
            public string StateType;
        }

        [Serializable]
        private class SaveData
        {
            public List<EntityData> Entities = new List<EntityData>();
        }
    }
}
