using System;
using System.Threading.Tasks;
using Delta.Common.UI;
using Delta.ProjectName;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules
{
    // Waterline's persistent gameplay chrome: settings button + timer readout. Per-pipe move
    // budgets are shown directly on the board by PipeView (the anchor flange digit from the
    // spec), not duplicated here. Refactored from a merge/collection-game HUD this template was
    // cloned from (see docs/plans/core-gameplay-plan.md) - the old pond/merge-streak/category-
    // progress/stack-container fields had no Waterline equivalent and are gone.
    public class GamePlayUI : UIController, IGameplayUIController
    {
        [SerializeField] private Button _settingsBtn;
        [SerializeField] private RectTransform _top;
        [SerializeField] private RectTransform _bottom;
        [SerializeField] private TextMeshProUGUI _timerText;

        public event Action OnSettings;

        protected override void OnUIStart()
        {
            _settingsBtn.onClick.RemoveListener(OnSettingsClicked);
            _settingsBtn.onClick.AddListener(OnSettingsClicked);
        }

        public void UpdateTimerDisplay(string text)
        {
            if (_timerText != null) _timerText.text = text;
        }

        private void OnSettingsClicked() => OnSettings?.Invoke();

        private float GetSafeAreaTop()
        {
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return (Screen.height - Screen.safeArea.yMax) / scale;
        }

        private float GetSafeAreaBottom()
        {
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return Screen.safeArea.yMin / scale;
        }

        public Task PlayAppearAnimation(float duration = 0.4f)
        {
            var topPos = _top.anchoredPosition;
            topPos.y = _top.rect.size.y + GetSafeAreaTop();
            _top.anchoredPosition = topPos;

            var botPos = _bottom.anchoredPosition;
            botPos.y = -(_bottom.rect.size.y + GetSafeAreaBottom());
            _bottom.anchoredPosition = botPos;

            var tcs = new TaskCompletionSource<bool>();
            DOTween.Sequence()
                .Join(_top.DOAnchorPosY(0f, duration).SetEase(Ease.OutCubic))
                .Join(_bottom.DOAnchorPosY(0f, duration).SetEase(Ease.OutCubic))
                .OnComplete(() => tcs.SetResult(true));
            return tcs.Task;
        }

        public Task PlayDisappearAnimation(float duration = 0.4f)
        {
            var tcs = new TaskCompletionSource<bool>();
            DOTween.Sequence()
                .Join(_top.DOAnchorPosY(_top.rect.size.y + GetSafeAreaTop(), duration).SetEase(Ease.InCubic))
                .Join(_bottom.DOAnchorPosY(-(_bottom.rect.size.y + GetSafeAreaBottom() + 200f), duration).SetEase(Ease.InCubic))
                .OnComplete(() => tcs.SetResult(true));
            return tcs.Task;
        }
    }
}
