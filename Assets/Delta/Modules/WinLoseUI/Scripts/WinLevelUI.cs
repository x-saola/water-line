using Delta.Common.UI;
using Delta.Core;
using Delta.Services;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Delta.Modules
{
    public class WinLevelUI : BounceAppearUI
    {
        [Space]
        [SerializeField] Image _overlay;
        [SerializeField] Button _continueButton;
        [SerializeField] Button _homeButton;
        [SerializeField] Button _retryButton;
        [SerializeField] TextMeshProUGUI _completionTimeText;
        [SerializeField] RectTransform _boardZone;

        [Header("Remove Ads")]
        [SerializeField] GameObject _removeAdsOffer;
        [SerializeField] TextMeshProUGUI _promoText;
        [SerializeField] DeltaIAPButton _buyRemoveAdsButton;
        [SerializeField] Button _continueWithAdsButton;

        private IAPService m_IAPService;

        public RectTransform BoardZone => _boardZone;
        public System.Action OnContinueButtonClicked;
        public System.Action OnHomeButtonClicked;
        public System.Action OnRetryButtonClicked;

        public System.Action OnPurchaseConfirmed;
        public System.Action OnPurchaseFailed;

        protected override void Awake()
        {
            base.Awake();
            m_IAPService = ServiceLocator.Get<IAPService>();
            _homeButton.onClick.AddListener(OnHomeButtonClickedHandler);
            _continueButton.onClick.AddListener(OnContinueButtonClickedHandler);
            _continueWithAdsButton.onClick.AddListener(OnContinueButtonClickedHandler);
            _retryButton.onClick.AddListener(OnRetryButtonClickedHandler);

            _overlay.gameObject.SetActive(false);
            _parentContent.gameObject.SetActive(false);
            ShowRemoveAdsOffer(false);
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
            _overlay.DOFade(253/255f, 0.33f);

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

        private void OnRetryButtonClickedHandler()
        {
            OnRetryButtonClicked?.Invoke();
        }

        public void SetActiveHomeButton(bool value)
        {
            _homeButton.gameObject.SetActive(value);
        }

        public void SetActiveContinueButton(bool value)
        {
            _continueButton.gameObject.SetActive(value);
        }

        public void SetActiveRetryButton(bool value)
        {
            _retryButton.gameObject.SetActive(value);
        }

        public void SetEnableContinueButton(bool value)
        {
            _continueButton.enabled = value;
        }

        // "Piped in mm:ss" (spec) - completion time shown on the Win overlay.
        public void SetCompletionTime(string formattedTime)
        {
            if (_completionTimeText != null)
                _completionTimeText.text = formattedTime;
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
    }
}
