#if USE_ADDRESSABLES
using System.Collections.Generic;
using System.Threading.Tasks;
using Delta.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Delta.Services
{
    public class AddressablesService : IService
    {
        private Dictionary<object, AsyncOperationHandle> _handles;
        private List<IResourceLocator> _updateCatalogsResult;

        public System.Action<float> OnDownloadProgress;

        public void Initialize()
        {
            _handles = new();
        }

        public void Shutdown()
        {
            ReleaseAll();
            _handles = null;
        }

        public async Task<T> LoadAssetAsync<T>(string path) where T : Object
        {
            if (_handles.ContainsKey(path))
            {
                return _handles[path].Result as T;
            }

            AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(path);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[path] = handle;
                return handle.Result as T;
            }

            Debug.LogError($"Failed to load asset from {path}");
            return null;
        }

        public async Task<T> LoadAssetAsync<T>(AssetReference reference) where T : Object
        {
            if (_handles.ContainsKey(reference))
            {
                return _handles[reference].Result as T;
            }

            AsyncOperationHandle handle = Addressables.LoadAssetAsync<T>(reference);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[reference] = handle;
                return handle.Result as T;
            }

            Debug.LogError($" Failed to load asset reference {reference.RuntimeKey}");
            return null;
        }

        public async Task<IList<T>> LoadAssetsAsync<T>(List<string> paths) where T : Object
        {
            var handle = Addressables.LoadAssetsAsync<T>(paths, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (var path in paths)
                {
                    _handles[path] = handle;
                }
                return handle.Result;
            }

            Debug.LogError($"Failed to load assets of {typeof(T)}");
            return null;
        }

        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : Object
        {
            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _handles[label] = handle;
                return handle.Result;
            }

            Addressables.LoadResourceLocationsAsync(label);

            Debug.LogError($"Failed to load assets of {typeof(T)} with label {label}");
            return null;
        }

        public void Release(string path)
        {
            if (_handles.TryGetValue(path, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(path);
            }
        }

        public void Release(AssetReference reference)
        {
            if (_handles.TryGetValue(reference, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(reference);
            }
        }

        public void ReleaseAll()
        {
            foreach (var handle in _handles.Values)
            {
                Addressables.Release(handle);
            }
            _handles.Clear();
        }

        public async Task CheckAndUpdateCatalogAsync()
        {
            var initHandle = Addressables.InitializeAsync();
            await initHandle.Task;

            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task;

            if (checkHandle.Status == AsyncOperationStatus.Succeeded)
            {
                var catalogsToUpdate = checkHandle.Result;
                if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
                {
                    var updateCatalogsHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                    await updateCatalogsHandle.Task;

                    _updateCatalogsResult = updateCatalogsHandle.Result;
                    
                    Addressables.Release(updateCatalogsHandle);
                }
                else
                {
                    Debug.Log("No catalog updates available.");
                }
            }
            else
            {
                Debug.LogError("Failed to check for catalog updates.");
            }

            Addressables.Release(checkHandle);
            Addressables.CleanBundleCache();
        }

        public async Task CheckForUpdatesAsync()
        {
            await CheckAndUpdateCatalogAsync();

            var locators = _updateCatalogsResult;
            if (locators != null && locators.Count > 0)
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(locators);
                await sizeHandle.Task;

                long downloadSize = sizeHandle.Result;
                if (downloadSize > 0)
                {
                    Debug.Log($"Download size: {downloadSize / 1024f / 1024f:F2} MB");

                    var downloadHandle = Addressables.DownloadDependenciesAsync(locators, Addressables.MergeMode.Union, false);
                    while (!downloadHandle.IsDone)
                    {
                        float percent = downloadHandle.PercentComplete;
                        Debug.Log($"Download progress: {percent * 100f:F2}%");

                        OnDownloadProgress?.Invoke(percent);

                        await Task.Yield();
                    }

                    Debug.Log("All assets have been downloaded.");
                    Addressables.Release(downloadHandle);
                }

                Addressables.Release(sizeHandle);
            }
        }
    }
}
#endif
