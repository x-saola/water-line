using UnityEngine;
using UnityEngine.UI;

namespace Delta.Common.UI
{
    [ExecuteInEditMode]
    public class CanvasScalerLandscape : MonoBehaviour
    {
        public Vector2 DesignRatio;

#if UNITY_EDITOR
        public bool useEditorSafeArea;
        public Vector2 editorSafeAreaMin = Vector2.zero;
        public Vector2 editorSafeAreaMax = Vector2.one;
#endif
        private CanvasScaler _scaler;

#if UNITY_EDITOR
        private int _width;
        private int _height;
        private bool _useEditorSafeArea;
#endif

        void UpdateRatio()
        {
            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
            }

            float ratio = (float)Screen.width / Screen.height;
            float designRatio = DesignRatio.x / DesignRatio.y;
            _scaler.matchWidthOrHeight = ratio > designRatio ? 1 : 0;

#if UNITY_IOS
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            var canvas = GetComponent<Canvas>();
            anchorMin.x /= canvas.pixelRect.width;
            anchorMin.y /= canvas.pixelRect.height;
            anchorMax.x /= canvas.pixelRect.width;
            anchorMax.y /= canvas.pixelRect.height;

#if UNITY_EDITOR
            if (useEditorSafeArea)
            {
                anchorMin = editorSafeAreaMin;
                anchorMax = editorSafeAreaMax;
            }
#endif
#else
            var anchorMin = Vector2.zero;
            var anchorMax = Vector2.one;
#endif
            for (int i = 0; i < transform.childCount; i++)
            {
                var rect = transform.GetChild(i) as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = anchorMin;
                    rect.anchorMax = anchorMax;
                }
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            Update();
#else
            UpdateRatio();
#endif
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_width != Screen.width || _height != Screen.height || useEditorSafeArea != _useEditorSafeArea)
            {
                _width = Screen.width;
                _height = Screen.height;
                _useEditorSafeArea = useEditorSafeArea;

                UpdateRatio();
            }
        }
#endif
    }
}
