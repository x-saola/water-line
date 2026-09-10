using Delta.Core;
using Delta.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Delta.Common.UI
{
    public class DeltaIAPButton : MonoBehaviour
    {
        private enum ProductType
        {
            Consumable,
            NonConsumable,
            Subscription
        }

        [SerializeField] string _productId;
        [SerializeField] ProductType _productType;
        [SerializeField] Button _button;
        [SerializeField] TextMeshProUGUI _priceText;

        [Space]
        [SerializeField] CanvasGroup _canvasGroup;

        public string ProductId => _productId;
        public Button Button => _button;
        public TextMeshProUGUI PriceText => _priceText;

        private IAPService m_IAPService;

        private void Awake()
        {
            m_IAPService = ServiceLocator.Get<IAPService>();
            _button.onClick.AddListener(OnClickedHandler);
        }

        private void OnEnable()
        {
            var product = m_IAPService.FetchedProducts.Find(p => p.definition.id == _productId);
            if (product != null)
            {
                _priceText.text = product.metadata.localizedPriceString;
            }

            m_IAPService.OnPurchaseConfirmed += OnPurchaseConfirmedHandler;
            m_IAPService.OnPurchaseFailed += OnPurchaseFailedHandler;
        }

        private void OnDisable()
        {
            m_IAPService.OnPurchaseConfirmed -= OnPurchaseConfirmedHandler;
            m_IAPService.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }

        private void OnPurchaseConfirmedHandler(Order order)
        {
            // var productId = order.Info.PurchasedProductInfo[0].productId;
            // if (productId != _productId)
            // {
            //     return;
            // }

            // if (_productType == ProductType.NonConsumable)
            // {
            //     _canvasGroup.interactable = false;
            //     _canvasGroup.alpha = 0.5f;
            //     _priceText.text = "Purchased";
            // }
        }

        private void OnPurchaseFailedHandler(FailedOrder failedOrder)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.alpha = 1f;
        }

        private void OnClickedHandler()
        {
            m_IAPService.Purchase(_productId);

            _canvasGroup.interactable = false;
            _canvasGroup.alpha = 0.5f;
        }
    }
}
