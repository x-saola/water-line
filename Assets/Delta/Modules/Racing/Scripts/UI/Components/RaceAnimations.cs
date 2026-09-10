using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Delta.Modules.Racing
{
    /// <summary>
    /// Helper class for race-related animations using DOTween
    /// </summary>
    public class RaceAnimations : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _progressBarAnimationDuration = 0.3f;
        [SerializeField] private float _celebrationScale = 1.2f;
        [SerializeField] private float _celebrationDuration = 0.5f;

        /// <summary>
        /// Animate progress bar fill
        /// </summary>
        public void AnimateProgressBar(Slider slider, int newValue, System.Action onComplete = null)
        {
            slider.DOValue(newValue, _progressBarAnimationDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Play celebration animation on a transform
        /// </summary>
        public void PlayCelebrationAnimation(Transform target, System.Action onComplete = null)
        {
            Sequence seq = DOTween.Sequence();
            
            seq.Append(target.DOScale(_celebrationScale, _celebrationDuration * 0.5f).SetEase(Ease.OutBack))
               .Append(target.DOScale(1f, _celebrationDuration * 0.5f).SetEase(Ease.InBack))
               .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Pulse animation for attention-grabbing
        /// </summary>
        public void PlayPulseAnimation(Transform target, int loops = 3)
        {
            target.DOScale(1.1f, 0.3f)
                .SetEase(Ease.InOutSine)
                .SetLoops(loops * 2, LoopType.Yoyo);
        }

        /// <summary>
        /// Shake animation for defeat or error
        /// </summary>
        public void PlayShakeAnimation(Transform target, float strength = 10f, float duration = 0.5f)
        {
            target.DOShakePosition(duration, strength, 10, 90, false, true);
        }

        /// <summary>
        /// Flash color animation
        /// </summary>
        public void PlayFlashAnimation(Graphic graphic, Color flashColor, float duration = 0.2f)
        {
            Color originalColor = graphic.color;
            Sequence seq = DOTween.Sequence();
            
            seq.Append(graphic.DOColor(flashColor, duration * 0.5f))
               .Append(graphic.DOColor(originalColor, duration * 0.5f));
        }

        /// <summary>
        /// Fade in animation
        /// </summary>
        public void FadeIn(CanvasGroup canvasGroup, float duration = 0.3f, System.Action onComplete = null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Fade out animation
        /// </summary>
        public void FadeOut(CanvasGroup canvasGroup, float duration = 0.3f, System.Action onComplete = null)
        {
            canvasGroup.DOFade(0f, duration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Number count-up animation
        /// </summary>
        public void AnimateNumberCountUp(TMPro.TextMeshProUGUI text, int from, int to, float duration = 1f)
        {
            DOVirtual.Int(from, to, duration, (value) =>
            {
                text.text = value.ToString();
            }).SetEase(Ease.OutQuad);
        }

        private void OnDestroy()
        {
            // Kill all tweens on this object
            DOTween.Kill(transform);
        }
    }
}

