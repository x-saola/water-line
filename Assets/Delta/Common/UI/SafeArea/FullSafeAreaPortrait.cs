using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Delta.Common.UI;

[ExecuteInEditMode]
public class FullSafeAreaPortrait : MonoBehaviour
{
    public enum SafeMode
    {
        SafePortrait,
        SafeTop,
        SafeBottom,
        Ignore
    }

    public Vector2 DesignRatio;
    public bool useEditorSafeArea;
    public SafeMode Mode;

    public Rect SafeArea
    {
        get
        {
            switch (Mode)
            {
                case SafeMode.SafeTop:
#if UNITY_EDITOR
                    if (useEditorSafeArea)
                        return new Rect(0, 0, 1125, 2202 + 34 * 3);
                    return new Rect(0, 0, Screen.safeArea.width, Screen.safeArea.height + Screen.safeArea.y);
#else
                    return new Rect(0, 0, Screen.safeArea.width, Screen.safeArea.height + Screen.safeArea.y);
#endif
                case SafeMode.SafePortrait:
                default:
#if UNITY_EDITOR
                    if (useEditorSafeArea)
                        return new Rect(0, 34 * 3, 1125, 2202);
                    return Screen.safeArea;
#else
                    return Screen.safeArea;
#endif
            }
        }
    }

    public RectTransform SafeAreaRect { get { return transform as RectTransform; } }

    private Rect _lastSafeArea = Rect.zero;

    private void Awake()
    {
        Update();
    }

    private void Update()
    {
        if (UIManager.Instance == null)
            return;

        if (_lastSafeArea != SafeArea)
        {
            _lastSafeArea = SafeArea;
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        if (SafeAreaRect == null)
        {
            return;
        }

        Rect safeArea = SafeArea;
        var pixelRect = UIManager.Instance.MainCanvas.pixelRect;
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
    }
}
