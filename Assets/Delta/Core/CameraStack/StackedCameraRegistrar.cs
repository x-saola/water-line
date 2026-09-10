using System.Collections;
using UnityEngine;

namespace Delta.Core
{
    // Only one camera project-wide must have _isBaseCamera = true active at any time.
    // URP will produce render artifacts or log errors if two Base cameras are active simultaneously.
    public class StackedCameraRegistrar : MonoBehaviour
    {
        [SerializeField] CameraLayer _layer;
        [SerializeField] bool _isBaseCamera;

        Camera _cam;

        void Awake() => _cam = GetComponent<Camera>();

        void OnEnable()
        {
            if (CameraStackManager.Instance != null)
                Register();
            else
                StartCoroutine(RetryRegister());
        }

        void OnDisable()
        {
            if (CameraStackManager.Instance == null)
            {
                Debug.LogWarning($"[StackedCameraRegistrar] CameraStackManager is gone when '{name}' disabled — stack may be stale.", this);
                return;
            }

            if (_isBaseCamera)
                CameraStackManager.Instance.UnregisterBase();
            else
                CameraStackManager.Instance.UnregisterOverlay(_cam);
        }

        void Register()
        {
            if (_isBaseCamera)
                CameraStackManager.Instance.RegisterBase(_cam);
            else
                CameraStackManager.Instance.RegisterOverlay(_cam, _layer);
        }

        IEnumerator RetryRegister()
        {
            for (int i = 0; i < 10; i++)
            {
                Debug.LogWarning($"[StackedCameraRegistrar] CameraStackManager not ready, retrying (attempt {i + 1}/10)...", this);
                yield return null;
                if (CameraStackManager.Instance != null)
                {
                    Register();
                    yield break;
                }
            }
            Debug.LogError($"[StackedCameraRegistrar] CameraStackManager never found after 10 frames. '{name}' will not be registered in the camera stack.", this);
        }
    }
}
