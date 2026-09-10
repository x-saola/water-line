using System;
using System.Threading.Tasks;
using Delta.Common.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules
{
    public class LoadingUI : UIController
    {
        public static LoadingUI Instance { get; private set; }

        [SerializeField] private RectTransform _logoRectTf;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _progressFillRectTf;
        [SerializeField] private float _minDisplayDuration = 2f;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private float _dotAnimationInterval = 0.4f;
        [SerializeField] private float _screenFadingDuration = 0.5f;
        [SerializeField] private float _logoScaleBounceDuration = 1f;
        
        [Header("First Phase")]
        [SerializeField] private float _firstPhaseDuration = 1.2f;
        [SerializeField] private float _firstPhaseProgressPercent = 0.2f;
        [SerializeField] private float _firstPhaseDelay = 0.2f;
        [SerializeField] private float _afterFirstPhaseDelay = 0.2f;

        [Header("No Internet popup")]
        [SerializeField] private GameObject _noInternetPopup;
        [SerializeField] private Image _noInternetOverlay;
        [SerializeField] private RectTransform _noInternetContainer;
        [SerializeField] private Button _noInternetRetryButton;
        [SerializeField] private float _noInternetOverlayFadeDuration = 0.3f;
        [SerializeField] private float _noInternetContainerBounceDuration = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _noInternetPopupProgressThreshold = 0.6f;

        private Tween _progressTween;
        private Tween _textTween;
        private Tween _noInternetOverlayTween;
        private Tween _noInternetContainerTween;
        private float _noInternetOverlayAlpha;
        private float _progressWidth;
        private Action _pendingNoInternetRetry;
        private TaskCompletionSource<bool> _minDisplayTcs;

        protected override void OnUIStart()
        {
            Instance = this;
            _noInternetOverlayAlpha = _noInternetOverlay.color.a;
        }

        protected override void OnUIRemoved()
        {
            _progressTween?.Kill();
            _textTween?.Kill();
            _noInternetOverlayTween?.Kill();
            _noInternetContainerTween?.Kill();
            _noInternetRetryButton.onClick.RemoveAllListeners();
            _pendingNoInternetRetry = null;
            if (Instance == this)
                Instance = null;
        }

        public void ShowNoInternetPopup(Action onRetry)
        {
            _noInternetOverlayTween?.Kill();
            _noInternetContainerTween?.Kill();
            _noInternetRetryButton.onClick.RemoveAllListeners();
            _noInternetRetryButton.onClick.AddListener(() => onRetry?.Invoke());

            _noInternetPopup.SetActive(true);

            var overlayColor = _noInternetOverlay.color;
            _noInternetOverlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
            _noInternetOverlayTween = _noInternetOverlay.DOFade(_noInternetOverlayAlpha, _noInternetOverlayFadeDuration).SetEase(Ease.OutQuad);

            _noInternetContainer.gameObject.SetActive(true);
            _noInternetContainer.localScale = Vector3.zero;
            _noInternetContainerTween = _noInternetContainer.DOScale(1f, _noInternetContainerBounceDuration).SetEase(Ease.OutBack);
        }

        public void HideNoInternetPopup()
        {
            _noInternetOverlayTween?.Kill();
            _noInternetContainerTween?.Kill();
            _noInternetRetryButton.onClick.RemoveAllListeners();
            _pendingNoInternetRetry = null;

            _noInternetOverlayTween = _noInternetOverlay.DOFade(0f, _noInternetOverlayFadeDuration).SetEase(Ease.InQuad)
                .OnComplete(() => _noInternetPopup.SetActive(false));
        }

        // Defers the popup until the loading bar visually catches up to it, instead of
        // flashing it the instant the connectivity check fails.
        public void ScheduleNoInternetPopup(Action onRetry)
        {
            _pendingNoInternetRetry = onRetry;
            TryShowScheduledNoInternetPopup();
        }

        private void TryShowScheduledNoInternetPopup()
        {
            if (_pendingNoInternetRetry == null || _progressWidth <= 0f)
                return;

            var progressPercent = (_progressWidth + _progressFillRectTf.sizeDelta.x) / _progressWidth;
            if (progressPercent < _noInternetPopupProgressThreshold)
                return;

            var onRetry = _pendingNoInternetRetry;
            _pendingNoInternetRetry = null;

            // Freeze the bar right where it is instead of letting it finish filling behind the popup.
            _progressTween?.Kill();

            ShowNoInternetPopup(onRetry);
        }

        // Continues the fill from wherever it got frozen once connectivity is confirmed again.
        public void ResumeProgressFill()
        {
            if (_progressTween != null && _progressTween.IsActive())
                return;

            var remainingDuration = Mathf.Max(0f, _minDisplayDuration - _firstPhaseDuration);
            _progressTween = DOTween.To(() => _progressFillRectTf.sizeDelta.x, SetProgressFill, 0, remainingDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => _minDisplayTcs.TrySetResult(true));
        }

        private void SetProgressFill(float sizeDeltaX)
        {
            _progressFillRectTf.sizeDelta = new Vector2(sizeDeltaX, 0);
            TryShowScheduledNoInternetPopup();
        }

        public async Task PlayAppearAnimation()
        {
            _minDisplayTcs = new TaskCompletionSource<bool>();
            
            _logoRectTf.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
            
            // Screen Fade
            var tcs = new TaskCompletionSource<bool>();
            _canvasGroup.DOFade(1f, _screenFadingDuration).SetEase(Ease.OutQuad).OnComplete(() => tcs.SetResult(true));
            await tcs.Task;
            
            // Logo Bounce
            _logoRectTf.DOScale(1f, _logoScaleBounceDuration).SetEase(Ease.OutBack);
            
            // Text Animator
            var loadingLabel = _loadingText.text.TrimEnd('.');
            var dotCount = 0;
            _textTween = DOTween.Sequence()
                .AppendInterval(_dotAnimationInterval)
                .AppendCallback(() =>
                {
                    dotCount = (dotCount + 1) % 4;
                    _loadingText.text = loadingLabel + new string('.', dotCount);
                })
                .SetLoops(-1);
            
            // Progress bar
            var progressBar = (RectTransform)_progressFillRectTf.parent;
            _progressWidth = progressBar.sizeDelta.x;
            _progressFillRectTf.sizeDelta = new Vector2(-_progressWidth, 0);

            // First phase: quick initial jump so the bar doesn't look stalled at 0%
            var firstPhaseTargetX = -_progressWidth * (1f - _firstPhaseProgressPercent);
            _progressTween = DOTween.To(() => _progressFillRectTf.sizeDelta.x,
                    SetProgressFill, firstPhaseTargetX,
                    _firstPhaseDuration)
                .SetDelay(_firstPhaseDelay)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Second phase: crawl the rest of the way, keeping the total fill time at _minDisplayDuration
                    var remainingDuration = Mathf.Max(0f, _minDisplayDuration - _firstPhaseDuration);
                    _progressTween = DOTween.To(() => _progressFillRectTf.sizeDelta.x,
                            SetProgressFill, 0,
                            remainingDuration)
                        .SetDelay(_afterFirstPhaseDelay)
                        .SetEase(Ease.Linear)
                        .OnComplete(() => _minDisplayTcs.SetResult(true));
                });
            
            await _minDisplayTcs.Task;
        }

        // Callers await this before hiding so the progress bar always finishes its fill,
        // even when the real boot/scene-load work underneath completes sooner.
        public Task WaitForMinimumDisplay() => _minDisplayTcs?.Task ?? Task.CompletedTask;

        public Task PlayDisappearAnimation()
        {
            var tcs = new TaskCompletionSource<bool>();
            _canvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                tcs.SetResult(true);
            });
            return tcs.Task;
        }
    }
}
