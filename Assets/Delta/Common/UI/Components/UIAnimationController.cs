using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace KenToolbox.UI
{
    /// <summary>
    /// UIAnimationController provides various UI animation options using DOTween.
    /// It supports movement, fade, shake and other effects for open and close animations.
    /// </summary>
    public class UIAnimationController : MonoBehaviour
    {
        /// <summary>
        /// Enum for selecting the type of UI animation (movement direction).
        /// </summary>
        public enum UIAnimationType
        {
            None,
            SlideInFromRight,
            SlideInFromLeft,
            SlideInFromTop,
            SlideInFromBottom,
            SlideOutToRight,
            SlideOutToLeft,
            SlideOutToTop,
            SlideOutToBottom
        }

        /// <summary>
        /// Enum for selecting the distance option for UI animation.
        /// </summary>
        public enum UIDistanceOption
        {
            Full,
            Half,
            Quarter,
            Custom
        }

        #region General Settings

        /// <summary>
        /// RectTransform of the UI element to animate.
        /// </summary>
        [Tooltip("RectTransform of the UI element to animate.")]
        public RectTransform targetRect;

        /// <summary>
        /// Play animation each time the GameObject is enabled.
        /// </summary>
        [Tooltip("Play animation each time the GameObject is enabled.")]
        public bool animateOnEnable = true;

        /// <summary>
        /// Stores the original anchored position of the UI element.
        /// </summary>
        private Vector2 originalPosition;
        private Vector3 originalScale;
        private Tween pulseTween;
        #endregion

        #region Open Animation Settings

        [Header("Open Animation Settings")]

        /// <summary>
        /// Select the open animation type (movement direction).
        /// </summary>
        [Tooltip("Select the open animation type (movement direction).")]
        public UIAnimationType openAnimationType = UIAnimationType.SlideInFromRight;

        /// <summary>
        /// Duration of the open animation in seconds.
        /// </summary>
        [Tooltip("Duration of the open animation in seconds.")]
        public float openAnimationDuration = 1f;

        /// <summary>
        /// Delay before starting the open animation.
        /// </summary>
        [Tooltip("Delay before starting the open animation.")]
        public float openAnimationDelay = 0f;

        /// <summary>
        /// Select the distance option for the open animation.
        /// </summary>
        [Tooltip("Select the distance option for the open animation.")]
        public UIDistanceOption openDistanceOption = UIDistanceOption.Full;

        /// <summary>
        /// Custom distance in pixels for the open animation (only if Custom is selected).
        /// </summary>
        [Tooltip("Custom distance in pixels for the open animation (only if Custom is selected).")]
        public float openCustomDistance = 100f;

        /// <summary>
        /// If true, the UI element will move during the open animation.
        /// </summary>
        [Tooltip("If true, the UI element will move during the open animation.")]
        public bool openApplyMovement = true;

        /// <summary>
        /// Easing type for the open movement tween.
        /// </summary>
        [Tooltip("Easing type for the open movement tween.")]
        public Ease openMovementEase = Ease.OutBack;

        /// <summary>
        /// If true, a fade effect will be applied during the open animation.
        /// </summary>
        [Tooltip("If true, a fade effect will be applied during the open animation.")]
        public bool openApplyFade = false;

        /// <summary>
        /// Starting alpha for the open fade effect.
        /// </summary>
        [Tooltip("Starting alpha for the open fade effect.")]
        public float openFadeFromAlpha = 0f;

        /// <summary>
        /// Target alpha for the open fade effect.
        /// </summary>
        [Tooltip("Target alpha for the open fade effect.")]
        public float openFadeToAlpha = 1f;

        /// <summary>
        /// If true, a shake effect will be applied after the open movement tween.
        /// </summary>
        [Tooltip("If true, a shake effect will be applied after the open movement tween.")]
        public bool openApplyShake = false;

        /// <summary>
        /// Duration of the open shake effect.
        /// </summary>
        [Tooltip("Duration of the open shake effect.")]
        public float openShakeDuration = 0.5f;

        /// <summary>
        /// Strength of the open shake effect in pixels.
        /// </summary>
        [Tooltip("Strength of the open shake effect in pixels.")]
        public float openShakeStrength = 20f;

        #endregion

        #region Close Animation Settings

        [Header("Close Animation Settings")]

        /// <summary>
        /// Select the close animation type (movement direction).
        /// </summary>
        [Tooltip("Select the close animation type (movement direction).")]
        public UIAnimationType closeAnimationType = UIAnimationType.SlideOutToRight;

        /// <summary>
        /// Duration of the close animation in seconds.
        /// </summary>
        [Tooltip("Duration of the close animation in seconds.")]
        public float closeAnimationDuration = 1f;

        /// <summary>
        /// Delay before starting the close animation.
        /// </summary>
        [Tooltip("Delay before starting the close animation.")]
        public float closeAnimationDelay = 0f;

        /// <summary>
        /// Select the distance option for the close animation.
        /// </summary>
        [Tooltip("Select the distance option for the close animation.")]
        public UIDistanceOption closeDistanceOption = UIDistanceOption.Full;

        /// <summary>
        /// Custom distance in pixels for the close animation (only if Custom is selected).
        /// </summary>
        [Tooltip("Custom distance in pixels for the close animation (only if Custom is selected).")]
        public float closeCustomDistance = 100f;

        /// <summary>
        /// If true, the UI element will move during the close animation.
        /// </summary>
        [Tooltip("If true, the UI element will move during the close animation.")]
        public bool closeApplyMovement = true;

        /// <summary>
        /// Easing type for the close movement tween.
        /// </summary>
        [Tooltip("Easing type for the close movement tween.")]
        public Ease closeMovementEase = Ease.InBack;

        /// <summary>
        /// If true, a fade effect will be applied during the close animation.
        /// </summary>
        [Tooltip("If true, a fade effect will be applied during the close animation.")]
        public bool closeApplyFade = false;

        /// <summary>
        /// Starting alpha for the close fade effect.
        /// </summary>
        [Tooltip("Starting alpha for the close fade effect.")]
        public float closeFadeFromAlpha = 1f;

        /// <summary>
        /// Target alpha for the close fade effect.
        /// </summary>
        [Tooltip("Target alpha for the close fade effect.")]
        public float closeFadeToAlpha = 0f;

        /// <summary>
        /// If true, a shake effect will be applied after the close movement tween.
        /// </summary>
        [Tooltip("If true, a shake effect will be applied after the close movement tween.")]
        public bool closeApplyShake = false;

        /// <summary>
        /// Duration of the close shake effect.
        /// </summary>
        [Tooltip("Duration of the close shake effect.")]
        public float closeShakeDuration = 0.5f;

        /// <summary>
        /// Strength of the close shake effect in pixels.
        /// </summary>
        [Tooltip("Strength of the close shake effect in pixels.")]
        public float closeShakeStrength = 20f;

        #endregion

        #region Pulse Loop Settings
        [Header("Pulse Loop Settings")]
        [Tooltip("If true, the element will continuously pulse while enabled.")]
        public bool pulseLoopEnabled = false;

        [Tooltip("Starting scale multiplier for the pulse loop.")]
        public float pulseScaleFrom = 1f;

        [Tooltip("Target scale multiplier for the pulse loop.")]
        public float pulseScaleTo = 1.1f;

        [Tooltip("Duration of a single pulse (out or in) in seconds.")]
        public float pulseDuration = 0.4f;

        [Tooltip("Delay before the pulse loop starts.")]
        public float pulseDelay = 0f;

        [Tooltip("Easing type for the pulse tween.")]
        public Ease pulseEase = Ease.InOutSine;
        #endregion

        /// <summary>
        /// Initializes the UI element and saves its original position.
        /// </summary>
        private void Awake()
        {
            if (targetRect == null)
                targetRect = GetComponent<RectTransform>();

            originalPosition = targetRect.anchoredPosition;
            originalScale = targetRect.localScale; 
        }

        /// <summary>
        /// Plays the open animation when the GameObject is enabled, if animateOnEnable is true.
        /// </summary>
        private void OnEnable()
        {
            if (animateOnEnable)
                PlayAnimation();

            if (pulseLoopEnabled)
            {
                float totalDelay = openAnimationDelay + openAnimationDuration;
                DOVirtual.DelayedCall(totalDelay, StartPulseLoop).SetUpdate(true);
            }
        }

        /// <summary>
        /// Cancels any active DOTween animations on the targetRect when the GameObject is disabled.
        /// </summary>
        private void OnDisable()
        {
            targetRect.DOKill();
            StopPulseLoop();    
        }

        /// <summary>
        /// Plays the open animation using the configured open animation settings.
        /// </summary>
        public void PlayAnimation()
        {
            Animate(openAnimationType, openAnimationDuration, openAnimationDelay, openDistanceOption, openCustomDistance,
                openApplyMovement, openMovementEase, openApplyFade, openFadeFromAlpha, openFadeToAlpha,
                openApplyShake, openShakeDuration, openShakeStrength,
                null);
        }

        /// <summary>
        /// Plays the close animation using the configured close animation settings,
        /// and deactivates the GameObject upon animation completion.
        /// </summary>
        public void PlayAnimationAndClose()
        {
            Animate(closeAnimationType, closeAnimationDuration, closeAnimationDelay, closeDistanceOption, closeCustomDistance,
                closeApplyMovement, closeMovementEase, closeApplyFade, closeFadeFromAlpha, closeFadeToAlpha,
                closeApplyShake, closeShakeDuration, closeShakeStrength,
                () => { gameObject.SetActive(false); });
        }

        public void StartPulseLoop()
        {
            if (pulseTween != null && pulseTween.IsActive())
                pulseTween.Kill();

            targetRect.localScale = originalScale * pulseScaleFrom;
            pulseTween = targetRect.DOScale(originalScale * pulseScaleTo, pulseDuration)
                                   .SetEase(pulseEase)
                                   .SetLoops(-1, LoopType.Yoyo)
                                   .SetUpdate(true);
        }

        public void StopPulseLoop()
        {
            if (pulseTween != null && pulseTween.IsActive())
            {
                pulseTween.Kill();
                pulseTween = null;
            }
            targetRect.localScale = originalScale;
        }

        /// <summary>
        /// Animates the UI element based on the specified parameters.
        /// </summary>
        /// <param name="animType">Type of UI animation to perform.</param>
        /// <param name="duration">Duration of the animation in seconds.</param>
        /// <param name="delay">Delay before starting the animation.</param>
        /// <param name="distanceOption">Distance option to calculate the movement offset.</param>
        /// <param name="customDistance">Custom distance in pixels (used if distanceOption is Custom).</param>
        /// <param name="applyMovement">If true, the UI element will move.</param>
        /// <param name="movementEase">Easing type for the movement tween.</param>
        /// <param name="applyFade">If true, a fade effect will be applied.</param>
        /// <param name="fadeFromAlpha">Starting alpha value for the fade effect.</param>
        /// <param name="fadeToAlpha">Target alpha value for the fade effect.</param>
        /// <param name="applyShake">If true, a shake effect will be applied after movement.</param>
        /// <param name="shakeDuration">Duration of the shake effect.</param>
        /// <param name="shakeStrength">Strength of the shake effect in pixels.</param>
        /// <param name="onComplete">Callback to invoke when the animation completes.</param>
        private void Animate(
            UIAnimationType animType,
            float duration,
            float delay,
            UIDistanceOption distanceOption,
            float customDistance,
            bool applyMovement,
            Ease movementEase,
            bool applyFade,
            float fadeFromAlpha,
            float fadeToAlpha,
            bool applyShake,
            float shakeDuration,
            float shakeStrength,
            System.Action onComplete)
        {
            // Calculate movement offset based on distance option
            float offsetHorizontal = GetOffset(distanceOption, Screen.width, customDistance);
            float offsetVertical = GetOffset(distanceOption, Screen.height, customDistance);

            Vector2 startPos = originalPosition;
            Vector2 endPos = originalPosition;

            switch (animType)
            {
                case UIAnimationType.SlideInFromRight:
                    startPos = new Vector2(offsetHorizontal, originalPosition.y);
                    break;
                case UIAnimationType.SlideInFromLeft:
                    startPos = new Vector2(-offsetHorizontal, originalPosition.y);
                    break;
                case UIAnimationType.SlideInFromTop:
                    startPos = new Vector2(originalPosition.x, offsetVertical);
                    break;
                case UIAnimationType.SlideInFromBottom:
                    startPos = new Vector2(originalPosition.x, -offsetVertical);
                    break;
                case UIAnimationType.SlideOutToRight:
                    endPos = new Vector2(offsetHorizontal, originalPosition.y);
                    break;
                case UIAnimationType.SlideOutToLeft:
                    endPos = new Vector2(-offsetHorizontal, originalPosition.y);
                    break;
                case UIAnimationType.SlideOutToTop:
                    endPos = new Vector2(originalPosition.x, offsetVertical);
                    break;
                case UIAnimationType.SlideOutToBottom:
                    endPos = new Vector2(originalPosition.x, -offsetVertical);
                    break;
                default:
                    break;
            }

            // Apply fade effect concurrently if enabled
            if (applyFade)
            {
                CanvasGroup cg = targetRect.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = targetRect.gameObject.AddComponent<CanvasGroup>();
                }
                cg.alpha = fadeFromAlpha;
                cg.DOFade(fadeToAlpha, duration).SetDelay(delay).SetUpdate(true);
            }

            // Apply movement (and shake if enabled)
            if (applyMovement)
            {
                targetRect.anchoredPosition = startPos;
                targetRect.DOAnchorPos(endPos, duration)
                    .SetDelay(delay)
                    .SetEase(movementEase)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (applyShake)
                        {
                            // Use DOTween's shake with custom implementation
                            Sequence shakeSeq = DOTween.Sequence();
                            shakeSeq.Append(DOVirtual.Float(0f, 1f, shakeDuration, (val) =>
                            {
                                targetRect.anchoredPosition = endPos + Random.insideUnitCircle * shakeStrength;
                            }))
                            .OnComplete(() =>
                            {
                                targetRect.anchoredPosition = endPos;
                                onComplete?.Invoke();
                            });
                        }
                        else
                        {
                            onComplete?.Invoke();
                        }
                    });
            }
            else
            {
                // No movement tween; apply shake if enabled, or simply wait
                if (applyShake)
                {
                    Sequence shakeSeq = DOTween.Sequence();
                    shakeSeq.AppendInterval(delay)
                        .Append(DOVirtual.Float(0f, 1f, shakeDuration, (val) =>
                        {
                            targetRect.anchoredPosition = originalPosition + Random.insideUnitCircle * shakeStrength;
                        }))
                        .SetUpdate(true)
                        .OnComplete(() =>
                        {
                            targetRect.anchoredPosition = originalPosition;
                            onComplete?.Invoke();
                        });
                }
                else
                {
                    DOVirtual.DelayedCall(delay + duration, () =>
                    {
                        onComplete?.Invoke();
                    }).SetUpdate(true);
                }
            }
        }

        /// <summary>
        /// Calculates the offset value based on the selected distance option.
        /// </summary>
        /// <param name="option">The selected distance option.</param>
        /// <param name="screenDimension">The screen width or height.</param>
        /// <param name="customDistance">Custom distance in pixels (if applicable).</param>
        /// <returns>The computed offset value.</returns>
        private float GetOffset(UIDistanceOption option, float screenDimension, float customDistance)
        {
            switch (option)
            {
                case UIDistanceOption.Full:
                    return screenDimension;
                case UIDistanceOption.Half:
                    return screenDimension / 2f;
                case UIDistanceOption.Quarter:
                    return screenDimension / 4f;
                case UIDistanceOption.Custom:
                    return customDistance;
                default:
                    return screenDimension;
            }
        }
    }
}

#if UNITY_EDITOR

namespace KenToolbox.UI
{
    /// <summary>
    /// Custom editor for UIAnimationController.
    /// Provides organized box areas for open and close animation settings,
    /// displaying custom distance and effect fields conditionally.
    /// </summary>
    [CustomEditor(typeof(UIAnimationController))]
    public class UIAnimationControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // General Settings Box
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetRect"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animateOnEnable"));
            EditorGUILayout.EndVertical();

            // Open Animation Settings Box
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Open Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openAnimationType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openDistanceOption"));
            SerializedProperty openDistProp = serializedObject.FindProperty("openDistanceOption");
            if ((UIAnimationController.UIDistanceOption)openDistProp.enumValueIndex == UIAnimationController.UIDistanceOption.Custom)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("openCustomDistance"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openAnimationDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openAnimationDelay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openApplyMovement"));
            if (serializedObject.FindProperty("openApplyMovement").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("openMovementEase"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openApplyFade"));
            if (serializedObject.FindProperty("openApplyFade").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("openFadeFromAlpha"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("openFadeToAlpha"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("openApplyShake"));
            if (serializedObject.FindProperty("openApplyShake").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("openShakeDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("openShakeStrength"));
            }
            EditorGUILayout.EndVertical();

            // Pulse Loop Settings Box
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Pulse Loop Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseLoopEnabled"));
            if (serializedObject.FindProperty("pulseLoopEnabled").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseScaleFrom"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseScaleTo"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pulseEase"));
            }
            EditorGUILayout.EndVertical();

            // Close Animation Settings Box
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Close Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeAnimationType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeDistanceOption"));
            SerializedProperty closeDistProp = serializedObject.FindProperty("closeDistanceOption");
            if ((UIAnimationController.UIDistanceOption)closeDistProp.enumValueIndex == UIAnimationController.UIDistanceOption.Custom)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeCustomDistance"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeAnimationDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeAnimationDelay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeApplyMovement"));
            if (serializedObject.FindProperty("closeApplyMovement").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeMovementEase"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeApplyFade"));
            if (serializedObject.FindProperty("closeApplyFade").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeFadeFromAlpha"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeFadeToAlpha"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closeApplyShake"));
            if (serializedObject.FindProperty("closeApplyShake").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeShakeDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("closeShakeStrength"));
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif