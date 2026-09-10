using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Delta.Services
{
    public class IAPService
    {
        private StoreController _storeController;
        private List<Product> _fetchedProducts;

        public bool IsInitialized { get; private set; } = false;
        public List<Product> FetchedProducts => _fetchedProducts;

        public event System.Action<PendingOrder> OnPurchasePending;
        public event System.Action<Order> OnPurchaseConfirmed;
        public event System.Action<FailedOrder> OnPurchaseFailed;
        public event System.Action<DeferredOrder> OnPurchaseDeferred;
        public event System.Action<Orders> OnPurchasesFetched;

        public IAPService()
        {
            InitializeIAP();
        }

        private async void InitializeIAP()
        {
            _storeController = UnityIAPServices.StoreController();

            _storeController.OnPurchasePending += HandlePurchasePending;
            _storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            _storeController.OnPurchaseFailed += HandlePurchaseFailed;
            _storeController.OnPurchaseDeferred += HandlePurchaseDeferred;

            await _storeController.Connect();

            _storeController.OnProductsFetched += HandleProductsFetched;
            _storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
            _storeController.OnPurchasesFetched += HandlePurchasesFetched;
            _storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;

            var initialProductsToFetch = new List<ProductDefinition>
            {
                new("removeads", ProductType.NonConsumable),
            };

            _storeController.FetchProducts(initialProductsToFetch);
        }

        private void HandleProductsFetched(List<Product> products)
        {
            _fetchedProducts = products;
            _storeController.FetchPurchases();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed error)
        {
            Debug.Log($"Product fetch failed: {error.FailureReason}");
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            IsInitialized = true;
            OnPurchasesFetched?.Invoke(orders);
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription error)
        {
            Debug.Log($"Purchases fetch failed: {error.FailureReason}");
        }

        public void Purchase(string productId)
        {
            if (!IsInitialized)
            {
                return;
            }

            _storeController.PurchaseProduct(productId);
        }

        private void HandlePurchasePending(PendingOrder pendingOrder)
        {
            Debug.Log($"Pending order: {pendingOrder}");
            
            _storeController.ConfirmPurchase(pendingOrder);
        }

        private void HandlePurchaseConfirmed(Order order)
        {
            Debug.Log($"Purchase confirmed: {order}");

            if (order?.Info?.PurchasedProductInfo == null || order?.Info?.PurchasedProductInfo.Count == 0)
            {
                Debug.Log("No purchased product info found.");
                return;
            }

            OnPurchaseConfirmed?.Invoke(order);
        }

        private void HandlePurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log($"Purchase failed: {failedOrder}");

            OnPurchaseFailed?.Invoke(failedOrder);
        }

        private void HandlePurchaseDeferred(DeferredOrder deferredOrder)
        {
            Debug.Log($"Purchase deferred: {deferredOrder}");

            OnPurchaseDeferred?.Invoke(deferredOrder);
        }
    }
}
