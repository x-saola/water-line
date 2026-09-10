using Coffee.UIExtensions;
using Delta.Core;
using Delta.Common.UI;
using Delta.Services;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Delta.Modules
{
    public class WinLevelUIFeature : BounceAppearUI
    {
        [Header("References")]
        [SerializeField] Image _overlay;
        [SerializeField] Button _continueButton;
        [SerializeField] Button _homeButton;
        [SerializeField] RectTransform _boardZone;

        [Header("Remove Ads")]
        [SerializeField] GameObject _removeAdsOffer;
        [SerializeField] TextMeshProUGUI _promoText;
        [SerializeField] DeltaIAPButton _buyRemoveAdsButton;
        [SerializeField] Button _continueWithAdsButton;

        [Header("Next Feature")]
        [SerializeField] GameObject _nextFeatureContainer;
        [SerializeField] Image _nextFeatureIcon;
        [SerializeField] TextMeshProUGUI _nextFeaturePercentageText;
        [SerializeField] Slider _nextFeatureProgressBar;
        [SerializeField] UIParticle _nextFeatureCompleteParticle;

        private IAPService m_IAPService;

        public RectTransform BoardZone => _boardZone;
        public System.Action OnContinueButtonClicked;
        public System.Action OnHomeButtonClicked;

        public System.Action OnPurchaseConfirmed;
        public System.Action OnPurchaseFailed;

        protected override void Awake()
        {
            base.Awake();
            m_IAPService = ServiceLocator.Get<IAPService>();
            _homeButton.onClick.AddListener(OnHomeButtonClickedHandler);
            _continueButton.onClick.AddListener(OnContinueButtonClickedHandler);
            // _continueWithAdsButton.onClick.AddListener(OnContinueButtonClickedHandler);

            _overlay.gameObject.SetActive(false);
            _parentContent.gameObject.SetActive(false);
            // ShowRemoveAdsOffer(false);
        }

        protected override void OnEnable()
        {
            m_IAPService.OnPurchaseConfirmed += OnPurchaseConfirmedHandler;
            m_IAPService.OnPurchaseFailed += OnPurchaseFailedHandler;
        }

        protected void OnDisable()
        {
            m_IAPService.OnPurchaseConfirmed -= OnPurchaseConfirmedHandler;
            m_IAPService.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }

        public override void PlayAppearAnimation()
        {
            _overlay.gameObject.SetActive(true);
            _overlay.color = new Color(0, 0, 0, 0);
            _overlay.DOFade(253 / 255f, 0.33f);

            _parentContent.gameObject.SetActive(true);
            base.PlayAppearAnimation();
        }

        private void OnContinueButtonClickedHandler()
        {
            OnContinueButtonClicked?.Invoke();
        }

        private void OnHomeButtonClickedHandler()
        {
            OnHomeButtonClicked?.Invoke();
        }

        public void SetActiveHomeButton(bool value)
        {
            _homeButton.gameObject.SetActive(value);
        }

        public void SetActiveContinueButton(bool value)
        {
            _continueButton.gameObject.SetActive(value);
        }

        public void SetEnableContinueButton(bool value)
        {
            _continueButton.enabled = value;
        }

        public void ShowRemoveAdsOffer(bool show)
        {
            _removeAdsOffer.SetActive(show);

            if (show)
            {
                _removeAdsOffer.transform.localScale = Vector2.zero;
                _removeAdsOffer.transform.DOScale(1, 0.33f).SetEase(Ease.OutBack);

                _homeButton.gameObject.SetActive(false);
                _continueButton.gameObject.SetActive(false);

                _continueWithAdsButton.GetComponent<CanvasGroup>().alpha = 0;
                _continueWithAdsButton.GetComponent<CanvasGroup>().DOFade(1, 0.33f)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(1.0f);
            }
        }

        public void OnPurchaseConfirmedHandler(Order order)
        {
            var productId = order.Info.PurchasedProductInfo[0].productId;

            if (productId == _buyRemoveAdsButton.ProductId)
            {
                ShowRemoveAdsOffer(false);
            }

            OnPurchaseConfirmed?.Invoke();
        }

        public void OnPurchaseFailedHandler(FailedOrder failedOrder)
        {
            OnPurchaseFailed?.Invoke();
        }

        public void SetPromoText(string text)
        {
            _promoText.text = text;
        }

        public void HideNextFeatureProgression()
        {
            _nextFeatureContainer.SetActive(false);
        }

        public void PlayNextFeatureProgression(Sprite icon, float fromProgress, float toProgress, float duration = 1.0f, System.Action onComplete = null)
        {
            _continueButton.gameObject.SetActive(false);
            _nextFeatureIcon.sprite = icon;
            _nextFeatureIcon.color = new Color(0, 0, 0, 200 / 255f);
            _nextFeatureProgressBar.value = fromProgress;
            _nextFeaturePercentageText.text = Mathf.RoundToInt(fromProgress * 100f) + "%";

            DOTween.To(
                () => _nextFeatureProgressBar.value,
                v =>
                {
                    _nextFeatureProgressBar.value = v;
                    _nextFeaturePercentageText.text = Mathf.RoundToInt(v * 100f) + "%";
                },
                toProgress,
                duration
            )
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (Mathf.Approximately(toProgress, 1f))
                {
                    _nextFeatureIcon.rectTransform.DOScale(1.2f, duration * 0.4f).From(Vector3.one).SetEase(Ease.OutBack).OnComplete(() =>
                    {
                        _nextFeatureIcon.rectTransform.DOAnchorPosY(15f, 1f).From(new Vector2(0, 0)).SetLoops(-1, LoopType.Yoyo).SetDelay(duration * 0.1f);
                    });
                    _nextFeatureCompleteParticle.gameObject.SetActive(true);
                    _nextFeatureIcon.DOColor(new Color(1, 1, 1, 1), duration * 0.5f).SetEase(Ease.InOutSine);
                    _nextFeaturePercentageText.transform.DOScale(0, duration * 0.1f).SetEase(Ease.InBack);
                }
                DOVirtual.DelayedCall(duration * 0.5f, () =>
                {
                    _continueButton.transform.DOScale(1, duration * 0.4f).From(Vector3.zero).SetEase(Ease.OutBack);
                    _continueButton.gameObject.SetActive(true);
                });

                onComplete?.Invoke();
            });
        }
    }
}
