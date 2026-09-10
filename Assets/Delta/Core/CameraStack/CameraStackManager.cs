using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Delta.Core
{
    public class CameraStackManager : MonoBehaviour
    {
        public static CameraStackManager Instance { get; private set; }

        Camera _baseCamera;
        readonly SortedList<int, List<Camera>> _overlays = new SortedList<int, List<Camera>>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            // Belt-and-suspenders: parent Bootstrapper GO already calls DontDestroyOnLoad,
            // but guard here in case this component is placed elsewhere.
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterBase(Camera cam)
        {
            _baseCamera = cam;
            RebuildStack();
        }

        public void UnregisterBase()
        {
            _baseCamera = null;
            // Stack is now without a base — overlays remain registered and will be
            // re-added automatically when the next RegisterBase call rebuilds the stack.
        }

        public void RegisterOverlay(Camera cam, CameraLayer layer)
        {
            cam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            int key = (int)layer;
            if (!_overlays.ContainsKey(key))
                _overlays[key] = new List<Camera>();

            if (!_overlays[key].Contains(cam))
                _overlays[key].Add(cam);

            RebuildStack();
        }

        public void UnregisterOverlay(Camera cam)
        {
            foreach (var list in _overlays.Values)
                list.Remove(cam);

            RebuildStack();
        }

        public void RebuildStack()
        {
            // Purge Unity-destroyed camera refs (Unity overloads == for destroyed objects).
            foreach (var list in _overlays.Values)
                list.RemoveAll(c => c == null);

            // Drop empty buckets to keep the collection tidy.
            foreach (var key in _overlays.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList())
                _overlays.Remove(key);

            if (_baseCamera == null) return;

            var stack = _baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            stack.Clear();

            // SortedList iterates in ascending key order → lower CameraLayer values render first (underneath).
            foreach (var list in _overlays.Values)
                foreach (var cam in list)
                    stack.Add(cam);
        }

#if UNITY_EDITOR
        public void DebugLogStack()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[CameraStackManager] Current stack:");
            sb.AppendLine($"  [Base] {(_baseCamera != null ? _baseCamera.name : "<none>")}");
            foreach (var kvp in _overlays)
            {
                string layerName = System.Enum.IsDefined(typeof(CameraLayer), kvp.Key)
                    ? ((CameraLayer)kvp.Key).ToString()
                    : kvp.Key.ToString();
                foreach (var cam in kvp.Value)
                    sb.AppendLine($"  [{kvp.Key}:{layerName}] {(cam != null ? cam.name : "<destroyed>")}");
            }
            Debug.Log(sb.ToString());
        }
#endif
    }
}
