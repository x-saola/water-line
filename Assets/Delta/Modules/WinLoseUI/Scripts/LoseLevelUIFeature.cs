using Delta.Common;
using Delta.Common.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules
{
    public class LoseLevelUIFeature : BounceAppearUI
    {
        [Header("References")]
        [SerializeField] Image _overlay;
        [SerializeField] TextMeshProUGUI _loseCauseText;
        [SerializeField] TextMeshProUGUI _loseCauseTextOutline;
        [SerializeField] TextMeshProUGUI _loseCauseTextShadowOutline;
        [SerializeField] Button _homeButton;
        [SerializeField] Button _retryButton;
        [SerializeField] DeltaButton _reviveButton;
        [SerializeField] RectTransform _boardZone;

        [Header("Next Feature")]
        [SerializeField] GameObject _nextFeatureContainer;
        [SerializeField] Image _nextFeatureIcon;
        [SerializeField] TextMeshProUGUI _nextFeaturePercentageText;
        [SerializeField] Slider _nextFeatureProgressBar;

        public RectTransform BoardZone => _boardZone;
        public event System.Action OnRetry;
        public event System.Action OnRevive;

        protected override void Awake()
        {
            base.Awake();
            _overlay.gameObject.SetActive(false);
            _parentContent.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            _retryButton.onClick.AddListener(OnClickRetryButtonHandler);
            // _reviveButton.onClick.AddListener(OnClickReviveButtonHandler);
        }

        void OnDisable()
        {
            _retryButton.onClick.RemoveListener(OnClickRetryButtonHandler);
            // _reviveButton.onClick.RemoveListener(OnClickReviveButtonHandler);
        }

        public override void PlayAppearAnimation()
        {
            _overlay.gameObject.SetActive(true);
            _overlay.color = new Color(0, 0, 0, 0);
            _overlay.DOFade(253 / 255f, 0.33f);

            _parentContent.gameObject.SetActive(true);
            base.PlayAppearAnimation();
        }
        
        public void SetLoseCause(string cause)
        {
            _loseCauseText.text = cause;
            _loseCauseTextOutline.text = cause;
            _loseCauseTextShadowOutline.text = cause;
        }

        public void ShowReviveOption(bool show)
        {
            _reviveButton.gameObject.SetActive(show);

            if (show)
            {
                _retryButton.transform.localScale = Vector3.one * 0.7f;
                _boardZone.offsetMin = new Vector2(_boardZone.offsetMin.x, 630);
            }
            else
            {
                _retryButton.transform.localScale = Vector3.one;
                _boardZone.offsetMin = new Vector2(_boardZone.offsetMin.x, 450);
            }
        }

        public void SetExtraTimeText(int seconds)
        {
            _reviveButton.SetText($"+{seconds}s");
        }

        private void OnClickRetryButtonHandler()
        {
            OnRetry?.Invoke();
        }

        private void OnClickReviveButtonHandler()
        {
            OnRevive?.Invoke();
        }

        public void HideNextFeature()
        {
            _nextFeatureContainer.SetActive(false);
        }

        public void ShowNextFeature(FeatureData featureData, float progressTowardNextFeature)
        {
            if (featureData == null)
            {
                _nextFeatureContainer.SetActive(false);
                return;
            }

            _nextFeatureContainer.SetActive(true);
            _nextFeatureIcon.sprite = featureData.sprite;
            _nextFeatureIcon.color = new Color(0, 0, 0, 200/255f);
            _nextFeaturePercentageText.text = $"{Mathf.RoundToInt(progressTowardNextFeature * 100)}%";
            _nextFeatureProgressBar.value = progressTowardNextFeature;
        }
    }
}
