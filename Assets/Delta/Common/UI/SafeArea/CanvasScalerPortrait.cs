using UnityEngine;
using UnityEngine.UI;

namespace Delta.Common.UI
{
    [ExecuteInEditMode]
    public class CanvasScalerPortrait : MonoBehaviour
    {
        public bool NoSafeArea;
        public Vector2 DesignRatio;

        public bool useEditorSafeArea;
        public RectTransform TopArea { get { return UIManager.Instance.TopSafeRect; } }
        public RectTransform BottomArea { get { return UIManager.Instance.BottomSafeRect; } }
        public RectTransform SafeAreaRect { get { return UIManager.Instance.GameRect; } }

        public bool ShouldShowSafeRects { get; private set; }

        public Rect SafeArea
        {
            get
            {
                if (NoSafeArea)
                    return Rect.zero;

#if UNITY_EDITOR
                if (useEditorSafeArea)
                    return new Rect(0, 34 * 3, 1125, 2202);
                return Screen.safeArea;
#else
                return Screen.safeArea;
#endif
            }
        }

        private CanvasScaler _scaler;
        private Rect _lastSafeArea = Rect.zero;
        private Canvas _canvas;

        private int _width;
        private int _height;

        void UpdateRatio()
        {
            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
            }

            float ratio = (float)Screen.width / Screen.height;
            float designRatio = DesignRatio.x / DesignRatio.y;
            _scaler.matchWidthOrHeight = ratio > designRatio ? 1 : 0;
        }

        void ApplySafeArea()
        {
            if (SafeAreaRect == null)
            {
                return;
            }

            Rect safeArea = SafeArea;
            var pixelRect = _canvas.pixelRect;
#if UNITY_EDITOR
            if (useEditorSafeArea)
                pixelRect = new Rect(0, 0, 1125, 2436);
#endif
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= pixelRect.width;
            anchorMin.y /= pixelRect.height;
            anchorMax.x /= pixelRect.width;
            anchorMax.y /= pixelRect.height;

            SafeAreaRect.anchorMin = anchorMin;
            SafeAreaRect.anchorMax = anchorMax;
            SafeAreaRect.offsetMin = SafeAreaRect.offsetMax = Vector2.zero;

            ShouldShowSafeRects = safeArea.position.y > 0;
            TopArea.gameObject.SetActive(ShouldShowSafeRects);
            BottomArea.gameObject.SetActive(ShouldShowSafeRects);
            if (ShouldShowSafeRects)
            {
                BottomArea.anchorMin = Vector2.zero;
                BottomArea.anchorMax = new Vector2(1f, anchorMin.y);
                BottomArea.offsetMin = BottomArea.offsetMax = Vector2.zero;

                TopArea.anchorMin = new Vector2(0f, anchorMax.y);
                TopArea.anchorMax = Vector2.one;
                TopArea.offsetMin = TopArea.offsetMax = Vector2.zero;
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            Update();
        }

        private void Update()
        {
            if (UIManager.Instance == null)
                return;

            if (_width != Screen.width || _height != Screen.height)
            {
                _width = Screen.width;
                _height = Screen.height;

                UpdateRatio();
            }

            if (_lastSafeArea != SafeArea)
            {
                _lastSafeArea = SafeArea;
                ApplySafeArea();
            }
        }
    }
}