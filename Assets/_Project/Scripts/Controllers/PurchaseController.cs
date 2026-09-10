using Delta.Core;
using Delta.Services;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Delta.ProjectName
{
    public class PurchaseController : MonoBehaviour
    {
        private IAPService _iapService;
        private ITrackingService _trackingService;
        private IUserDataService _userDataService;

        private UserData Data => (UserData)_userDataService.Data;

        private const string NO_ADS_PRODUCT_ID = "com.delta.projectname.removeads";

        private void Awake()
        {
            _iapService = ServiceLocator.Get<IAPService>();
            _trackingService = ServiceLocator.Get<ITrackingService>();
            _userDataService = ServiceLocator.Get<IUserDataService>();
        }

        private void OnEnable()
        {
            if (_iapService == null) return;
            _iapService.OnPurchasesFetched += HandlePurchasesFetched;
            _iapService.OnPurchaseConfirmed += HandlePurchaseConfirmed;
        }

        private void OnDisable()
        {
            if (_iapService == null) return;
            _iapService.OnPurchasesFetched -= HandlePurchasesFetched;
            _iapService.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            foreach (var confirmedOrder in orders.ConfirmedOrders)
            {
                var productId = confirmedOrder.Info.PurchasedProductInfo[0].productId;

                if (productId == NO_ADS_PRODUCT_ID)
                {
                    Data.HasNoAds = true;
                }
            }
        }

        private void HandlePurchaseConfirmed(Order order)
        {
            if (order?.Info?.PurchasedProductInfo == null || order?.Info?.PurchasedProductInfo.Count == 0)
            {
                Debug.Log("No purchased product info found.");
                return;
            }

            var productId = order.Info.PurchasedProductInfo[0].productId;
            Debug.Log($"Purchased product ID: {productId}");

            switch (productId)
            {
                case NO_ADS_PRODUCT_ID:
                    Data.HasNoAds = true;

                    var product = _iapService.FetchedProducts.Find(p => p.definition.id == productId);
                    var productName = product.metadata.localizedTitle;
                    var price = (double)product.metadata.localizedPrice;
                    var currency = product.metadata.isoCurrencyCode;
                    var transactionId = order.Info.TransactionID;
                    _trackingService.TrackBusinessEvent(productName, productId, price, currency, transactionId);
                    break;
                default:
                    break;
            }
        }
    }
}
